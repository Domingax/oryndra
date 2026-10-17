using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// A collision query made of blocks: the core's reference geometry.
/// </summary>
/// <remarks>
/// <para>
/// It answers the seam without an engine, which makes it both the geometry the
/// engine-free tests run against and the geometry a greybox is built from — the
/// game asks it the same questions a test does, so a block behaves the same on a
/// screen as in an assertion. A Map's own geometry (ticket #5) will answer the
/// seam in its place; the seam does not move.
/// </para>
/// <para>
/// The implementation is deliberately blunt: it resolves one axis at a time and
/// clamps the moving body against every block it would cross. That is enough to
/// answer the questions the simulation asks, and being immune to tunnelling means
/// the fuzz harness can use it as the authority on "did the actor stay outside
/// the geometry".
/// </para>
/// </remarks>
public sealed class BoxRoom : ICollisionQuery
{
    private readonly List<Box> solids = [];

    /// <summary>Gets the blocks that make up this room.</summary>
    public IReadOnlyList<Box> Solids => solids;

    /// <summary>Gets the smallest box containing every block: the room's outer limit.</summary>
    /// <exception cref="InvalidOperationException">The room has no geometry at all.</exception>
    public Box OuterBounds
    {
        get
        {
            if (solids.Count == 0)
            {
                throw new InvalidOperationException("A room needs at least one block before it can be bounded.");
            }

            var bounds = solids[0];
            for (var index = 1; index < solids.Count; index++)
            {
                bounds = bounds.Union(solids[index]);
            }

            return bounds;
        }
    }

    /// <summary>Builds a room: a floor, four walls, and open sky.</summary>
    /// <param name="halfWidth">Inner half width of the room, in metres.</param>
    /// <param name="wallHeight">Height of the walls above the floor, in metres.</param>
    /// <param name="thickness">Wall and floor thickness, in metres.</param>
    /// <returns>A room whose floor top sits at y = 0.</returns>
    public static BoxRoom Room(float halfWidth = 8f, float wallHeight = 4f, float thickness = 0.5f)
    {
        var outer = halfWidth + thickness;
        return new BoxRoom()
            .Add(Box.FromCorners(
                new Vector3(-outer, -thickness, -outer),
                new Vector3(outer, 0f, outer)))
            .Add(Box.FromCorners(
                new Vector3(halfWidth, -thickness, -outer),
                new Vector3(outer, wallHeight, outer)))
            .Add(Box.FromCorners(
                new Vector3(-outer, -thickness, -outer),
                new Vector3(-halfWidth, wallHeight, outer)))
            .Add(Box.FromCorners(
                new Vector3(-outer, -thickness, halfWidth),
                new Vector3(outer, wallHeight, outer)))
            .Add(Box.FromCorners(
                new Vector3(-outer, -thickness, -outer),
                new Vector3(outer, wallHeight, -halfWidth)));
    }

    /// <summary>Adds a block of solid geometry.</summary>
    /// <param name="box">The block to add.</param>
    /// <returns>This room, so blocks can be chained.</returns>
    public BoxRoom Add(Box box)
    {
        solids.Add(box);
        return this;
    }

    /// <summary>Gets a value indicating whether a body standing at <paramref name="feet"/> is inside a block.</summary>
    /// <param name="feet">Bottom centre of the body, in metres.</param>
    /// <param name="body">The body being checked.</param>
    /// <returns><see langword="true"/> when the body's volume intersects a block.</returns>
    public bool Overlaps(Vector3 feet, BodyShape body)
    {
        var volume = Box.BodyVolume(feet, body);
        return solids.Any(solid => solid.Intersects(volume));
    }

    /// <inheritdoc />
    public BodyMoveResult MoveBody(Vector3 position, Vector3 delta, BodyShape body)
    {
        var start = position;
        var grounded = false;

        // One axis at a time: floors and ceilings, then the horizontals.
        position = position with { Y = ClampAxis(position, Axis.Y, delta.Y, body, ref grounded) };
        position = position with { X = ClampAxis(position, Axis.X, delta.X, body, ref grounded) };
        position = position with { Z = ClampAxis(position, Axis.Z, delta.Z, body, ref grounded) };

        return new BodyMoveResult(position, position - start, grounded);
    }

    private float ClampAxis(Vector3 position, Axis axis, float delta, BodyShape body, ref bool grounded)
    {
        var current = Component(position, axis);
        if (delta == 0f)
        {
            return current;
        }

        var volume = Box.BodyVolume(position, body);
        var sweep = new AxisSweep(
            current,
            current + delta,
            delta,
            Component(volume.Min, axis) - current,
            Component(volume.Max, axis) - current);

        foreach (var solid in solids)
        {
            if (CrossesOnOtherAxes(volume, solid, axis))
            {
                sweep = StopAt(sweep, solid, axis, ref grounded);
            }
        }

        return sweep.Candidate;
    }

    /// <summary>
    /// Narrows a move to a block's face, when that block stops it.
    /// </summary>
    /// <param name="sweep">Where the body's faces are, and where the move would take them.</param>
    /// <param name="solid">The block the body crosses on the other axes.</param>
    /// <param name="axis">The axis being resolved.</param>
    /// <param name="grounded">Set when a downward move is stopped, meaning the actor stands on it.</param>
    /// <returns>The move, narrowed if the block is in the way.</returns>
    /// <remarks>
    /// Only a face the body reaches from the outside stops it, and only when the move
    /// would actually cross that face. Both halves matter: a body standing against a
    /// block has its volume a hair inside it from the last tick's float error, and a
    /// body falling *past* a tall block is not standing on it. Without the first, a
    /// body pressed against a wall was resolved against the wall's far face — thrown
    /// onto its top, or across the level, with the throw fed back as speed. Without
    /// the second, a body in the air over a floor it has left is reported as grounded.
    /// </remarks>
    private static AxisSweep StopAt(AxisSweep sweep, Box solid, Axis axis, ref bool grounded)
    {
        var solidMin = Component(solid.Min, axis);
        var solidMax = Component(solid.Max, axis);

        if (sweep.Delta > 0f)
        {
            return sweep.Current + sweep.OffsetHigh > solidMin
                ? sweep
                : sweep with { Candidate = MathF.Min(sweep.Candidate, solidMin - sweep.OffsetHigh) };
        }

        if (sweep.Current + sweep.OffsetLow < solidMax)
        {
            return sweep;
        }

        float stop = solidMax - sweep.OffsetLow;
        if (sweep.Candidate > stop)
        {
            return sweep;
        }

        if (axis == Axis.Y)
        {
            grounded = true;
        }

        return sweep with { Candidate = stop };
    }

    /// <summary>One axis of one move: where the body's faces are, and where they would go.</summary>
    /// <param name="Current">The body's position on the axis, before the move.</param>
    /// <param name="Candidate">Where the move would put it.</param>
    /// <param name="Delta">How far the move asks to travel.</param>
    /// <param name="OffsetLow">The body's low face, relative to its position.</param>
    /// <param name="OffsetHigh">The body's high face, relative to its position.</param>
    private readonly record struct AxisSweep(
        float Current,
        float Candidate,
        float Delta,
        float OffsetLow,
        float OffsetHigh);

    private static bool CrossesOnOtherAxes(Box volume, Box solid, Axis axis) => axis switch
    {
        Axis.X => volume.Min.Y < solid.Max.Y && solid.Min.Y < volume.Max.Y
            && volume.Min.Z < solid.Max.Z && solid.Min.Z < volume.Max.Z,
        Axis.Y => volume.Min.X < solid.Max.X && solid.Min.X < volume.Max.X
            && volume.Min.Z < solid.Max.Z && solid.Min.Z < volume.Max.Z,
        _ => volume.Min.X < solid.Max.X && solid.Min.X < volume.Max.X
            && volume.Min.Y < solid.Max.Y && solid.Min.Y < volume.Max.Y,
    };

    private static float Component(Vector3 vector, Axis axis) => axis switch
    {
        Axis.X => vector.X,
        Axis.Y => vector.Y,
        _ => vector.Z,
    };

    private enum Axis
    {
        X,
        Y,
        Z,
    }
}
