namespace Oryndra.Core.Movement;

/// <summary>
/// The upright volume the simulation moves through the geometry.
/// </summary>
/// <remarks>
/// The movement model works with a capsule; the collision seam only needs the two
/// numbers that describe it, and a greybox may approximate it as a box.
/// </remarks>
/// <param name="Radius">Radius of the body, in metres.</param>
/// <param name="Height">Height of the body, from the feet up, in metres.</param>
public readonly record struct BodyShape(float Radius, float Height);
