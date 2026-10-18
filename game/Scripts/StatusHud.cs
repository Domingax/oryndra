using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The little status readout in the corner: the speed band, the speed and the
/// sprint meter, plus the controls.
/// </summary>
/// <remarks>
/// It is not a navigation aid and not a game mechanic — it exists so that a
/// person can see what the simulation is doing while the greybox is played by
/// hand, and so that the walking skeleton's controls are discoverable.
/// </remarks>
public partial class StatusHud : CanvasLayer
{
    /// <summary>The node name the status readout is added under.</summary>
    public const string NodeName = "Hud";

    private const float Margin = 16f;

    private readonly Label status = new() { Name = "Status" };

    /// <inheritdoc />
    public override void _Ready()
    {
        status.Position = new Vector2(Margin, Margin);
        status.AddThemeFontSizeOverride("font_size", 18);
        status.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.92f));
        status.AddThemeConstantOverride("outline_size", 4);
        status.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.75f));
        AddChild(status);
    }

    /// <summary>Renders the actor's state as a line of text.</summary>
    /// <param name="state">The actor's state as of the last tick.</param>
    /// <param name="speed">The actor's planar speed, in metres per second.</param>
    public void Display(ActorState state, float speed)
    {
        var meter = Mathf.RoundToInt(state.SprintReserve * 100f);
        status.Text = $"{state.SpeedBand}  {speed:0.0} m/s   sprint {meter}%\n"
            + "WASD move · Alt walk · Space jump · Esc quit";
    }
}
