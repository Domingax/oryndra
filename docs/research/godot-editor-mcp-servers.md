# Editor-driving MCP servers: a working POC

Research carried out as the follow-up of `docs/adr/0011-godot-mcp-observability.md`,
which answered the *running game* half of agent observability (a zero-footprint
server, no editor needed). This note answers the other half: an agent with hands
in the **live editor** — authoring scenes, animations and tiles while a human
watches — and it records two proofs that were actually run rather than a reading
list.

**Nothing is wired into the repository.** `opencode.json` still starts exactly one
server (`docs/agents/godot-mcp.md`), neither addon is committed, and this note is
the trace: what each candidate does, what it costs, and what was measured on this
machine on 2026-10-17.

## Question asked

Which server should give an agent hands in the Godot editor, and what does each
one cost this repository?

## Candidates analysed

| Server | Licence | State on 2026-10-17 | Shape |
|---|---|---|---|
| **Swallowtail** — `regiellis/godot-mcp-go` | MIT | v1.2.0 (2026-10-09), 68 ★, one maintainer, created 2026-07 | Go CLI (337 commands) + GDScript addon; MCP optional, stdio or the editor's HTTP endpoint |
| **Godot AI** — `hi-godot/godot-ai` | MIT | v4.3.0 (2026-10-03), 3033 ★, several maintainers, very active | GDScript addon + Python backend through `uvx`; MCP over stdio (`godot-ai attach`) |
| Godot MCP Runtime — `Erodenn/godot-mcp-runtime` | MIT | the current choice (ADR-0011) | `npx`, no addon; drives a game **without** an editor |

Both candidates were taken because they drive the editor, which is what ADR-0011
deliberately did not buy. Both install an addon in `game/`, which is the cost that
ADR is about.

## Method

- A **full copy of the repository** per candidate under `/tmp`, never the working
  tree: `game/project.godot`, `game/addons/` and the C# build were touched only in
  the copies, and both copies were deleted afterwards.
- **Swallowtail 1.2.0**: `swallowtail_1.2.0_linux_amd64.tar.gz`, its SHA-256
  checked against the release's `SHA256SUMS.txt`.
- **Godot AI 4.3.0**: the canonical signed archive `godot-ai-v4-plugin.zip`; its
  SHA-256 and size checked against `godot-ai-v4-plugin.manifest.json`, then all
  **313 files** checked one by one against the manifest inventory. The manifest's
  own signature (minisign) was **not** verified.
- **Godot 4.7.2 mono** (this repository's pinned engine), editor opened windowed,
  and the MCP clients driven over stdio the way OpenCode would.
- Telemetry was disabled for the Godot AI run (`GODOT_AI_DISABLE_TELEMETRY=true`).
- `./verify.sh` was also run in the Swallowtail copy, with the addon enabled, to
  see whether the addon breaks the gate.

## Findings: Swallowtail

Everything below ran against `res://Scenes/Main.tscn` and a scratch scene, through
the CLI, from inside the copied project.

| Gesture | Result |
|---|---|
| `doctor` | detects the project, the addon, the running editor, its port, and the .NET SDK |
| `scene tree` | the **live edited scene**: `Main`, `Node3D`, **script `res://Scripts/Main.cs`** — C# is seen |
| `scene create` + `node add` (CSGBox3D) + `node set` (size, position, `use_collision`) | a visible block, four commands |
| `scene validate` | 0 issue |
| `scene play` → `runtime tree` | the **running game's** tree |
| `runtime set` on a running node | applied and read back |
| `runtime screenshot` | PNG 1152×648, verified by eye (the block is rendered) |
| `animation create` + `add_track` + `set_keyframe` ×2 + `get_info` | animation "Hop", a `Block:position` track, keys at 0 s and 0.3 s, read back |
| `csharp build` after breaking a script on purpose | `CS1525`, `Scripts/Main.cs:34:30`, `Invalid expression term ';'` — **structured**, with file, line, column |
| a command called with a wrong flag | error listing the declared params and suggesting the right name |

Frictions: none that blocked a gesture. Parameter **names** are the discovery cost
(`--node-path`, `--track-index`), and the CLI's own `--help` settles them; the
launcher looks for a `godot` binary on `PATH`, which this machine does not have
(we have `godot-mono`), so a wrapper would be needed for `launch` — the same
wrapper `verify.sh` already implies for the current server.

## Findings: Godot AI

The same gestures, through `uvx … godot-ai attach` over stdio.

| Gesture | Result |
|---|---|
| `session_manage` | the live session: editor 4.7.2, current scene, plugin and server versions |
| `scene_get_hierarchy` | the scene's nodes (name, type, path, child count — no script field) |
| `node_create` | a `Camera3D` (`undoable: true`) |
| `editor_screenshot` | the **editor viewport**: PNG 640×268, verified by eye |
| `animation_create` | animation "Hop" created, `undoable: true` |
| `script_manage read` | `Main.cs` read as **text** (documented: C# is text-only) |
| `project_run` → `logs_read game` | `oryndra: simulation running` — the ready line, once the game helper is live |
| `editor_screenshot {"source": "game"}` | the running game, `frames_drawn: 10` |
| `editor_state` | full session state: `status: live`, `helper_live: true` |

Frictions, which are the substance of the comparison:

- Parameter shapes are not guessable: `path` (not `node_path`), and a position must
  be `{"x":7,"y":6,"z":10}` — the Godot literal `Vector3(7,6,10)` is **refused**
  (`Cannot coerce String to Vector3`), where Swallowtail coerces it.
- Wrong-parameter errors come from Pydantic (`Unexpected keyword argument`) instead
  of naming the accepted parameters.
- The game log **lags the game**: the first `logs_read(source="game")` returned
  zero lines; the helper went live about 13 s later and then returned the ready
  line. A caller has to poll rather than read once.
- The addon's dock tried to parse the **global** `~/.config/opencode/opencode.jsonc`
  and warned about its comments (JSONC is not handled). Harmless here — the
  repository's project config is strict JSON — but it is friction for an OpenCode
  user pressing *Configure*.
- The editor log repeated `readiness -> importing/ready` about 140 times in 40 s.
- Telemetry is on by default, with an environment opt-out.

## Measured costs

| | Godot MCP Runtime (current) | Swallowtail | Godot AI |
|---|---|---|---|
| Addon committed in `game/` | **none** | 257 files, 2.6 MB uncompressed | 313 files, 2.2 MB |
| Written into `project.godot` | nothing (transient autoload while a game runs) | plugin enabled + 2 autoloads (`MCPGameInspector`, `MCPGameInput`) | plugin enabled |
| Outside the repository | none | one 16 MB Go binary | Python backend (73 packages on first `uvx`), `~/.config/godot-ai` |
| MCP tools / schema cost | 39 tools | **1 tool (`godot_run`) — 2 000 bytes, ~500 tokens** with `--typed=false`; 338 tools, 234 493 bytes, ~59 000 tokens typed | 47 tools, 109 787 bytes, ~27 000 tokens |
| Needs an editor open | no | yes | yes |
| C# | — | `csharp build` → structured diagnostics | text read/write only, no compiler diagnostics |
| Usable with no agent | no | yes (the CLI, 337 commands) | no |
| Telemetry | none | none | on by default |

`./verify.sh` stayed green in the Swallowtail copy with the addon enabled and its
two autoloads running: 28 core tests, 1 runtime test, the headless boot included.
The tool does not break the gate; it does run inside every boot.

The two published context numbers were reproduced here within a few percent, and
the single-tool mode was exercised: `godot_run {"method": "scene.tree"}` reached
the live editor through one tool.

## What this does not settle

- No export, no `.pck` scan, no shipping story: the addon's own "before you ship"
  guidance (disable the plugin, exclude `addons/<name>/*` from every preset, then
  grep the `.pck`) was read, not run — this repository has no export yet.
- No input simulation, no playtest session, no multi-session or remote use.
- No measurement of what either addon costs the editor's own frame time while it
  runs, beyond the readiness churn noted above.
- Godot AI's manifest signature was not verified, only the archive against the
  inventory.

## Status: studied, not wired

Neither addon is in the repository. When the greybox and the movement animations
arrive — the point where the gestures above stop being hypothetical — the
reasonable starting position is:

- **Swallowtail**, in `--typed=false`, for the C# diagnostics, the literals that
  coerce, the ~500-token surface, and a CLI a human can use too;
- **Godot AI** if maturity and a wider curated surface outweigh a Python backend,
  a ~27 000-token tool list, and telemetry to switch off.

Whichever it is, it costs a committed addon in `game/` and two (Swallowtail) or no
extra autoloads — which is a decision of its own, not a bump of ADR-0011, and one
that should be taken then, in writing, with the rule that **only one server drives
the project at a time**.

## Reproducing the study

```bash
# A copy of the repository, never the working tree.
rsync -a --exclude '.git' --exclude '.godot' --exclude 'bin' --exclude 'obj' . /tmp/poc/

# Swallowtail 1.2.0
curl -fsSLO https://github.com/regiellis/godot-mcp-go/releases/download/v1.2.0/swallowtail_1.2.0_linux_amd64.tar.gz
curl -fsSLO https://github.com/regiellis/godot-mcp-go/releases/download/v1.2.0/SHA256SUMS.txt
tar xzf swallowtail_1.2.0_linux_amd64.tar.gz
./swallowtail install --project /tmp/poc/game --enable --skill=false

# Godot AI 4.3.0: the canonical archive, checked against the manifest inventory,
# then extracted into the project (extraction root is the project, not addons/).
curl -fsSLO https://github.com/hi-godot/godot-ai/releases/download/v4.3.0/godot-ai-v4-plugin.zip
curl -fsSLO https://github.com/hi-godot/godot-ai/releases/download/v4.3.0/godot-ai-v4-plugin.manifest.json

# Both then need the editor open with the plugin enabled:
godot-mono --path /tmp/poc/game --editor

# Swallowtail drives the editor through its CLI (the editor writes
# .godot/swallowtail.json, from which the port is discovered).
# Godot AI drives it through the stdio bridge:
uvx --isolated --no-config --no-env-file --no-sources --no-build \
  --index https://pypi.org/simple --default-index https://pypi.org/simple \
  --find-links https://pypi.org/simple/godot-ai/ --index-strategy first-index \
  --keyring-provider disabled --link-mode copy \
  --from godot-ai==4.3.0 godot-ai attach --disable-telemetry
```
