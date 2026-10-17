using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The movement simulation's whole view of the world: a single swept move.
/// </summary>
/// <remarks>
/// This is the only seam between the simulation and the engine. Anything richer —
/// probe results, surface kinds, ladders, ropes — is layered on top of a world
/// query that implements this method, so the simulation stays testable without an
/// engine (see docs/testing.md).
/// </remarks>
public interface IWorldQuery
{
    /// <summary>
    /// Moves an actor's body from <paramref name="position"/> by
    /// <paramref name="delta"/>, stopping it when it meets solid geometry.
    /// </summary>
    /// <param name="position">The body's bottom centre before the move, in metres.</param>
    /// <param name="delta">The requested displacement for one tick, in metres.</param>
    /// <param name="body">The body being moved.</param>
    /// <returns>The resulting position, the motion actually achieved and whether the body landed.</returns>
    BodyMoveResult MoveBody(Vector3 position, Vector3 delta, BodyShape body);
}
