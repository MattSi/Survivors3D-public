using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonBuildChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static SurvivorPlayground game;
    static WaveConfiguration profile;
    static object original;
    static double ready;
    static int stage;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)
    {
        var method=typeof(CombatController).GetMethod(name,F);
        if(method!=null) method.Invoke(game,args); else GameTestAccess.Call(game,name,args);
    }
    static IList Pools=>(IList)Get("poisonPools");
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static Dictionary<Transform,int> HP=>(Dictionary<Transform,int>)Get("enemyHealth");
    static RunEquipment Eq=>game.Run.Equipment;
    static int Valve=>ItemLibrary.ShopId((int)RunItem.SlowReleaseValve);
    static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name).GetValue(value);
    static int Active {get {int n=0; foreach(var p in Pools) if(Field<bool>(p,"Active")) n++; return n;}}
    static void Check(bool value,string message) {if(!value) throw new Exception("POISON_BUILD_CHECK: "+message);}
    [MenuItem("Tools/Survivors/Verify Poison Build")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("PoisonBuildChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PoisonBuildChecks",false)) return;
            stage=0; ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Fresh(bool valve=true,bool frost=false)
    {
        game.NewGame(); game.BeginRun(10); game.enabled=false;
        ((PlayerController)Get("player")).enabled=false; Call("ClearCombatObjects");
        var obstacles=Get("obstacles"); ((List<Bounds>)obstacles.GetType().GetField("blocks",F).GetValue(obstacles)).Clear();
        Eq.Collect(10000); if(valve) Check(Eq.Buy(Valve),"Buy valve"); if(frost) Eq.ChooseStarter(6);
        Call("UpdateNewWeapons",.001f);
    }
    static Transform Target(Vector3 position,int hp=1000)
    {Call("SpawnEnemy"); var e=Enemies[Enemies.Count-1]; e.position=position; HP[e]=hp; return e;}
    static void Pool(Vector3 at)
    {
        at.y=.025f; Call("SpawnPoisonPool",at,at+Vector3.up);
        foreach(var p in Pools) if(Field<bool>(p,"Active")) p.GetType().GetField("Age").SetValue(p,0f);
    }
    static void Fields(float duration)
    {while(duration>.00001f) {float dt=Mathf.Min(.01f,duration); duration-=dt; Call("UpdatePoisonPools",dt); Call("UpdatePoisonDamage",dt);}}
    static bool Eligible(WaveShop shop)=> (bool)typeof(WaveShop).GetMethod("IsEligible",F).Invoke(shop,new object[]{Valve});
    static void Verify()
    {
        Check(Valve==30 && WeaponCatalog.ShopId(10)==29 && WeaponCatalog.WeaponId(Valve)==-1,"Existing weapon IDs are preserved");
        for(int i=0;i<ItemLibrary.Count;i++) Check(ItemLibrary.Index(ItemLibrary.ShopId(i))==i && WeaponCatalog.Passive(ItemLibrary.ShopId(i)),"All passive IDs round-trip");
        for(int i=0;i<WeaponCatalog.Count;i++) Check(WeaponCatalog.WeaponId(WeaponCatalog.ShopId(i))==i && !WeaponCatalog.Passive(WeaponCatalog.ShopId(i)),"Weapons cannot alias passives");
        var eq=new RunEquipment(); eq.ChooseStarter(0); eq.Collect(10000); var shop=new WaveShop(eq,new System.Random(4));
        shop.Open(8); Check(!Eligible(shop),"Valve is filtered without poison"); eq.ChooseStarter(10);
        shop.Open(3); Check(!Eligible(shop),"Valve is locked through wave three"); shop.Open(4); Check(Eligible(shop),"Valve unlocks after wave four");
        int slot=-1;
        for(int attempt=0;attempt<300 && slot<0;attempt++) {for(int i=0;i<shop.Count;i++) if(shop.Item(i)==Valve) slot=i; if(slot<0) shop.Refresh();}
        Check(slot>=0,"Valve appears in actual stock"); shop.ToggleLock(slot); shop.Refresh(); Check(shop.Item(slot)==Valve && shop.Price(slot)==30,"Lock and refresh preserve valve offer");
        int balance=eq.Materials; Check(shop.Buy(slot) && eq.Materials==balance-30 && !eq.Buy(Valve),"Actual purchase and one-copy cap");
        eq.ChooseStarter(1); Check(eq.Buy(WeaponCatalog.ShopId(8),10) && !Eligible(shop),"Replacing poison removes eligibility");
        Check(eq.Stacks(RunItem.SlowReleaseValve)==1 && eq.Buy(WeaponCatalog.ShopId(10),8) && Math.Abs(eq.PoisonSprayInterval-1.0f)<.001f,"Reacquiring poison restores retained valve");
        eq.Reset(); Check(eq.Stacks(RunItem.SlowReleaseValve)==0,"New run clears valve");
        for(int group=0;group<4;group++) {eq=new RunEquipment(); eq.ChooseStarter(10); int cost=PoisonBuildLab.Equip(eq,group); Check(cost==new[]{20,40,70,70}[group] && eq.Materials==100-cost,"Build prices and equal-spend comparison");}
        Check(ItemIcons.Get(16)!=ItemIcons.Get(15),"Valve has its own cached icon");

        Fresh(); Call("UpdateNewWeapons",.8f); Check(Active==0,"Valve does not spray at old cadence");
        Call("UpdateNewWeapons",.2f); Check(Active==1,"Valve sprays at 1.0 seconds");
        foreach(var p in Pools) if(Field<bool>(p,"Active")) Check(Field<float>(p,"Duration")==6,"Level one valve pool lasts six seconds");
        Check(((Transform)Get("poisonValve")).gameObject.activeSelf,"Equipped valve is visible on tank");
        Fresh(false); Call("UpdateNewWeapons",.8f); Check(Active==1,"Base spray cadence is retained");
        Check(!((Transform)Get("poisonValve")).gameObject.activeSelf,"No valve model without the item");
        Fresh(); Pool(new Vector3(0,0,-1)); Fields(3.5f); Check(Active==1,"Old route survives beyond baseline duration"); Fields(2.6f); Check(Active==0,"Valve pool still expires");
        Fresh(); Eq.Buy(WeaponCatalog.ShopId(10)); Eq.Buy(WeaponCatalog.ShopId(10));
        Check(Eq.PoisonPoolDuration==7,"Tier three adds to valve duration");
        Call("UpdateNewWeapons",20f); Check(Pools.Count<=8 && Active<=8,"Long-frame spray remains bounded without recycling live pools early");
        Fresh(); var target=Target(new Vector3(0,.375f,-1)); Pool(target.position); Pool(target.position); Pool(target.position);
        Fields(1); Check(HP[target]==994 && game.Metrics.For(CombatMetrics.Source.Poison).Damage==6,"Valve and overlapping pools do not amplify tick damage");
        target.position=new Vector3(8,.375f,8); Fields(1); Check(HP[target]==988,"Leaving still gives one second of lingering poison");
        Fresh(true,true); target=Target(new Vector3(0,.375f,0)); Pool(target.position); Call("UpdateNewWeapons",.36f); Fields(.5f);
        Check(game.Metrics.For(CombatMetrics.Source.Frost).Damage>0 && game.Metrics.PoisonDamageWhileSlowed>0,"Frost and poison retain separate damage and report overlap");
        float clock=(float)Get("poisonSprayClock"); Call("TogglePause"); Call("Tick",1f);
        Check((float)Get("poisonSprayClock")==clock,"Pause freezes valve spray clock"); Call("ResumeGame");
        Call("ShowDefeat"); Check(Active==0 && ((IDictionary)Get("poisonedEnemies")).Count==0,"Defeat removes extended pools and poison");
        Fresh(); Pool(Vector3.zero); Call("FinishWave"); Check(Active==0,"Settlement removes extended pools");
        Fresh(); Pool(Vector3.zero); Eq.ChooseStarter(0); Eq.ChooseStarter(1); Eq.Buy(WeaponCatalog.ShopId(8),10); Call("UpdateNewWeapons",.01f);
        Check(Active==0,"Weapon replacement clears extended pools");
        WaveSessionChecks.Run();
        Debug.Log("POISON_BUILD_CHECKS_PASSED: stable IDs, shop filters/lock/purchase/replacement, recipes, valve model, cadence/lifetime/cap, overlap/linger, frost metrics, pause/death/settlement and run rules.");
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        try
        {
            if(stage==0)
            {
                game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); original=Get("waveConfiguration");
                profile=PoisonBuildLab.CreateProfile(0); GameTestAccess.Set(game,"waveConfiguration",profile); Verify();
                GameTestAccess.Set(game,"waveConfiguration",original); ItemSystemChecks.Run(game);
                GameTestAccess.Set(game,"waveConfiguration",profile);
                Fresh(true,true); Pool(new Vector3(0,0,-1)); Pool(new Vector3(1,0,-2)); Pool(new Vector3(-1,0,-3));
                Target(new Vector3(.2f,.375f,-1)); Call("UpdateNewWeapons",.4f);
                foreach(var e in Enemies) {var avatar=e.GetComponent<EnemyAvatar>(); typeof(EnemyAvatar).GetField("emergeStarted",F).SetValue(avatar,Time.time-2); typeof(EnemyAvatar).GetMethod("Update",F).Invoke(avatar,null);}
                Time.timeScale=0; stage=1; ready=EditorApplication.timeSinceStartup+.6; return;
            }
            Capture(); Finish(0);
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Capture()
    {
        System.IO.Directory.CreateDirectory("Logs"); var camera=Camera.main;
        var target=new RenderTexture(1200,800,24); var previous=RenderTexture.active; var originalTarget=camera.targetTexture;
        Texture2D picture=null;
        try
        {
            camera.transform.position=new Vector3(3,8,-9); camera.transform.LookAt(new Vector3(0,.5f,-1));
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            picture=new Texture2D(1200,800,TextureFormat.RGB24,false); picture.ReadPixels(new Rect(0,0,1200,800),0,0); picture.Apply();
            System.IO.File.WriteAllBytes("Logs/PoisonBuild-Field.png",picture.EncodeToPNG());
        }
        finally {camera.targetTexture=originalTarget; RenderTexture.active=previous; UnityEngine.Object.Destroy(target); if(picture!=null) UnityEngine.Object.Destroy(picture);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("PoisonBuildChecks",false); Time.timeScale=1;
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original); if(profile!=null) UnityEngine.Object.Destroy(profile);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
