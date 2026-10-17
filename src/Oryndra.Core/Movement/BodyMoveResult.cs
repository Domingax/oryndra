using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// What happened when the geometry moved an actor's body by a requested amount.
/// </summary>
/// <param name="Position">Where the body actually ended up, in metres.</param>
/// <param name="Motion">How far the body actually travelled, in metres.</param>
/// <param name="Grounded">
/// Whether a downward move was stopped by a floor, meaning the actor is standing on it.
/// </param>
/// <remarks>
/// Position and motion are both reported because they are not always equal: a
/// collision query that steps over a ledge or slides along a slope ends somewhere
/// other than straight ahead.
/// </remarks>
public readonly record struct BodyMoveResult(Vector3 Position, Vector3 Motion, bool Grounded);
