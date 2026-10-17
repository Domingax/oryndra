using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The observable state of an actor after a simulation tick.
/// </summary>
/// <remarks>
/// <see cref="Position"/> is the bottom centre of the actor's body, in metres: it
/// is the point the actor stands on and the point the collision query sweeps. Yaw
/// is the only rotation the movement model needs, so it is stored as a scalar
/// rather than a basis.
/// </remarks>
public readonly record struct ActorState
{
    /// <summary>Gets the bottom centre of the actor's body, in metres.</summary>
    public Vector3 Position { get; init; }

    /// <summary>Gets the actor's velocity, in metres per second.</summary>
    public Vector3 Velocity { get; init; }

    /// <summary>Gets the facing direction in radians, where 0 faces +Z.</summary>
    public float Yaw { get; init; }

    /// <summary>Gets a value indicating whether the actor is standing on solid ground.</summary>
    public bool Grounded { get; init; }

    /// <summary>
    /// Gets a value indicating whether every part of the state is a finite number.
    /// </summary>
    /// <remarks>
    /// A simulation that produced a NaN position can never recover: the actor
    /// would silently stop colliding and disappear. The fuzz harness asserts this
    /// invariant on every tick.
    /// </remarks>
    public bool IsFinite =>
        float.IsFinite(Position.X)
        && float.IsFinite(Position.Y)
        && float.IsFinite(Position.Z)
        && float.IsFinite(Velocity.X)
        && float.IsFinite(Velocity.Y)
        && float.IsFinite(Velocity.Z)
        && float.IsFinite(Yaw);

    /// <summary>Creates a resting state at <paramref name="position"/>.</summary>
    /// <param name="position">The bottom centre of the actor's body, in metres.</param>
    /// <returns>A state with no velocity, facing +Z.</returns>
    public static ActorState StandingAt(Vector3 position) => new() { Position = position };
}
