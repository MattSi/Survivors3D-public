# Survivors3D Playground

Open `Assets/Scenes/Playground.unity` in Unity 6000.3.22f1, press Play, then click the Game view so it receives keyboard focus.

## Wave flow

- There are **5 waves** of **45 seconds** each. The HUD shows the current wave and countdown. Total combat time is **3 min 45 s**; rest breaks have no time limit.
- When the countdown reaches zero in the first four waves, a rest break begins: remaining enemies and bullets are cleared, and movement, attacks, spawning, and the combat timer stop. Clearing the field does not add kills.
- Rest breaks have no time limit and keep current health and total kills. Press **Enter / R / gamepad A / ×** or click the button to start the next wave; the player returns to the center of the arena.
- When the fifth wave's countdown reaches zero, victory is shown. Reaching zero health in any combat wave is a defeat.
- Confirming a replay after victory or defeat restores 100 health, resets kills and the timer, and starts again from wave 1.
- All waves currently use the same enemy configuration; during rest breaks you can buy equipment with materials.

## Materials and equipment

### Weapon slots and levels

This stage has **3 fixed weapon slots**. You start with a level 1 gun, level 1 orbiting twin blades, and a level 1 flamethrower, all attacking automatically at the same time. The HUD shows each weapon's level, damage, attacks per second, and range.

| Weapon | Starting damage | Starting attack interval | Starting range |
| --- | --- | --- | --- |
| Three-round burst rifle | 6 per shot | 0.7 s per burst, 0.1 s within a burst | 14 |
| Machete | 20 | 1.25 s | 3 |

Enemies have 20 health, so the starting gun kills in two shots and the machete in one hit. The gun only aims at enemies in range; bullets disappear when they reach their range or hit the arena border. The two blades stay 180° apart and keep orbiting the player at a fixed **2 seconds per revolution**. Blades deal melee damage as they sweep through enemies; repeated hits on the same enemy are limited by the melee attack interval, and the blade flashes white briefly on a hit. The blades stop orbiting during rest breaks, death, and victory, and resume in the next wave. Both blades share one melee slot and upgrade level.

The shop randomly offers `GUN UPGRADE` and `MACHETE UPGRADE`, which upgrade the matching slot up to **level 3**. Each level adds **+5** damage, reduces the attack interval by **10%** of the starting value, and increases range by **10%** of the starting value. Upgrades cost **20, then 25 materials**. A level 3 gun deals 16 damage per shot and kills regular enemies in one shot. The two weapons don't affect each other, and existing magazine and blade equipment keeps applying to its weapon. Restarting restores both weapons to level 1.

- Each enemy killed drops one green crystal worth **1 material**. Within a distance of **3.5**, it is pulled to the player automatically and added to the HUD balance.
- When a wave ends, remaining enemies explode in place and each drops one crystal; these are collected automatically along with crystals still on the ground. Each crystal from an active kill adds 1 material and 1 point; each gold shard from a cleared enemy adds only 1 point and does not count as a kill. After the celebration and collection, each wave grants a fixed reward of 5 materials (no extra score), paid only once.
- Each rest break offers **3–4 items** for free at random, drawn from Fast Magazine (gun fire rate +20%, starting price 12 materials), Long Blade (melee range +15%, starting price 15 materials), Running Shoes (movement speed +10%, starting price 10 materials), Gun Upgrade, and Machete Upgrade. The same item can appear more than once. Each item slot can be bought only once and shows `SOLD` after purchase.
- `REFRESH` costs **3 materials** each time and redraws 3–4 items; you can't refresh without enough materials. Bonuses stack relative to base stats, each item goes up to level 3, and each level raises the price by 5. Remaining items of the same type update to show the next level's price. Maxed items no longer appear in later refreshes; once everything is maxed, refreshing is free.
- Click to buy, or use the arrow keys / gamepad left stick / D-pad to select up and down and press Enter / A / × to confirm. Select `START WAVE` to start the next wave.
- Materials and equipment carry over between waves in a run and reset in a new run.

- **WASD / arrow keys / gamepad left stick / D-pad**: move on the ground. Diagonal movement is not faster, a light push on the stick moves slowly, and the Input System's default stick dead zone is used.
- The player is a red-clad blocky warrior made in Blender, with **100** starting health. Source files and previews are in `ArtSource/Player`; Unity uses `Assets/Resources/Characters/BlockHero.fbx`. The original movement collider is unchanged.
- Enemies are gray cubes with **0.75** sides. One spawns from the arena edge every **0.65 s** and chases the player, with at most **80** at once.
- Every **0.9 s**, a spherical bullet with a **0.24** diameter is fired at the nearest enemy. Enemies take **two** bullets to kill; the machete still kills in one hit.
- Two opposed, continuously orbiting machetes with silver blades and dark grips surround the player: starting range **3**, damage **20**, attack interval **1.25 s**. Upgrades apply to both blades.
- Bullets disappear when they touch the arena border and never pass through it.
- Each contact hit deals **10** damage, followed by **0.7 s** of protection. Staying in contact with enemies keeps dealing damage.
- At zero health, movement, spawning, and shooting stop and the death screen appears.
- After death, press **R**, the gamepad south button (Xbox **A** / PlayStation **×**), or click **RESTART** to reset.

Input uses Unity's **New Input System**, with bindings in `Player/Move` and `Player/Restart` in `Assets/InputSystem_Actions.inputactions`. Keyboard and gamepad can be switched freely, and a gamepad is detected automatically when connected; attacks stay automatic. Death only disables movement; the restart input stays enabled.

The round stick indicator in the bottom-right HUD shows the left stick's direction, X/Y analog values, push percentage, and a progress bar in real time (using input after dead-zone processing). With no gamepad connected, it shows `OFFLINE` and recenters; keyboard input is not shown as stick input.

The arena is 36 × 36, and the player can't leave it. The HUD now uses compact, semi-transparent corner cards: health at top left, wave and countdown at top center, materials and kills at top right, weapon stats at bottom left, and analog stick input at bottom right. The permanent large title and full-width top and bottom bars were removed, so combat uses the full screen. The camera tilts down **55°** by default, closer to straight top-down than before, with a **50°** field of view, and follows the player smoothly. Click `CAMERA` at the bottom center to open the adjustment panel: the `TILT` slider sets the tilt between **40–80°**, `DIST` sets the distance between **18–38**, and `RESET CAMERA` restores the defaults. These parameters can also be changed in the Inspector. The camera panel is collapsed by default; click the button again to close it. Runtime adjustments are not saved across restarts.

Select `Playground` in the scene to adjust the fire interval, spawn interval, enemy speed, and bullet speed; select `Player` to adjust movement speed.

The original `SampleScene` is kept, and `Playground` is set as the first scene in the build list. The menu `Tools > Survivors > Create Playground Scene` rebuilds Playground, overwriting that scene.

## Verification

Runtime code and editor scripts were compile-checked using the local Unity 6 assemblies and Roslyn.

The wave state logic passed standalone C# checks covering five-wave timing, rest breaks, manual continuation, victory, death, restart, and long frames that cross a deadline. The check source is `Assets/Editor/WaveSessionChecks.cs`.

At the time, the local Unity returned `No valid Unity Editor license found`, so Play Mode and visual verification were not completed. With a valid license, the following command generates the scene and verifies WASD, auto kills, health loss, protection time, death, and R to restart:

```powershell
& '<UnityEditorPath>/6000.3.22f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<ProjectPath>' -executeMethod PlaygroundBuilder.BuildAndVerify -logFile '<ProjectPath>/Logs/PlaygroundVerify.log'
```

The success marker is `PLAYGROUND_VERIFY_PASSED`. This verification command rebuilds the Playground scene.

## Red fire breath

- The third weapon slot automatically breathes fire at the nearest enemy in range, with a starting length of **5** and a cone angle of **44°**. A short burst of red fire comes out of the character's mouth; each burst lasts **0.45 s**, dense up close and expanding outward, with **2 s** between the starts of two bursts.
- During a short burst, the flame deals **3 damage** every **0.075 s**, only to enemies inside the forward cone. Flame particle lifetime is limited by range, and particles vanish before the arena border.
- Hit enemies immediately switch to a charred material and keep burning for up to **2 s** after leaving the flame, taking **1 damage** every **0.5 s**, with rising embers.
- Charred enemies leave a charred husk for **2 s** after dying; material drops are unchanged. Fire breathing stops during rest breaks, victory, and death, and wave cleanup removes flames, burns, and husks.
- Flamethrower upgrades appear randomly in the shop, up to level 3, using the same weapon upgrade prices and stat rules. `Slot 3 / Fire Breath` in the Inspector adjusts base range, damage, and burst interval.

The fire breath no longer uses a gun-barrel shape; the mouth is on the face of the player cube. The fire and glow are all red, and the limited range, charring, and ember effects are kept.

The characters are tuned for the top-down camera: the head tilts up 15°, the eyes and eyebrows are about 25% larger, and the bangs are shorter. The default and reset camera angles are both 55°. Enemies randomly use one of three Blender models (horned, hooded, and rock), keeping 20 health, the original speed, and the drop rules; charring applies to the whole model. The model source file is `ArtSource/Enemies/BlockEnemies.blend`.
