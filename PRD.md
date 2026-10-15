# Product Requirements Document – oryndra

## 🎯 Concept

A fast, competitive and creative **3D parkour** game. The player moves through **Maps** — handcrafted, community-made, or generated — alone or in co-op (up to 3), and lays out timed **Courses** on them.

Two ways to play, each **solo or in a team**:
- **For fun** — discovering community Maps, exploring, playing with friends.
- **For competition** — chasing the best time, beating one's own records and those of others.

The heart of the game is its **editor** and its **community**: the community builds its own experience through Maps and sharing. The vision: **"the Minecraft of parkour"**.

## 👥 Target audience

- Players who enjoy fast, fun and creative gameplay.
- Two communities: **competitors** (times, records) and **explorers** (discovery, social play).
- Launch platform: **PC** (Windows, Linux). Mac later. Consoles/mobile only if successful.

## 🕹 Gameplay

### Movement — the heart of the game

- **Every movement mechanic of Mirror's Edge Catalyst** is targeted:
  run / sprint, jump (including long jump), **wallrun** (two-tier, with an apex), wallclimb, **vault** (several variants), **heave**, **slide**, **roll**, **balance**, climb (pipes/ladders), **coil**, springboard, **MAG rope** (swing / pull-up / pull-down), zipline, swing.
- **No combat.** Movement is the only verb; no kick, no takedown.
- **Sprint is a meter** (a reserve that builds and drains), not a plain boolean.
- **Data-driven, extensible movement architecture**: adding or modifying a move must be simple. The reference model is *probe → decide → animate* (cheap geometric probes feed a movement "driver"), not a monolithic controller — validated by Mirror's Edge, where each move is a data record (probes to run, speed modifiers, animation times, cooldown).
- **Contextual triggering**: as in Mirror's Edge, only **jump** and **crouch** have a dedicated key; wallrun, vault, heave, slide, balance and zipline trigger from **context** (speed, direction, surface encountered), not from a button. The **MAG rope** is the exception: a capability, with a dedicated key.
- **Tuning reference**: the movement model and real values of **Mirror's Edge (2008)** are documented in `docs/research/mirrors-edge-1-movement.md` — starting points to re-tune to oryndra's scale, not to copy.

### Navigation & accessibility

- **No navigation aid**: no highlighted path, no arrows. **Reading the environment is a mechanic.**
- **Baseline input tolerance**, not optional (coyote time, ledge-grab margin): without it the game *seems* broken; it is not made easier.
- "Accessible" means **easy-to-understand mechanics**, never **easy levels**.

### Game loop & competition

- A **Course** is a timed route (checkpoints + goal) laid out on a **Map**.
- **Ghost**: the semi-transparent replay of a run, played back alongside.
  - At MVP: **personal best** + **author's reference Ghost**, both offline.
  - Later: friends' Ghosts and online leaderboards.
- **Progression**: every move is **available from the start** (competitive fairness); progression rewards **mastery** (medals per Course, records), not unlocking abilities.
- Online leaderboards, team mode and advanced medals arrive after the MVP and co-op.

### Built-in editor

- **Compose** Maps from prefabricated **Segments** (snapping, entry/exit connectors).
- Place **gameplay elements**: spawn, checkpoints, goal, hazards, moving objects.
- **Test** the Map immediately, inside the editor.
- **Export / share** (an open Map file, then a Steam Workshop mirror).
- Free-form geometry modelling (CSG) is out of the initial scope.

### Procedural generation

- Goal (Alpha phase): **assembling prefabricated Segments** with connectors, guaranteeing a playable Course — **no** free-form geometry generation.
- It reuses **the same vocabulary as the editor** (a single Segment system).

### Co-op (up to 3) — teamplay

- Co-op is **not** a parallel race: it rests on **dedicated mechanics** where players help each other clear obstacles **impossible alone** (leg-up/boost, counterweight, throw, slingshot, human ladder, catch, relay).
- Every mechanic is **reciprocal** (the helper must be able to progress in turn).
- Courses that depend on it are **flagged as co-op**; a solo variant is provided where possible.
- The mechanics' detail is **to be prototyped**; co-op arrives **after** the full move set and procedural generation.

## 🎨 Art & sound direction

- **Stylised rendering (cel shading)**: simple, timeless, friendly to modest configurations.
- **Colour and outline** carry the readability of traversable surfaces (what can be climbed, what can be crossed) — not texture detail.
- A **modular** universe per Map, **with no imposed narration**.
- **Sound**:
  - SFX feedback **per move and per material** (sprint, wallrun, slide, landing).
  - **Light adaptive music** driven by *Flow* (intensity rises with speed and chaining).
  - A dedicated **"Course" mixer** that foregrounds the rhythm.
  - Discreet "fun" effects.

## ⚙️ Technical

- **Engine: Godot 4.x**, language **C# (.NET) first** (GDScript possible for small scripts).
- Targets: **Windows + Linux** first, Mac later.
- **60 FPS minimum** at 1080p on a modest configuration.
- **Modular architecture**, gameplay core first; multiplayer (P2P co-op / local hosting) later.
- **Open formats**: Maps are redistributable files (see the licence).

## 📦 Production & Business

- **Solo indie** development, controlled scope, priority on the pleasure of movement.
- **Open source**, no revenue ambition:
  - **Code: GPL-3.0**; **content (assets + Maps): CC BY-SA 4.0**.
  - A **hub charter** (terms of use, not a licence) keeps published Maps **free and open**; a Map placed behind a paywall is taken down.
- **Free distribution**: itch.io, Steam (+ Steam Workshop). Maps live in an **open repository** (versioned files) with a **Workshop mirror** for discovery.
- **No monetization**; any cosmetics would be **free unlocks / community rewards**.

### Roadmap

1. **MVP** — 1-2 handcrafted Maps + the **Training Map** (tutorial); **movement core** (run/sprint, jump, wallrun, vault, slide, heave, roll); the **time-trial loop with Ghosts** (personal best + author's Ghost, offline); a **minimal editor** (place Segments + gameplay elements + test); **solo** only.
2. **Alpha** — **full move set** (MAG rope, coil, zipline, balance, climb, springboard…) and **procedural generation** (Segment assembly).
3. **Beta** — **co-op up to 3** (teamplay) and **Map sharing** (open repository + Workshop).
4. **Release** — online leaderboards, team mode, moderation/curation, visual and sound polish, platform extensions.

## 🚀 Vision

A parkour game that is simple to understand yet deep and fun, where the community builds its own experience through the editor and sharing. A game that lives as much through **competitive challenge** as through the **creative and social pleasure** of play — the Minecraft of parkour.

---

## 📎 Decisions & vocabulary

- Architecture and product decisions: `docs/adr/`.
- Project vocabulary: `GLOSSARY.md`.
- Research: `docs/research/` — an inventory of the **systems** of Mirror's Edge Catalyst, and the **movement model** of Mirror's Edge (2008) (`TdMove_*` state machine + real tuning values).
