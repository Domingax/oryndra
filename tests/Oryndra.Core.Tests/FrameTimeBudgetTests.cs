using System.Diagnostics;
using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests;

/// <summary>
/// Guards the simulation's slice of the frame budget.
/// </summary>
/// <remarks>
/// The game targets 60 FPS, so a whole frame is 16.67 ms. A co-op session runs up
/// to three actors, each simulated on every tick: if one tick ever costs more than
/// 0.2 ms the simulation alone eats more than 3.6 % of the frame, before physics,
/// animation and rendering get their share.
/// <para>
/// The budget is generous compared with the real cost (tens of microseconds), so a
/// loaded CI machine will not trip it. What it does catch is the kind of
/// regression that never shows up as a failing unit test: an allocation, a LINQ
/// query or a geometry search added to the per-tick path.
/// </para>
/// </remarks>
public sealed class FrameTimeBudgetTests
{
    private const double BudgetMillisecondsPerTick = 0.2;
    private const int MeasuredTicks = 200_000;
    private const int WarmupTicks = 10_000;

    [Fact]
    public void Advance_StaysWithinTheFrameBudget()
    {
        var simulation = new MovementSimulation(BoxRoom.Room());
        var input = new InputFrame
        {
            MoveAxis = new Vector2(1f, 1f),
            Yaw = 0.3f,
            Jump = true,
        };

        for (var tick = 0; tick < WarmupTicks; tick++)
        {
            simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
        }

        var start = Stopwatch.GetTimestamp();
        for (var tick = 0; tick < MeasuredTicks; tick++)
        {
            simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
        }

        var elapsed = Stopwatch.GetElapsedTime(start);
        var perTick = elapsed.TotalMilliseconds / MeasuredTicks;

        Assert.True(
            perTick < BudgetMillisecondsPerTick,
            $"one tick averaged {perTick:0.#####} ms over {MeasuredTicks} ticks, above the "
            + $"{BudgetMillisecondsPerTick} ms budget; three co-op actors would spend "
            + $"{perTick * 3 / 16.6667:P1} of a 60 FPS frame");
    }

    [Fact]
    public void Advance_DoesNotDependOnFrameCountForItsCost()
    {
        // A cost that grows with the run length (a growing list, a leak, a
        // replayed history) would not show in a single average. Compare an early
        // window with a late one and demand they are in the same league.
        var simulation = new MovementSimulation(BoxRoom.Room());
        var input = new InputFrame { MoveAxis = new Vector2(0f, 1f), Yaw = 0.1f };
        const int Window = 20_000;

        var early = Measure(simulation, input, Window);
        for (var tick = 0; tick < Window * 10; tick++)
        {
            simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
        }

        var late = Measure(simulation, input, Window);

        Assert.True(
            late < Math.Max(early, 1e-4) * 20,
            $"a tick got {late / Math.Max(early, 1e-6):0.#}x more expensive after 220k ticks (early {early:0.#####} ms, late {late:0.#####} ms)");
    }

    private static double Measure(MovementSimulation simulation, InputFrame input, int ticks)
    {
        var start = Stopwatch.GetTimestamp();
        for (var tick = 0; tick < ticks; tick++)
        {
            simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
        }

        return Stopwatch.GetElapsedTime(start).TotalMilliseconds / ticks;
    }
}
