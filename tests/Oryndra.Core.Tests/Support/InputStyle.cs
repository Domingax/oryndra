namespace Oryndra.Core.Tests.Support;

/// <summary>The shape of the input a fuzz run drives the actor with.</summary>
public enum InputStyle
{
    /// <summary>A fresh direction every tick: the input space, unshaped.</summary>
    Chaotic,

    /// <summary>A direction held for a stretch, with jumps on purpose: the input a player produces.</summary>
    Deliberate,
}
