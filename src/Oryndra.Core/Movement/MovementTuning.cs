namespace Oryndra.Core.Movement;

/// <summary>
/// The numbers the movement model is tuned with.
/// </summary>
/// <remarks>
/// <para>
/// Movement is data-driven: a move is a record of values rather than a pile of
/// constants (see PRD.md, "Movement — the heart of the game"). These defaults are
/// provisional starting points in oryndra's scale, in metres and seconds; the
/// reference model is documented in docs/research/mirrors-edge-1-movement.md and
/// is to be re-tuned against play tests, not copied.
/// </para>
/// <para>
/// Two of these are not tuning knobs in the ordinary sense.
/// <see cref="CoyoteTime"/> is baseline input tolerance: the PRD keeps it always
/// on, and it is deliberately not a difficulty setting (PRD.md, "Navigation &amp;
/// accessibility"). The sprint values are a reserve rather than a button: the PRD
/// asks for a meter that builds and drains (PRD.md, "Movement").
/// </para>
/// </remarks>
public sealed record MovementTuning
{
    /// <summary>Gets the tuning every new simulation starts with.</summary>
    public static MovementTuning Default { get; } = new();

    /// <summary>Gets the walking speed, at the bottom of the speed bands, in metres per second.</summary>
    public float WalkSpeed { get; init; } = 3f;

    /// <summary>Gets the running speed, in metres per second.</summary>
    public float RunSpeed { get; init; } = 6f;

    /// <summary>Gets the sprinting speed, reached while the sprint reserve lasts, in metres per second.</summary>
    public float SprintSpeed { get; init; } = 8f;

    /// <summary>
    /// Gets how far the movement axis has to travel for the actor to run rather
    /// than walk, from 0 to 1.
    /// </summary>
    /// <remarks>
    /// Only a stick can hold a middle value, so a keyboard — whose axes reach full
    /// travel — always runs. Walking is what the player does on purpose, with a
    /// light push.
    /// </remarks>
    public float RunAxisThreshold { get; init; } = 0.5f;

    /// <summary>Gets how fast the actor reaches the band's speed on the ground, in metres per second squared.</summary>
    public float GroundAcceleration { get; init; } = 60f;

    /// <summary>Gets how fast the actor steers while airborne, in metres per second squared.</summary>
    public float AirAcceleration { get; init; } = 12f;

    /// <summary>Gets the downward acceleration, in metres per second squared.</summary>
    public float Gravity { get; init; } = 24f;

    /// <summary>Gets the upward speed an impulse leaves the ground with, in metres per second.</summary>
    public float JumpSpeed { get; init; } = 7f;

    /// <summary>
    /// Gets how long after leaving the ground a jump still counts, in seconds.
    /// </summary>
    /// <remarks>
    /// Baseline input tolerance, always on: without it a jump pressed on the last
    /// stride off a ledge is dropped and the game seems broken. It makes pressing
    /// the button more forgiving; it does not make a gap easier.
    /// </remarks>
    public float CoyoteTime { get; init; } = 0.12f;

    /// <summary>Gets how fast the sprint reserve fills while the actor runs on the ground, per second.</summary>
    public float SprintBuildRate { get; init; } = 0.5f;

    /// <summary>Gets how fast the sprint reserve empties while the actor sprints, per second.</summary>
    public float SprintDrainRate { get; init; } = 0.25f;

    /// <summary>Gets the reserve, from 0 to 1, at which a run turns into a sprint.</summary>
    /// <remarks>Full, so that a sprint costs a real stretch of clean running first.</remarks>
    public float SprintEngageThreshold { get; init; } = 1f;

    /// <summary>Gets the reserve, from 0 to 1, at which a sprint gives out.</summary>
    /// <remarks>
    /// Spent, and below <see cref="SprintEngageThreshold"/>, so that the two
    /// thresholds form a hysteresis a jittering reserve cannot rattle across.
    /// </remarks>
    public float SprintReleaseThreshold { get; init; }

    /// <summary>Gets the actor's body volume.</summary>
    public BodyShape Body { get; init; } = new(Radius: 0.4f, Height: 1.8f);
}
