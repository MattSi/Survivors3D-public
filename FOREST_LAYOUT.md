# Forest Camp: First Layout Pass

Starting a game or a new run in `Assets/Scenes/ForestCamp.unity` uses the new layout. The layout is generated at the start of each run and stays the same between waves. The boss fight clears combat obstacles; restarting restores them.

## Fixed skeleton

- The center keeps a clearing with a radius of at least 5.5 m.
- The west timber area uses stumps and crates; the east stone circle uses rocks. There are 8 fixed obstacles whose positions, sizes, and orientations do not change with the seed.
- Each side has a north and a south group of two obstacles each. The player can go around them or pass between them. Horizontally adjacent obstacles have at least about 1.6 m of clearance.
- The east–west and north–south crossing routes always keep at least 3 m of walkable space along their center lines.
- An outer square retreat lane centered on x/z = ±15 m keeps at least 2.4 m of walkable space and connects both sides and the central exits.
- By default, 2 extra random crates/barrels are placed, chosen only from 8 reserved spots in the north and south supply bays. Their positions, sizes, and orientations vary slightly without blocking the main routes. The total obstacle count in the Inspector supports 8–16.

The fixed skeleton lets players learn routes; the random supply obstacles only change local cover. The timber and rock areas look different, which helps with orientation, and they reuse the existing art assets.

## Movement intent

- Ranged: use the crossing routes to keep lines of fire, and reposition behind cover on either side.
- Melee: use a single obstacle to split chasers, and move in and out of fights from the clearing.
- Poison / lantern: walk short arcs around the side obstacles and use the gaps to pull enemies back; the lantern still needs line of sight to enemies.
- When surrounded: switch sides through the center, or move along the outer retreat lane. Dead ends, terrain damage, and destructible obstacles have not been added yet.

## Verification and limitations

`Tools → Survivors → Verify Forest Layout` checks 40 random seeds: skeleton stability, crate/barrel variation, central clearance, the two shortcuts, the closed outer loop, crossing on both sides, enemy pathfinding from spawn points to the center, 8 spawn points, player/projectile collision, boss cleanup, and restart recovery. It outputs `Logs/ForestLayout.png`.

The grouped obstacles exposed a problem where the old pathfinding kept re-selecting the same node after reaching a turn point. Reached nodes are now skipped so the path keeps moving forward.

This pass mainly verifies routes and mechanics; ground art and spawn-point direction design have not changed yet. Whether different builds actually favor different areas needs playtest records; it cannot be inferred from route tests alone.
