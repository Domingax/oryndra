using Godot;

using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The first-person presentation of the simulated actor: the head the camera rides.
/// </summary>
/// <remarks>
/// The simulation ticks at a fixed rate; the engine renders at whatever rate it
/// can. The head is therefore placed between the last two ticks rather than on
/// one of them — the interpolation is what keeps a 60 Hz simulation looking
/// smooth on a 144 Hz screen, and what keeps it honest on a slow one.
/// </remarks>
public partial class FirstPersonCamera : Node3D
{
    /// <summary>The node name the camera rig is added under.</summary>
    public const string NodeName = "Camera";

    /// <summary>The node name of the camera inside the rig.</summary>
    public const string CameraNodeName = "Camera3D";

    /// <summary>The path of the camera under the game root.</summary>
    public const string CameraPath = NodeName + "/" + CameraNodeName;

    /// <summary>How high above the actor's feet the camera sits, in metres.</summary>
    public const float EyeHeight = 1.62f;

    /// <summary>The camera's field of view at rest, in degrees.</summary>
    public const float BaseFov = 88f;

    /// <summary>The camera's field of view at full sprint, in degrees.</summary>
    public const float SprintFov = 94f;

    /// <summary>How fast the field of view follows the speed, as a time constant.</summary>
    private const float FovSharpness = 4f;

    private const float BobAmplitude = 0.018f;
    private const float BobCyclesPerMetre = 0.28f;
    private const float IdleSpeed = 0.1f;

    private readonly MovementTuning tuning = MovementTuning.Default;

    private Camera3D camera = null!;
    private float bobPhase;

    /// <summary>Gets the camera the rig owns.</summary>
    public Camera3D Camera => camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        camera = new Camera3D
        {
            Name = CameraNodeName,
            Current = true,
            Fov = BaseFov,
            Near = 0.05f,
            Far = 500f,
        };
        AddChild(camera);
    }

    /// <summary>
    /// Places the head between two simulation ticks and points it where the mouse
    /// asked, with the speed's own head motion and a field of view that opens up.
    /// </summary>
    /// <param name="previous">The state before the last tick.</param>
    /// <param name="current">The state after the last tick.</param>
    /// <param name="alpha">How far the render is between the two ticks, in [0, 1].</param>
    /// <param name="yaw">The facing the player asked for, in radians.</param>
    /// <param name="pitch">The look angle the player asked for, in radians.</param>
    /// <param name="deltaSeconds">The render frame's duration, in seconds.</param>
    public void Apply(
        ActorState previous,
        ActorState current,
        float alpha,
        float yaw,
        float pitch,
        float deltaSeconds)
    {
        var speed = GodotInterop.PlanarLength(current.Velocity);

        Position = FeetBetween(previous, current, alpha)
            + new Vector3(0f, EyeHeight + HeadBob(current.Grounded, speed, deltaSeconds), 0f);
        Rotation = new Vector3(pitch, yaw + Mathf.Pi, 0f);

        var sprinting = Mathf.Clamp(
            (speed - tuning.RunSpeed) / (tuning.SprintSpeed - tuning.RunSpeed),
            0f,
            1f);
        var targetFov = Mathf.Lerp(BaseFov, SprintFov, sprinting);
        camera.Fov = Mathf.Lerp(camera.Fov, targetFov, 1f - MathF.Exp(-FovSharpness * deltaSeconds));
    }

    /// <summary>Reads the actor's feet between two ticks: what the camera actually follows.</summary>
    /// <param name="previous">The state before the last tick.</param>
    /// <param name="current">The state after the last tick.</param>
    /// <param name="alpha">How far the render is between the two ticks, in [0, 1].</param>
    /// <returns>The feet position to render at.</returns>
    public static Vector3 FeetBetween(ActorState previous, ActorState current, float alpha) =>
        GodotInterop.ToGodot(previous.Position).Lerp(GodotInterop.ToGodot(current.Position), alpha);

    /// <summary>
    /// A slight vertical sway tied to the distance travelled on the ground. It is
    /// deliberately small: enough to feel the stride, never enough to read as
    /// camera noise.
    /// </summary>
    private float HeadBob(bool grounded, float speed, float deltaSeconds)
    {
        if (!grounded || speed < IdleSpeed)
        {
            return 0f;
        }

        bobPhase = Mathf.Wrap(
            bobPhase + (speed * BobCyclesPerMetre * Mathf.Tau * deltaSeconds),
            0f,
            Mathf.Tau);

        var normalized = Mathf.Clamp(speed / tuning.RunSpeed, 0f, 1f);
        return MathF.Sin(bobPhase) * BobAmplitude * normalized;
    }
}
