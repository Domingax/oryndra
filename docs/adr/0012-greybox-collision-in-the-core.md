# Greybox collision: the core's block query

The greybox the walking skeleton is played in is built from blocks (`Box`), and the collision query the simulation moves through is the core's `BoxRoom` — the same resolver the engine-free tests run against. The Godot layer decides **where the blocks are** and draws them; it does not decide how a body meets them. A block is a solid and a mesh at once (`game/Scripts/Greybox.cs`), so a surface cannot be visible and passable.

This is a deliberate exception to the sentence in ADR-0009 that has the engine layer carry "Godot physics behind `ICollisionQuery`". The seam does not change: the simulation still asks one swept move, and a Map's own geometry (ticket #5) is still free to answer it with Godot physics.

## Considered and rejected

- **Godot physics behind `ICollisionQuery`** (`PhysicsDirectSpaceState3D` shape casts, one axis at a time). It is the shape ADR-0009 sketches, and it would let a Map's real colliders answer the seam directly. Rejected for the greybox: a box body resting exactly on a box floor makes the *contact* case the interesting one — margins, and whether a sweep that starts in contact slides or sticks — and that behaviour lives in the engine, where no core test can see it. The tests are what define the movement rules; a greybox that meets them differently produces "the game feels broken" reports that no level in `docs/testing.md` can attribute.
- **A second resolver in the game layer.** Two sweeps for one seam — one proven, one shipping — drift apart, and the drift is invisible until a player feels it.
- **Leaving `BoxRoom` in `tests/`.** The game would then depend on a test project or duplicate it. It moved into `src/Oryndra.Core/Movement` instead: it is engine-free geometry, not movement rules, so the engine-agnostic core is where it belongs.

## Consequences

- The core ships a geometry type as well as the seam. Its lines are covered like the rest of the core: the fuzz harness drives `MoveBody` on every tick, and `MovementSimulationTests`/`BoxRoomTests` pin the wall, floor and bound behaviour.
- The Godot layer's collision work is content — the block list — and its evidence is a different level: the engine-runtime tests assert the actor comes up grounded on the greybox, and a play-through is how the space is judged (docs/testing.md).
- Replacing the greybox with a Map changes which `ICollisionQuery` the simulation is built with, and nothing else about it.

## Evidence

The walking skeleton itself: `src/Oryndra.Core/Movement/BoxRoom.cs`, `game/Scripts/Greybox.cs`, `tests/Oryndra.Core.Tests/FuzzHarnessTests.cs`, and the levels table in `docs/testing.md`.
