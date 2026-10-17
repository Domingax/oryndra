using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The device boundary: the player's hands turned into the frame of intent the
/// simulation reads.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of what the Godot layer knows about a device. The action
/// names are constants the project's input map has to answer to, so the map and
/// the code cannot drift apart in silence — a missing action reads as a zero and
/// the player would simply never move.
/// </para>
/// <para>
/// WASD and the left stick move, Space and the bottom face button jump. The stick
/// is what the speed bands are for: it can hold a light push, which is a walk,
/// where a keyboard's axes only reach full travel.
/// </para>
/// <para>
/// The jump is a press, not a held key: the core spends one coyote window per
/// press, and a held key would fire it on every tick of the window.
/// </para>
/// </remarks>
public static class MovementInput
{
    /// <summary>The action that walks the player forward, along the view.</summary>
    public const string MoveForward = "move_forward";

    /// <summary>The action that walks the player back, along the view.</summary>
    public const string MoveBack = "move_back";

    /// <summary>The action that steps the player to their right.</summary>
    public const string MoveRight = "move_right";

    /// <summary>The action that steps the player to their left.</summary>
    public const string MoveLeft = "move_left";

    /// <summary>The action that jumps.</summary>
    public const string Jump = "jump";

    /// <summary>Reads the devices into one frame of intent.</summary>
    /// <param name="yaw">Where the player is looking, in radians, where 0 faces +Z.</param>
    /// <returns>The frame the simulation should advance on.</returns>
    public static InputFrame Capture(float yaw) => Frame(
        right: Input.GetActionStrength(MoveRight) - Input.GetActionStrength(MoveLeft),
        forward: Input.GetActionStrength(MoveForward) - Input.GetActionStrength(MoveBack),
        yaw: yaw,
        jump: Input.IsActionJustPressed(Jump));

    /// <summary>Builds a frame from the player's axes, without reading a device.</summary>
    /// <param name="right">How far the stick is pushed towards the player's right, from -1 to 1.</param>
    /// <param name="forward">How far the stick is pushed along the view, from -1 to 1.</param>
    /// <param name="yaw">Where the player is looking, in radians, where 0 faces +Z.</param>
    /// <param name="jump">Whether the player pressed jump.</param>
    /// <returns>The frame the simulation should advance on.</returns>
    public static InputFrame Frame(float right, float forward, float yaw, bool jump) => new()
    {
        // The core reads X as right as the player sees it, so the axes cross the
        // boundary with their sign unchanged.
        MoveAxis = new System.Numerics.Vector2(right, forward),
        Yaw = yaw,
        Jump = jump,
    };
}
