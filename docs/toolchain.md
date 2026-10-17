# Toolchain

Versions of the tools this project builds and runs with, so CI, agents and contributors agree on the same ones.

| Tool | Version | Command | Notes |
|---|---|---|---|
| .NET SDK | 10.0.113 | `dotnet` | `/usr/share/dotnet/sdk` (Arch `dotnet-sdk`) |
| Godot | 4.7.2.stable.mono | `godot-mono` | Arch `godot-mono`. **The binary is `godot-mono`, not `godot`.** |
| GitHub CLI | 2.96.0 | `gh` | used for the issue tracker |
| Git | system | `git` | |
| Blender | not installed | `blender` | planned; see `docs/adr/0008-asset-pipeline.md` |

## Notes

- Godot is the **.NET edition** (a.k.a. "mono"). Plain GDScript builds cannot run C#.
- The engine targets the .NET runtime installed above; the C# core is a separate, engine-agnostic library (see the harness ticket).
- Keep this file in step whenever a tool is upgraded; the CI version of Godot must match the local one.
