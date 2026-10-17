using Godot;

using Oryndra.Core;
using Oryndra.Core.Movement;

namespace Oryndra.Game;

/// <summary>
/// The game's root node.
/// </summary>
/// <remarks>
/// It owns the simulation and advances it on the fixed tick, catching up when an
/// engine frame ran long. Input capture and the first-person camera arrive with
/// the walking skeleton (see PRD.md, "Walking skeleton: run in first person");
/// everything they need is already on this seam.
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

    private readonly MovementSimulation simulation = new(new FlatGroundWorld());
    private float unspentTime;

    /// <summary>Gets the number of ticks the simulation has advanced.</summary>
    public int Ticks { get; private set; }

    /// <summary>Gets the actor's state as of the last tick.</summary>
    public ActorState State => simulation.State;

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        unspentTime += (float)delta;

        for (var tick = 0; tick < MaxTicksPerFrame && unspentTime >= SimulationClock.FixedDeltaSeconds; tick++)
        {
            unspentTime -= SimulationClock.FixedDeltaSeconds;
            simulation.Advance(InputFrame.Idle, SimulationClock.FixedDeltaSeconds);
            Ticks++;

            if (Ticks == 1)
            {
                GD.Print(ReadyMessage);
            }
        }
    }
}
