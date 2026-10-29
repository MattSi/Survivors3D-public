# Forest Stone King

`ForestBoss-Concept.png` is the approved AI concept reference; the actual model is built by `create_forest_boss.py` from simple beveled blocks.
The editable source file is `ForestStoneKing.blend`, and the runtime asset is `Assets/Resources/Characters/ForestStoneKing.fbx`.
`BossLeftArm` / `BossRightArm` are separate shoulder pivots, `FaceAnchor` sets the facing direction, and `Boss_Core` / `Boss_Seam` support the charge-up and rage glow.
The model is 3.2 m tall with a 1.05 m body hit radius, and solid collision stops the player from walking through it.

## Entering the fight

- It appears on the final wave (wave 20) of the default regular Mixed mode. This wave clears the randomly generated cover, hides the portals, keeps the surrounding forest, and stops regular spawning.
- The boss has its own health, 3600 by default. The code default matches the actual configuration asset, and it is not affected by the regular enemy health multiplier. These are the current buffed values and still need balancing through playtests with different loadouts.
- The wave editor adds **Final-wave Forest Stone King** (`最终波林地石王`) and **Forest Stone King health** (`林地石王血量`). With a custom total wave count, the boss follows the final wave; endless mode and Only test modes keep the timing rules.
- The final wave's timer does not trigger victory; you must defeat the boss. Settlement runs after the combat tick completes, so kills never clear the collections being iterated by weapons.
- Quick playtest: outside Play Mode, run **Tools > Survivors > Play Forest Stone King**. It uses a temporary wave 20 configuration with an upgraded rifle, twin blades, and frost, without modifying the default asset.

## Attacks

- **Combat loop**: the attack phase lasts about 18 seconds normally and about 14 seconds after half health. When time is up, the boss finishes its current combo and recovery, then runs at 9 m/s to the arena edge away from the player. Leaving deals no contact damage and is affected by frost and cover.
- **Rest at the edge and horde waves**: on arrival it rests for 8 seconds, stops attacking, and gains 50% damage reduction from all sources, but can still be killed. Below the health bar it shows `休息·减伤50%·兵潮` (Rest · 50% damage reduction · Horde). A normal horde has a total budget of 24 enemies with at most 18 alive at once; after half health, the budget is 32 with a cap of 24. Normal batches have at most 4 enemies, rage batches at most 5, and each batch has a 1-second ground warning. Entry points are spread over three arena edges; the edge closest to the player when the rest starts spawns nothing for the whole round. Spawns still check safe distance, cover, and occupancy. When the cap is reached, spawning pauses; after enemies are cleared it can refill up to the round's budget. Any unspawned slots are discarded when the 8 seconds end.
- **Charge back in**: when the rest ends, it first cancels any unfinished spawn warnings, then locks its charge path and warns for a full 1 second. The charge is followed by a slam with 2 seconds of recovery, then it re-enters the attack loop. Rest damage reduction ends when the return warning starts. Remaining minions keep fighting and count toward later caps; they don't vanish instantly. The half-health check waits for the full attack and recovery to finish, so it never changes attacks mid-rest or mid-return.
- Wakes up over 2 seconds. Approach speed is 3.6 m/s, rising to 4.6 after half health. After recovery it pauses for only 0.25 seconds; the old 11 m attack range limit was removed, so from far away it charges in on its own.
- **Slam**: a 3 m radius attack on the ground 0.9 m in front along the locked direction, with a 1.25-second warning, 32 damage, and 1.1 seconds of recovery for a single slam. In rage: 3.4 m radius, 1-second warning, 38 damage.
- **Charge into slam**: a wide arrow locks the whole path, leading the player by up to 1.8 m; 0.85-second warning, 18 m/s, up to 14 m. In rage: 0.7-second warning, speed 22, up to 16 m. Regular, combo, and return charges all use this speed. 26 damage, 32 in rage, blocked by cover and boundaries. After stopping, it warns for 0.6 seconds centered on the landing spot, then slams. Once the circle and path are locked they don't track the player, and long frames never skip the warning.
- **Two kinds of bone-needle volleys**: it alternates between a fan at the player's current position (five needles 18° apart) and a predictive volley (five needles 12° apart, leading by up to 5 m). The prediction accounts for the final lock time and projectile flight time. Raised arms, a glowing chest core, facing, and the name label at the top telegraph the charge-up. The direction locks 0.35 seconds before each volley fires, so turning after the lock lets you dodge. Normally: 1-second charge-up, one volley, 0.9 seconds of recovery. In rage: 0.85-second charge-up, two volleys about 0.65 seconds apart. Boss needles travel at 12 m/s with a 26 m range and 12 damage (14 in rage); regular shooters still use speed 8.5, range 12, and damage 8.
- **Half-health combos**: it cycles through "double volley → charge → slam" and "charge → slam → charge again → slam". Each extra charge re-aims and gives a full warning. No recovery or summons are inserted mid-combo; after the final slam there is one shared 2-second recovery, and the top shows `连招·冲锋 / 连招·重砸` (Combo · Charge / Combo · Slam). The regular-phase charge into slam also keeps its 2-second recovery.
- **Small reinforcements during the attack phase**: 6 seconds after the start or the return, it waits for a gap between attacks to summon. After that it summons 3 regular melee enemies every 10–12 seconds normally, or every 7–9 seconds in rage. Right after the half-health awakening it summons a wave of 4 first, and stops adding more once 6 minions are alive. Spawn points are spread around the boss with a 1-second ground circle warning, and at spawn time it re-checks that they are at least 3 m from the player; if the player walks near a warning, that spawn is cancelled. Summoning is followed by 0.6 seconds of recovery. Minions use regular enemy health, speed, and material drops, and are cleaned up with settlement after the boss dies; defeat and restart also cancel unfinished summons and hordes.
- **Stone armor vs. bullets**: direct rifle hits on the boss deal 50% damage (rounded, minimum 1); stacked with the rest damage reduction, that is about 25% of the original damage. Other weapons and damage over time are only halved during rests. Damage rules for regular enemies are unchanged. The health bar, name, and attack telegraphs are merged into the top status area.
- After half health, once a full recovery period ends, a one-time 1.5-second rage awakening triggers. It never steals a damage window or changes an attack that is already locked. Long frames still keep the full awakening time, and the chest core and cracks glow brighter.
- All attacks respect the player's existing shield, dash protection, and post-hit invulnerability. Recovery periods deal no contact damage, leaving room for melee.
- Frost slow, burning, poison, crits, and life steal still work. It is immune to weapon knockback; the hammer adds at most 0.4 seconds of stagger per recovery period, preventing permanent control.
- Area damage and melee range are measured against the boss's surface; regular enemies keep their original range rules.

## Verification

**Tools > Survivors > Verify Forest Stone King**, or Unity `-batchmode -executeMethod ForestBossChecks.Run` (don't add `-quit`).
It checks the final-wave victory condition, the configured health, the model and arm raise, long-range offense, charge into slam, dodgeable and bounded half-health combos, full recovery windows, both volley types and turning after the lock, independent boss and regular-shooter values, summon warnings/pacing/caps/safe distance/drops, long frames, obstacles, all 11 weapons, status effects, and death/restart/settlement cleanup.
The horde check also outputs `FOREST_BOSS_HORDE_PASSED`, covering the full loop, the 24/32 total budgets, the 18/24 alive caps, the whole-side open lane, damage reduction, long frames, and cleanup. The overall output is `FOREST_BOSS_CHECKS_PASSED`. In graphics mode, it captures screenshots of combat, slam, volleys, summons, minions, the horde (`ForestBoss-Horde.png`), and the return (`ForestBoss-Return.png`).

Regenerate the model:

```powershell
& '<BlenderPath>/blender.exe' --background --threads 4 --python ArtSource/Enemies/create_forest_boss.py
```
