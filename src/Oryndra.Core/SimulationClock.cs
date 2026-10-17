namespace Oryndra.Core;

/// <summary>
/// The fixed step at which the simulation advances.
/// </summary>
/// <remarks>
/// The simulation is decoupled from the render rate: presentation reads
/// <see cref="Movement.ActorState"/> between two ticks and interpolates. Every move in the
/// game is tuned against this single step, so it is a constant rather than a
/// per-frame duration.
/// </remarks>
public static class SimulationClock
{
    /// <summary>Number of simulation ticks per second.</summary>
    public const int TicksPerSecond = 60;

    /// <summary>Duration of one simulation tick, in seconds.</summary>
    public const float FixedDeltaSeconds = 1f / TicksPerSecond;
}
