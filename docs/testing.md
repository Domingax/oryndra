# Testing

How a change in oryndra gets proven, and at which level.

`./verify.sh` is the authority: restore, build with warnings as errors, format
check, tests. If it is green locally, CI should agree; if it is red, nothing else
matters. CI runs the very same script (`.github/workflows/verify.yml`), so this
document only has to explain *where* a test belongs, not how to run one.

## Test levels

| Level | Lives in | Runs against | Proves | Cost |
|---|---|---|---|---|
| Core logic | `tests/Oryndra.Core.Tests` (xUnit) | the engine-agnostic core, in-process | movement rules, determinism, invariants, the frame budget | milliseconds, no engine |
| Engine runtime | `tests/Oryndra.Game.Tests` (gdUnit4Net) | a headless Godot runtime | the project boots, script bindings work, the simulation seam is wired to the engine | seconds, needs Godot 4.7.2 |

Two rules keep that table from growing:

- **Movement behaviour is proven at the core level.** If a rule can only be
  exercised through Godot, it is probably living in the wrong layer: the
  simulation is the thing that decides, the engine layer only converts input and
  presents state. Adding a move means adding core tests, not runtime tests.
- **The engine runtime level is for what only the engine can break** — a scene
  that no longer loads, a script whose class no longer matches its file, an
  assembly Godot refuses, an engine API used with wrong units. Its tests stay
  few, because each one costs a Godot boot.

## Invariants and fuzzing

Unit tests say what the simulation does for the inputs we thought of; the
harness in `tests/Oryndra.Core.Tests/Support` says what it must never do for the
inputs we did not. `SimulationFuzzer` drives the public seam with random input
sequences and checks four invariants after every tick:

1. the state is finite (no NaN, no infinity);
2. the body is never left inside solid geometry;
3. the body never escapes the world's outer bounds;
4. the actor is never stuck in open space for a whole window of ticks.

Violations are collected instead of thrown, so a failing seed reads as a report.
Adding a seed to `FuzzHarnessTests` is the cheap way to widen the net; a new
invariant belongs in `SimulationFuzzer.Check`.

`BoxWorld` is the world those tests run in: blocks, resolved one axis at a time.
It is deliberately blunt, and deliberately immune to tunnelling — when a fuzz run
asks "did the actor stay outside the geometry?", the world it asks is not the
code under test.

## Determinism

Ghosts replay recorded input frames, so a run that drifts by one bit no longer
matches its recording. `DeterminismTests` therefore compares states bit for bit
(plain equality would hide a NaN, and treat `-0.0` as `0.0`), across two runs of
the same seed, across a reset-and-replay, and across two simulations interleaved.
Anything the simulation reads that is not its input frame — a clock, a random
source, a static cache — shows up as a failure there.

Nothing about the simulation may depend on the render rate; it advances on
`SimulationClock.FixedDeltaSeconds` and presentation interpolates between ticks.

## The frame budget

`FrameTimeBudgetTests` fails when a tick costs more than the budget it documents
(0.2 ms, about 3.6 % of a 60 FPS frame for three co-op actors), and when a tick
gets dramatically more expensive as a run goes on. It is not a benchmark: it is
there to catch the regression that no unit test sees, such as an allocation or a
geometry search added to the per-tick path.

## Godot-runtime tests

The Godot test project is itself a Godot project (`project.godot` next to its
`.csproj`), because gdUnit4Net runs the tests inside a headless Godot it launches
through its VSTest adapter. Two consequences worth knowing:

- it needs `GODOT_BIN` in the environment, pointing at the .NET build of Godot;
- its `TargetFramework` is pinned in the project file, because the adapter
  rewrites a Godot project that does not name its own framework to net8.0.

The adapter also writes `gdunit4_testadapter_v*/GdUnit4TestRunnerScene.cs` into
that project on every run. It is generated, so it is git-ignored and declared as
generated in `.editorconfig`: the format check and the analyzers step over it,
and no cleanup step is needed to keep the tree honest.
