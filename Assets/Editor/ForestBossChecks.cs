using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestBossChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static SurvivorPlayground game;
    static WaveConfiguration profile;
    static object original;
    static double ready;
    static int stage;
    static bool originalAsync;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Set(string name,object value)=>GameTestAccess.Set(game,name,value);
    static void Call(string name,params object[] args)
    {
        var method=typeof(CombatController).GetMethod(name,F);
        if(method!=null) method.Invoke(game,args); else GameTestAccess.Call(game,name,args);
    }
    static Transform Boss=>(Transform)Get("forestBoss");
    static Dictionary<Transform,int> HP=>(Dictionary<Transform,int>)Get("enemyHealth");
    static IList Needles=>(IList)Get("enemyNeedles");
    static List<Bounds> Blocks=>(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"));
    static void Check(bool value,string message) {if(!value) throw new Exception("FOREST_BOSS_CHECK: "+message);}
    static object TestBullet(int weapon,int damage,bool secondary=false)
    {
        var type=typeof(CombatController).GetNestedType("Bullet",BindingFlags.NonPublic|BindingFlags.Public);
        var shot=Activator.CreateInstance(type,true);
        type.GetField("Weapon").SetValue(shot,weapon); type.GetField("Damage").SetValue(shot,damage);
        type.GetField("Secondary").SetValue(shot,secondary); type.GetField("Critical").SetValue(shot,(bool?)false);
        type.GetField("Direction").SetValue(shot,Vector3.forward); return shot;
    }
    static void Phase(string name,float timer=100)=>SetPhase(name,timer);
    static void SetPhase(string name,float timer)
    {Set("forestBossPhase",Enum.Parse(Get("forestBossPhase").GetType(),name)); Set("bossTimer",timer);}
    static string PhaseName=>Get("forestBossPhase").ToString();

    [MenuItem("Tools/Survivors/Verify Forest Stone King")]
    public static void Run()=>Begin(false);
    [MenuItem("Tools/Survivors/Play Forest Stone King")]
    public static void Play()=>Begin(true);
    static void Begin(bool preview)
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        if(!Application.isBatchMode)
        {
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            view.Show(); view.Focus();
        }
        SessionState.SetInt("ForestBossChecks",preview?2:1); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || SessionState.GetInt("ForestBossChecks",0)==0) return;
            originalAsync=ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation=false;
            ready=EditorApplication.timeSinceStartup+2; stage=0; EditorApplication.update+=Tick;
        };
    }
    static void MovePlayer(Vector3 at)
    {
        var p=(PlayerController)Get("player"); var cc=p.GetComponent<CharacterController>();
        cc.enabled=false; p.transform.SetPositionAndRotation(at,Quaternion.identity); cc.enabled=true; p.enabled=false;
    }
    static void Fresh(int weapon=0)
    {
        game.NewGame(); game.BeginRun(weapon); game.enabled=false;
        MovePlayer(new Vector3(0,.5f,0)); Boss.position=new Vector3(0,.375f,2.3f);
        Boss.GetComponent<EnemyAvatar>().Face(Vector3.back); Phase("Recover");
        // Fixtures start after the entrance animation, including frozen-frame screenshots.
        var avatar=Boss.GetComponent<EnemyAvatar>();
        typeof(EnemyAvatar).GetField("emergeStarted",F).SetValue(avatar,Time.time-2f);
        typeof(EnemyAvatar).GetMethod("Update",F).Invoke(avatar,null);
        avatar.ChargePose(0,false);
        var camera=(PlaygroundCamera)Get("viewCamera"); camera.enabled=false;
        camera.GetComponent<Camera>().orthographic=true; camera.GetComponent<Camera>().orthographicSize=10;
        camera.transform.SetPositionAndRotation(new Vector3(6,13,-11),Quaternion.LookRotation(new Vector3(0,.7f,2)-new Vector3(6,13,-11)));
        Physics.SyncTransforms();
    }
    static void Prepare()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); original=Get("waveConfiguration");
        profile=ScriptableObject.CreateInstance<WaveConfiguration>();
        profile.Rules.StartWave=20; profile.Rules.FinalBoss=true; profile.Rules.TestMode=EnemyTestMode.Mixed;
        Set("waveConfiguration",profile);
    }
    static void VerifyRules()
    {
        WaveRulesChecks.Run();
        var session=new WaveSession(new WaveRules {StartWave=20});
        Check(session.IsBossWave && !session.Advance(10000) && session.State==WaveSession.Phase.Combat,"Final boss cannot be won by waiting");
        Check(session.CompleteBoss() && !session.CompleteBoss() && !session.Continue(),"Boss victory resolves once");
        session.Reset(); session.Defeat(); Check(!session.CompleteBoss(),"Dead player cannot receive boss victory");
        session=new WaveSession(new WaveRules {StartWave=20,Endless=true});
        Check(!session.IsBossWave && session.Advance(1000) && session.Continue(),"Endless wave twenty stays timed");
        session=new WaveSession(new WaveRules {StartWave=20,TestMode=EnemyTestMode.ShooterOnly});
        Check(!session.IsBossWave && session.Advance(1000),"Single-type test modes retain their enemy and deadline");
        session=new WaveSession(new WaveRules {FinalWave=3,StartWave=3});
        Check(session.IsBossWave,"Custom finite final wave supports the boss");
        session=new WaveSession(new WaveRules {StartWave=20,FinalBoss=false});
        Check(!session.IsBossWave && session.Advance(1000),"Configuration can retain a timed finale");
    }
    static void Verify()
    {
        VerifyRules(); Fresh();
        Check(Boss!=null && game.EnemyCount==1 && HP[Boss]==3600,"Exactly one boss with configured health");
        Check(Resources.Load<GameObject>("Characters/ForestStoneKing")!=null,"Boss FBX imports");
        var avatar=Boss.GetComponent<EnemyAvatar>(); Transform arm=null,fist=null;
        foreach(var part in Boss.GetComponentsInChildren<Transform>())
        { if(part.name=="BossLeftArm") arm=part; if(part.name=="KingFist" && fist==null) fist=part; }
        Check(arm!=null && fist!=null,"Articulated arms and fists survive export");
        float restY=fist.position.y; avatar.BossPose(1,1);
        Check(fist.position.y>restY+.4f,"Slam visibly raises the fists"); avatar.BossPose(0,0);
        var body=Boss.GetComponent<CapsuleCollider>(); Check(body!=null && body.enabled && Blocks.Count==0,"Boss has a solid body and open fighting space");
        Call("SpawnEnemy"); Check(game.EnemyCount==1,"Ordinary spawning is suppressed during boss combat");

        Fresh(); Set("bossAttackIndex",0); Call("BeginBossAttack",Vector3.back*2.3f);
        Check(PhaseName=="Slam" && Get("bossWarning")!=null,"Slam starts with a visible locked ring");
        float timer=(float)Get("bossTimer"); Call("TickForestBoss",0f); Call("TickForestBoss",float.NaN);
        Check((float)Get("bossTimer")==timer && game.Health.Current==100,"Pause and invalid ticks cannot advance attacks");
        Call("TickForestBoss",.5f); Check(game.Health.Current==100,"Slam windup cannot damage");
        Call("TickForestBoss",1f); Check(game.Health.Current==68 && PhaseName=="Recover","Slam strikes once and opens recovery");
        Call("TickForestBoss",.1f); Check(game.Health.Current==68,"Recovery cannot repeatedly damage");
        float before=(float)Get("bossTimer"); Call("StunEnemy",Boss,1f); Call("StunEnemy",Boss,1f);
        Check(Mathf.Abs((float)Get("bossTimer")-before-.4f)<.001f,"Hammer earns one bounded recovery stagger");
        Fresh(); Set("bossAttackIndex",0); Call("BeginBossAttack",Vector3.back*2.3f);
        MovePlayer(new Vector3(6,.5f,0)); Call("TickForestBoss",2f); Check(game.Health.Current==100,"Leaving the ring dodges the slam");

        Fresh(); Boss.position=new Vector3(0,.375f,6); Set("bossAttackIndex",1); Call("BeginBossAttack",Vector3.back*6);
        Vector3 origin=Boss.position; Call("TickForestBoss",20f);
        Check(PhaseName=="Charge" && Boss.position==origin && game.Health.Current==100,"Long windup frame never also executes the charge");
        Call("TickForestBoss",2f); Check(game.Health.Current==74 && PhaseName=="Slam","Charge sweeps the entire segment and chains into slam");
        Check(Mathf.Abs((float)Get("bossTimer")-.6f)<.001f && Get("bossWarning")!=null,"Charge leaves a full 0.6 second landing warning even on a long frame");
        MovePlayer(new Vector3(5,.5f,0)); Call("TickForestBoss",.3f);
        Check(PhaseName=="Slam" && game.Health.Current==74,"Follow-up cannot strike before its warning expires");
        Call("TickForestBoss",.31f);
        Check(PhaseName=="Recover" && Mathf.Abs((float)Get("bossTimer")-2f)<.001f && game.Health.Current==74,"Dodged follow-up opens two seconds of recovery");
        Fresh(); Boss.position=new Vector3(0,.375f,6); Set("bossAttackIndex",1); Call("BeginBossAttack",Vector3.back*6);
        MovePlayer(new Vector3(4,.5f,0)); Call("TickForestBoss",2f); Call("TickForestBoss",2f);
        Check(game.Health.Current==100 && Mathf.Abs(Boss.position.x)<.001f,"Locked charge cannot track a sidestep");
        Fresh(); Boss.position=new Vector3(0,.375f,6); Blocks.Add(new Bounds(new Vector3(0,0,3),new Vector3(3,20,.3f)));
        Set("bossAttackIndex",1); Call("BeginBossAttack",Vector3.back*6); Call("TickForestBoss",2f); Call("TickForestBoss",2f);
        Check(Boss.position.z>4 && game.Health.Current==100,"Charge respects padded cover");

        Fresh(); Boss.position=new Vector3(0,.375f,6); Set("bossAttackIndex",2); Call("BeginBossAttack",Vector3.back*6);
        Check(Get("bossWarning")==null,"Fan has no ground aim lines; arms and core provide its windup cue");
        Call("TickForestBoss",.5f); Check(Needles.Count==0,"Fan waits for its warning");
        Call("TickForestBoss",1); Check(Needles.Count==0 && (bool)Get("bossVolleyLocked"),"Long fan windup preserves a final lock window");
        Call("TickForestBoss",.36f); Check(Needles.Count==5,"First phase emits five bone needles");
        Call("UpdateEnemyNeedles",2f); Check(game.Health.Current==88,"Fan needles use player damage and consumption");
        Fresh(); Boss.position=new Vector3(0,.375f,6); Set("bossAttackIndex",2); Call("BeginBossAttack",Vector3.back*6);
        Check(Get("bossVolleyMode").ToString()=="Fan","First volley is current-position fan");
        Set("bossAttackIndex",2); Set("bossPlayerVelocity",Vector3.right*5); Call("BeginBossAttack",Vector3.back*6);
        Check(Get("bossVolleyMode").ToString()=="Predictive" && ((Vector3)Get("bossAim")).x>.1f,"Second volley leads player movement");
        Set("bossPreviousPlayerPosition",new Vector3(-5,.5f,0)); Call("TickForestBoss",1f);
        Vector3 locked=(Vector3)Get("bossAim"); MovePlayer(new Vector3(-5,.5f,0)); Call("TickForestBoss",.36f);
        Check((Vector3)Get("bossAim")==locked && Needles.Count==5,"Predictive shot cannot follow a reversal after locking");
        Fresh(); Phase("Chase"); HP[Boss]=1800; Call("TickForestBoss",.01f);
        Check((bool)Get("bossEnraged") && PhaseName=="Enrage","Half health triggers a distinct second phase");
        Call("TickForestBoss",2f);
        Check(PhaseName=="Summon" && ((IList)Get("bossSummonWarnings")).Count==4 && game.EnemyCount==1,"Half health announces four minions before spawning");
        Call("TickForestBoss",1.01f); Check(game.EnemyCount==5,"Half-health summon creates four minions");
        Set("bossAttackIndex",2); Call("BeginBossAttack",Vector3.back*2.3f);
        Call("TickForestBoss",1.1f); Call("TickForestBoss",.36f); Check(Needles.Count==5 && PhaseName=="Volley","Enraged fan leaves an interval before its second burst");
        Call("TickForestBoss",.65f); Check(Needles.Count==5,"Second burst also preserves its lock window");
        Call("TickForestBoss",.36f); Check(Needles.Count==10 && PhaseName=="ChargeWindup","Second burst chains directly into a warned charge");
        VerifyMinions();
        VerifyPressure();
        VerifyHordeCycle();

        Fresh(); int hp=HP[Boss]; var round=TestBullet(0,16);
        Call("ResolveBulletHit",round,Boss,Boss.position);
        Check(HP[Boss]==hp-8 && (int)round.GetType().GetField("Damage").GetValue(round)==16,"Boss halves rifle hits without mutating piercing projectile damage");
        Call("ResolveBulletHit",TestBullet(4,16),Boss,Boss.position);
        Call("ResolveBulletHit",TestBullet(0,16,true),Boss,Boss.position);
        Check(HP[Boss]==hp-40,"Crossbow and secondary damage retain full strength");
        var target=Boss; Set("forestBoss",null);
        Call("ResolveBulletHit",TestBullet(0,16),target,target.position); Set("forestBoss",target);
        Check(HP[Boss]==hp-56,"Ordinary enemies retain full rifle damage");
        Fresh(); game.Run.Equipment.Collect(10000);
        game.Run.Equipment.Buy(3); game.Run.Equipment.Buy(3);
        for(int i=0;i<3;i++) game.Run.Equipment.Buy(0);
        for(int i=0;i<200;i++)
        {
            Call("UpdateRifle",.05f);
            foreach(var shot in (IList)Get("bullets")) shot.GetType().GetField("Critical").SetValue(shot,(bool?)false);
            Call("MoveBullets",.05f);
        }
        int sustained=profile.Rules.BossHealth-HP[Boss];
        Check(sustained>500 && sustained<800,"Fully upgraded rifle stays within the revised ten-second damage budget");
        Debug.Log("FOREST_BOSS_RIFLE_BALANCE: fully upgraded rifle, no crits, 10 seconds = "+sustained+" damage (previous direct damage would be "+sustained*2+").");

        for(int weapon=0;weapon<WeaponCatalog.Count;weapon++)
        {
            Fresh(weapon);
            if(weapon==10) Boss.position=new Vector3(0,.375f,-2.5f);
            int initial=HP[Boss];
            for(int i=0;i<50 && HP[Boss]==initial;i++)
            {
                if(weapon==0) Call("UpdateRifle",.1f);
                else if(weapon==1) Call("UpdateMelee",.1f);
                else if(weapon==2) Call("UpdateFlamethrower",.1f);
                else Call("UpdateNewWeapons",.1f);
                Call("MoveBullets",.1f);
            }
            Check(HP[Boss]<initial,"Weapon damages boss: "+WeaponCatalog.Names[weapon]);
        }
        Fresh(); Call("Ignite",Boss,false);
        Check(((IDictionary)Get("burns")).Contains(Boss),"Boss accepts lasting burns");
        Vector3 planted=Boss.position; Call("PushEnemyApart",Boss,Vector3.right,5f);
        Check(Boss.position==planted,"Weapon knockback cannot displace a telegraphed boss");
        ((Dictionary<Transform,float>)Get("chilled"))[Boss]=Time.time+10;
        Phase("Chase",5); Boss.position=new Vector3(0,.375f,8); Call("TickForestBoss",.5f);
        Check(Boss.position.z>6.2f && Boss.position.z<8,"Frost slows boss movement");

        Fresh(); Set("bossAttackIndex",0); Call("BeginBossAttack",Vector3.back*2.3f);
        var warning=(Transform)Get("bossWarning"); Call("ReleaseNeedle",new Vector3(0,.65f,6),Vector3.back);
        Call("DamageEnemy",0,100000,Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic|BindingFlags.Public),"Secondary"));
        Check(game.Run.Session.State==WaveSession.Phase.Combat && Needles.Count==0 && !warning.gameObject.activeSelf,"Lethal hit clears hazards without clearing weapon collections mid-iteration");
        Call("Tick",.01f);
        Check(game.Run.Session.State==WaveSession.Phase.Victory && Boss==null && game.EnemyCount==0,"Flow resolves boss victory and cleanup after combat");
        Fresh(); Set("bossAttackIndex",0); Call("BeginBossAttack",Vector3.back*2.3f); warning=(Transform)Get("bossWarning");
        game.Health.TakeDamage(100);
        Check(game.Run.Session.State==WaveSession.Phase.Defeat && !warning.gameObject.activeSelf && Needles.Count==0,"Player defeat cancels boss hazards");
        Fresh(); Call("ClearCombatObjects"); Check(Boss==null && game.EnemyCount==0,"Restart cleanup removes boss state");
        Debug.Log("FOREST_BOSS_CHECKS_PASSED: final-wave gate, model and arms, arena, charge-slam, both volleys and aim locks, capped melee summons, warning/dodge/long-frame sweeps, cover, phase two, all 11 weapons, status effects, stagger, death, victory and cleanup.");
    }
    static void VerifyMinions()
    {
        Fresh(); Phase("Chase"); Set("bossSummonTimer",0f); Call("TickForestBoss",.01f);
        var warnings=(IList)Get("bossSummonWarnings");
        Check(PhaseName=="Summon" && warnings.Count==3 && game.EnemyCount==1,"Periodic summon reserves three visible warnings");
        Check((float)Get("bossSummonTimer")>=10 && (float)Get("bossSummonTimer")<=12,"Normal summon cadence is 10 to 12 seconds");
        Vector3 planted=Boss.position; Call("TickForestBoss",.5f);
        Check(Boss.position==planted && game.EnemyCount==1,"Boss stops during summon and births wait one second");
        Call("MoveEnemies",.51f); Check(game.EnemyCount==4,"Summons integrate with crowd movement without mutating its enumeration");
        Check(((IDictionary)Get("shooters")).Count==0 && ((IDictionary)Get("chargers")).Count==0,"Summons are ordinary melee enemies");
        foreach(var enemy in (List<Transform>)Get("enemies")) if(enemy!=Boss)
            Check(Vector3.Distance(enemy.position,((PlayerController)Get("player")).transform.position)>2,"Summons start away from the player");
        int loot=((IList)Get("pickups")).Count; Call("KillEnemy",1);
        Check(game.EnemyCount==3 && ((IList)Get("pickups")).Count==loot+1,"Killed minions drop ordinary material");
        Call("BeginBossSummon",4); Call("TickForestBoss",1.01f); Check(game.EnemyCount==7,"Summons fill up to six minions");
        Call("BeginBossSummon",4); Check(warnings.Count==0,"Full population cannot reserve more births");
        Call("TickForestBoss",2f); Check(game.EnemyCount==7,"Population cap includes surviving minions");
        Call("KillEnemy",0); Call("Tick",.01f);
        Check(game.Run.Session.State==WaveSession.Phase.Victory && game.EnemyCount==0,"Boss victory clears all surviving minions");

        Fresh(); Call("BeginBossSummon",3); warnings=(IList)Get("bossSummonWarnings");
        Vector3 occupied=((Transform)warnings[0]).position; MovePlayer(new Vector3(occupied.x,.5f,occupied.z));
        Call("TickForestBoss",1.01f);
        foreach(var enemy in (List<Transform>)Get("enemies")) if(enemy!=Boss)
            Check(Vector3.Distance(enemy.position,((PlayerController)Get("player")).transform.position)>=3,"Walking onto a marker prevents an unsafe birth");
        Fresh(); Call("BeginBossSummon",3); var warning=(Transform)((IList)Get("bossSummonWarnings"))[0];
        game.Health.TakeDamage(100);
        Check(!warning.gameObject.activeSelf && ((IList)Get("bossSummonWarnings")).Count==0,"Player defeat clears pending summon warnings");
        Fresh(); Call("BeginBossSummon",3); warning=(Transform)((IList)Get("bossSummonWarnings"))[0];
        Call("KillEnemy",0); Check(!warning.gameObject.activeSelf && ((IList)Get("bossSummonWarnings")).Count==0,"Boss death cancels pending births immediately");
        Fresh(); Call("BeginBossSummon",3); Call("ClearCombatObjects");
        Check(((IList)Get("bossSummonWarnings")).Count==0 && game.EnemyCount==0,"Restart clears minions and pending births");
    }
    static void VerifyPressure()
    {
        Fresh();
        Check(Resources.Load<WaveConfiguration>("Config/DefaultWaveConfiguration").Rules.BossHealth==3600,"Shipped asset and code default both use the stronger boss health");
        Check((float)Get("bossSummonTimer")==6f,"First reinforcements arrive early enough to matter");
        Boss.position=new Vector3(0,.375f,17); Phase("Chase",0);
        Call("TickForestBoss",.01f);
        Check(PhaseName=="ChargeWindup" && (float)Get("bossTravel")>12,"Distant players trigger a gap-closing attack instead of passive chasing");
        Fresh(); Boss.position=new Vector3(0,.375f,17); Set("bossAttackIndex",2);
        Call("BeginBossAttack",Vector3.back*17); Call("TickForestBoss",2f); Call("TickForestBoss",.36f);
        Call("UpdateEnemyNeedles",2f);
        Check(game.Health.Current==88,"Boss volley reaches and damages a player beyond the former 12 metre range");
        Call("ClearShooters"); game.Health.ResetHealth();
        Call("ReleaseNeedle",new Vector3(0,.65f,7),Vector3.back); Call("UpdateEnemyNeedles",.5f);
        Check(game.Health.Current==100,"Ordinary shooter needle keeps its slower travel speed");
        Call("UpdateEnemyNeedles",.5f); Check(game.Health.Current==92,"Ordinary shooter needle retains eight damage");

        Fresh(); Set("bossEnraged",true); Set("bossAttackIndex",1); Boss.position=new Vector3(0,.375f,6);
        Call("BeginBossAttack",Vector3.back*6);
        MovePlayer(new Vector3(5,.5f,0)); Call("TickForestBoss",2f); Call("TickForestBoss",2f);
        Check(PhaseName=="Slam" && (int)Get("bossComboCharges")==1,"Enraged charge reserves exactly one further charge");
        Call("TickForestBoss",.61f);
        Check(PhaseName=="ChargeWindup" && (float)Get("bossTimer")>=.7f && (int)Get("bossComboCharges")==0,"First impact reacquires player and leaves a full second-charge warning");
        Vector3 aim=(Vector3)Get("bossAim");
        MovePlayer(new Vector3(-6,.5f,6)); Call("TickForestBoss",2f);
        Check((Vector3)Get("bossAim")==aim,"Second charge cannot track a dodge after its warning starts");
        Call("TickForestBoss",2f); Call("TickForestBoss",.61f);
        Check(PhaseName=="Recover" && (float)Get("bossTimer")==2f && game.Health.Current==100,"Double charge combo is dodgeable and ends in a full two-second opening");

        Fresh(); Set("bossEnraged",true); Set("bossAttackIndex",2); Call("BeginBossAttack",Vector3.back*2.3f);
        MovePlayer(new Vector3(8,.5f,0)); Call("TickForestBoss",1f); Call("TickForestBoss",.36f);
        Call("TickForestBoss",1f); Call("TickForestBoss",.36f);
        Check(PhaseName=="ChargeWindup" && (int)Get("bossComboCharges")==0,"Volley combo adds one charge without an intervening recovery");
        MovePlayer(new Vector3(-8,.5f,0)); Call("TickForestBoss",2f); Call("TickForestBoss",2f); Call("TickForestBoss",.61f);
        Check(PhaseName=="Recover" && (float)Get("bossTimer")==2f,"Volley-charge-slam combo terminates in recovery");

        Fresh(); HP[Boss]=1800; Phase("Recover",2f); Call("TickForestBoss",1f);
        Check(PhaseName=="Recover" && !(bool)Get("bossEnraged"),"Half health does not steal an earned recovery opening");
        Call("TickForestBoss",1.01f); Call("TickForestBoss",10f);
        Check(PhaseName=="Enrage" && (float)Get("bossTimer")==1.5f,"Even a long frame preserves the full phase-change cue");
        Call("TickForestBoss",1.51f);
        Check(PhaseName=="Summon" && (float)Get("bossSummonTimer")>=7 && (float)Get("bossSummonTimer")<=9,"Enraged reinforcement cadence increases");

        Fresh(); Set("bossEnraged",true); Set("bossAttackIndex",1); Call("BeginBossAttack",Vector3.back*2.3f);
        game.Health.TakeDamage(100);
        Check((int)Get("bossComboCharges")==0 && Get("bossWarning")==null,"Player death cancels queued combo attacks");
        Debug.Log("FOREST_BOSS_PRESSURE_PASSED: distant attacks, boss-only projectile stats, dodgeable bounded combos, guaranteed recovery, phase-change safety, summon cadence and death cancellation.");
    }
    static void VerifyHordeCycle()
    {
        Fresh(); Check((float)Get("bossAssaultTimer")==18,"Normal assault lasts eighteen seconds before waiting for a safe retreat");
        Set("bossAssaultTimer",0f); Set("bossSummonTimer",100f); Phase("Recover",2f);
        Call("TickForestBoss",1f); Check(PhaseName=="Recover","Retreat cannot steal recovery time");
        Call("TickForestBoss",1.01f); Vector3 start=Boss.position;
        Call("TickForestBoss",.01f); Check(PhaseName=="Retreat" && Boss.position==start,"Retreat starts as a visible move, never a teleport");
        Call("TickForestBoss",.2f);
        Check(Vector3.Distance(start,Boss.position)>1 && Vector3.Distance(start,Boss.position)<2,"Boss runs toward the edge at a bounded speed");
        Call("TickForestBoss",5f);
        Check(PhaseName=="Rest" && (float)Get("bossTimer")==8f && game.Health.Current==100,"Retreat reaches rest without contact damage and preserves eight seconds on a long frame");
        Check(Mathf.Max(Mathf.Abs(Boss.position.x),Mathf.Abs(Boss.position.z))>=14.9f,"Rest takes place at the arena edge");
        var warning=(Transform)((IList)Get("bossSummonWarnings"))[0];
        Vector3 parked=Boss.position; float timer=(float)Get("bossTimer");
        Call("TickForestBoss",0f); Call("TickForestBoss",float.NaN);
        Check((float)Get("bossTimer")==timer && game.EnemyCount==1,"Pause and invalid ticks cannot advance rest or spawn minions");
        Call("TickForestBoss",.5f); Check(Boss.position==parked && game.EnemyCount==1,"Boss rests motionless and entrances have a full warning");
        int open=(int)Get("bossOpenSide");
        foreach(Transform marker in (IList)Get("bossSummonWarnings"))
        {
            Vector3 p=marker.position;
            int side=Mathf.Abs(p.x)>Mathf.Abs(p.z)?(p.x>0?1:3):(p.z>0?0:2);
            Check(side!=open,"The selected escape side receives no entrances");
        }
        Call("MoveEnemies",.51f);
        Check(game.EnemyCount==5 && !warning.gameObject.activeSelf,"First four minions emerge after warning outside crowd enumeration");
        var secondary=Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic|BindingFlags.Public),"Secondary");
        int hp=HP[Boss]; Call("DamageEnemy",0,20,secondary);
        Check(HP[Boss]==hp-10,"Resting boss takes half secondary damage and remains attackable");
        Call("ResolveBulletHit",TestBullet(0,16),Boss,Boss.position);
        Check(HP[Boss]==hp-14,"Rest armor stacks with the existing rifle resistance");
        HP[((List<Transform>)Get("enemies"))[1]]=100;
        int minionHP=100;
        Call("DamageEnemy",1,10,secondary);
        Check(HP[((List<Transform>)Get("enemies"))[1]]==minionHP-10,"Rest armor never protects minions");
        for(int i=0;i<5;i++) Call("TickForestBoss",1.01f);
        Check(game.EnemyCount==19,"Uncleared normal horde is capped at eighteen living minions");
        Set("bossTimer",.1f); Call("TickForestBoss",.2f);
        Check(PhaseName=="ChargeWindup" && (float)Get("bossTimer")==1f && ((IList)Get("bossSummonWarnings")).Count==0,"Return stops reinforcements and provides a complete one-second charge cue");
        hp=HP[Boss]; Call("DamageEnemy",0,20,secondary); Check(HP[Boss]==hp-20,"Rest armor ends immediately on return");
        int count=game.EnemyCount; Call("TickForestBoss",.5f);
        Check(PhaseName=="ChargeWindup" && game.EnemyCount==count,"No late births spill into the return cue");
        Call("TickForestBoss",.51f); MovePlayer(new Vector3(8,.5f,8)); Call("TickForestBoss",2f);
        Check(PhaseName=="Slam","Return charge ends in a warned slam");
        Call("TickForestBoss",.61f);
        Check(PhaseName=="Recover" && (float)Get("bossTimer")==2f && (float)Get("bossAssaultTimer")==18f,"Return ends with recovery and a fresh assault budget");
        Set("bossAssaultTimer",0f); Call("TickForestBoss",2.01f); Call("TickForestBoss",.01f);
        Check(PhaseName=="Retreat","The cycle repeats after the next assault");
        Call("KillEnemy",0); Call("Tick",.01f);
        Check(game.EnemyCount==0 && game.Run.Session.State==WaveSession.Phase.Victory,"Boss victory clears a large surviving horde");

        for(int phase=0;phase<2;phase++)
        {
            Fresh(); Set("bossEnraged",phase==1); Call("BeginBossRest");
            int total=0,budget=phase==0?24:32;
            for(int batch=0;batch<7;batch++)
            {
                Call("TickForestBoss",1.001f); total+=game.EnemyCount-1;
                while(game.EnemyCount>1) Call("KillEnemy",game.EnemyCount-1);
            }
            Check(total==budget && (int)Get("bossHordeRemaining")==0,"Clearing minions allows exactly the finite wave budget: "+budget);
            Call("TickForestBoss",2f);
            Check(PhaseName=="ChargeWindup" && (float)Get("bossAssaultTimer")== (phase==0?18f:14f),"Return resets the correct normal/enraged assault duration");
        }
        Fresh(); Set("bossEnraged",true); Call("BeginBossRest");
        for(int i=0;i<7;i++) Call("TickForestBoss",1.001f);
        Check(game.EnemyCount==25,"Enraged horde caps at twenty-four survivors");
        Fresh(); Call("BeginBossRest"); warning=(Transform)((IList)Get("bossSummonWarnings"))[0];
        Call("TickForestBoss",20f);
        Check(game.EnemyCount==1 && PhaseName=="ChargeWindup" && (float)Get("bossTimer")==1f && !warning.gameObject.activeSelf,"A stalled rest frame never dumps a whole horde or skips the return cue");
        Fresh(); Call("BeginBossRest"); warning=(Transform)((IList)Get("bossSummonWarnings"))[0];
        game.Health.TakeDamage(100);
        Check(!warning.gameObject.activeSelf && (int)Get("bossHordeRemaining")==0,"Player death cancels horde reservations");
        Fresh(); Call("BeginBossRest"); Call("TickForestBoss",1.01f); Call("ClearCombatObjects");
        Check(game.EnemyCount==0 && ((IList)Get("bossSummonWarnings")).Count==0 && (float)Get("bossAssaultTimer")==0,"Restart clears the entire rest-wave cycle");
        Debug.Log("FOREST_BOSS_HORDE_PASSED: full retreat/rest/return cycle, finite 24/32 waves, 18/24 caps, open side, safe warnings, armor, phase-two cadence, pause, long frames, death and restart.");
    }
    static void SetupCapture()
    {
        Fresh(9); MovePlayer(new Vector3(-2,.5f,-1)); Boss.position=new Vector3(0,.375f,3);
        Boss.GetComponent<EnemyAvatar>().Face(Vector3.back); Boss.GetComponent<EnemyAvatar>().BossPose(0,.4f);
        game.enabled=true; Time.timeScale=0;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        try
        {
            if(stage==0)
            {
                Prepare();
                if(SessionState.GetInt("ForestBossChecks",0)==2)
                {
                    game.NewGame(); game.BeginRun(0); game.Run.Equipment.Collect(10000);
                    foreach(int id in new[]{0,1,6}) for(int i=0;i<3;i++) game.Run.Equipment.Buy(WeaponCatalog.ShopId(id));
                    Call("ApplyEquipment"); EditorApplication.update-=Tick; SessionState.SetInt("ForestBossChecks",0);
                    ShaderUtil.allowAsyncCompilation=originalAsync;
                    return;
                }
                Verify(); SetupCapture();
            }
            else if(stage==1) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-InGame.png");
            else if(stage==2) {HP[Boss]=1800; Set("bossEnraged",true); Set("bossAim",new Vector3(-2,0,-4).normalized); Call("BeginBossSlam",false); Call("TickForestBoss",.75f);}
            else if(stage==3) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Slam.png");
            else if(stage==4) {Set("bossAttackIndex",2); Call("BeginBossAttack",new Vector3(-2,0,-4)); Call("TickForestBoss",.4f);}
            else if(stage==5) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Fan.png");
            else if(stage==6) {SetupCapture(); Call("BeginBossSummon",3);}
            else if(stage==7) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Summon.png");
            else if(stage==8) {Call("TickForestBoss",1.01f);}
            else if(stage==9) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Minions.png");
            else if(stage==10)
            {
                Fresh(); Boss.position=new Vector3(0,.375f,15); Call("BeginBossRest");
                var camera=((PlaygroundCamera)Get("viewCamera")).GetComponent<Camera>(); camera.orthographicSize=21;
                camera.transform.SetPositionAndRotation(new Vector3(0,26,-17),Quaternion.LookRotation(new Vector3(0,0,0)-new Vector3(0,26,-17)));
                for(int i=0;i<45;i++) Call("MoveEnemies",.1f);
                game.enabled=true; Time.timeScale=0;
            }
            else if(stage==11) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Horde.png");
            else if(stage==12) {Set("bossTimer",.01f); Call("TickForestBoss",.02f);}
            else if(stage==13) ScreenCapture.CaptureScreenshot("ArtSource/Enemies/ForestBoss-Return.png");
            else { Finish(0); return; }
            stage++; ready=EditorApplication.timeSinceStartup+.7;
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetInt("ForestBossChecks",0); Time.timeScale=1;
        ShaderUtil.allowAsyncCompilation=originalAsync;
        if(game!=null && profile!=null) {Set("waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
