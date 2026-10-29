# Stone-armored rockling

`EnemyStoneArmor.fbx` is the approved wide-shouldered gray stone monster with moss patches,
heavy forearms and a stepped chest seam. The single mesh has 1,056 triangles. Source:
`BlockStoneArmor.blend`; regenerate with Blender `--background --python ArtSource/Enemies/create_stone_armor.py`.

## Initial balance

- Starts on wave 5. Every seventh successful arrival is eligible, yielding to a charging imp
  when both schedules coincide. Replaces an ordinary enemy and uses the existing total enemy cap.
- At most 2 alive on waves 5–9, 3 on waves 10–14, and 4 on waves 15–20.
- Base health is an enemy-owned 120 HP, multiplied by the explicit wave health multiplier (default 1). No damage-type immunity. A level-one hammer deals 38 center damage, so an unmodified stone survives with 82 HP.
- Movement is 55% of normal wave speed. Frost and hammer stun still work.
- Height 1.32 m versus ordinary enemies' 1.02 m; collision radius 0.6 m versus 0.375 m.
- Solid capsule blocks player movement. Contact retains 10 damage and the normal invulnerability window.
- Small enemies yield more during separation. Chargers stop at the stone's body.
- Projectile and spear bounds, orbiting blade reach, obstacle clearance and knockback bounds
  use the enlarged footprint. The model has a restrained slow pulse rather than a springy bounce.
- Uses normal material drops. A dead or cleared stone immediately loses its solid collider.

## Verification

Unity menu: **Tools > Survivors > Verify Stone Armor**, outside Play Mode.
Includes the charging-imp regression checks, stone wave gating/caps, model size, HP, movement,
frost/stun, real CharacterController blocking and contact damage, grazing projectile hits,
spacing, border and obstacle clearance, death/cleanup, and charger collision.
Also runs the pure C# wave/equipment/shop checks. Success marker: `STONE_ARMOR_CHECKS_PASSED`.
`StoneArmor-InGame.png` is a real scene render; `StoneArmor-preview.png` is the Blender model render.
