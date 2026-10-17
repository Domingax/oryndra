using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests;

/// <summary>
/// Keeps the core honest about being engine-agnostic.
/// </summary>
/// <remarks>
/// The whole point of the seam is that the movement model can be tested, tuned and
/// replayed without Godot. A slipped-in engine reference would not fail any other
/// test until a headless run somewhere else blew up; this one fails immediately.
/// </remarks>
public sealed class CoreIsEngineAgnosticTests
{
    [Fact]
    public void TheCore_ReferencesNoEngineAssembly()
    {
        var core = typeof(MovementSimulation).Assembly;

        var engines = core
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(name => name.Contains("Godot", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(engines);
    }
}
