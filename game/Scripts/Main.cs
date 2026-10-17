using Godot;

using Oryndra.Core;
using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The game's root node.
/// </summary>
/// <remarks>
/// <para>
/// It owns the simulation and advances it on the fixed tick, catching up when an
/// engine frame ran long. Everything else it does is the boundary: the greybox and
/// the eye are built here, a frame's input is turned into an <see cref="InputFrame"/>
/// here, and the resulting <see cref="ActorState"/> is handed to the camera between
/// ticks. No movement rule lives here — the simulation owns those (ADR-0009).
/// </para>
/// <para>
/// The world is built in code rather than in <c>Main.tscn</c> so that the node is
/// complete wherever it is created: the engine-runtime tests build a <c>Main</c>
/// directly, because a test project has its own <c>res://</c> and cannot load this
/// one's scene. The scene stays the one node the project boots.
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

    private readonly MovementTuning tuning = MovementTuning.Default;
    private readonly Greybox greybox = new();
    private readonly MovementSimulation simulation;
    private FirstPersonCamera view = null!;
    private ActorState previous;
    private float unspentTime;
    private double sinceTick;

    /// <summary>Initializes a new instance of the <see cref="Main"/> class.</summary>
    public Main()
    {
        simulation = new MovementSimulation(greybox.Room, ActorState.StandingAt(Greybox.Spawn), tuning);
        previous = simulation.State;
    }

    /// <summary>Gets the number of ticks the simulation has advanced.</summary>
    public int Ticks { get; private set; }

    /// <summary>Gets the actor's state as of the last tick.</summary>
    public ActorState State => simulation.State;

    /// <summary>
    /// Gets or sets what the next frame's input is read from.
    /// </summary>
    /// <remarks>
    /// The device is the default; the engine-runtime tests replace it to drive the
    /// actor without a keyboard, which is the only way to prove the wiring from an
    /// input frame to the simulation and the eye inside a real Godot.
    /// </remarks>
    public Func<float, InputFrame> ReadInput { get; set; } = MovementInput.Capture;

    /// <inheritdoc />
    public override void _Ready()
    {
        AddChild(greybox.Visuals());
        AddChild(Lighting());

        view = new FirstPersonCamera { Name = "View", Tuning = tuning };
        AddChild(view);

        // Place the eye before the first frame is drawn, so a boot that renders
        // before it ticks is not a view from the origin.
        view.Present(previous, State, alpha: 0f, delta: 0d);

        // The mouse is the look, so the pointer has to be out of the way; Escape
        // gives it back (`_UnhandledInput`).
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        unspentTime += (float)delta;

        // One frame's input drives every tick it catches up on, and only the first
        // of them sees the jump: a press is an event, and a burst of catch-up ticks
        // must not turn it into a held key.
        InputFrame input = ReadInput(view.Yaw);
        bool ticked = false;

        for (var tick = 0; tick < MaxTicksPerFrame && unspentTime >= SimulationClock.FixedDeltaSeconds; tick++)
        {
            unspentTime -= SimulationClock.FixedDeltaSeconds;
            previous = simulation.State;
            simulation.Advance(input, SimulationClock.FixedDeltaSeconds);
            input = input with { Jump = false };
            Ticks++;
            ticked = true;

            if (Ticks == 1)
            {
                GD.Print(ReadyMessage);
            }
        }

        // The simulation runs on its own step, so how far the display is past the
        // tick it just drew is render time, not simulation time: that is what the
        // camera interpolates with, and why it is not the same accumulator.
        if (ticked)
        {
            sinceTick = 0d;
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        sinceTick += delta;

        float alpha = Mathf.Clamp((float)(sinceTick / SimulationClock.FixedDeltaSeconds), 0f, 1f);
        view.Present(previous, State, alpha, delta);
    }

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    /// <summary>Builds the sun and the ambient light a greybox needs to be readable.</summary>
    /// <returns>The root of the scene's lighting nodes.</returns>
    private static Node3D Lighting()
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.52f, 0.62f, 0.75f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.45f, 0.5f, 0.6f),
            AmbientLightEnergy = 0.8f,
        };

        var root = new Node3D { Name = "Lighting" };
        root.AddChild(new WorldEnvironment { Environment = environment });
        root.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55f, -35f, 0f),
            LightEnergy = 1.1f,
        });

        return root;
    }
}
