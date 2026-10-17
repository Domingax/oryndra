using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The observable state of an actor after a simulation tick.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Position"/> is the bottom centre of the actor's body, in metres: it
/// is the point the actor stands on and the point the collision query sweeps. Yaw
/// is the only rotation the movement model needs, so it is stored as a scalar
/// rather than a basis.
/// </para>
/// <para>
/// The state carries the decisions a later tick builds on as well as the position:
/// the <see cref="Gait"/> and the <see cref="SprintReserve"/> it came from, and the
/// <see cref="CoyoteSecondsLeft"/> a jump can still spend. They live here rather
/// than inside the simulation so that <see cref="MovementSimulation.Reset"/>
/// restores them, a Ghost replays them and presentation draws them.
/// </para>
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

    /// <summary>Gets the speed band the simulation moved the actor in this tick.</summary>
    public Gait Gait { get; init; }

    /// <summary>Gets the sprint reserve, from 0 (spent) to 1 (full).</summary>
    /// <remarks>
    /// A reserve rather than a held button, as the PRD asks: it fills while the
    /// actor runs on the ground, a full reserve turns the run into a sprint, and
    /// the sprint lasts until the reserve is spent.
    /// </remarks>
    public float SprintReserve { get; init; }

    /// <summary>Gets how much of the coyote window is left, in seconds.</summary>
    /// <remarks>
    /// Refreshed while the actor is on the ground and spent by the jump that uses
    /// it, so that a held button cannot fire twice. It is the baseline input
    /// tolerance the PRD keeps always on.
    /// </remarks>
    public float CoyoteSecondsLeft { get; init; }

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
        && float.IsFinite(Yaw)
        && float.IsFinite(SprintReserve)
        && float.IsFinite(CoyoteSecondsLeft);

    /// <summary>Creates a resting state at <paramref name="position"/>.</summary>
    /// <param name="position">The bottom centre of the actor's body, in metres.</param>
    /// <returns>A state with no velocity, facing +Z, and no sprint reserve.</returns>
    public static ActorState StandingAt(Vector3 position) => new() { Position = position };
}
