using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BaitLanternChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static SurvivorPlayground game;
    static double ready;
    static bool capturing;
    static WaveConfiguration profile;
    static object original;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static object Call(string name,params object[] args)=>typeof(CombatController).GetMethod(name,F).Invoke(game,args);
    static void Check(bool condition,string message) {if(!condition) throw new Exception("LANTERN: "+message);}
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static List<Bounds> Blocks=>(List<Bounds>)Get("obstacles").GetType().GetField("blocks",F).GetValue(Get("obstacles"));
    static Transform Visual=>((Transform[])Get("newWeaponVisuals"))[11];
    static void Fresh()
    {
        game.NewGame(); game.BeginRun(10); game.Run.Equipment.ChooseStarter(11); game.enabled=false;
        ((PlayerController)Get("player")).enabled=false; Call("ClearCombatObjects");
        ((PlayerController)Get("player")).transform.position=new Vector3(0,.5f,0);
        Blocks.Clear(); game.Run.Equipment.Collect(1000);
    }
    static Transform Enemy(Vector3 at)
    {
        Call("SpawnEnemy"); var enemy=Enemies[Enemies.Count-1]; enemy.position=at;
        ((Dictionary<Transform,int>)Get("enemyHealth"))[enemy]=1000;
        return enemy;
    }
    [MenuItem("Tools/Survivors/Verify Bait Lantern")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("BaitLanternChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("BaitLanternChecks",false)) return;
            capturing=false; ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        int code=0;
        try
        {
            if(capturing) {Capture(); Finish(0); return;}
            game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
            original=Get("waveConfiguration"); profile=PoisonBuildLab.CreateProfile(0); GameTestAccess.Set(game,"waveConfiguration",profile);
            WaveSessionChecks.Run();
            VerifyExplosions();
            var eq=new RunEquipment(); eq.Collect(1000); eq.ChooseStarter(10);
            Check(WeaponCatalog.ShopId(11)==31 && WeaponCatalog.WeaponId(30)==-1,"Valve ID preserved");
            for(int id=0;id<WeaponCatalog.Count;id++) Check(WeaponCatalog.WeaponId(WeaponCatalog.ShopId(id))==id && !WeaponCatalog.Passive(WeaponCatalog.ShopId(id)),"Catalog mapping");
            Check(eq.Price(31)==20 && eq.Buy(31) && eq.Price(31)==20 && eq.Buy(31) && eq.Price(31)==25 && eq.Buy(31) && !eq.Buy(31),"Purchase and three tiers");
            Check(eq.Weapon(11).Damage(24)==40 && eq.Weapon(11).Interval(8)==6 && !eq.HasCriticalWeapon,"Explosion upgrades without crits");
            Fresh(); Call("UpdateBaitLantern",.1f); Check(Visual==null || !Visual.gameObject.activeSelf,"No enemies, no placement");
            var enemy=Enemy(new Vector3(0,.375f,-4)); Call("UpdateBaitLantern",.3f);
            Check(Visual!=null && Visual.gameObject.activeSelf && Mathf.Abs(Visual.position.z+1.5f)<.01f,"Automatic drop behind movement");
            var originalVisual=Visual; var spot=Visual.position;
            Call("UpdateBaitLantern",.1f); Check(Visual==originalVisual && Visual.position==spot,"Single anchored lantern");
            float before=Vector3.Distance(enemy.position,spot);
            Check((bool)Call("MoveToBaitLantern",enemy,enemy.GetComponent<EnemyAvatar>(),2f,.2f) && Vector3.Distance(enemy.position,spot)<before,"Pursuer redirects");
            ((HashSet<Transform>)Get("stoneArmors")).Add(enemy);
            Check(!(bool)Call("LanternCanAttract",enemy,spot),"Armored elite immune");
            ((HashSet<Transform>)Get("stoneArmors")).Remove(enemy);
            Blocks.Add(new Bounds(new Vector3(0,0,-2.6f),new Vector3(4,20,.3f)));
            Check(!(bool)Call("LanternCanAttract",enemy,spot),"Cover blocks attraction"); Blocks.Clear();
            enemy.position=spot+new Vector3(0,.325f,-.75f);
            Call("MoveToBaitLantern",enemy,enemy.GetComponent<EnemyAvatar>(),2f,.1f);
            Check((int)Get("lanternHealth")==20,"Enemy damages lantern");
            Call("MoveToBaitLantern",enemy,enemy.GetComponent<EnemyAvatar>(),2f,.1f);
            Check((int)Get("lanternHealth")==20,"Attack rate limited");
            for(int i=0;i<2;i++) {var other=Enemy(spot+new Vector3(i*.1f,.325f,-.75f)); Call("MoveToBaitLantern",other,other.GetComponent<EnemyAvatar>(),2f,.1f);}
            Check(!Visual.gameObject.activeSelf && !(bool)Call("MoveToBaitLantern",enemy,enemy.GetComponent<EnemyAvatar>(),2f,.1f),"Break releases targets");
            Fresh(); Enemy(new Vector3(0,.375f,-4)); Blocks.Add(new Bounds(new Vector3(0,0,-1),new Vector3(4,20,.4f)));
            Call("UpdateBaitLantern",1f); Check(!Visual.gameObject.activeSelf,"Blocked placement waits"); Blocks.Clear();
            Call("UpdateBaitLantern",1f); Check(Visual.gameObject.activeSelf,"Placement retries");
            Call("UpdateBaitLantern",0f); Check((float)Get("lanternLife")==5,"Pause does not age lantern");
            Call("UpdateBaitLantern",5.1f); Check(!Visual.gameObject.activeSelf,"Lifetime expires");
            Call("UpdateBaitLantern",3f); Check(Visual.gameObject.activeSelf,"Cooldown permits redeployment");
            Call("ClearCombatObjects"); Check(!Visual.gameObject.activeSelf && (float)Get("lanternCooldown")==0,"Wave cleanup");
            Fresh(); enemy=Enemy(new Vector3(0,.375f,-4)); Call("UpdateBaitLantern",.1f);
            game.Run.Equipment.ChooseStarter(10); game.Run.Equipment.ChooseStarter(6);
            Check(game.Run.Equipment.Buy(WeaponCatalog.ShopId(0),11),"Replace lantern");
            Call("UpdateBaitLantern",.1f); Check(!Visual.gameObject.activeSelf,"Unequip cleans state");
            Fresh(); enemy=Enemy(new Vector3(0,.375f,-4)); Call("UpdateBaitLantern",.1f);
            spot=Visual.position; enemy.position=spot+new Vector3(0,.325f,-.75f);
            ((Dictionary<Transform,float>)Get("stunned"))[enemy]=Time.time+10;
            Call("MoveEnemies",.1f); Check((int)Get("lanternHealth")==30,"Stunned pursuer cannot attack");
            ((Dictionary<Transform,float>)Get("stunned")).Clear();
            Call("MoveEnemies",.1f); Check((int)Get("lanternHealth")==20,"Actual enemy loop attacks bait");
            Call("UpdateNewWeapons",.001f); Call("SpawnPoisonPool",spot,spot+Vector3.up);
            Call("UpdatePoisonPools",.2f); Call("UpdatePoisonDamage",.5f);
            Check(((Dictionary<Transform,int>)Get("enemyHealth"))[enemy]<1000,"Pursuer at bait takes poison damage");
            Call("ShowDefeat"); Check(!Visual.gameObject.activeSelf,"Defeat clears lantern");
            Fresh();
            var player=(PlayerController)Get("player"); player.transform.position=new Vector3(0,.5f,-17.8f);
            enemy=Enemy(new Vector3(0,.375f,-17)); Call("UpdateBaitLantern",1f);
            Check(!Visual.gameObject.activeSelf,"Outward drop near border waits");
            GameTestAccess.Set(game,"waveConfiguration",original); ItemSystemChecks.Run(game);
            GameTestAccess.Set(game,"waveConfiguration",profile);
            Fresh(); Enemy(new Vector3(0,.375f,-4)); Call("UpdateBaitLantern",.1f);
            Debug.Log("BAIT_LANTERN_CHECKS_PASSED: IDs, shop tiers, automatic placement, single instance, aggro, elite immunity, walls, health, attack cadence, lifetime, cooldown, pause, cleanup, replacement.");
            capturing=true; ready=EditorApplication.timeSinceStartup+.6; return;
        }
        catch(Exception error) {Debug.LogException(error); code=1;}
        Finish(code);
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("BaitLanternChecks",false);
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original);
        if(profile!=null) UnityEngine.Object.Destroy(profile);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
    static void VerifyExplosions()
    {
        profile.Rules.StartWave=1;
        game.NewGame(); game.BeginRun(11); game.enabled=false;
        Check(game.Run.Equipment.HasWeapon(11) && game.Run.Equipment.EquippedCount==1,"Lantern-only starter");
        ((PlayerController)Get("player")).enabled=false; Call("ClearCombatObjects"); Blocks.Clear();
        ((PlayerController)Get("player")).transform.position=new Vector3(0,.5f,0);
        Call("SpawnEnemy"); var first=Enemies[Enemies.Count-1]; first.position=new Vector3(0,.375f,-3);
        var hp=(Dictionary<Transform,int>)Get("enemyHealth");
        Check(hp[first]<=24,"First-wave natural enemy health fits blast");
        Call("UpdateBaitLantern",.1f);
        first.position=Visual.position+new Vector3(0,.325f,-.75f);
        int playerHP=game.Health.Current;
        GameTestAccess.Set(game,"lanternHealth",10);
        Call("MoveEnemies",.01f);
        Check(!hp.ContainsKey(first) && !Visual.gameObject.activeSelf,"Actual movement loop breaks lamp and kills first-wave enemy safely");
        Check(game.Health.Current==playerHP,"Blast never damages player");
        profile.Rules.StartWave=8;
        Fresh(); var target=Enemy(new Vector3(0,.375f,-3)); Call("UpdateBaitLantern",.1f);
        Vector3 center=Visual.position;
        var hidden=Enemy(center+Vector3.right*1.8f);
        var far=Enemy(center+Vector3.left*4);
        Blocks.Add(new Bounds(center+Vector3.right*.9f,new Vector3(.2f,20,1)));
        Call("UpdateBaitLantern",5.1f); Call("ResolveBaitLanternBlast");
        Check(hp[target]==976 && hp[hidden]==1000 && hp[far]==1000,"Expiry deals one blast, respecting cover and radius");
        Call("ResolveBaitLanternBlast"); Check(hp[target]==976,"Explosion cannot repeat");
        Fresh(); target=Enemy(new Vector3(0,.375f,-3));
        ((HashSet<Transform>)Get("stoneArmors")).Add(target);
        Call("UpdateBaitLantern",.1f);
        Check(Visual.gameObject.activeSelf && !(bool)Call("LanternCanAttract",target,Visual.position),"Elite-only encounter still deploys without taunting");
        Call("UpdateBaitLantern",5.1f); Call("ResolveBaitLanternBlast");
        Check(hp[target]<1000,"Elite takes blast damage");
        Fresh(); target=Enemy(new Vector3(0,.375f,-3)); Call("UpdateBaitLantern",.1f);
        Call("ClearBaitLantern"); Call("ResolveBaitLanternBlast"); Check(hp[target]==1000,"Cleanup never explodes");
        Call("UpdateBaitLantern",.1f); Call("UpdateBaitLantern",5.1f);
        Call("ClearBaitLantern"); Call("ResolveBaitLanternBlast"); Check(hp[target]==1000,"Cleanup cancels pending blast");
        for(int level=2;level<=3;level++)
        {
            Fresh(); for(int i=1;i<level;i++) game.Run.Equipment.Buy(31);
            target=Enemy(new Vector3(0,.375f,-3)); Call("UpdateBaitLantern",.1f);
            Call("UpdateBaitLantern",7f); Call("ResolveBaitLanternBlast");
            Check(hp[target]==1000-(24+(level-1)*8),"Upgraded explosion damage");
        }
        Debug.Log("LANTERN_EXPLOSION_CHECKS_PASSED: solo starter, natural wave-one kill, break/expiry, tiers, no self-damage, cover/radius, elite-only deployment, cleanup and single detonation.");
    }
    static void Capture()
    {
        var camera=Camera.main; var target=new RenderTexture(1000,800,24);
        var previous=RenderTexture.active; var oldTarget=camera.targetTexture;
        var picture=new Texture2D(1000,800,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position=Visual.position+new Vector3(3,3,-4); camera.transform.LookAt(Visual.position+Vector3.up*.6f);
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            picture.ReadPixels(new Rect(0,0,1000,800),0,0); picture.Apply();
            System.IO.Directory.CreateDirectory("Logs"); System.IO.File.WriteAllBytes("Logs/BaitLantern.png",picture.EncodeToPNG());
        }
        finally {camera.targetTexture=oldTarget; RenderTexture.active=previous; UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(picture);}
    }
}
