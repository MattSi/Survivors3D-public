using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShooterChecks
{
    static SurvivorPlayground game;
    static double ready;
    static bool capturePending;
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static object Call(string name,params object[] args)=>typeof(CombatController).GetMethod(name,Flags).Invoke(game,args);
    static object Field(object state,string name)=>state.GetType().GetField(name).GetValue(state);
    static void Set(object state,string name,object value)=>state.GetType().GetField(name).SetValue(state,value);
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static IDictionary Shooters=>(IDictionary)Get("shooters");
    static IList Needles=>(IList)Get("enemyNeedles");
    static List<Bounds> Blocks=>(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",Flags).GetValue(Get("obstacles"));
    static PlayerHealth Health=>(PlayerHealth)Get("health");
    static Camera Camera=>((PlaygroundCamera)Get("viewCamera")).GetComponent<Camera>();
    static void Check(bool ok,string message) {if(!ok) throw new Exception("SHOOTER_CHECK: "+message);}
    [MenuItem("Tools/Survivors/Verify Bone Needle Shooter")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("ShooterChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ShooterChecks",false)) return;
            capturePending=false; ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void PositionPlayer(Vector3 at)
    {
        var player=(PlayerController)Get("player"); var controller=player.GetComponent<CharacterController>();
        controller.enabled=false; player.transform.position=at; controller.enabled=true; player.enabled=false;
    }
    static void FrameArena()
    {
        ((PlaygroundCamera)Get("viewCamera")).enabled=false;
        Camera.orthographic=true; Camera.orthographicSize=12;
        Camera.transform.SetPositionAndRotation(new Vector3(0,16,-8),Quaternion.LookRotation(new Vector3(0,0,3)-new Vector3(0,16,-8)));
    }
    static Transform Spawn(Vector3 at,int ordinal=8)
    {
        int count=Enemies.Count;
        GameTestAccess.Set(game,"waveSpawnCount",ordinal-1); Call("SpawnEnemy");
        Check(Enemies.Count==count+1,"Enemy arrives within the configured population budget");
        Transform enemy=Enemies[Enemies.Count-1]; enemy.position=at; return enemy;
    }
    static Transform Fresh()
    {
        Call("ClearCombatObjects"); Call("ClearPickups",false); Health.ResetHealth(); Call("SetPlayerHit",false);
        PositionPlayer(new Vector3(0,.5f,0)); Blocks.Clear(); FrameArena();
        var enemy=Spawn(new Vector3(0,.375f,7)); Set(Shooters[enemy],"Timer",0f); return enemy;
    }
    static void Windup(Transform enemy)
    {
        Call("MoveEnemies",.01f);
        Check((bool)Field(Shooters[enemy],"WindingUp") && Needles.Count==0,"Visible shooter warns before releasing a needle");
        Check(Field(Shooters[enemy],"Warning") as LineRenderer != null,"Aim line exists throughout windup");
    }
    static void Release(Transform enemy)
    {
        Windup(enemy); Call("MoveEnemies",.51f); Call("MoveEnemies",.31f);
        Check(Needles.Count==1,"One completed windup emits one needle");
    }
    static void Verify()
    {
        WaveRulesChecks.Run();
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        object original=Get("waveConfiguration");
        var profile=ScriptableObject.CreateInstance<WaveConfiguration>();
        profile.Rules.TestMode=EnemyTestMode.ShooterOnly; profile.Rules.StartWave=6;
        GameTestAccess.Set(game,"waveConfiguration",profile);
        try
        {
            game.NewGame(); game.BeginRun(0); game.enabled=false;
            var enemy=Fresh(); var avatar=enemy.GetComponent<EnemyAvatar>();
            Check(((Dictionary<Transform,int>)Get("enemyHealth"))[enemy]==18,"Shooter uses independent base health");
            Check(Resources.Load<GameObject>("Characters/EnemyShooter")!=null && enemy.GetComponentsInChildren<MeshRenderer>().Length>=3,"Body and articulated arm are imported");
            Transform muzzle=null,breech=null;
            foreach(var part in enemy.GetComponentsInChildren<Transform>()) {if(part.name=="Muzzle") muzzle=part; if(part.name=="Breech") breech=part;}
            Check(muzzle!=null && breech!=null,"Named muzzle anchors survive FBX import");
            Set(Shooters[enemy],"Timer",1.5f); Call("MoveEnemies",.1f);
            Check(Mathf.Abs(enemy.position.z-7)<.001f,"Shooter holds midrange");
            enemy.position=new Vector3(0,.375f,12); Call("MoveEnemies",.1f);
            Check(enemy.position.z<12,"Distant shooter approaches");
            enemy.position=new Vector3(0,.375f,3); Call("MoveEnemies",.1f);
            float retreat=enemy.position.z-3; Check(retreat>0,"Close shooter makes a short retreat");
            Set(Shooters[enemy],"RetreatCycle",0f);
            ((Dictionary<Transform,float>)Get("chilled"))[enemy]=Time.time+10;
            enemy.position=new Vector3(0,.375f,3); Call("MoveEnemies",.1f);
            Check(enemy.position.z-3<retreat,"Frost slows the retreat");
            Set(Shooters[enemy],"RetreatCycle",.7f); var rest=enemy.position; Call("MoveEnemies",.1f);
            Check(enemy.position==rest,"Retreat includes a pause that melee players can punish");

            enemy=Fresh(); avatar=enemy.GetComponent<EnemyAvatar>();
            float idleY=avatar.ShooterMuzzlePosition.y;
            Windup(enemy); Call("MoveEnemies",.51f);
            Check(avatar.ShooterMuzzlePosition.y>idleY+.08f,"Firing arm visibly rises");
            var locked=(Vector3)Field(Shooters[enemy],"Direction");
            Check((bool)Field(Shooters[enemy],"Locked") && Needles.Count==0,"Last 0.3 seconds reserve a locked dodge window");
            PositionPlayer(new Vector3(3,.5f,0)); Call("MoveEnemies",.2f);
            Check((Vector3)Field(Shooters[enemy],"Direction")==locked && Needles.Count==0,"Locked aim does not track a sidestep");
            Call("MoveEnemies",.11f); Check(Needles.Count==1 && (Vector3)Field(Needles[0],"Direction")==locked,"Projectile keeps the warned direction");
            Call("UpdateEnemyNeedles",3f); Check(Needles.Count==0 && Health.Current==100,"Sidestep avoids the full swept projectile");

            enemy=Fresh(); Windup(enemy); Call("MoveEnemies",5f);
            Check(Needles.Count==0 && (bool)Field(Shooters[enemy],"Locked"),"Long frame cannot erase the committed warning window");
            Call("MoveEnemies",.31f); Check(Needles.Count==1,"Long-frame windup still releases only one shot");
            Call("UpdateEnemyNeedles",2f); Check(Health.Current==92 && Needles.Count==0,"Swept collision deals eight damage without tunneling");
            Call("UpdateEnemyNeedles",2f); Check(Health.Current==92,"A consumed needle cannot hit twice");

            enemy=Fresh(); Release(enemy);
            var friendly=Spawn(new Vector3(0,.375f,3.5f));
            var enemyHealth=(Dictionary<Transform,int>)Get("enemyHealth");
            int friendlyHealth=enemyHealth[friendly],ownerHealth=enemyHealth[enemy];
            Call("UpdateEnemyNeedles",.5f);
            Check(Needles.Count==1 && ((Transform)Field(Needles[0],"Visual")).position.z<friendly.position.z
                && enemyHealth[friendly]==friendlyHealth && Health.Current==100,"Needle passes through an enemy without damage or consumption");
            Call("UpdateEnemyNeedles",1f);
            Check(Needles.Count==0 && Health.Current==92 && enemyHealth[friendly]==friendlyHealth
                && enemyHealth[enemy]==ownerHealth,"Needle damages only the player after passing through an enemy");

            enemy=Fresh(); Release(enemy); Health.ShieldCharges=1; Call("UpdateEnemyNeedles",2f);
            Check(Health.Current==100 && Health.ShieldCharges==0 && Needles.Count==0,"Existing shield consumes the needle");
            enemy=Fresh(); Release(enemy); Health.TakeDamage(1); Call("UpdateEnemyNeedles",2f);
            Check(Health.Current==99 && Needles.Count==0,"Invulnerability consumes the needle without a second hit");
            enemy=Fresh(); Release(enemy); Blocks.Add(new Bounds(new Vector3(0,0,3.5f),new Vector3(3,20,.25f)));
            Call("UpdateEnemyNeedles",2f); Check(Health.Current==100 && Needles.Count==0,"Cover blocks the swept shot before the player");
            enemy=Fresh(); Blocks.Add(new Bounds(new Vector3(0,0,3.5f),new Vector3(3,20,.25f))); Call("MoveEnemies",.1f);
            Check(!(bool)Field(Shooters[enemy],"WindingUp"),"Blocked line of sight cannot start a shot");
            enemy=Fresh(); Camera.transform.rotation=Quaternion.LookRotation(Vector3.back); Call("MoveEnemies",.1f);
            Check(!(bool)Field(Shooters[enemy],"WindingUp"),"Offscreen shooter cannot start a shot");
            FrameArena(); Windup(enemy); Camera.transform.rotation=Quaternion.LookRotation(Vector3.back); Call("MoveEnemies",.1f);
            Check(!(bool)Field(Shooters[enemy],"WindingUp") && Field(Shooters[enemy],"Warning")==null,"Leaving the screen cancels windup and its warning");

            enemy=Fresh(); Windup(enemy); Call("StunEnemy",enemy,1f); Call("MoveEnemies",2f);
            Check(!(bool)Field(Shooters[enemy],"WindingUp") && Needles.Count==0,"Hammer stun cancels unreleased shots");
            enemy=Fresh(); Windup(enemy); Call("KillEnemy",Enemies.IndexOf(enemy));
            Check(Shooters.Count==0 && Needles.Count==0,"Death cancels windup");
            enemy=Fresh(); Release(enemy); Call("KillEnemy",Enemies.IndexOf(enemy));
            Check(Shooters.Count==0 && Needles.Count==1,"Released needle persists after its owner's death");
            var visual=(Transform)Field(Needles[0],"Visual"); Call("ClearCombatObjects");
            Check(Needles.Count==0 && !visual.gameObject.activeSelf,"Cleanup immediately hides every projectile");
            enemy=Fresh(); Release(enemy); PositionPlayer(new Vector3(3,.5f,0)); Call("UpdateEnemyNeedles",.5f);
            var before=((Transform)Field(Needles[0],"Visual")).position;
            Call("UpdateEnemyNeedles",0f); Call("UpdateEnemyNeedles",float.NaN);
            Check(((Transform)Field(Needles[0],"Visual")).position==before,"Paused and invalid ticks do not move shots");
            Call("UpdateEnemyNeedles",2f); Check(Needles.Count==0,"Missed shots expire at their range limit");

            enemy=Fresh(); var second=Spawn(new Vector3(2,.375f,7)); Set(Shooters[second],"Timer",0f);
            Call("SpawnEnemy"); Check(Shooters.Count==2 && Enemies.Count==2,"Early wave caps shooter population at two");
            Windup(enemy);
            Check(!(bool)Field(Shooters[second],"WindingUp"),"Only one early shooter can reserve an attack");
            Call("MoveEnemies",.51f); Call("MoveEnemies",.31f); Set(Shooters[second],"Timer",0f); Call("MoveEnemies",.01f);
            Check(Needles.Count==1 && !(bool)Field(Shooters[second],"WindingUp"),"Live needles also occupy the attack budget");
            Call("ShowDefeat"); Check(Needles.Count==0 && Shooters.Count==0,"Defeat clears all shooter state");
            enemy=Fresh(); Release(enemy); Health.TakeDamage(92);
            typeof(PlayerHealth).GetField("vulnerableAt",Flags).SetValue(Health,0f);
            Call("UpdateEnemyNeedles",2f); Check(Health.IsDead && Needles.Count==0 && Shooters.Count==0,"Lethal projectile safely triggers synchronous defeat cleanup");
            game.NewGame(); game.BeginRun(0); game.enabled=false;
            enemy=Fresh(); Release(enemy); Call("FinishWave");
            Check(Shooters.Count==0 && Needles.Count==0,"Wave settlement removes hostile shots");

            profile.Rules.StartWave=13; profile.Rules.TestMode=EnemyTestMode.Mixed;
            game.NewGame(); game.BeginRun(0); game.enabled=false;
            enemy=Fresh(); Spawn(new Vector3(-3,.375f,6),5); Spawn(new Vector3(3,.375f,6),7);
            Spawn(new Vector3(5,.375f,7),9); Spawn(new Vector3(-5,.375f,7),11);
            Check(Shooters.Count==1 && ((IDictionary)Get("chargers")).Count==1 && ((IDictionary)Get("bombers")).Count==1,"Shooter coexists with other special enemies");
            Call("MoveEnemies",.1f); Check(Enemies.Count==5,"Mixed roster moves without replacing existing types");
            ReleaseForPreview();
            Debug.Log("SHOOTER_CHECKS_PASSED: imported model/arm, wave rules, range, retreat/frost, aim lock, dodge, swept hits, cover, visibility, shields, stun, death, range, pause, caps, cleanup and mixed roster.");
        }
        finally {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
        capturePending=true; ready=EditorApplication.timeSinceStartup+.5;
    }
    static void ReleaseForPreview()
    {
        var enemy=Fresh(); Release(enemy); Call("UpdateEnemyNeedles",.24f);
        enemy.GetComponent<EnemyAvatar>().ShootPose(1,(Vector3)Field(Needles[0],"Direction"));
        PositionPlayer(new Vector3(-1.6f,.5f,1.2f));
    }
    static void CapturePreview()
    {
        Camera.orthographic=false; Camera.fieldOfView=44;
        Vector3 at=new Vector3(5,6,-1.5f);
        Camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(new Vector3(0,.4f,4.5f)-at));
        var target=new RenderTexture(1200,800,24); var previous=RenderTexture.active; var original=Camera.targetTexture;
        Camera.targetTexture=target; Camera.Render(); RenderTexture.active=target;
        var picture=new Texture2D(1200,800,TextureFormat.RGB24,false);
        picture.ReadPixels(new Rect(0,0,1200,800),0,0); picture.Apply();
        System.IO.File.WriteAllBytes("ArtSource/Enemies/Shooter-InGame.png",picture.EncodeToPNG());
        Camera.targetTexture=original; RenderTexture.active=previous;
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
        EditorApplication.update-=Tick; SessionState.SetBool("ShooterChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
