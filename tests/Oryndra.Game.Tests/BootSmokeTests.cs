using GdUnit4;

using static GdUnit4.Assertions;

namespace Oryndra.Game.Tests;

/// <summary>
/// Proves the game assembly comes up inside a headless Godot runtime and its
/// simulation advances.
/// </summary>
/// <remarks>
/// <para>
/// The node is built here rather than loaded from the game project's
/// <c>Main.tscn</c>: a test project is its own Godot project, with its own
/// <c>res://</c>, so it cannot load the game's scene. What this level proves is
/// that the game's C# assembly loads, its nodes tick, and the seam is wired to the
/// engine — a script whose class name drifted from its file, or an assembly Godot
/// refuses, fails here.
/// </para>
/// <para>
/// Booting the project itself (project.godot, <c>Main.tscn</c>, the ready line) is
/// asserted by <c>./verify.sh</c>, which has the game project to run.
/// </para>
/// </remarks>
[RequireGodotRuntime]
[TestSuite]
public sealed class BootSmokeTests
{
    [TestCase]
    public async Task Main_WhenTheRuntimeBoots_AdvancesASimulationTick()
    {
        var main = new Main();
        using var runner = ISceneRunner.Load(main, autoFree: true);

        AssertThat(main.Ticks).IsEqual(0);

        // One tick to reach the frame, one more to see it processed.
        await ISceneRunner.SyncPhysicsFrame;
        await ISceneRunner.SyncPhysicsFrame;

        AssertThat(main.Ticks).IsGreater(0);
        AssertThat(main.State.IsFinite).IsTrue();
        AssertThat(main.State.Grounded).IsTrue();
    }
}
