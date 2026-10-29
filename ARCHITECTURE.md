# Runtime responsibilities

`ArenaObstacles` builds a stable forest-camp layout: eight timber/rock anchors, optional randomized supply bays, two protected crossing routes and an outer retreat lane. Collision and navigation share renderer-derived footprints. See `FOREST_LAYOUT.md`; `ForestLayoutChecks` verifies seeded connectivity, collision and boss/reset cleanup.

`CombatController.ForestBoss` owns the finite campaign's optional final encounter: articulated stone-king model,
locked ground telegraphs, swept charge, hostile needle fans, a one-time half-health phase and hazard cleanup.
`CombatController.ForestBoss.Waves` adds timed assaults, movement to an arena edge, an eight-second armored rest,
bounded three-sided reinforcement waves and a telegraphed return charge. Births share the boss warning lifecycle
and occur outside enemy enumeration; rest resistance is applied in the common damage path, including burn fractions.
`WaveSession.IsBossWave` prevents timeout victory; flow observes `BossDefeated` only after the combat tick and calls
`CompleteBoss` before settlement, so lethal weapon hits never clear iterated collections synchronously.
Boss HUD data remains read-only through `ICombatView`. Its surface-aware weapon range helpers preserve existing
small-enemy hit rules. The boss's arena clears generated cover for this encounter; restarting regenerates it.

This refactor preserves the current scenes and IMGUI appearance. There is no separate main-menu scene yet.

## Modules

`SurvivorPlayground` also owns one scene-local `GameAudio` component. Combat, movement, flow and UI emit
presentation-only `AudioCue` requests at successful event boundaries. `GameAudio` owns bounded reusable
voices, pause/focus behavior, persistent volume controls, and music transitions. Its resource-loaded
`GameAudioBank` references the curated Kenney assets directly. Audio uses an independent random generator
and does not mutate combat state. See `AUDIO.md` for mappings and `Tools > Survivors > Verify Audio`.

Wave pacing and enemy schedules are now authored in `Assets/Resources/Config/DefaultWaveConfiguration.asset`,
edited via `Tools > Survivors > Wave Configuration`. `WaveRules` remains pure C#; `WaveConfiguration`
is the Unity asset adapter. Each restart takes a deep snapshot, and combat caches only the current wave row.
Finite/endless progression is owned by `WaveSession`, including configurable starting/final waves.
See `WAVE_CONFIGURATION.md` for table semantics, testing modes and endless growth caps.

`EnemyDefinition` owns each enemy type's base health (including split offspring), authored in the
configuration's separate enemy-attribute table. Wave rows only supply an explicit health multiplier,
defaulting to 1 throughout the campaign. Spawn health is resolved from both and bounded; run snapshots
deep-copy definitions. Earthbreaker uses weapon damage instead of current target HP, so durable enemy
attributes remain meaningful when testing it in early waves.

`CombatController.Splitter` owns mother/offspring membership and deferred births. A mother reserves one extra
population slot; death queues exactly two offspring outside weapon iteration, and portal spawns account for
all reservations. Birth placement is local and obstacle-aware. Combat cleanup cancels pending births, so
settlement and restarts cannot spawn another generation. Offspring never enter the mother set.

| Module | Responsibility |
| --- | --- |
| `SurvivorPlayground` | Scene-compatible composition root. Connects the run state, flow controller, UI, and combat events; forwards Unity lifecycle callbacks. Its existing script GUID and inherited serialized fields preserve scene references. |
| `CombatController` and its partial files | Enemy spawning/movement, weapons, damage, pickups, world effects, and arena cleanup. Receives explicit ticks and commands; contains no menu drawing or input handling. |
| `GameFlowController` | Start/weapon choice, combat deadlines, settlement, shop, defeat/restart, and purchase/replacement commands. Owns screen-transition state. |
| `RunState` | One authoritative instance of `WaveSession`, `RunEquipment`, and `WaveShop`, plus kills, loot score, and survival time. Reset together for a new run. |
| `GameUI` | UI input lifetime, navigation, common IMGUI helpers, and dispatch to the views. Sends gameplay requests to the flow controller. |
| `MenuUI` | Main menu, settings, starter selection, and item codex rendering. |
| `BattleUI` | HUD, shop/replacement, floating score, victory, and defeat rendering. |
| `ICombatView` | Presentation-only combat queries: health, weapon stats, player/camera position, and effect display data. Does not expose spawning, attacks, or health mutation. |

## Calls and ownership

```text
SurvivorPlayground
  ├─ RunState
  ├─ GameFlowController ──commands/ticks──> CombatController
  └─ GameUI
       ├─ MenuUI
       └─ BattleUI

UI interaction ──request──> GameFlowController
UI drawing     ──read─────> RunState + ICombatView
Combat         ──events───> PlayerDied / SettlementReady
```

The composition root subscribes the flow controller to combat events and unsubscribes on destruction. Only the host ticks the flow, so the extracted modules do not introduce additional Unity `Update` calls or duplicate spawners. UI actions are enabled/disabled with the host and disposed on destruction; disabling while the codex is open restores the previous time scale.

Purchases, refreshes, locking, replacement, new runs, and wave continuation go through `GameFlowController`. Rendering reads the existing equipment/shop rule models; it does not execute their gameplay mutations. Camera settings remain presentation controls.

At a wave deadline, flow asks combat to finish the arena. Combat clears enemies/projectiles, creates remaining-enemy drops, and animates all surviving drops. Once the celebration and pickups finish and the player confirms, `SettlementReady` lets flow open the shop. This preserves the existing per-drop material and score rules.

`SurvivorPlayground.Restart`, `NewGame`, and `BeginRun` remain compatibility entry points for existing builders and editor tools. New gameplay code belongs in combat/rule modules, UI code in the views, and transition decisions in flow; do not add rules to this entry point.

## Verification

`CombatMetrics` records per-wave effective damage, hits and kills for blades, hammer and other sources,
actual player health loss, item healing, and blade hits during active stun. Combat lifecycle resets it on
restart and wave start, retaining results through settlement/defeat. `PlayerHealth.Damaged` reports health
actually lost (lethal overkill excluded). The editor-only `MeleeBuildLab` reads these counters and creates
transient copies of wave configuration; it never writes experimental values into the campaign asset.
See `MELEE_BUILD.md` for the component comparisons and automated movement probe limitations.

`CombatMetrics` also separates poison/frost and records effective poison damage while the target has an
active frost slow. `PoisonBuildLab` provides isolated recipes and equal-spend alternatives. The seventeenth
passive keeps existing shop IDs stable: use `ItemLibrary.ShopId/Index` instead of enum-plus-six arithmetic.
Poison pool duration is snapshotted on spawn; the valve changes spray cadence without changing damage,
overlap rules, pooling limits or cleanup. See `POISON_BUILD.md` for measured results and remaining tuning.

Run `Tools > Survivors > Verify Weapon Expansion` outside Play Mode. It includes:

- Architecture checks: one run-data owner, inherited scene references, menu/settings isolation, duplicate-start rejection, pause/input lifetime, death/restart, and clean re-entry.
- All sixteen item rules and the existing weapon, first-wave crowd, and shop replacement checks.
- End-wave drops combined with arena pickups and credited exactly once.

Success markers are `ARCHITECTURE_CHECKS_PASSED`, `ITEM_SYSTEM_PASSED`, and `WEAPON_EXPANSION_PASSED`.

`StartMenuPreview.Capture` and `VictoryLootPreview.Capture` additionally exercise menu screenshots and simulated keyboard/gamepad settlement confirmation. `GameTestAccess` is an editor-only adapter for legacy test fixtures across the new module boundaries; runtime code does not use reflection.

- `CombatController.BaitLantern.cs`：灯笼自动放置、普通追兵目标覆盖、耐久与可复用低多边形模型；在敌人移动前更新，不更改精英或首领状态机。武器 ID 11 / 商店 ID 31，保留缓释阀 ID 30。`StarterCount` 包含灯笼；破碎/到期爆燃在敌人遍历结束后结算，避免击杀修改遍历中的集合。清理取消待结算爆炸。
