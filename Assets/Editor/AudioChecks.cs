using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AudioChecks
{
    private static SurvivorPlayground game;
    private static GameAudio audio;
    private static float master,effects,music;
    private static double next;
    private static int stage;
    private const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static object Get(string name)=>GameTestAccess.Get(game,name);
    private static void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
    private static void Check(bool ok,string message) { if(!ok) throw new Exception("AUDIO_CHECK: "+message); }
    private static void Fresh(int weapon=0)
    {
        Time.timeScale=1; game.NewGame(); game.BeginRun(weapon); game.enabled=false; audio.enabled=true;
        // Headless editor has no focused window; explicitly simulate foreground gameplay.
        typeof(GameAudio).GetMethod("OnApplicationFocus",F).Invoke(audio,new object[] {true});
        ((PlayerController)Get("player")).enabled=false;
        Call("ClearCombatObjects");
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"))).Clear();
    }
    private static Transform Target()
    {
        Call("SpawnEnemy"); var enemies=(List<Transform>)Get("enemies");
        var enemy=enemies[enemies.Count-1]; enemy.position=new Vector3(0,.375f,2);
        ((Dictionary<Transform,int>)Get("enemyHealth"))[enemy]=10000;
        return enemy;
    }
    private static void Expect(AudioCue cue,Action action)
    {
        audio.StopWorld(); int before=audio.CountPlayed(cue);
        action(); Check(audio.CountPlayed(cue)>before,"Event emits "+cue);
    }
    [MenuItem("Tools/Survivors/Verify Audio")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("AudioChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] private static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("AudioChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    private static void Verify()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); audio=game.Audio;
        Check(audio!=null && audio.Bank!=null,"Scene loads audio service and serialized bank");
        master=audio.MasterVolume; effects=audio.EffectsVolume; music=audio.MusicVolume;
        audio.SetVolume(0,.8f); audio.SetVolume(1,.75f);
        Check(audio.VoiceCount==21 && audio.GetComponentsInChildren<AudioSource>().Length==22,"Fixed voice budget including music");
        var cues=new HashSet<AudioCue>();
        foreach(var sound in audio.Bank.sounds)
        {
            Check(cues.Add(sound.cue),"No duplicate cue mappings");
            Check(sound.clips!=null && sound.clips.Length>0,"Clips assigned for "+sound.cue);
            foreach(var clip in sound.clips) Check(clip!=null && clip.length>0,"Imported audio clip is valid");
        }
        Check(cues.Count==Enum.GetValues(typeof(AudioCue)).Length,"Every cue configured");
        var deps=AssetDatabase.GetDependencies("Assets/Resources/Config/DefaultAudioBank.asset",true);
        Check(Array.Exists(deps,p=>p.EndsWith("sfx_melee_metal_hit_01.ogg")),"Bank retains clips outside Resources in builds");
        var rng=UnityEngine.Random.state;
        Fresh(); rng=UnityEngine.Random.state;
        Check(audio.TryPlay(AudioCue.EnemyHit),"Foreground hit starts a voice");
        Check(JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"Audio does not change gameplay random state");
        int before=audio.PlayedCount;
        for(int i=0;i<1000;i++) audio.TryPlay(AudioCue.EnemyHit);
        Check(audio.PlayedCount==before,"Crowd hit audio is rate limited");
        audio.StopWorld();
        Time.timeScale=0;
        Check(!audio.TryPlay(AudioCue.Rifle),"Paused combat cannot emit shots");
        Check(audio.TryPlay(AudioCue.Back),"Pause UI remains audible");
        Time.timeScale=1;
        audio.SetVolume(0,0); Check(!audio.TryPlay(AudioCue.Footstep),"Master mute prevents new voices"); audio.SetVolume(0,.8f);

        Fresh(); int noTarget=audio.PlayedCount; Call("Shoot"); Check(audio.PlayedCount==noTarget,"No target means no rifle sound");
        var enemy=Target(); Expect(AudioCue.Rifle,()=>Call("Shoot"));
        Expect(AudioCue.EnemyHit,()=>Call("DamageEnemy",0,1,Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic),"Gun")));
        Expect(AudioCue.PlayerHurt,()=>game.Health.TakeDamage(10));
        int balance=game.Run.Equipment.Materials;
        int pickupBefore=audio.PlayedCount; Call("CollectMaterial",1,true);
        Check(audio.PlayedCount>pickupBefore && audio.LastCue==AudioCue.Pickup && game.Run.Equipment.Materials==balance+1,"Pickup sound follows credit");
        int empty=audio.PlayedCount; Call("CollectMaterial",0,false); Check(audio.PlayedCount==empty,"Empty cleanup is silent");

        Fresh(3); Target(); Expect(AudioCue.Spear,()=> {Call("UpdateNewWeapons",.01f); Call("UpdateNewWeapons",.13f);});
        Fresh(4); Expect(AudioCue.Crossbow,()=>Call("FireNewProjectile",4,Vector3.zero,Vector3.forward));
        Fresh(5); Expect(AudioCue.Hammer,()=>Call("SlamHammer",Vector3.zero));
        Fresh(6); Expect(AudioCue.Frost,()=>Call("UpdateNewWeapons",.36f));
        Fresh(7); Target(); Expect(AudioCue.Torch,()=>Call("UpdateNewWeapons",.15f));
        Fresh(8); Expect(AudioCue.FireballCast,()=>Call("FireFireball",Vector3.zero,Vector3.forward));
        var bullets=(System.Collections.IList)Get("bullets"); Expect(AudioCue.Explosion,()=>Call("ExplodeFireball",bullets[0]));
        Fresh(9); Target(); Expect(AudioCue.Boomerang,()=>Call("UpdateNewWeapons",.01f));
        Fresh(10); Expect(AudioCue.Poison,()=>Call("UpdateNewWeapons",.81f));
        Fresh(2); Target(); Expect(AudioCue.Flame,()=>Call("UpdateFlamethrower",.01f));
        Fresh(1); Target(); Expect(AudioCue.Blade,()=> {Call("UpdateMelee",.5f);});

        Fresh(); var player=(PlayerController)Get("player"); player.enabled=true;
        typeof(PlayerController).GetMethod("OnApplicationFocus",F).Invoke(player,new object[] {true});
        var move=typeof(PlayerController).GetMethod("TickMovement",F);
        Expect(AudioCue.Footstep,()=>move.Invoke(player,new object[] {Vector3.forward,false,.4f}));
        Expect(AudioCue.Dash,()=>move.Invoke(player,new object[] {Vector3.forward,true,.02f}));
        player.enabled=false;
        Fresh();
        game.Run.Session.Configure(new WaveRules()); game.Run.Session.Reset(); game.Run.Session.Advance(double.MaxValue);
        game.Run.Shop.Open(); GameTestAccess.Set(game,"phaseChangedFrame",-1);
        Expect(AudioCue.Error,()=>Call("BuyEquipment",0));
        game.Run.Equipment.Collect(100);
        Expect(AudioCue.Purchase,()=>Call("BuyEquipment",0));
        Check(game.Run.Shop.Sold(0),"Purchase sound accompanies actual transaction");
        Expect(AudioCue.Toggle,()=>Call("ToggleLock",1));
        Check(game.Run.Shop.Locked(1),"Lock sound accompanies changed lock state");
        int lockedCount=audio.CountPlayed(AudioCue.Toggle); Call("ToggleLock",0);
        Check(audio.CountPlayed(AudioCue.Toggle)==lockedCount,"Sold card cannot emit lock feedback");
        Expect(AudioCue.Refresh,()=>Call("RefreshShop"));

        Fresh(); Expect(AudioCue.WaveClear,()=>Call("FinishWave"));
        Fresh(); Expect(AudioCue.Defeat,()=>game.Health.TakeDamage(100));
        Fresh(); Expect(AudioCue.WaveStart,()=> { game.NewGame(); game.BeginRun(0); });
        game.enabled=true; ArchitectureChecks.Run(game);
        Fresh(); audio.TryPlay(AudioCue.Rifle); Check(audio.ActiveWorldVoices>0,"World voice starts");
        Time.timeScale=0;
    }
    private static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0) Verify();
            else if(stage==1)
            {
                Check(audio.WorldPaused && audio.ActiveWorldVoices>0,"Paused voice retains its remaining lifetime");
                Time.timeScale=1; audio.StopWorld(); Check(audio.ActiveWorldVoices==0,"Cleanup releases all world voices");
                audio.SetMusic(0);
            }
            else if(stage==2)
            {
                Check(audio.CurrentMusic==audio.Bank.menuMusic,"Music transitions to menu track");
                audio.SetVolume(2,.31f); audio.SavePreferences();
                Check(Mathf.Approximately(PlayerPrefs.GetFloat("Survivors.Audio.Music"),.31f),"Music volume persists");
                game.enabled=true; game.enabled=false;
                Check(!audio.enabled && audio.ActiveWorldVoices==0,"Host disable stops audio");
                game.enabled=true;
                Check(audio.enabled && UnityEngine.Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length==1,"Re-enable retains one service");
                Call("OpenSettings");
                if(!Application.isBatchMode)
                    EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Show();
            }
            else if(stage==3)
            {
                if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)
                    ScreenCapture.CaptureScreenshot("Logs/Audio-Settings.png");
            }
            else
            {
                Debug.Log("AUDIO_CHECKS_PASSED: 28 cue mappings, weapon/movement/shop/hit/pickup/wave bindings, RNG isolation, rate limiting, pause, cleanup, music, volume persistence and architecture.");
                Finish(0); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+1.2;
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    private static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("AudioChecks",false); Time.timeScale=1;
        if(audio!=null) {audio.SetVolume(0,master); audio.SetVolume(1,effects); audio.SetVolume(2,music); audio.SavePreferences();}
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
