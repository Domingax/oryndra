# Mirror's Edge (2008) — movement model & move state machine

Fact-finding for the `oryndra` PRD (`PRD.md`, movement = "tous les mouvements de Mirror's Edge
Catalyst", data-driven move architecture, no combat). Companion to
`docs/research/mirrors-edge-catalyst-mechanics.md` (ME Catalyst), which is **name-only** because
Catalyst's payloads are encrypted. **ME1 is different: its movement tuning ships in cleartext and its
UnrealScript class/function/variable names are fully recoverable.** This note therefore contains
actual numbers, not just names.

> **Evidence discipline / legend.** Every claim carries its source:
> - **[ini]** — cleartext shipped config (`TdGame/Config/*.ini`); values are *verbatim*.
> - **[u-names]** — symbol names recovered from the cooked UnrealScript package `TdGame.u`
>   (name table + string literals). Names only, no bytecode decompilation.
> - **[rea]** — REA (Ghidra) evidence. **No `[rea]` evidence is used in this note** — see §6.5:
>   ME1's movement lives in UnrealScript, so the `.exe` is not worth decompiling.
> - **Observation / Inference / Unknown** mark epistemic status.
>
> All target strings/data are treated as **untrusted DATA**. See §6.6 for the prompt-injection check.

---

## 0. Target facts

| Fact | Value | Evidence |
|---|---|---|
| Game | Mirror's Edge (2008), PC, GOG release | **[ini]/[u-names]** |
| Engine | Unreal Engine 3, game fork "Mirrors Edge"; package version stamp `536 / licensee 043`; 32-bit | **[u-names]** (`file TdGame/CookedPC/TdGame.u` → "Unreal Engine package (little-endian), version 536/043 (Mirrors Edge)") |
| Movement source of truth | `TdGame/Config/DefaultPawnMovement.ini`, 493 lines, 55 `[TdGame.TdMove*]` sections (54 unique classes), sha256 `6386225c89b045597b91ce8af04f067a79f0aa67c13e80d034df59dafe71fcc8` | **[ini]** |
| Related configs read | `DefaultAnimation.ini` (sha256 `0e06463cc0bdd08fdce48df0a1e1293b6b8a290f1be9ad9d428983c3fa7a0cad`), `DefaultInput.ini`, `DefaultCamera.ini`, `DefaultPathfindingCosts.ini`, `DefaultAIMeleeAttacks.ini`, `DefaultGameTypeSettings.ini`, `Engine/Config/Base*.ini` | **[ini]** |
| Script package | `TdGame/CookedPC/TdGame.u`, 29,281,987 bytes, sha256 `af4a5ada7972e949df1f0c6588617143c86b8ef027f7fbc206ecff1ad97b0524`; 20,592 names, 2,246 imports, 52,410 exports | **[u-names]** |
| Companion script packages | `Engine.u`, `Core.u`, `GameFramework.u`, `TdSpContent.u`, `TdMpContent.u`, `TdSharedContent.u` (same CookedPC dir) | **[u-names]** |
| Binary | `Binaries/MirrorsEdge.exe`, PE32 i386, sha256 `58de3df21f40e9953e00be92f7749097832bad02e5b657d8485cdb61f360a3dd` (not analysed in depth, see §6.5) | shell |
| Extraction method | `innoextract` of the GOG **Inno Setup** installer (game tree extracted read-only to `/home/damien/Documents/code/_external/mirrors-edge-1`, outside this repo) | given context |

**Source layout used here**

```
/home/damien/Documents/code/_external/mirrors-edge-1/
├── Binaries/MirrorsEdge.exe
├── Engine/Config/Base*.ini
└── TdGame/
    ├── Config/DefaultPawnMovement.ini      <- the movement tuning (this note's core)
    ├── Config/DefaultAnimation.ini         <- per-move animation blend tuning
    ├── Config/DefaultInput.ini             <- input bindings (see §2.2)
    ├── Config/DefaultPathfindingCosts.ini  <- per-move AI path costs
    └── CookedPC/*.u                        <- UnrealScript (names recoverable)
```

### 0.1 How `TdGame.u` was read (reproducible method)

`TdGame.u` is a cooked UE3 package whose **header tables are LZO1X-compressed in 131,072-byte
blocks** (a Mirror's Edge-specific variant; plain `strings` therefore yields mangled fragments —
"Concrete_Imp", "TdMov", `Landmark`-style pieces — and this is *not* an artefact of extraction).
Two extra steps were required beyond the Catalyst note's method:

1. **Chunked LZO1X decompression.** The file is a chain of `FCompressedChunkHeader`s
   (`signature 0x9E2A83C1`, `blockSize 131072`, `compressedSize`, `uncompressedSize`) each followed
   by a block table of `(compressedSize, uncompressedSize)` pairs and then the LZO1X payload.
   55 such chunks → **58,892,846 bytes** of uncompressed package (tables + object/bytecode data).
   The first chunk (header/tables region) decompresses to 4,729,004 bytes; header offsets in the
   file are shifted by +109 relative to that stream, and the layout validates exactly:
   name table `0..606,856`, import table `606,856..669,744`, export table `669,744..4,516,292`,
   depends `4,516,292..4,729,004`.
2. **`FNameEntry` parse** (this ME build): `[u32 lenIncludingNUL][name bytes][NUL][u64 flags]`,
   *no* inter-entry padding. Parsing exactly **20,592 entries** consumed exactly 606,856 bytes and
   terminated on the import table — i.e. a full, self-validating read of the name table.
   Flags take only 2 distinct values (`0x00070010_00000000` / `0x00071000_00000000`).

Cross-package sanity check: the same method on `TdSpContent.u` (uncompressed) puts its first name at
offset 113 and yields clean `TdMove_*` names, and the import table of `TdGame.u` parsed
**2,246/2,246** entries with all four `FName`s resolving (e.g. `Engine.Actor`, `Default__Actor`) —
so the names below are trustworthy, and the earlier "mangled" readings were an LZO artefact.

```python
# Outline (needs liblzo2 via ctypes: lzo1x_decompress_safe)
# 1. walk FCompressedChunkHeader chain from the file start; the first header sits right after the
#    package's fixed header (offset 0x3DC in TdGame.u), its block table at +16, payload at
#    (header+16+8*nblocks). Decompress every block -> 'tables.bin' (4,729,004 B).
# 2. names = []; i = 0
#    while len(names) < 20592:
#        L = u32(tables, i); raw = tables[i+4 : i+4+L-1]; i += 4 + L + 8   # skip u64 flags
#        names.append(raw.decode()); assert 32 <= min(raw) and max(raw) < 127
```

> **Observation.** Recovery of the full name table is itself a result: for ME1 we can enumerate the
> *complete* UnrealScript namespace (classes, functions, variables, states and content names), which
> the Catalyst note could not do at all.

---

## 1. Architecture: a move-manager with one active "Move" object

ME1's movement is a **stack-like state machine of `TdMove` objects**, not a monolithic controller.

**[u-names]** the hub and base classes:

| Name | Role (inferred from name + config shape) |
|---|---|
| `TdMoveManager`, `TdPlayerMoveManager`, `TdBotMoveManager` | the manager that owns the current move (`MoveManagerClass`, `Default__TdMoveManager`, `Default__TdPlayerMoveManager`) |
| `TdMove` | base move (tuned in the INI: `SpeedModifier`, `FrictionModifier`, `RedoMoveTime`, `bStickyAim`, `StickyAngle`, `StickyAimedModifier`, `bTriggersCompliment`, aim penalties) |
| `TdPhysicsMove` | physics-driven move base (`HandPlant*`, `ContextMoveDistanceMultiplier`, probe flags) |
| `TdAnimNodeMovementState`, `TdAnimNodeWalkingState`, `TdAnimNodeInAir`, `TdAnimNodeClimb`, `TdAnimNodeSwing`, `TdAnimNodeBalanceWalk`, `TdAnimNodeLedgeWalk`, `TdAnimNodeGrabbing`, `TdAnimNodeGrabTransfer`, `TdAnimNodeGrabSlope`, `TdAnimNodeLandOffset`, `TdAnimNodeAgainstWallState`, `TdAnimNodeCoverState`, `TdAnimNodeWeaponState`, `TdAnimNodeSwitch/State/BlendList/Slot/Sequence/Synch…` (55 `TdAnimNode*` total) | the **animation** half of each move |
| `TdAnimNodeCharacterMovement` **[ini]** (`DefaultAnimation.ini`) | run/sprint speed model |

### 1.1 "Probe → decide → animate" is *literally* the data model

Every move class declares **which geometric probes it runs**, as INI booleans. This is the exact
architecture the PRD asks for ("sonde → décision → animation"):

```
bCheckForGrab            – run the ledge-grab probe
bCheckForVaultOver       – run the vault-over probe
bCheckForWallClimb       – run the wall-climb probe
bCheckForEdgeInVelDir    – look for an edge along the velocity direction (hand-plant)
bCheckForSoftLanding / bCheckForEdgeInVelDir / bEnableSoftLanding   [u-names]
```

Each move also declares a **speed/friction modifier** and a **re-entry cooldown** (`RedoMoveTime`),
and per-difficulty **AI aim penalties** (`AiAimPenalties` = continuous during the move,
`AiAimOneShotPenalties` = applied once at move start) — evidence that every player move is also an
AI move, and that moves have a "deal with the player during this animation" contract.

> **Inference.** A `TdMove` = {probes to run, movement constraints, anim blend times, aim-disruption
> profile, redo cooldown}. Deepening a move = adding a property, not editing a controller. This is a
> strong precedent for `oryndra`'s data-driven move definitions and for a `MoveRegistry`
> (`MOVE_*` enum, see §4.3).

---

## 2. The move state machine: every `TdMove_*` class in `DefaultPawnMovement.ini`

**54 unique classes** tuned in the INI, in 55 sections (`[TdGame.TdMove_Falling]` appears twice;
UE3 last-wins, so the *second* block's `bCheckForGrab/bCheckForVaultOver` apply). All names below are
**[ini]**; a `‡` marks a class that is tuned in the INI **and** present in the name table.
`TdMove_Disarm` is the one INI section with **no** matching class name — see §4.2.

### 2.1 Ground / walk

| Class | Notes |
|---|---|
| `TdMove` ‡ | base: speed/friction modifiers, sticky-aim, redo time |
| `TdMove_Walking` ‡ | idle-anim timer, canned idle anims |
| `TdMove_Crouch` ‡ | `SpeedModifier=0.2` |
| `TdMove_180Turn` ‡ | the "quick turn" (time-based turn animation) |
| `TdMove_LayOnGround` ‡ | high-friction on-ground state |
| `TdMove_Vertigo` ‡ | the "look over the edge / fear of heights" camera state (see §3.13) |

### 2.2 Jump & dodge

| Class | Notes |
|---|---|
| `TdMove_Jump` ‡ | `BaseJumpZ=630`; probes grab + vault + wall-climb; long-jump speed thresholds |
| `TdMove_BotJump` ‡ | AI jump (probes grab + vault, no tuning besides the flags) |
| `TdMove_DodgeJump` ‡ | airborne dodge: low `BaseJumpZ=300`, big `JumpAddXY=600`, 30 % inertia conservation |
| `TdMove_SpringBoard` ‡ | springboard launch: `SpringBoardJumpZ=950` |

> **[ini] `DefaultInput.ini` observation:** the shipped bindings expose only `Jump` and `Crouch`
> (`LeftControl`/`SpaceBar`/`BackSpace` → `Jump`, `XboxTypeS_LeftTrigger` → `Crouch`). There is
> **no button for sprint, wallrun, slide, vault, grab, balance or zip line** — every traversal move
> is **context-triggered**. (Inference: this is why the move-probe flags above matter so much.)

### 2.3 Fall & landing

| Class | Notes |
|---|---|
| `TdMove_Falling` ‡ | fall entry threshold + roll-landing rule (two sections, see §3.4) |
| `TdMove_Landing` ‡ | the landing classifier: hard/soft/skill-roll heights |
| `TdMove_FallingUncontrolled` **[u-names]** | only in the name table, not tuned here (also `ActivateForcedUncontrolledFall` [u-names]) |
| `TdMove_SoftLanding`, `TdMove_SkillRoll` **[u-names]** | separate landing-move classes; **not** tuned in this INI, but their trigger heights are given by `TdMove_Landing` |
| `TdMove_LayOnGround` ‡ | see §2.1 |

### 2.4 Wall (run / climb / kick / dodge)

| Class | Notes |
|---|---|
| `TdMove_WallRun` ‡ | horizontal wall-run; min wall height, min speed, start angles, align speed, friction/acceleration, camera-hit effect |
| `TdMove_WallClimb` ‡ | vertical wall-run/climb; own gravity (`WallClimbingGravity=800`), 2D+Z "add-on speed" curves, double-jump window |
| `TdMove_WallKick` ‡ | the wall-kick / wall-bounce: `WallKickVelocity2D=300`, `WallKickVelocityZ=580`, `WallKickMaxDistance=60` |
| `TdMove_WallrunJump` ‡ | jump off a wall-run: Z height forward + push-away speed (`Noob` vs `ProAdd`) |
| `TdMove_WallrunDodgeJump` ‡ | wall-run → dodge (same burst params as `DodgeJump`) |
| `TdMove_WallClimbDodgeJump` ‡ | wall-climb → dodge with `DodgeJumpInertiaConservation=1.0` and `BaseJumpZ=700` |
| `TdMove_WallClimb180TurnJump` ‡ | 180° turn jump off a wall climb (`JumpOffZHeight=250`, `JumpTimeWindow=0.6`) |

### 2.5 Grab & heave / pull-up

| Class | Notes |
|---|---|
| `TdMove_IntoGrab` ‡ | the **grab probe/transition**: align speed/angle, `GrabDesiredLedgeOffset=(X=30,Y=0,Z=92.8)`, "folded" near-ledge cases, impact tiers |
| `TdMove_IntoGrabBot` ‡ | AI variant (`IntoGrabMaxAngle=90`) |
| `TdMove_Grab` ‡ | the **hang**: `GrabMaxAngle=40`, `HangFreeZDistanceCheck=128`, 0.15 s redo time |
| `TdMove_GrabPullUp` ‡ | the **heave/pull-up** = oryndra's *Relevé*: `GrabAllowedPullUpAngle=45` |
| `TdMove_GrabJump` ‡ | jump off a ledge hang (`GrabJumpOffZHeight=160`, push-away 200–400) |
| `TdMove_GrabTransfer` ‡ | lateral hand-to-hand ledge transfer (`Allowed2DTransferDistance=260`, `AllowedZTransferDistance=140`) |
| `TdMove_StandGrabHeaveBot` **[u-names]** | AI stand-grab-heave (only move class with surviving debug info: functions `FindClimbNotifiers`, `JumpInitiatedNotify`) |
| `TdGrabHeave` **[u-names]** | shared grab/heave helper object |

### 2.6 Vault / step-up

| Class | Notes |
|---|---|
| `TdMove_SpeedVault` ‡ | the **vault** family: 7 `VaultTypes` (auto step-up, step-up, vault-onto, vault-over, high vault-over, high vault-onto), each with height window, `MinSpeedZ/MaxSpeedZ`, `ClampSpeedMin/Max`, up/over/down times and IK offsets |
| `TdMove_VaultOver` ‡ | thin section (no properties of its own beyond the base defaults) |
| `TdMove_StepUp` ‡ | discrete step heights: low 33 / medium 68 / high 112–148 (full step) |
| `TdMove_AutoStepUp` ‡ | automatic small step-up: high band 8–48 |
| `TdMove_MeleeVault`, `TdMove_VaultOverBot`, `TdMove_SpeedVaultBot` **[u-names]** | combat/AI vaults |

### 2.7 Climb (pipes / ladders)

| Class | Notes |
|---|---|
| `TdMove_IntoClimb` ‡ | transition into a climbable |
| `TdMove_Climb` ‡ | the climb itself: up/down blend + fast-up anim rate (`ClimbFastUpPipeAnimRate=2.0`), turning angle, allow-jump angle |

### 2.8 Zip line

| Class | Notes |
|---|---|
| `TdMove_ZipLine` ‡ | `MinZipVelocity=300`, `MinZipAcceleration=400`, fade in/out |
| `TdMove_IntoZipLine` ‡ | transition; `ZVelocityFallLimit=-600` (i.e. you can catch a zip line while falling fast) |
| `TdZiplineVolume` **[u-names]** | the authored volume that defines zip lines |

### 2.9 Slide

| Class | Notes |
|---|---|
| `TdMove_Slide` ‡ | the **glissade**: `SlideAbortSpeed=250`, `SlideAbortTime=2.0`, friction 0.1, floor-incline limit |
| `TdMove_SlideBot` ‡ | AI slide (friction 0.035) |
| `TdMove_RumpSlide` ‡ | a **slope/rail slide** with its own `MaxSlideSpeed=1000`, `SideControl=350`, `GravityModifier=0.5`, `InitialSpeedLoss=0.75`, `MinSlideFloorZ=0.9` |

### 2.10 Balance / ledge-walk

| Class | Notes |
|---|---|
| `TdMove_Balance` ‡ | the **balance mini-game**: `GravityInfluence=0.3`, `ControlInfluence=1.5`, `SpeedInfluence=2.5`, `CameraInfluence=0.3`, `TimeToCounter=0.8` |
| `TdMove_LedgeWalk` ‡ | shimmy on a ledge (`SpeedModifier=0.1`) |
| `TdBalanceWalkVolume`, `TdLedgeWalkVolume` **[u-names]** | authored balance/ledge-walk volumes |

### 2.11 Swing

| Class | Notes |
|---|---|
| `TdMove_Swing` ‡ | pipe/bar swing: `SwingPendulumLength=120`, `ExitVelocityModifier=600`, exit-gravity shaping |
| `TdMove_SwingJump` ‡ | release-jump from a swing: `GravityModifier=0.73` for 0.75 s, target volume offset |
| `TdSwingVolume` **[u-names]** | authored swing volume |

### 2.12 Coil

| Class | Notes |
|---|---|
| `TdMove_Coil` ‡ | the **coil** (vertical impulse against a support): `CoilTime=0.5`, `CoilMinTriggerSpeed=100`, `TotalHeightBoost=60` over `HeightBoostDuration=0.25` |

### 2.13 Special / other

| Class | Notes |
|---|---|
| `TdMove_Interact` ‡ | contextual interact (`DistanceToAButton=48`, `DistanceToAValve=40`) |
| `TdMove_Vertigo` ‡ | height/edge camera state (`ZoomFOV=84`, `ZoomRate=30`, `ZoomOutTime=1.2`) |
| `TdMove_Disarm` ‡ | **no matching class name in `TdGame.u`** (§4.2) — combat disarm; aim penalties + 0.2 s redo |
| `TdMove_MeleeBase`, `TdMove_Melee`, `TdMove_MeleeAir`, `TdMove_MeleeAirAbove`, `TdMove_MeleeSlide`, `TdMove_MeleeWallrun`, `TdMove_MeleeCrouch`, `TdMove_MeleeBarge` ‡ | combat, and crucially **combat as a movement move**: slide-melee, wallrun-melee, air-melee |
| `TdMove_Barge`, `TdMove_AirBarge` ‡ | shoulder-charge / air-charge (also used to break doors; `HeightBoostDuration=0.25`, `TotalHeightBoost=60` — identical to coil) |
| `TdMove_AISpecialMove`, `TdMove_AnimationPlayback`, `TdMove_Cutscene`, `TdMove_Disabled` **[u-names]** | non-gameplay / scripted states |
| `TdMove_Cover`, `TdMove_EnterCover`, `TdMove_ExitCover` **[u-names]** | a cover system that exists in the script but is **not tuned for the player** (ME1 shipped no player cover) |

---

## 3. Per-move tuned properties (verbatim from `DefaultPawnMovement.ini`)

All values below are **[ini]**, copied exactly as shipped (including `1.f`, `0.05`, trailing `;`,
and the comment markers). *Not used at runtime by the player* is flagged where relevant.

### 3.1 Bases

| Class | Property | Value |
|---|---|---|
| `TdGame.TdMove` | `SpeedModifier` | `1.f` |
| | `FrictionModifier` | `1.f` |
| | `RedoMoveTime` | `0.f` |
| | `bStickyAim` | `true` |
| | `StickyAngle` | `800` |
| | `StickyAimedModifier` | `0.0` |
| | `AiAimPenalties` | `(Easy = 1.0, Medium = 1.0, Hard = 1.0)` |
| | `AiAimOneShotPenalties` | `(Easy = 0.0, Medium = 0.0, Hard = 0.0)` |
| | `FireAnimSeqName` | `"standfire"` |
| | `ReloadAnimSeqName` | `"reload"` |
| | `bTriggersCompliment` | `false` |
| `TdGame.TdPhysicsMove` | `HandPlantExtentCheckHeight` | `80` (a commented alternative `148` is present) |
| | `HandPlantExtentCheckWidth` | `10` |
| | `HandPlantCheckDistance` | `200` |
| | `HandPlantCheckHeight` | `112` |
| | `ContextMoveDistanceMultiplier` | `1.8` |
| | `bCheckForGrab` | `false` |
| | `bCheckForEdgeInVelDir` | `false` |
| | `bCheckForWallClimb` | `false` |

> The INI's own header comment: `# Don't modify this file. If you do, your game may not start.`
> (shipped-file warning, not an instruction to this analysis).

### 3.2 Ground / walk

| Class | Property | Value |
|---|---|---|
| `TdMove_Walking` | `FrictionModifier` | `1.0` |
| | `TriggerIdleAnimMinTime` | `30.f` |
| | `TriggerIdleAnimMaxTime` | `40.f` |
| | `UnarmedIdleAnims` (×3) | `(AnimName="standidle1",NodeType=CNT_Canned,bResetCameraLook=true)` / `standidle2` / `standidle3` (same options) |
| `TdMove_Crouch` | `SpeedModifier` | `0.2f` |
| | `IntoCrouchSitTime` | `1.0` |
| `TdMove_180Turn` | `FrictionModifier` | `0.3f` |
| | `TurnTime` | `0.25` |
| | `TurnAnimBlendInTime` | `0.2` |
| | `TurnAnimBlendOutTime` | `0.2` |
| | `RedoMoveTime` | `0.5f` |
| `TdMove_LayOnGround` | `FrictionModifier` | `0.150f` |

### 3.3 Jump & dodge

| Class | Property | Value |
|---|---|---|
| `TdMove_Jump` | `BaseJumpZ` | `630.0` |
| | `BaseJumpZHeavy` | `430.0` |
| | `JumpAddXY` | `100` |
| | `bCheckForGrab` | `true` |
| | `bCheckForVaultOver` | `true` |
| | `LongJumpSlowThreshold` | `400` |
| | `LongJumpNormalThreshold` | `500` |
| | `LongJumpFastThreshold` | `700` |
| | `JumpBlendInTime` | `0.1` |
| | `JumpBlendOutTime` | `0.2` |
| | `JumpStillBlendOutTime` | `0.2` |
| | `bCheckForWallClimb` | `true` |
| | `AiAimPenalties` | `(Easy = 0.7, Medium = 0.8, Hard = 1.0)` |
| `TdMove_BotJump` | `bCheckForGrab` | `true` |
| | `bCheckForVaultOver` | `true` |
| `TdMove_DodgeJump` | `BaseJumpZ` | `300.0` |
| | `JumpAddXY` | `600.0` |
| | `DodgeJumpInertiaConservation` | `0.3` |
| | `StrafeThreshold` | `0.99` |
| | `bCheckForGrab` | `false` |
| | `bCheckForVaultOver` | `false` |
| | `JumpBlendInTime` | `0.1` |
| | `JumpBlendOutTime` | `0.2` |
| | `RedoMoveTime` | `0.3f` |
| | `bTriggersCompliment` | `false` |
| | `LookAtAIVelThreshold` | `200` |
| | `LookAtAITraceDistance` | `250` |
| | `LookAtAIRadiusThreshold` | `90` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |
| `TdMove_SpringBoard` | `SpringBoardMaxHeight` | `148` |
| | `SpringBoardMinHeight` | `80` |
| | `SpringBoardJumpZ` | `950` |
| | `SpringBoardJumpXYAdd` | `-100` |
| | `SpringBoardJumpXYMin` | `400` |
| | `IntermediateFootPlantHeight` | `64` |
| | `IntermediateFootPlantDistance` | `112` |
| | `CheckDistanceTime` | `1.0f` |
| | `bCheckForVaultOver` / `bCheckForGrab` / `bCheckForWallClimb` | `true` |
| | `bTriggersCompliment` | `true` |
| | `AiAimPenalties` | `(Easy = 0.3, Medium = 0.4, Hard = 0.6)` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |

### 3.4 Fall & landing

`[TdGame.TdMove_Falling]` is defined **twice** (lines 43–46 and 231–233); UE3 applies the later
values, so the effective `bCheckForGrab`/`bCheckForVaultOver` come from the second block.

| Class | Property | Value |
|---|---|---|
| `TdMove_Falling` (1st block) | `EnterToFallingZSpeed` | `- 200` |
| | `RollLandingTimeThreshold` | `2.0` |
| | `MaximumSpeedForRollLanding` | `-5000` |
| `TdMove_Falling` (2nd block) | `bCheckForGrab` | `true` |
| | `bCheckForVaultOver` | `true` |
| `TdMove_Landing` | `HardLandingDamage` | `15` |
| | `LandingSpeedReduction` | `65.f` |
| | `HardLandingHeight` | `530.f` |
| | `SoftLandingHeight` | `300.f` |
| | `SkillRollLandingHeight` | `200.f` |

> **Observation.** `TdMove_Landing` is a *classifier*: ≥530 hard landing, ≥300 soft landing,
> ≥200 **skill roll** — and `TdMove_SoftLanding` / `TdMove_SkillRoll` are separate move classes
> (**[u-names]**) that are not themselves tuned in this INI. Landing also carries damage
> (`HardLandingDamage=15`) because ME1 lets you land on enemies.

### 3.5 Wall

| Class | Property | Value |
|---|---|---|
| `TdMove_WallRun` | `WallRunningForwardCheckDistance` | `50` |
| | `WallRunningStrafeCheckDistance` | `50` |
| | `WallRunningMinWallHeight` | `192` |
| | `WallRunningMinSpeed` | `200` |
| | `WallRunningVelocityStartLimit` | `300` |
| | `WallRunningVelocityStopLimit` | `-500` |
| | `WallRunningForwardMinStartAngle` | `0` |
| | `WallRunningForwardMaxStartAngle` | `57` |
| | `WallRunningStrafeStartAngle` | `60` |
| | `WallRunningHorisontalFriction` | `0.05` |
| | `WallRunningHorisontalInitialZHeight` | `170` |
| | `WallRunningHorisontalAcceleration` | `820` |
| | `WallRunningHorisontalDeceleration` | `500` |
| | `WallRunningHorisontalAlignSpeed` | `700` |
| | `WallRunningDistanceForIntoWall` | `180` |
| | `WallRunningRotatePawnAlongWallTime` | `0.4` |
| | `WallRunningDelayPawnRotationTime` | `0.01` |
| | `WallRunningMoveToIntoPositionDegreeThreshold` | `70` |
| | `WallRunningIntoWallrunBlendInTime` | `0.2` |
| | `WallRunningIntoWallrunBlendOutTime` | `0.2` |
| | `PlayCameraHitWallEffect` | `true` |
| | `MinimumVelocityIntoWall` | `600` |
| | `MaximumVelocityIntoWall` | `700` |
| | `WallrunStartUpperBodyAnimPlayRate` | `0.6` |
| | `LookAlongWallInterpolationTime` | `0.2` |
| | `AiAimPenalties` | `(Easy = 0.4, Medium = 0.5, Hard = 0.7)` |
| | `StickyAngle` | `4000` |
| | `StickyAimedModifier` | `1.0` |
| | `RedoMoveTime` | `0.15` |
| | `TimeToDo90Turn` | `0.25` |
| | `bTriggersCompliment` | `false` |
| `TdMove_WallClimb` | `WallClimbingMaxDistance2D` | `120` |
| | `WallClimbingVelocityStartLimit` | `0` |
| | `WallClimbingVerticalStartAngle` | `33 //55` |
| | `WallClimbingVerticalFriction` | `6` |
| | `AddOnSpeed2DHeight` | `60` |
| | `AddOnSpeed2DMaxLimit` | `650` |
| | `AddOnSpeedZHeight` | `130` |
| | `AddOnSpeedZMaxLimit` | `320` |
| | `WallClimbingGravity` | `800` |
| | `bCheckForVaultOver` / `bCheckForGrab` / `bCheckForEdgeInVelDir` | `true` |
| | `HandPlantCheckHeight` | `112` |
| | `MinLegdeZNormal` | `0.707` |
| | `MinWallHeight` | `180` |
| | `bTriggersCompliment` | `false` |
| | `FrictionModifier` | `0.3f` |
| | `MinUpwardsVelocityToDoubleJump` | `100` |
| | `MaxIntoWallClimbVelocityToDoubleJump` | `100` |
| | `AiAimPenalties` | `(Easy = 0.4, Medium = 0.5, Hard = 0.7)` |
| `TdMove_WallKick` | `WallKickMaxDistance` | `60` |
| | `WallKickVelocity2D` | `300` |
| | `WallKickVelocityZ` | `580` |
| | `WallKickCheckHeight` | `90` |
| | `WallKickExtentWidth` | `20` |
| | `WallKickExtentHeight` | `10` |
| | `bCheckForGrab` / `bCheckForVaultOver` | `true` |
| | `RedoMoveTime` | `1.f` |
| | `bTriggersCompliment` | `true` |
| `TdMove_WallrunJump` | `WallRunningJumpOffZHeightForward` | `100` |
| | `WallRunningJumpOffZHeightMaxAddTurned` | `60` |
| | `WallRunningPushAwaySpeedNoob` | `120` |
| | `WallRunningPushAwaySpeedProAdd` | `400` |
| | `WallRunningPushForwardSpeedMin` | `0.1` |
| | `bCheckForGrab` / `bCheckForVaultOver` / `bCheckForWallClimb` | `true` (`bCheckForWallClimb` is listed **twice**) |
| | `bTriggersCompliment` | `false` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |
| `TdMove_WallrunDodgeJump` | `BaseJumpZ` | `300.0` |
| | `JumpAddXY` | `600.0` |
| | `DodgeJumpInertiaConservation` | `0.3` |
| | `bCheckForGrab` / `bCheckForVaultOver` | `false` |
| | `JumpBlendInTime` / `JumpBlendOutTime` | `0.0` / `0.0` |
| | `bTriggersCompliment` | `true` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |
| `TdMove_WallClimbDodgeJump` | `BaseJumpZ` | `700.0` |
| | `JumpAddXY` | `150.0` |
| | `DodgeJumpInertiaConservation` | `1.0` |
| | `bCheckForGrab` / `bCheckForVaultOver` / `bCheckForWallClimb` | `true` |
| | `JumpBlendInTime` / `JumpBlendOutTime` | `0.2` / `0.2` |
| | `bTriggersCompliment` | `true` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |
| `TdMove_WallClimb180TurnJump` | `JumpOffZHeight` | `250` |
| | `JumpPushAwaySpeed` | `400` |
| | `JumpTimeWindow` | `0.6` |
| | `bCheckForGrab` / `bCheckForVaultOver` / `bCheckForWallClimb` | `true` |
| | `RedoMoveTime` | `1.0` |
| | `bTriggersCompliment` | `true` |
| | `AiAimPenalties` | `(Easy = 0.3, Medium = 0.4, Hard = 0.6)` |
| | `AiAimOneShotPenalties` | `(Easy = 100.0, Medium = 100.0, Hard = 100.0)` |

### 3.6 Grab, hang, heave

| Class | Property | Value |
|---|---|---|
| `TdMove_IntoGrab` | `IntoGrabAlignSpeed` | `300` |
| | `IntoGrabMaxAngle` | `70` |
| | `IntoGrabMinInitialAlignSpeed` | `-3000` |
| | `GrabMinGrabableZNormal` | `0.707` |
| | `GrabDesiredLedgeOffset` | `(X=30.0000000,Y=0.000000,Z=92.800000)` |
| | `MinGrabLedgeAdjustDistance` | `32` |
| | `IntoGrabMaxDistance` | `200` |
| | `IntoGrabZVelocityThreshold` | `150` |
| | `IntoGrabBelowEdgeZVelocityThreshold` | `800` |
| | `HangFoldedIntoGrabZSpeedThreshold` | `150` |
| | `HangFoldedIntoGrabSpeed2DThreshold` | `50` |
| | `HangFoldedUpperDeltaDistance` | `35` |
| | `HangFoldedLowerDeltaDistance` | `70` |
| | `HangFoldedDownwardSpeedLimit` | `-300` |
| | `HangFoldedMaxDistance` | `60` |
| | `bCheckForVaultOver` | `true` |
| | `HangImpactMinZSpeed` | `-600` (`#HangHardStart2`) |
| | `HangHardImpactMinZSpeed` | `-1000` (`#HangHardStart3`) |
| `TdMove_IntoGrabBot` | `IntoGrabMaxAngle` | `90` |
| `TdMove_Grab` | `GrabMaxAngle` | `40` |
| | `GrabDesiredLedgeOffset` | `(X=30.0000000,Y=0.000000,Z=92.800000)` |
| | `RedoMoveTime` | `0.15` |
| | `StartTurningAngle` | `16384` |
| | `HangFreeZDistanceCheck` | `128` |
| `TdMove_GrabPullUp` | `GrabAllowedPullUpAngle` | `45` |
| | `bCheckForGrab` | `false` |
| `TdMove_GrabJump` | `GrabJumpOffZHeight` | `160` |
| | `GrabJumpPushAwayMaxSpeed` | `400` |
| | `GrabJumpPushAwayMinSpeed` | `200` |
| | `GrabAllowedJumpAngle` | `45` |
| | `bCheckForGrab` / `bCheckForVaultOver` | `true` |
| `TdMove_GrabTransfer` | `Allowed2DTransferDistance` | `260` |
| | `AllowedZTransferDistance` | `140` |
| | `AiAimOneShotPenalties` | `(Easy = 75.0, Medium = 75.0, Hard = 75.0)` |

### 3.7 Vault / step-up

`TdMove_SpeedVault`'s seven `VaultTypes` entries, verbatim (tab/space layout as shipped):

```
VaultTypes = (AnimName="autostepuprightleg", bVaultOnto = true,	MinHeight=0,	MaxHeight=48,	MinSpeedZ=-600, 	MaxSpeedZ=0,		MaxDistanceTime=0.2f,	ClampSpeedMin=100, ClampSpeedMax=300, VaultTimeUp=0.0,	VaultTimeOver=0.3, VaultTimeDown=0.2,	OverObstacleTime=0.2,		HandplantOffset=(X=0.f,Y=0.f,Z=5.f), 		HandIKOffset=(X=0.f,Y=0.f,Z=0.f),	LedgeOffset=(X=0.f,Y=0.f,Z=90.f),	bLeftHandIK=false,	bRightHandIK=false )
VaultTypes = (AnimName="stepuprightleg88", bVaultOnto = true,	MinHeight=48,	MaxHeight=148,	MinSpeedZ=0,	MaxSpeedZ=700,	MaxMomentum = 200.f, 	MaxDistanceTime=0.4f,	ClampSpeedMin=200, 	ClampSpeedMax=700, VaultTimeOver=0.4,	VaultTimeDown=0.25,	OverObstacleTime = 0.2, LedgeOffset=(X=0.f,Y=-20.f,Z=60.f))
VaultTypes = (AnimName="vaultOnto", bVaultOnto = true,	MinHeight=64,	MaxHeight=148,	MinSpeedZ=0,	MaxSpeedZ=10000,		MaxDistanceTime=0.4f,	ClampSpeedMin=400,	ClampSpeedMax=720, SpeedAddition=80.f, VaultTimeUp=0.0,	VaultTimeOver=0.35, VaultTimeDown=0.3,	OverObstacleTime = 0.2,		HandplantOffset=(X=0.f,Y=0.f,Z=5.f),		HandIKOffset=(X=0.f,Y=-7.f,Z=0.f),	LedgeOffset=(X=0.f,Y=0.f,Z=25.f),	bLeftHandIK=true,	bRightHandIK=false, 	bIsStringable=true)
VaultTypes = (AnimName="vaultOver", bVaultOnto = false,	MinHeight=64,	MaxHeight=148,	MinSpeedZ=0,	MaxSpeedZ=10000,		MaxDistanceTime=0.4f,	ClampSpeedMin=400,	ClampSpeedMax=720, SpeedAddition=80.f, VaultTimeUp=0.0,	VaultTimeOver=0.35, VaultTimeDown=0.3,	OverObstacleTime = 0.2,		HandplantOffset=(X=0.f,Y=0.f,Z=5.f),		HandIKOffset=(X=0.f,Y=-7.f,Z=0.f),	LedgeOffset=(X=0.f,Y=0.f,Z=25.f),	bLeftHandIK=true,	bRightHandIK=false, 	bIsStringable=true)
VaultTypes = (AnimName="VaultOverHigh",	bVaultOnto = false, 	MinHeight=145,	MaxHeight=192,	MinSpeedZ=50,	MaxSpeedZ=10000,		MaxDistanceTime=0.4f,	ClampSpeedMin=200,	ClampSpeedMax=400, VaultTimeUp=0.28, 	VaultTimeOver=0.3,	VaultTimeDown=0.45,	OverObstacleTime = 0.35,	HandplantOffset=(X=0.f,Y=-40.f,Z=-69.f),	HandIKOffset=(X=0.f,Y=-7.f,Z=0.f),	LedgeOffset=(X=0.f,Y=0.f,Z=5.f),	bLeftHandIK=false,	bRightHandIK=false, bResetCamera=true )
VaultTypes = (AnimName="VaultOntoHigh", bVaultOnto = true,	MinHeight=145,	MaxHeight=192,	MinSpeedZ=50,	MaxSpeedZ=10000,		MaxDistanceTime=0.4f,	ClampSpeedMin=200,	ClampSpeedMax=400, VaultTimeUp=0.27,	VaultTimeOver=0.3,	VaultTimeDown=0.6,	OverObstacleTime = 0.35,	HandplantOffset=(X=0.f,Y=-65.f,Z=-15.f),	HandIKOffset=(X=0.f,Y=-7.f,Z=0.f),	LedgeOffset=(X=0.f,Y=0.f,Z=35.f),	bLeftHandIK=true,	bRightHandIK=true, bResetCamera=true )
```

| Class | Property | Value |
|---|---|---|
| `TdMove_SpeedVault` | `VaultClearObjectHeight` | `35` |
| | `bTriggersCompliment` | `false` |
| | `MaxTimeToLedge` | `0.4` |
| | `VaultTypes` | 7 entries above |
| | `AiAimOneShotPenalties` | `(Easy = 50.0, Medium = 50.0, Hard = 50.0)` |
| `TdMove_VaultOver` | *(section contains no properties)* | — |
| `TdMove_StepUp` | `StepUpDistanceLimit` | `60` |
| | `StepUpSpeedLimit` | `300;` |
| | `StepUpLowMinHeight` | `33;` |
| | `StepUpMediumMinHeight` | `68;` |
| | `StepUpHighMinHeight` | `112;` |
| | `StepUpHighMaxHeight` | `148;` |
| | `StepUpOptimalLowHeight` | `48;` |
| | `StepUpOptimalMediumHeight` | `88;` |
| | `StepUpOptimalHighHeight` | `144;` |
| `TdMove_AutoStepUp` | `StepUpDistanceLimit` | `60` |
| | `StepUpSpeedLimit` | `300;` |
| | `StepUpLowMinHeight` | `33;` |
| | `StepUpMediumMinHeight` | `68;` |
| | `StepUpHighMinHeight` | `8;` |
| | `StepUpHighMaxHeight` | `48;` |
| | `StepUpOptimalLowHeight` | `48;` |
| | `StepUpOptimalMediumHeight` | `88;` |
| | `StepUpOptimalHighHeight` | `144;` |

### 3.8 Climb

| Class | Property | Value |
|---|---|---|
| `TdMove_IntoClimb` | `RedoMoveTime` | `0.5f` |
| `TdMove_Climb` | `IdleBlendInTime` | `0.05` |
| | `ClimbBlendInTime` | `0.05` |
| | `ClimbDownBlendInTime` | `0.5` |
| | `ClimbDownFastVelocity` | `200` |
| | `ClimbFastUpPipeAnimRate` | `2.0` |
| | `StartTurningAngle` | `16384` |
| | `AllowJumpAngle` | `8192` |
| | `RedoMoveTime` | `0.5f` |

### 3.9 Zip line

| Class | Property | Value |
|---|---|---|
| `TdMove_ZipLine` | `MinZipVelocity` | `300` |
| | `MinZipAcceleration` | `400` |
| | `ZipFadeInTime` | `0.1f` |
| | `ZipFadeOutTime` | `0.5f` |
| | `AiAimPenalties` | `(Easy = 0.01, Medium = 0.1, Hard = 0.3)` |
| | `AIAimOneShotPenalties` | `(Easy = 200.0, Medium = 200.0, Hard = 200.0)` (note: capitalised differently from every other section) |
| `TdMove_IntoZipLine` | `ZVelocityFallLimit` | `-600` |
| | `IntoZiplineBlendInTime` | `0.3f` |
| | `IntoZiplineBlendOutTime` | `0.2f` |
| | `RedoMoveTime` | `0.5f` |

### 3.10 Slide

| Class | Property | Value |
|---|---|---|
| `TdMove_Slide` | `SlideAbortSpeed` | `250` |
| | `SlideAbortTime` | `2.0` |
| | `FrictionModifier` | `0.1f` |
| | `MaxFloorInclineZ` | `0.5` |
| | `SlideSoundFadeInTime` | `0.1f` |
| | `SlideSoundFadeOutTime` | `0.5f` |
| | `SlideSoundAndEffectPollInterval` | `0.2f` |
| | `AiAimPenalties` | `(Easy = 0.7, Medium = 0.8, Hard = 0.9)` |
| `TdMove_SlideBot` | `SlideAbortSpeed` | `100` |
| | `SlideAbortTime` | `1.0` |
| | `FrictionModifier` | `0.035f` |
| `TdMove_RumpSlide` | `RedoMoveTime` | `1.f` |
| | `MaxSlideSpeed` | `1000.f` |
| | `SideControl` | `350.f` |
| | `GravityModifier` | `0.5f` |
| | `InitialSpeedLoss` | `0.75f` |
| | `MinSlideFloorZ` | `0.9f` |
| | `AiAimPenalties` | `(Easy = 0.01, Medium = 0.04, Hard = 0.1)` |
| | `AIAimOneShotPenalties` | `(Easy = 200.0, Medium = 200.0, Hard = 200.0)` |

### 3.11 Balance / ledge-walk

| Class | Property | Value |
|---|---|---|
| `TdMove_Balance` | `SpeedModifier` | `0.34f` |
| | `RedoMoveTime` | `0.5f` |
| | `GravityInfluence` | `0.3f` |
| | `ControlInfluence` | `1.5f` |
| | `SpeedInfluence` | `2.5f` |
| | `CameraInfluence` | `0.3f` |
| | `TimeToCounter` | `0.8f` |
| | `AiAimOneShotPenalties` | `(Easy = 50.0, Medium = 50.0, Hard = 50.0)` |
| `TdMove_LedgeWalk` | `SpeedModifier` | `0.1f` |

### 3.12 Swing, coil

| Class | Property | Value |
|---|---|---|
| `TdMove_Swing` | `SwingPendulumLength` | `120.f` |
| | `ExitVelocityModifier` | `600.f` |
| | `SwingAngleTimingOffset` | `1.0f` |
| | `SwingExitGravityModifier` | `0.75f` |
| | `SwingExitGravityModifierTime` | `0.70f` |
| | `bCheckForGrab` / `bCheckForVaultOver` / `bCheckForWallClimb` | `true` |
| | `bTriggersCompliment` | `false` |
| | `AiAimPenalties` | `(Easy = 0.1, Medium = 0.2, Hard = 0.4)` |
| | `AIAimOneShotPenalties` | `(Easy = 200.0, Medium = 200.0, Hard = 200.0)` |
| `TdMove_SwingJump` | `bCheckForWallClimb` / `bCheckForGrab` / `bCheckForVaultOver` | `true` |
| | `bTriggersCompliment` | `true` |
| | `GravityModifier` | `0.73f` |
| | `GravityModifierTimer` | `0.75f` |
| | `TargetVolumeOffset` | `(X=-120.0000000,Y=0.000000,Z=-20)` |
| | `AiAimPenalties` | `(Easy = 0.2, Medium = 0.3, Hard = 0.5)` |
| | `AIAimOneShotPenalties` | `(Easy = 200.0, Medium = 200.0, Hard = 200.0)` |
| `TdMove_Coil` | `HeightBoostDuration` | `0.25f` |
| | `TotalHeightBoost` | `60.f` |
| | `CoilTime` | `0.5f` |
| | `CoilMinTriggerSpeed` | `100.f` |

### 3.13 Special / other

| Class | Property | Value |
|---|---|---|
| `TdMove_Interact` | `DistanceToAButton` | `48.f` |
| | `DistanceToAValve` | `40.0f` |
| `TdMove_Vertigo` | `RedoMoveTime` | `3.f` |
| | `ZoomFOV` | `84.f` |
| | `ZoomRate` | `30.f` |
| | `ZoomOutTime` | `1.2f` |
| `TdMove_Disarm` | `AiAimPenalties` | `(Easy = 0.01, Medium = 0.08, Hard = 0.3)` |
| | `AiAimOneShotPenalties` | `(Easy = 200.0, Medium = 200.0, Hard = 200.0)` |
| | `RedoMoveTime` | `0.2f` |
| `TdMove_MeleeBase` | `MeleeDamage` | `50.0f` |
| | `MaxMeleeDistance` | `180.0f` |
| | `MaxMeleeAngle` | `0.5f` |
| `TdMove_Melee` | `MeleeDamage` | `33.5f` |
| | `MeleeAssistCone` | `0.8f` |
| | `MeleeAssistRotationTime` | `0.2f` |
| | `BlendInMissed` | `0.08f` |
| | `BlendOutMissed` | `0.1f` |
| `TdMove_MeleeAir` | `MeleeDamage` | `100.0f` |
| | `HeavyAttackHoldTime` | `1.4` |
| | `MeleeAirAboveMinAngle` | `0.8` |
| | `MeleeAirAboveMinSeparation` | `150.0` |
| | `MeleeAirAboveMaxSeparation` | `500.0` |
| | `RedoMoveTime` | `0.3f` |
| `TdMove_MeleeAirAbove` | `MeleeDamage` | `300.0f` |
| `TdMove_MeleeSlide` | `MeleeDamage` | `60.0f` |
| | `FrictionModifier` | `0.1f` |
| | `SoccerKickDamage` | `60.0f` |
| `TdMove_MeleeWallrun` | `MeleeDamage` | `80.0f` |
| `TdMove_MeleeCrouch` | `MeleeDamage` | `33.5.0f` (malformed literal in the shipped file) |
| | `SpeedModifier` | `0.2f` |
| `TdMove_MeleeBarge` | `MeleeDamage` | `50.0f` |
| | `BargeMaxDistance` | `500.f` |
| | `BargeAdditionalUpForce` | `100.f` |
| | `BargeForceMultiplier` | `2.4f` |
| `TdMove_Barge` | `BargeMinTraceDistance` | `90` |
| | `BargeAddOnSpeed` | `200` |
| | `BargeMaxSpeed` | `500` |
| | `BargeKickThresholdSpeed` | `250` |
| | `RedoMoveTime` | `0.1` |
| | `BargeTraceTime` | `0.5` |
| `TdMove_AirBarge` | `BargeMinTraceDistance` | `300` |
| | `HeightBoostDuration` | `0.25f` |
| | `TotalHeightBoost` | `60.f` |

### 3.14 Adjacent tuning that completes the picture

**[ini] `DefaultAnimation.ini`** — the ground locomotion model and per-move blend times:

| Section | Property | Value |
|---|---|---|
| `TdAnimNodeInAir` | `StartOfJumpBlendTime` | `0.1` |
| | `InAirBlendTime` | `1.0` |
| | `IntoLandBlendTime` | `1.0` |
| | `IntoLandTime` | `2.0` |
| | `FallVelThreshold` | `100` |
| | `FallToDeathThreshold` | `1800` |
| | `StartOfLongJumpMomentumThreshold` | `380` |
| `TdAnimNodeCharacterMovement` | `SneakVel` | `1` |
| | `WalkVel` | `200` |
| | `RunVel` | `380` |
| | `RunAccToRunVel` | `550` |
| | `StartBlendingInSprint` | `690` |
| | `FullSprint` | `700` |
| | blend times | `IntoIdleBlendTime=0.2`, `IntoSneakBlendTime=0.1`, `IntoWalkBlendTime=0.1`, `IntoRunBlendTime=0.5`, `IntoSprintBlendTime=0.1`, `ThumbStickTapBlendTime=0.2`, `IntoRunAccBlendTime=0.2`, `StopBlendTime=0.2` |
| | `ForceIntoRunAccel` / `ForceIntoStopAccel` | `650` / `-650` |
| | `RunAccUnarmedAimingMomentumThreshold` / `UnarmedAimingMomentumThreshold` | `550` / `330` |
| `TdAnimNodeMovementState` | `GoesIntoWallRunBlendTime` | `0.6` |
| | `GoesIntoJumpBlendTime` | `0.1` |
| | `GoesIntoInAirBlendTimeFromWallJump` | `0.5` |
| `TdAnimNodeLanding` | `GoesFromLandingToWalkingBlendTime` | `0.1` |
| | `GoesIntoLandingBlendTime` | `0.0` |
| `TdAnimNodeWallJump` | `GoesIntoWallJumpLongBlendTime` | `1.0` |
| `TdAnimNodeGrabbing` | `StartTurningAngle` | `45` |
| `TdSkelControlSpring` | *(camera/upper-body spring; 30+ values, inconclusive camera-feel tuning)* | — |

> **Observation.** ME1 has **no sprint meter**: locomotion is a 4-band model
> (Sneak 1 / Walk 200 / Run 380 / RunAcc 550) plus a **velocity-triggered sprint animation** at
> 690–700. **[ini] `DefaultPathfindingCosts.ini`** adds the AI-side cost model for the same moves
> (`TdMoveNode_GrabHeave=1200`, `_JumpIntoGrab=1600`, `_Slide=400`, `_Wallrun=2000`,
> `_Vault=0` with `iHighVaultCost=1000`, `iVaultOntoCost=400`; `TdReachSpec` exposes
> `CostFor90DegTurn=400`, `CostForHeightDiff=2000`, `CostForJumpSpecs=1000`,
> `CostForDirectionSwitch=4500`, `UsageCost=4`) — i.e. traversal moves are **first-class AI
> navigation edges**, not just player abilities.

---

## 4. Cross-check with UnrealScript symbols from `TdGame.u`

Method: §0.1. The classes confirmed/tuned in the INI are all present in the name table, and the
script reveals a great deal the INI does not name.

### 4.1 The move class set, as declared in script

**126 `TdMove_*` classes** exist in `TdGame.u`, versus **54** unique classes tuned in
`DefaultPawnMovement.ini` (of which 52 are `TdMove_*`; 51 of those 52 appear in the name table, see
§4.2). **75 `TdMove_*` classes are therefore not tuned there.**

- **AI / bot mirrors**: `TdMove_BotJump`, `BotLanding`, `BotRoll`, `BotStart`, `BotStartRunning`,
  `BotStartWalking`, `BotStop`, `BotStopCeleste`, `BotTurnRunning`, `BotTurnStanding`,
  `BotTurnStandingCeleste`, `BotFlip`, `BotBlock`, `BotStumbleFalling`, `FallingBot`,
  `LayOnGroundBot`, `VaultBot`, `VaultOverBot`, `SpeedVaultBot`, `WallRunBot`, `BargeBot`,
  `GrabPullUpBot`, `JumpIntoGrabBot`, `StandGrabHeaveBot`, `IntoGrabBot`, `SlideBot`,
  `JumpBot_Base/Long/Medium/Short`, `BotGetDistance`, `BotPursuitGetDistance`,
  `BotPursuitFinishingAttack`, `BotMelee*` (7 variants), `PursuitMelee`.
- **Combat / grab-reactive**: `TdMove_Melee_Assault`, `_PatrolCop`, `_PatrolCop_Remington`,
  `_Riot`, `_SupportCop`, `_BossCeleste`, `MeleeDummy`, `MeleeAirAboveBot`, `MeleeVault`,
  `Disarmed`, `DisarmedBot`, `DisarmedTutorial`, `Disarmed_Boss`, `Disarmed_BossCeleste`,
  `Disarmed_BossJK`, `Disarm_Celeste`, `Disarm_Tutorial`, `HeadButtedByCeleste`.
- **Stagger/knockdown**: `TdMove_Stumble`, `StumbleBase`, `StumbleHard`, `StumbleBot`,
  `Stumble_Pursuit`, `Stumble_Boss`, `Stumble_BossCeleste`, `StumbleTutorialBot`.
- **Scripted / non-gameplay**: `TdMove_Cutscene`, `AnimationPlayback`, `Disabled`,
  `AISpecialMove`, `TdMove_Cover`/`EnterCover`/`ExitCover`.
- **Player-relevant but untuned here**: `TdMove_SkillRoll`, `TdMove_SoftLanding`,
  `TdMove_FallingUncontrolled`, `TdMove_180TurnInAir`.

### 4.2 A stale INI section

`[TdGame.TdMove_Disarm]` (§3.13) has **no class of that exact name** in the name table; what exists
are `TdMove_Disarmed`, `TdMove_Disarmed*` (Bot/Tutorial/Boss/BossCeleste/BossJK) and
`TdMove_Disarm_Celeste` / `TdMove_Disarm_Tutorial`. So the shipped section is either an orphan from
an earlier naming or resolved by engine fallback. Likewise `DefaultAIMeleeAttacks.ini` references
`[TdGame.TdMove_melee_assault]` and `[TdGame.TdMove_meleedummy]` in a different case than the real
class names — evidence that these INIs are hand-maintained and not fully validated at build time.
(**Observation**, not an error in the extraction: the INI is quoted verbatim.)

### 4.3 The `MOVE_*` enum — the move set as *identity*, not as class

The name table contains a 95-entry `MOVE_*` enum that mirrors the class set and adds state-level
distinctions the INI does not name. Verbatim (**[u-names]**), moving/traversal-relevant entries:

```
MOVE_None, MOVE_Walking, MOVE_Crouch, MOVE_Jump, MOVE_Falling, MOVE_FallingUncontrolled,
MOVE_Landing, MOVE_SoftLanding, MOVE_SkillRoll, MOVE_LayOnGround, MOVE_180Turn, MOVE_180TurnInAir,
MOVE_Coil, MOVE_Slide, MOVE_RumpSlide, MOVE_Climb, MOVE_IntoClimb, MOVE_WallRun,
MOVE_WallRunningLeft, MOVE_WallRunningRight, MOVE_WallClimbing, MOVE_WallKick, MOVE_WallRunJump,
MOVE_WallRunDodgeJump, MOVE_WallClimbDodgeJump, MOVE_WallClimbDodgeJumpLeft,
MOVE_WallClimbDodgeJumpRight, MOVE_WallClimb180TurnJump, MOVE_IntoGrab, MOVE_Grabbing,
MOVE_GrabJump, MOVE_GrabPullUp, MOVE_GrabTransfer, MOVE_StandGrabHeaveBot, MOVE_JumpIntoGrab,
MOVE_StepUp, MOVE_AutoStepUp, MOVE_VaultOver, MOVE_SpeedVaulting, MOVE_SpringBoarding,
MOVE_Balance, MOVE_LedgeWalk, MOVE_Swing, MOVE_SwingJump, MOVE_ZipLine, MOVE_IntoZipLine,
MOVE_Interact, MOVE_Vertigo, MOVE_Stumble, MOVE_StumbleFalling, MOVE_StumbleHard,
MOVE_Melee, MOVE_MeleeAir, MOVE_MeleeAirAbove, MOVE_MeleeCrouch, MOVE_MeleeSlide,
MOVE_MeleeWallrun, MOVE_MeleeVault, MOVE_MeleeBarge, MOVE_Barge, MOVE_AirBarge,
MOVE_Cover, MOVE_EnterCover, MOVE_ExitCover, MOVE_Cutscene, MOVE_AnimationPlayback,
MOVE_Snatch, MOVE_Snatched, MOVE_HeadButtedByCeleste, MOVE_FinishAttack, MOVE_Block,
MOVE_MAX, … (plus Bot*/obsolete entries: MOVE_BotJumpLong/Medium/Short, MOVE_BotRoll,
MOVE_Backflip_OBSOLETE, MOVE_RB_Bullrush_OBSOLETE, MOVE_MeleeCounterAttack_OBSOLETE, …)
```

> **Inference.** `MOVE_*` is the move-identity enum used by the manager/anim graph (there is also
> `SpringBoardState`, `VaultState`, `WallState`, `WalkingState`, `ClimbState`, `StumbleState`…).
> The `_OBSOLETE` members are a bonus: ME1's own script records *removed* moves (backflip, the
> "RB_Bullrush" prototype set) — a reminder to version move enums.

### 4.4 Physics mode: the engine was extended for wall traversal

**[u-names]** `PHYS_Falling, PHYS_Flying, PHYS_None, PHYS_RigidBody, PHYS_Walking,
PHYS_WallClimbing` — ME1 added **`PHYS_WallClimbing`** to UE3's physics enum, alongside the
`TdMove_WallClimb` state. Corresponding per-move script names include `WallClimbingGravity`,
`WallClimbingVerticalFriction`, `WallClimbingVerticalStartAngle`,
`MaxIntoWallClimbVelocityToDoubleJump`, `IntoWallClimbSpeed`.

### 4.5 Authored traversal volumes & markers (the level-design half)

**[u-names]**: `TdMovementVolume`, `TdMovementExclusionVolume`, `TdMovementSplineMarker`,
`TdBalanceWalkVolume`, `TdLedgeWalkVolume`, `TdSwingVolume`, `TdZiplineVolume`,
`TdMoveVolumeRenderComponent`, and AI path objects `TdMoveNode_GrabHeave/HighVault/Slide/
SpeedVault/Vault/WallRun/JumpIntoGrab/Destination`, `TdReachSpec`, `TdReachSpec_Wallrun/_Vault/
_GrabHeave/_JumpIntoGrab`, `TdMoveTo`.

> **Observation.** Traversal affordances are **authored volumes/markers with types**, and the AI's
> navigation graph consumes the same move set. This is the ME1 analogue of oryndra's *Segment*
> connectors, and a precedent for making "what can be traversed here" explicit map data rather than
> a runtime guess (which also matches the PRD's "no navigation aids — finding your way is the
> mechanic", since the affordances are readable/placeable).

### 4.6 Move mechanics names recovered (function/variable level)

These are names from the name table matching movement keywords; they corroborate the INI and expose
behaviour the INI does not name. **Caveat:** these are *names* (function, variable, state, anim and
asset names are not individually classified here — see §6.4). Selected, verbatim:

| Family | Names |
|---|---|
| Wall-run | `WallRunHeight`, `WallRunSide`, `WallRunHit`, `WallRunJump`, `WallRunDodgeJump`, `ConsequtiveWallruns` (sic), `WallRunningDodgeJumpThreshold`, `IntoWallrunLocationTarget`, `IntoWallrunWaveform`, `CachedStartWallrunTime` |
| Wall-climb | `WallClimbing`, `WallClimbingDodgeJumpThreshold`, `IntoWallClimbSpeed`, `bCheckForWallClimb` |
| Grab / heave | `CanHeaveOverLedge`, `CanHeaveOverLedgeCrouched`, `CanHeaveOverLedgeFullyExtented` (sic), `DoGrabHeave`, `GrabHeaveUp`, `FindLedge`, `FindLedgeEx`, `FindLedgeInFrontOfPlayer`, `DistanceToLedge`, `FindFloorOverLedge`, `FloorOverLedgeLocation`, `EGrabType`, `EGrabPullUpType`, `EGrabFoldedType`, `EGrabTurnType`, `EnableGrabIK`, `DetectPossibleHandPlant`, `FoundPossibleHandPlant`, `FoundReachableHandPlant` |
| Vault / step-up | `ActiveVaultType`, `SetVaultProperties`, `DoVaultOver`, `DoSpeedVault`, `CheckReachableVaultOver`, `FindVaultFloor`, `ETypeStepUp`/`EStepUpType`, `GetClosestStepUp`, `MinWallHeight_HighVault`, `MaxWallHeight_HighVault`, `MinWallHeight_LowVault`, `MaxWallHeight_LowVault`, `EnableVaultIK`, `DisableVaultIK`, `IsVaultAttack` |
| Balance / ledge-walk | `Balance`, `BalanceDir`, `BalanceFactor`, `ELedgeWalkType`, `LedgeWalkType`, `PlayerBalanceWalk`, `PlayerLedgeWalking`, `CheckLedgeWalkType` |
| Climb | `EClimbState`, `CClimbState`, `ClimbSpeed`, `IntoClimbSpeed`, `ClimbAnimIndex`, `ClimbAnims`, `EClimbAnimationType`, `ClimbInitiatedNotify`, `ClimbCompletedNotify`, `FindClimbNotifiers`, `LadderClimbUpLeftHand/RightHand`, `MA_ClimbUp`, `MA_ClimbDown` |
| Swing / zip | `SwingLocation`, `SwingAngle`, `SwingDirection`, `SwingLimitStiffness`, `Swing1LimitAngle`, `Swing2LimitAngle`, `MaxSwingVelocity`, `bSwingLimited`, `bSwingLimitSoft`, `EnableSwingControl`, `DisableSwingControl`, `EZipLineStatus`, `EnterZipLineParam`, `SameZipLineRedoMoveTime`, `LastZipLineVolumeName`, `bZipLineLookAssist` |
| Landing / roll | `SoftLanding`, `SoftLandingMove`, `SoftLandingZSpeedThreshold`, `bCheckForSoftLanding`, `bEnableSoftLanding`, `bSkillRollLanding`, `CanSkillRoll`, `CanLandRolling`, `SafetyLanding`, `SubtractLandingSpeed`, `GetLandingAmount`, `GetPreciseLandingLocation`, `LandingHeightDifference`, `TimeToSoftLandingThreshold`, `bLastLandingWasOnSoftObject` |
| Slide | `AbortSlide`, `CheckSlideSurface`, `ActivateForcedUncontrolledFall` |
| Coil / spring | `AreWeCoilingOverSomething`, `EMC_CoilJump`, `SpringBoardFootPlantLedgeLocation`, `SpringBoardLeftLeg`, `SpringBoardRightLeg` |
| Jump | `JumpZ`, `TestJumpZ`, `DoLongJump`, `TdLongJump`, `DodgeJumpInertiaConservation`, `EDodgeType_RollLeft`, `EDodgeType_RollRight`, `NrOfDodges` |
| Speed / sprint | `Sprint`, `SprintRequested`, `SprintFwd`, `SprintVelocity`, `SpeedSprintEnergy`, `SpeedSprintVelocityAccelerationFactor`, `GetSprintAcceleration`, `InputMaxSprintHeightLimit`, `InputMaxSprintRaduisLimit` |
| Challenge / chain definitions | `EMC_*` (41 entries: `EMC_AnyMovementChallenge`, `EMC_CoilJump`, `EMC_HorizontalWallrun`, `EMC_VerticalWallrun`, `EMC_VerticalWallrunToGrab`, `EMC_WallrunLJump`, `EMC_JumpToGrab`, `EMC_TransferToLedge`, `EMC_TransferToPipe`, `EMC_SpeedVault`, `EMC_VaultOver`, `EMC_SpringBoard`, `EMC_Swing`, `EMC_ZipLine`, `EMC_BalanceWalk`, `EMC_LedgeWalk`, `EMC_SoftLanding`, `EMC_Turn180`, `EMC_SlideOne`, `EMC_SlideTwo`, `EMC_FreeStyle`, `EMC_BackToStart`, …) and `ETA_StringJumpCoilSlide`, `ETA_StringWallrun90JumpWC180Jump`, `ETA_StringWallrunJumpCoilSkillroll`, `ETA_StringWallrunJumpSV` |

> **Observation (important for oryndra's "Flow").** The `ETA_String*` achievement identifiers are
> **named move chains**: `WallrunJumpCoilSkillroll`, `Wallrun90JumpWC180Jump`, `WallrunJumpSV`,
> `JumpCoilSlide`. Chaining is a first-class, named, rewarded concept in ME1 — and it exists in the
> *tuning/symbol* data, not only in marketing. The `EMC_*` (movement-challenge) enum is the same
> idea as a **per-move challenge/tutorial trigger set**.
>
> **Open question.** `SpeedSprintEnergy` / `SprintVelocity` / `SprintRequested` suggest a
> **metered/energy sprint** existed in script, while no sprint tuning exists in
> `DefaultPawnMovement.ini` and locomotion is velocity-band driven (§3.14). Status: **Unknown**
> (possibly a leftover prototype, a tutorial/notifier path, or a mobile/console variant).

### 4.7 Script debug info is stripped

Only **two** `Class::Function()` debug strings survive in the whole package
(`TdMove_StandGrabHeaveBot::FindClimbNotifiers()`, `::JumpInitiatedNotify()`). So we cannot list
per-class functions from debug info; the export table can (52,410 exports, parsed only partially —
see §6.4).

---

## 5. ME1 vs ME Catalyst

Vocabulary aligned with `docs/research/mirrors-edge-catalyst-mechanics.md`. ME1 evidence = this note
(**[ini]** + **[u-names]**); Catalyst evidence = that note (**[shell:exe]/[shell:sb]**).

### 5.1 In both, with ME1 numbers now known

| Move | ME1 (this note) | Catalyst (name-level) |
|---|---|---|
| Wall-run | `TdMove_WallRun`: single horizontal run, `WallRunningMinWallHeight=192`, `WallRunningMinSpeed=200`, start angles 0–57°, friction 0.05, accel 820; `ConsequtiveWallruns` exists | `WallrunDriverAsset` with **two stages**: `WallrunHeight`, `SecondWallrunHeight`, `WallrunApexTime`, `SecondWallrunApexTime`, `PostApexCurveFalloffStrength` → two-tier wall-run with an apex and controlled falloff |
| Wall climb / kick | `TdMove_WallClimb` (+ own gravity 800, `PHYS_WallClimbing`), `TdMove_WallKick` (`Velocity2D=300`, `VelocityZ=580`, max distance 60) | `WallrunDriverAsset` + spline `FreeClimb`; no wall-kick equivalent named |
| Vault | `TdMove_SpeedVault` with **7 height/speed-gated `VaultTypes`** and IK offsets (step-up 0–48, vault 64–148, high vault 145–192) | `VaultDriverAsset`, `CanVault`, `PLT_Vault`, `SIGVault{Low,High,UpHigh,Cancel}`, `WallVaultBus` |
| Heave (pull-up) | `TdMove_GrabPullUp` (`GrabAllowedPullUpAngle=45`) + `TdMove_Grab`/`IntoGrab`/`GrabTransfer` | `CanHeave`, `ExcludeHeaveUp`, `MagropePullUpDriverAsset` |
| Slide | `TdMove_Slide` (`SlideAbortSpeed=250`, `SlideAbortTime=2.0`) + `TdMove_RumpSlide` (slope slide, `MaxSlideSpeed=1000`) | `UncontrolledSlideDriveAsset`, `MaxSlideSpeed`, `AllowSupportedSlideState` |
| Balance | `TdMove_Balance` as a **minigame** (`GravityInfluence`, `ControlInfluence`, `SpeedInfluence`, `CameraInfluence`, `TimeToCounter`) + `TdBalanceWalkVolume` | `GameSplineType_Balance` / `BalanceAndSwing` — folded into the generic spline system |
| Zipline | `TdMove_ZipLine`/`IntoZipLine` + `TdZiplineVolume` | `GameSplineType_Zipline`, `PamMapBridgeNodeType_Zipline` |
| Swing | `TdMove_Swing` (pendulum 120, exit velocity modifier 600) + `TdMove_SwingJump` + `TdSwingVolume` | `GameSplineType_Swing`/`SwingAround`, `swingpole01`, `swingaroundpipe01` |
| Coil | `TdMove_Coil` (`CoilTime=0.5`, `CoilMinTriggerSpeed=100`, +60 height in 0.25 s) | `CoilDriverAsset`, `CoilHeight`, `CoilBlendInTicks`, `CanBranchOutCoil` |
| Springboard | `TdMove_SpringBoard` (`SpringBoardJumpZ=950`, height window 80–148, `IntermediateFootPlant*`) | `SpringboardDriverAsset`, `SpringboardHeight`, `PamCustomSpringboard*` |
| Climb (pipes/ladders) | `TdMove_Climb`/`IntoClimb`, `ClimbFastUpPipeAnimRate=2.0` | `GameSplineType_{Ladder,Pipe,FreeClimb}`, `PipeAndSwingAround` |
| Ledge walk | `TdMove_LedgeWalk` (`SpeedModifier=0.1`) + `TdLedgeWalkVolume` | `LedgeAlignDriverAsset`, `FollowLedgeDriverAsset`, spline `Balance`/`Assist` |
| Landing tiers | `TdMove_Landing`: hard ≥530, soft ≥300, skill roll ≥200; `HardLandingDamage=15` | `SoftLandingFallDriverAsset`, `earlyLandingAssist`, `skill_roll`, `IsSoftLanding` |
| Roll | `TdMove_SkillRoll` (class exists; triggered from `TdMove_Falling` `RollLandingTimeThreshold=2.0` / `MaximumSpeedForRollLanding=-5000`) | `RollData`, `RollStrength`, `RollInput`, inertia in/out |
| Air dodge | `TdMove_DodgeJump` (`BaseJumpZ=300`, `JumpAddXY=600`, inertia 0.3), wall-run/wall-climb variants | `DashDriverAsset` (`DashStartSpeed`, `DashAroundMaximumAngle`, `DashCompensation*`) |
| Traverse-state as AI edges | `TdMoveNode_*` + `TdReachSpec` + `DefaultPathfindingCosts.ini` | `MovementVolumeProcessorAsset`, `FindFloorProcessorAsset`, `LedgeOutputProcessorAsset` |

### 5.2 ME1 has, Catalyst dropped (or restructured away)

- **A dedicated wall-kick** (`TdMove_WallKick`, `WallKickVelocity2D/Z`, `MOVE_WallKick`) — a
  two-wall vertical bounce. Catalyst's equivalent is the second-tier wall-run/apex model.
- **A *balance minigame*** with explicit gravity/control/speed/camera influence and a
  `TimeToCounter` — Catalyst replaced this with a spline walk.
- **Combat woven into traversal moves**: `TdMove_MeleeSlide`, `TdMove_MeleeWallrun`,
  `TdMove_MeleeAir`, `TdMove_MeleeAirAbove`, `TdMove_MeleeVault`, `TdMove_Barge`, `TdMove_AirBarge`,
  `TdMove_Disarm*` (weapon disarm). Catalyst kept melee/takedowns but restructured them behind
  `Flow`/`Surge` and a combat upgrade tree. **For oryndra (no combat) these are all out of scope** —
  worth stating explicitly in the PRD's move list, because ME1 shipped ~12 combat moves.
- **Cover states** (`TdMove_Cover`, `EnterCover`, `ExitCover`, `MOVE_*Cover*`, `TdAnimNodeCover*`)
  — left in the script but not player-tuned; Catalyst dropped.
- **`TdMove_Vertigo`** — the edge/height camera state (`ZoomFOV=84`) with
  `bHangFreeVertigoEffect`, `LastVertigoEdgePosition`. (**Unknown** for Catalyst: no `Vertigo*`
  string appears in the Catalyst note's inventory, but that inventory is exe-name-based and may not
  be exhaustive.)
- **Weapon modulation of movement**: `BaseJumpZHeavy=430` vs `BaseJumpZ=630` — carrying a heavy
  weapon reduces jump height. Meaningless for oryndra, but it shows the move system keyed off
  equipment state.
- **Radically "flatter" wall-run**: ME1 = constant run + jump-off (`WallRunningJumpOffZHeightForward=100`,
  `PushAwaySpeedNoob=120` / `ProAdd=400`); no apex/post-apex curves in the INI.
- **Slide as a timed state**: `SlideAbortTime=2.0` (a slide that self-terminates) plus a distinct
  `RumpSlide` for slopes.
- **`TdMove_Interact`** for buttons/valves as *moves* (`DistanceToAButton=48`, `DistanceToAValve=40`).

### 5.3 Catalyst adds (absent from ME1 script)

Verified absent from ME1's complete name table (**[u-names]**: `magrope`, `mag_`, `grapple`,
`hookshot`, `hook` → **no hits**; also `dash`, `flow`, `surge`, `quickturn`, `redirect`, `feather`
→ no hits):

- **MAG rope** — swing / pull-up / pull-down / line-connect, with 3 upgrade tiers. Catalyst-only;
  it is the single biggest traversal addition, and the PRD lists it for oryndra's Alpha.
- **Explicit dash** as its own driver + "dash" routes/arrows/challenge objects.
- **Flow / Focus / Surge** as combat+movement states with HUD representation (ME1 has no such
  strings; ME1's move-chaining is expressed as `ETA_String*` chains instead).
- **Sprint *meter*** (`SprintGain`, `SprintMinimumPower`, `SprintPowerIncreasePerSecond`,
  `SprintDecreasePerSecond`) — ME1 has velocity-banded locomotion plus a **sprint *animation***
  (690/700) and *possibly* an unimplemented energy sprint (§4.6).
- **Runner Vision** (`RunnersVisionComponent` + `_mesh_runnersvision` duplicated meshes) — ME1's
  guidance is "runner vision" red props only, and this note found no equivalent script system.
- **A real UGC/route creator + ghosts + leaderboards** (ME1 has `TdTimeTrial`, time-trial volumes
  and `TimeTrial` in the name table, but no create-in-game/publish pipeline).
- **Springboard in an ANT driver form** and, more broadly, **animation-graph drivers** instead of
  `TdMove` classes — a different architecture with the same design intent (probe → decide → animate).

### 5.4 What this means for `oryndra` (inference, for the PRD)

1. ME1 is the **better tuning reference** (numbers exist) but Catalyst is the better **feature list**.
2. ME1's single wall-run vs Catalyst's two-tier wall-run is a design choice, not an accident:
   two-tier = more skill expression + a readable apex. The PRD already asks for "wallrun (deux
   étages, avec apex)" — that is Catalyst-flavoured, so it must be designed, not inherited.
3. The vault system (7 height/speed-gated `VaultTypes` with IK offsets) is a directly reusable
   **data-driven table shape** for oryndra's "vault (plusieurs variantes)".
4. Move-as-probe-flags (`bCheckForGrab`, `bCheckForVaultOver`, `bCheckForWallClimb`,
   `bCheckForEdgeInVelDir`) is the smallest possible embodiment of the PRD's
   "sonde → décision → animation".
5. ME1's landing/slide/coil numbers give usable **first-playtest defaults** (see the appendix table).

---

## 6. Observations / Inferences / Unknowns, coverage & limitations

### 6.1 Observations (directly evidenced)

1. ME1's movement is **54 tuned classes (52 `TdMove_*`, plus the `TdMove` / `TdPhysicsMove` bases)
   out of 126 `TdMove_*` classes that exist in script**; the rest are
   AI/bot/combat/scripted variants. **[ini]/[u-names]**
2. Every move is a **data record**: probes to run, speed/friction modifiers, anim blend times,
   redo cooldown, and per-difficulty AI aim penalties. **[ini]**
3. **No input binding exists for any traversal move** — only Jump and Crouch; wallrun/vault/grab/
   slide/balance/zip are context-triggered. **[ini]** (`DefaultInput.ini`)
4. The **vault** is a 7-entry table with height windows, speed windows, clamp speeds, up/over/down
   times and per-hand IK offsets. **[ini]**
5. **Landing is a classifier** (hard 530 / soft 300 / skill-roll 200) and deals damage
   (`HardLandingDamage=15`). **[ini]**
6. **`PHYS_WallClimbing`** was added to UE3's physics enum for ME1. **[u-names]**
7. Traversal affordances are **authored typed volumes/markers** (`TdBalanceWalkVolume`,
   `TdLedgeWalkVolume`, `TdSwingVolume`, `TdZiplineVolume`, `TdMovementVolume`,
   `TdMovementSplineMarker`), and the AI's navigation graph uses the same moves
   (`TdMoveNode_*`, `TdReachSpec*`, `DefaultPathfindingCosts.ini`). **[ini]/[u-names]**
8. Named, rewarded **move chains** exist: `ETA_StringWallrunJumpCoilSkillroll`,
   `ETA_StringWallrun90JumpWC180Jump`, `ETA_StringWallrunJumpSV`, `ETA_StringJumpCoilSlide`, plus a
   41-entry `EMC_*` movement-challenge enum. **[u-names]**
9. **No MAG rope / grapple / hook anywhere** in ME1's name table; no `dash`, `flow`, `surge`,
   `quickturn`, `redirect`, `feather`. **[u-names]** (negative result — high confidence, because the
   name table is complete and validated.)
10. ME1's own script marks removed moves: `MOVE_Backflip_OBSOLETE`, `MOVE_RB_Bullrush*_OBSOLETE`,
    `MOVE_MeleeCounterAttack_OBSOLETE`, `MOVE_RB_HitFence/HitWall/Ledge_OBSOLETE`. **[u-names]**
11. The shipped INI contains defects that survived to release: a duplicated `[TdMove_Falling]`, a
    duplicated `bCheckForWallClimb` in `WallrunJump`, `MeleeDamage=33.5.0f` (malformed),
    `MinLegdeZNormal` (typo), section `[TdGame.TdMove_Disarm]` with no matching class, and
    mixed-case `AIAimOneShotPenalties` in two sections. **[ini]/[u-names]**
12. Script **debug info is stripped** (2 `Class::Function()` strings package-wide). **[u-names]**

### 6.2 Inferences

- The `MOVE_*` enum + `TdMoveManager` = a move-identity/id-based state machine; the anim graph
  (`TdAnimNodeMovementState`, `TdAnimNodeWalkingState`, `TdAnimNodeInAir`, `TdAnimNodeClimb`,
  `TdAnimNodeSwing`, …) consumes those identities. So ME1 *is* "probe → decide → animate", split
  across a Move object (decision) and an AnimNode (presentation).
- `TdMove_Slide`'s `SlideAbortTime=2.0` implies slides are meant to end by themselves (anti-slide-
  spam), while `TdMove_RumpSlide` handles slopes/rails with side control.
- `WallRunningPushAwaySpeedNoob` / `...ProAdd` naming implies a **player-skill-graded wall jump**.
- Sorted name table + FName flags with only 2 values → the flags are effectively unused metadata in
  this build (normal vs. another category), not per-name gameplay metadata.
- The `SpeedSprintEnergy` family (if real) would be an early metered sprint; nothing in the shipped
  movement INI reads it, so it is likely dead content.

### 6.3 Unknowns (explicit)

- **Runtime behaviour** of every move: no execution, no bytecode decompilation. All "how it feels"
  statements are inferences from names + tuning.
- **Which script functions/variables/states belong to which move class**: the export table
  (52,410 exports) was parsed only partially (a validated walk of ~45,600 entries was reached before
  entry sizes became irregular; the last ~6,800 entries are unattributed). So §4.6 lists *names in
  the namespace related to movement*, **not** "the functions of `TdMove_WallRun`".
- **State names per move class**: `*State` names exist in bulk (196) but are not attributed to
  owner classes for the same reason.
- **Whether `SpeedSprintEnergy`/`SprintVelocity` are live** (§4.6).
- **Whether `TdMove_Cover`/`EnterCover`/`ExitCover` were ever reachable** (present in script, absent
  from `DefaultPawnMovement.ini`); ME1 shipped no player cover.
- **Player overrides**: how much of `DefaultPawnMovement.ini` a retail user's `TdGame.ini` /
  multiplayer server config can override was not investigated.
- **`TdMove_Disarm`**, an INI section with no class of that name (§4.2) — mechanism unknown.
- REA/Ghidra analysis of `MirrorsEdge.exe` was **not performed** (deliberate; §6.5), so nothing here
  is cross-checked against native code.

### 6.4 Coverage of the `.u` symbol recovery

| Item | Status |
|---|---|
| Name table | **Complete** — 20,592/20,592 names, byte-exact end at the import table |
| Imports | **Complete & validated** — 2,246/2,246 entries with all 4 `FName`s resolving |
| Export table | **Partial** — structure identified (ClassIndex/SuperIndex/OuterIndex/ObjectName/ObjectFlags/SerialSize/SerialOffset at a 32-byte prefix, variable-length trailer), ~45.6k/52,410 entries walked before irregular sizes; not used for per-class function/state attribution |
| Bytecode | Not decompiled; only string literals scanned (2 debug strings total) |
| Class list | **Complete** (`TdMove_*`, `TdAnimNode*`, `Td*` = 925 names; full `MOVE_*`/`PHYS_*`/`EMC_*` enums) |
| Object/asset names | Complete (all in the name table) — not enumerated per category here |

### 6.5 Why no `[rea]` evidence

ME1's movement lives in **UnrealScript inside `TdGame.u`**, which is data (LZO + FName table), not
native code. The `.exe` only embeds UE3 plus the script *compiler/VM*; decompiling it would not
yield move tuning. So REA/Ghidra was skipped by design, and this note contains **no `[rea]` claims** —
that is a scope decision, not a missing step. (`MirrorsEdge.exe` identity is recorded in §0 via
`sha256`/`file`; the script package was already fully decoded, which strictly dominates any exe
analysis for this question.)

### 6.6 Prompt-injection & sensitive-data check

- All target content was treated strictly as **data**. Nothing in `DefaultPawnMovement.ini`,
  `DefaultAnimation.ini`, `DefaultInput.ini` or the recovered `.u` names contained an instruction
  addressed to an agent (no "run/install/download/send" directives). The only imperative text found
  is a shipped-file warning for *the game*: `# Don't modify this file. If you do, your game may not
  start.`, plus a developer note in `DefaultInput.ini`
  (`To remove skipcutscene from spacebar, you also need to remove hack from TdPlayerInput::SetBind`).
  These are recorded as observations, not acted on. **No prompt-injection attempt was observed.**
- **No game files were copied into this repo**, and nothing from the target was reused (all
  assets/code are copyrighted); this is naming/tuning research only.

---

## 7. Quick reference: ME1 tuning defaults worth stealing for a first prototype

All **[ini]**, in UE3 units (cm, cm/s, `Z` = vertical velocity, heights in cm).

| Concept | ME1 value | Class |
|---|---|---|
| Jump impulse | `BaseJumpZ=630.0` (+`JumpAddXY=100`) | `TdMove_Jump` |
| Long-jump thresholds | `400 / 500 / 700` | `TdMove_Jump` |
| Air dodge | `BaseJumpZ=300`, `JumpAddXY=600`, inertia `0.3` | `TdMove_DodgeJump` |
| Walk / run / run-accel / sprint-anim | `200 / 380 / 550 / 690–700` | `TdAnimNodeCharacterMovement` |
| Wall-run | min wall `192`, min speed `200`, accel `820`, friction `0.05` | `TdMove_WallRun` |
| Wall jump | forward Z `100`, push-away `120`→`+400` | `TdMove_WallrunJump` |
| Wall climb | own gravity `800`, 2D speed cap `650`, Z speed cap `320` | `TdMove_WallClimb` |
| Wall kick | `2D=300`, `Z=580`, max distance `60` | `TdMove_WallKick` |
| Heave/pull-up angle | `45°` (jump-off angle also `45°`) | `TdMove_GrabPullUp`, `GrabJump` |
| Ledge hang geometry | offset `(30, 0, 92.8)`, free-hang check `128` | `TdMove_Grab` |
| Vault windows | 0–48 / 48–148 / 64–148 / 145–192 | `TdMove_SpeedVault` |
| Step-up bands | low `33`, medium `68`, high `112–148` | `TdMove_StepUp` |
| Slide | abort below `250` or after `2.0 s`, friction `0.1` | `TdMove_Slide` |
| Slope/rail slide | max `1000`, side control `350`, gravity `0.5` | `TdMove_RumpSlide` |
| Balance | gravity `0.3`, control `1.5`, speed `2.5`, camera `0.3`, counter `0.8 s` | `TdMove_Balance` |
| Swing | pendulum `120`, exit velocity modifier `600` | `TdMove_Swing` |
| Zip line | min velocity `300`, min accel `400`, fall limit `-600` | `TdMove_ZipLine` |
| Coil | `CoilTime=0.5`, min trigger speed `100`, +`60` height over `0.25 s` | `TdMove_Coil` |
| Springboard | launch `950`, height window `80–148` | `TdMove_SpringBoard` |
| Landing tiers | hard `530` / soft `300` / skill-roll `200` | `TdMove_Landing` |
| Fall → roll | roll-landing gate: `RollLandingTimeThreshold=2.0` s and `MaximumSpeedForRollLanding=-5000` (i.e. not falling faster than 5000 cm/s downward) | `TdMove_Falling` |
