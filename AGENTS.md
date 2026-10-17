# oryndra

A 3D parkour game (inspired by Mirror's Edge). Engine: **Godot**, language: **C# (.NET)**.
The product requirements are described in `PRD.md`.

## Layout

| Path | What it is |
|---|---|
| `src/Oryndra.Core` | Engine-agnostic C# library: the movement simulation and its seam. References no Godot. |
| `game/` | The Godot 4.7 (.NET) project. Owns input, presentation and the world query; no movement rules. |
| `tests/Oryndra.Core.Tests` | xUnit tests of the core: no engine, no display. |
| `tests/Oryndra.Game.Tests` | gdUnit4Net tests that run inside a headless Godot. |

## Build, test and format

`./verify.sh` is the gate: restore, build with warnings as errors, format check,
tests. Run it before calling any change done. CI runs exactly the same script.

```bash
./verify.sh                                       # everything, using godot-mono from the PATH
GODOT_BIN=/path/to/godot ./verify.sh              # when it is somewhere else
```

The individual steps, when the whole gate is too slow:

```bash
dotnet build oryndra.sln                                        # build
dotnet format oryndra.sln --verify-no-changes                   # check formatting
dotnet format oryndra.sln                                       # apply formatting
dotnet test tests/Oryndra.Core.Tests/Oryndra.Core.Tests.csproj  # core tests (fast)
GODOT_BIN=$(command -v godot-mono) dotnet test tests/Oryndra.Game.Tests/Oryndra.Game.Tests.csproj \
  --settings tests/Oryndra.Game.Tests/.runsettings              # Godot-runtime tests
```

Warnings are errors, analyzers and nullable checking are on, and `.editorconfig`
carries the style rules: a code change that is only "nearly" formatted fails the
build, not the reviewer's patience. Which test belongs at which level, and what
the fuzz harness guarantees: `docs/testing.md`. Tool versions: `docs/toolchain.md`.

## Agent skills

### Issue tracker

Issues and specs live as GitHub issues on `Domingax/oryndra`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five roles (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `GLOSSARY.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
