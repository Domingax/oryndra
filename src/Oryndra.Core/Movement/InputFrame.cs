using System.Numerics;

namespace Oryndra.Core.Movement;

/// <summary>
/// What the player asked for during a single simulation tick.
/// </summary>
/// <remarks>
/// The engine layer converts devices into this frame of intent; the simulation
/// never reads an input device. This keeps replays, Ghosts and tests able to
/// drive the actor with the exact same values a player produced.
/// </remarks>
public readonly record struct InputFrame
{
    private const float MinimumAxisMagnitude = 1e-4f;

    /// <summary>
    /// Gets the requested movement in the actor's local frame: X is right and Y is
    /// forward, each in the [-1, 1] range.
    /// </summary>
    public Vector2 MoveAxis { get; init; }

    /// <summary>Gets the facing direction in radians, where 0 faces +Z.</summary>
    public float Yaw { get; init; }

    /// <summary>
    /// Gets a value indicating whether the player pressed jump during this tick.
    /// </summary>
    /// <remarks>
    /// A press, not a held state: one press is one jump. The engine layer is what
    /// turns a held key into a press; a frame that said "still held" would make the
    /// same jump fire on every tick of the coyote window.
    /// </remarks>
    public bool Jump { get; init; }

    /// <summary>
    /// Gets the direction the player is asking for in world space, following
    /// <see cref="Yaw"/>: unit length, or zero when the axis is at rest.
    /// </summary>
    /// <remarks>
    /// Yaw 0 faces +Z, and a positive yaw turns towards +X. The heading lives here
    /// rather than in the simulation so that Ghosts, tests and presentation all
    /// read the same direction out of the same input.
    /// <para>
    /// Right is right as the player sees it, which is the corner that surprises:
    /// looking along +Z, with +Y up, the hand on the right of a first-person view is
    /// -X. The vector is the facing turned a quarter turn the same way, so
    /// <c>Cross(WishDirection(forward), UnitY)</c> is the right one — the engine
    /// layer does not have to re-derive the convention as it converts a stick.
    /// </para>
    /// </remarks>
    public Vector3 WishDirection
    {
        get
        {
            var axis = MoveAxis;
            var magnitude = axis.Length();
            if (magnitude < MinimumAxisMagnitude)
            {
                return Vector3.Zero;
            }

            axis /= magnitude;
            var (sin, cos) = MathF.SinCos(Yaw);
            return new Vector3(
                (axis.Y * sin) - (axis.X * cos),
                0f,
                (axis.Y * cos) + (axis.X * sin));
        }
    }

    /// <summary>Gets a frame in which the player asked for nothing.</summary>
    public static InputFrame Idle => default;
}
