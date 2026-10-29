# Bone-Needle Shooter

The model is based on `Shooter-Concept.png`: a gray-green block body, a bone-white firing arm, orange eyes and muzzle, and three bone needles on its back.
`BlockShooter.blend` is the editable source file, and `create_shooter.py` regenerates the model and `Shooter-preview.png`.
Run Blender with `--background --python ArtSource/Enemies/create_shooter.py`.
It exports `Assets/Resources/Characters/EnemyShooter.fbx`, 782 triangles in total.
Separate `ShootArm`, `Muzzle`, and `Breech` nodes support raising the arm and positioning the muzzle; `FaceAnchor` follows the existing model facing workflow.

## Combat and configuration

- `EnemyKind.Shooter` and `ShooterOnly` are appended to the end of their enums, so existing asset type numbers are kept.
- From wave 6, every 8th successful spawn is eligible to replace a regular enemy; the existing special enemy priority is kept.
  Waves 6–12 allow at most 2 alive and 1 attack slot; from wave 13, at most 3 alive and 2 attack slots.
  Attack slots include charging up and needles in flight, so shooters don't fire at the same time and create bullet hell.
- Base health is 18, multiplied by the current wave's health multiplier; contact damage, drops, and kill scoring follow the regular enemy rules.
- Beyond 8.5 m it approaches at 70% of base speed, and between 4.5 and 8.5 m it keeps its distance.
  Closer than 4.5 m, it backs off at 55% of base speed for up to 0.6 seconds, then stops for 1.8 seconds.
  Frost affects movement; backing off is limited by obstacles and the arena boundary.
- It charges up at 4.5–9 m when it has line of sight and is inside the screen's safe area. The arm rises, the muzzle glows orange, and a thin line on the ground shows the trajectory.
  It tracks for the first 0.5 seconds and locks its direction for the last 0.3 seconds. Long frames still keep at least the full lock window.
  A hammer stun, death, or leaving the screen cancels an attack that hasn't fired yet.
- A single straight bone needle: speed 7 m/s, range 12 m, damage 8, collision radius 0.12 m, with a short orange trail.
  After firing, the cooldown is 2–2.4 seconds, so the full cycle including charge-up is about 3 seconds. It stops briefly after firing.
- Needles use swept collision over their whole movement, and cover blocks them first; the player's invulnerability time and shield apply.
  Needles already fired keep flying after the shooter dies; wave settlement, defeat, restart, and destruction clear them immediately.
- The default configuration asset has the shooter stage added; other existing configurations are unchanged. Custom configurations need a Shooter stage added by hand.
  `ShooterOnly` in the `Tools > Survivors > Wave Configuration` menu lets you test it alone from wave 1.

## Verification

Outside Play Mode, run `Tools > Survivors > Verify Bone Needle Shooter`, or Unity batchmode
`-executeMethod ShooterChecks.Run` (don't add `-quit`; the verification exits on its own).

Verification covers model import, arm raise, wave rules, approach/back-off/frost, lock and strafing, hits during long frames, cover, off-screen limits,
shield/invulnerability, stun/death, range, pause, count/attack slots, defeat/settlement cleanup, and the mixed lineup.
The success marker is `SHOOTER_CHECKS_PASSED`; the in-game screenshot is `Shooter-InGame.png`.
