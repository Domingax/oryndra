using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// Locomotion: the speed bands the stick selects, the sprint reserve that turns a
/// run into a sprint, and the coyote window that forgives a late jump.
/// </summary>
/// <remarks>
/// The bands are a decision, not a consequence: these tests read it off the state
/// (<see cref="ActorState.Gait"/>) rather than inferring it from a velocity, so a
/// band that stops reaching the tuning fails here even if the motion looks right.
/// </remarks>
public sealed class LocomotionTests
{
    private static readonly float Tick = SimulationClock.FixedDeltaSeconds;

    /// <summary>How many ticks a full run takes to fill the sprint reserve.</summary>
    private static readonly int TicksToFill =
        (int)MathF.Ceiling(1f / (MovementTuning.Default.SprintBuildRate * Tick));

    /// <summary>How many ticks a full reserve lasts at the drain rate.</summary>
    private static readonly int TicksOfSprint =
        (int)MathF.Ceiling(1f / (MovementTuning.Default.SprintDrainRate * Tick));

    [Fact]
    public void Advance_WithTheStickAtRest_ReportsIdleAndCoastsToAStop()
    {
        MovementSimulation simulation = LongRoom();

        ActorState state = simulation.RunFor(InputFrame.Idle, ticks: 30);

        Assert.Equal(Gait.Idle, state.Gait);
        Assert.Equal(Vector3.Zero, state.Velocity);
    }

    [Fact]
    public void Advance_WithALightPush_WalksAtTheWalkBand()
    {
        ActorState state = LongRoom().RunFor(Sticks(0.25f), ticks: 30);

        Assert.Equal(Gait.Walk, state.Gait);
        Nearly.Equal(MovementTuning.Default.WalkSpeed, state.Velocity.Z, "a light push is a walk");
    }

    [Fact]
    public void Advance_WithAFullPush_RunsAtTheRunBand()
    {
        ActorState state = LongRoom().RunFor(Sticks(1f), ticks: 30);

        Assert.Equal(Gait.Run, state.Gait);
        Nearly.Equal(MovementTuning.Default.RunSpeed, state.Velocity.Z, "a full push is a run");
    }

    [Fact]
    public void Advance_WhileRunningOnTheGround_FillsTheSprintReserve()
    {
        ActorState state = LongRoom().RunFor(Sticks(1f), ticks: TicksToFill - 1);

        Assert.Equal(Gait.Run, state.Gait);
        Nearly.Equal(MovementTuning.Default.RunSpeed, state.Velocity.Z, "a half-built reserve is still a run");
        Nearly.Equal(
            1f - (MovementTuning.Default.SprintBuildRate * Tick),
            state.SprintReserve,
            "the reserve should be a tick short of full",
            tolerance: 1e-3f);
    }

    [Fact]
    public void Advance_WhenTheReserveIsFull_TurnsTheRunIntoASprint()
    {
        ActorState state = LongRoom().RunFor(Sticks(1f), ticks: TicksToFill + 20);

        Assert.Equal(Gait.Sprint, state.Gait);
        Nearly.Equal(MovementTuning.Default.SprintSpeed, state.Velocity.Z, "a full reserve is a sprint");
    }

    [Fact]
    public void Advance_WhenTheReserveIsSpent_DropsBackToTheRunBand()
    {
        ActorState state = LongRoom().RunFor(Sticks(1f), ticks: TicksToFill + TicksOfSprint + 20);

        Assert.Equal(Gait.Run, state.Gait);
        Nearly.Equal(MovementTuning.Default.RunSpeed, state.Velocity.Z, "a spent reserve runs, it does not walk");
    }

    [Fact]
    public void Advance_WithoutTheGroundUnderfoot_DoesNotFillTheSprintReserve()
    {
        var airborne = new MovementSimulation(
            BoxRoom.Room(),
            new ActorState { Position = new Vector3(0f, 5f, 0f) });

        ActorState state = airborne.RunFor(Sticks(1f), ticks: 10);

        Assert.False(state.Grounded, "the actor should still be falling");
        Assert.Equal(0f, state.SprintReserve);
    }

    [Fact]
    public void Advance_JustAfterLeavingTheGround_StillJumps()
    {
        var simulation = new MovementSimulation(Ledge(), ActorState.StandingAt(new Vector3(-2f, 0f, 0f)));
        RunOffTheLedge(simulation);

        ActorState state = simulation.Advance(new InputFrame { Jump = true }, Tick);

        Assert.True(state.Velocity.Y > 0f, $"the jump should still count, was {state.Velocity.Y}");
    }

    [Fact]
    public void Advance_PastTheCoyoteWindow_DropsTheJump()
    {
        var simulation = new MovementSimulation(Ledge(), ActorState.StandingAt(new Vector3(-2f, 0f, 0f)));
        RunOffTheLedge(simulation);

        int window = (int)MathF.Ceiling(MovementTuning.Default.CoyoteTime / Tick) + 1;
        ActorState falling = simulation.RunFor(InputFrame.Idle, ticks: window);

        Assert.Equal(0f, falling.CoyoteSecondsLeft);

        ActorState late = simulation.Advance(new InputFrame { Jump = true }, Tick);

        Assert.True(late.Velocity.Y < 0f, $"a jump asked for too late should not fire, was {late.Velocity.Y}");
    }

    [Fact]
    public void Advance_WithTheJumpPressedAgainInTheSameWindow_DoesNotJumpTwice()
    {
        // The window is spent by the jump that uses it: without that, a second
        // press one tick later would fire again and float the actor upward.
        var simulation = new MovementSimulation(Ledge(), ActorState.StandingAt(new Vector3(-2f, 0f, 0f)));
        RunOffTheLedge(simulation);

        ActorState first = simulation.Advance(new InputFrame { Jump = true }, Tick);
        ActorState second = simulation.Advance(new InputFrame { Jump = true }, Tick);

        Assert.True(first.Velocity.Y > 0f, $"the first press should jump, was {first.Velocity.Y}");
        Nearly.Equal(
            first.Velocity.Y - (MovementTuning.Default.Gravity * Tick),
            second.Velocity.Y,
            "the second press should leave gravity as the only thing acting");
    }

    [Fact]
    public void Advance_ThroughAWholeReserve_KeepsTheReserveInItsBounds()
    {
        MovementSimulation simulation = LongRoom();
        InputFrame input = Sticks(1f);

        for (int tick = 0; tick < TicksToFill + TicksOfSprint + 60; tick++)
        {
            ActorState state = simulation.Advance(input, Tick);

            Assert.InRange(state.SprintReserve, 0f, 1f);
        }
    }

    /// <summary>Builds a simulation in a room wide enough to run for several seconds.</summary>
    private static MovementSimulation LongRoom() =>
        new(BoxRoom.Room(halfWidth: 60f), ActorState.StandingAt(Vector3.Zero));

    /// <summary>Builds a frame pushing the stick forward by <paramref name="travel"/>.</summary>
    private static InputFrame Sticks(float travel) => new() { MoveAxis = new Vector2(0f, travel) };

    /// <summary>A floor that stops at x = 0, so the actor can run off its edge.</summary>
    private static BoxRoom Ledge() =>
        new BoxRoom().Add(Box.FromCorners(new Vector3(-20f, -1f, -20f), new Vector3(0f, 0f, 20f)));

    /// <summary>Runs the actor towards +X until it is off the ground; fails if it never is.</summary>
    private static ActorState RunOffTheLedge(MovementSimulation simulation)
    {
        InputFrame input = Simulate.RunningTowards(MathF.PI / 2f);

        for (int tick = 0; tick < 600; tick++)
        {
            ActorState state = simulation.Advance(input, Tick);
            if (!state.Grounded)
            {
                return state;
            }
        }

        throw new InvalidOperationException("the actor never left the ledge");
    }
}
