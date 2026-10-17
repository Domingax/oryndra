using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>
/// A collision query made of blocks, used to test the movement seam without an engine.
/// </summary>
/// <remarks>
/// The implementation is deliberately blunt: it resolves one axis at a time and
/// clamps the moving body against every block it would cross. That is enough to
/// answer the questions the simulation asks, and being immune to tunnelling means
/// the fuzz harness can use it as the authority on "did the actor stay outside
/// the geometry".
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
        foreach (var solid in solids)
        {
            if (solid.Intersects(volume))
            {
                return true;
            }
        }

        return false;
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

        var candidate = current + delta;
        var volume = Box.BodyVolume(position, body);
        var offsetLow = Component(volume.Min, axis) - current;
        var offsetHigh = Component(volume.Max, axis) - current;

        foreach (var solid in solids)
        {
            if (!CrossesOnOtherAxes(volume, solid, axis))
            {
                continue;
            }

            var solidMin = Component(solid.Min, axis);
            var solidMax = Component(solid.Max, axis);

            // The body's faces sweep an interval; the block is in the way when
            // that interval meets the block's. Using the faces rather than the
            // centre is what keeps a fast body from ending up buried in a wall.
            var faceFrom = MathF.Min(current, candidate) + offsetLow;
            var faceTo = MathF.Max(current, candidate) + offsetHigh;
            if (faceTo <= solidMin || faceFrom >= solidMax)
            {
                continue;
            }

            if (delta > 0f)
            {
                candidate = MathF.Min(candidate, solidMin - offsetHigh);
            }
            else
            {
                candidate = MathF.Max(candidate, solidMax - offsetLow);
                if (axis == Axis.Y)
                {
                    grounded = true;
                }
            }
        }

        return candidate;
    }

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
