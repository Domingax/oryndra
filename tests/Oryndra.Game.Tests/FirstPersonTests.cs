using GdUnit4;

using Godot;

using Oryndra.Core.Movement;

using static GdUnit4.Assertions;

namespace Oryndra.Game.Tests;

/// <summary>
/// The walking skeleton's wiring, inside a real Godot: the greybox the simulation
/// collides with, the eye that draws it, and the input frame that drives it.
/// </summary>
/// <remarks>
/// What only the engine can break lives here. The movement rules themselves are
/// proven against the core in xUnit (docs/testing.md); what these cases add is the
/// engine's half of the seam — that the greybox actually grounds the actor, that
/// the node a script names is the node the scene has, and that the two conventions
/// (the core's +Z facing, Godot's -Z camera) are reconciled the way the player feels
/// rather than the way the source reads.
/// </remarks>
[RequireGodotRuntime]
[TestSuite]
public sealed class FirstPersonTests
{
    [TestCase]
    public async Task Main_WhenTheRuntimeBoots_BuildsTheGreyboxAndTheEye()
    {
        var main = new Main();
        using var runner = ISceneRunner.Load(main, autoFree: true);

        await ISceneRunner.SyncPhysicsFrame;

        var greybox = main.GetNodeOrNull<Node3D>("Greybox");
        AssertThat(greybox).IsNotNull();
        AssertThat(greybox!.GetChildCount()).IsGreater(0);

        var view = main.GetNodeOrNull<Camera3D>("View");
        AssertThat(view).IsNotNull();
        AssertThat(view!.Current).IsTrue();
    }

    [TestCase]
    public async Task Main_WhenTheActorStands_TheEyeSitsOnItsHead()
    {
        var main = new Main();
        using var runner = ISceneRunner.Load(main, autoFree: true);
        var view = main.GetNode<FirstPersonCamera>("View");

        // The first frame's delta can fall a hair under the fixed step, so one
        // awaited frame is not one tick: the smoke test waits two for the same
        // reason.
        await ISceneRunner.SyncPhysicsFrame;
        await ISceneRunner.SyncPhysicsFrame;

        var eye = view.GlobalPosition;
        var feet = main.State.Position;

        AssertThat(main.State.Grounded).IsTrue();
        AssertThat(eye.Y).IsEqualApprox(feet.Y + view.EyeHeight, 0.01f);
        AssertThat(new Vector2(eye.X, eye.Z)).IsEqualApprox(new Vector2(feet.X, feet.Z), new Vector2(0.01f, 0.01f));
    }

    [TestCase]
    public async Task Main_WhenThePlayerPushesRight_MovesTowardsTheRightOfTheView()
    {
        var main = new Main();
        using var runner = ISceneRunner.Load(main, autoFree: true);
        var start = main.State.Position;
        main.ReadInput = yaw => MovementInput.Frame(right: 1f, forward: 0f, yaw: yaw, jump: false);

        for (var frame = 0; frame < 40; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }

        var view = main.GetNode<Camera3D>("View");
        var travelled = new Vector3(
            main.State.Position.X - start.X,
            0f,
            main.State.Position.Z - start.Z);

        AssertThat(travelled.Dot(view.GlobalTransform.Basis.X)).IsGreater(1f);
    }

    [TestCase]
    public async Task Main_WhenJumpIsPressed_LeavesTheGround()
    {
        var main = new Main();
        using var runner = ISceneRunner.Load(main, autoFree: true);
        var jump = true;
        main.ReadInput = yaw => MovementInput.Frame(right: 0f, forward: 0f, yaw: yaw, jump: jump);

        for (var frame = 0; frame < 5 && main.State.Position.Y <= 0f; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }

        jump = false;

        AssertThat(main.State.Position.Y).IsGreater(0f);
    }

    [TestCase]
    public void View_WhenDrawnBetweenTwoTicks_StandsBetweenThem()
    {
        // The simulation advances on a fixed step and the display draws whenever it
        // likes; this is the seam between the two. Half a step past the last tick is
        // halfway along it, which is what keeps movement smooth on a display that
        // does not refresh at the tick rate.
        var view = new FirstPersonCamera();
        using var runner = ISceneRunner.Load(view, autoFree: true);
        var previous = ActorState.StandingAt(new System.Numerics.Vector3(0f, 0f, 0f));
        var current = ActorState.StandingAt(new System.Numerics.Vector3(0f, 0f, 4f));

        view.Present(previous, current, alpha: 0.5f, delta: 0d);

        AssertThat(view.GlobalPosition.Z).IsEqualApprox(2f, 0.01f);
    }
}
