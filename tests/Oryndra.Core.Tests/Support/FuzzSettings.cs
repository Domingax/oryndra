using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>How to run one fuzz session.</summary>
public sealed record FuzzSettings
{
    /// <summary>Gets the seed of the input sequence; the same seed replays the same run.</summary>
    public required int Seed { get; init; }

    /// <summary>Gets how many ticks to simulate.</summary>
    public int Ticks { get; init; } = 2_000;

    /// <summary>Gets the duration of one tick, in seconds.</summary>
    public float DeltaSeconds { get; init; } = SimulationClock.FixedDeltaSeconds;

    /// <summary>Gets the state the actor starts in: on the floor, just off it, in the middle of the room.</summary>
    public ActorState Start { get; init; } = ActorState.StandingAt(new Vector3(0f, 0.1f, 0f));

    /// <summary>Gets the speed above which a run counts as out of control, in metres per second.</summary>
    /// <remarks>
    /// A room-sized greybox cannot produce more than a fall from wall height
    /// (around 14 m/s); anything beyond this bound means the model is feeding
    /// itself speed.
    /// </remarks>
    public float MaxSpeed { get; init; } = 30f;

    /// <summary>Gets how many violations to collect before giving up on the run.</summary>
    public int MaxViolations { get; init; } = 20;
}
