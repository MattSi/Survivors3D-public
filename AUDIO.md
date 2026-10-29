# Game Audio

ForestCamp and Playground automatically create a scene-level `GameAudio` through `SurvivorPlayground`, so no manual scene changes are needed.
`Assets/Resources/Config/DefaultAudioBank.asset` is the audio configuration, editable in the Inspector. It references assets in `Assets/Audio/Kenney` directly, so referenced audio is kept in builds.

## Bound events

| Event | When it plays |
| --- | --- |
| Menu select, click, confirm, back | Moving between options with keyboard/gamepad, mouse clicks, confirming menus, opening/closing the codex |
| Shop purchase, refresh, lock, failure | The matching sound after a successful charge/trade; failed requests play an error cue; cancelling a replacement plays no purchase sound |
| Material pickup | When materials are actually added or a floating score is settled; zero-amount cleanup stays silent |
| Grass footsteps, dash | Every 1.65 m of actual player movement; when a dash starts successfully; pushing against a wall in place makes no footsteps |
| Three-round burst rifle | Each bullet actually fired; nothing plays without a target |
| Orbiting twin blades | When they actually hit an enemy |
| Spear, crossbow, boomerang blade | At the start of a throw/shot; a full crossbow volley plays only once |
| Hammer, frost | When the hammer lands and when ice crystals land |
| Torch, flamethrower | When the backswing ends and the swing begins, and when a flame burst starts; damage over time does not replay it |
| Fireball | Separate sounds for launch and explosion |
| Poison backpack | On a successful spray; poison ticks do not play it |
| Bomber enemy | When its bomb actually explodes |
| Player hurt, regular hit | On actual damage taken; when bullets/melee deal damage; burn and poison do not replay the hit sound |
| Wave start, complete, fail | One jingle per matching phase transition |

## Music and volume

Current defaults: main menu/starter weapon choice uses `Mishief Stroll`, combat uses `Mission Plausible`, shop/settlement uses `Wacky Waiting`. Music fades out on defeat.
Switching tracks fades out first, then fades in. While paused or with the codex open, combat sounds and jingles pause and music drops to 30% of its current volume; UI sounds still play. When the window loses focus, combat sounds stop and music is muted.

The right side of Settings adjusts master, SFX, and music volume; defaults are 80%, 75%, and 22%. Navigate to a volume option with gamepad/keyboard up/down and adjust with left/right; the mouse can drag the sliders directly. Values are saved in PlayerPrefs under `Survivors.Audio.*`.

## Previewing and replacing sounds

In the Unity Project window, select `Assets/Resources/Config/DefaultAudioBank.asset`, expand Sounds, and edit Clips, Volume, Pitch, Cooldown, and Max Duration per Cue. Indices and purposes match the `AudioCue` enum. One event can have several variants; playback avoids repeating the same variant back to back.

The rifle currently uses a short metal impact as temporary mechanical feedback, and the crossbow uses a blade-draw sound as a placeholder; the flamethrower comes from a thruster sound and frost from a glass sound. They are wired to their events, but the final sounds and the three candidate music tracks still need tuning through in-game listening. The original audio files are not modified; Max Duration only limits runtime playback length, with a short fade-out at the end.

## Limits and lifecycle

- 16 combat voices, 4 UI voices, 1 jingle voice, and 1 music voice, all using reusable AudioSources.
- Each event has its own cooldown: at least 90 ms between regular hits and 70 ms between material pickups, so crowds and batch settlement don't stack sounds.
- Player-hurt and UI cues can replace older sounds in their pool; regular combat sounds are skipped when the pool is full.
- A separate `System.Random` picks sounds and pitch, so the combat random sequence is unaffected.
- Changing waves, dying, and restarting clear combat voices; disabling the host stops all sound. Everything is cleaned up when the scene is destroyed, with no persistent cross-scene copy.
- Sounds currently use 2D mixing, so switching between top-down and third-person views does not change volume.

## Verification

Unity menu: `Tools > Survivors > Verify Audio` (run outside Play Mode). It checks audio references, build dependencies, event bindings, rate limiting, random-number isolation, pause/cleanup, music transitions, volume persistence, and the existing architecture checks.
Success marker: `AUDIO_CHECKS_PASSED`. Batch log: `Logs/AudioChecks.log`.

`ArtSource/Audio/build_audio_bank.py` rebuilds the initial configuration and overwrites the configuration asset above. For everyday tuning, edit it in the Inspector instead of running this script.
