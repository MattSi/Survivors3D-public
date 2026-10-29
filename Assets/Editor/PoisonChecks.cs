using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonChecks
{
    static SurvivorPlayground game;
    static WaveConfiguration configuration;
    static object originalConfiguration;
    static int stage;
    static double next;
    static bool originalAsync;
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)
    {
        var method=typeof(CombatController).GetMethod(name,F);
        if(method!=null) method.Invoke(game,args); else GameTestAccess.Call(game,name,args);
    }
    static PlayerController Player=>(PlayerController)Get("player");
    static RunEquipment Equipment=>game.Run.Equipment;
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static Dictionary<Transform,int> Health=>(Dictionary<Transform,int>)Get("enemyHealth");
    static IList Pools=>(IList)Get("poisonPools");
    static IDictionary Poisoned=>(IDictionary)Get("poisonedEnemies");
    static List<Bounds> Blocks=>(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"));
    static T Field<T>(object item,string field)=>(T)item.GetType().GetField(field).GetValue(item);
    static int Active { get { int n=0; foreach(var p in Pools) if(Field<bool>(p,"Active")) n++; return n; } }
    static void Check(bool ok,string message) { if(!ok) throw new Exception("POISON_CHECK: "+message); }
    static void Position(Vector3 position)
    {
        var body=Player.GetComponent<CharacterController>(); body.enabled=false;
        Player.transform.SetPositionAndRotation(position,Quaternion.identity); body.enabled=true; Physics.SyncTransforms();
    }
    static void Move(Vector3 direction,float dt,bool dash=false)
    {
        Player.enabled=true;
        typeof(PlayerController).GetMethod("OnApplicationFocus",F).Invoke(Player,new object[] {true});
        typeof(PlayerController).GetMethod("TickMovement",F).Invoke(Player,new object[] {direction,dash,dt});
        Player.enabled=false;
    }
    static void Fresh()
    {
        Time.timeScale=1; game.NewGame(); game.BeginRun(10); game.enabled=false; Player.enabled=false;
        Call("ClearCombatObjects"); Blocks.Clear(); Position(new Vector3(0,.5f,0));
        Call("UpdateNewWeapons",.001f);
    }
    static Transform Target(Vector3 at,int hp=100)
    {
        Call("SpawnEnemy"); Transform enemy=Enemies[Enemies.Count-1]; enemy.position=at; Health[enemy]=hp;
        enemy.GetComponent<EnemyAvatar>()?.Face(Vector3.back); return enemy;
    }
    static void Pool(Vector3 at)
    {
        at.y=.025f; Call("SpawnPoisonPool",at,at+Vector3.up);
        foreach(var pool in Pools) if(Field<bool>(pool,"Active")) pool.GetType().GetField("Age").SetValue(pool,0f);
    }
    static void Fields(float dt)
    {
        while(dt>.00001f) {float step=Mathf.Min(dt,.01f); dt-=step; Call("UpdatePoisonPools",step); Call("UpdatePoisonDamage",step);}
    }
    [MenuItem("Tools/Survivors/Verify Poison Backpack")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("PoisonChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PoisonChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Verify()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        originalConfiguration=Get("waveConfiguration"); configuration=ScriptableObject.CreateInstance<WaveConfiguration>();
        configuration.Rules.TestMode=EnemyTestMode.NormalOnly; GameTestAccess.Set(game,"waveConfiguration",configuration);
        originalAsync=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
        Check(WeaponCatalog.Description.Length==WeaponCatalog.Count && WeaponCatalog.EnglishDescription.Length==WeaponCatalog.Count
            && WeaponCatalog.Description[10].StartsWith("自动向最近移动方向"),"Localized weapon descriptions retain catalog ordering");
        Fresh(); Transform visual=((Transform[])Get("newWeaponVisuals"))[10];
        Check(visual!=null && visual.gameObject.activeSelf && visual.GetComponentsInChildren<MeshRenderer>().Length>=10,"Starter displays a wood/iron backpack with window and nozzle");
        Check(!Equipment.HasAmmo && !Equipment.HasMelee && !Equipment.HasCriticalWeapon,"Poison-only loadout has no ammo, melee or crit item eligibility");
        Call("UpdateNewWeapons",.77f); Check(Active==0,"No spray before the 0.8-second cadence");
        Call("UpdateNewWeapons",.04f); Check(Active==1,"Automatic spray requires no target");
        object first=null; foreach(var p in Pools) if(Field<bool>(p,"Active")) first=p;
        Check(Field<Vector3>(first,"Center").z<-1 && Field<float>(first,"Age")<0,"First spray lands behind default movement, after a flight delay");
        Fresh(); Move(Vector3.right,.1f); Call("UpdateNewWeapons",.81f);
        foreach(var p in Pools) if(Field<bool>(p,"Active")) Check(Field<Vector3>(p,"Center").x<Player.transform.position.x-1,"Real movement sets rear spray direction");
        Vector3 direction=Player.LastTravelDirection;
        var camera=(PlaygroundCamera)Get("viewCamera"); camera.ThirdPerson=true;
        typeof(PlaygroundCamera).GetField("<Yaw>k__BackingField",F).SetValue(camera,180f);
        Check(Player.LastTravelDirection==direction,"Camera rotation alone cannot rotate remembered movement");
        Move(Vector3.zero,.01f); Check(Player.LastTravelDirection==direction,"Standing still preserves spray direction");
        Fresh(); Move(Vector3.right,.15f,true); Call("UpdateNewWeapons",.15f);
        Check(Active==0 && Player.transform.position.x>2.4f && Player.LastTravelDirection.x>.99f,"Dash moves the backpack without multiplying spray count");
        Call("UpdateNewWeapons",.66f); Check(Active==1,"Dash retains ordinary 0.8-second cadence");

        Fresh(); Transform a=Target(new Vector3(0,.375f,-1)),b=Target(new Vector3(.3f,.375f,-1));
        Pool(new Vector3(0,0,-1)); Pool(new Vector3(0,0,-1)); Pool(new Vector3(0,0,-1));
        Fields(.49f); Check(Health[a]==100 && Health[b]==100,"Poison has no instant impact damage");
        Fields(.01f); Check(Health[a]==97 && Health[b]==97,"Overlapping pools tick each enemy only once for three damage");
        Fields(.5f); Check(Health[a]==94 && Health[b]==94,"Sustained refresh never postpones the shared damage timer");
        a.position=new Vector3(5,.375f,5); Fields(1f);
        Check(Health[a]==88 && !Poisoned.Contains(a),"Leaving a pool gives exactly two lingering ticks in one second");
        Fields(1f); Check(Health[a]==88 && !Poisoned.Contains(a),"Expired poison stops damaging");
        Fields(2f); Check(Active==0 && Poisoned.Count==0,"Pools and residual poison expire completely");
        Check(game.Health.Current==100,"Own poison never damages the player");

        Fresh(); a=Target(new Vector3(0,.375f,-1)); Pool(new Vector3(0,0,-1)); Fields(.3f);
        a.position=new Vector3(5,.375f,5); Fields(.1f); a.position=new Vector3(0,.375f,-1); Fields(.1f);
        Check(Health[a]==97,"Re-entering refreshes duration without duplicating or resetting ticks");
        var avatar=a.GetComponent<EnemyAvatar>();
        avatar.Chill(Time.time+2); avatar.FlashHit();
        var bodies=(Renderer[])typeof(EnemyAvatar).GetField("bodies",F).GetValue(avatar); var tint=new MaterialPropertyBlock(); bodies[0].GetPropertyBlock(tint);
        Check(tint.GetColor("_BaseColor")==Color.white,"Hit flash remains white while poisoned and frosted");
        Call("ClearPoison"); Check(!(bool)typeof(EnemyAvatar).GetField("poisoned",F).GetValue(avatar),"Cleanup restores poison tint independently of frost");

        Fresh(); a=Target(new Vector3(0,.375f,-1)); Blocks.Add(new Bounds(new Vector3(0,0,-.45f),new Vector3(5,4,.1f)));
        Pool(Vector3.zero); Fields(1f); Check(Health[a]==100,"Pool cannot poison through a thin wall");
        foreach(var p in Pools) if(Field<bool>(p,"Active")) foreach(Vector3 v in Field<Mesh>(p,"Mesh").vertices)
            Check(v.z>-.41f,"Pool geometry clips at cover");
        Fresh(); Blocks.Add(new Bounds(new Vector3(0,0,-.7f),new Vector3(5,4,.1f))); Call("UpdateNewWeapons",1.1f);
        foreach(var p in Pools) if(Field<bool>(p,"Active")) Check(Field<Vector3>(p,"Center").z>-.66f,"Spray cannot land behind cover");

        for(int tier=1;tier<=3;tier++)
        {
            Fresh(); for(int level=1;level<tier;level++) Equipment.Weapon(10).Upgrade();
            a=Target(new Vector3(0,.375f,-1)); Pool(new Vector3(0,0,-1)); Fields(.5f);
            Check(Health[a]==100-(tier+2),"Actual poison ticks use tier damage 3/4/5");
            foreach(var p in Pools) if(Field<bool>(p,"Active")) Check(Mathf.Abs(Field<float>(p,"Radius")-(.8f+(tier-1)*.15f))<.001f && Field<float>(p,"Duration")==3+(tier-1)*.5f,"Tier radius and lifetime match upgrade preview");
        }
        Fresh(); Equipment.Collect(1000); for(int i=0;i<3;i++) Equipment.Buy(6+(int)RunItem.Whetstone);
        a=Target(new Vector3(0,.375f,-1),1000); Pool(new Vector3(0,0,-1)); Fields(2);
        Check(Health[a]==988,"Poison cannot crit even with stacked whetstones");
        Fresh(); a=Target(new Vector3(0,.375f,-1),3); Pool(new Vector3(0,0,-1)); Fields(.5f);
        Check(!Health.ContainsKey(a) && Poisoned.Count==0 && game.Kills==1,"Poison kill awards score and removes status immediately");
        Fresh(); Call("UpdateNewWeapons",8f); Check(Active<=6 && Pools.Count<=8,"Long frames preserve cadence and bound reusable pool count");
        Call("ShowDefeat"); Check(Active==0 && Poisoned.Count==0 && !visual.gameObject.activeSelf,"Defeat clears pool/status/backpack");
        Fresh(); a=Target(new Vector3(0,.375f,-1)); Pool(new Vector3(0,0,-1)); Fields(.1f); Call("FinishWave");
        Check(Active==0 && Poisoned.Count==0,"Settlement clears all poison");
        Fresh(); Pool(new Vector3(0,0,-1)); Equipment.ChooseStarter(0); Equipment.ChooseStarter(1); Equipment.Collect(100);
        Check(Equipment.Buy(WeaponCatalog.ShopId(8),10),"Poison backpack can be replaced in a full loadout"); Call("UpdateNewWeapons",.01f);
        Check(Active==0 && Poisoned.Count==0 && !visual.gameObject.activeSelf,"Unequipping removes leftover pools");
        Fresh(); Call("UpdateNewWeapons",.81f); first=null; foreach(var p in Pools) if(Field<bool>(p,"Active")) first=p;
        float age=Field<float>(first,"Age"); Call("TogglePause"); Call("Tick",.5f);
        Check(Field<float>(first,"Age")==age,"Pause freezes spray and poison clocks"); Call("ResumeGame");
        var equipment=new RunEquipment(); equipment.ChooseStarter(0); equipment.ChooseStarter(1); equipment.ChooseStarter(2); equipment.Collect(200);
        Check(WeaponCatalog.ShopId(10)==29 && equipment.Price(29)==20 && equipment.Buy(29,2),"Shop acquisition costs twenty materials and replaces one slot");
        Check(equipment.Buy(29) && equipment.Buy(29) && !equipment.Buy(29),"Shop upgrades cap at tier three");
        var uiType=typeof(CombatController).Assembly.GetType("GameUI");
        Check((int)uiType.GetMethod("NextStarter",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[] {6,Vector2.down})==10,"Controller reaches the eleventh starter");
        Fresh(); Target(new Vector3(0,.375f,-1),9); GameTestAccess.Set(game,"nextSpawn",100f);
        for(int i=0;i<400 && game.Kills==0;i++) { Move(Vector3.forward,.02f); Call("TickCombat",.02f); }
        Check(game.Kills>0,"Ordinary combat automatically sprays and kills pursuing enemies while the player kites");
        Debug.Log("POISON_CHECKS_PASSED: actual movement/dash cadence, overlap, linger/reentry, tier damage, no crit/self-hit, cover, tint, death/settlement/replacement/pause, shop and ordinary combat.");
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0)
            {
                Verify(); Fresh();
                ((PlaygroundCamera)Get("viewCamera")).ThirdPerson=false;
                game.enabled=true; Time.timeScale=0;
            }
            else if(stage==1) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Poison-Backpack-Held.png");
            else if(stage==2)
            {
                for(int i=0;i<4;i++) { Position(new Vector3(0,.5f,-3+i*1.1f)); Call("UpdateNewWeapons",.8f); }
                Target(new Vector3(.15f,.375f,-2.5f)); Target(new Vector3(-.2f,.375f,-3.3f));
                Call("UpdateNewWeapons",.25f);
            }
            else if(stage==3) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Poison-Backpack-InGame.png");
            else if(stage==4) { game.NewGame(); GameTestAccess.Set(game,"starterSelection",10); }
            else if(stage==5) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Poison-Backpack-Starters.png");
            else { Finish(0); return; }
            stage++; next=EditorApplication.timeSinceStartup+.7;
        }
        catch(Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("PoisonChecks",false); Time.timeScale=1;
        ShaderUtil.allowAsyncCompilation=originalAsync;
        if(configuration!=null) { if(game!=null) GameTestAccess.Set(game,"waveConfiguration",originalConfiguration); UnityEngine.Object.Destroy(configuration); }
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
