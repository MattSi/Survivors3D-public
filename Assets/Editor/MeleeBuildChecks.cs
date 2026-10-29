using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MeleeBuildChecks
{
    static SurvivorPlayground game;
    static WaveConfiguration profile;
    static object original;
    static double ready;
    static int stage;
    static bool originalAsync;
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static Dictionary<Transform,int> HP=>(Dictionary<Transform,int>)Get("enemyHealth");
    static Dictionary<Transform,float> Stuns=>(Dictionary<Transform,float>)Get("stunned");
    static void Check(bool ok,string message) {if(!ok) throw new Exception("MELEE_BUILD_CHECK: "+message);}
    [MenuItem("Tools/Survivors/Verify Melee Build")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("MeleeBuildChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MeleeBuildChecks",false)) return;
            ready=EditorApplication.timeSinceStartup+2; stage=0; EditorApplication.update+=Tick;
        };
    }
    static void Fresh(int group=2)
    {
        UnityEngine.Random.InitState(31415); game.NewGame(); game.BeginRun(1); game.enabled=false;
        ((PlayerController)Get("player")).enabled=false;
        Call("ClearCombatObjects");
        var obstacles=Get("obstacles");
        ((List<Bounds>)obstacles.GetType().GetField("blocks",F).GetValue(obstacles)).Clear();
        MeleeBuildLab.Equip(game.Run.Equipment,group); Call("ApplyEquipment");
    }
    static Transform Target(float x,float z,int health=100)
    {Call("SpawnEnemy"); var target=Enemies[Enemies.Count-1]; target.position=new Vector3(x,.375f,z); HP[target]=health; return target;}
    static void Blade()=>Call("UpdateMelee",1f);
    static void Verify()
    {
        for(int group=0;group<4;group++)
        {
            var eq=new RunEquipment(); eq.ChooseStarter(1); int price=MeleeBuildLab.Equip(eq,group);
            Check(price==new[]{15,43,63,0}[group] && eq.Materials==100-price,"Preset uses actual prices and preserves unspent budget");
        }
        var baseline=Resources.Load<WaveConfiguration>("Config/DefaultWaveConfiguration");
        string before=JsonUtility.ToJson(baseline);
        for(int scenario=0;scenario<4;scenario++)
        {
            var config=MeleeBuildLab.CreateProfile(scenario);
            Check(config.Rules.StartWave==(scenario==3?config.Rules.SafeFinalWave:8),"Scenario starting wave");
            config.Rules.Waves[0].Duration=1;
            UnityEngine.Object.Destroy(config);
        }
        Check(JsonUtility.ToJson(baseline)==before,"Experiments deep-copy and do not change campaign assets");

        Fresh(); var near=Target(2.45f,0); Blade();
        Check(HP[near]<100 && game.Metrics.For(CombatMetrics.Source.Blades).Hits==1,"Blade orbit hits at safe edge distance");
        Call("MoveEnemies",.001f); Check(game.Health.Current==100,"Edge target does not cause contact damage");
        near.position=new Vector3(.2f,.375f,0); Call("MoveEnemies",.001f);
        Check(game.Health.Current==90 && game.Metrics.DamageTaken==10,"Crossing into contact causes real damage");

        Fresh(); var outside=Target(3.2f,0); Blade(); Check(HP[outside]==100,"Outside visible blade reach plus enemy footprint misses");
        Fresh(); near=Target(2.45f,0);
        var blocks=(List<Bounds>)Get("obstacles").GetType().GetField("blocks",F).GetValue(Get("obstacles"));
        blocks.Add(new Bounds(new Vector3(1.2f,.5f,0),new Vector3(.2f,2,2)));
        Blade(); Check(HP[near]==100,"Blade cannot reach through cover");

        Fresh(); near=Target(2.2f,0,120); Call("SlamHammer",new Vector3(2.2f,.035f,0));
        Check(HP[near]==82 && Stuns.ContainsKey(near),"Hammer leaves a durable target stunned after 38 damage");
        Blade(); Check(game.Metrics.BladeHitsDuringStun==1 && game.Metrics.BladeDamageDuringStun>0,"Blades exploit the hammer window");
        var hammer=game.Metrics.For(CombatMetrics.Source.Hammer);
        Check(hammer.Damage==38 && hammer.Hits==1 && hammer.Kills==0,"Hammer and blade attribution are separate");
        Stuns[near]=Time.time+.15f; Call("UpdateHammerEffects",.01f);
        var stars=((Dictionary<Transform,Transform>)Get("stunStars"))[near];
        Check(Mathf.Abs(stars.localScale.x-.5f)<.01f,"Stars shrink near the end of control");
        Stuns[near]=Time.time-1; Call("UpdateHammerEffects",.01f);
        Check(!Stuns.ContainsKey(near),"Expired stun and stars clear");

        Fresh(); near=Target(2.45f,0,1); Blade();
        Check(game.Metrics.For(CombatMetrics.Source.Blades).Damage==1 && game.Metrics.For(CombatMetrics.Source.Blades).Kills==1,"Overkill is excluded from metrics");
        Target(5,0); int kills=game.Metrics.For(CombatMetrics.Source.Blades).Kills; Call("FinishWave");
        Check(game.Metrics.For(CombatMetrics.Source.Blades).Kills==kills,"Settlement despawn is not a weapon kill");

        Fresh(1); game.Health.TakeDamage(10); near=Target(2,0,100000);
        object melee=Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic|BindingFlags.Public),"Melee");
        for(int i=0;i<200 && game.Health.Current==90;i++) Call("OnPrimaryHit",near,melee);
        Check(game.Health.Current==91 && game.Metrics.LifeStolen==1,"Real melee procs restore and record one HP");
        for(int i=0;i<200;i++) Call("OnPrimaryHit",near,melee);
        Check(game.Health.Current==91,"Shared cooldown prevents same-frame multi-target healing bursts");
        Call("HealFromItem",50,false); Check(game.Health.Current==100 && game.Metrics.LifeStolen==10,"Healing excludes overheal");
        Call("HealFromItem",1,false); Check(game.Metrics.LifeStolen==10,"Full-health procs record no healing");
        game.Health.ResetHealth(); game.Health.TakeDamage(1000);
        Check(game.Metrics.DamageTaken==110,"Lethal damage event reports actual health loss");
        Call("HealFromItem",100,false); Check(game.Health.Current==0 && game.Metrics.LifeStolen==10,"Healing cannot revive");
        game.NewGame(); Check(game.Metrics.DamageTaken==0 && game.Metrics.LifeStolen==0,"Restart clears metrics");
        game.BeginRun(1); game.Metrics.Hurt(1); Call("StartWave"); Check(game.Metrics.DamageTaken==0,"New wave resets metrics");
        Debug.Log("MELEE_BUILD_CHECKS_PASSED: presets, config isolation, blade edge/contact/cover, hammer window, stun feedback, damage attribution, actual healing/cooldown, overkill, death and wave lifecycle.");
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        if(stage==1)
        {
            try {CaptureFeedback();} catch(Exception error) {Debug.LogException(error); Finish(1); return;}
            stage=2; ready=EditorApplication.timeSinceStartup+1; return;
        }
        if(stage==2) {Finish(0); return;}
        try
        {
            originalAsync=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
            game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); original=Get("waveConfiguration");
            profile=MeleeBuildLab.CreateProfile(0); GameTestAccess.Set(game,"waveConfiguration",profile);
            Verify();
            GameTestAccess.Set(game,"waveConfiguration",original);
            ItemSystemChecks.Run(game);
            GameTestAccess.Set(game,"waveConfiguration",profile); Fresh();
            var enemy=Target(2.2f,0,120); Call("SlamHammer",new Vector3(2.2f,.035f,0));
            Target(-2,1,120); game.Health.TakeDamage(10); Call("HealFromItem",1,false);
            foreach(var target in Enemies)
            {
                var avatar=target.GetComponent<EnemyAvatar>();
                typeof(EnemyAvatar).GetField("emergeStarted",F).SetValue(avatar,Time.time-2);
                typeof(EnemyAvatar).GetMethod("Update",F).Invoke(avatar,null);
            }
            Call("UpdateNewWeapons",.01f);
            var camera=(PlaygroundCamera)Get("viewCamera"); camera.ThirdPerson=true; camera.Distance=7;
            game.enabled=true; Time.timeScale=0;
            stage=1; ready=EditorApplication.timeSinceStartup+.7;
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; Time.timeScale=1; ShaderUtil.allowAsyncCompilation=originalAsync;
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original);
        if(profile!=null) UnityEngine.Object.Destroy(profile);
        SessionState.SetBool("MeleeBuildChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
    static void CaptureFeedback()
    {
        System.IO.Directory.CreateDirectory("Logs");
        var camera=Camera.main; var target=new RenderTexture(1200,800,24);
        var previous=RenderTexture.active; var originalTarget=camera.targetTexture;
        Texture2D picture=null;
        try
        {
            camera.transform.position=new Vector3(0,7,-7);
            camera.transform.LookAt(new Vector3(0,.5f,0));
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            picture=new Texture2D(1200,800,TextureFormat.RGB24,false);
            picture.ReadPixels(new Rect(0,0,1200,800),0,0); picture.Apply();
            System.IO.File.WriteAllBytes("Logs/MeleeBuild-Feedback.png",picture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=originalTarget; RenderTexture.active=previous;
            UnityEngine.Object.Destroy(target); if(picture!=null) UnityEngine.Object.Destroy(picture);
        }
    }
}
