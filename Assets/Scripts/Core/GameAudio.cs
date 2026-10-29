using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned, bounded audio voices. Audio randomness never changes gameplay RNG.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class GameAudio : MonoBehaviour
{
    public static GameAudio Current { get; private set; }
    public GameAudioBank Bank { get; private set; }
    public float MasterVolume { get; private set; }
    public float EffectsVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public int PlayedCount { get; private set; }
    public AudioCue LastCue { get; private set; }
    private readonly Dictionary<AudioCue,int> playCounts=new Dictionary<AudioCue,int>();
    public int CountPlayed(AudioCue cue)=>playCounts.TryGetValue(cue,out int count)?count:0;
    public int VoiceCount=>voices.Count;
    public int ActiveWorldVoices { get { int count=0; foreach(var voice in voices) if(voice.active && !voice.ui) count++; return count; } }
    public AudioClip CurrentMusic=>music!=null?music.clip:null;
    public bool WorldPaused { get; private set; }
    private sealed class Voice
    {
        public AudioSource source;
        public bool ui, jingle, active;
        public float remaining, gain;
    }
    private readonly List<Voice> voices=new List<Voice>();
    private readonly Dictionary<AudioCue,GameAudioBank.Sound> sounds=new Dictionary<AudioCue,GameAudioBank.Sound>();
    private readonly Dictionary<AudioCue,float> ready=new Dictionary<AudioCue,float>();
    private readonly Dictionary<AudioCue,int> lastVariant=new Dictionary<AudioCue,int>();
    private readonly System.Random random=new System.Random();
    private AudioSource music;
    private AudioClip desiredMusic;
    private float musicFade;
    private bool focused=true;

    private void Awake()
    {
        if(Current!=null && Current!=this) { enabled=false; return; }
        Current=this;
        MasterVolume=ReadVolume("Master",.8f); EffectsVolume=ReadVolume("Effects",.75f); MusicVolume=ReadVolume("Music",.22f);
        Bank=Resources.Load<GameAudioBank>("Config/DefaultAudioBank");
        if(Bank!=null && Bank.sounds!=null)
        { foreach(var sound in Bank.sounds) if(sound!=null) sounds[sound.cue]=sound; }
        else Debug.LogWarning("Audio bank is missing; game audio is disabled.");
        for(int i=0;i<21;i++)
            voices.Add(new Voice {source=CreateSource("Audio Voice "+i),ui=i>=16 && i<20,jingle=i==20});
        music=CreateSource("Music"); music.loop=true;
    }
    private static float ReadVolume(string key,float fallback)
    {
        float value=PlayerPrefs.GetFloat("Survivors.Audio."+key,fallback);
        return float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp01(value);
    }
    private AudioSource CreateSource(string label)
    {
        var child=new GameObject(label); child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>();
        source.playOnAwake=false; source.spatialBlend=0; source.dopplerLevel=0;
        source.ignoreListenerPause=true;
        return source;
    }
    public static void Play(AudioCue cue) { if(Current!=null) Current.TryPlay(cue); }
    public bool TryPlay(AudioCue cue)
    {
        if(!isActiveAndEnabled || !focused || Bank==null || !sounds.TryGetValue(cue,out var sound)
            || sound.clips==null || sound.clips.Length==0 || MasterVolume*EffectsVolume<=0) return false;
        if(!sound.interfaceSound && Time.timeScale<=0) return false;
        if(ready.TryGetValue(cue,out float at) && Time.unscaledTime<at) return false;
        Voice voice=null;
        foreach(var candidate in voices)
            if(candidate.ui==sound.interfaceSound && candidate.jingle==sound.jingle && !candidate.active) {voice=candidate; break;}
        // Important feedback always has a reserved pool and can replace its oldest/shortest remaining cue.
        if(voice==null && (sound.interfaceSound || sound.jingle || cue==AudioCue.PlayerHurt))
            foreach(var candidate in voices)
                if(candidate.ui==sound.interfaceSound && candidate.jingle==sound.jingle && (voice==null || candidate.remaining<voice.remaining)) voice=candidate;
        if(voice==null) return false;
        int index=random.Next(sound.clips.Length);
        if(sound.clips.Length>1 && lastVariant.TryGetValue(cue,out int previous) && index==previous) index=(index+1)%sound.clips.Length;
        var clip=sound.clips[index]; if(clip==null) return false;
        lastVariant[cue]=index; ready[cue]=Time.unscaledTime+sound.cooldown;
        voice.source.Stop(); voice.source.clip=clip;
        voice.source.pitch=Mathf.Clamp(sound.pitch+(float)(random.NextDouble()*2-1)*sound.pitchVariation,.2f,3);
        voice.gain=sound.volume; voice.source.volume=voice.gain*MasterVolume*EffectsVolume;
        voice.remaining=clip.length/voice.source.pitch;
        if(sound.maxDuration>0) voice.remaining=Mathf.Min(voice.remaining,sound.maxDuration);
        voice.active=true; voice.source.Play(); PlayedCount++; LastCue=cue; playCounts[cue]=CountPlayed(cue)+1;
        return true;
    }
    public void SetVolume(int channel,float value)
    {
        if(float.IsNaN(value)||float.IsInfinity(value)) return;
        value=Mathf.Clamp01(value);
        if(channel==0) MasterVolume=value; else if(channel==1) EffectsVolume=value; else MusicVolume=value;
        PlayerPrefs.SetFloat("Survivors.Audio."+(channel==0?"Master":channel==1?"Effects":"Music"),value);
        foreach(var voice in voices) voice.source.volume=voice.gain*MasterVolume*EffectsVolume;
        if(music!=null) music.volume=MasterVolume*MusicVolume*musicFade;
    }
    public void SavePreferences()=>PlayerPrefs.Save();
    public void SetMusic(int mode)
    {
        if(Bank==null) return;
        desiredMusic=mode==1?Bank.combatMusic:mode==2?Bank.shopMusic:mode==0?Bank.menuMusic:null;
    }
    public void StopWorld()
    {
        foreach(var voice in voices) if(!voice.ui) {voice.source.Stop(); voice.active=false;}
        foreach(var sound in sounds.Values) if(!sound.interfaceSound) ready.Remove(sound.cue);
    }
    private void Update()
    {
        bool paused=Time.timeScale<=0 || !focused;
        if(paused!=WorldPaused)
        {
            WorldPaused=paused;
            foreach(var voice in voices) if(voice.active && !voice.ui)
            { if(paused) voice.source.Pause(); else voice.source.UnPause(); }
        }
        foreach(var voice in voices)
        {
            if(!voice.active || (!voice.ui && paused)) continue;
            voice.remaining-=Time.unscaledDeltaTime;
            // Fade short placeholders/tails rather than cutting the waveform abruptly.
            voice.source.volume=voice.gain*MasterVolume*EffectsVolume*Mathf.Clamp01(voice.remaining/.025f);
            if(voice.remaining<=0) {voice.source.Stop(); voice.active=false;}
        }
        if(music==null) return;
        if(music.clip!=desiredMusic)
        {
            musicFade=Mathf.MoveTowards(musicFade,0,Time.unscaledDeltaTime*2);
            if(musicFade<=0) {music.Stop(); music.clip=desiredMusic; if(desiredMusic!=null) music.Play();}
        }
        else musicFade=Mathf.MoveTowards(musicFade,paused?.3f:1,Time.unscaledDeltaTime);
        music.volume=focused?MasterVolume*MusicVolume*musicFade:0;
    }
    private void OnApplicationFocus(bool value) {focused=value; if(!value) StopWorld();}
    private void OnDisable()
    {
        foreach(var voice in voices) {voice.source.Stop(); voice.active=false;}
        if(music!=null) music.Stop();
    }
    private void OnEnable() { if(music!=null && music.clip!=null) music.Play(); }
    private void OnDestroy() { if(Current==this) {SavePreferences(); Current=null;} }
}
