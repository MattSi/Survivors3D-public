# Survivors3D

A 3D survivors-like action game prototype built with Unity 6. Fight off waves of enemies in a forest camp, spend the materials you collect between waves to buy and upgrade weapons, and finally take on the Forest Stone King boss.

![Forest Camp](ArtSource/World/ForestCamp-Unity.png)

## Features

- Wave system with a finite campaign and an endless mode; the wave table is editable in the Unity Editor
- 12 weapons (rifle, flamethrower, crossbow, boomerang blade, poison backpack, bait lantern, and more) with levels, evolutions, and builds such as melee and poison
- Varied enemies (chargers, shooters, bombers, splitters, stone-armored) and a multi-phase boss
- Dash, shop, item codex, Chinese and English UI, gamepad and keyboard/mouse support
- Full sound effect and music system with volume controls

## Requirements

- Unity **6000.3.22f1** (Unity 6.3) with the Universal Render Pipeline (URP)
- Main packages: Input System and AI Navigation. See `Packages/manifest.json` for the full list.

## Getting started

1. Clone the repository and open the project root in Unity Hub.
2. Open `Assets/Scenes/Playground.unity`, press Play, then click the Game view so it receives keyboard input.

The `Playground` scene does not depend on any paid assets and runs out of the box.

## Forest Camp scene (requires a third-party asset pack)

`Assets/Scenes/ForestCamp.unity` uses **KUBIKOS - World**, a paid asset pack from the Unity Asset Store. Its license does not allow redistribution, so it is **not included in this repository**. The project still opens without it, but the terrain, trees, and rocks in the Forest Camp scene will show up as missing.

If you own the asset pack:

1. Find and download KUBIKOS - World on the [Unity Asset Store](https://assetstore.unity.com/?q=KUBIKOS).
2. Import it with the Package Manager, keeping the default path `Assets/KUBIKOS - World/`.
3. The pack's asset GUIDs match the references in the scene, so the references are restored automatically after import.
4. Open the scene from `Tools > Survivors > Open Forest Camp`.

The folder is already listed in `.gitignore`, so it won't be committed by accident.

## Editor tools

The `Tools > Survivors` menu contains scene setup, verification scripts for weapons and enemies, and build test labs. For example:

- `Wave Configuration`: edit the wave table
- `Verify Audio`, `Verify Forest Layout`: check audio bindings and arena connectivity
- `Melee Build Lab`, `Poison Build Lab`: test specific builds

## Documentation

The design documents are written in Chinese.

| Document | Contents |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Runtime module responsibilities |
| [PLAYGROUND.md](PLAYGROUND.md) | Gameplay rules, waves, weapon stats |
| [WAVE_CONFIGURATION.md](WAVE_CONFIGURATION.md) | Wave table and endless mode |
| [WEAPONS.md](WEAPONS.md) | Weapon design |
| [ITEM_SYSTEM.md](ITEM_SYSTEM.md) | Item system |
| [MELEE_BUILD.md](MELEE_BUILD.md) / [POISON_BUILD.md](POISON_BUILD.md) | Melee build / poison build |
| [FOREST_CAMP.md](FOREST_CAMP.md) / [FOREST_LAYOUT.md](FOREST_LAYOUT.md) | Forest Camp scene and layout |
| [AUDIO.md](AUDIO.md) | Sound events and music |

## Project structure

```
Assets/
├── Scripts/      # Game code
├── Editor/       # Editor tools and verification scripts
├── Resources/    # Character models, config (waves, audio), fonts
├── Audio/Kenney/ # Sound effects and music
├── Materials/
└── Scenes/
ArtSource/        # Blender source files, concept art, and generator scripts
```

## License

- **Code**: [MIT](LICENSE)
- **Audio**: from [Kenney](https://kenney.nl), CC0. Original licenses are in `Assets/Audio/Kenney/Licenses/`.
- **Font**: Noto Sans CJK SC, SIL Open Font License 1.1. See `Assets/Resources/Fonts/OFL.txt`.
- **KUBIKOS - World**: a Unity Asset Store asset, not included in this repository. Follow the Asset Store license terms when using it.
