using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// The movement simulation's whole view of the geometry around the actor: a single
/// swept move.
/// </summary>
/// <remarks>
/// <para>
/// This is the only seam between the simulation and the engine, and it is named
/// after what it answers rather than after the playable Map (which the glossary
/// keeps for the environment a Course is laid out on).
/// </para>
/// <para>
/// Anything richer — probe results, surface kinds, ladders, ropes — is layered on
/// top of a collision query that implements this method, so the simulation stays
/// testable without an engine (see docs/testing.md).
/// </para>
/// </remarks>
public interface ICollisionQuery
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
