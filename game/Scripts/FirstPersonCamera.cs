using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The player's eye: a first-person camera that renders the simulated actor.
/// </summary>
/// <remarks>
/// <para>
/// It holds the two things the simulation does not: where the head is pointed
/// (yaw and pitch, which no move needs) and how a state is drawn between two
/// ticks. The camera reads the state; it never decides one (ADR-0009).
/// </para>
/// <para>
/// Interpolation is what makes a fixed 60 Hz simulation draw smoothly on a display
/// that refreshes at some other rate: drawing the last tick's position would judder
/// by up to one tick, which is exactly the kind of thing first-person movement
/// shows. The look is the exception — it is applied as it arrives, because a frame
/// of lag in the player's own hand is felt.
/// </para>
/// </remarks>
public partial class FirstPersonCamera : Camera3D
{
    /// <summary>How far below the top of the body the eye sits, in metres.</summary>
    private const float EyeDrop = 0.15f;

    /// <summary>How far the view turns per pixel of mouse motion, in radians.</summary>
    private const float LookSensitivity = 0.0025f;

    /// <summary>How far the view may tilt from the horizon, in radians (a little under straight up).</summary>
    private const float MaxPitch = 1.5533f;

    /// <summary>The field of view at a standstill, in degrees.</summary>
    private const float BaseFov = 80f;

    /// <summary>How many degrees the field of view opens up at full sprint.</summary>
    private const float FovGain = 8f;

    /// <summary>How far the head moves, in metres: the whole of the head motion, on purpose.</summary>
    private const float BobAmplitude = 0.02f;

    /// <summary>Metres of travel per head-bob cycle.</summary>
    private const float BobStride = 2.4f;

    private float pitch;
    private float bobPhase;

    /// <summary>
    /// Gets or sets the tuning the view takes its eye height and its field of view
    /// from, so that the two stay in step with the movement they present.
    /// </summary>
    public MovementTuning Tuning { get; set; } = MovementTuning.Default;

    /// <summary>Gets where the player is looking, in radians, where 0 faces +Z.</summary>
    public float Yaw { get; private set; }

    /// <summary>Gets how far above the actor's feet the eye sits, in metres.</summary>
    public float EyeHeight => Tuning.Body.Height - EyeDrop;

    /// <inheritdoc />
    public override void _Ready() => Current = true;

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            // Turning right is a quarter turn towards the actor's right, which is
            // the direction a positive yaw already turns away from: hence the
            // subtraction. The engine layer hands the yaw to the simulation as it
            // stands, and the core's InputFrame owns the convention.
            Yaw -= motion.Relative.X * LookSensitivity;
            pitch = Mathf.Clamp(
                pitch - (motion.Relative.Y * LookSensitivity),
                -MaxPitch,
                MaxPitch);
        }
    }

    /// <summary>Draws the actor where it is between two simulation ticks.</summary>
    /// <param name="previous">The state the actor was in before the last tick.</param>
    /// <param name="current">The state the actor is in after the last tick.</param>
    /// <param name="alpha">How far past the last tick the display is, from 0 to 1.</param>
    /// <param name="delta">The frame's duration, in seconds, for the head's own motion.</param>
    public void Present(ActorState previous, ActorState current, float alpha, double delta)
    {
        var feet = System.Numerics.Vector3.Lerp(previous.Position, current.Position, alpha);
        var speed = current.Velocity.Length();

        // A head that never moves reads as a body sliding, so the eye rides a short
        // cycle driven by the distance covered. Only while the actor is on the
        // ground: in the air there is nothing to step with.
        if (current.Grounded && speed > 0.1f)
        {
            bobPhase = Mathf.PosMod(bobPhase + ((float)(speed * delta) / BobStride), 1f);
        }

        var bob = current.Grounded ? Mathf.Sin(bobPhase * Mathf.Tau) * BobAmplitude : 0f;

        var eye = feet.ToGodot();
        eye.Y += EyeHeight + bob;
        GlobalPosition = eye;

        // Godot's camera looks down its own -Z, and the actor faces +Z: half a turn
        // reconciles the two conventions, and pitch is applied about the turned axis.
        Rotation = new Vector3(pitch, Yaw + Mathf.Pi, 0f);

        // Speed widening the view is the cheapest honest way to make running feel
        // faster; it is presentation, so it is free to lie about nothing but light.
        Fov = BaseFov + (FovGain * Mathf.Clamp(speed / Tuning.SprintSpeed, 0f, 1f));
    }
}
