# Testing

How a change in oryndra gets proven, and at which level.

`./verify.sh` is the authority: restore, build with warnings as errors, format
check, boot the game headlessly, tests. If it is green locally, CI should agree;
if it is red, nothing else matters. CI runs the very same script
(`.github/workflows/verify.yml`), so this document only has to explain *where* a
test belongs, not how to run one. A second CI job reports a SonarQube Cloud
analysis beside it, and its quality gate binds the merge; that is a gate, not a
level — see [The Sonar report](#the-sonar-report).

## Test levels

| Level | Lives in | Runs against | Proves | Cost |
|---|---|---|---|---|
| Core logic | `tests/Oryndra.Core.Tests` (xUnit) | the engine-agnostic core, in-process | movement rules, determinism, invariants, the frame budget | milliseconds, no engine |
| Game boot | `./verify.sh` | `game/project.godot`, headlessly | the project runs: main scene, C# assembly and the ready line the first tick prints | a couple of seconds, needs Godot |
| Engine runtime | `tests/Oryndra.Game.Tests` (gdUnit4Net) | a headless Godot runtime, the game assembly loaded | the game's nodes come up and tick inside a real Godot, and the seam is wired to the engine | seconds, needs Godot |

Two rules keep that table from growing:

- **Movement behaviour is proven at the core level.** If a rule can only be
  exercised through Godot, it is probably living in the wrong layer: the
  simulation decides, the engine layer converts input and presents state. Adding
  a move means adding core tests, not runtime tests.
- **The Godot levels are for what only the engine can break** — a scene that no
  longer loads, a script whose class no longer matches its file, an assembly
  Godot refuses, an engine API used with wrong units. They stay few, because each
  one costs a Godot boot.

A test project is its own Godot project, with its own `res://`, so it cannot load
the game's `Main.tscn`. That is why booting the *project* is asserted by
`verify.sh` (which greps the ready line `Main` prints on its first tick) and the
runtime test asserts the *assembly and its nodes* instead.

## Invariants and fuzzing

Unit tests say what the simulation does for the inputs we thought of; the harness
in `tests/Oryndra.Core.Tests/Support` says what it must never do for the inputs we
did not. `SimulationFuzzer` drives the public seam with random input sequences and
checks five things after every tick:

1. the state is finite (no NaN, no infinity);
2. the body is never left inside solid geometry;
3. the body never leaves the room's outer bounds;
4. the speed stays within a coarse bound, so nothing feeds itself speed;
5. the actor is never stuck in open space for a whole window of ticks.

Violations are collected instead of thrown, so a failing seed reads as a report.
Adding a seed to `FuzzHarnessTests` is the cheap way to widen the net; a new
invariant belongs in `SimulationFuzzer`'s tick check.

`BoxRoom` is the collision query those tests run against: blocks, resolved one axis
at a time. It is deliberately blunt, and deliberately immune to tunnelling. Being
blunt has a limit worth stating: 1–4 ask the same query that handed the
simulation its position, so they establish that the simulation never puts the body
where the geometry says it cannot be — it does not invent a position, slip through
a wall or lose speed through one. A bug inside `BoxRoom` itself is outside what
the fuzz run can see; the wall and floor behaviour it provides is what the seam
tests in `MovementSimulationTests` assert.

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

## The Sonar report

A second CI job (`sonar`, in `.github/workflows/verify.yml`) reports a SonarQube
Cloud analysis of the repository on every pull request and on `main`. It is not a
fourth test level: it reads what the levels above already prove, and it adds the
one number they cannot produce — how much of the core the core tests touch. It is
also a gate: the job waits for the quality gate and fails with it, and `main`
requires that check, so a failed gate is a pull request that cannot be merged.
`./verify.sh` keeps its own job, unchanged, and remains the command that decides
locally and offline.

Where to read it:

- **On the pull request.** SonarQube Cloud comments with the quality gate, the new
  issues, and a link to the detailed analysis of that pull request. That comment is
  SonarQube Cloud's own, posted through its GitHub app: the `sonar` job uploads the
  analysis, the app decorates the pull request. No comment, no link, and the
  analysis is still there — the binding between the project and the repository is
  the thing to look at.
- **From an agent.** The SonarQube tools read the same project. The key is
  `Domingax_oryndra` in the `domingax` organization: `search_my_sonarqube_projects`
  finds it, `get_project_quality_gate_status` and `search_sonar_issues_in_projects`
  answer for `main`, `get_component_measures` carries coverage, and
  `list_pull_requests` gives the `pullRequest` key that scopes an issue search to a
  pull request's analysis.
- **In a browser.** The link the pull request comment carries.

What the numbers mean here:

- **Coverage is the core's.** It comes from the core tests' coverage report and is
  narrowed to `src/Oryndra.Core`: the Godot layer and the tests are excluded
  (`sonar.coverage.exclusions`), because nothing records line coverage for the game
  and a test is not a thing to be covered. A coverage drop points at the movement
  rules — which is where this table puts the weight.
- **Issues cover the whole repository**, the Godot layer included, the Godot import
  cache and the generated test runner aside (`SonarQube.Analysis.xml`). Reading
  the level that cannot be unit-tested is what the report is for.
- **The quality gate blocks the merge.** The `sonar` job waits for the gate on the
  analysis it just uploaded and fails if the gate fails (`sonar.qualitygate.wait`),
  and the `Sonar gate` ruleset on the default branch requires a pull request
  carrying the `SonarCloud Code Analysis` check that the SonarQube Cloud app posts.
  So `main` takes changes through pull requests, and a fork's pull request — no
  secret, no analysis — cannot be merged at all. What the gate checks lives in the
  SonarQube Cloud UI, not in this repository; why it binds, and what that costs:
  `docs/adr/0010-sonar-analysis-as-a-gate.md`.
- **A red `sonar` job is usually configuration, not code.** The commonest cause is
  *Automatic Analysis* being left on for the SonarQube Cloud project: the free plan
  refuses to combine it with an analysis from CI.
- **The local loop needs no Sonar account.** `./verify.sh` never talks to
  SonarQube Cloud and never asks for a token; the `sonar` job does, from the
  `SONAR_TOKEN` repository secret.
