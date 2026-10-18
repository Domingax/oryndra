using Godot;

using Oryndra.Core;
using Oryndra.Core.Movement;

using SysVec3 = System.Numerics.Vector3;

namespace Oryndra.Game;

/// <summary>
/// The game's root node: it owns the simulation, turns player intent into
/// <see cref="InputFrame"/> values, and hands the resulting
/// <see cref="ActorState"/> to the presentation.
/// </summary>
/// <remarks>
/// <para>
/// No movement rule lives here. This layer captures input, asks the simulation to
/// advance at the fixed step, and lets <see cref="FirstPersonCamera"/> interpolate
/// between the last two ticks.
/// </para>
/// <para>
/// The camera rides the actor rather than driving it: the simulation decides where
/// the actor is, and the head is placed relative to the interpolated feet position.
/// </para>
/// </remarks>
public partial class Main : Node3D
{
    /// <summary>
    /// The line the project prints once its first tick has run.
    /// </summary>
    /// <remarks>
    /// A headless boot has nothing else to show for itself, and it is the one
    /// signal that proves the whole chain came up: project, main scene, script,
    /// simulation. `./verify.sh` greps for it.
    /// </remarks>
    public const string ReadyMessage = "oryndra: simulation running";

    /// <summary>How many ticks one engine frame may catch up on, before time is dropped.</summary>
    private const int MaxTicksPerFrame = 5;

    private const float MouseSensitivity = 0.0022f;
    private const float MaxPitch = 1.53f;

    /// <summary>Where the actor starts, in the greybox's scale, in metres.</summary>
    private static readonly SysVec3 SpawnPoint = new(0f, 0f, 4f);

    private readonly MovementTuning tuning = MovementTuning.Default;

    private GodotCollisionQuery? collision;
    private MovementSimulation simulation = null!;
    private FirstPersonCamera camera = null!;
    private StatusHud hud = null!;
    private ActorState previousState;
    private ActorState currentState;
    private float unspentTime;
    private float yaw;
    private float pitch;

    /// <summary>Gets the number of ticks the simulation has advanced.</summary>
    public int Ticks { get; private set; }

    /// <summary>Gets the actor's state as of the last tick.</summary>
    public ActorState State => simulation.State;

    /// <inheritdoc />
    public override void _Ready()
    {
        PlayerInput.EnsureActions();

        collision = new GodotCollisionQuery(this, tuning.Body);
        simulation = new MovementSimulation(
            collision,
            ActorState.StandingAt(SpawnPoint),
            tuning);
        previousState = simulation.State;
        currentState = simulation.State;

        camera = new FirstPersonCamera { Name = FirstPersonCamera.NodeName };
        AddChild(camera);
        camera.Position = GodotInterop.ToGodot(SpawnPoint) + new Vector3(0f, FirstPersonCamera.EyeHeight, 0f);
        camera.Rotation = new Vector3(0f, Mathf.Pi, 0f);

        hud = new StatusHud { Name = StatusHud.NodeName };
        AddChild(hud);
        hud.Display(currentState, 0f);

        if (DisplayServer.GetName() != "headless")
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        collision?.Dispose();
        collision = null;
    }

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            yaw = Mathf.Wrap(yaw + (motion.Relative.X * MouseSensitivity), -Mathf.Pi, Mathf.Pi);
            pitch = Mathf.Clamp(pitch - (motion.Relative.Y * MouseSensitivity), -MaxPitch, MaxPitch);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            GetTree().Quit();
        }
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        unspentTime += (float)delta;

        var axis = PlayerInput.MoveAxis(tuning);
        var input = new InputFrame
        {
            MoveAxis = new System.Numerics.Vector2(axis.X, axis.Y),
            Yaw = yaw,
            Jump = PlayerInput.Jumped,
        };

        for (var tick = 0; tick < MaxTicksPerFrame && unspentTime >= SimulationClock.FixedDeltaSeconds; tick++)
        {
            unspentTime -= SimulationClock.FixedDeltaSeconds;
            previousState = currentState;
            currentState = simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
            Ticks++;

            if (Ticks == 1)
            {
                GD.Print(ReadyMessage);
            }
        }

        // Never let a long frame leave a backlog for the next one.
        unspentTime = MathF.Min(unspentTime, SimulationClock.FixedDeltaSeconds);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        var alpha = Mathf.Clamp(unspentTime / SimulationClock.FixedDeltaSeconds, 0f, 1f);
        camera.Apply(previousState, currentState, alpha, yaw, pitch, (float)delta);
        hud.Display(currentState, GodotInterop.PlanarLength(currentState.Velocity));
    }
}
