using GdUnit4;
using Godot;
using Oryndra.Core.Movement;
using static GdUnit4.Assertions;
using SysVec3 = System.Numerics.Vector3;

namespace Oryndra.Game.Tests;

/// <summary>
/// Proves the game assembly comes up inside a headless Godot runtime, its
/// simulation advances, and both seams — input and physics — are wired to the
/// engine.
/// </summary>
/// <remarks>
/// <para>
/// The node is built here rather than loaded from the game project's
/// <c>Main.tscn</c>: a test project is its own Godot project, with its own
/// <c>res://</c>, so it cannot load the game's scene. The greybox is replaced by a
/// floor and, where a test needs one, a wall. What this level proves is that the
/// game's C# assembly loads, its nodes tick, its input map drives the simulation,
/// and its swept physics query stops the actor on real geometry — a script whose
/// class name drifted from its file, an assembly Godot refuses, or a query
/// against the wrong shape all fail here.
/// </para>
/// <para>
/// Booting the project itself (project.godot, <c>Main.tscn</c>, the greybox and the
/// ready line) is asserted by <c>./verify.sh</c>, which has the game project to run.
/// </para>
/// </remarks>
[RequireGodotRuntime]
[TestSuite]
public sealed class BootSmokeTests
{
    private const int TickFrames = 60;
    private const float WallFaceZ = 6f;

    [TestCase]
    public async Task Main_WhenTheRuntimeBoots_AdvancesASimulationTick()
    {
        var main = WithFloor(new Main());
        using var runner = ISceneRunner.Load(main, autoFree: true);

        AssertThat(main.Ticks).IsEqual(0);

        // One tick to reach the frame, one more to see it processed.
        await ISceneRunner.SyncPhysicsFrame;
        await ISceneRunner.SyncPhysicsFrame;

        AssertThat(main.Ticks).IsGreater(0);
        AssertThat(main.State.IsFinite).IsTrue();
        AssertThat(main.State.Grounded).IsTrue();
        AssertThat(main.GetNodeOrNull<Camera3D>(FirstPersonCamera.CameraPath)).IsNotNull();
    }

    [TestCase]
    public async Task Main_WhenTheActorStands_TheCameraRidesAtEyeHeight()
    {
        var main = WithFloor(new Main());
        using var runner = ISceneRunner.Load(main, autoFree: true);

        await ISceneRunner.SyncPhysicsFrame;
        await ISceneRunner.SyncProcessFrame;

        var camera = main.GetNodeOrNull<Camera3D>(FirstPersonCamera.CameraPath);
        AssertThat(camera).IsNotNull();
        AssertThat(camera!.Current).IsTrue();

        var head = main.GetNodeOrNull<Node3D>(FirstPersonCamera.NodeName);
        AssertThat(head).IsNotNull();
        AssertThat(head!.Position.Y).IsEqualApprox(main.State.Position.Y + FirstPersonCamera.EyeHeight, 0.05f);
    }

    [TestCase]
    public async Task Main_WhenThePlayerHoldsTheWalkModifier_StaysInTheWalkBand()
    {
        var main = WithFloor(new Main());
        using var runner = ISceneRunner.Load(main, autoFree: true);
        runner.SimulateActionPress(PlayerInput.Walk);
        runner.SimulateActionPress(PlayerInput.MoveForward);

        await RunPhysicsFrames(TickFrames);

        runner.SimulateActionRelease(PlayerInput.Walk);
        runner.SimulateActionRelease(PlayerInput.MoveForward);

        AssertThat(main.State.SpeedBand).IsEqual(SpeedBand.Walk);
        AssertThat(PlanarSpeed(main)).IsEqualApprox(MovementTuning.Default.WalkSpeed, 0.05f);
    }

    [TestCase]
    public async Task Main_WhenThePlayerRunsIntoAWall_StopsOnTheWallFace()
    {
        var main = WithFloorAndWall(new Main());
        using var runner = ISceneRunner.Load(main, autoFree: true);
        runner.SimulateActionPress(PlayerInput.MoveForward);

        await RunPhysicsFrames(TickFrames * 2);

        runner.SimulateActionRelease(PlayerInput.MoveForward);

        var radius = MovementTuning.Default.Body.Radius;
        AssertThat(main.State.Position.Z).IsLess(WallFaceZ);
        AssertThat(main.State.Position.Z).IsGreater(WallFaceZ - radius - 0.05f);
    }

    [TestCase]
    public void FeetBetween_BetweenTwoTicks_ReadsTheInterpolatedFeet()
    {
        var previous = ActorState.StandingAt(new SysVec3(0f, 0f, 0f));
        var current = ActorState.StandingAt(new SysVec3(0f, 0f, 2f));

        var feet = FirstPersonCamera.FeetBetween(previous, current, 0.25f);

        AssertThat(feet.Z).IsEqualApprox(0.5f, 1e-4f);
    }

    private static float PlanarSpeed(Main main) =>
        MathF.Sqrt(
            (main.State.Velocity.X * main.State.Velocity.X)
            + (main.State.Velocity.Z * main.State.Velocity.Z));

    private static async Task RunPhysicsFrames(int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            await ISceneRunner.SyncPhysicsFrame;
        }
    }

    /// <summary>Puts a floor under the actor: the test cannot load the game's greybox.</summary>
    private static Main WithFloor(Main main)
    {
        main.AddChild(Solid(new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f)));
        return main;
    }

    /// <summary>Puts a floor and a wall across the actor's path.</summary>
    private static Main WithFloorAndWall(Main main)
    {
        WithFloor(main);
        main.AddChild(Solid(
            new Vector3(0f, 1.5f, WallFaceZ + 0.5f),
            new Vector3(20f, 3f, 1f)));
        return main;
    }

    private static StaticBody3D Solid(Vector3 position, Vector3 size)
    {
        var body = new StaticBody3D { Position = position };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        return body;
    }
}
