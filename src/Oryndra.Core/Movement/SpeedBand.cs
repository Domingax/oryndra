namespace Oryndra.Core.Movement;

/// <summary>
/// The discrete ground speed an actor is in during a tick.
/// </summary>
/// <remarks>
/// The bands are discrete on purpose: the player asks for a direction and an
/// amount of input, and the simulation picks the tuned speed that intent belongs
/// to. Presentation and tests read the band directly instead of inferring it.
/// </remarks>
public enum SpeedBand
{
    /// <summary>The actor is not asking to move.</summary>
    Idle = 0,

    /// <summary>The actor is moving at <see cref="MovementTuning.WalkSpeed"/>.</summary>
    Walk = 1,

    /// <summary>The actor is moving at <see cref="MovementTuning.RunSpeed"/>.</summary>
    Run = 2,

    /// <summary>The actor is spending the sprint meter to move at <see cref="MovementTuning.SprintSpeed"/>.</summary>
    Sprint = 3,
}
