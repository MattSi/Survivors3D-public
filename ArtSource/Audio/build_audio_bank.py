"""Rebuild the initial editable bank from the curated Kenney assets (no audio duplication)."""
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
AUDIO = ROOT / 'Assets/Audio/Kenney'

def guid(path):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        importer = ('MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
                    if path.suffix == '.cs' else 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
        meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{importer}', encoding='utf-8')
    return re.search(r'^guid: (\w+)', meta.read_text(), re.M)[1]

# cue, clip prefix, variants, gain, pitch, cooldown, maximum playback seconds, bus
SOUNDS = [
    ('Select','UI/sfx_ui_select',3,.22,1,.09,.18,'ui'),
    ('Click','UI/sfx_ui_click',2,.25,1,.08,.2,'ui'),
    ('Confirm','UI/sfx_ui_confirm',1,.4,1,.1,.7,'ui'),
    ('Back','UI/sfx_ui_back',1,.3,1,.12,.35,'ui'),
    ('Error','UI/sfx_ui_error',1,.35,1,.25,.55,'ui'),
    ('Toggle','UI/sfx_ui_toggle',2,.3,1,.12,.3,'ui'),
    ('Purchase','Shop/sfx_shop_coins',2,.45,1,.12,.6,'ui'),
    ('Refresh','Shop/Cards/sfx_shop_card_shuffle',1,.4,1,.2,.6,'ui'),
    ('Pickup','Pickups/sfx_pickup_material',3,.22,1.12,.07,.22,'ui'),
    ('PlayerHurt','Combat/Hits/sfx_hit_punch_heavy',3,.7,.85,.2,.4,'world'),
    ('EnemyHit','Combat/Hits/sfx_hit_soft_medium',3,.22,1,.09,.17,'world'),
    ('Footstep','Player/Footsteps/sfx_footstep_grass',5,.16,1,.18,.22,'world'),
    ('Dash','Weapons/Melee/sfx_melee_whoosh',2,.38,1.15,.15,.25,'world'),
    ('Rifle','Weapons/Melee/sfx_melee_metal_hit',2,.4,1.35,.045,.12,'world'),
    ('Blade','Weapons/Melee/sfx_melee_knife_slice',2,.3,1,.16,.25,'world'),
    ('Spear','Weapons/Melee/sfx_melee_whoosh',3,.35,.9,.12,.35,'world'),
    ('Crossbow','Weapons/Melee/sfx_melee_knife_draw',2,.35,1.25,.12,.22,'world'),
    ('Hammer','Weapons/Hammer/sfx_hammer_mining_hit',3,.65,.8,.18,.65,'world'),
    ('Frost','Weapons/Frost/sfx_frost_glass_medium',3,.4,1.1,.2,.55,'world'),
    ('Torch','Weapons/Melee/sfx_melee_whoosh',2,.32,.8,.15,.4,'world'),
    ('Flame','Weapons/Fire/sfx_fire_thruster',1,.3,1,.4,.65,'world'),
    ('FireballCast','Weapons/Fire/sfx_fire_thruster',1,.22,1.25,.12,.22,'world'),
    ('Explosion','Weapons/Fireball/sfx_fireball_explosion_crunch',3,.5,1,.14,.65,'world'),
    ('Boomerang','Weapons/Melee/sfx_melee_whoosh',3,.32,1.2,.15,.3,'world'),
    ('Poison','Weapons/Poison/sfx_poison_slime',2,.28,1,.3,.45,'world'),
    ('WaveStart','Jingles/jingle_pizzicato',1,.38,1,.5,1.5,'jingle'),
    ('WaveClear','Jingles/jingle_pizzicato',1,.5,1,.5,2,'jingle'),
    ('Defeat','Jingles/jingle_hit',1,.5,.8,.5,2,'jingle'),
]

def ref(path):
    assert path.is_file(), path
    assert Path(str(path) + '.meta').is_file(), f'Import this audio in Unity first: {path}'
    return '{fileID: 8300000, guid: ' + guid(path) + ', type: 3}'

bank = ROOT / 'Assets/Resources/Config/DefaultAudioBank.asset'
script = ROOT / 'Assets/Scripts/Core/GameAudioBank.cs'
lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
         '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}',
         '  m_PrefabInstance: {fileID: 0}', '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}',
         '  m_Enabled: 1', '  m_EditorHideFlags: 0', f'  m_Script: {{fileID: 11500000, guid: {guid(script)}, type: 3}}',
         '  m_Name: DefaultAudioBank', '  m_EditorClassIdentifier: ', '  sounds:']
rows = []
for cue_id, (cue, prefix, variants, volume, pitch, cooldown, duration, bus) in enumerate(SOUNDS):
    start = 5 if cue == 'WaveClear' else 1
    paths = [AUDIO / f'{prefix}_{i:02}.ogg' for i in range(start, start + variants)]
    lines += [f'  - cue: {cue_id}', '    clips:'] + [f'    - {ref(p)}' for p in paths]
    lines += [f'    volume: {volume}', f'    pitch: {pitch}', f'    pitchVariation: {0 if bus == "jingle" else .04}',
              f'    cooldown: {cooldown}', f'    maxDuration: {duration}',
              f'    interfaceSound: {int(bus == "ui")}', f'    jingle: {int(bus == "jingle")}']
    rows.append(f'| {cue} | {prefix}_*.ogg | {variants} | {cooldown} |')
for field, filename in [('menuMusic','music_loop_mishief_stroll'), ('combatMusic','music_loop_mission_plausible'), ('shopMusic','music_loop_wacky_waiting')]:
    lines.append(f'  {field}: {ref(AUDIO / "Music/Loops" / (filename + ".ogg"))}')
bank.write_text('\n'.join(lines) + '\n', encoding='utf-8')
guid(bank)
print(f'Created {bank}: {len(SOUNDS)} cues, 3 music references')
