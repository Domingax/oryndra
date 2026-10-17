using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The movement seam: one actor's motion through the geometry around it, tick by tick.
/// </summary>
/// <remarks>
/// <para>
/// The simulation is pure with respect to its inputs: it reads nothing but the
/// <see cref="InputFrame"/> it is handed and the collision query it was built with,
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

    private readonly ICollisionQuery collision;
    private readonly MovementTuning tuning;
    private ActorState state;

    /// <summary>Initializes a new instance of the <see cref="MovementSimulation"/> class.</summary>
    /// <param name="collision">The geometry the actor moves through.</param>
    /// <param name="initialState">The state to start from; defaults to standing at the origin.</param>
    /// <param name="tuning">The tuning to use; defaults to <see cref="MovementTuning.Default"/>.</param>
    public MovementSimulation(
        ICollisionQuery collision,
        ActorState? initialState = null,
        MovementTuning? tuning = null)
    {
        ArgumentNullException.ThrowIfNull(collision);

        this.collision = collision;
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

    private ActorState Step(InputFrame input, float deltaSeconds)
    {
        // The ground state comes from the collision query, never from an inherited
        // flag alone: an actor spawned on the floor is grounded on its very first
        // tick, so a jump is never silently dropped. A flag left over from the
        // previous tick counts too, which is what keeps a body that has just walked
        // off a ledge under ground control for one tick.
        bool underfoot = HasGroundBelow();
        bool grounded = state.Grounded || underfoot;

        // Coyote time: the window after leaving the ground in which a jump still
        // counts. Only real ground refills it — the inherited flag above is not
        // enough, or the window would be a tick wider than it is tuned to be.
        // Standing on the ground refills it; the jump that spends it empties it, so
        // a press firing once cannot fire again next tick.
        float coyote = underfoot
            ? tuning.CoyoteTime
            : MathF.Max(0f, state.CoyoteSecondsLeft - deltaSeconds);

        Vector2 wish = Planar(input.WishDirection);
        (Gait gait, float reserve) = ChooseGait(input, wish, grounded, deltaSeconds, state.SprintReserve);

        Vector2 planar = Approach(
            wish * SpeedOf(gait),
            Planar(state.Velocity),
            Acceleration(deltaSeconds, grounded));

        float vertical = state.Velocity.Y;
        if (grounded && vertical < 0f)
        {
            vertical = 0f;
        }

        if (input.Jump && (grounded || coyote > 0f))
        {
            vertical = tuning.JumpSpeed;
            coyote = 0f;
        }

        vertical -= tuning.Gravity * deltaSeconds;

        var requested = new Vector3(planar.X, vertical, planar.Y);
        var moved = collision.MoveBody(state.Position, requested * deltaSeconds, tuning.Body);

        // The actor keeps only the motion the geometry allowed: an axis that ran
        // into it loses its speed instead of pushing through.
        var achieved = moved.Motion / deltaSeconds;
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
            Gait = gait,
            SprintReserve = reserve,
            CoyoteSecondsLeft = coyote,
        };
    }

    /// <summary>
    /// Picks the speed band for this tick and moves the sprint reserve it costs.
    /// </summary>
    /// <param name="input">What the player asked for.</param>
    /// <param name="wish">The direction asked for, unit length or zero.</param>
    /// <param name="grounded">Whether the actor has the ground under it.</param>
    /// <param name="deltaSeconds">The tick's duration, in seconds.</param>
    /// <param name="reserve">The sprint reserve the actor has, from 0 to 1.</param>
    /// <returns>The band the actor moves in, and the reserve it leaves behind.</returns>
    private (Gait Gait, float Reserve) ChooseGait(
        InputFrame input,
        Vector2 wish,
        bool grounded,
        float deltaSeconds,
        float reserve)
    {
        // The stick's travel bands the target speed: a light push walks, a full
        // push runs.
        bool moving = wish != Vector2.Zero;
        bool running = moving && input.MoveAxis.Length() >= tuning.RunAxisThreshold;

        Gait gait;
        if (!moving)
        {
            gait = Gait.Idle;
        }
        else if (running)
        {
            gait = Gait.Run;
        }
        else
        {
            gait = Gait.Walk;
        }

        // The sprint reserve is a hysteresis rather than a button. It fills while
        // the actor runs on the ground, a full reserve turns the run into a sprint,
        // and the sprint holds until the reserve is spent — at which point the
        // reserve is empty and the engage threshold is a whole run away again.
        bool sprinting = state.Gait == Gait.Sprint && running && reserve > tuning.SprintReleaseThreshold;
        if (!sprinting && running && grounded && reserve >= tuning.SprintEngageThreshold)
        {
            sprinting = true;
        }

        if (sprinting)
        {
            reserve = MathF.Max(0f, reserve - (tuning.SprintDrainRate * deltaSeconds));
        }
        else if (running && grounded)
        {
            reserve = MathF.Min(1f, reserve + (tuning.SprintBuildRate * deltaSeconds));
        }

        return (sprinting ? Gait.Sprint : gait, reserve);
    }

    /// <summary>Gets the speed a band moves at, in metres per second.</summary>
    private float SpeedOf(Gait gait) => gait switch
    {
        Gait.Sprint => tuning.SprintSpeed,
        Gait.Run => tuning.RunSpeed,
        Gait.Walk => tuning.WalkSpeed,
        _ => 0f,
    };

    /// <summary>Projects a vector onto the horizontal plane the actor walks on.</summary>
    private static Vector2 Planar(Vector3 vector) => new(vector.X, vector.Z);

    /// <summary>Asks the collision query whether anything solid is just below the actor's feet.</summary>
    /// <returns><see langword="true"/> when a downward probe is stopped by a floor.</returns>
    private bool HasGroundBelow() =>
        collision.MoveBody(state.Position, new Vector3(0f, -GroundProbeDistance, 0f), tuning.Body).Grounded;

    private float Acceleration(float deltaSeconds, bool grounded) =>
        (grounded ? tuning.GroundAcceleration : tuning.AirAcceleration) * deltaSeconds;

    /// <summary>Moves <paramref name="from"/> towards <paramref name="to"/> by at most <paramref name="maxDistance"/>.</summary>
    private static Vector2 Approach(Vector2 to, Vector2 from, float maxDistance)
    {
        var difference = to - from;
        var distance = difference.Length();
        return distance <= maxDistance ? to : from + (difference / distance * maxDistance);
    }
}
