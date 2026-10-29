using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BomberChecks
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
    static void Check(bool value,string message) { if(!value) throw new Exception("BOMBER_CHECK: "+message); }

    [MenuItem("Tools/Survivors/Verify Hooded Bomber")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("BomberChecks",true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BomberChecks",false)) return;
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
    static IDictionary Bombers=>(IDictionary)Get("bombers");
    static IList Bombs=>(IList)Get("enemyBombs");
    static Transform Spawn(int ordinal,Vector3 at)
    {
        GameTestAccess.Set(game,"waveSpawnCount",ordinal-1); Call("SpawnEnemy");
        Transform enemy=Enemies[Enemies.Count-1]; enemy.position=at; return enemy;
    }
    static Transform Fresh()
    {
        Call("ClearCombatObjects"); ((PlayerHealth)Get("health")).ResetHealth(); Call("SetPlayerHit",false);
        PositionPlayer(new Vector3(0,.5f,0));
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"))).Clear();
        Transform enemy=Spawn(9,new Vector3(0,.375f,6)); Set(Bombers[enemy],"Timer",0f); return enemy;
    }
    static void Windup(Transform enemy)
    {
        Call("MoveEnemies",.1f);
        Check((bool)Field(Bombers[enemy],"WindingUp") && Bombs.Count==0,"Windup precedes projectile release");
        Check(Field(Bombers[enemy],"Warning") as Transform != null,"Warning starts before the throw");
    }
    static void Throw(Transform enemy) {Windup(enemy); Call("MoveEnemies",.7f); Check(Bombs.Count==1,"One windup releases one bomb");}
    static void Verify()
    {
        typeof(StoneArmorChecks).GetMethod("Verify",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        game.NewGame(); game.BeginRun(0); game.enabled=false; ((PlayerController)Get("player")).enabled=false;
        var session=game.Run.Session;
        for(int wave=1;wave<8;wave++)
        {
            for(int n=0;n<45;n++) {GameTestAccess.Set(game,"waveSpawnCount",n); Check(!(bool)Get("NextEnemyIsBomber"),"Bombers unlock at wave eight");}
            session.Advance(session.WaveDuration); session.Continue();
        }
        Transform enemy=Fresh();
        Check(Bombers.Contains(enemy),"Ninth successful arrival can become a bomber");
        Check(enemy.GetComponentsInChildren<MeshRenderer>().Length>=4,"Bomber body, arm and held bomb are imported");
        Check(((Dictionary<Transform,int>)Get("enemyHealth"))[enemy]==24,"Bomber uses its own base health");
        Set(Bombers[enemy],"Timer",10f); Vector3 start=enemy.position; Call("MoveEnemies",.1f);
        Check(enemy.position==start,"Bomber holds a useful midrange distance");
        enemy.position=new Vector3(0,.375f,12); Call("MoveEnemies",.1f); Check(enemy.position.z<12,"Distant bomber approaches");
        enemy.position=new Vector3(0,.375f,3); Call("MoveEnemies",.1f); Check(enemy.position.z>3,"Close bomber retreats");
        float normalRetreat=enemy.position.z-3;
        ((Dictionary<Transform,float>)Get("chilled"))[enemy]=Time.time+10;
        enemy.position=new Vector3(0,.375f,3); Call("MoveEnemies",.1f);
        Check(enemy.position.z-3<normalRetreat,"Frost slows retreat");

        enemy=Fresh(); float handY=enemy.GetComponent<EnemyAvatar>().BombHandPosition.y;
        Windup(enemy); Vector3 locked=(Vector3)Field(Bombers[enemy],"Target");
        Call("MoveEnemies",.4f);
        Check(enemy.GetComponent<EnemyAvatar>().BombHandPosition.y>handY+.1f,"Throw arm visibly raises the held bomb");
        PositionPlayer(new Vector3(3,.5f,0)); Call("MoveEnemies",.3f);
        Check(Bombs.Count==1 && (Vector3)Field(Bombs[0],"Target")==locked,"Throw locks the warned point instead of tracking a sidestep");
        Call("UpdateEnemyBombs",.45f);
        Check(((Transform)Field(Bombs[0],"Visual")).position.y>1.5f,"Bomb follows a raised arc");
        Call("UpdateEnemyBombs",2f);
        Check(Bombs.Count==0 && ((PlayerHealth)Get("health")).Current==100,"Sidestepping avoids the explosion even on long frames");

        enemy=Fresh(); Throw(enemy); Call("UpdateEnemyBombs",.9f); Call("UpdateEnemyBombs",.74f);
        Check(((PlayerHealth)Get("health")).Current==100 && Bombs.Count==1,"Landing leaves the full fuse window");
        Call("UpdateEnemyBombs",.02f);
        Check(((PlayerHealth)Get("health")).Current==85 && Bombs.Count==0,"Explosion damages once and removes the bomb");
        Call("UpdateEnemyBombs",3f); Check(((PlayerHealth)Get("health")).Current==85,"Detonation does not repeat");

        enemy=Fresh(); Throw(enemy); PositionPlayer(new Vector3(1.5f,.5f,0)); Call("UpdateEnemyBombs",2f);
        Check(((PlayerHealth)Get("health")).Current==100,"Outside the warning radius is safe");
        enemy=Fresh(); Throw(enemy); PositionPlayer(new Vector3(1.3f,.5f,0)); Call("UpdateEnemyBombs",2f);
        Check(((PlayerHealth)Get("health")).Current==85,"Inside the warning radius receives damage");
        enemy=Fresh(); Throw(enemy); PositionPlayer(new Vector3(1,.5f,0));
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"))).Add(new Bounds(new Vector3(.5f,0,0),new Vector3(.2f,20,2)));
        Call("UpdateEnemyBombs",2f); Check(((PlayerHealth)Get("health")).Current==100,"Solid cover blocks explosion damage");

        enemy=Fresh(); Windup(enemy); Call("StunEnemy",enemy,1f);
        Check(!(bool)Field(Bombers[enemy],"WindingUp") && Field(Bombers[enemy],"Warning") as Transform==null,"Stun cancels an unreleased throw and warning");
        Call("MoveEnemies",1f); Check(Bombs.Count==0,"A stunned thrower cannot release a bomb");
        enemy=Fresh(); Windup(enemy); Call("KillEnemy",Enemies.IndexOf(enemy));
        Check(Bombers.Count==0 && Bombs.Count==0,"Killing a winding-up bomber prevents release");
        enemy=Fresh(); Throw(enemy); Call("KillEnemy",Enemies.IndexOf(enemy));
        Check(Bombs.Count==1,"Released bombs retain their warning after their owner dies");
        Transform warning=(Transform)Field(Bombs[0],"Warning"); Call("ClearCombatObjects");
        Check(Bombs.Count==0 && !warning.gameObject.activeSelf,"Wave cleanup immediately removes airborne bombs and warnings");
        enemy=Fresh(); Throw(enemy); Call("ShowDefeat"); Check(Bombers.Count==0 && Bombs.Count==0,"Defeat clears all bombs and warnings");

        enemy=Fresh(); Spawn(9,new Vector3(3,.375f,6));
        GameTestAccess.Set(game,"waveSpawnCount",8); Check(Bombers.Count==2 && !(bool)Get("NextEnemyIsBomber"),"Early bomber population is capped at two");
        Windup(enemy); Call("MoveEnemies",.7f); Set(Bombers[enemy],"Timer",0f);
        Call("MoveEnemies",.1f); Call("MoveEnemies",.7f); Set(Bombers[enemy],"Timer",0f); Call("MoveEnemies",.1f);
        Check(Bombs.Count==2 && !(bool)Field(Bombers[enemy],"WindingUp"),"Pressure cap includes both released bombs and reserved windups");

        // Hold a real airborne projectile for the in-game preview; the combat host is disabled.
        enemy=Fresh(); Throw(enemy); Call("UpdateEnemyBombs",.45f);
        PositionPlayer(new Vector3(-1.8f,.5f,0));
        WaveSessionChecks.Run();
        Debug.Log("BOMBER_CHECKS_PASSED: model/arm, wave gating, range control, frost, windup, locked target, arc/fuse, damage, cover, stun, death, cleanup and pressure caps.");
        capturePending=true; ready=EditorApplication.timeSinceStartup+.5;
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
        System.IO.File.WriteAllBytes("ArtSource/Enemies/Bomber-InGame.png",picture.EncodeToPNG());
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
        EditorApplication.update-=Tick; SessionState.SetBool("BomberChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
