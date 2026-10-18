namespace Oryndra.Game;

/// <summary>
/// The boundary between the core's <see cref="System.Numerics"/> vectors and the
/// engine's <see cref="Godot.Vector3"/>.
/// </summary>
/// <remarks>
/// Both the input and the collision layer cross this boundary; keeping the
/// conversion in one place means the coordinate convention is stated once.
/// </remarks>
internal static class GodotInterop
{
    /// <summary>Converts a core vector into the engine's vector, metres for metres.</summary>
    /// <param name="vector">The vector to convert.</param>
    /// <returns>The same point in the engine's coordinates.</returns>
    public static Godot.Vector3 ToGodot(System.Numerics.Vector3 vector) =>
        new(vector.X, vector.Y, vector.Z);

    /// <summary>Gets the length of the horizontal part of a vector, ignoring its height.</summary>
    /// <param name="vector">The vector whose planar length is wanted.</param>
    /// <returns>The length of the vector projected onto the XZ plane.</returns>
    public static float PlanarLength(System.Numerics.Vector3 vector) =>
        MathF.Sqrt((vector.X * vector.X) + (vector.Z * vector.Z));
}
