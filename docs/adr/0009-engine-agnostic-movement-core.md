# Engine-agnostic movement core

The movement rules live in a plain C# library (`src/Oryndra.Core`) that references no Godot type, behind a single seam: the simulation asks the geometry to **move a body** (`ICollisionQuery.MoveBody(position, delta, body) -> BodyMoveResult`), and it is a plain object with no clock, no random source and no frame rate (`MovementSimulation.Advance(InputFrame, dt) -> ActorState`). The Godot project only converts input, implements the collision query, and presents the state.

ADR-0002 chose C# for a **more robust harness for agents**. This is where that buys something: a move can be unit-tested, fuzzed, replayed and re-tuned without booting an engine, and the whole loop runs headlessly in CI.

## Considered and rejected

- **Movement in Godot nodes** (`CharacterBody3D`, `MoveAndSlide`): the fastest way to something playable, but every rule would then need a running engine to test, determinism — and therefore Ghost replay — would inherit the engine's internals and the frame rate, and the fuzz harness could not exist.
- **A core that references Godot types** (`Godot.Vector3`, the physics server): fewer conversions at the boundary, but the core stops being testable without the engine, which is the property the seam exists to protect.
- **A richer first seam** (probe results, surface kinds, ladder and rope queries): postponed. One swept move is enough for the trivial move; the seam grows when a real move needs it, following the PRD's *probe → decide → animate* model.

## Consequences

- The engine layer carries the conversions: `InputFrame` from input devices, `System.Numerics.Vector3` against `Godot.Vector3`, and Godot physics behind `ICollisionQuery`. That boundary is where engine-unit mismatches get caught.
- Movement behaviour is proven at the core level, in xUnit, with no engine. The Godot-runtime level exists only for what the engine can break — see `docs/testing.md` for the levels and what each one is allowed to claim.
- The Godot project lives in `game/` rather than at the repository root: the engine scans every `.cs` file under its project directory, so a root project would pull the core and the tests into the editor, its compilation and a `.uid` file per source. Godot opens `game/`; the rest of the repository is ordinary C#.
- The simulation advances on a fixed 60 Hz step (`SimulationClock`) and presentation interpolates between ticks. Nothing in the core may read a clock, a random source or a frame rate.
- A test asserts that the core assembly references no engine assembly, so the boundary cannot rot silently.

## Evidence

`PRD.md` ("Movement — the heart of the game": data-driven moves, the Mirror's Edge reference model), ADR-0002, and the harness itself: `./verify.sh` builds the core, the Godot project and both test levels, and fails if the core ever grows an engine dependency.
