# Splitter and Splitling

The design follows the approved concept: a swollen blue-cyan block body, yellow-green cracks, square eyes, and short feet. The offspring are about half the mother's height, with fewer cracks.

- Mother model: `Assets/Resources/Characters/EnemySplitter.fbx`, 374 triangles, 1.15 tall in the scene.
- Offspring model: `Assets/Resources/Characters/EnemySplitling.fbx`, 362 triangles, 0.58 tall in the scene.
- Blender source file: `ArtSource/Enemies/BlockSplitter.blend`.
- Generator script: `ArtSource/Enemies/create_splitter.py`; run it with Blender in background mode to rebuild both FBX files and the model preview.
- Model preview: `Splitter-preview.png`; actual Unity Forest Camp screenshot: `Splitter-InGame.png`.

## Behavior

The mother chases at 80% of regular enemy speed, with its own base health of 45 multiplied by the wave health multiplier. On death it flashes a crack-colored ring and four fragments, and the next combat update spawns two offspring. Offspring have their own base health of 14 multiplied by the wave health multiplier, and 130% movement speed; offspring don't split again. All health values are rounded up, with a minimum of 1.

Spawned offspring have a 0.4-second unfolding animation during which they don't move or deal contact damage but can be attacked by the player. Spawn positions are chosen near where the mother died, checking obstacles, boundaries, the player, and other enemies; if no space is free, they wait. Frost slow, hammer stun, and weapon hit ranges all use the matching body size.

Each mother reserves one split slot in advance, so after it dies its own slot plus the reserved slot are enough for two offspring. Active and pending offspring both count against the total population budget, which keeps endless mode from getting out of control. Mothers and offspring each count as one kill with normal material drops. Field clearing, settlement, defeat, and restart don't trigger splitting and cancel any pending spawns.

## Configuration and testing

Open **Tools → Survivors → Wave Configuration**.

- Normal mix: from wave 11, one can appear every 11th portal spawn, with at most 2 mothers; from wave 16, at most 3. Chargers, stone-armored enemies, and bombers have higher priority.
- Quick test: choose `SplitterOnly`, set the start wave to 1, save, and start a new run. Kill a mother to watch it split; to see more mothers, raise the Splitter stage's alive cap.
- Keep **Simultaneous attacks** (`同时攻击`) at 0; this column doesn't control splitting on death. Set the total on-field cap to at least 2.
- Older custom configurations need a `Splitter` stage added by hand; the default asset already has one, and existing parameters were not reset.

After leaving Play Mode, run **Tools → Survivors → Verify Splitter** to test the model, stats, slow/stun, split count, area kills, non-recursion, drops, obstacles/boundaries, population reservation, and cleanup, plus regressions for the existing special enemies and wave configuration. The test uses a temporary configuration and doesn't modify the saved game configuration.
