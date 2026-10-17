namespace Oryndra.Core.Movement;

/// <summary>
/// The speed band the actor is moving in.
/// </summary>
/// <remarks>
/// The band is a decision the simulation makes each tick — how far the player
/// pushed the stick, and what the sprint reserve allows — and it is part of the
/// state rather than something presentation infers from the velocity. Animation,
/// sound and a sprint meter all read it, and a Ghost replays it.
/// </remarks>
public enum Gait
{
    /// <summary>Standing still: the player asked for no direction.</summary>
    Idle,

    /// <summary>Moving at walking pace, on a light push of the stick.</summary>
    Walk,

    /// <summary>Moving at running pace, on a full push of the stick.</summary>
    Run,

    /// <summary>Running faster while the sprint reserve lasts.</summary>
    Sprint,
}
