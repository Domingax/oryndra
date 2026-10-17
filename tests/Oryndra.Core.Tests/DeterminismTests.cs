using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// Determinism: identical inputs must produce an identical state, tick for tick.
/// </summary>
/// <remarks>
/// Ghosts replay recorded input frames, so a run that drifts by one bit is a run
/// that no longer matches the recording. The comparison is on the raw bits, which
/// is stricter than equality and catches a NaN creeping in.
/// </remarks>
public sealed class DeterminismTests
{
    private const int Ticks = 3_000;

    [Fact]
    public void TwoRunsOfTheSameInputs_ProduceTheSameStates()
    {
        var settings = new FuzzSettings { Seed = 20261017, Ticks = Ticks };

        var first = SimulationFuzzer.Run(settings, BoxRoom.Room());
        var second = SimulationFuzzer.Run(settings, BoxRoom.Room());

        Assert.Equal(first.States.Count, second.States.Count);
        for (var tick = 0; tick < first.States.Count; tick++)
        {
            ActorStateAssertions.AssertBitIdentical(first.States[tick], second.States[tick], $"tick {tick}");
        }
    }

    [Fact]
    public void RestartingASimulation_ReplaysTheSameStates()
    {
        var settings = new FuzzSettings { Seed = 42, Ticks = 500 };
        var room = BoxRoom.Room();
        var inputs = SimulationFuzzer.Run(settings, room).Inputs;
        var simulation = new MovementSimulation(room, settings.Start);

        var first = inputs.Select(input => simulation.Advance(input, settings.DeltaSeconds)).ToList();

        simulation.Reset(settings.Start);
        var second = inputs.Select(input => simulation.Advance(input, settings.DeltaSeconds)).ToList();

        for (var tick = 0; tick < first.Count; tick++)
        {
            ActorStateAssertions.AssertBitIdentical(first[tick], second[tick], $"replay tick {tick}");
        }
    }

    [Fact]
    public void InterleavingTwoSimulations_ChangesNeither()
    {
        // A simulation that read shared mutable state (a static cache, a shared
        // random source, a clock) would drift as soon as a second one runs beside
        // it. This is the cheap way to prove it does not.
        var settings = new FuzzSettings { Seed = 7, Ticks = 400 };
        var alone = SimulationFuzzer.Run(settings, BoxRoom.Room());

        var left = new MovementSimulation(BoxRoom.Room(), settings.Start);
        var right = new MovementSimulation(BoxRoom.Room(), settings.Start);
        for (var tick = 0; tick < alone.Inputs.Count; tick++)
        {
            var a = left.Advance(alone.Inputs[tick], settings.DeltaSeconds);
            var b = right.Advance(alone.Inputs[tick], settings.DeltaSeconds);

            ActorStateAssertions.AssertBitIdentical(alone.States[tick], a, $"left tick {tick}");
            ActorStateAssertions.AssertBitIdentical(alone.States[tick], b, $"right tick {tick}");
        }
    }
}
