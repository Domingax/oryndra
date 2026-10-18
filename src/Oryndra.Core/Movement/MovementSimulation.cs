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
/// <para>
/// Locomotion is a small set of speed bands rather than a single top speed. The
/// input magnitude picks walk or run; running long enough with a charged sprint
/// meter promotes the actor to sprint, which spends it. The meter is always
/// draining or refilling, so sprinting is a rhythm to manage, never a boolean to
/// hold.
/// </para>
/// </remarks>
public sealed class MovementSimulation
{
    /// <summary>
    /// How far below the feet the simulation looks for the ground when the previous
    /// tick did not already report it.
    /// </summary>
    private const float GroundProbeDistance = 0.05f;

    /// <summary>Below this input magnitude the stick counts as released.</summary>
    private const float MinimumAxisMagnitude = 1e-4f;

    private readonly ICollisionQuery collision;
    private readonly MovementTuning tuning;
    private ActorState state;

    /// <summary>How much of the coyote window is left, in seconds.</summary>
    /// <remarks>Per-episode history rather than observable state; <see cref="Reset"/> clears it.</remarks>
    private float coyoteSeconds;

    /// <summary>How long the actor has been running without a break, in seconds.</summary>
    /// <remarks>Per-episode history rather than observable state; <see cref="Reset"/> clears it.</remarks>
    private float runSeconds;

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
    /// <remarks>
    /// The tick-to-tick history <see cref="ActorState"/> does not carry — the coyote
    /// window and the sprint build-up timer — is cleared with it, so a replay starts
    /// a fresh episode and reproduces a recorded run from its first tick.
    /// </remarks>
    public void Reset(ActorState newState)
    {
        state = newState;
        coyoteSeconds = 0f;
        runSeconds = 0f;
    }

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
        // tick, so a jump is never silently dropped. An inherited flag counts for
        // exactly one tick; past that, the coyote window decides.
        var grounded = state.Grounded || HasGroundBelow();
        coyoteSeconds = grounded
            ? tuning.CoyoteSeconds
            : MathF.Max(0f, coyoteSeconds - deltaSeconds);

        var inputBand = BandFor(input.MoveAxis.Length());
        if (inputBand == SpeedBand.Run)
        {
            if (grounded)
            {
                runSeconds += deltaSeconds;
            }
        }
        else
        {
            runSeconds = 0f;
        }

        var band = NextBand(inputBand, grounded);

        var targetSpeed = band switch
        {
            SpeedBand.Walk => tuning.WalkSpeed,
            SpeedBand.Run => tuning.RunSpeed,
            SpeedBand.Sprint => tuning.SprintSpeed,
            _ => 0f,
        };

        var planar = Approach(
            Planar(input.WishDirection * targetSpeed),
            Planar(state.Velocity),
            Acceleration(deltaSeconds, grounded));

        var vertical = state.Velocity.Y;
        if (grounded && vertical < 0f)
        {
            vertical = 0f;
        }

        if (input.Jump && (grounded || coyoteSeconds > 0f))
        {
            vertical = tuning.JumpSpeed;
            coyoteSeconds = 0f;
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
            SpeedBand = band,
            SprintReserve = NextSprintReserve(band, deltaSeconds),
        };
    }

    /// <summary>Maps how far the stick is pushed to the band the actor is asking for.</summary>
    private SpeedBand BandFor(float magnitude)
    {
        if (magnitude <= MinimumAxisMagnitude)
        {
            return SpeedBand.Idle;
        }

        return magnitude <= tuning.WalkAxisThreshold ? SpeedBand.Walk : SpeedBand.Run;
    }

    /// <summary>
    /// Chooses this tick's band. Sprint is automatic: it engages after a run long
    /// enough and a meter charged enough, and it holds until the meter empties or
    /// the player asks for less.
    /// </summary>
    private SpeedBand NextBand(SpeedBand inputBand, bool grounded)
    {
        var sprinting = state.SpeedBand == SpeedBand.Sprint
            && inputBand == SpeedBand.Run
            && state.SprintReserve > 0f;
        if (sprinting)
        {
            return SpeedBand.Sprint;
        }

        var canEngage = inputBand == SpeedBand.Run
            && grounded
            && state.SprintReserve >= tuning.SprintActivationThreshold
            && runSeconds >= tuning.SprintBuildUpSeconds;

        return canEngage ? SpeedBand.Sprint : inputBand;
    }

    /// <summary>Refills the sprint meter outside the sprint band, spends it inside.</summary>
    private float NextSprintReserve(SpeedBand band, float deltaSeconds) =>
        band == SpeedBand.Sprint
            ? MathF.Max(0f, state.SprintReserve - (tuning.SprintDrainPerSecond * deltaSeconds))
            : MathF.Min(1f, state.SprintReserve + (tuning.SprintBuildPerSecond * deltaSeconds));

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
