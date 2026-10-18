using Godot;

using Oryndra.Core.Movement;

using SysVec3 = System.Numerics.Vector3;

namespace Oryndra.Game;

/// <summary>
/// The collision seam backed by the engine's physics space.
/// </summary>
/// <remarks>
/// <para>
/// It answers the one question the simulation asks — how far can this body move
/// before it meets solid geometry — with a swept capsule, resolved one axis at a
/// time. Resolving Y before X and Z is what lets the actor slide along a wall
/// instead of sticking to it, and it is the same order the engine-agnostic test
/// room uses, so the two seam implementations agree.
/// </para>
/// <para>
/// This class holds no movement rule: it converts the simulation's request into a
/// physics query and reports what the engine did. Swapping it for a different
/// backend changes nothing above the seam.
/// </para>
/// <para>
/// The shape and the query are built once and reused, because the query runs on
/// every tick and the simulation's frame budget is not the place for garbage.
/// </para>
/// </remarks>
public sealed class GodotCollisionQuery : ICollisionQuery, IDisposable
{
    /// <summary>The physics layer the greybox geometry is on.</summary>
    private const uint WorldCollisionLayer = 1;

    /// <summary>How thick the query's shape is, in metres, when it looks for a resting contact.</summary>
    private const float QueryMargin = 0.001f;

    /// <summary>
    /// How far above the feet the query's shape starts, in metres. The simulated
    /// body stands on the floor; a shape that also starts exactly on it grazes the
    /// floor on every horizontal sweep, and the physics engine reports that graze
    /// as a wall. Lifting the shape a few millimetres keeps the sweeps honest.
    /// </summary>
    private const float ShapeClearance = 0.005f;

    /// <summary>
    /// How far short of a surface the sweep stops, in metres. Without it the body
    /// would come to rest exactly on a face, and the next sweep would ignore that
    /// face as an existing overlap — an actor pressed against a wall could walk
    /// through it one tick later. The gap is far below what the eye can see.
    /// </summary>
    private const float Skin = 0.002f;

    /// <summary>How vertical a contact normal must be to count as floor.</summary>
    private const float MinimumFloorNormalY = 0.5f;

    private readonly Node3D world;
    private readonly CapsuleShape3D capsule = new();
    private readonly PhysicsShapeQueryParameters3D query = new()
    {
        CollisionMask = WorldCollisionLayer,
        CollideWithAreas = false,
        CollideWithBodies = true,
        Margin = QueryMargin,
    };

    private BodyShape defaultBody;

    /// <summary>Initializes a new instance of the <see cref="GodotCollisionQuery"/> class.</summary>
    /// <param name="world">A node inside the physics world to query, in metres.</param>
    /// <param name="body">The body volume every cast uses unless another is passed.</param>
    public GodotCollisionQuery(Node3D world, BodyShape body)
    {
        this.world = world;
        defaultBody = body;
        Reshape(body);
        query.Shape = capsule;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        query.Dispose();
        capsule.Dispose();
    }

    /// <inheritdoc />
    public BodyMoveResult MoveBody(SysVec3 position, SysVec3 delta, BodyShape body)
    {
        if (body != defaultBody)
        {
            defaultBody = body;
            Reshape(body);
        }

        var start = position;
        var grounded = false;

        position = position with { Y = SweepAxis(position, Axis.Y, delta.Y, ref grounded) };
        position = position with { X = SweepAxis(position, Axis.X, delta.X, ref grounded) };
        position = position with { Z = SweepAxis(position, Axis.Z, delta.Z, ref grounded) };

        return new BodyMoveResult(position, position - start, grounded);
    }

    /// <summary>
    /// Sweeps the body along one axis and returns the coordinate it stopped at. A
    /// downward sweep that was stopped is what "grounded" means.
    /// </summary>
    private float SweepAxis(SysVec3 position, Axis axis, float delta, ref bool grounded)
    {
        var current = Component(position, axis);
        if (delta == 0f)
        {
            return current;
        }

        query.Transform = new Transform3D(
            Basis.Identity,
            GodotInterop.ToGodot(position) + new Vector3(0f, defaultBody.Height * 0.5f, 0f));
        query.Motion = GodotInterop.ToGodot(AxisVector(axis) * delta);

        var result = world.GetWorld3D().DirectSpaceState.CastMotion(query);
        var safe = result.Length > 0 ? Mathf.Clamp(result[0], 0f, 1f) : 1f;

        if (axis == Axis.Y && delta < 0f && (safe < 1f || IsRestingOnFloor()))
        {
            // A sweep ignores a surface the body already touches, so a body
            // spawned or left exactly on a floor would report no collision and
            // fall through it. The resting contact answers for that case.
            grounded = true;
            if (safe >= 1f)
            {
                return current;
            }
        }

        if (safe >= 1f)
        {
            return current + delta;
        }

        // Stop a skin short of the contact so the next sweep still sees it.
        return current + MathF.CopySign(MathF.Max(0f, (MathF.Abs(delta) * safe) - Skin), delta);
    }

    /// <summary>Asks the physics space whether the body's feet already touch a floor.</summary>
    private bool IsRestingOnFloor()
    {
        var contact = world.GetWorld3D().DirectSpaceState.GetRestInfo(query);
        return contact.Count > 0 && contact["normal"].AsVector3().Y > MinimumFloorNormalY;
    }

    private void Reshape(BodyShape shape)
    {
        capsule.Radius = shape.Radius;
        capsule.Height = Mathf.Max(shape.Height - (2f * ShapeClearance), shape.Radius * 2f);
    }

    private static SysVec3 AxisVector(Axis axis) => axis switch
    {
        Axis.X => new SysVec3(1f, 0f, 0f),
        Axis.Y => new SysVec3(0f, 1f, 0f),
        _ => new SysVec3(0f, 0f, 1f),
    };

    private static float Component(SysVec3 vector, Axis axis) => axis switch
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
