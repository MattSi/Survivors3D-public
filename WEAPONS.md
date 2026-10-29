# Weapons and Equipment Choices

Poison build: a new shop item, the **Slow-Release Valve**, adds +3 seconds to poison pool duration and changes the spray interval from 0.8 to 1.0 seconds. **Tools → Survivors → Poison Build Lab** compares poison, frost, the valve, and equal-cost upgrades. See [Poison Trail Herding](POISON_BUILD.md).

Close-range build experiment: **Tools → Survivors → Melee Build Lab** provides item-by-item comparisons of twin blades, range, life steal, and the hammer, free loadouts, and per-wave damage/healing stats. Playtest steps, first-round results, and limitations are in [Close-Quarters Shredding Experiment](MELEE_BUILD.md).

Each wave lasts 30 seconds, with 20 waves in total. There are currently 12 weapons. At the start you pick 1 of the 12; you can equip up to 3 different weapons at once, each up to level 3. Buying a new weapon in the shop costs 20 materials, and upgrades cost 20, then 25 materials.

When buying with all slots full, you first choose which weapon to replace, then pay. Cancelling costs nothing. Replacing refunds no materials, and the old weapon's level is lost. A replaced weapon can be bought again later, starting at level 1. Existing passive items are kept; shop cards that no longer have a matching weapon are removed, and refreshes generate cards for the new equipment.

| Weapon | Base damage | Interval | Range | Mechanics |
| --- | ---: | ---: | ---: | --- |
| Three-round burst rifle | 6 per shot | 0.7 s between bursts, 0.1 s within a burst | 14 m | Three-round burst, each shot re-selects a target; no default splash, ammo upgrades kept; wooden stock and iron body, muzzle flash, bullet tracer, and recoil; hits flash white with slight knockback |
| Orbiting twin blades | 10/12/14 | Same-enemy hit cooldown 1.25/1.125/1 s | 2.2/2.42/2.64 m | Two short, wide iron blades orbiting once every 2 s; wooden grips, cloth wraps, dark guards, and light edges. A level 1 regular hit kills a 10-health wave 1 enemy |
| Flamethrower | 3/4/5 direct damage per hit | 2/1.8/1.6 s (Concentrated Fuel adds cooldown) | 5/5.5/6 m | Cone of flame plus burning; its own damage scaling |
| Spear | 24 | 0.75 s cooldown after it returns | Targets within 8 m, flies until a wall or obstacle | Held at the ready, 0.12 s wind-up, thrown in a straight line at 20 m/s; prefers lines that skewer several enemies, pierces infinitely with no damage falloff, and hits each enemy once per throw; stays stuck in a wall for 0.2 s before returning; only one on the field |
| Crossbow | 12 per bolt | 1.2 s | 16 m base | Fires 2/3/4/5 bolts at levels 1–4 in a 15/25/35/45° fan; several bolts can hit the same target, with regular crits; piercing comes from ammo items |
| Earthbreaker (hammer) | Instant kill at center | 1.5 s | Detects enemies within 3 m, picks a dense landing spot within 2 m | Prefers the most center kills, then the most outer hits, then the closer spot on ties; slams down 0.25 s after raising; the 1.5 m center crushes and flattens, and the 2.8 m outer shockwave knocks back 0.4 m, then stuns for 1 s |
| Frost Field | 20/25/30 on landing; 4 for later entry | 2.8 s | 2.5/2.9/3.3 m | Ice crystals fall from 5 m up and deal area damage after 0.35 s, leaving frost for 1.6/2/2.4 s; slows by 35/40/45% inside the area, no stun, no projectiles |
| Torch | 12/14/16 | 1 s | 2.8 m; sweep of 140/160/180° | 0.14 s backswing, 0.18 s sweep, 0.16 s recovery; hits each enemy once per swing, ignites and knocks back 0.9/1.2/1.5 m; attacks and knockback are blocked by walls and obstacles |
| Fireball | 12/17/22 | 1.2/1.08/0.96 s | Range 12/13.2/14.4 m; explosion radius 1.5/1.65/1.8 m | Auto-aims at the current position of the nearest visible enemy and flies straight at 14 m/s; explodes on the first enemy, an obstacle, or at max range, hitting each enemy once; area damage is blocked by obstacles |
| Boomerang Blade | 14/19/24 per pass | 1.1/0.99/0.88 s after it returns | Flies out 7/7.7/8.4 m | Automatic piercing throw, hitting each enemy once on the way out and once on the way back; returns to the player's current position, so strafing or dashing changes the return path; hitting an obstacle starts the return early, and a blocked return is caught with no damage |
| Poison Backpack | 3/4/5 per tick | Spray every 0.8 s, poison ticks every 0.5 s | Pool radius 0.8/0.95/1.1 m | Automatically sprays behind the most recent actual movement direction, landing after 0.18 s; pools last 3/3.5/4 s, and enemies stay poisoned for 1 s after leaving; overlaps don't stack damage; it doesn't hurt the player and can't crit |

Weapon upgrades add 5 base damage and 10% base range, and reduce the base attack interval by 10%. The hammer's center is an instant kill and doesn't use a damage value; the old Ghost Axe kill scaling was removed. Before landing, the target spot follows the player and always stays within 2 m in front. Center kills leave a flat husk for 0.6 s. Enemies stunned by the outer ring stop moving and dealing contact damage; enemies outside the shockwave range or behind obstacles are unaffected. Ground cracks fade after about 1.1 s.

Magazine, Armor-Piercing Rounds, Incendiary Rounds, Ricochet, and Split apply to guns and the crossbow. Frost Field doesn't use ammo upgrades. Melee range and life steal apply to the twin blades, spear, hammer, torch, and boomerang blade. Burn items require the flamethrower, the torch, or an ammo weapon together with Incendiary Rounds. General crit, health, and economy items stay available.

Select the starting weapon with the keyboard arrow keys or gamepad stick and confirm with Enter / A. The replacement screen supports the arrow keys, Enter / A, Esc / B to cancel, and the mouse.

Verification entry point: Unity menu `Tools > Survivors > Verify Weapon Expansion` (run outside Play Mode); success log `WEAPON_EXPANSION_PASSED`. The pure C# checks are in `WaveSessionChecks.Run()` and cover eleven starters, filtering, full-slot replacement, and charging. The Unity runtime checks cover hammer flattening, area stun, attack timing, and cleanup.

## Single-shot fireball

The fireball is the ninth starter and shop weapon (weapon ID 8). It uses its own magic hit type and supports general crit and general on-hit items, but not ammo, melee, or burn upgrades. The base fireball doesn't home, pierce, split, or ignite. Damage, crit, and explosion radius are locked in at launch; the target dying doesn't change its flight direction.

The visuals use a runtime-generated flat-shaded octahedron fire core, a short orange geometric trail, and a 12-segment ground impact ring; explosion fragments shrink and fade within 0.35 s. No external effect assets are needed. Changing waves and restarting clear projectiles and explosions; death clears fireballs in flight. The starter screen shows weapon cards in four columns, with a description of the selected weapon below.

`WeaponExpansionChecks.Capture` covers single shots and cooldown, hits during long frames, single area damage, explosion at max range, wall blocking, upgrade crits, death cleanup, and purchase/replacement, and outputs `Logs/Fireball-Flight.png`, `Logs/Fireball-Impact.png`, and `Logs/Fireball-Starters.png`.

Fireball visibility fix: once equipped, a floating fire core and three sparks stay at the player's shoulder, so the weapon is recognizable even with no target; during cooldown the core shrinks and then recovers. The projectile launches from the same side, with a larger core and trail and a raised visible model; near a wall, the launch origin falls back to the character's position to avoid passing through the wall. Area damage and collision still use the original checks. Verification adds idle display with no target, hiding on cleanup, and automatic launch and kill over a full combat tick.

Fire core animation: the small core orbits the shoulder in an ellipse with a 2.6 s period, bobbing slightly and spinning; the three sparks circle the core once per second. With a target, the core shrinks and brightens during the last 0.22 s of cooldown; on launch it flicks toward the target for 0.18 s, then recovers from a smaller, dimmer state. The idle core uses its own material, so the animation doesn't change the color of the projectile or explosion. It advances with combat time, stops when paused, and resets on wave changes, death, and unequipping.

## Defensive torch rework

The thick wooden handle, cloth-wrapped head, iron bands, and rivets use beveled blocks; the top has three clusters of orange geometric flame with a yellow inner flame that flickers with a few sparks. At idle it's held up at the side; when attacking it swings back, then sweeps, and the short arc trail only shows during the swing. Melee crit, life steal, range, and burn items are kept. Torch upgrades are an exception: each level adds only 2 damage, and range and interval don't change with level; upgrades mainly increase sweep angle and knockback distance.

Runtime verification covers no early hits during wind-up, one hit per enemy per sweep segment, upgraded angle/knockback, obstacle blocking, and cleanup. In-game screenshots are `Logs/Torch-Idle.png`, `Logs/Torch-Windup.png`, and `Logs/Torch-Sweep.png`.

Torch swing effect upgrade: a wide orange arc connects 12 geometric flame tongues with yellow cores, unfolding along the path swept by the torch tip. It lingers for 0.22 s after the attack, drifting upward and shrinking. The flame on the torch head grows during the swing. Effect objects are reused and hidden on wave changes, death, and unequipping the torch; hit range and damage rules are unchanged.

Torch range and visual upgrade: the base hit radius grows from 2 m to 2.8 m, the sweep angles for the three levels become 140/160/180°, and the fire arc trail keeps up to 105°. The swing model and fire arc extend 40% further outward together. The head flame is wider and taller, and grows further to 1.75× during a swing. Damage is 12/14/16, so a level 1 enemy with 10 health dies in one hit without a crit.

## Stat and item fixes

- Upgrade cards show each weapon's base stats as current level → next level, making clear that item bonuses stack separately; long descriptions can be scrolled.
- With only the hammer, the Whetstone and Hunting Badge are not offered; they return when paired with a weapon that can crit, and items already owned are kept. With only the hammer, the Vengeance Charm is offered only after the Magnetic Coil provides extra damage.
- Burning first applies Concentrated Fuel and Vengeance bonuses, then accumulates fractional damage per enemy; re-igniting doesn't reset the accumulated value. Without Vengeance, ten burn ticks with 0/1/2/3 stacks of fuel total 20/28/36/44.

## Boomerang Blade

The tenth weapon, built from the approved concept as a low-poly shallow-V twin-wing model with a silver blade, wooden handle, dark iron bands, and light wraps. It needs no new input keys, can be a starter or bought for 20 materials, and takes one weapon slot. Detailed parameters and in-game images are in the [Boomerang Blade notes](ArtSource/Weapons/BOOMERANG.md).

The Unity menu `Tools > Survivors > Verify Boomerang Blade` verifies two-way hits, piercing, catching while moving, obstacles and boundaries, long frames, purchase and upgrades, gamepad card selection, and cleanup on wave change, death, and unequipping.

## Poison Backpack

The eleventh weapon, using the approved wood-and-iron square canister icon and a back-mounted model in the same style. It automatically sprays behind the last actual movement direction. Standing still doesn't change the direction, turning the camera doesn't change it, and short dashes don't raise the spray rate. Poison pools don't stack damage; enemies stay poisoned for 1 s after leaving a pool, and re-entering only refreshes the remaining time. Poison and burn are independent; the white hit flash and frost tint take priority over the poison tint. Poison damage can trigger general on-hit effects and benefits from Vengeance, but doesn't use crit, melee, ammo, or burn-specific bonuses. With only the backpack, the Whetstone and Hunting Badge are not offered.

The first purchase costs 20 materials and takes one weapon slot, up to level 3. Pools and sprays are blocked by obstacles, and the ground shape is clipped at obstacle edges. Pools and bubbles are reused, with at most 8 pool objects. Death, settlement, restart, or unequipping clears all poison areas and poison status; pausing freezes the timers. Details are in [Poison Backpack](ArtSource/Weapons/POISON.md); the verification entry point is `Tools > Survivors > Verify Poison Backpack`.

## Bait Lantern

Automatically placed about 1.5 m behind the most recent movement direction; it waits when no enemies are nearby. At most one at a time. It takes one weapon slot, costs 20 materials, and upgrades cost 20 / 25 materials. It can be a starter. When it is smashed or expires, it bursts into flame for 24 / 32 / 40 damage within a 2.2 m radius. The burst doesn't hurt the player, can't crit, doesn't apply burn, and is blocked by walls. Elites and bosses are only immune to the lure and still take burst damage; the lantern is placed automatically even when only these enemies are present. Unequipping, death, and wave cleanup don't trigger the explosion.

| Level | Durability | Lure radius | Duration | Cooldown (from placement) |
| --- | ---: | ---: | ---: | ---: |
| I | 30 | 5 m | 5 s | 8 s |
| II | 40 | 5.5 m | 5.5 s | 7 s |
| III | 50 | 6 m | 6 s | 6 s |

Regular chasers instead approach and attack the lantern, each attacking at most once per second for 10 durability per hit. Elites, splitters and their offspring, and bosses keep their original behavior. Obstacles block the lure; placement checks the path, the stand's footprint, and the arena boundary. The player still takes contact damage. After the lantern is smashed or expires, chasers resume chasing, and it is placed again automatically when the cooldown ends.

The green bait bottle, wooden stand, and iron bands follow the existing low-poly model style; a green bar above shows durability, and the bottle pulses during its last second. The model is reused, and timers stop while paused. Unequipping, death, and wave cleanup remove the lantern and its attack records.

How to play: turn near an old poison pool, leave the lantern by the poison trail, then walk a short arc to pull enemies back. The weaknesses are that clustered chasers break the lantern quickly, elites aren't controlled, and the burst has a long interval. There are no balance conclusions from extensive play yet.

Playtest: **Tools → Survivors → Poison Build Lab → loadout "Bait: Poison II + Frost I + Lantern I"** (`诱饵：毒液Ⅱ＋寒霜Ⅰ＋灯笼Ⅰ`). Verification: **Tools → Survivors → Verify Bait Lantern**.
