using System.Numerics;

using Oryndra.Core.Movement;

namespace Oryndra.Core.Tests.Support;

/// <summary>Conveniences the movement tests share.</summary>
internal static class Simulate
{
    /// <summary>Advances a simulation for <paramref name="ticks"/> ticks, always with the same frame.</summary>
    /// <param name="simulation">The simulation to drive.</param>
    /// <param name="input">The frame to repeat.</param>
    /// <param name="ticks">How many ticks to advance.</param>
    /// <returns>The state after the last tick.</returns>
    public static ActorState RunFor(this MovementSimulation simulation, InputFrame input, int ticks)
    {
        var state = simulation.State;
        for (var tick = 0; tick < ticks; tick++)
        {
            state = simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
        }

        return state;
    }

    /// <summary>Builds a frame asking for movement along the actor's local axes, facing +Z.</summary>
    /// <param name="right">Component along the actor's right.</param>
    /// <param name="forward">Component along the actor's facing.</param>
    /// <returns>The input frame.</returns>
    public static InputFrame Moving(float right, float forward) =>
        new() { MoveAxis = new Vector2(right, forward) };

    /// <summary>Builds a frame asking for a full-speed run towards a facing.</summary>
    /// <param name="yaw">Facing, in radians.</param>
    /// <returns>The input frame: full forwards stick, no jump.</returns>
    public static InputFrame RunningTowards(float yaw) =>
        new() { MoveAxis = new Vector2(0f, 1f), Yaw = yaw };
}
