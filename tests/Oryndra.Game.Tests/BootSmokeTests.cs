using GdUnit4;

using static GdUnit4.Assertions;

namespace Oryndra.Game.Tests;

/// <summary>
/// Proves the project boots: the game's root node comes up inside a headless
/// Godot runtime and its simulation advances.
/// </summary>
/// <remarks>
/// This is the cheapest test that can catch an engine-layer break — a scene that
/// no longer loads, an assembly Godot refuses, a script whose class name drifted
/// from its file. The core's own tests cannot see any of those.
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
