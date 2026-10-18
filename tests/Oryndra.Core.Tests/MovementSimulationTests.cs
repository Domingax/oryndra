using System.Numerics;

using Oryndra.Core.Movement;

using Oryndra.Core.Tests.Support;

namespace Oryndra.Core.Tests;

/// <summary>
/// The movement seam's behaviour: what one tick does with a given input, a given
/// state and a given collision query.
/// </summary>
public sealed class MovementSimulationTests
{
    private static readonly float Tick = SimulationClock.FixedDeltaSeconds;

    [Fact]
    public void Advance_WithAnIdleFrame_LeavesTheActorStandingExactlyStill()
    {
        var simulation = new MovementSimulation(BoxRoom.Room(), ActorState.StandingAt(Vector3.Zero));

        var state = simulation.Advance(InputFrame.Idle, Tick);

        Assert.True(state.Grounded, "the actor should be resting on the floor");
        Assert.Equal(Vector3.Zero, state.Position);
        Assert.Equal(Vector3.Zero, state.Velocity);
    }

    [Fact]
    public void Advance_WithForwardInput_RunsTowardsPositiveZ()
    {
        var simulation = new MovementSimulation(BoxRoom.Room(), ActorState.StandingAt(Vector3.Zero));

        var state = simulation.RunFor(Simulate.Moving(right: 0f, forward: 1f), ticks: 60);

        Assert.True(state.Position.Z > 5f, $"the actor should be a few metres forwards, was {state.Position}");
        Nearly.Equal(0f, state.Position.X, "no sideways input means no sideways drift");
    }

    [Fact]
    public void Advance_WhenTheActorIsTurned_RunsAlongItsFacing()
    {
        var simulation = new MovementSimulation(BoxRoom.Room(), ActorState.StandingAt(Vector3.Zero));

        var state = simulation.RunFor(Simulate.RunningTowards(MathF.PI / 2f), ticks: 60);

        Assert.True(state.Position.X > 5f, $"a quarter turn should send the actor along +X, was {state.Position}");
        Nearly.Equal(0f, state.Position.Z, "the actor should not drift sideways while running");
        Nearly.Equal(MathF.PI / 2f, state.Yaw);
    }

    [Fact]
    public void Advance_WithSustainedInput_ReachesTheTunedSpeed()
    {
        var simulation = new MovementSimulation(BoxRoom.Room(halfWidth: 20f), ActorState.StandingAt(Vector3.Zero));

        // Shorter than the sprint build-up: this test is about the run band.
        var state = simulation.RunFor(Simulate.Moving(right: 0f, forward: 1f), ticks: 30);

        Nearly.Equal(MovementTuning.Default.RunSpeed, state.Velocity.Length(), "the actor should be at full speed");
        Nearly.Equal(MovementTuning.Default.RunSpeed, state.Velocity.Z, "full speed should be spent going forwards");
    }

    [Fact]
    public void Advance_WhenJumpIsHeld_LeavesTheGround()
    {
        var simulation = new MovementSimulation(BoxRoom.Room(), ActorState.StandingAt(Vector3.Zero));

        var state = simulation.Advance(new InputFrame { Jump = true }, Tick);

        Assert.False(state.Grounded, "a jump should leave the floor");
        Assert.True(state.Position.Y > 0f, $"a jump should raise the actor, was {state.Position.Y}");
        Assert.True(state.Velocity.Y > 0f, $"a jump should be an upward velocity, was {state.Velocity.Y}");
    }

    [Fact]
    public void Advance_WhenTheActorRunsIntoAWall_StopsOnTheWallFace()
    {
        var tuning = MovementTuning.Default;
        var room = BoxRoom.Room(halfWidth: 2f);
        var simulation = new MovementSimulation(room, ActorState.StandingAt(new Vector3(1f, 0f, 0f)), tuning);

        var state = simulation.RunFor(Simulate.Moving(right: 1f, forward: 0f), ticks: 120);

        Nearly.Equal(2f - tuning.Body.Radius, state.Position.X, "the body should stop with its face on the wall");
        Assert.False(room.Overlaps(state.Position, tuning.Body), "the actor must not end up inside the wall");
    }

    [Fact]
    public void Advance_ReturnsTheStateItStores()
    {
        var simulation = new MovementSimulation(BoxRoom.Room());

        var state = simulation.Advance(InputFrame.Idle, Tick);

        Assert.Equal(state, simulation.State);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void Advance_WithANonPositiveTick_Throws(float deltaSeconds)
    {
        var simulation = new MovementSimulation(BoxRoom.Room());

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Advance(InputFrame.Idle, deltaSeconds));
    }

    [Fact]
    public void Constructor_WithANullCollisionQuery_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MovementSimulation(null!));
    }

    [Fact]
    public void Reset_PutsTheActorBackAtTheGivenState()
    {
        var simulation = new MovementSimulation(BoxRoom.Room());
        simulation.RunFor(Simulate.Moving(right: 0f, forward: 1f), ticks: 30);

        simulation.Reset(ActorState.StandingAt(Vector3.Zero));

        Assert.Equal(ActorState.StandingAt(Vector3.Zero), simulation.State);
    }
}
