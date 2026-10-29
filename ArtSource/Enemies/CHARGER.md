# Charging imp

The approved orange block imp is implemented as `Assets/Resources/Characters/EnemyCharger.fbx`.
Its editable source is `BlockCharger.blend`; regenerate the 748-triangle single-mesh model and
preview with Blender's `--background --python ArtSource/Enemies/create_charger.py`.
It uses the existing `FaceAnchor`, shared material palette and enemy avatar rendering system.

## Encounters

- Waves 1–2: no charging imps.
- Waves 3–7: every sixth successful enemy arrival becomes a charger, up to two alive.
- Waves 8–12: every fifth arrival, up to three alive.
- Waves 13–20: every fifth arrival, up to four alive.
- Chargers replace normal enemies and use their own 20 base HP times the wave health multiplier, with normal material drops.
- Only one can prepare/charge at once before wave 8; two thereafter.

## Behavior

Chase with the normal wave speed. At 2.5–7.5 metres and with an unobstructed approach,
lock the player's direction and spend 0.9 seconds preparing. Crouch and lean forward,
light the horns/eyes amber, and display a translucent orange ground arrow.
The arrow marks the fixed path; it does not follow a player who sidesteps.

Charge at 10 m/s for the locked target distance plus 2 m, capped at 9 m and 1.6 seconds.
Deal 15 damage at most once per dash, checked over the whole movement segment.
Obstacles and arena walls stop the enemy's footprint. Frost slows movement;
hammer stun cancels both windup and dash. The charger pauses for 1.1 seconds afterward,
then resumes pursuit with a 2-second charge cooldown. Windup/recovery have no contact damage.
Ordinary pursuit retains the existing 10-damage contact attack.

Warning geometry and state are removed on death, defeat, wave cleanup and restart.
`CombatController.Charger.cs` owns behavior; `EnemyAvatar` owns pose and material glow.

## Verification

Unity menu: **Tools > Survivors > Verify Charging Imp** (outside Play Mode).
The checks cover wave gating, FBX rendering, locked aim and sidestepping, swept damage,
obstacle/border collision, recovery, frost, stun interruption and cleanup, plus the existing
pure C# wave/equipment/shop checks. A scene render is written to `Charger-InGame.png`.
Success marker: `CHARGER_CHECKS_PASSED`.
