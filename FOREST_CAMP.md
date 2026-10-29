# Forest Camp Survival Scene

Open `Assets/Scenes/ForestCamp.unity` and press Play, or use the menu `Tools > Survivors > Open Forest Camp`. This scene is first in Build Settings; the original Playground scene is kept.

- A flat central combat area of about 36 × 36 m, with low stone edges marking the original game boundary.
- Surrounding grass islands, trees, rocks, wooden fences, tents, crates, chests, and a campfire.
- Uses the current player/enemy models, keyboard and gamepad movement, gun, orbiting blades, flamethrower, material pickup, shop, and 20-wave gameplay (45 seconds per wave). You pick one weapon at the start and buy the rest in the shop; the 16 passive item rules are in [ITEM_SYSTEM.md](ITEM_SYSTEM.md).
- At the start of each run, 10 rocks, crates, barrels, or stumps are placed randomly in the combat area, avoiding the central spawn area and keeping walkable gaps. All 20 waves of a run share the layout; it refreshes when a new run starts after death or victory.
- Obstacles block the player and bullets; enemies route around them using a shared path graph. Melee and flamethrower damage cannot hit through obstacles. Material pickup still passes through obstacles so pickups don't get stuck.
- The Obstacle Count on the `ArenaObstacles` component sets the number (default 10, maximum 18). The forest scene adds this component automatically at runtime; you can also add it to the Forest Camp Arena object in advance to configure it.

The environment art comes from the **KUBIKOS - World** asset pack, imported into `Assets/KUBIKOS - World` with its original GUIDs. The pack is not included in this public repository; see the [README](README.md) for how to import it. The new scene uses its own URP materials in `Assets/Materials/ForestArena`. The pack's original demo scenes may show Unity version compatibility warnings for their old baked lighting; the new scene does not depend on that baked data.

## Maintenance

`ArtSource/World/build_forest_scene.py` can regenerate the scene from Playground and the imported assets (this overwrites manual edits to ForestCamp). `layout.json` records the set-dressing layout. The Blender layout preview `ForestCamp-preview.blend` is not included in this repository because it contains meshes from the asset pack. The Blender preview is not a game screenshot.

The Unity batch verification entry point is `ForestSceneChecks.Verify`, which checks models, materials, gameplay components, and wave rules. `ForestSceneChecks.VerifyPlay` checks movement, kills, damage, death, and restart in the current forest scene; do not pass `-quit` when running this entry point.

Verification results: on Unity 6000.3.22f1, 296 mesh/material references and the wave rules passed. In actual Play Mode, WASD movement, auto-attack kills, 10-point damage, invulnerability frames, death, and R to restart all passed. The log is saved in `Logs/ForestScenePlayVerify.log`.

## Upgrade screen

When a wave ends, an English card-style shop opens: 4 item cards in the center, live stats on the right, and Refresh and Next Wave below. Click to buy with the mouse. With the keyboard arrow keys or gamepad left stick/D-pad, move across the cards horizontally, move down to the bottom buttons, and confirm with Enter/A. L/X locks an item; Tab/View opens the item codex.

## Combat pacing

The gun is now a wooden-stock three-round burst rifle: 6 base damage per shot, 0.1 s between shots in a burst, 0.7 s between bursts, and a bullet speed of 30 m/s. Each shot re-selects a visible target in range. There is no default splash; pierce, burn, ricochet, and split are still triggered by equipment. It has muzzle flash, a short tracer, recoil, a white hit flash, and a slight 0.12 m knockback. Knockback does not go through walls and does not push stunned enemies. Basic enemies have 10 health, so a regular kill takes two shots.

Eight red rings in the arena mark spawn points, randomly placed each run away from the center and obstacle areas. Wave 1 opens with 4 enemies, spawns 1 every 1.5 s, and allows at most 14 at once; Danger Contracts adjust the rate and cap. From wave 2 on, the original scaling formula applies: opening count 12 + (wave − 1) × 2, capped at 48; batch size 2 + floor((wave − 1) / 4), capped at 6; base on-field cap 60 + (wave − 1) × 8, capped at 240. A spawn point is skipped when it is less than 8 m from the player or already occupied by an enemy. Enemies keep about 0.85 m apart; separation checks obstacles and boundaries, and stunned enemies stay in place. Enemy speed is 2.8 m/s in wave 1 and increases by 0.1 m/s per wave, reaching the 4.2 m/s cap from wave 15. A new run resets it to 2.8 m/s; the player's base speed is 5 m/s. Player contact damage uses the intersection of a body box with a 0.3 m half-width and the enemy's circular footprint with a 0.375 m radius, which stops the old large box and its corners from triggering damage early.

When any weapon kills an enemy, a +10 floating score appears above its head and disappears after about 0.9 s. The HUD shows the total SCORE, which resets on restart. Each material picked up adds 1 more point; each kill is still worth 10 points.

At the end of each wave the battlefield stays in place and a victory message appears at the top of the screen. Remaining enemies explode where they stand and each drops 1 item. These drops, together with drops still on the ground, fly to the top-right HUD. On arrival, each green crystal from an active kill adds 1 material and 1 point; each gold shard from the wave clear adds only 1 point and does not count as a kill. After the celebration and collection finish, a fixed reward of 5 materials is granted with no extra score; all rewards are settled only once. Ground drops disappear 6 seconds after spawning. Press Space or gamepad A to enter the shop; confirming early waits until drops are collected. After wave 20, the final equipment page opens and offers NEW RUN.

## Squash-and-stretch animation

The visible player and enemy models stretch on a 0.65 s cycle: about ±12% horizontally and ±18% vertically, with feet kept on the ground. Enemies use staggered animation phases. Scaling applies only to the model child nodes; the character root, CharacterController, enemy hit ranges, weapon roots, and obstacle pathfinding ranges are not scaled. The animation does not play in Edit Mode.

## Third-person camera preview

The default is a third-person follow camera. The right stick controls yaw; pitch is 28°, distance 7 m, and FOV 60°. The Settings page in the start menu can switch back to the original top-down mode; each mode keeps its own angle and distance. The camera avoids obstacles that have colliders. In third person, WASD/left stick moves relative to the camera direction. The camera focus height is 1.35 m. Obstacle avoidance uses the actual visible obstacle bounds, so the tall collision box of a low stump does not falsely pull the camera in. After an occlusion clears, the camera holds briefly and then smoothly restores its distance.

The start menu offers New Game, Settings, and Quit. Camera angle, distance, and view mode are adjusted and saved only on the Settings page; the combat HUD no longer shows camera settings buttons. The right stick still controls the view direction.
