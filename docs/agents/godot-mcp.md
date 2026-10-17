# Godot MCP: watching a running game

How an agent gets **eyes** on oryndra while it runs, and what that is worth:
observability, never proof. The decision behind it is ADR
`0011-godot-mcp-observability.md`; the versions are in `docs/toolchain.md`.

`./verify.sh` remains the authority. Nothing a server here reports decides
whether a change is acceptable: it is how an agent looks at the game between two
runs of the gate, instead of asking a human to paste a screenshot back.

## What runs, and where

| Piece | Path | Role |
|---|---|---|
| Server entry | `opencode.json` | OpenCode starts the server with the session, over stdio |
| Launcher | `scripts/godot-mcp.sh` | Resolves `godot-mono` (or `GODOT_BIN`), pins the server version, hands over stdio |
| Runtime artifacts | `game/.mcp/` | Bridge scripts, policies and screenshots of a run; ignored by `game/.gitignore` |

The server is **Godot MCP Runtime** 3.8.1 (`npx godot-mcp-runtime`), by Erodenn,
MIT. It installs **no addon** in the project: when it launches the game it
injects a temporary autoload, and on stop it removes it and restores
`project.godot`. It needs Node.js and Godot — both already on the machine — and
it talks to nothing but the project directory: no account, no cloud. Its one
network access is the npm registry, on the first run, for the pinned package.

## How to start and stop it

- **Start**: nothing to do. OpenCode connects it with the session; `opencode mcp
  list` shows whether it is connected, and `/mcps` connects or disconnects one by
  hand. It does **not** launch a game until asked.
- **Start the game**: `run_project` (the agent asks; the server prompts for
  confirmation first — see below). `background: true` hides the window off-screen
  while keeping screenshots and programmatic input working, which is the mode to
  use when nobody wants a window in the way.
- **Stop the game**: `stop_project`. The bridge is removed with it. A game left
  running from an earlier session is what `attach_project` is for; `detach_project`
  drops the bridge without touching that process. Note the trade-off: an attached
  game loses the debug output — `get_debug_output` only has the stream of a game
  the server launched itself — so attach for the scene tree, screenshot and input,
  and `run_project` when the log matters.
- **Stop the server**: disconnect it in `/mcps`, or remove its entry from
  `opencode.json`. No build, test or CI step notices either way.

Launching a project runs arbitrary code, so the server asks for confirmation
through MCP elicitation before `run_project`. OpenCode shows it as a form: a
human answers it, or an agent that is told to run unattended answers it in
advance. Do not switch the server's security gate off to avoid the prompt; the
prompt is the point.

## What it exposes

39 tools: project discovery and metadata, headless scene and script editing,
validation, profiling, and — the reason this repository wired it up — the
**runtime** family. Only the runtime half is used here; editing scenes or
writing `.gd` goes through files and `dotnet` like any other change, reviewed
like any other change.

The bar this ticket set, and the tool that answers it:

| Capability | Tool | What comes back |
|---|---|---|
| Read the running scene tree | `run_script` (full `SceneTree` access) | the live tree: node paths, classes, the script each node runs, any property |
| Read the error/debug log | `get_debug_output` | the launched project's stdout/stderr, including after a crash — only for a game the server launched: an attached game has no captured stream |
| Capture a screenshot | `take_screenshot` | a PNG of the real viewport, saved under `game/.mcp/` |
| Simulate input | `simulate_input` | keys, mouse, Godot actions, text — each with what it actually hit |

Also useful while looking at a game: `get_ui_elements`, `check_project`
(Godot version and session status — the cheapest "is it alive?"), `run_project`,
`attach_project`, `stop_project`, `profile_project`.

## Proving it works on this machine

The tools, in the order that answers "can an agent see this game?" — the
sequence was run against `game/` when this was wired up:

```text
check_project    { projectPath: ".../game" }        → godotVersion 4.7.2.stable.mono
run_project      { projectPath: ".../game", background: true }
get_debug_output {}                                 → "oryndra: simulation running"
run_script       { script: <reads scene_tree.root> } → /root/Main, res://Scripts/Main.cs
simulate_input   { actions: [{ type: "key", key: "W" }] }
take_screenshot  { responseMode: "path_only" }       → 1152×648 PNG under game/.mcp/
stop_project     {}
```

At this stage of the project the main scene is a bare `Node3D`, so a screenshot
is mostly empty and there is no UI to click. That is the scene's business, not
the server's: the capabilities are what this ticket proves, and they will have
more to look at as Maps and the first-person camera land.

## Rules

- **Never wire this into the gate.** `./verify.sh` does not start it, CI does not
  install it, and no test may require it: the local loop has to stay offline and
  valid on a machine that has never heard of it.
- **Never treat its output as a test.** A screenshot or a scene dump is evidence
  for a person, and a way for an agent to iterate. The levels and what each may
  claim are in `docs/testing.md`; a human playing is still how feel is judged.
- **Keep it out of the game.** No addon under `game/`, no NuGet package, no
  change to `Oryndra.Game.csproj` — the only thing it puts there is a
  `.gitignore` for its own artifact directory. If a future server wants more, it
  is a new decision, not a bump of this one.
