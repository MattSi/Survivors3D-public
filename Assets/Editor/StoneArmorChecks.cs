using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StoneArmorChecks
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
    static void Check(bool value,string message) { if(!value) throw new Exception("STONE_ARMOR_CHECK: "+message); }

    [MenuItem("Tools/Survivors/Verify Stone Armor")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("StoneArmorChecks",true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("StoneArmorChecks",false)) return;
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
    static HashSet<Transform> Stones=>(HashSet<Transform>)Get("stoneArmors");
    static Transform Spawn(int ordinal,Vector3 position)
    {
        GameTestAccess.Set(game,"waveSpawnCount",ordinal-1); Call("SpawnEnemy");
        var enemy=Enemies[Enemies.Count-1]; enemy.position=position; return enemy;
    }
    static Transform Fresh()
    {
        Call("ClearCombatObjects"); ((PlayerHealth)Get("health")).ResetHealth(); Call("SetPlayerHit",false);
        PositionPlayer(new Vector3(0,.5f,0));
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"))).Clear();
        return Spawn(7,new Vector3(0,.375f,5));
    }
    static void Verify()
    {
        // Re-run the charger regression checks against shared spawning, collision and avatar changes.
        typeof(ChargerChecks).GetMethod("Verify",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        game.NewGame(); game.BeginRun(0); game.enabled=false;
        var player=(PlayerController)Get("player"); player.enabled=false;
        var session=game.Run.Session;
        for(int wave=1;wave<5;wave++)
        {
            for(int n=0;n<35;n++) { GameTestAccess.Set(game,"waveSpawnCount",n); Check(!(bool)Get("NextEnemyIsStoneArmor"),"No stone armor before wave five"); }
            session.Advance(session.WaveDuration); session.Continue();
        }
        var enemy=Fresh();
        var hp=(Dictionary<Transform,int>)Get("enemyHealth");
        Check(Stones.Contains(enemy) && hp[enemy]==120,"Stone uses its own 120 base health");
        Check(enemy.GetComponent<CapsuleCollider>().enabled,"Stone armor has a solid player-blocking body");
        var renderers=enemy.GetComponentsInChildren<MeshRenderer>();
        Check(renderers.Length>=2 && renderers[1].bounds.size.y>1.25f,"Larger FBX model is loaded and grounded");
        Call("MoveEnemies",.1f);
        Check(Mathf.Abs(enemy.position.z-(5-3.2f*.55f*.1f))<.001f,"Stone walks at 55 percent normal wave speed");
        ((Dictionary<Transform,float>)Get("chilled"))[enemy]=Time.time+10;
        float before=enemy.position.z; Call("MoveEnemies",.1f);
        Check(before-enemy.position.z<3.2f*.55f*.1f,"Frost slows stone armor too");
        Call("StunEnemy",enemy,1f); before=enemy.position.z; Call("MoveEnemies",.1f);
        Check(enemy.position.z==before,"Hammer stun stops stone armor");

        enemy=Fresh(); enemy.position=new Vector3(0,.375f,2);
        Physics.SyncTransforms(); player.GetComponent<CharacterController>().Move(Vector3.forward*4);
        Check(player.transform.position.z<1.2f,"Player cannot walk through a stone-armored enemy");
        Call("MoveEnemies",.1f);
        Check(((PlayerHealth)Get("health")).Current==90,"Solid-body contact still damages the player");

        enemy=Fresh(); enemy.position=new Vector3(.7f,.375f,3);
        Call("Shoot"); var bullets=(IList)Get("bullets");
        Check(bullets.Count>0,"Rifle creates a test projectile");
        object bullet=bullets[bullets.Count-1];
        bullet.GetType().GetField("Direction").SetValue(bullet,Vector3.forward);
        ((Transform)bullet.GetType().GetField("Transform").GetValue(bullet)).position=new Vector3(0,.5f,0);
        Call("MoveBullets",.2f);
        Check(hp[enemy]<120,"Grazing shots hit the widened stone body");

        enemy=Fresh(); var normal=Spawn(1,new Vector3(.5f,.375f,5));
        Call("SeparateEnemies");
        Check(Vector3.Distance(enemy.position,normal.position)>=1.074f,"Enemy separation includes the larger stone footprint");
        Check(Mathf.Abs(enemy.position.x)<Mathf.Abs(normal.position.x-.5f),"Heavy stone yields less than a normal enemy");
        enemy.position=new Vector3(17,.375f,5); Call("PushEnemyApart",enemy,Vector3.right,5f);
        Check(enemy.position.x<=17.4f,"Knockback keeps the full stone body inside arena bounds");

        enemy=Fresh(); var second=Spawn(7,new Vector3(3,.375f,5));
        Check(Stones.Count==2,"Early stone armor cap is two");
        GameTestAccess.Set(game,"waveSpawnCount",6); Check(!(bool)Get("NextEnemyIsStoneArmor"),"Stone cap falls back to regular enemies");
        Call("KillEnemy",Enemies.IndexOf(enemy));
        Check(!Stones.Contains(enemy) && !enemy.GetComponent<CapsuleCollider>().enabled,"Dead stone stops blocking immediately");
        Call("ClearCombatObjects"); Check(Stones.Count==0 && !second.GetComponent<CapsuleCollider>().enabled,"Wave cleanup removes solid bodies immediately");

        enemy=Fresh(); enemy.position=new Vector3(0,.375f,2);
        var charger=Spawn(6,new Vector3(0,.375f,5));
        object state=State(charger); Set(state,"Timer",0f);
        Call("MoveEnemies",.1f); Call("MoveEnemies",.9f); Call("MoveEnemies",1f);
        Check(Phase(charger)=="Recover" && charger.position.z>enemy.position.z+.9f,"A charging imp cannot pass through stone armor");

        enemy=Fresh();
        var obstacles=(ArenaObstacles)Get("obstacles"); obstacles.Generate();
        var bounds=obstacles.Blocks[0];
        Vector3 origin=new Vector3(bounds.min.x-1,.375f,bounds.center.z);
        Vector3 moved=obstacles.Move(origin,new Vector3(bounds.max.x+2,.375f,bounds.center.z),.1f,.6f);
        var inflated=bounds; inflated.Expand(new Vector3(1.2f,0,1.2f));
        Check(!inflated.Contains(moved),"Navigation respects stone radius near obstacles");

        // Pose three real models together in the existing arena for a size/style comparison.
        enemy=Fresh(); enemy.position=new Vector3(0,.375f,4);
        normal=Spawn(1,new Vector3(-1.6f,.375f,4));
        charger=Spawn(6,new Vector3(1.6f,.375f,4));
        foreach(var unit in Enemies) unit.GetComponent<EnemyAvatar>().Face(Vector3.back);
        PositionPlayer(new Vector3(-1.5f,.5f,0)); Physics.SyncTransforms();
        WaveSessionChecks.Run();
        Debug.Log("STONE_ARMOR_CHECKS_PASSED: wave gating, health, size, speed, frost, stun, player blocking/contact, widened hits, separation, borders, caps, cleanup, charger collision and navigation.");
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
        System.IO.File.WriteAllBytes("ArtSource/Enemies/StoneArmor-InGame.png",picture.EncodeToPNG());
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
        EditorApplication.update-=Tick; SessionState.SetBool("StoneArmorChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
