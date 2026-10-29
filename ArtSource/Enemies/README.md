# Block Enemy Models

`BlockEnemies.blend` contains three enemies in the same style plus a preview studio; `Enemies-preview.png` is a top-down render. The source generator script is `create_enemies.py`.

- `EnemyImp`: a small enemy with horns and wide ears, 572 triangles.
- `EnemyHood`: a small hooded enemy, 484 triangles.
- `EnemyRock`: a broad-shouldered small rock enemy, 484 triangles.

The game FBX files are in `Assets/Resources/Characters`. Each model is merged into a single mesh, keeping its material slots and `FaceAnchor`, with no lights or ground exported. Unity uses shared URP materials. Models face their chase target, feet are aligned to the ground, and they are about 1.02 m tall.

The three regular looks are picked at random and share the current enemy health, speed, and drop rules. Flamethrower charring covers all of a model's material slots, and the charred husk effect is kept after death.

New `EnemyCharger` (charging imp): a brick-red block design, 748 triangles. The source file is `BlockCharger.blend` and the generator script is `create_charger.py`. From wave 3, it replaces a few regular enemies, with direction lock, a charge-up warning, a straight-line charge, and a recovery pause. Parameters and the verification entry point are in [CHARGER.md](CHARGER.md); the in-game preview is `Charger-InGame.png`.

New `EnemyStoneArmor`: broad-shouldered gray stone armor with moss-green blocks, 1,056 triangles. The source file is `BlockStoneArmor.blend` and the generator script is `create_stone_armor.py`. It appears in small numbers from wave 5, with its own base health of 120, 55% movement speed, and a larger solid collider. Parameters and the verification entry point are in [STONE_ARMOR.md](STONE_ARMOR.md).

New `EnemyBomber` (hooded bomber): a dark purple hood, a belt pouch, a separate throwing arm, and a bomb, 768 triangles in total. The source file is `BlockBomber.blend` and the generator script is `create_bomber.py`. It appears in small numbers from wave 8, keeps its distance, and lobs delayed-fuse bombs at a telegraphed landing spot. Parameters and the verification entry point are in [BOMBER.md](BOMBER.md).

The regular models have passed Blender rendering and Unity assembly compile checks; Play Mode checks for the special enemies are in their own notes.

# Splitter

New `EnemySplitter.fbx` (mother) and `EnemySplitling.fbx` (offspring). Source files, behavior, and test entry points are in [SPLITTER.md](SPLITTER.md).

# Bone-Needle Shooter

New `EnemyShooter.fbx`: a gray-green block body, a bone-white firing arm, and three bone needles on its back, 782 triangles in total.
The source file is `BlockShooter.blend` and the generator script is `create_shooter.py`; the model preview is `Shooter-preview.png`.
It appears in small numbers from wave 6 and, after charging up, fires straight bone needles that can be dodged and are blocked by cover. Parameters and test entry points are in [SHOOTER.md](SHOOTER.md).
