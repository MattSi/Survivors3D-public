# Block Hero

The character now has a free short dash: Space / LB / L1, 2.5 m, 0.15 s, 6-second cooldown, with brief invulnerability and a blue-white trail.
Controls, collision rules, and the verification entry point are in [DASH.md](DASH.md).

An original blocky little warrior made in Blender 5.2.1, with low-poly proportions inspired by a big head, short limbs, and simple facial features: a red tunic, short dark hair, gold trim, and a belt buckle.

- `BlockHero.blend`: the editable Blender source file, including the model and a preview studio.
- `BlockHero-preview.png`: an actual Blender render preview.
- `create_block_hero.py`: the script that generates the model, exports the FBX, and renders the preview.
- `../../Assets/Resources/Characters/BlockHero.fbx`: the Unity game model (no lights, ground, or camera).

The model has 35 mesh parts, 1540 triangles, flat-color materials, and a `MouthAnchor` mouth anchor, with no bones or animation. Each part can be adjusted separately in Blender.

Top-down adjustments: the whole head tilts up 15°, the eyes and eyebrows are about 25% larger, and the bangs are shorter. The mouth anchor transforms with the head. The new preview uses a top-down camera close to the game's; the default game camera is 55°.

Unity's `PlayerAvatar` automatically loads the FBX, converts it to URP materials, normalizes its height to 1.4 m, aligns its feet to the ground, and hides the old red cube. The original Player's movement collider and weapon root are kept. Only the character's appearance turns with movement; the orbiting twin blades are unaffected by turning. The hurt flash applies to the whole body, and fire breath uses the model's mouth anchor.

`PlayerAvatar` is already attached in the scene, so the model shows in both the editor and at runtime; it appears automatically after the FBX is first imported. The camera, lights, weapons, and arena are not part of the model asset.

Regenerate:

```powershell
& '<BlenderPath>/blender.exe' --background --threads 4 --python '<ProjectPath>/ArtSource/Player/create_block_hero.py'
```

Blender export, an actual render check, and the Unity assembly compile check are done. Unity Play Mode in-game verification has not been done.
