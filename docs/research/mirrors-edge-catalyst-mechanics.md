# Mirror's Edge Catalyst — mechanics & systems inventory

Fact-finding for the `oryndra` PRD (`PRD.md`). Goal: an inventory of **what systems exist** and
**what they are named internally**, for an *inspired* (not cloned) parkour game. This document
deliberately does **not** try to extract tuning numbers: the gameplay payloads are encrypted.

Target analysed: shipped PC build at `/data/SteamLibrary/steamapps/common/Mirrors Edge Catalyst/`
(Frostbite "Venice"/Frostbite 3, internal codename **Pamplona**, build 2016-06-16).

> **Evidence discipline / legend.** Every claim is tagged:
> - **[REA]** — REA (Ghidra 12.1.4) evidence, with an Evidence ID.
> - **[shell:sb]** — shell observation: cleartext asset-name tables inside `Data/Win32/*.sb`
>   superbundle listings (the *chunk payloads* are compressed/encrypted; the name tables are not).
> - **[shell:toc]** — shell observation: cleartext name tables in `Data/**/*.toc`.
> - **[shell:exe]** — shell observation: `strings` dump of `MirrorsEdgeCatalyst.exe`
>   (pre-extracted to `/tmp/opencode/mec_exe_strings.txt`; this is **not** REA evidence).
> - **[shell:initfs]** — shell observation: plaintext payloads embedded in `Data/initfs_Win32`
>   (an initfs that stores small config/Lua/XML files unencrypted).
> - Keyword **Observation / Inference / Unknown** marks epistemic status.
>
> All target strings are treated as **untrusted data**. See the prompt-injection note at the end.

## 0. Target facts (identity / build)

| Fact | Value | Evidence |
|---|---|---|
| Main executable | `MirrorsEdgeCatalyst.exe`, x86-64 PE, 87,786,928 bytes | **[REA]** `open_binary` (recorded) — sha256 `e9d76463766973964a97cd63bd64c1191a317ec6ed6753d82b36f4ba0da45136` |
| Build stamp | `2016-06-16`, `2016-06-16T17:16:09Z` | **[REA]** `list_strings` on `Engine.BuildInfo_Win64_retail.dll`, ID `ev_106ce1ad8a5e54e3add1fd278cb5c4acab7acbca440daaaf1bc2d00e3ba57ffa` |
| Internal names | `Pamplona`, `PamplonaFuture`, `monkey.pamplona` | **[REA]** same Evidence ID |
| DB manifest | `Database id="PamplonaFuture" family="Venice" pipelineTag="Pamplona" licensee="Pamplona"` | **[shell:initfs]** `Data/initfs_Win32` → `database.dbmanifest` |
| Package version | `1.0.3.47248`, contentID `1026480` | **[shell:toc]** `__Installer/installerdata.xml` |
| Other REA-opened artefacts | `dbdata.dll` sha256 `d09b2863…`, `dbdataEA.dll` `868d0a3e…`, `NvCameraSDK64.dll` `6d388391…` | **[REA]** `open_binary` |
| REA negative results | `dbdata.dll` contains **no** `Magrope` / `PamplonaFeature` strings → gameplay type names live in the `.exe`, not the data DLL | **[REA]** `search_strings` IDs `ev_7b926dd4aedfda7422ab1590917d96ecf8baba49a64b4685051556aaf0d9a0ee`, `ev_3917e4a5dea617b02ab3e48e77d19e593a3190bda1b218c16b7ca71d884039b0` |

**Coverage limitation.** Deep REA analysis of the 87 MB `.exe` **timed out** (`provider_timeout`)
for both `search_strings` and `binary_overview`. So the exe's internal strings below are shell
observations only. The two low-cost REA Evidence records above are the only REA Evidence for
*content*; REA did, however, fully open and hash the exe (identity confirmed).

---

## 1. Movement / traversal moves

### 1.1 Architecture: animation-driven "drivers" (ANT plug-in)

ME Catalyst does **not** ship a hand-rolled character controller. Movement is expressed as an
EA **ANT** animation state machine with a game-specific plug-in, `pamplonaplugins`.

- Internal source path: `C:\monkey\p\TnT\Code\External\EA\ANT\ANT_Plugins\dice\pamplonaplugins\dev\include\pamplonaplugins\expressions\movementexpressions.edf` **[shell:exe]** (Observed)
- Movement/obstacle logic is split into **Processors** + **QueryFilters** + **ResultBuses**, e.g.
  `AnalyzeClimbingProcessorAsset`, `AnalyzeObstacleProcessorAsset`, `MovementVolumeProcessorAsset`,
  `FindFloorProcessorAsset`, `SpringboardProcessorAsset`, `MagropeProcessorAsset`,
  `LedgeOutputProcessorAsset`, `PositionPredictionProcessorAsset`, and result buses
  `LedgeResultBusAsset`, `FloorResultBusAsset`, `SplineResultBusAsset`, `PositionPredictionResultBusAsset`,
  `WallVaultBus`. **[shell:exe]** (Observed)

**Inference:** each move is a "driver" (a timed transition into/out of an animation state) that
consumes the output of cheap geometric probes. This is the reusable idea: probe → decide → drive.

### 1.2 The move set, as named internally

Driver assets found (**[shell:exe]**, Observed):

| Family | Internal names |
|---|---|
| Ground run/walk | `WalkingDriverAsset`, `VelocityDriverAsset`, `GravityEquationDriverAsset` |
| Dash / burst | `DashDriverAsset`, `DashCompensationDriverAsset`, `DashCombatDriverAsset` |
| Falling | `FallingDriverAsset`, `SoftLandingFallDriverAsset` |
| Wall-run | `WallrunDriverAsset` |
| Vault | `VaultDriverAsset`, `VaultController`, `VaultControllerAsset` |
| Slide | `UncontrolledSlideDriveAsset` |
| Coil (spring-up) | `CoilDriverAsset` |
| Springboard | `SpringboardDriverAsset`, `SpringboardProcessorAsset` |
| Ledge grab/hang | `LedgeAlignDriverAsset`, `IntoLedgeAlignDriverAsset`, `FollowLedgeDriverAsset` |
| Spline entry/follow | `SplineDriverAsset`, `FollowSplineDriverAsset`, `IntoSplineDriverAsset` |
| MAG rope | `MagropePullUpDriverAsset`, `MagropeSwingDriverAsset`, `SwingJumpDriverAsset` |
| Alignment/IK | `AlignmentDriverAsset`, `AlignmentCollisionDriverAsset`, `SmoothAlignmentDriverAsset`, `WarpTrajectoryDriverAsset`, `FootPlantEventDriverAsset` |

**Caution (do not misattribute):** the `Mantle*` strings in the exe
(`MantleEnable`, `MantleDispatchJobEnable`, `MantleReZEnable`, `MantleDisplaySettings`…) are the
**AMD Mantle graphics API**, not a parkour move. Likewise `Slider*`/`Roll*` in engine context is
mostly UI or camera roll. The parkour "pull-up over a ledge" is named **`CanHeave` / `ExcludeHeaveUp`**
(and `MagropePullUp`). **[shell:exe]** (Observed)

### 1.3 Per-move subsystems (properties that reveal behaviour)

**Wall-run** **[shell:exe]** (Observed):
`ConsecutiveWallruns`, `MinWallrunLength`, `MaxWallrunLength`, `WallrunHeight`,
`WallrunPostApexHeight`, `PostApexCurveFalloffStrength`, `VerticalPostApexCurveFalloffStrength`,
`SecondWallrunHeight`, `SecondWallrunPostApexHeight`, `SecondPostApexCurveFalloffStrength`,
`WallrunTime`, `WallrunApexTime`, `AlignTime`, `SecondWallrunTime`, `SecondAlignTime`,
`SecondWallrunApexTime`, `MaximumAngleDelta`, `CanWallrunOnSurface`, `ExcludeWallrun`,
`CantWallrun`, `RelayWallrunType`.
→ Observation: a **two-stage wall-run** (first + second, each with its own height/apex timing) with
configurable post-apex falloff. Progression icon `move/double_wallrun` **[shell:sb]**.

**Vault / heave** **[shell:exe]** (Observed): `CanVault`, `CantVault`, `ExcludeVault`,
`ObstacleCanVault`, `CanHeave`, `ExcludeHeaveUp`, `PLT_Vault`, `SIGVaultHigh`, `SIGVaultLow`,
`SIGVaultUpHigh`, `SIGVaultCancel`, `SIGVaultPosition{X,Z}`, `WallVaultBus`. Progression icon
`move/jumpvault` → `movejumpvault` **[shell:sb]**.

**Slide** **[shell:exe]** (Observed): `UncontrolledSlideDriveAsset`, `PamUncontrollableSlideData`,
`PamClientUncontrollableSlideEntity`, `IsUncontrollableSlide`, `MaxSlideSpeed`,
`AllowSupportedSlideState`. Progression icon `move/extended_slide`; sound bank
`me_footstep_*slide*` (per material) **[shell:sb]**.

**Sprint (with a power/stamina meter)** **[shell:exe]** (Observed):
`SprintSetting`, `IsSprinting`, `AllowContinousSprinting`, `StopSprintingWhenReleasingSprint`,
`StopSprintingWhenReleasingThrottle`, `SprintGain`, `SprintMultiplier`, `SprintMinimumPower`,
`SprintPowerIncreasePerSecond`, `SprintPowerDecreasePerSecond`, `SprintReleaseTime`.
→ Observation: sprint is **metered** (power rises while sprinting-eligible, falls otherwise, with a
minimum threshold and release time) — not a simple boolean.

**Jump & landing assist** **[shell:exe]** (Observed): `JumpSprintMultiplier`, `JumpOutHeight`,
`JumpOutSpeed`, `JumpOutUseLookDirection`, `AllowedJumpAssistDistance`, `JumpAssistPenalty`,
`HasJumpLedge`/`JumpLedgePosition`/`JumpLedgeNormal`, `EarlyLandingAssistLookAheadSegments`,
`EarlyLandingAssistLookAheadTime`, `EarlyLandingAssistTraceDistance`, `EarlyLandingAssistMaximumDistance`,
`EarlyLandingAssistWillHang`, `MinimumJumpHeight`, `MinimumForwardSpeed`. Landing:
`SoftLandingCenter`/`Radius`/`Guidance`, `IsSoftLanding`. Progression icon `move/soft_landing_night`.

**Roll** **[shell:exe]** (Observed): `RollData`, `RollStrength`, `RollSoftness`, `RollInput`,
`RollDeadzone`, `RollLag`, `RollInertiaInDuration`/`RollInertiaOutDuration`/`RollInertiaMinRatio`,
`RollWithTargetOptionControl`. Progression icon `move/skill_roll`; audio `me_faith_cloth_roll_land`.
→ **Inference:** "skill roll" is a landing technique (reduces impact / preserves speed).

**Coil** **[shell:exe]** (Observed): `CoilDriverAsset`, `CoilHeight`, `CoilBlendInTicks`,
`CanBranchOutCoil`; progression icon `move/coil`. → **Inference:** a spring/launch move
(a targeted upward leap); exact behaviour unconfirmed.

**Dash** **[shell:exe]** (Observed): `DashStartSpeed`, `DashEndSpeed`, `DashStartPosition`,
`DashDuration`, `DashAroundMaximumAngle`, `DashAroundMaximumHoldAngle`, `CompensationTime`,
`CompensationSpeed`, `CompensationExitSpeed`. Also `ConceptBattledashToggle`,
`PamPlayerTagPrio_DashCircle`, and world props
`objects/props/runnerobject/dasharrows/{dash_start,dash_end,dash_arrowsmall,_medium,_large,_rightlarge,checkpointcircle}`
**[shell:sb]**, plus UI sounds `me_dash_start`, `me_dash_complete`, `me_dashtimer_loop`,
`ui_dash_stars_result` **[shell:sb]**.
→ **Observation:** "Dash" is overloaded — a *movement burst* (and a combat variant) **and** a named
*challenge/route object* placed in the world with its own arrows and star rating.

**Traversal splines** — `GameSplineType_*` **[shell:exe]** (Observed):
`Assist`, `Balance`, `BalanceAndSwing`, `FreeClimb`, `Generic`, `Ladder`, `Pipe`,
`PipeAndSwingAround`, `Swing`, `SwingAround`, `Zipline`; plus `ExtendedSplineType`,
`PreviewSplineTypeFlags`, `PamGameSpline`, `ActiveSpline`.
→ Observation: balance-beams, pipes, ladders, free-climb walls, ziplines and swing-bars are all
**one spline system** with a type flag; props mirror the types
(`gameplay/movement/{balance/pipes,climbing/ladders,climbing/pipes,climbing/newpipes,swing,swingaround}`) **[shell:sb]**.

**MAG rope** **[shell:exe]** (Observed): `PamMagrope`, `PamMagropeValueAsset`, `MagropeTrigger`
(`_Box`/`_Sphere`), `CanFindMAGRopeLineConnector`, `CanFindMAGRopePullUp`, `CanFindMAGRopePullDown`,
`CanFindMAGRopeSwing`, `ClosestMagRope`, `IsPlayerSwinging`, `RopeLength`, `RopeStartSlack`,
`RopeIsStretched`, `MagropePosition`, `MagropeNormal`, `MinimumAngleMagropePullUp`/`PullDown`/`Swing`,
`FBMagropeType_Swing`, `ActivateMagropeTag`, `isMagRopeInRange`, `MagRopeMapMarkerDefinition`,
`PamClientMagropeEntity`, `PamClientMagropeRenderEntity`, `PamClientMagRopeVisualizerEntity`,
`PamMagRopeRenderJoint`, `PamMapBridgeNodeType_MagSwing`, `PamMapBridgeNodeType_Zipline`.
Assets: `characters/faith/armgear/magrope/{magrope_skin, magrope_pickup_1..3_skin, unlock_faith_magrope, unlock_faith_magr_pickup_1..3}`,
`skeletons/magrope_ske`, `animations/prefabs/misc/pf_lookatmagrope` **[shell:sb]**; mission
sublevel `savantextraordinairegetrope` **[shell:toc]**.
→ Observation: the MAG rope is **three upgrade tiers** on an `armgear` slot + a cosmetic skin, with
four interaction modes (swing / pull-up / pull-down / line-connect). Progression icons
`gear/magrope_1`, `gear/magrope_upgrade_1`, `gear/magrope_upgrade_2_1`,
`icons/gearmagrope{pullup,pulldown,swing}` **[shell:sb]**.

**Springboard** **[shell:exe]** (Observed): `SpringboardDriverAsset`, `SpringboardProcessorAsset`,
`PamCustomSpringboard*`, `SpringboardHeight`, `SpringboardAllowed`, `SpringboardPosition/Normal`,
`SpringboardObstacleType`, `SpringboardTimestamp`, `PamCustomArcType_Springboard`. Asset family
`gameplay/movement/reference/csg_springboardmeasure01`; props `objects/props/runnerobject/springboards/*`
**[shell:sb]**. Progression icon `move/springboard`.

**Fences & gates (contextual interactables)** **[shell:exe]** (Observed):
`PamplonaFenceSceneOp`, `PamplonaGateOpenerSceneOp`, `GateAsset`, `GateToOpen`, `ShouldOpen`,
`CopyPoseGate`, `DefaultState`. → **Inference:** vaultable fences / openable gates are authored ops,
not generic physics.

**Hand-plant / foot-plant** **[shell:exe]** (Observed): `PamplonaHandplantSceneOpAsset`,
`PamplonaHandplantState`, `EnableHandplantLeftIK`/`RightIK`, `LeftHandplantEffector*`,
`EnableHandplantOrientationEffector`, `PamplonaFootplantSceneOpAsset`, `EnableFootplanting`,
`LeftFootPlantTag`/`RightFootPlantTag`, `FootSpeedTagThreshold`, `FootHeightTagThreshold`.
Prop sound: `objects/props/runnerobject/swingpole01/*` **[shell:sb]**.

### 1.4 Move tracking / sequences (telemetry & reward)

**[shell:exe]** (Observed): `PamClientMoveTrackerComponent`, `PamClientMoveTrackerEntity`,
`PamMoveTrackerMoveCondition`, `MoveTrackerAsset`, `PamClientMovementMetricTweakEntity`,
`PamMovementMetricTweakData`, `PamTelemetryMove`, `TelemetryMoves`, `MaxMoveSequences`,
`PamMoveSequence`, `PamMoveSequencePart`, `PamMoveSequenceTrackerEntity`, `PamMoveSeqRewardCondition`,
`PamMoveSeq`, `PamClientReplayRecordingComponent`, `PamClientReplayMarkerEntity`,
`PamClientReplayPlayerComponent`, `PamReplayAsset`, `PamReplayMarkerEntityData`,
`gameplay/movement/pf_replay`, `gameplay/social/pf_replaymarker`,
`gameplay/social/pf_skillreplaymarkerwithautoplay`, `gameplay/social/pf_socialcontent` **[shell:sb]**.
→ Observation: there is a first-class **move-sequence tracker** (records named move chains, can
reward them, and drives replay/highlight markers). UGC challenge props:
`objects/props/runnerobject/dasharrows/checkpointcircle` **[shell:sb]**.

---

## 2. Player-state & guidance systems

### 2.1 Runner Vision (the guidance layer)

**[shell:exe]** (Observed): `RunnersVisionComponent`, `runnersVision`, `RunnersVisionEnable`,
`RunnersVisionArrow`, `RunnersVisionColor`, `runnersVisionColor`, `runnersVisionAlphaMaskTexture`,
`runVis_fillAmount`, `RunnersVisionMode`, `RunnersVisionNavTypes`, `RunnersVisionIconStyle`,
`RunnersVisionTarget`, `RunnersVisionTargetIconShape`, `RunnersVisionTargetStickyIconStyle`,
`RunnersVisionVariation`, `ActiveRunnersVisionObjects`, `CanBeRunnersVisionTarget`,
`DrawRunnersVisionDebug`, `RunnersVisionAsyncClearEnable`, `StickyIconsRunnersVisionArrow`,
`StickyIconsRunnersVisionIconStyle`; gameplay entities `PamClientRunVisTrailRenderEntity`,
`PamRunVisTrailRenderEntityData`, `PamRunnersVisionNavigationMasterEntity`,
`PamRunVisMapRendPrimBuilder`, `PamRunVisMapRendPrimDraw` (Runner Vision on the *map*); navigation
master entity; `PamClientMapBeaconPositionSelector` / `PamMapBeaconPositionSelectorEntityData` /
`UIHudIcon_RadioBeacon`.
Config: `WorldRender.RunnersVisionEnable 1`, `WorldRender.RunnersVisionAsyncClearEnable 1` **[shell:initfs]** (`Scripts/Game.cfg`).
Assets **[shell:sb]**: the reflex layer is a **duplicated mesh variant** — suffix `_mesh_runnersvision`
on traversable props (e.g. `climbpipe_512_cluster_mesh_runnersvision`,
`anc_climbing_module_1_mesh_runnersvision`, `dronesnooperbody_mesh_runnersvision`); it is also a
**player-customizable projection**: `characters/customizations/projection/runvis/{customization_projection_runvis, unlock_faith_projection_runvis, basevariations/ov_faith_proj_runvis_base}`.

Projections (ECHO-style visual filters) **[shell:sb]**: `basevariations`, `chaoslines`, `ghost`,
`halftone`, `rain`, `runvis`, `scanline`, `streams`, `triangles`.
→ Observation: "Runner Vision" = (a) a navigation/prompt component, (b) a render pass, (c) a
duplicated-mesh authoring convention, and (d) a cosmetic projection. Inference: affordance is
pre-authored (costly) rather than runtime-inferred.

### 2.2 Flow (movement *and* combat state)

**[shell:exe]** (Observed): `Flow`, `FlowMode`, `InFlow`, `IsInFlow`, `InFlowDamageMap`,
`FlowAttackDamage`, `FlowAttackPowerAttackUnlock`, `FlowAttackSpecialPowerAttackUnlock`,
`InputFlowOrComboAttack`, `IsFlowAttackUnlocked`, `InputMoveEnemyAttack`,
`MoveEnemyPowerAttackUnlock`, `MoveEnemySpecialPowerAttackUnlock`, `MoveAttackDamage`,
`ComboAttackDamage`, `DamageFlowAttack`, `IsSpeedyAttack`, `ForceFatalAttack`,
`PamFlowAction`, `PamFlowEntityData`, `PamFlowRewardCondition`, `PamFlowUIEventMessage`,
`NetworkPamFlowUIEventMessage`, `PamAIKillCompareType_WhileInFlow`, `PamUIElementFlowIconsEntity`,
`PamUIElementFlowIconsEntityData`, `IsDisarmDisabled`, `FlowDirection`, `FlowDistance`.
Also `PamClientSurgeLogicEntity`, `PamSurgeLogicData`, `IsSurgeActive`, `SurgeBuildup`,
`SurgeDecay`, `SurgeDuration`, `SurgeIncrement`, `TakedownSurgeAmount`.
→ Observation: **Flow** is a runtime battle/movement state that gates stronger attacks and scales
damage; it is separately surfaced in the HUD (`PamUIElementFlowIconsEntity`). **Surge** is a
charge meter that builds (e.g. via takedowns) and decays over time.
→ **Inference:** `Flow` in ME Catalyst is authored as a *combat* state; the movement-"flow" reading is
largely a design interpretation. (Distinct from Frostbite's unrelated `StateFlow`/`FlowGraph` and
the level-authoring `_flow` content layers.)

### 2.3 Combat & takedowns

**[shell:exe]** (Observed): `PamMeleeManager`, `PamMeleeParameters`, `MeleeProcessorAsset`,
`MeleeQueryFilterAsset`, `PamCreatureMeleeBinding`, `RangedCombatProcessorAsset`,
`CombatDropEvaluatorEntity`, `PamCombatImpactEffectEntity`/`SoundEntity`, `FinisherTag`,
`PlayFinisherOnDeath`, `PamFinisherCameraDirectorTrack`/`Data`, `PamFinisherCameraOriginTransformLayer`,
`PamFinisherTimelineEntity`/`Data`, `TakedownMaterial`, `TakedownSurgeAmount`,
`PamNetworkAIKillType_{Melee,Takedown,Weapon}`, `IsDisarmDisabled`, `RelayDisarm`,
`DisruptorGloveProcessorAsset`, `PamClientGloveDisruptorReceiverEntity`, `HasDisruptorDestroyUnlock`,
`PamDamageGameState` (`Amount`, `HeartAmount`, `AllowLastChance`, `IsFatal`, `IsFromBehind`, `Category`,
`ZoneAsset`, `StumbleTypeAsset`), lock-on (`IsInLockOnMode`, `LockOnTargetPosition/Id/Time`,
`ComputeLockOnTargetPositionProcessorAsset`), weapons (`PamCharacterWeapon*`,
`PamWeaponAimingEntity`, `PamWeaponFiringComponent`), `PamHeliWeaponType_*`.
Assets **[shell:sb]**: `ui/art/map/runnerbeatdown*` (+`_outline`, `_stickyinview`),
`ui/art/hud/{greenorb,focusgradient,lens,arrowcompass,cityalert,hexagon_outlined,hexagon_filled}`,
sounds `me_runnerbeatdowns_*`, `melee/disarmslidefront2`.
→ Observation: melee/takedown combat with contextual "finisher" timelines+directed cameras, a
**disruptor glove** (anti-drone/anti-weapon tool), lock-on targeting, and a Runner Vision-styled
"beatdown" prompt. `heart`-based health with a "last chance" state.

### 2.4 Health, tags, tutorials

- Health: `HealthComponent`, `ClientGameplayControllableLowHealthMessage`,
  `EndCriticalEffectHealthThreshold`, `DisableRegenerateHealth`, `AllowLastChance`, `HeartAmount`. **[shell:exe]**
- Player tags (social billboards): `PamClientPlayerTagComponent`, `PamPlayerTagsDefinitionAsset`,
  `PamPlayerTagLayer`/`Image`/`Prio`, `PamPlayerTagPrio_{Default,DashCircle,BigBillboard,MegaBillboard,Screen,UI}`,
  `getPlayerTags`, `PamBackendTextMessageType_PushNotificationPlayerTagUpdate`; asset dir
  `ui/art/playertags/{frames,details}` (`details/ghost`) **[shell:sb]**. **[shell:exe]**
- Tutorial: `PamUINotificationType_Tutorial`, `TutorialDummy`; asset `pf_tutorial_pipeclimb_672_red`. **[shell:exe]/[shell:sb]**

---

## 3. Game modes & progression

### 3.1 User-generated content — internally "**Shared Content**" / "**UGC**"

**[shell:exe]** (Observed) — full API family:
`PamClientSharedContentManagerEntity`, `PamClientSharedContentInfoEntity`,
`PamClientSharedContentMarkerEntity`, `PamClientCreateSharedHighlightEntity`, `IsSharedContent`,
`PamGatewayBackend{Upload,Download}SharedContent*`, `PamGatewayBackendDownloadSharedContentDataRequest/Result`,
`PamGatewayBackendDownloadSharedContentLeaderboard*`, `PamGatewayBackendDownloadSharedContentReplayUrl*`,
`PamGatewayBackendSetSharedContent{Published,Bookmark}*`, `PamGatewayBackendSharedContent{Deleted,Renamed,BookmarkUpdated}Message`,
`PamGatewayBackendGetSharedContentByUser*`, `PamGatewayBackendGetPromotedUGC*`,
`PamGatewayBackendGetPlayerUGCLimits*`, `PamGatewayBackendRemoveSharedContentFromCacheMessage`,
`PamGatewayBackendIncrementSharedContentCounterMessage`,
`PamBackendTextMessageType_Event{Created,Finished}SharedContent`,
`PamBackendTextMessageType_EventNewRecordSharedContent`,
`PamBackendTextMessageType_PushNotificationUgc{Deleted,Renamed}`,
bookmarks (`addUGCBookmark`, `PamBookmarkListRow`), profanity/ownership gates
(`ID_SGM_UGC_CONTAINS_PROFANITY`, `ID_SGM_UGC_NOT_OWNED`, `ID_SGM_TOO_MANY_UGC`,
`ID_SGM_TOO_MANY_PUBLISHED_UGC`). Config `UI.ShowSocialContentInMenu`, `PamDynamicContentBackend.DbServer`,
`PamDynamicContentBackend` **[shell:initfs]**. Assets **[shell:sb]**:
`gameplay/social/pf_l_createtimetrial`, `gameplay/social/pf_l_createreachthis`,
`gameplay/social/pf_socialcontent`, `gameplay/social/dynamiccontent{definitions,blueprints}`,
`ui/prefabs/pf_l_ui_showmappanel_challenges{byplayer,recentlyplayed}`.

→ Observation: players **create timed routes in-game** (markers + checkpoints) and publish them as
UGC with publish/unpublish, bookmarks, follower notifications, per-player UGC quota, and moderation
hooks. Two authored route kinds appear: **Time Trial** (`pf_l_createtimetrial`, `pf_timetrialmarker`,
`pf_timetrialcheckpoint`, `/timetrial/`, `/userTimeTrials`, `/playerTimeTrials`) and
**Reach This** (`pf_l_createreachthis`, `gameplay/social/reachthis/pf_reachthismarker`, `/reachthis/`,
`/userReachThis`, `/playerReachThis`) **[shell:sb]/[shell:exe]**.

### 3.2 Dash / Time Trial — the measurable activity

**[shell:sb]/[shell:exe]** (Observed): world props `objects/props/runnerobject/dasharrows/*`
(`dash_start`, `dash_end`, `dash_arrow*`, `checkpointcircle`); UI sounds `me_dash_start`,
`me_dash_complete`, `me_dash_number_counter`, `me_dash_star_flash`, `me_dashtimer_loop`,
`ui_dash_stars_result`, `ui_dashintrocamera`, `ui_dash_start`; markers
`PamClientChallengeMarkerEntity`, `PamClientCheckpointEntity`, `PamChallengeMarkerEntityData`,
`PamCheckpointEntityData`, `PamChallengeCheckpointOrder_{InOrder,AnyOrder,None}`,
`PamDynamicContentType_{Checkpoint,CheckpointChallengeMarker,GenericChallengeMarker,HighlightMarker,DeathHighlightMarker}`.
Config: `UI.ShowFreeCamInMenu` (comment: *"create timetrial"*), `BeatYourRecordNewsSid`,
`trialServiceName`. Map filter `settings/profile/opitionfilter_{dash,timetrial}`.
→ Observation: Dash/Time-Trial routes are **3D world objects** with start/end/checkpoints and a
**star rating** (star flash / star result sounds) + a recap camera. Order can be enforced or free.

### 3.3 Named Challenges, leaderboards, ghosts, Runner's Route

**[shell:exe]** (Observed):
- `PamNamedChallenge*` — `Data`, `DatabaseAsset`/`Entry`, `DivisionsAsset`, `Divisions`,
  `EntityData`, `InfoEntityData`, `MapIconData`, `MapMarker`, `NPCStats`, `Property`,
  `RewardCondition`, `SelectorEntity`, `Stat`, `StatsData`, `TransformOverride`, `TypeData`,
  `TypeCompareEntity`, `IsNamedChallenge`, `PamNamedChallengesAsset`.
- Leaderboards: `PamLeaderboardListRow`, `PamLeaderboardScope_{Global,Geo,Friends,Local,Overview,Invalid}`,
  `PamClientUILeaderboardWidgetDataProviderEntity`, `...ChallengeLeaderboard...`,
  `...NamedChallengeLeaderboard...`, `PamLeaderboardScope`, `/leaderboard`, `/division`, `/percentile`,
  `/globalLeader`, `/totalCount`.
- Ghosts: `PamGatewayBackend{GetPlayerGhosts,SetPlayerGhost}*`, `PlayerGhost`,
  `ClientState_WaitingForGhosts`, `PamGhostInRvColorBurnPass`; config `Network.MaxGhostCount 8192` **[shell:initfs]**.
- **Runner's Route**: `PamGatewayBackendGetPlayerRunnersRoutePercentiles*`,
  `SetRunnersRouteReplay`, `GetRunnersRouteReplayUrl*`, `PamClientUIRunnersRoutePercentilesProviderEntity`,
  `PamUIRunnersRoutePercentilesProviderEntityData`, `gameplay/social/pf_runnersroute`,
  `pf_l_runnersrouteanticheat`, sound `runnersroutedirection`.
→ Observation: leaderboards are **scoped** (global/geo/friends/local/overview) with divisions and
percentiles; ghosts are downloadable; "Runner's Route" exposes the fastest players' route as a
percentile-ranked replay. There is explicit **anti-cheat** logic for those routes.

### 3.4 Missions, districts, collectibles, progression

- Missions: `PamMissionManagerEntityData`, `PamMissionEntityData`, `PamMissionDescription*`,
  `PamMissionObjectiveListRow`, `PamMissionStatusEntityData`, `PamMissionStartPointAsset`,
  `PamProgressionMissionType_{Bronze,Silver,Gold,Misc,Debug}`, `PamMissionAwardsStatusMessage`,
  `PamProgressionMissionCompletedRewardCondition`, `UI.ShowDebugStartPointSelector`. **[shell:exe]**
- Districts & world progression: `PamClientMapDistrict{Manager,Part,Reveal,Tracker,ProgressionFlagGroup}Entity`,
  `PamMapDistrictDefinitionAsset`, `PamNavigationMapHistoryEntity`, `CityUnlockState`,
  `IgnoreDistrictUnlock`, `PamplonaDistrictChangedMessage`. **[shell:exe]**
- Collectibles **[shell:sb]/[shell:exe]**: `gameplay/collectibles/{pf_collectibleitem_green*}`,
  `PamFlagsCollectionAsset`, `ui/art/map/documents/*` (documents), `gameplay/collectibles/pf_lairphotolocation`,
  `objects/architecture/unique/trainstation/thelair_photos/photoatlas_polaroid*` (findable photos).
- **Achievements**: `PamAchievements`, `PamAchievementData`, `PamAwardStatus`, `PamAwardGainedMessage`. **[shell:exe]**
- **City Alert** (the pursuit/alarm system): `PamCityAlertManagerData`,
  `PamCityAlertTelemetryEventEntity`, `gameplay/cityalert/cameras/*` (194 camera assets),
  `ui/art/hud/cityalert*`, `ui/art/hud/cityalertscannermask`, and level sublevels
  `*/cityalert` (anchor/downtown/rezoning/trainstation/theview) **[shell:sb]/[shell:toc]**.
  → Observation: pursuit is a first-class system with surveillance cameras, not a scripted set-piece.

### 3.5 Runner Kit — the unlock / progression tree

**[shell:exe]** (Observed): `PamClientRunnerKitEntity`, `PamClientRunnerKitManagerEntity`,
`PamRunnerKitDefinitionsAsset`, `PamRunnerKitTypeData`/`Meta`, `PamRunnerKitRewardMeta`,
`PamGrantedRunnerKitItemInfo`, `PamRunnerKitGrantKitMessage`, `PamRunnerKitLicenseMapAsset`,
`PamLisenceRunnerKitGuidMappingAsset`, `settings/licenserunnerkitguidmapping`,
`PamUIRunnerKitDataProviderEntity`, `PamSocialPlayRunnerKitOpenedMessage`, `gameplay/social/runnerkit/pf_runnerkit`.

The upgrade tree is enumerable from the progression icon assets **[shell:sb]**:
- **Moves**: `move/{wallrun, double_wallrun, coil, extended_slide, skill_roll, quickturn, shift,
  combat_shift, springboard, swing_bar, balance_hang, freerunning_1, freerunning_2, climb_efficency}`,
  `icons/{movecoil, moveshift, movecombatshift, movedoublewallrun, movequickturn, movejumpvault,
  moveclimbbalance, movefocusflow}`.
- **Focus tiers**: `move/focus_01`, `focus_02`, `focus_03`, `focus_traversal`.
- **Gear**: `gear/{magrope_1, magrope_upgrade_1, magrope_upgrade_2_1, disrupt, disrupt_ai,
  disrupt_range, gridleak_mapping, ksec_surveillance_tap, power_upgrade, beat_link, beat_map}`,
  `icons/{gearbeatlink, gearbreakmechs, geardisruptbattery, geardisrupthumans, geardisruptmechs,
  gearextrahealth, gearglove, geargridleakmapping, gearmagrope{pullup,pulldown,swing}}`.
- **Combat**: `icons/{combatheavytraversaldmg1/2, combatimpactattack, combatlighttraversaldmg1/2,
  combatlighttraversalfocus1, combatpositionaladvantage, combatstumbleknock, combatswiftattack,
  combatswitchplace}`, `combat/{combatrecovery, combat_shift_and_positional_advantage,
  heavy_attack_*, light_attack_*, impact_switch, stamina_01..04, stun_advantage}`.
→ Observation: progression is a **kit of unlocks** split into *movement*, *focus*, *gear* and
*combat* nodes. Movement and gear are **orthogonal axes** (e.g. MAG rope has both gameplay tiers
`upgrade_1/2` and a cosmetic `magrope_skin`).

---

## 4. World / editor / content systems

- **FrostEd / editor config** **[shell:initfs]** (`database.dbmanifest`):
  `EditorGridAsset = Systems/EditorGrid/EditorGrid`, `MatOptShaderAsset = Systems/Pamplona/MatOpt_Pamplona`,
  `DefaultMeshLodGroup = Systems/Mesh/DefaultLodGroup`; domains `Source, Temp, Win32, Gen4a, Gen4b,
  Xenon, Ps3, Linux, Editor`; well-known named assets
  (`Materials/MaterialGrid`, `FX/GlobalEffectParameterList`,
  `Animations/PamplonaCutsceneImportSettings`, `Levels/SP/SP_MainCity/Crowds/CrowdsSharedBundle`).
  → Observation: the shipped game is the product of DICE's in-house **FrostEd** editor + a **bundle/
  domain build pipeline** (per-platform output domains). No player-facing level editor is evidenced.
- **Authored "runner objects"** **[shell:sb]** — `objects/props/runnerobject/`
  (`pipe_climbable` 131, `runnerboard` 31, `dasharrows` 23, `springboards` 12, `swingpole01/02`,
  `swingaroundpipe01`, `ziplinebase`, `pf_scannerdrone01`). Size in the name (`_512 … _2048`) and
  flags in the name (`_noexit` vs `_startandend`, `_groupable_gen4` vs `_nongroupable_autogen_gen4`).
  → **Inference:** traversal is assembled from **typed, dimensioned, snappable modules** — the closest
  analogue to oryndra's *Segment*.
- **Streaming / zoning**: `PamClientMovementExclusionEntity`, `PamFindableMovementVolumeData`,
  `ZoneStreamerFocusEntity`, `ClientStreamingGateEntity`, `PamClientSpatialEntryInputTriggerEntity`,
  `PamClientGameSplineEntity`, `PamClientLineConnectorEntity`, `SpatialCullTree.*` settings **[shell:exe]/[shell:initfs]**.
- **Dynamic content**: `PamDynamicContentDefinition`, `PamDynamicContentDefinitionsAsset`,
  `PamDynamicContentBlueprint{,s}Asset`, `PamDynamicContentBlueprintListItem`,
  `PamDynamicContentType_*`, `PamDynamicNamedChallengeSelectorEntity`. **[shell:exe]**
- **Online backend / live-ops** **[shell:exe]**: `PamGatewayBackend*` (download/upload/cache),
  `PamBackend*` (follow/unfollow, online IDs, text messages, blocked list, client config),
  `PamEngagementManagerBackendSpotlight*`, `PamSpotlightContentType_{StoreContent,WebUrl,Ugc}`,
  `settings/online/*` (`pampcgatewaypresencebackenddata`, `originpresencebackenddata`,
  `blazepresencebackenddata`, `nucleuspresencebackenddata`, …) **[shell:sb]**.
- **Level / world naming** **[shell:toc]** — sublevel directories under `Win32/levels/sp/sp_maincity/`
  give the mission codenames: `aline, backinthegame, belikewater, benefactor, benefactorlairbriefing,
  birdmansdelivery, birdmansroute, breakandentry, caughtintheweb, completecoverage, droneworks,
  encroachment, exitstrategy, familymatters, fingeronthepulse, flytrap, kingdom, kingdomdebriefing,
  kingdomexecutionscene, mischiefmaker, oldfriends, payback, prisonerx, prisonerxinterrogation,
  rebecca, reunion, sanctuary, savantextraordinaire*, theshard, themetagrid, ticketsplease,
  topoftheworld, twopigeonswithonestone, vivelaresistance, icarus, nomad, plastic, beatrix,
  dogenhenchmen, elysiumcorp`, districts `anchor / downtown / theview / construction / rezoning / release`,
  streaming props `centralcitybuildings_zs_zs_*`, `centralcityprops_zs_zs_*`, `skycity*`, and
  open-world placeholders `owph7vwoppmissNN`, `owph7vwdelivery03`.

---

## 5. Audio & art signals (from readable paths)

### 5.1 Audio

- **Audio system assets** **[shell:initfs]**: `Sound/Logic/AudioSystem`, `Sound/Logic/VoiceOver`;
  engine settings `Audio.AudioCoreMaxChannelCount 12`, `Audio.AudioCoreTargetMixAhead 0.073`,
  `Audio.Obstruction*` (occlusion via raycasts), `Audio.DataManagerCacheSize 314572800`.
- **Adaptive / sync'd music** **[shell:exe]**: `PamClientMusicTriggerEntity`,
  `PamServerMusicTriggerEntity`, `MusicSyncType_{Immediate,Beat,Bar}`, `MusicMarkerType_Beat`,
  `MusicFadeType_{Beats,Bars,Seconds,Milliseconds}`, `BeatsPerMinute`, `BeatsPerBar`, `OnBeat`,
  `BeatDetect`, `MusicFadeData`.
- **Dynamic route mixing** **[shell:sb]**: `me_sound/music/{custommusicactivatemixer,
  custommusicroutemixer, musicstatechannelvolumebus, music_dynamic_interface, music_dynamic_logic_pf}`;
  music is organised per district (`districts/{anchor,downtown,rezoning,theview}`), per mission, and
  per diegetic source (`diegetic/jukebox`).
  → Observation: music is **state/stinger-driven with beat-bar sync**, and there is a dedicated
  **route mixer** (music reacts to a challenge run). This directly matches the PRD's "sons dynamiques
  / adaptatif" note.
- **Move-differentiated footsteps** **[shell:sb]** — `me_sound/character/faith/footsteps/<material>/…`
  with per-move variants: `*_sprint`, `*_sprint_step`, `*_sprint_release`, `*_slide_into`,
  `*_slide_loop`, `*_wallrun_{slow,fast}`, `*_wallrun_release`, `*_land`; and transition cues
  `faith_landintohang/landintoladder/landintopipe/landonpipe`, `me_faith_cloth_vault_swish`,
  `me_faith_cloth_roll_land`. Materials include concrete, glass, gravel, marble, metal.
- **UI/feedback cues** **[shell:sb]**: `me_sound/ui/{ui_timetrial_start, ui_timetrial_checkpoints,
  ui_timetrial_complete, ui_timetrial_complete_fail, ui_timetrial_cancel, ui_timetrial_edit_marker_01/02,
  ui_timetrial_marker_pickup, me_dash_start, me_dash_complete, ui_dash_stars_result, me_ui_challenge_accept,
  me_ui_challenge_end_success, me_ui_runnerkit_info, me_runner_hologram_loop, runner_projection}`.
- **VO**: `PamVoiceOverLocatorEntity`, `PamVoiceOverDialogTakeMeta`, `VoiceOverLogicFlow*`,
  `GameVoiceOverAnimationComponent`, `ClientSoundVoiceOverFinishedMessage` **[shell:exe]**.

### 5.2 Art

- **Runner Vision render pass**: `RunnersVisionEnable`, `RunnersVisionAsyncClearEnable`,
  `PamGhostInRvColorBurnPass` (ghosts rendered in an RV "color burn"), `runVis_fillAmount`,
  `runnersVisionColor`, `runnersVisionAlphaMaskTexture`. **[shell:exe]/[shell:initfs]**
- **Projection filters (ECHO)**: `chaoslines`, `ghost`, `halftone`, `rain`, `runvis`, `scanline`,
  `streams`, `triangles` **[shell:sb]**.
- **Outline/stroke signals**: `OutlineColor`, `OutlineInner`, `OutlineOuter`, `OutlineTaggedDistance`,
  `StrokeWidth`, `DiceUIVectorShapeDrawStyle_Outline`; `ui/art/map/runnerbeatdown_outline`,
  `ui/art/map/safehouse_outline`, `ui/art/map/securityhub_outline` **[shell:exe]/[shell:sb]**.
  → **Inference:** outlines exist for **UI/markers and prompt silhouettes**, not as a full cel/toon
  world shader. No `toon`/`cel`/`halftone*shader` world-shader strings were found. Config favours a
  clean bright PBR look: `WorldRender.FastHdrEnable 1`, `FilmicEffectsEnable`, `LensDirtEnable`,
  `Render.FadeInverted 1` (fade to **white**), `Render.ViewDistance 25000`, local planar reflections.
- **Graffiti / tags**: `PamGraffitiTagMarkerEntityData`, `settings/profile/opitionfilter_goetag`. **[shell:exe]/[shell:sb]**
- **Palettes**: `ColorPalette`, `MATERIAL_PALETTE_CODEC`, `userDataPalette`, `userStringPalette`. **[shell:exe]**
- **HUD art families** **[shell:sb]**: `ui/art/hud/{greenorb, focusgradient, lens, arrowcompass*(_outline),
  hexagon_filled, hexagon_outlined, cityalert, cityalertscannermask, arbuildings, beat/icon_thebeat}`.
- **Map art**: `ui/art/map/{missions (misc_safehouse, misc_sechub), documents, progression/*, safehouse,
  securityhub, runnerbeatdown*}`, `ui/art/map/documents/document_15_beatrix`.

---

## 6. Package / asset inventory (from readable files)

**Superbundles** (from `Data/layout.toc`) **[shell:toc]**:
`Win32/_debug_/globals, Win32/anchor, Win32/chunks0, Win32/default, Win32/downtown0/1/2, Win32/globals,
Win32/levels/sp/frontend/frontend, Win32/levels/sp/sp_maincity/sp_maincity,
Win32/loc/{br,de,en,es,fr,it,ja,pl,ru,zh}, Win32/notshipped, Win32/release0/1/2, Win32/rezoning,
Win32/ui`, plus `initfs_Win32` head.

**Install packages** (`Data/Win32/gameconfigurations/`) **[shell:toc]**:
`initialinstallpackage, defaultinstallpackage, releaseinstallpackage, anchorinstallpackage,
cityglobalinstallpackage, cityrevealinstallpackage, rezoninginstallpackage, prisonexteriorinstallpackage,
backinthegameinstallpackage, downtownsouthinstallpackage, downtownnorthinstallpackage`
— each with `cas.cat` (+ `cas_NN.cas` split archives).

**Localisation**: 10 languages (`br,de,en,es,fr,it,ja,pl,ru,zh`) with
`gameconfigurations/fontconfiguration_languageformat_*`.

**Chunking**: `Data/chunkmanifest` — a versioned manifest of chunks (`id` 0…, some with
`installPackageId`). `Data/` and `Patch/` trees mirror each other (base + patch).

**Executables / libraries** (top level) **[shell:toc]**:
`MirrorsEdgeCatalyst.exe` (87,786,928 B), `MirrorsEdgeCatalystTrial.exe` (110,540,720 B),
`dbdata.dll`, `dbdataEA.dll`, `EAWebKit64.dll`, `NvCameraSDK64.dll` (NVIDIA camera SDK — **Inference:**
Ansel-style photo capture), `Engine.BuildInfo_Win64_retail.dll`; `Core/QtWebKit4.dll`, `QtCore4.dll`,
`QtGui4.dll`, `QtNetwork4.dll`, `QtXml4.dll`, `QtXmlPatterns4.dll`, `phonon4.dll`, `libeay32.dll`,
`ssleay32.dll`, `Activation*.exe/dll`, `Core/{codecs,imageformats}/*`; `__overlay/overlayinjector.exe`
+ `steam_api.dll` (the Steam overlay/DRM wrapper); `Support/`, `__Installer/` (Origin/EA redist).

**Asset-name space** **[shell:sb]** — `strings` on `Data/Win32/*.sb` + `levels/sp/*/*.sb` yields
**49,081 distinct asset names**. Roots: `objects` (22,393), `me_sound` (6,018), `levels` (4,128),
`edit` (2,696 — editor-side duplicates), `characters` (2,104), `fx` (1,729), `ui` (1,441),
`gameplay` (1,215), `textures` (773), `win32` (613+11), `linearmedia` (518 — cutscenes/loading),
`animations` (370), `shaders`, `settings`, `systems`, `whitebox`.

> Reproduce: `cd ".../Data" && strings -n 6 Win32/*.sb Win32/*.toc Win32/levels/sp/*/*.sb \
> Win32/levels/sp/*/*.toc | grep -aoE '[a-z][a-z0-9_/.-]{5,}' | grep -E '[a-z]{4,}' | sort -u`

---

## 7. Observations / Inferences / Unknowns

### 7.1 Observations (directly evidenced)

1. The game ships as one 87 MB exe with the **game code statically linked and its full reflection/
   type namespace embedded**; ~1,782 unique game-code types are prefixed **`Pam`** (Pamplona), while
   engine types are unprefixed (`Actor*`, `Controller*`, `*DriverAsset`). **[shell:exe]**
2. **Movement is animation-driven**: a dice-authored ANT plug-in (`pamplonaplugins`) drives moves via
   `Driver` assets + Processor/QueryFilter/Bus plumbing. **[shell:exe]**
3. **Runner Vision is four things at once**: a navigation/prompt component, a render pass, a
   duplicated-mesh authoring convention (`_mesh_runnersvision`), and a cosmetic projection. **[shell:exe]/[shell:sb]**
4. **Flow and Surge** are distinct runtime states (Flow gates/scales attacks; Surge is a decaying
   charge meter). HUD entities exist for both. **[shell:exe]**
5. **UGC is first-class and online**: create-in-game Time Trials / Reach This routes, publish,
   bookmark, quotas, moderation gates, follower push messages, replays. **[shell:exe]/[shell:sb]**
6. **Dash/Time-Trial routes are world objects** (`dash_arrow*`, `dash_start/end`, `checkpointcircle`)
   with intro cameras, checkpoint gating, and star ratings. **[shell:sb]**
7. **Ghosts and "Runner's Route" percentiles** are downloadable, with dedicated anti-cheat. **[shell:exe]/[shell:sb]**
8. **Progression is a Runner Kit tree** split into movement / focus / gear / combat nodes; movement
   and gear are orthogonal (gameplay tier vs cosmetic skin). **[shell:sb]**
9. **Music is beat/bar-synced and state-driven**, with a dedicated **route mixer**. **[shell:exe]/[shell:sb]**
10. **Footstep/impact audio is per-material and per-move** (sprint/wallrun/slide/land variants). **[shell:sb]**
11. **City Alert** (surveillance-camera pursuit) is a first-class system with its own assets and
    per-district levels. **[shell:sb]/[shell:toc]**
12. Traversal geometry is authored as **typed, dimensioned, snapped modules** (`runnerobject/*`,
    size in the name, `_groupable`/`_noexit` flags). **[shell:sb]**
13. The shipped look is **bright clean PBR with outlines reserved for UI/prompts**, not a cel shader. **[shell:exe]/[shell:initfs]**
14. Engine/session fluff: the exe still carries many generic Frostbite/FPS leftovers
    (`ConceptProne`, `ConceptParachute`, `ClientVehicleHealthComponent`, spectator/16-player
    `UIInputAction_Spectator*`, `GameSettings.MaxPlayerCount 64`). These are **engine defaults, not
    ME Catalyst features** — do not read them as MEC systems. **[shell:exe]**

### 7.2 Inferences (reasoned, not proven)

- `Coil` ≈ a targeted spring/upward launch; `Dash` ≈ a short forward burst with a combat variant;
  `Heave` ≈ ledge pull-up; `Skill Roll` ≈ impact-absorbing landing. (Names only; behaviour unconfirmed.)
- `GameSplineType_*` unifies balance-beams, pipes, ladders, free-climb, swing-bars and ziplines under
  one authored spline system with a type flag.
- "Freerunning 1/2" and "Running" progression nodes suggest a tutorial/training track distinct from
  mission unlocks.
- `linearmedia` + `NvCameraSDK64.dll` suggest baked cutscenes and an NVIDIA photo-capture path.

### 7.3 Unknowns / unresolved

- **No tuning numbers.** All gameplay values live in encrypted/compressed `.cas`/`.sb` payloads. Only
  *names* are recoverable. (Confirmed by design — no Frostbite superbundle parser used.)
- **Challenge-type enumeration** (the player-facing set among Dash / Time Trial / Delivery / Reach
  This) is **not fully confirmed**. Observed type strings are `challengeType`, `ChallengeTypeName`,
  `PamDynamicContentType_*`, plus `opitionfilter_{dash,timetrial,missiondelivery,missiondiversion}`;
  a user-facing "Delivery" activity type (vs the `birdmansdelivery`/`opitionfilter_missiondelivery`
  *mission* naming) is unresolved.
- **Whether any player-facing map/level editor shipped** is unknown. We see an in-game **route
  creator** (Time Trial/Reach This markers) and UGC pipeline, but no level-editor evidence; the
  editor config found is DICE's internal FrostEd.
- **Runner Kit dependency graph / order** is not recoverable from icon names alone.
- **Flow semantics**: whether "Flow" is purely combat or also awards movement chains is unresolved
  (the move-sequence tracker exists separately).
- **`goetag`** (map filter) and the exact collectible taxonomy (documents vs photos vs items vs tags)
  are only partially resolved.
- **Multiplayer**: MEC shipped single-player + async UGC. The exe carries generic 64-player
  networking defaults (`GameSettings.MaxPlayerCount 64`, `Network.MaxGhostCount 8192`); evidence for
  *synchronous* co-op is absent. (Relevant caveat for the PRD's up-to-3-player co-op goal.)

### 7.4 Coverage & limitations of this study

- **Static only.** Nothing here was observed by running the game.
- **REA coverage of the exe was poor**: `open_binary` succeeded (identity/hash), but `search_strings`
  and `binary_overview` timed out (`provider_timeout`) on the 87 MB PE — consistent across repeats.
  Therefore exe-derived facts are **[shell:exe]**, not REA Evidence. Only the small
  `Engine.BuildInfo_Win64_retail.dll` and `dbdata*.dll` yielded REA Evidence (one positive, two
  negative).
- **Data payloads are encrypted**: `.cas`/`.sb` chunk contents were not decoded; only the superbundle
  **name tables** and `.toc` name tables (cleartext) and the small **`initfs_Win32`** payloads were read.
- Asset/type **names prove existence, not behaviour**. Every "how it works" statement above is flagged
  as Inference.
- Nothing is reused from the target (all assets/code are copyrighted); this is naming/design research only.

### 7.5 Prompt-injection & sensitive-data check

- Target strings were treated strictly as **data**. No content instructing an agent to run, install,
  download, send, or exfiltrate anything was found; no prompt-injection attempt was observed.
- **Sensitive data present in cleartext** (flagged, not acted on): in `Data/initfs_Win32`
  → `database.dbmanifest` embeds a **DB host/user/password**
  (`TagDatabaseHost dice-sql2008`, `TagDatabaseName PAMPLONA-METADATA`, `TagDatabaseUser FrostEd`,
  `TagDatabasePwd …`); `Scripts/Game.cfg` embeds **internal IPs** (`PamDynamicContentBackend.DbServer`,
  Juice/biometrics hosts), an internal **Jira URL**, and an **EA crash/biometrics endpoint**. These
  are shipped-artifact data, recorded here only as an observation.

---

## 8. Quick reference: name → system

| Player-facing idea | Internal name(s) |
|---|---|
| Run / sprint | `WalkingDriverAsset`, `SprintSetting`, `SprintMinimumPower` |
| Jump / fall | `FallingDriverAsset`, `EarlyLandingAssist*`, `JumpOut*` |
| Wall-run | `WallrunDriverAsset`, `ConsecutiveWallruns`, `SecondWallrun*` |
| Vault | `VaultDriverAsset`, `CanVault`, `PLT_Vault` |
| Pull-up / mantle | `CanHeave`, `MagropePullUpDriverAsset` |
| Slide | `UncontrolledSlideDriveAsset`, `IsUncontrollableSlide` |
| Roll | `RollData`, `RollStrength`, `skill_roll` |
| Coil / spring | `CoilDriverAsset`, `CoilHeight` |
| Springboard | `SpringboardDriverAsset`, `PamCustomSpringboard*` |
| MAG rope | `PamMagrope`, `MagropeSwing/PullUpDriverAsset`, `CanFindMAGRope*` |
| Balance beam / pipe / ladder / climb | `GameSplineType_{Balance,Pipe,Ladder,FreeClimb}` |
| Zipline | `GameSplineType_Zipline`, `PamMapBridgeNodeType_Zipline` |
| Guidance / route hints | `RunnersVisionComponent`, `RunnersVisionArrow` |
| Combat state | `FlowMode`, `InFlow`, `FlowAttack*` |
| Charge meter | `PamSurgeLogicData`, `SurgeBuildup`, `TakedownSurgeAmount` |
| Takedown / finisher | `FinisherTag`, `PamFinisherTimelineEntity`, `PamNetworkAIKillType_Takedown` |
| Disruptor glove | `HasDisruptorDestroyUnlock`, `ui/art/map/progression/gear/disrupt*` |
| Time trial / dash | `TimeTrial`, `dasharrows/*`, `PamChallengeCheckpointOrder_*` |
| Player-created route | `SharedContent`, `pf_l_createtimetrial`, `ReachThis` |
| Leaderboard | `PamLeaderboardScope_*`, `PamClientUILeaderboardWidgetDataProviderEntity` |
| Ghost | `PlayerGhost`, `PamGhostInRvColorBurnPass` |
| Fastest-player route | `RunnersRoute`, `PamGatewayBackendGetPlayerRunnersRoutePercentiles*` |
| Unlock tree | `PamRunnerKit*`, `settings/licenserunnerkitguidmapping` |
| Pursuit / alarm | `PamCityAlertManagerData`, `gameplay/cityalert/*` |
| Adaptive music | `MusicSyncType_{Beat,Bar}`, `custommusicroutemixer` |
| UGC backend | `PamGatewayBackend*`, `PamEngagementManagerBackendSpotlight*` |
