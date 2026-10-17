using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// An axis-aligned block of solid geometry.
/// </summary>
/// <remarks>
/// Greybox geometry only: the movement seam never sees a mesh, just volumes to
/// run into. Touching faces do not count as an overlap, so a body resting exactly
/// on a floor is outside it.
/// </remarks>
public readonly record struct Box(Vector3 Min, Vector3 Max)
{
    /// <summary>Creates the smallest box containing <paramref name="corner"/> and <paramref name="opposite"/>.</summary>
    /// <param name="corner">One corner of the block.</param>
    /// <param name="opposite">The corner diagonally opposite.</param>
    /// <returns>The block spanning both corners.</returns>
    public static Box FromCorners(Vector3 corner, Vector3 opposite) =>
        new(Vector3.Min(corner, opposite), Vector3.Max(corner, opposite));

    /// <summary>Creates a block from its centre and its size.</summary>
    /// <param name="centre">The centre of the block, in metres.</param>
    /// <param name="size">The block's full extent along each axis, in metres.</param>
    /// <returns>The block spanning <paramref name="centre"/> ± half of <paramref name="size"/>.</returns>
    public static Box Around(Vector3 centre, Vector3 size) =>
        FromCorners(centre - (size / 2f), centre + (size / 2f));

    /// <summary>Creates the volume an actor's body occupies when standing at <paramref name="feet"/>.</summary>
    /// <param name="feet">Bottom centre of the body, in metres.</param>
    /// <param name="body">The body being placed.</param>
    /// <returns>The block the body would fill.</returns>
    public static Box BodyVolume(Vector3 feet, BodyShape body) => new(
        new Vector3(feet.X - body.Radius, feet.Y, feet.Z - body.Radius),
        new Vector3(feet.X + body.Radius, feet.Y + body.Height, feet.Z + body.Radius));

    /// <summary>Creates the smallest box containing both this box and <paramref name="other"/>.</summary>
    /// <param name="other">The box to include.</param>
    /// <returns>The union of the two boxes.</returns>
    public Box Union(Box other) => new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    /// <summary>Gets a value indicating whether the two boxes share volume.</summary>
    /// <param name="other">The box to compare against.</param>
    /// <returns><see langword="true"/> when the two boxes overlap.</returns>
    public bool Intersects(Box other) =>
        Min.X < other.Max.X
        && other.Min.X < Max.X
        && Min.Y < other.Max.Y
        && other.Min.Y < Max.Y
        && Min.Z < other.Max.Z
        && other.Min.Z < Max.Z;

    /// <summary>Gets a value indicating whether this box contains <paramref name="other"/>.</summary>
    /// <param name="other">The box to look for.</param>
    /// <param name="tolerance">How far outside the faces <paramref name="other"/> may still reach.</param>
    /// <returns><see langword="true"/> when nothing of <paramref name="other"/> falls outside this box.</returns>
    public bool Contains(Box other, float tolerance = 0f) =>
        other.Min.X >= Min.X - tolerance
        && other.Min.Y >= Min.Y - tolerance
        && other.Min.Z >= Min.Z - tolerance
        && other.Max.X <= Max.X + tolerance
        && other.Max.Y <= Max.Y + tolerance
        && other.Max.Z <= Max.Z + tolerance;
}
