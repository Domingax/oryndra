using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>
/// Drives the movement simulation with random input sequences and reports every
/// invariant it breaks.
/// </summary>
/// <remarks>
/// <para>
/// Unit tests say what the simulation does for the inputs we thought of; the fuzz
/// harness says what it must never do for the inputs we did not. It drives the
/// simulation exactly as a player would — one <see cref="InputFrame"/> per tick,
/// through the public seam — and checks four things after every tick:
/// </para>
/// <list type="number">
/// <item>the state is finite (no NaN, no infinity);</item>
/// <item>the body is never left inside solid geometry;</item>
/// <item>the body never escapes the world's outer bounds;</item>
/// <item>the actor is never stuck against nothing.</item>
/// </list>
/// <para>
/// Violations are collected rather than thrown so a failing run reads as a report
/// instead of a single line.
/// </para>
/// </remarks>
public static class SimulationFuzzer
{
    /// <summary>Runs <paramref name="settings"/>' seed through <paramref name="world"/> once.</summary>
    /// <param name="settings">How long to run, from where, and within which limits.</param>
    /// <param name="world">The world to move the actor through.</param>
    /// <param name="tuning">The tuning to simulate with; defaults to <see cref="MovementTuning.Default"/>.</param>
    /// <returns>The recorded run, including every invariant it broke.</returns>
    public static FuzzRun Run(FuzzSettings settings, BoxWorld world, MovementTuning? tuning = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(world);

        var tuningInUse = tuning ?? MovementTuning.Default;
        var simulation = new MovementSimulation(world, settings.Start, tuningInUse);
        var random = new Random(settings.Seed);
        var softLock = new SoftLockDetector(world, tuningInUse);

        var inputs = new List<InputFrame>(settings.Ticks);
        var states = new List<ActorState>(settings.Ticks);
        var violations = new List<string>();

        for (var tick = 0; tick < settings.Ticks && violations.Count < settings.MaxViolations; tick++)
        {
            var input = NextInput(random);
            var previous = simulation.State;
            var current = simulation.Advance(input, settings.DeltaSeconds);

            inputs.Add(input);
            states.Add(current);
            Check(settings, tuningInUse, world, softLock, tick, previous, input, current, violations);
        }

        return new FuzzRun
        {
            Inputs = inputs,
            States = states,
            Violations = violations,
        };
    }

    private static InputFrame NextInput(Random random)
    {
        var angle = random.NextSingle() * MathF.Tau;
        var axis = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var magnitude = random.Next(3) switch
        {
            0 => 0f, // the player lets go
            1 => 0.5f,
            _ => 1f,
        };

        return new InputFrame
        {
            MoveAxis = axis * magnitude,
            Yaw = random.NextSingle() * MathF.Tau,
            Jump = random.Next(20) == 0,
        };
    }

    private static void Check(
        FuzzSettings settings,
        MovementTuning tuning,
        BoxWorld world,
        SoftLockDetector softLock,
        int tick,
        ActorState previous,
        InputFrame input,
        ActorState current,
        List<string> violations)
    {
        if (!current.IsFinite)
        {
            violations.Add($"tick {tick}: the state is not finite ({current})");
            return;
        }

        var body = Box.BodyVolume(current.Position, tuning.Body);
        if (world.Overlaps(current.Position, tuning.Body))
        {
            violations.Add($"tick {tick}: the body is inside solid geometry at {current.Position}");
        }
        else if (!world.OuterBounds.Contains(body, tolerance: 1e-3f))
        {
            violations.Add($"tick {tick}: the body escaped the world at {current.Position}");
        }

        var speed = current.Velocity.Length();
        if (speed > settings.MaxSpeed)
        {
            violations.Add($"tick {tick}: speed {speed:0.###} m/s exceeds the {settings.MaxSpeed} m/s bound");
        }

        if (softLock.Observe(previous, input, current))
        {
            violations.Add(
                $"tick {tick}: the actor stayed stuck in open space for {SoftLockDetector.WindowTicks} ticks at {current.Position}");
        }
    }

    /// <summary>
    /// Detects a soft-lock: the player keeps asking to run in open space and the
    /// actor still gets nowhere.
    /// </summary>
    /// <remarks>
    /// A window is needed because a single tick proves nothing: a full reversal
    /// decelerates through zero, so the actor legitimately crawls for one tick.
    /// What a stuck actor cannot fake is the distance it covers while it is being
    /// pushed, so the detector adds up the path travelled over a window and only
    /// complains when that path is empty.
    /// </remarks>
    private sealed class SoftLockDetector(BoxWorld world, MovementTuning tuning)
    {
        /// <summary>How many ticks of pushed-in-open-space count as a stall.</summary>
        public const int WindowTicks = 30;

        private const float MinimumProgress = 0.05f;
        private const float ProbeDistance = 0.05f;

        private float path;
        private int ticks;

        /// <summary>Records one tick and reports whether the run is now stuck.</summary>
        /// <param name="previous">The state before the tick.</param>
        /// <param name="input">What the player asked for during the tick.</param>
        /// <param name="current">The state after the tick.</param>
        /// <returns><see langword="true"/> when the actor has been stuck for a whole window.</returns>
        public bool Observe(ActorState previous, InputFrame input, ActorState current)
        {
            if (!IsPushingIntoOpenSpace(input, current))
            {
                Restart();
                return false;
            }

            var travelled = current.Position - previous.Position;
            path += MathF.Abs(travelled.X) + MathF.Abs(travelled.Z);
            ticks++;

            if (ticks < WindowTicks)
            {
                return false;
            }

            var stuck = path < MinimumProgress;
            Restart();
            return stuck;
        }

        private bool IsPushingIntoOpenSpace(InputFrame input, ActorState current)
        {
            var wish = input.WishDirection;
            if (!current.Grounded || wish == Vector3.Zero)
            {
                return false;
            }

            return !world.Overlaps(current.Position + (wish * ProbeDistance), tuning.Body);
        }

        private void Restart()
        {
            path = 0f;
            ticks = 0;
        }
    }
}
