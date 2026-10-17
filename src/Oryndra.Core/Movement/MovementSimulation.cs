using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The movement seam: one actor's motion through the world, tick by tick.
/// </summary>
/// <remarks>
/// <para>
/// The simulation is pure with respect to its inputs: it reads nothing but the
/// <see cref="InputFrame"/> it is handed and the world it was constructed with,
/// and it never consults a clock or a random source. That is what makes replays,
/// Ghosts, headless tests and the fuzz harness possible.
/// </para>
/// <para>
/// One tick is one <see cref="SimulationClock"/> step. The engine layer is
/// responsible for calling <see cref="Advance"/> at the fixed rate and for
/// interpolating the state between ticks.
/// </para>
/// </remarks>
public sealed class MovementSimulation
{
    /// <summary>
    /// How far below the feet the simulation looks for the ground when the previous
    /// tick did not already report it.
    /// </summary>
    private const float GroundProbeDistance = 0.05f;

    private readonly IWorldQuery world;
    private readonly MovementTuning tuning;
    private ActorState state;

    /// <summary>Initializes a new instance of the <see cref="MovementSimulation"/> class.</summary>
    /// <param name="world">The world the actor moves through.</param>
    /// <param name="initialState">The state to start from; defaults to standing at the origin.</param>
    /// <param name="tuning">The tuning to use; defaults to <see cref="MovementTuning.Default"/>.</param>
    public MovementSimulation(
        IWorldQuery world,
        ActorState? initialState = null,
        MovementTuning? tuning = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        this.world = world;
        this.tuning = tuning ?? MovementTuning.Default;
        state = initialState ?? ActorState.StandingAt(Vector3.Zero);
    }

    /// <summary>Gets the state the actor is in now.</summary>
    public ActorState State => state;

    /// <summary>Puts the actor back into a known state, e.g. at the start of a replay.</summary>
    /// <param name="newState">The state to start the next tick from.</param>
    public void Reset(ActorState newState) => state = newState;

    /// <summary>Advances the actor by exactly one tick.</summary>
    /// <param name="input">What the player asked for during this tick.</param>
    /// <param name="deltaSeconds">The tick's duration, in seconds.</param>
    /// <returns>The actor's state after the tick, the same value <see cref="State"/> now holds.</returns>
    public ActorState Advance(InputFrame input, float deltaSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deltaSeconds);

        state = Step(input, deltaSeconds);
        return state;
    }

    private ActorState Step(InputFrame input, float dt)
    {
        // The ground state comes from the world, never from an inherited flag
        // alone: an actor spawned on the floor is grounded on its very first tick,
        // so a jump is never silently dropped. A flag left over from the previous
        // tick counts too, which is exactly one tick of coyote time.
        var grounded = state.Grounded || HasGroundBelow();

        var planar = Approach(
            Planar(input.WishDirection * tuning.MoveSpeed),
            Planar(state.Velocity),
            Acceleration(tuning, dt, grounded));

        var vertical = state.Velocity.Y;
        if (grounded && vertical < 0f)
        {
            vertical = 0f;
        }

        if (input.Jump && grounded)
        {
            vertical = tuning.JumpSpeed;
        }

        vertical -= tuning.Gravity * dt;

        var requested = new Vector3(planar.X, vertical, planar.Y);
        var moved = world.MoveBody(state.Position, requested * dt, tuning.Body);

        // The actor keeps only the motion the world allowed: an axis that ran
        // into geometry loses its speed instead of pushing through it.
        var achieved = moved.Motion / dt;
        if (moved.Grounded)
        {
            achieved = new Vector3(achieved.X, 0f, achieved.Z);
        }

        return new ActorState
        {
            Position = moved.Position,
            Velocity = achieved,
            Yaw = input.Yaw,
            Grounded = moved.Grounded,
        };
    }

    /// <summary>Projects a vector onto the horizontal plane the actor walks on.</summary>
    private static Vector2 Planar(Vector3 vector) => new(vector.X, vector.Z);

    /// <summary>Asks the world whether anything solid is just below the actor's feet.</summary>
    /// <returns><see langword="true"/> when a downward probe is stopped by a floor.</returns>
    private bool HasGroundBelow() =>
        world.MoveBody(state.Position, new Vector3(0f, -GroundProbeDistance, 0f), tuning.Body).Grounded;

    private static float Acceleration(MovementTuning tuning, float dt, bool grounded) =>
        (grounded ? tuning.GroundAcceleration : tuning.AirAcceleration) * dt;

    /// <summary>Moves <paramref name="from"/> towards <paramref name="to"/> by at most <paramref name="maxDistance"/>.</summary>
    private static Vector2 Approach(Vector2 to, Vector2 from, float maxDistance)
    {
        var difference = to - from;
        var distance = difference.Length();
        return distance <= maxDistance ? to : from + (difference / distance * maxDistance);
    }
}
