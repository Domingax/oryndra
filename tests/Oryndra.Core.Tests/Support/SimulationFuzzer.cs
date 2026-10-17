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
/// through the public seam — and checks six things after every tick:
/// </para>
/// <list type="number">
/// <item>the state is finite (no NaN, no infinity);</item>
/// <item>the body is never left inside solid geometry;</item>
/// <item>the body never leaves the room's outer bounds;</item>
/// <item>the speed stays within a coarse bound, so nothing feeds itself speed;</item>
/// <item>the sprint reserve stays between empty and full;</item>
/// <item>the actor is never stuck in open space for a whole window of ticks.</item>
/// </list>
/// <para>
/// The first four are only as independent as the collision query they ask: the
/// simulation takes the position that query hands back, so what they establish is
/// that the simulation never places the body where the geometry says it cannot be
/// — it does not invent a position, skip a wall or lose speed through one. A bug
/// inside <see cref="BoxRoom"/> itself is outside what this harness can see.
/// </para>
/// <para>
/// Violations are collected rather than thrown so a failing run reads as a report
/// instead of a single line.
/// </para>
/// </remarks>
public static class SimulationFuzzer
{
    /// <summary>Runs <paramref name="settings"/>' seed against <paramref name="collision"/> once.</summary>
    /// <param name="settings">How long to run, from where, and within which limits.</param>
    /// <param name="collision">The geometry to move the actor through.</param>
    /// <param name="tuning">The tuning to simulate with; defaults to <see cref="MovementTuning.Default"/>.</param>
    /// <returns>The recorded run, including every invariant it broke.</returns>
    public static FuzzRun Run(FuzzSettings settings, BoxRoom collision, MovementTuning? tuning = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(collision);

        return new FuzzSession(settings, collision, tuning ?? MovementTuning.Default).Run();
    }

    private static InputFrame NextInput(Random random)
    {
        var angle = random.NextSingle() * MathF.Tau;
        var axis = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var magnitude = random.Next(4) switch
        {
            0 => 0f, // the player lets go of the stick
            1 => 0.25f, // a light push: the walk band
            2 => 0.5f, // the bottom of the run band
            _ => 1f,
        };

        return new InputFrame
        {
            MoveAxis = axis * magnitude,
            Yaw = random.NextSingle() * MathF.Tau,
            Jump = random.Next(20) == 0,
        };
    }

    /// <summary>One tick of a run: what went in, and what came out.</summary>
    private readonly record struct Tick(int Index, ActorState Before, InputFrame Input, ActorState After);

    /// <summary>A single fuzz run, and everything it is checked against.</summary>
    private sealed class FuzzSession(FuzzSettings settings, BoxRoom collision, MovementTuning tuning)
    {
        private readonly MovementSimulation simulation = new(collision, settings.Start, tuning);
        private readonly SoftLockDetector softLock = new(collision, tuning);
        private readonly List<string> violations = [];
        private readonly List<InputFrame> inputs = [];
        private readonly List<ActorState> states = [];

        public FuzzRun Run()
        {
            var random = new Random(settings.Seed);

            for (var index = 0; index < settings.Ticks && violations.Count < settings.MaxViolations; index++)
            {
                var input = NextInput(random);
                var before = simulation.State;
                var after = simulation.Advance(input, settings.DeltaSeconds);

                inputs.Add(input);
                states.Add(after);
                Check(new Tick(index, before, input, after));
            }

            return new FuzzRun
            {
                Inputs = inputs,
                States = states,
                Violations = violations,
            };
        }

        private void Check(Tick tick)
        {
            if (!tick.After.IsFinite)
            {
                violations.Add($"tick {tick.Index}: the state is not finite ({tick.After})");
                return;
            }

            var body = Box.BodyVolume(tick.After.Position, tuning.Body);
            if (collision.Overlaps(tick.After.Position, tuning.Body))
            {
                violations.Add($"tick {tick.Index}: the body is inside solid geometry at {tick.After.Position}");
            }
            else if (!collision.OuterBounds.Contains(body, tolerance: 1e-3f))
            {
                violations.Add($"tick {tick.Index}: the body left the room at {tick.After.Position}");
            }

            var speed = tick.After.Velocity.Length();
            if (speed > settings.MaxSpeed)
            {
                violations.Add(
                    $"tick {tick.Index}: speed {speed:0.###} m/s exceeds the {settings.MaxSpeed} m/s bound");
            }

            if (tick.After.SprintReserve is < 0f or > 1f)
            {
                violations.Add(
                    $"tick {tick.Index}: the sprint reserve left its bounds at {tick.After.SprintReserve}");
            }

            if (softLock.Observe(tick.Before, tick.Input, tick.After))
            {
                violations.Add(
                    $"tick {tick.Index}: the actor stayed stuck in open space for {SoftLockDetector.WindowTicks} ticks at {tick.After.Position}");
            }
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
    private sealed class SoftLockDetector(BoxRoom collision, MovementTuning tuning)
    {
        /// <summary>How many ticks of pushed-in-open-space count as a stall.</summary>
        public const int WindowTicks = 30;

        private const float MinimumProgress = 0.05f;
        private const float ProbeDistance = 0.05f;

        private float path;
        private int ticks;

        /// <summary>Records one tick and reports whether the run is now stuck.</summary>
        /// <param name="before">The state before the tick.</param>
        /// <param name="input">What the player asked for during the tick.</param>
        /// <param name="after">The state after the tick.</param>
        /// <returns><see langword="true"/> when the actor has been stuck for a whole window.</returns>
        public bool Observe(ActorState before, InputFrame input, ActorState after)
        {
            if (!IsPushingIntoOpenSpace(input, after))
            {
                Restart();
                return false;
            }

            var travelled = after.Position - before.Position;
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

        private bool IsPushingIntoOpenSpace(InputFrame input, ActorState actor)
        {
            var wish = input.WishDirection;
            if (!actor.Grounded || wish == Vector3.Zero)
            {
                return false;
            }

            return !collision.Overlaps(actor.Position + (wish * ProbeDistance), tuning.Body);
        }

        private void Restart()
        {
            path = 0f;
            ticks = 0;
        }
    }
}
