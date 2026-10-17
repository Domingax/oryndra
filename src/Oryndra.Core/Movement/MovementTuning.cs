namespace Oryndra.Core.Movement;

/// <summary>
/// The numbers the movement model is tuned with.
/// </summary>
/// <remarks>
/// Movement is data-driven: a move is a record of values rather than a pile of
/// constants (see PRD.md, "Movement — the heart of the game"). These defaults are
/// provisional starting points in oryndra's scale, in metres and seconds; the
/// reference model is documented in docs/research/mirrors-edge-1-movement.md and
/// is to be re-tuned against play tests, not copied.
/// </remarks>
public sealed record MovementTuning
{
    /// <summary>Gets the tuning every new simulation starts with.</summary>
    public static MovementTuning Default { get; } = new();

    /// <summary>Gets the maximum planar speed the actor accelerates to, in metres per second.</summary>
    public float MoveSpeed { get; init; } = 6f;

    /// <summary>Gets how fast the actor reaches <see cref="MoveSpeed"/> on the ground, in metres per second squared.</summary>
    public float GroundAcceleration { get; init; } = 60f;

    /// <summary>Gets how fast the actor steers while airborne, in metres per second squared.</summary>
    public float AirAcceleration { get; init; } = 12f;

    /// <summary>Gets the downward acceleration, in metres per second squared.</summary>
    public float Gravity { get; init; } = 24f;

    /// <summary>Gets the upward speed an impulse leaves the ground with, in metres per second.</summary>
    public float JumpSpeed { get; init; } = 7f;

    /// <summary>Gets the actor's body volume.</summary>
    public BodyShape Body { get; init; } = new(Radius: 0.4f, Height: 1.8f);
}
