# Hooded bomber

The purple cuboid hood, orange eyes, belt pouch and small bomb follow the approved concept.
`EnemyBomber.fbx` contains a body mesh, a separately pivoted `ThrowArm` mesh and `HeldBomb` prop,
768 triangles in total. `FaceAnchor` preserves the existing enemy orientation workflow.
Editable source: `BlockBomber.blend`. Rebuild with Blender
`--background --python ArtSource/Enemies/create_bomber.py`.

## Initial balance

- Unlocks on wave 8. Every ninth successful arrival is eligible, after charger/stone priority.
  Replaces a regular arrival within the existing spawn and population budgets.
- At most 2 bombers on waves 8–12, 3 on waves 13–20; 24 enemy-owned base HP times the wave health multiplier, with normal material drops.
- Approaches beyond 7 m at 80% normal speed, retreats inside 4.5 m at 70% speed,
  and holds the middle distance. Retreat steps respect arena walls and obstacles.
- Can throw at 4.5–9 m with a clear approach. Locks the target position and shows the full
  blast warning during the 0.65 s arm windup. Hammer stun or death cancels unreleased throws.
- Flight takes 0.9 s along a parabolic arc; the landed bomb waits 0.75 s before exploding.
  Ground warnings pulse during the fuse. Blast radius is 1.4 m; damage is 15, once per bomb.
  The player's ground center determines whether it is inside the displayed circle.
- Cooldown is 2.8 s after release. At most 2 pending attacks (windups + bombs) before wave 13,
  3 thereafter. Moving away from the locked circle avoids the blast; solid arena cover blocks it.
- Frost slows movement, not the fuse. Existing contact damage applies if cornered.
- Already released bombs remain dangerous after their thrower dies. Wave cleanup, restart,
  defeat and controller disposal remove all bomb visuals and warnings immediately.

## Verification

Unity menu: **Tools > Survivors > Verify Hooded Bomber** outside Play Mode.
Includes the stone/charger regression suites, normal wave rules, movement/range and frost,
arm raising, fixed targeting, flight/fuse timing, inside/outside radius damage, cover,
stun/death cancellation, cleanup and attack/population caps.
Success: `BOMBER_CHECKS_PASSED`. Real scene capture: `Bomber-InGame.png`.
