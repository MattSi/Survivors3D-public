# Poison Backpack

Automatically sprays poison about 1.15 m behind the last actual movement direction. No new buttons are needed: the direction is kept after you stop, turning the camera doesn't change it, and short dashes keep the 0.8-second spray interval. By circling or retreating, the player can lead chasers into the poison pools.

| Level | Damage every 0.5 s | Pool radius | Pool duration |
| --- | --- | --- | --- |
| 1 | 3 | 0.8 m | 3 s |
| 2 | 4 | 0.95 m | 3.5 s |
| 3 | 5 | 1.1 m | 4 s |

- Sprays every 0.8 seconds; the poison takes effect after a 0.18-second flight to the ground, with no instant damage on landing.
- Damage ticks every 0.5 seconds once an enemy enters a pool. Overlapping pools use only the highest poison damage, and staying in contact doesn't reset the tick timer.
- Poison lasts 1 second after leaving a pool; re-entering only refreshes the duration and doesn't add an extra tick.
- Doesn't hurt the player and can't crit; doesn't use melee, ammo, or burn-specific bonuses. Supports general on-hit and Vengeance effects.
- Poison can't hit through walls; spray distance and the ground polygon are both limited by arena obstacles.
- Pools shrink gradually over their last 0.35 seconds, and the hit area shrinks with them.
- Can be picked as a starter, or bought in the shop for 20 materials, taking one weapon slot. Upgrades cost 20 and 25 materials.

Visuals: a beveled wooden square canister, dark iron bands, two shoulder straps, a yellow-green liquid level window, and a short nozzle. Pools have dark edges, a yellow-green inner layer, and square bubbles. Poisoned enemies are tinted on their lower half; the white hit flash and frost effect still take priority.

## Files

- `Poison-Backpack-Icon.png`: a transparent icon, drawn at runtime by `PoisonBackpackIcon` and used in the equipment bar and shop.
- When the verification tool passes, it generates three Unity in-game screenshots: `Poison-Backpack-Held.png`, `Poison-Backpack-InGame.png`, and `Poison-Backpack-Starters.png`.
- `Assets/Scripts/Core/CombatController.Poison.cs`: the model, pools, damage, and status cleanup.
- `Assets/Editor/PoisonChecks.cs`: menu `Tools/Survivors/Verify Poison Backpack`, log `Logs/poison-checks.log`.

Weapon ID 10, shop ID 29; existing weapon and passive item IDs are kept.
