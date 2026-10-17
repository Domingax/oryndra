using System.Numerics;

namespace Oryndra.Core.Tests.Support;

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
    public static Box FromCorners(Vector3 corner, Vector3 opposite) =>
        new(Vector3.Min(corner, opposite), Vector3.Max(corner, opposite));

    /// <summary>Creates the volume an actor's body occupies when standing at <paramref name="feet"/>.</summary>
    public static Box BodyVolume(Vector3 feet, Core.Movement.BodyShape body) => new(
        new Vector3(feet.X - body.Radius, feet.Y, feet.Z - body.Radius),
        new Vector3(feet.X + body.Radius, feet.Y + body.Height, feet.Z + body.Radius));

    /// <summary>Creates the smallest box containing both this box and <paramref name="other"/>.</summary>
    public Box Union(Box other) => new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    /// <summary>Gets a value indicating whether the two boxes share volume.</summary>
    public bool Intersects(Box other) =>
        Min.X < other.Max.X
        && other.Min.X < Max.X
        && Min.Y < other.Max.Y
        && other.Min.Y < Max.Y
        && Min.Z < other.Max.Z
        && other.Min.Z < Max.Z;

    /// <summary>Gets a value indicating whether this box contains <paramref name="other"/>.</summary>
    public bool Contains(Box other, float tolerance = 0f) =>
        other.Min.X >= Min.X - tolerance
        && other.Min.Y >= Min.Y - tolerance
        && other.Min.Z >= Min.Z - tolerance
        && other.Max.X <= Max.X + tolerance
        && other.Max.Y <= Max.Y + tolerance
        && other.Max.Z <= Max.Z + tolerance;
}
