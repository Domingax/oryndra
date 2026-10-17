using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// Random input sequences against the movement seam, checking the invariants the
/// simulation must never break.
/// </summary>
/// <remarks>
/// These are properties, not examples: any seed is expected to pass, so the seeds
/// listed here are a fixed sample of an inexhaustible input space. Adding a seed is
/// the cheap way to widen the net; the two input styles are the other, and the
/// deliberate one is the one that found a collision bug the chaotic one walked past
/// (see <see cref="InputStyle"/>).
/// </remarks>
public sealed class FuzzHarnessTests
{
    /// <summary>Every seed against every input style.</summary>
    public static TheoryData<int, InputStyle> SeedsAndStyles
    {
        get
        {
            var data = new TheoryData<int, InputStyle>();
            foreach (int seed in new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
            {
                foreach (InputStyle style in Enum.GetValues<InputStyle>())
                {
                    data.Add(seed, style);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SeedsAndStyles))]
    public void InputSequences_NeverBreakAnInvariant(int seed, InputStyle style)
    {
        FuzzRun run = SimulationFuzzer.Run(
            new FuzzSettings { Seed = seed, Style = style },
            FuzzRooms.WithAStep());

        Assert.True(
            run.Healthy,
            $"seed {seed} with {style} input broke {run.Violations.Count} invariant(s):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, run.Violations)}");
    }

    [Theory]
    [InlineData(1, InputStyle.Chaotic)]
    [InlineData(4, InputStyle.Chaotic)]
    [InlineData(7, InputStyle.Chaotic)]
    [InlineData(1, InputStyle.Deliberate)]
    [InlineData(4, InputStyle.Deliberate)]
    [InlineData(7, InputStyle.Deliberate)]
    public void AFuzzedActor_CanStillBreakFree(int seed, InputStyle style)
    {
        // The soft-lock invariant, checked from the worst state a fuzz run can
        // leave behind: whatever the actor was doing, it must still be able to run
        // away in at least one direction.
        BoxRoom room = FuzzRooms.WithAStep();
        FuzzRun run = SimulationFuzzer.Run(new FuzzSettings { Seed = seed, Style = style, Ticks = 3_000 }, room);
        var last = run.States[^1];

        var travelled = new List<float>();
        foreach (var yaw in new[] { 0f, MathF.PI / 2f, MathF.PI, 3f * MathF.PI / 2f })
        {
            var simulation = new MovementSimulation(room, last);
            var end = simulation.RunFor(Simulate.RunningTowards(yaw), ticks: 60);
            travelled.Add(new Vector2(end.Position.X - last.Position.X, end.Position.Z - last.Position.Z).Length());
        }

        Assert.True(
            travelled.Any(distance => distance > 1f),
            $"seed {seed} with {style} input: the actor could not run anywhere from {last.Position}: {string.Join(", ", travelled)}");
    }
}
