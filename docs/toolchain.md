# Toolchain

Versions of the tools this project builds and runs with, so CI, agents and contributors agree on the same ones.

| Tool | Version | Command | Notes |
|---|---|---|---|
| .NET SDK | 10.0.113 | `dotnet` | `/usr/share/dotnet/sdk` (Arch `dotnet-sdk`); pinned in `global.json` |
| Godot | 4.7.2.stable.mono | `godot-mono` | Arch `godot-mono`. **The binary is `godot-mono`, not `godot`.** CI downloads the same version and passes it as `GODOT_BIN`. |
| gdUnit4Net API | 5.1.0-rc5 | `gdUnit4.api` (NuGet) | the C# side of the Godot test framework; latest published, and the one proven against Godot 4.7 |
| gdUnit4Net adapter | 3.1.1 | `gdUnit4.test.adapter` (NuGet) | the VSTest adapter `dotnet test` goes through |
| gdUnit4 analyzers | 1.0.0 | `gdUnit4.analyzers` (NuGet) | static analysis of test code |
| GitHub CLI | 2.96.0 | `gh` | used for the issue tracker |
| Git | system | `git` | |
| Blender | not installed | `blender` | planned; see `docs/adr/0008-asset-pipeline.md` |

## Notes

- Godot is the **.NET edition** (a.k.a. "mono"). Plain GDScript builds cannot run C#.
- The engine targets the .NET runtime installed above; the C# core is a separate, engine-agnostic library (see the harness ticket).
- Keep this file in step whenever a tool is upgraded; the CI version of Godot must match the local one.

## Target framework

Every project targets **net10.0** (agreed in `Directory.Build.props`), built by the
SDK above and run on the .NET runtime it ships.

The two Godot projects state `net10.0` in their own project files as well, and
that is not redundant: a Godot project that does not name its framework gets
rewritten to `net8.0` by the tool that touches it first — the Godot editor on
import, or the gdUnit4 adapter on a test run — leaving a `.csproj.old` behind.
Naming the framework keeps the tooling from disagreeing with the repository.

gdUnit4Net's own documentation lists net8.0 and net9.0 as supported; its adapter
accepts the explicitly pinned net10.0 and runs against Godot 4.7.2 (that is what
CI proves). If a gdUnit4 upgrade starts insisting on an older framework, this is
the decision to revisit.
