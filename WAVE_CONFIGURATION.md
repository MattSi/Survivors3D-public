# Wave and Enemy Configuration

Unity menu: **Tools → Survivors → Wave Configuration**.

> The configuration window's UI is in Chinese. Below, each field or button is given in English, with the original label in backticks where it helps you find it.

Regular Mixed mode now ends with a fight against the Forest Stone King on the final wave by default (wave 20, 1800 health), and you must defeat it to win.
The editor's **Final-wave Forest Stone King** (`最终波林地石王`) option turns this off, and **Forest Stone King health** (`林地石王血量`) sets the boss's health separately.
Endless and single-enemy test modes still use the original timing rules. To play the boss directly, use **Tools → Survivors → Play Forest Stone King**.
Models, attacks, and verification are described in `ArtSource/Enemies/FOREST_BOSS.md`.

Default configuration asset: `Assets/Resources/Config/DefaultWaveConfiguration.asset`.
Both the `ForestCamp` and `Playground` scenes read it automatically. You can also right-click in the Project window and choose
**Create → Survivors → Wave Configuration**, or click **Copy as test configuration** (`复制为测试配置`) in the editor window,
then drag the new asset into the **Wave Configuration** field of the scene's `SurvivorPlayground` component.
Selecting an asset in the window only edits that asset; it does not apply a custom asset to the scene automatically.

After editing, click **Save** (`保存`) and start a new run for the changes to take effect. Each run reads a deep copy, so editing the asset doesn't change a fight in progress.
Changes made to the configuration asset in Play Mode are also kept, so using a test copy is recommended.

## Quick testing

| Goal | Settings |
|---|---|
| Test the full enemy mix directly | Start wave = 11, enemy test mode = Mixed |
| Test only chargers from wave 1 | Start wave = 1, test mode = ChargerOnly |
| Test only stone-armored enemies | Test mode = StoneArmorOnly |
| Test only bombers | Test mode = BomberOnly |
| Test only bone-needle shooters | Test mode = ShooterOnly; works from wave 1 |
| Test only splitters and their offspring | Test mode = SplitterOnly; works from wave 1 |
| Spawn only regular enemies | Test mode = NormalOnly |
| Test the shop and equipment | Set starting test materials; shorten the wave's duration, and the shop opens after it ends |
| Test the transition into endless | Endless mode = on, start wave = 20 |
| Restore normal play | Default asset: start wave = 1, Mixed, materials = 0, endless mode = off |

Only modes ignore that type's unlock wave and every-N spacing, but still respect the stage's enabled toggle, alive cap, and simultaneous attack cap.
When testing before unlock, the type's first stage is used. When a cap is reached, spawning pauses; after deaths, the selected type keeps refilling and regular enemies are never spawned instead.
To watch movement only, set simultaneous attacks to 0. Stone-armored enemies have no active attack, so this value doesn't affect their contact damage.

## Enemy base stats table

The **Enemy base stats** (`敌人基础属性`) section in the same window sets `BaseHealth` per type, separately from unlock waves and the start wave.

| Type | Default base health |
|---|---:|
| Normal (shared by three looks) | 10 |
| Charger (charging imp) | 20 |
| StoneArmor | 120 |
| Bomber | 24 |
| Splitter (mother) | 45 |
| Splitling (split offspring) | 14 |
| Shooter (bone-needle shooter) | 18 |

**Actual spawn health = enemy base health × wave health multiplier**, rounded up, with a minimum of 1 and a maximum of 1,000,000.
By default, waves 1–20 all have a multiplier of 1, so an enemy moved to wave 1 for testing keeps its full health. Only modes also use the explicit multiplier; to test base stats, set the wave's multiplier to 1.
Mothers and offspring each look up their own stats row; offspring health is no longer a percentage of the mother's or a regular enemy's.
For duplicate types, the last row wins; missing types use that type's default health. The old `EnemyHealth` and `ExtraHealthPerWave` settings are retired, while other wave/spawn parameters are kept; missing new stats use defaults. Older custom assets need their desired multipliers filled in again.

To change stone-armor health, just edit StoneArmor's base health, save, and start a new run. The current fight uses its own copy, so health doesn't change mid-fight.

The hammer's center now deals a fixed 38/43/48 damage (levels 1/2/3). It can't crit but does get the Vengeance Charm bonus. It flattens enemies it kills; otherwise it stuns for 1 second. The outer ring still only knocks back and stuns. A level 1 hammer with no bonuses leaves a 120-health stone-armored enemy at 82 health, and four center hits kill it.

## Wave table

Each row contains: wave, duration (seconds), opening count, base spawn rate (enemies/second), on-field cap, health multiplier, and base movement speed.
The default 20 rows keep the durations, counts, and spawn pacing, with all health multipliers set to 1. The spawn rate you enter applies at the start of the wave; at the end of the wave it is multiplied by the **end-of-wave spawn multiplier** (`波末刷怪倍率`).
Danger Contracts still raise speed and the on-field cap, within the global hard caps.
Health comes from the enemy stats table above; movement still uses the wave's base speed and each enemy type's built-in speed multiplier.

Wave rows can be in any order; the closest row at or below the current wave is used, and missing rows reuse the previous row. For duplicate waves, the later row in the array wins.
With no valid rows, safe base values are used. Duration is at least 1 second, the health multiplier is clamped to 0.1–1000, negative counts become zero, and invalid floating-point values fall back to defaults.

## Special enemy stage table

Each enemy type uses the closest stage at or below the current wave, so you don't need to repeat it every wave.

| Type | Starting wave | Every N successful spawns | Alive cap | Simultaneous attack cap |
|---|---:|---:|---:|---:|
| Charger | 3 | 6 | 2 | 1 |
| Charger | 8 | 5 | 3 | 2 |
| Charger | 13 | 5 | 4 | 2 |
| StoneArmor | 5 | 7 | 2 | 0 |
| StoneArmor | 10 | 7 | 3 | 0 |
| StoneArmor | 15 | 7 | 4 | 0 |
| Bomber | 8 | 9 | 2 | 2 |
| Bomber | 13 | 9 | 3 | 3 |
| Splitter | 11 | 11 | 2 | 0 |
| Splitter | 16 | 11 | 3 | 0 |
| Shooter | 6 | 8 | 2 | 1 |
| Shooter | 13 | 8 | 3 | 2 |

The table above shows the initial values for a new configuration. Stages you've edited in existing assets keep their values; the editor window shows the current ones.

The spawn count includes opening enemies and resets each wave. Priority is Charger → StoneArmor → Bomber → Splitter → Shooter; once a type reaches its cap, later types or regular enemies fill in.
The bomber's simultaneous attack cap includes bombers winding up and bombs already thrown; the charger's includes charging up and dashing.
The shooter's simultaneous attack cap includes charging and needles already fired. Older custom configurations need a Shooter stage added by hand; the default asset already has one.
Turning off a stage's enabled toggle stops that type from spawning from that stage onward; a later enabled stage can bring it back.

The splitter mother has 45 health and 80% of base speed by default. On death it spawns two offspring, each with 14 health and 130% of base speed by default. Mother and offspring health are each multiplied by the wave multiplier. Offspring don't split again, and for 0.4 seconds after spawning they don't move or deal contact damage. Mothers and offspring each score and drop loot like regular kills.
The splitter's alive cap counts only mothers, and simultaneous attacks should be 0; it doesn't control splitting on death. Offspring count toward the total on-field count, and each mother reserves one extra slot, so portal spawning stops early near the cap. The on-field cap must be at least 2 for a mother to spawn.
Splitting on death doesn't advance the portal spawn counter. If nearby spots are taken by the player, enemies, or obstacles, offspring wait for space. Settlement, death, and restart all clear pending offspring.
`SplitterOnly` allows split offspring to appear. Existing custom configurations need a `Splitter` stage added by hand before it spawns; the default asset already has one.

## Endless mode and growth past the table

With endless mode off, you win after the **total waves in regular mode** (`普通模式总波数`); the start wave is clamped to that range.
With it on, waves keep increasing: after wave 20 you still get settlement, the shop, and the next wave. The HUD shows `current wave / ∞`, and Piggy Bank interest keeps working.

Past the highest configured wave, the last row is the base, and values grow with the number of extra waves: opening count, on-field cap, base spawn rate, health multiplier, and base movement speed.
The health multiplier grows by 0.1 per wave by default (additive), for example 1.1 at wave 21 and 1.2 at wave 22; set it to 0 to keep base health. Duration and special enemy stages reuse the end of the table; you can still add dedicated rows, for example for waves 30 and 50.
The total on-field hard cap is at most 240, the final spawn rate at most 30 enemies/second, base movement speed at most 10, and final enemy health at most 1,000,000.
The default limits are more conservative: at most 64 opening enemies, a final spawn rate of at most 12 enemies/second, and base movement speed of at most 4.2.
These limits also apply to regular mode. There are no endless-only enemy types, equipment tiers, or economy curves yet; they can be built on top of this later.

## Code and checks

- `EnemyDefinition.cs`: each enemy's own base health and defaults.
- `WaveRules.cs`: pure C# configuration model, default table, stage selection, and growth logic.
- `WaveConfiguration.cs`: serializable Unity asset and copy creation.
- `WaveConfigurationEditor.cs`: the table editor window (Chinese UI).
- `WaveSession`: timing from the configuration, distinguishing finite and endless runs.
- `CombatController`: reads wave stats from the run's configuration; special enemy files only keep their own behavior.
- `WaveSession.TotalWaves` is kept only as a compatibility constant for the default 20-wave checks; the runtime uses `FinalWave` and `Endless`.

Automated verification: **Tools → Survivors → Verify Wave Configuration** (run outside Play Mode).
It covers default configuration compatibility, regressions for the three special enemies, edited configurations taking effect in a new run, targeted enemy tests, the finite end, the endless wave 20 → 21 shop flow, and late-game growth caps.
Success log: `WAVE_CONFIGURATION_CHECKS_PASSED`.

Full splitter verification: **Tools → Survivors → Verify Splitter** includes the regressions above plus splitter-specific tests; the success log is `SPLITTER_CHECKS_PASSED`.

A separate health check runs with the configuration verification, with the success marker `ENEMY_HEALTH_CHECKS_PASSED`. It covers a stone-armored enemy surviving a wave 1 hammer hit, identical base stats across waves, edit isolation, health multipliers, offspring stats, old-data compatibility, and hammer regressions.
