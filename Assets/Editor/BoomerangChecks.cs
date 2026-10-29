using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BoomerangChecks
{
    static SurvivorPlayground game;
    static object originalConfiguration;
    static WaveConfiguration configuration;
    static double next;
    static int stage;
    static bool originalAsync;
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
    static int Phase=>(int)Get("boomerangPhase");
    static Vector3 Position=>(Vector3)Get("boomerangPosition");
    static RunEquipment Equipment=>game.Run.Equipment;
    static PlayerController Player=>(PlayerController)Get("player");
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static Dictionary<Transform,int> Health=>(Dictionary<Transform,int>)Get("enemyHealth");
    static List<Bounds> Blocks=>(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"));
    static Transform Visual=>((Transform[])Get("newWeaponVisuals"))[9];
    static void Check(bool ok,string message) { if(!ok) throw new Exception("BOOMERANG_CHECK: "+message); }
    static void MovePlayer(Vector3 position)
    {
        var body=Player.GetComponent<CharacterController>(); body.enabled=false;
        Player.transform.SetPositionAndRotation(position,Quaternion.identity); body.enabled=true; Physics.SyncTransforms();
    }
    static void Fresh()
    {
        game.NewGame(); game.BeginRun(9); game.enabled=false; Player.enabled=false;
        Call("ClearCombatObjects"); Blocks.Clear(); MovePlayer(new Vector3(0,.5f,0));
        Call("UpdateNewWeapons",.001f);
    }
    static Transform Target(float x,float z,int health=1000)
    {
        Call("SpawnEnemy"); Transform enemy=Enemies[Enemies.Count-1]; enemy.position=new Vector3(x,.375f,z); Health[enemy]=health;
        enemy.GetComponent<EnemyAvatar>()?.Face(Vector3.back); return enemy;
    }
    static void Step(float dt)=>Call("UpdateNewWeapons",dt);
    static void UntilReturn() { for(int i=0;i<100 && Phase!=2;i++) Step(.01f); Check(Phase==2,"Outbound reaches its range and turns back"); }
    static void UntilCaught() { for(int i=0;i<350 && Phase!=0;i++) Step(.01f); Check(Phase==0,"Returning blade is always recoverable"); }
    static bool OneHit(int damage)=>damage==14 || damage==28;

    [MenuItem("Tools/Survivors/Verify Boomerang Blade")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("BoomerangChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BoomerangChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Verify()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        originalConfiguration=Get("waveConfiguration"); configuration=ScriptableObject.CreateInstance<WaveConfiguration>();
        configuration.Rules.TestMode=EnemyTestMode.NormalOnly; GameTestAccess.Set(game,"waveConfiguration",configuration);
        originalAsync=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
        Fresh();
        Check(Visual!=null && Visual.gameObject.activeSelf && Phase==0,"Starter has one visible held blade without targets");
        Check(Visual.GetComponentsInChildren<MeshRenderer>().Length==7,"Approved model contains two wings, two collars, grip and two wraps");
        Check(Equipment.HasMelee && !Equipment.HasAmmo && WeaponCatalog.ShopId(9)==28 && WeaponCatalog.WeaponId(28)==9,"Catalog, melee tags and stable shop ID");
        Transform near=Target(.65f,3),far=Target(.65f,5),off=Target(3,4);
        UntilReturn(); int nearOut=Health[near],farOut=Health[far];
        Check(OneHit(1000-nearOut) && OneHit(1000-farOut) && Health[off]==1000,"Pierces lined-up enemies once on outbound pass only");
        Check(Mathf.Abs(Position.z-7)<.02f,"Outbound distance is seven metres");
        UntilCaught();
        Check(OneHit(nearOut-Health[near]) && OneHit(farOut-Health[far]) && Health[off]==1000,"Return adds exactly one hit per enemy, with no per-frame repeat");
        int hp=Health[near]; Step(.5f); Check(Phase==0 && Health[near]==hp,"Cooldown begins after catch, preventing repeated launches");
        Step(.65f); Check(Phase==1,"Automatically throws again after catch cooldown");

        Fresh(); near=Target(.65f,3); UntilReturn();
        // Player dash/strafe changes the live catch point enough to hit a different lane.
        MovePlayer(new Vector3(3,.5f,0)); Transform returning=Target(2.9f,2);
        UntilCaught(); Check(Health[returning]<1000,"Moving the player bends return into a new enemy lane");
        Check(Vector3.Distance(Visual.position,Player.transform.position+Vector3.right*.65f+Vector3.up*.3f)<.01f,"Catch follows current player hand, not throw origin");

        Fresh(); near=Target(.65f,3); Step(.2f); int before=Health[near]; Vector3 at=Position;
        Step(0); Step(float.NaN); Check(Position==at && Health[near]==before,"Zero or invalid delta cannot advance or damage");
        UntilReturn(); UntilCaught();
        Fresh(); near=Target(.65f,3); far=Target(.65f,5); Step(1.2f);
        Check(Phase==0 && (1000-Health[near])%14==0 && Health[near]<=972 && Health[near]>=944 && Health[far]<=972,"Long frame sweeps both legs without tunneling or extra hits");

        Fresh(); near=Target(.65f,2); far=Target(.65f,5);
        Blocks.Add(new Bounds(new Vector3(.65f,0,4),new Vector3(5,4,.15f)));
        UntilReturn(); Check(Position.z<3.6f,"Obstacle turns the blade before its surface enters cover");
        UntilCaught(); Check(Health[far]==1000,"Blade cannot damage behind the obstacle");
        Fresh(); near=Target(.65f,3); UntilReturn(); before=Health[near];
        Blocks.Add(new Bounds(new Vector3(.65f,0,4),new Vector3(12,4,.1f)));
        UntilCaught(); Check(Health[near]==before,"Newly blocked return recalls without hitting through cover");
        Check(Visual.localScale==Vector3.one,"Recall restores full held scale");
        Fresh(); MovePlayer(new Vector3(0,.5f,14)); near=Target(.65f,16);
        UntilReturn(); Check(Position.z<17.56f,"Arena edge turns the blade safely"); UntilCaught();

        Fresh(); near=Target(.65f,3,14); Step(.3f);
        Check(!Health.ContainsKey(near),"Outbound can kill its target"); UntilReturn(); UntilCaught();
        Fresh(); near=Target(.65f,3); Step(.1f); Equipment.ChooseStarter(0); Equipment.ChooseStarter(1); Equipment.Collect(100);
        Check(Equipment.Buy(WeaponCatalog.ShopId(8),9),"Full-slot replacement can remove an in-flight boomerang");
        Step(.01f); Check(Phase==0 && !Visual.gameObject.activeSelf && ((HashSet<Transform>)Get("boomerangHit")).Count==0,"Unequip removes flight, visual and hit history");
        Fresh(); Target(.65f,3); Step(.1f); Call("ShowDefeat");
        Check(Phase==0 && !Visual.gameObject.activeSelf,"Defeat clears the flying weapon");
        Fresh(); Target(.65f,3); Step(.1f); Call("FinishWave");
        Check(Phase==0 && !Visual.gameObject.activeSelf,"Wave settlement clears the flying weapon");

        var equipment=new RunEquipment(); equipment.ChooseStarter(0); equipment.ChooseStarter(1); equipment.ChooseStarter(2); equipment.Collect(200);
        Check(equipment.Price(28)==20 && equipment.Buy(28,1) && equipment.HasWeapon(9) && !equipment.HasWeapon(1),"Twenty-material shop purchase replaces a full slot");
        for(int tier=1;tier<=3;tier++)
        {
            Check(equipment.Weapon(9).Damage(14)==14+(tier-1)*5,"Upgrade damage is 14/19/24");
            Check(Mathf.Abs(equipment.Weapon(9).Range(7)-(7+(tier-1)*.7f))<.001f,"Upgrade reach is 7/7.7/8.4 metres");
            if(tier<3) Check(equipment.Buy(28),"Purchased upgrade succeeds");
        }
        Check(!equipment.Buy(28),"Upgrades cap at tier three");
        var pool=new RunEquipment(); pool.ChooseStarter(9); var shop=new WaveShop(pool,new System.Random(42)); bool offered=false;
        for(int roll=0;roll<100;roll++) { shop.Open(1); for(int i=0;i<shop.Count;i++) offered|=shop.Item(i)==28; }
        Check(offered,"Shop offers boomerang upgrades");
        Fresh(); Equipment.Collect(100); Equipment.Buy(1); Check(Mathf.Abs((float)typeof(CombatController).GetMethod("NewRange",F).Invoke(game,new object[] {9})-8.05f)<.001f,"Melee reach modifier applies");
        Check(WeaponUpgradePreview.Describe(9,Equipment,true).Contains("回手后冷却"),"Upgrade preview describes catch cooldown");
        var uiType=typeof(CombatController).Assembly.GetType("GameUI");
        var navigate=uiType.GetMethod("NextStarter",BindingFlags.Static|BindingFlags.NonPublic);
        Check((int)navigate.Invoke(null,new object[] {5,Vector2.down})==9 && (int)navigate.Invoke(null,new object[] {9,Vector2.up})==5,"Controller reaches the tenth card vertically");
        Check((int)navigate.Invoke(null,new object[] {7,Vector2.down})==3,"Navigation skips empty last-row cells");
        Fresh(); bool launched=false;
        for(int i=0;i<800;i++) { Call("TickCombat",.02f); launched|=Phase!=0; if(game.Kills>0) break; }
        Check(launched && game.Kills>0,"Ordinary combat tick launches and kills with boomerang-only start");
        Debug.Log("BOOMERANG_CHECKS_PASSED: two-pass piercing, moving catch, finite/long-frame sweeps, walls/border/blocked recall, target death, cooldown, cleanup, shop/upgrades/melee tags, controller grid, ordinary combat.");
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0) { Verify(); Fresh(); game.enabled=true; Time.timeScale=0; }
            else if(stage==1) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Boomerang-Blade-Held.png");
            else if(stage==2) { Target(.65f,4); Target(.65f,5.5f); Target(2,6); Step(.16f); }
            else if(stage==3) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Boomerang-Blade-InGame.png");
            else if(stage==4) { UntilReturn(); MovePlayer(new Vector3(2,.5f,0)); Step(.14f); }
            else if(stage==5) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Boomerang-Blade-Return.png");
            else if(stage==6) { game.NewGame(); GameTestAccess.Set(game,"starterSelection",9); }
            else if(stage==7) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Boomerang-Blade-Starters.png");
            else { Finish(0); return; }
            stage++; next=EditorApplication.timeSinceStartup+.6;
        }
        catch(Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("BoomerangChecks",false);
        Time.timeScale=1;
        ShaderUtil.allowAsyncCompilation=originalAsync;
        if(configuration!=null) { if(game!=null) GameTestAccess.Set(game,"waveConfiguration",originalConfiguration); UnityEngine.Object.Destroy(configuration); }
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
