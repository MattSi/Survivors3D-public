# Boomerang Blade

The design is based on `Boomerang-Blade-Concept.png`: a wide shallow-V twin-wing blade, a wooden grip, two light wraps, and dark iron bands. In game it uses a procedurally generated low-poly mesh with separate silver beveled faces, reusing the existing weapon materials and resource release mechanism.

## How it works

- Available as a starting weapon; the first shop purchase costs 20 materials and takes one weapon slot (up to 3 slots).
- Automatically throws at a visible enemy with no extra button; keyboard/mouse and gamepad work the same.
- Pierces through crowds on the way out, and turns back when it reaches its range or hits an obstacle. Each throw hits the same enemy once on the way out and once on the way back.
- The return always follows the player's current position. Strafing or a short dash can steer the return path into another row of enemies.
- Obstacles block its flight and damage. If the return is blocked, it shrinks and is recalled on the spot without damaging enemies, so it never gets stuck.
- Supports melee range, Whetstone, life steal, and general damage effects; doesn't use ammo pierce, ricochet, or fire-rate bonuses.

| Level | Base damage per pass | Throw distance | Cooldown after return |
| --- | --- | --- | --- |
| 1 | 14 | 7 m | 1.1 s |
| 2 | 19 | 7.7 m | 0.99 s |
| 3 | 24 | 8.4 m | 0.88 s |

Outbound speed is 12 m/s and return speed is 17 m/s; a single blade is thrown in a loop. All three levels use regular melee crits, with a 0.45 m hit radius per pass.

## Code and verification

- `Assets/Scripts/Core/CombatController.Boomerang.cs`: the model, flight, separate hit records for each pass, obstacle handling, and cleanup.
- `Assets/Editor/BoomerangChecks.cs`: Unity menu `Tools/Survivors/Verify Boomerang Blade`.
- In-game screenshots: `Boomerang-Blade-Held.png`, `Boomerang-Blade-InGame.png`, `Boomerang-Blade-Return.png`, `Boomerang-Blade-Starters.png`.
- Weapon ID 9, shop ID 28; existing weapon and item IDs are unchanged. The starter screen expands to 4 columns, and gamepad up/down navigation skips empty spots in the last row.
