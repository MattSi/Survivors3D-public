using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ChargerChecks
{
    static SurvivorPlayground game;
    static double ready,deadline;
    static bool capturePending;
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(string key)=>GameTestAccess.Get(game,key);
    static void Call(string key,params object[] args)=>GameTestAccess.Call(game,key,args);
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static IDictionary Chargers=>(IDictionary)Get("chargers");
    static object State(Transform enemy)=>Chargers[enemy];
    static object Field(object state,string key)=>state.GetType().GetField(key).GetValue(state);
    static void Set(object state,string key,object value)=>state.GetType().GetField(key).SetValue(state,value);
    static string Phase(Transform enemy)=>Field(State(enemy),"Phase").ToString();
    static void Check(bool value,string message) { if(!value) throw new Exception("CHARGER_CHECK: "+message); }

    [MenuItem("Tools/Survivors/Verify Charging Imp")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("ChargerChecks",true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ChargerChecks",false)) return;
            capturePending=false; ready=EditorApplication.timeSinceStartup+2; deadline=ready+60;
            EditorApplication.update+=Tick;
        };
    }
    static void PositionPlayer(Vector3 at)
    {
        var player=(PlayerController)Get("player");
        var controller=player.GetComponent<CharacterController>();
        controller.enabled=false; player.transform.position=at; controller.enabled=true;
    }
    static Transform Fresh()
    {
        Call("ClearCombatObjects"); ((PlayerHealth)Get("health")).ResetHealth();
        Call("SetPlayerHit",false);
        PositionPlayer(new Vector3(0,.5f,0));
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"))).Clear();
        GameTestAccess.Set(game,"waveSpawnCount",5);
        Call("SpawnEnemy");
        Transform enemy=Enemies[Enemies.Count-1];
        enemy.position=new Vector3(0,.375f,5);
        Check(Chargers.Contains(enemy),"Wave three substitutes a charger on the sixth successful spawn");
        Set(State(enemy),"Timer",0f);
        return enemy;
    }
    static void Windup(Transform enemy)
    {
        Call("MoveEnemies",.1f);
        Check(Phase(enemy)=="Windup" && enemy.position.z==5,"Windup plants the enemy before charging");
        Check(Field(State(enemy),"Warning") as Transform != null,"Windup creates a ground warning");
    }
    static void Dash(Transform enemy)
    {
        Windup(enemy); Call("MoveEnemies",.9f);
        Check(Phase(enemy)=="Dash" && Field(State(enemy),"Warning") as Transform == null,"Dash starts only after warning and removes the warning");
    }
    static void Verify()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        game.NewGame(); game.BeginRun(0); game.enabled=false;
        ((PlayerController)Get("player")).enabled=false;
        var session=game.Run.Session;
        for(int wave=1;wave<=2;wave++)
        {
            for(int spawn=0;spawn<30;spawn++)
            {
                GameTestAccess.Set(game,"waveSpawnCount",spawn);
                Check(!(bool)Get("NextEnemyIsCharger"),"No chargers in the first two waves");
            }
            session.Advance(session.WaveDuration); session.Continue();
        }
        Transform enemy=Fresh();
        Check(enemy.GetComponentsInChildren<MeshRenderer>().Length>=2,"The charger FBX renders rather than its fallback cube");
        Windup(enemy);
        Vector3 locked=(Vector3)Field(State(enemy),"Direction");
        PositionPlayer(new Vector3(3,.5f,0));
        Call("MoveEnemies",.9f); Call("MoveEnemies",.7f);
        Check((Vector3)Field(State(enemy),"Direction")==locked && Mathf.Abs(enemy.position.x)<.001f,
            "A sidestep does not bend the locked charge lane");
        Check(((PlayerHealth)Get("health")).Current==100,"Sidestepping the lane avoids damage");
        Check(Phase(enemy)=="Recover","Dash has a bounded distance and enters recovery");
        Vector3 stopped=enemy.position; Call("MoveEnemies",.5f);
        Check(enemy.position==stopped,"Recovery gives a stationary retaliation window");
        Call("MoveEnemies",.7f); Check(Phase(enemy)=="Chase","Recovery returns to chase");

        enemy=Fresh(); Dash(enemy); Call("MoveEnemies",.7f);
        Check(((PlayerHealth)Get("health")).Current==85,"Swept dash hits once even when one frame crosses the player");
        Call("MoveEnemies",.2f); Check(((PlayerHealth)Get("health")).Current==85,"Recovery does not add contact damage");

        enemy=Fresh(); Dash(enemy);
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles")))
            .Add(new Bounds(new Vector3(0,0,2),new Vector3(3,20,1)));
        Call("MoveEnemies",1f);
        Check(enemy.position.z>=2.875f && Phase(enemy)=="Recover","A swept dash stops its footprint before an obstacle");
        Check(((PlayerHealth)Get("health")).Current==100,"A rock blocks damage to the player behind it");

        enemy=Fresh(); Dash(enemy);
        ((Dictionary<Transform,float>)Get("chilled"))[enemy]=Time.time+10;
        Call("MoveEnemies",.1f);
        Check(5-enemy.position.z<1 && 5-enemy.position.z>0,"Frost slows the dash");
        Call("StunEnemy",enemy,1f);
        Check(Phase(enemy)=="Recover" && Field(State(enemy),"Warning") as Transform==null,"Stun interrupts a dash");
        stopped=enemy.position; Call("MoveEnemies",.5f);
        Check(enemy.position==stopped,"Stunned chargers remain planted");

        enemy=Fresh(); Windup(enemy); Call("StunEnemy",enemy,1f);
        Check(Phase(enemy)=="Recover" && Field(State(enemy),"Warning") as Transform==null,"Stun also cancels windup telegraphs");
        enemy=Fresh(); Windup(enemy); Call("KillEnemy",0);
        Check(Chargers.Count==0,"Death removes charge state and warning");
        enemy=Fresh(); Windup(enemy); Call("ClearCombatObjects");
        Check(Chargers.Count==0,"Arena cleanup removes charge state and warning");

        enemy=Fresh();
        GameTestAccess.Set(game,"waveSpawnCount",5); Call("SpawnEnemy");
        Transform second=Enemies[Enemies.Count-1]; second.position=new Vector3(3,.375f,5);
        Check(Chargers.Count==2,"Early waves permit at most two living chargers");
        GameTestAccess.Set(game,"waveSpawnCount",5);
        Check(!(bool)Get("NextEnemyIsCharger"),"Reaching the charger cap keeps normal spawns");
        Set(State(second),"Timer",0f); Windup(enemy);
        Check(Phase(second)=="Chase","Only one early charger may commit at once");
        Call("ShowDefeat"); Check(Chargers.Count==0,"Defeat clears all charge warnings and state");

        enemy=Fresh(); Dash(enemy);
        enemy.position=new Vector3(0,.375f,-17);
        PositionPlayer(new Vector3(4,.5f,0));
        Call("MoveEnemies",1f);
        Check(enemy.position.z>=-17.625f && Phase(enemy)=="Recover","Charge footprint stays inside arena borders");

        // Make a real scene render with a visible windup, not an ImageGen mockup.
        enemy=Fresh(); Windup(enemy); Call("MoveEnemies",.3f);
        enemy.GetComponent<EnemyAvatar>().enabled=false;
        WaveSessionChecks.Run();
        capturePending=true; ready=EditorApplication.timeSinceStartup+.5;
        Debug.Log("CHARGER_CHECKS_PASSED: model, wave gating, locked aim, sidestep, swept damage, obstacles, recovery, frost, stun, cleanup, caps and bounds.");
    }
    static void CapturePreview()
    {
        // Give deferred Destroy calls a frame to remove earlier test targets before rendering.
        var camera=((PlaygroundCamera)Get("viewCamera")).GetComponent<Camera>();
        ((PlaygroundCamera)Get("viewCamera")).enabled=false;
        camera.orthographic=false; camera.fieldOfView=48;
        camera.transform.SetPositionAndRotation(new Vector3(4,6,-4),Quaternion.LookRotation(new Vector3(0,.4f,2.5f)-new Vector3(4,6,-4)));
        var target=new RenderTexture(1200,800,24);
        var previous=RenderTexture.active; var original=camera.targetTexture;
        camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var picture=new Texture2D(1200,800,TextureFormat.RGB24,false);
        picture.ReadPixels(new Rect(0,0,1200,800),0,0); picture.Apply();
        System.IO.File.WriteAllBytes("ArtSource/Enemies/Charger-InGame.png",picture.EncodeToPNG());
        camera.targetTexture=original; RenderTexture.active=previous;
        UnityEngine.Object.Destroy(picture); target.Release(); UnityEngine.Object.Destroy(target);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Charger checks timed out");
            if(capturePending) { CapturePreview(); Finish(0); }
            else Verify();
        }
        catch(Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("ChargerChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
