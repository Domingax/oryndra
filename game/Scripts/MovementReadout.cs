using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The greybox's instrument panel: what the movement is doing, right now.
/// </summary>
/// <remarks>
/// <para>
/// Feel cannot be read out of a source file, and a play test that comes back with
/// "the view opens too fast" is much easier to act on when the view's number sits on
/// screen beside the speed that drives it. This is a greybox tool, not a game HUD:
/// the PRD keeps navigation aids out of the game, and this shows numbers rather than
/// a path.
/// </para>
/// <para>
/// Visible by default while a greybox is all there is to play, and toggled with F3
/// (<see cref="Main._UnhandledInput"/>).
/// </para>
/// </remarks>
public partial class MovementReadout : CanvasLayer
{
    private Label text = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        text = new Label
        {
            Name = "Text",
            Position = new Vector2(12f, 8f),
            LabelSettings = new LabelSettings { FontSize = 14, OutlineSize = 4 },
        };

        AddChild(text);
    }

    /// <summary>Writes what the movement is doing.</summary>
    /// <param name="state">The actor's state as of the last tick.</param>
    /// <param name="tuning">The tuning it was advanced with.</param>
    /// <param name="fov">The field of view the eye is drawing with, in degrees.</param>
    /// <param name="ticks">How many ticks the simulation has advanced.</param>
    public void Report(ActorState state, MovementTuning tuning, float fov, int ticks) =>
        text.Text = Describe(state, tuning, fov, ticks);

    /// <summary>Formats the movement, so the numbers can be read without a screen.</summary>
    /// <param name="state">The actor's state as of the last tick.</param>
    /// <param name="tuning">The tuning it was advanced with.</param>
    /// <param name="fov">The field of view the eye is drawing with, in degrees.</param>
    /// <param name="ticks">How many ticks the simulation has advanced.</param>
    /// <returns>The panel's text.</returns>
    public static string Describe(ActorState state, MovementTuning tuning, float fov, int ticks)
    {
        // The ground speed is the one the bands and the view answer to, so it is the
        // one worth putting first; the vertical one is what a fall or a jump adds.
        float ground = new System.Numerics.Vector2(state.Velocity.X, state.Velocity.Z).Length();
        string footing = state.Grounded ? "grounded" : "airborne";

        return $"{state.Gait} {ground:0.0}/{tuning.SprintSpeed:0} m/s  vertical {state.Velocity.Y:0.0}"
            + $"\nsprint {state.SprintReserve:P0}  {footing}  coyote {state.CoyoteSecondsLeft:0.00} s"
            + $"\nfov {fov:0.0}°  ticks {ticks}";
    }
}
