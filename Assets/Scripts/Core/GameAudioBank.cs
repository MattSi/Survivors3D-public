using System;
using UnityEngine;

public enum AudioCue
{
    Select, Click, Confirm, Back, Error, Toggle, Purchase, Refresh, Pickup,
    PlayerHurt, EnemyHit, Footstep, Dash, Rifle, Blade, Spear, Crossbow,
    Hammer, Frost, Torch, Flame, FireballCast, Explosion, Boomerang, Poison,
    WaveStart, WaveClear, Defeat
}

[CreateAssetMenu(menuName="Survivors/Audio Bank")]
public sealed class GameAudioBank : ScriptableObject
{
    [Serializable] public sealed class Sound
    {
        public AudioCue cue;
        public AudioClip[] clips;
        [Range(0,1)] public float volume=.4f;
        [Range(.2f,3)] public float pitch=1;
        [Range(0,.3f)] public float pitchVariation=.04f;
        [Min(0)] public float cooldown=.08f;
        [Min(0)] public float maxDuration;
        public bool interfaceSound;
        public bool jingle;
    }
    public Sound[] sounds;
    public AudioClip menuMusic, combatMusic, shopMusic;
}
