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
/// listed here are a fixed sample of an inexhaustible input space. Adding a seed
/// is the cheap way to widen the net.
/// </remarks>
public sealed class FuzzHarnessTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void RandomInputSequences_NeverBreakAnInvariant(int seed)
    {
        var run = SimulationFuzzer.Run(new FuzzSettings { Seed = seed }, BoxWorld.Room());

        Assert.True(
            run.Healthy,
            $"seed {seed} broke {run.Violations.Count} invariant(s):{Environment.NewLine}{string.Join(Environment.NewLine, run.Violations)}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void AFuzzedActor_CanStillBreakFree(int seed)
    {
        // The soft-lock invariant, checked from the worst state a fuzz run can
        // leave behind: whatever the actor was doing, it must still be able to run
        // away in at least one direction.
        var room = BoxWorld.Room();
        var run = SimulationFuzzer.Run(new FuzzSettings { Seed = seed, Ticks = 3_000 }, room);
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
            $"seed {seed}: the actor could not run anywhere from {last.Position}: {string.Join(", ", travelled)}");
    }
}
