# Poison Trail Herding: The Second Build

The Poison Backpack lays down a trail, the Frost Field keeps chasers near the route, and the Slow-Release Valve extends how long old poison pools stay useful. Walk short arcs or horseshoe shapes, and when you turn, lead chasers back into poison areas that haven't faded yet. The third weapon slot can add range or emergency crowd clearing.

## The Slow-Release Valve in a normal run

- Costs **30 materials**, at most one. It enters the random shop once you hold the Poison Backpack and have finished wave **4**.
- Poison pools last **3 seconds** longer: **6/6.5/7 seconds** at levels 1/2/3.
- Trade-off: the spray interval goes from **0.8 to 1.0 seconds**. Damage, pool radius, poison tick rate, and poison duration after leaving a pool are unchanged.
- The existing rules stay: no crits, overlaps don't stack damage, and walls block it. Short dashes don't raise the spray rate.
- A gold valve wheel appears on the backpack. The shop, the Chinese/English codex, a dedicated valve icon, and the weapon upgrade descriptions are all wired up.
- Replacing the backpack clears all poison pools and poison status; a valve you've bought is kept, and re-equipping the backpack restores its effect. A new run clears it.

Buy backpack upgrades first to enlarge pools, then add frost, then decide whether to buy the valve. Buy the valve if you want to keep leading enemies back along old routes; if you need to change direction often or clear a new path right away, consider upgrading frost or adding a third weapon instead.

**The movement cost is real:** at a base speed of 5 m/s, the theoretical spray spacing when moving in a straight line at full speed grows from 4 m to 5 m. It's not a continuous poison carpet, and moving faster widens the gaps further. Ranged enemies that don't chase into pools, being forced to change course when caught in a pincer, and new enemies outside the poison area are all weaknesses of this build.

## Build experiment entry point

Unity: **Tools → Survivors → Poison Build Lab**. Choose a loadout, a scenario, and a seed, then enter Play Mode; you can also restart the experiment inside Play Mode.

| Group | Initial loadout (backpack start is free) | Actual cost |
|---|---|---:|
| 0 | Poison II | 20 |
| 1 | Poison II + Frost I | 40 |
| 2 | Poison II + Frost I + Slow-Release Valve | 70 |
| 3 | Poison II + Frost II + one Speed | 70 |

The initial budget is fixed at 100, and unspent materials are kept. The first three groups add one component at a time; the last two are same-cost alternatives. The first three scenarios are wave 8 regular enemies, stone-armored enemies, and mixed enemies; there is also a final boss entry point. The configuration is a deep copy of the default asset, applies only to the current Play Mode session, and doesn't modify the default asset.

The window shows poison damage/kills, frost damage, effective poison damage while targets are slowed by frost, and actual damage taken. Stats reset at the start of each wave and on restart, and are kept after death and settlement; export manually to `Logs/PoisonBuildLab.csv`. The initial cost doesn't include later shop spending. "Poison damage while slowed" here means both effects were active at once; it doesn't equal all the damage the slow created on its own.

## Verification and limitations

- **Verify Poison Build** / `-executeMethod PoisonBuildChecks.Run`: compatibility between old and new item IDs, shop filtering/unlocking/locking/buying/replacing, loadout costs, the valve wheel, spray cadence, extension/expiry, pool count during long frames, no damage stacking on overlap, poison after leaving a pool, frost-poison stats, pause/death/settlement, and wave rules. Marker `POISON_BUILD_CHECKS_PASSED`.
- **Verify Poison Backpack**: the original backpack regression, marker `POISON_CHECKS_PASSED`.
- **Probe Poison Build Movement**: 4 loadouts × 3 scenarios × 3 routes, 36 runs in total. 0.05-second step, at most 20 seconds per run, seed 31415, starting at full health. The routes are a short circle with a radius of about 3.2 m, a wide circle of about 12 m, and standing still. Real movement updates drive the rear spray direction. Outputs `Logs/PoisonBuildProbe.csv`.
- Field effect screenshot: `Logs/PoisonBuild-Field.png`.

The automated routes don't use the dash or plan paths around obstacles. A fixed seed only fixes the initial conditions; random calls during combat diverge with the route. Short results can't replace human play, the probability of the build coming together from shops, or a full boss clear; the valve isn't a must-buy upgrade on every route.

## First-round results (2026-09-16)

A 1.2-second spray interval was tried first. Short comparisons showed the trail cost was too high, so it was changed to the current 1.0 seconds; duration, poison damage, and range were not buffed to compensate. The initial data is saved in `Logs/PoisonBuildProbe-1.2.csv` and the current version in `Logs/PoisonBuildProbe.csv`, 36 runs each.

| Current version scenario/route | Loadout | Survival time | Poison damage | Frost damage | Poison damage while slowed |
|---|---|---:|---:|---:|---:|
| Regular chasers / short circle | Poison II + Frost I | 13.55 s, died | 138 | 92 | 0 |
| Regular chasers / short circle | Full valve loadout | 16.20 s, died | 172 | 128 | 20 |
| Mixed enemies / wide circle | Poison II + Frost I | 13.90 s, died | 151 | 144 | 48 |
| Mixed enemies / wide circle | Full valve loadout | 16.75 s, died | 190 | 229 | 94 |
| Mixed enemies / short circle | Full valve loadout | 14.40 s, died | 196 | 157 | 28 |
| Mixed enemies / short circle | Same-cost frost upgrade + speed loadout | 17.05 s, died | 134 | 354 | 14 |

The valve does change the trail rhythm and gives longer engagement time on some routes, but **leading enemies back on a short circle does not yet reliably beat a same-cost direct upgrade**, and the mixed-enemy scenario is still fragile. What's delivered now is a feature-complete build prototype ready for playtesting and tuning; it can't be claimed that the second build has passed balance sign-off. The next round should have a human player use the dash, actively choose routes that lead enemies through pools, and evaluate how a third weapon covers the build's weaknesses.

## Implementation conventions

The existing item IDs 0–29 are kept. The Slow-Release Valve uses item ID 30 and passive enum index 16. New passives always convert through `ItemLibrary.ShopId / Index`, no longer assuming every passive item ID equals the enum value plus 6; the existing weapon IDs 22–29 are unchanged. Codex counts, navigation, and the icon cache use the catalog count.

The longest 7-second pool with a 1.0-second interval, plus the 0.18-second landing delay, still doesn't exhaust the existing limit of 8 pool objects. Duration and damage are snapshotted when a pool spawns; death, settlement, unequipping, and restart reuse the existing cleanup paths.
