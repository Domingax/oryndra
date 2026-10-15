# Mirror's Edge Catalyst — gameplay and content vocabulary

Research carried out to inform oryndra's PRD (see `PRD.md` and `GLOSSARY.md`).
The goal is **not** to copy the game, but to name precisely the systems a parkour game must decide — so that `/grill-with-docs` has concrete questions to ask.

## Question asked

Which *named* systems structure Mirror's Edge Catalyst's parkour, and which are reusable as design vocabulary for oryndra?

## Target analysed

| Item | Value |
|---|---|
| Game | Mirror's Edge Catalyst (Windows, retail Steam) |
| Path | `/data/SteamLibrary/steamapps/common/Mirrors Edge Catalyst/` |
| Engine | Frostbite (EA DICE) |
| Main binary | `MirrorsEdgeCatalyst.exe` — PE, x86-64, 87,786,928 bytes |
| Binary SHA-256 | `e9d76463766973964a97cd63bd64c1191a317ec6ed6753d82b36f4ba0da45136` |
| Data | 23 GB under `Data/Win32/gameconfigurations/*installpackage/` |

## Method (and what each route yielded)

Two routes were tried, with very different results:

1. **`rea` MCP on the binary** — `open_binary` succeeded (provider **Ghidra 12.1.4**, PE x86-64 target bound by SHA-256). Deep analysis **failed by timeout** (`provider_timeout`, 330 s ceiling): importing an 87 MB PE exceeds the budget. The lightweight alternative `inspect_binary_layout` is unavailable (the `pwntools-elf` provider requires `REA_PWNTOOLS_PYTHON`, not configured) and only targets ELF anyway.
   → **Conclusion: REA covers the code well, not the data.** There is no Frostbite provider for `.sb`/`.toc` containers.

2. **Direct reading of the Frostbite indexes** — the index files (`layout.toc`, `globals.toc`, and the `.sb` headers) store their asset paths **in cleartext**, while the payloads (`.cas`, `.sb`) are compressed/encrypted. A string extraction over the `.toc`/`.sb` files yields a list of **49,081 distinct asset names** that document the game's vocabulary.

   Reproducible:
   ```sh
   cd "/data/SteamLibrary/steamapps/common/Mirrors Edge Catalyst/Data"
   strings -n 6 Win32/*.sb Win32/*.toc Win32/levels/sp/*/*.sb Win32/levels/sp/*/*.toc \
     | grep -aoE '[a-z][a-z0-9_/.-]{5,}' | grep -E '[a-z]{4,}' | sort -u
   ```

> The facts below are **observations** drawn from those names (authority: shipped artifact). Design readings are marked **inference**.

## What the structure reveals

### 1. The traversal kit: `runnerobject`

This is the heart of the system. Mirror's Edge Catalyst does not code "parkour": it places **typed runner objects**, each carrying its own movement semantics. Observed taxonomy (`objects/props/runnerobject/`):

| Object | Occurrences | Role |
|---|---|---|
| `pipe_climbable` | 173 | Climbable pipes (variants `_32` → `_2048`, elbows `c45`/`c90`, `t128`) |
| `runnerboard` | 31 | Slanted boards/ramps (`runnerboardlarge`, `runnerramp`) |
| `dasharrows` | 23 | Course guidance arrows, **including `checkpointcircle`** |
| `swingpole01` / `swingpole02` | 18 | Balance and swing bars (`balanceandswing`) |
| `springboards` | 13 | Springboards |
| `swingaroundpipe01` | 5 | Swing-around pipes |
| `ziplinebase` | 3 | Zipline start |

**Inference:** this is exactly oryndra's **Segment** notion in `GLOSSARY.md` ("a prefabricated geometry block with entry/exit connectors"). ME Catalyst validates that decomposition: traversal is an **assembly of typed objects with normalised dimensions**, not free-form geometry. The size suffixes (`_128`, `_256`, `_416`, `_512`…) show that dimensions are the true unit of level design.

### 2. The `gameplay/movement/` subtree

Movement has its own library, organised by **verb**:

```
gameplay/movement/climbing/ladders      (pf_climbladder_384 / 512 / 768 / 1536)
gameplay/movement/climbing/newpipes     (pf_climbpipe_512 … 2048)
gameplay/movement/climbing/pipes        (pf_pipeclimb_1024b_noexit_startandend)
gameplay/movement/swing                 (pf_swingbarsimple01, pf_swingbalancebarflag01)
gameplay/movement/swingaround           (pf_swingaroundpipe01/02_short)
gameplay/movement/springboard           (pf_springboardgeneric, pf_springboardairduct512)
gameplay/movement/balance/pipes
```

Two attributes are carried by the object's **name**, not by a separate flag:

- `_noexit` vs `_startandend`: whether you can exit the pipe at both ends;
- `_groupable_gen4` vs `_nongroupable_autogen_gen4`: whether the object can be **grouped** in the authoring pipeline (`gen4` = geometry generation).

### 3. Runner Vision is a *duplicated* art layer, not a shader

The `_mesh_runnersvision` suffix appears on a large share of traversable props (e.g. `climbpipe_1024_cluster_mesh_runnersvision`, `pipe02_416_mesh_runnersvision`, `runnerboardlarge_01_mesh_runnersvision`).

**Inference (strong):** the "affordance" (what is traversable is visible) is not derived at runtime — it is **authored upfront**, as a distinct mesh variant per object. This is an expensive production choice but perfectly readable and deterministic.

The game even exposes Runner Vision as an **unlockable cosmetic** (`characters/customizations/projection/runvis/unlock_faith_projection_runvis`), alongside `ghost`, `chaoslines`, `halftone`, `rain`, `scanline`, `streams` — "projections" are interchangeable visual filters. `settings/profile/…` and `ui/art/map/progression/icons/gearbeatlink` confirm it is a system in its own right.

### 4. The course system: created **by players**

```
gameplay/social/pf_l_createtimetrial
gameplay/social/timetrial/pf_timetrialcheckpoint
gameplay/social/timetrial/pf_timetrialmarker
```

And the associated feedback, in the sound bank:
`me_sound/ui/ui_timetrial_start`, `…_checkpoints`, `…_edit_marker_01/02`, `…_complete`, `…_complete_fail`, `…_cancel`.

**Inference:** the timer is not a separate mode, it is an **in-game Course editor**: the player places markers on an existing Map, and the game turns them into a Course. This is Catalyst's "Beat" model — and it is directly oryndra's **Course** in `GLOSSARY.md` ("a timed route laid out on a Map").

### 5. The MAG rope, the only *unlockable* capability

`characters/faith/armgear/magrope/` carries `magrope_pickup_1_skin`, `magrope_pickup_2_skin`, `magrope_pickup_3_skin` — **three upgrade tiers** — plus `magrope_skin` (cosmetic), `magrope_ske` (animation skeleton) and `animations/prefabs/misc/pf_lookatmagrope`.

**Inference:** the grappling hook is treated as an **equipment piece on a limb** (`armgear`), with a gameplay progression axis (tiers) and a cosmetic axis (skins), kept separate. oryndra **rejected** move gating (ADR `0005-all-moves-unlocked.md`); the reusable part here is only the tier/cosmetic split.

### 6. Level decomposition: the `_flow` layers

Levels declare layers named `*_flow`: `backinthegame_flow`, `belikewater_flow`, `billboard_flow`, `securityhubdowntown1_flow`… Alongside, a `*approach_flow` per level (the approach to the entry point).

> ⚠️ **Vocabulary collision to settle** — `GLOSSARY.md` defines **Flow** as the continuous movement that maintains speed. Frostbite calls `_flow` a **level content layer**. Two meanings for one word in the same project: to be resolved via `/domain-modeling`.

### 7. Content families beyond movement

`gameplay/` also covers `ai` (451), `cityalert` (392), `collectibles` (284), `mission` (114), `progression` (95), `social` (69), `logic` (70), `zipline_01` (58), `rvo` (52 — *reciprocal velocity obstacles*, crowd avoidance).

**Inference:** `cityalert` (the "Grid" that pursues the player) and `rvo` are full systems from the game's first version — pressure/pursuit is not a cosmetic addition, it is a first-class gameplay system. oryndra has **no** pursuit system (no combat, no narration).

## What this changes for oryndra (inferences)

1. **A "Segment" is the right abstraction — but it needs a dimension.** In ME Catalyst, size is in the name (`_512`, `_1536`). A Segment that does not declare its dimension can neither be generated nor snapped.
2. **Affordance is an explicit production cost.** `_mesh_runnersvision` roughly doubles the mesh of traversable surfaces. Cel shading (ADR `0003-art-direction-cel-shading.md`) promises to pay for that through colour and outline — that is the hypothesis to test in a prototype.
3. **The timer is an editor, not a mode.** Treating course creation as a separate mode misses what the genre does. oryndra already models a Course as a timed route authored on a Map.
4. **Gameplay tiers and cosmetics are two orthogonal axes** — noted in ME as `armgear` tiers vs skins.
5. **A Course needs an authority backing** on a Map (persisted markers), which is an architecture decision, not a UI one.

## Remaining limits and unknowns

- **No tuning constant was extracted.** Speed, jump height, wallrun durations, coyote time: those values live in the compressed `.cas`/`.sb` payloads, out of reach of an index read. **Unresolved, and probably unresolvable** without decompressing the Frostbite container. (Mirror's Edge 2008 later provided exactly this: see `mirrors-edge-1-movement.md`.)
- **The movement code was not read.** Ghidra expired (`provider_timeout`, 330 s) on `binary_overview`. Remedy if we want to return to it: re-run the Ghidra analysis as a background job and query only afterwards, or supply an already-analysed database.
- No PDB was found on the Ghidra side (`Skipping PDB processing: failed to locate PDB file in configured locations`); function names in a retail binary will be rare.
- `inspect_binary_layout` unavailable (`REA_PWNTOOLS_PYTHON` absent).
- Asset names are **production names**: they indicate the existence of a system, not its exact behaviour. Any "how it works" statement above is an inference, not an observation.
- No asset is reusable: these files are covered by copyright (already settled in ADR `0003-art-direction-cel-shading.md`).

## Evidence index

| Source | Nature | Content |
|---|---|---|
| `open_binary` on `MirrorsEdgeCatalyst.exe` | shipped artifact, observed | PE x86-64, SHA-256 `e9d76463…5136`, provider Ghidra 12.1.4 |
| `binary_overview` (attempt) | failure | `provider_timeout`, 330 s, Ghidra analysis unfinished |
| `bin`/`.toc`/`.sb` of `Data/` | shipped artifact, observed | 49,081 asset names; the taxonomies above |
| `Data/Win32/globals.toc`, `layout.toc` | shipped artifact, observed | cleartext superbundle and bundle list |
