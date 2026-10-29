using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SplitterChecks
{
    static SurvivorPlayground game;
    static double ready;
    static bool capturePending;
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(string key)=>typeof(CombatController).GetProperty(key,Flags)?.GetValue(game)??GameTestAccess.Get(game,key);
    static object Call(string key,params object[] args)
    {
        var method=typeof(CombatController).GetMethod(key,Flags);
        if(method==null) {GameTestAccess.Call(game,key,args); return null;}
        var parameters=method.GetParameters(); var supplied=new object[parameters.Length];
        for(int i=0;i<supplied.Length;i++) supplied[i]=i<args.Length?args[i]:parameters[i].DefaultValue;
        return method.Invoke(game,supplied);
    }
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static HashSet<Transform> Mothers=>(HashSet<Transform>)Get("splitters");
    static Dictionary<Transform,float> Children=>(Dictionary<Transform,float>)Get("splitlings");
    static Dictionary<Transform,int> HP=>(Dictionary<Transform,int>)Get("enemyHealth");
    static List<Bounds> Blocks=>(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"));
    static void Check(bool value,string message) {if(!value) throw new Exception("SPLITTER_CHECK: "+message);}

    [MenuItem("Tools/Survivors/Verify Splitter")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("SplitterChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("SplitterChecks",false)) return;
            capturePending=false; ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void PositionPlayer(Vector3 at)
    {
        var player=(PlayerController)Get("player"); var controller=player.GetComponent<CharacterController>();
        controller.enabled=false; player.transform.position=at; controller.enabled=true; player.enabled=false;
    }
    static Transform Spawn(Vector3 at)
    {
        int count=Enemies.Count; Call("SpawnEnemy");
        Check(Enemies.Count==count+1,"Test mother spawns");
        var enemy=Enemies[Enemies.Count-1]; enemy.position=at; return enemy;
    }
    static Transform Fresh()
    {
        Call("ClearCombatObjects"); Call("ClearPickups",false);
        ((PlayerHealth)Get("health")).ResetHealth(); Blocks.Clear(); PositionPlayer(new Vector3(0,.5f,0));
        return Spawn(new Vector3(0,.375f,5));
    }
    static void Verify()
    {
        // Includes all previous enemy and wave-configuration regression checks using an isolated profile.
        typeof(WaveConfigurationChecks).GetMethod("Verify",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        object original=Get("waveConfiguration");
        var profile=ScriptableObject.CreateInstance<WaveConfiguration>();
        profile.Rules.TestMode=EnemyTestMode.SplitterOnly; profile.Rules.StartWave=11;
        GameTestAccess.Set(game,"waveConfiguration",profile);
        try
        {
            game.NewGame(); game.BeginRun(0); game.enabled=false;
            Check(Mothers.Count==2,"Splitter-only mode respects the two-mother stage cap");
            var enemy=Fresh();
            Check(Mothers.Contains(enemy) && HP[enemy]==45,"Mother has its own 45 base health");
            Check(enemy.GetComponentsInChildren<MeshRenderer>().Length>=2,"Mother FBX is loaded");
            Check((float)Call("EnemyRadius",enemy)==.5f,"Mother has its larger footprint");
            Call("MoveEnemies",.1f); Check(Mathf.Abs(enemy.position.z-(5-3.8f*.8f*.1f))<.001f,"Mother is slower than ordinary enemies");
            ((Dictionary<Transform,float>)Get("chilled"))[enemy]=Time.time+10;
            float before=enemy.position.z; Call("MoveEnemies",.1f);
            Check(before-enemy.position.z<3.8f*.8f*.1f,"Frost slows mother");
            Call("StunEnemy",enemy,1f); before=enemy.position.z; Call("MoveEnemies",.1f);
            Check(enemy.position.z==before,"Stun stops mother");

            enemy=Fresh(); int kills=game.Kills; int ordinal=(int)Get("waveSpawnCount");
            Call("DamageEnemy",Enemies.IndexOf(enemy),1000);
            Check(Mothers.Count==0 && Children.Count==0 && (int)Get("pendingSplitlings")==2,"Lethal hit queues births outside damage iteration");
            Call("UpdateSplitBirths");
            Check(Children.Count==2 && Enemies.Count==2 && (int)Get("waveSpawnCount")==ordinal,"Exactly two offspring; births do not consume portal ordinals");
            var pair=new List<Transform>(Children.Keys);
            foreach(var child in pair)
            {
                Check(HP[child]==14 && child.GetComponentsInChildren<MeshRenderer>().Length>=2,"Child uses its own model and 14 base health");
                var bounds=(Bounds)Call("EnemyHitBounds",child,0f);
                Check(Mathf.Abs(bounds.size.y-.58f)<.001f && Mathf.Abs(bounds.min.y)<.001f,"Child hit bounds are small and grounded");
                Vector3 start=child.position; Call("MoveEnemies",.1f); Check(child.position==start,"Emergence prevents immediate movement");
            }
            var fast=pair[0]; pair[1].position=new Vector3(5,.375f,5);
            fast.position=new Vector3(0,.375f,.35f); Call("MoveEnemies",.1f);
            Check(((PlayerHealth)Get("health")).Current==100,"Emerging child cannot deal contact damage");
            Children[fast]=0; fast.position=new Vector3(0,.375f,5);
            Call("MoveEnemies",.1f); Check(Mathf.Abs(fast.position.z-(5-3.8f*1.3f*.1f))<.001f,"Child pursues faster than ordinary enemies");
            ((Dictionary<Transform,float>)Get("chilled"))[fast]=Time.time+10;
            before=fast.position.z; Call("MoveEnemies",.1f); Check(before-fast.position.z<3.8f*1.3f*.1f,"Frost slows offspring");
            Call("StunEnemy",fast,1f); before=fast.position.z; Call("MoveEnemies",.1f); Check(fast.position.z==before,"Stun stops offspring");
            fast.position=new Vector3(0,.375f,3); int childHealth=HP[fast];
            Call("Shoot"); var shots=(IList)Get("bullets"); Check(shots.Count>0,"Rifle creates a test shot");
            var shot=shots[shots.Count-1]; shot.GetType().GetField("Direction").SetValue(shot,Vector3.forward);
            ((Transform)shot.GetType().GetField("Transform").GetValue(shot)).position=new Vector3(0,.375f,0);
            Call("MoveBullets",.2f); Check(!HP.ContainsKey(fast) || HP[fast]<childHealth,"Real rifle shots hit the smaller offspring");
            foreach(var child in pair) Call("KillEnemy",Enemies.IndexOf(child));
            Call("UpdateSplitBirths");
            Check(Enemies.Count==0 && (int)Get("pendingSplitlings")==0 && game.Kills==kills+3,"Offspring die once and never split again");
            Check(((IList)Get("pickups")).Count==3,"One mother plus two children yield exactly three normal drops");

            enemy=Fresh(); Spawn(new Vector3(2,.375f,5));
            for(int i=Enemies.Count-1;i>=0;i--) Call("DamageEnemy",i,1000);
            Call("UpdateSplitBirths"); Check(Children.Count==4 && Enemies.Count==4,"An area attack killing two mothers safely produces four children");
            Call("UpdateSplitBirths"); Check(Children.Count==4,"Birth processing does not duplicate offspring");

            enemy=Fresh(); enemy.position=new Vector3(17.4f,.375f,5);
            Blocks.Add(new Bounds(new Vector3(16.3f,0,5),new Vector3(1,20,3)));
            Call("KillEnemy",0); Call("UpdateSplitBirths");
            Check(Children.Count==2,"Death next to a wall and obstacle still finds two nearby positions");
            foreach(var child in Children.Keys)
                Check(child.position.x<=17.76f && ((ArenaObstacles)Get("obstacles")).HitDistance(child.position,Vector3.forward,.24f)>0,"Offspring remain inside arena and outside cover");

            enemy=Fresh(); Blocks.Add(new Bounds(new Vector3(0,0,5),new Vector3(4,20,4)));
            Call("KillEnemy",0); Call("UpdateSplitBirths");
            Check(Children.Count==0 && (int)Get("pendingSplitlings")==2,"Blocked positions wait instead of teleporting through obstacles");
            Blocks.Clear(); Call("UpdateSplitBirths"); Check(Children.Count==2,"Pending children appear once space opens");

            profile.Rules.Waves[10].EnemyLimit=2; game.NewGame(); game.BeginRun(0);
            Check(Mothers.Count==1 && (int)Get("ReservedEnemyCount")==2,"Small population cap reserves room for the split");
            enemy=Enemies[0]; enemy.position=new Vector3(0,.375f,5); Blocks.Clear(); Call("KillEnemy",0);
            Call("SpawnEnemy"); Check(Enemies.Count==0,"Portal cannot consume pending birth reservations");
            Call("UpdateSplitBirths"); Check(Enemies.Count==2,"Split respects exact population cap");

            enemy=Fresh(); Call("KillEnemy",0); Call("ClearCombatObjects"); Call("UpdateSplitBirths");
            Check(Enemies.Count==0 && (int)Get("pendingSplitlings")==0,"Cleanup cancels pending births");
            enemy=Fresh(); Call("KillEnemy",0); Call("ShowDefeat"); Call("UpdateSplitBirths");
            Check(Enemies.Count==0 && Children.Count==0,"Defeat clears pending offspring");
            game.NewGame(); game.BeginRun(0); enemy=Fresh();
            game.Run.Session.Advance(game.Run.Session.Remaining); Call("CompleteWave"); Call("UpdateSplitBirths");
            Check(Mothers.Count==0 && Children.Count==0 && (int)Get("pendingSplitlings")==0,"Settlement clears mothers without splitting");

            // Pose an actual mother beside actual death-spawned offspring for the scene preview.
            profile.Rules.Waves[10].EnemyLimit=54; game.NewGame(); game.BeginRun(0);
            enemy=Fresh(); Call("KillEnemy",0); Call("UpdateSplitBirths");
            int n=0; foreach(var child in Children.Keys) child.position=new Vector3(n++==0?-1.2f:1.2f,.375f,3.5f);
            enemy=Spawn(new Vector3(0,.375f,4.2f));
            foreach(var unit in Enemies) unit.GetComponent<EnemyAvatar>().Face(Vector3.back);
            PositionPlayer(new Vector3(-1.4f,.5f,1)); Physics.SyncTransforms();
            Debug.Log("SPLITTER_CHECKS_PASSED: model, health/speed, frost/stun, deferred births, no recursion, drops, obstacle/border placement, population reservations, cleanup and settlement.");
        }
        finally {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
        capturePending=true; ready=EditorApplication.timeSinceStartup+.6;
    }
    static void CapturePreview()
    {
        var camera=((PlaygroundCamera)Get("viewCamera")).GetComponent<Camera>();
        ((PlaygroundCamera)Get("viewCamera")).enabled=false; camera.orthographic=false; camera.fieldOfView=48;
        camera.transform.SetPositionAndRotation(new Vector3(4,6,-3),Quaternion.LookRotation(new Vector3(0,.4f,3)-new Vector3(4,6,-3)));
        var target=new RenderTexture(1200,800,24); var previous=RenderTexture.active; var original=camera.targetTexture;
        camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var picture=new Texture2D(1200,800,TextureFormat.RGB24,false); picture.ReadPixels(new Rect(0,0,1200,800),0,0); picture.Apply();
        System.IO.File.WriteAllBytes("ArtSource/Enemies/Splitter-InGame.png",picture.EncodeToPNG());
        camera.targetTexture=original; RenderTexture.active=previous;
        UnityEngine.Object.Destroy(picture); target.Release(); UnityEngine.Object.Destroy(target);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        try {if(capturePending) {CapturePreview(); Finish(0);} else Verify();}
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("SplitterChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
