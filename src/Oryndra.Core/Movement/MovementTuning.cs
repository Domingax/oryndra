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
/// The three ground bands follow Mirror's Edge's locomotion model — walking at
/// 2.2 m/s, running at 5.5, sprinting at 7.5 — without copying its animation
/// thresholds. A half-tilted stick walks; a full stick runs; running long enough
/// with a charged meter turns into a sprint, which spends it.
/// </para>
/// <para>
/// The meter's rates put a rhythm on a long held run: a few seconds of sprint,
/// then a refill before the next one. Sprinting is a resource the player spends,
/// not a key they hold — the rates are the first thing to re-tune against a play
/// test.
/// </para>
/// </remarks>
public sealed record MovementTuning
{
    /// <summary>Gets the tuning every new simulation starts with.</summary>
    public static MovementTuning Default { get; } = new();

    /// <summary>Gets the maximum planar speed of the walk band, in metres per second.</summary>
    public float WalkSpeed { get; init; } = 2.2f;

    /// <summary>Gets the maximum planar speed of the run band, in metres per second.</summary>
    public float RunSpeed { get; init; } = 5.5f;

    /// <summary>Gets the maximum planar speed of the sprint band, in metres per second.</summary>
    public float SprintSpeed { get; init; } = 7.5f;

    /// <summary>
    /// Gets the input magnitude at or below which the actor walks, in the [0, 1]
    /// range. Above it the actor runs.
    /// </summary>
    public float WalkAxisThreshold { get; init; } = 0.5f;

    /// <summary>Gets how fast the actor reaches its band's speed on the ground, in metres per second squared.</summary>
    public float GroundAcceleration { get; init; } = 60f;

    /// <summary>Gets how fast the actor steers while airborne, in metres per second squared.</summary>
    public float AirAcceleration { get; init; } = 12f;

    /// <summary>Gets the downward acceleration, in metres per second squared.</summary>
    public float Gravity { get; init; } = 24f;

    /// <summary>Gets the upward speed an impulse leaves the ground with, in metres per second.</summary>
    public float JumpSpeed { get; init; } = 7f;

    /// <summary>
    /// Gets how long after leaving the ground a jump is still accepted, in
    /// seconds. The tolerance is always on: without it a precise player at a
    /// ledge feels cheated.
    /// </summary>
    public float CoyoteSeconds { get; init; } = 0.1f;

    /// <summary>Gets how fast the sprint meter refills outside the sprint band, per second.</summary>
    public float SprintBuildPerSecond { get; init; } = 0.2f;

    /// <summary>Gets how fast the sprint meter drains in the sprint band, per second.</summary>
    public float SprintDrainPerSecond { get; init; } = 0.3f;

    /// <summary>
    /// Gets the meter level required to enter the sprint band, from 0 to 1. A
    /// spent meter has to refill to this level before it can sprint again, which
    /// is what keeps a long run from flickering between run and sprint.
    /// </summary>
    public float SprintActivationThreshold { get; init; } = 0.35f;

    /// <summary>Gets how long the actor must keep running before the sprint band engages, in seconds.</summary>
    public float SprintBuildUpSeconds { get; init; } = 1f;

    /// <summary>Gets the actor's body volume.</summary>
    public BodyShape Body { get; init; } = new(Radius: 0.4f, Height: 1.8f);
}
