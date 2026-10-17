using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>What one fuzz session produced.</summary>
public sealed record FuzzRun
{
    /// <summary>Gets the input frame fed to the simulation on each tick.</summary>
    public required IReadOnlyList<InputFrame> Inputs { get; init; }

    /// <summary>Gets the state the simulation reported after each tick.</summary>
    public required IReadOnlyList<ActorState> States { get; init; }

    /// <summary>Gets every invariant the run broke, in the order it broke them.</summary>
    public required IReadOnlyList<string> Violations { get; init; }

    /// <summary>Gets a value indicating whether the run broke no invariant.</summary>
    public bool Healthy => Violations.Count == 0;
}
