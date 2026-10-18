using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// The walking skeleton's locomotion: discrete speed bands, the sprint meter and
/// the input tolerance that keeps a jump from being dropped at a ledge.
/// </summary>
/// <remarks>
/// Every assertion goes through <see cref="MovementSimulation.Advance"/>, the
/// same seam the Godot layer drives. These are the tests that make the movement
/// reviewable without an engine (docs/testing.md).
/// </remarks>
public sealed class LocomotionTests
{
    private static readonly float Tick = SimulationClock.FixedDeltaSeconds;
    private static readonly MovementTuning Tuning = MovementTuning.Default;

    private static readonly InputFrame Run = new() { MoveAxis = new Vector2(0f, 1f) };
    private static readonly InputFrame Walk = new() { MoveAxis = new Vector2(0f, 0.4f) };

    [Fact]
    public void Advance_WithAPartialStick_SettlesInTheWalkBand()
    {
        var state = Simulation().RunFor(Walk, ticks: 120);

        Assert.Equal(SpeedBand.Walk, state.SpeedBand);
        Nearly.Equal(Tuning.WalkSpeed, state.Velocity.Length(), "a half-tilted stick should walk");
    }

    [Fact]
    public void Advance_WithAFullStick_SettlesInTheRunBand()
    {
        // Less than the sprint build-up: this is the run band, not sprint.
        var state = Simulation().RunFor(Run, ticks: 30);

        Assert.Equal(SpeedBand.Run, state.SpeedBand);
        Nearly.Equal(Tuning.RunSpeed, state.Velocity.Length(), "a full stick should run");
    }

    [Fact]
    public void Advance_WhileWalking_BuildsTheSprintMeter()
    {
        var start = ActorState.StandingAt(Vector3.Zero) with { SprintReserve = 0.5f };

        var state = Simulation(start: start).RunFor(Walk, ticks: 60);

        Assert.Equal(SpeedBand.Walk, state.SpeedBand);
        Assert.True(state.SprintReserve > start.SprintReserve, "walking should refill the meter");
    }

    [Fact]
    public void Advance_AfterRunningForTheBuildUp_RaisesTheActorIntoTheSprintBand()
    {
        var state = Simulation().RunFor(Run, ticks: 150);

        Assert.Equal(SpeedBand.Sprint, state.SpeedBand);
        Nearly.Equal(Tuning.SprintSpeed, state.Velocity.Length(), "the sprint band should be the fastest band");
        Assert.True(state.SprintReserve < 1f, "sprinting should spend the meter");
    }

    [Fact]
    public void Advance_WhileSprinting_DrainsTheSprintMeter()
    {
        var simulation = Simulation();
        simulation.RunFor(Run, ticks: 90);

        var before = simulation.State.SprintReserve;
        var state = simulation.RunFor(Run, ticks: 60);

        Assert.Equal(SpeedBand.Sprint, state.SpeedBand);
        Assert.True(state.SprintReserve < before, "the meter should drain while sprinting");
    }

    [Fact]
    public void Advance_WithAnEmptyMeter_StaysInTheRunBandUntilItRefills()
    {
        var start = ActorState.StandingAt(Vector3.Zero) with { SprintReserve = 0f };
        var simulation = Simulation(start: start);

        var stillRunning = simulation.RunFor(Run, ticks: 90);
        Assert.Equal(SpeedBand.Run, stillRunning.SpeedBand);
        Assert.True(
            stillRunning.SprintReserve < Tuning.SprintActivationThreshold,
            "the meter should still be below the activation threshold");

        var recovered = simulation.RunFor(Run, ticks: 30);
        Assert.Equal(SpeedBand.Sprint, recovered.SpeedBand);
        Nearly.Equal(Tuning.SprintSpeed, recovered.Velocity.Length());
    }

    [Fact]
    public void Advance_JustAfterRunningOffALedge_StillHonoursTheJump()
    {
        var simulation = LedgeRun();
        RunOffTheLedge(simulation);

        var state = simulation.Advance(Run with { Jump = true }, Tick);

        Assert.True(state.Velocity.Y > 0f, $"coyote time should still allow a jump, was {state.Velocity.Y}");
    }

    [Fact]
    public void Advance_LongAfterRunningOffALedge_IgnoresTheJump()
    {
        var simulation = LedgeRun();
        RunOffTheLedge(simulation);
        var falling = simulation.RunFor(Run, ticks: 10);

        var state = simulation.Advance(Run with { Jump = true }, Tick);

        Assert.True(falling.Velocity.Y < 0f, "the actor should be falling by now");
        Assert.True(state.Velocity.Y <= 0f, $"the coyote window has expired, but the jump added {state.Velocity.Y}");
    }

    [Fact]
    public void StandingAt_StartsWithAFullSprintMeter()
    {
        Assert.Equal(1f, ActorState.StandingAt(Vector3.Zero).SprintReserve);
        Assert.Equal(SpeedBand.Idle, ActorState.StandingAt(Vector3.Zero).SpeedBand);
    }

    /// <summary>A simulation in an open room, wide enough to hold the band's top speed.</summary>
    private static MovementSimulation Simulation(float halfWidth = 40f, ActorState? start = null) =>
        new(BoxRoom.Room(halfWidth: halfWidth), start);

    /// <summary>A floor with a 1 m ledge to run off, so the coyote window can be observed.</summary>
    private static MovementSimulation LedgeRun()
    {
        var room = new BoxRoom()
            .Add(Box.FromCorners(new Vector3(-30f, -1f, -20f), new Vector3(30f, 0f, 40f)))
            .Add(Box.FromCorners(new Vector3(-3f, 0f, 16f), new Vector3(3f, 1f, 24f)));

        return new MovementSimulation(room, ActorState.StandingAt(new Vector3(0f, 1f, 22f)));
    }

    private static void RunOffTheLedge(MovementSimulation simulation)
    {
        ActorState state;
        do
        {
            state = simulation.Advance(Run, Tick);
        }
        while (state.Grounded);
    }
}
