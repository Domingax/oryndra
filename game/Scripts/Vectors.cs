namespace Oryndra.Game;

/// <summary>
/// Reads the core's vectors as Godot's.
/// </summary>
/// <remarks>
/// The two <c>Vector3</c> types are the boundary ADR-0009 warns about: the core
/// speaks <see cref="System.Numerics.Vector3"/> and Godot speaks its own, and the
/// only thing they agree on is that they are both three floats. Keeping the
/// conversion in one file means the engine layer is the only place that has to
/// know which side it is on, and it is a copy of the three components — no units,
/// no handedness, no scale to get wrong.
/// </remarks>
internal static class Vectors
{
    /// <summary>Reads a core vector as a Godot one.</summary>
    /// <param name="value">The vector to read.</param>
    /// <returns>The same three numbers, in metres.</returns>
    public static Godot.Vector3 ToGodot(this System.Numerics.Vector3 value) =>
        new(value.X, value.Y, value.Z);
}
