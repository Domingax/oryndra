using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The keyboard actions the player uses, registered with the engine's input map.
/// </summary>
/// <remarks>
/// The actions are created in code rather than authored in <c>project.godot</c> so
/// that the game is playable from a fresh checkout without an editor pass, and so
/// that headless runs and the runtime tests see the same bindings. Rebindable
/// settings are a later ticket; this is the walking skeleton's fixed layout.
/// </remarks>
public static class PlayerInput
{
    /// <summary>The action that moves the actor towards its facing.</summary>
    public const string MoveForward = "move_forward";

    /// <summary>The action that moves the actor backwards.</summary>
    public const string MoveBack = "move_back";

    /// <summary>The action that moves the actor to its left.</summary>
    public const string MoveLeft = "move_left";

    /// <summary>The action that moves the actor to its right.</summary>
    public const string MoveRight = "move_right";

    /// <summary>The action that jumps.</summary>
    public const string Jump = "jump";

    /// <summary>The action held down to walk instead of run.</summary>
    public const string Walk = "walk";

    /// <summary>
    /// How much of the core's walk threshold a held walk modifier aims for. It is
    /// deliberately below the threshold, so the band comes from the core's own
    /// number rather than from a magnitude copied here.
    /// </summary>
    private const float WalkFraction = 0.8f;

    /// <summary>Whether the player is holding the walk modifier.</summary>
    public static bool Walking => Input.IsActionPressed(Walk);

    /// <summary>Whether the jump was pressed on this frame.</summary>
    public static bool Jumped => Input.IsActionJustPressed(Jump);

    /// <summary>The movement the player is asking for, in the actor's local frame.</summary>
    /// <param name="tuning">The tuning whose walk threshold the modifier lands under.</param>
    /// <returns>X is right and Y is forward, exactly the frame the core expects.</returns>
    public static Vector2 MoveAxis(MovementTuning tuning)
    {
        var axis = Input.GetVector(MoveLeft, MoveRight, MoveBack, MoveForward);
        return Walking ? axis * (tuning.WalkAxisThreshold * WalkFraction) : axis;
    }

    /// <summary>Registers every action, unless the project already defines it.</summary>
    public static void EnsureActions()
    {
        Add(MoveForward, Key.W);
        Add(MoveBack, Key.S);
        Add(MoveLeft, Key.A);
        Add(MoveRight, Key.D);
        Add(Jump, Key.Space);
        Add(Walk, Key.Alt);
    }

    private static void Add(string action, Key key)
    {
        if (InputMap.HasAction(action))
        {
            return;
        }

        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
