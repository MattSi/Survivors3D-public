# Run items

Each run lasts up to 20 waves of 45 seconds. Choose Gun, Orbiting Blades or Fire Breath at the start; buy the other weapons in the shop. Each weapon supports three tiers. Death or New Run clears equipment and materials.

Rapid Magazine and Blade Reach Mod only enter shop stock after their corresponding weapon is equipped. Blade Reach Mod extends existing blades; the Orbiting Blades card marked NEW WEAPON equips the two rotating blades.

The shop offers four distinct products. Related items have a higher selection weight, without guaranteeing a particular build. Buy with collected materials. Refresh starts at 3 materials and increases by 1 per refresh within that shop. Locked offers retain their price and survive refreshes and wave transitions. Victory stays over the visible battlefield until Space or gamepad A is pressed. Unexpired drops fly to the HUD and each adds one material and one score point on arrival. An early confirmation waits for collection to finish. The final wave leads to the final equipment screen and New Run. Ground drops expire after six seconds of unpaused game time.

## Passive library

Wave means the completed wave after which an item can enter shop stock. Prices below are first-copy prices; each additional copy costs 10 more. Item effects stack only up to the listed cap.

| Item | Main effect per copy | Price | Cap | Wave |
|---|---|---:|---:|---:|
| Armor-Piercing Rounds | +1 pierce; retains 75% damage per pierce | 18 | 3 | 1 |
| Ricochet Chip | +1 bounce; retains 50% damage per bounce | 30 | 2 | 4 |
| Incendiary Rounds | +25% direct bullet ignition chance | 20 | 3 | 1 |
| Split Core | Direct bullet kills launch two 35% damage fragments | 70 | 1 | 11 |
| Fuse | Spread ignition to two nearby targets | 25 | 2 | 5 |
| Concentrated Fuel | +40% burn damage, +1s duration, +15% breath cooldown | 24 | 3 | 2 |
| Char Bomb | Burning enemy deaths explode for 15 damage within 2.5m | 65 | 1 | 10 |
| Whetstone | +10% critical chance, another +10% for melee; critical damage is doubled | 18 | 3 | 1 |
| Blood Pendant | +15% melee hit chance to heal 1 HP; shared 0.25s cooldown | 28 | 3 | 4 |
| Revenge Charm | +20% damage for five seconds after taking damage | 25 | 3 | 3 |
| Emergency Shield | Block the first hit each wave | 25 | 1 | 1 |
| First Aid Kit | Once per wave, heal 25 after surviving a hit at 30 HP or below | 35 | 1 | 6 |
| Savings Jar | Before waves 2–19, earn 20% interest on saved materials, capped at 40 | 30 | 1 | 1 |
| Hunting Badge | +30% critical kill chance to drop an extra material | 30 | 2 | 4 |
| Danger Contract | +20% enemy spawn rate and cap; +25% extra material chance | 20 | 2 | 2 |
| Magnetic Coil | +1.5m pickup radius; every 15 collected materials charges a three-target lightning attack, 12 damage per copy | 35 | 2 | 5 |
| Slow-release Valve | Poison pools last +3s; spray interval becomes 1.0s instead of 0.8s. Requires Poison Backpack. No extra damage or overlap stacking. | 30 | 1 | 4 |

The valve is the seventeenth passive. Legacy shop IDs remain unchanged; use `ItemLibrary.ShopId` and `ItemLibrary.Index` for passive IDs. See [Poison Build](POISON_BUILD.md) for the routing tradeoff and playtest presets.

Fragments, splash, item explosions and lightning cannot recursively trigger item attacks. Spread ignitions do not spread again. Magnetic Coil stores up to three charges; automatic end-of-wave collection does not charge it. Revenge hits refresh duration rather than repeatedly multiplying strength. First Aid cannot revive a lethal hit.

## Controls

Startup opens a main menu with New Game, Settings and Quit. New Game opens weapon selection; Esc/B or the back button returns to the main menu. Settings contains camera mode, tilt, distance, language and reset. The world remains visible for preview. Camera preferences are saved on return and restored on launch, separately for third-person and top-down views. Combat has no camera settings panel; right-stick yaw remains available. In the Unity editor, Quit stops Play mode; in a player build, it exits the application.

The default language is Simplified Chinese. Click the Chinese/English button, press F2, or press the gamepad Menu/Start button to switch languages. The preference is saved between sessions. Both languages use the bundled OFL-licensed Noto Sans CJK font. All seventeen passive items have distinct code-drawn icons shared by the shop and codex, including thumbnails in the codex list.

- Start/shop: arrows or left stick/D-pad to navigate; Enter or gamepad A to confirm. Mouse buttons also work.
- Shop: L or gamepad X toggles the selected offer's lock.
- Codex: Tab or gamepad View opens the library; Esc or B closes it. The codex pauses combat and shows effects, stacking limits, prices, availability and suggested combinations.
- Combat: WASD/left stick moves; right stick changes camera yaw. Weapons attack automatically.

## Implementation and validation

`Assets/Scripts/Core/ItemLibrary.cs` owns the passive definitions. `RunEquipment.cs` owns run inventory; `WaveShop.cs` owns selection, prices and locks. The SurvivorPlayground Items and Menus partials implement combat effects and UI.

`ForestSceneChecks.VerifyPlay` runs wave/shop checks, runtime checks for all 16 items, starter selection, projectile combat, movement, camera orbit, health, death and restart. `ItemPreview.Capture` renders starter, shop and codex screenshots to `ArtSource/World`. Numbers are an initial balance pass; full-length playtesting is still useful.
