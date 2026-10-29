using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeaponExpansionChecks
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static SurvivorPlayground game;
    static object originalConfiguration;
    static WaveConfiguration validationConfiguration;
    static int stage;
    static int replacementBalance,replacementWeapon;
    static double next,deadline;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
    static List<Transform> Enemies=>(List<Transform>)Get("enemies");
    static Dictionary<Transform,int> Health=>(Dictionary<Transform,int>)Get("enemyHealth");
    static RunEquipment Equipment=>(RunEquipment)Get("equipment");
    static void Check(bool ok,string message) { if(!ok) throw new Exception("WEAPON_CHECK: "+message); }
    [MenuItem("Tools/Survivors/Verify Weapon Expansion")]
    public static void Capture()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run this check outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("WeaponExpansionChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("WeaponExpansionChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; deadline=next+60; EditorApplication.update+=Tick;
        };
    }
    static void Fresh(int weapon)
    {
        game.NewGame(); game.BeginRun(weapon); Call("ClearCombatObjects");
    }
    static Transform Target(Vector3 position,int hp=100)
    {
        Call("SpawnEnemy"); Transform target=Enemies[Enemies.Count-1]; target.position=position; Health[target]=hp; return target;
    }
    static void CheckFireball()
    {
        Fresh(8);
        var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"));
        blocks.Clear();
        var bullets=(IList)Get("bullets");
        Call("UpdateNewWeapons",.01f);
        Transform focus=((Transform[])Get("newWeaponVisuals"))[8];
        Check(focus!=null && focus.gameObject.activeSelf && bullets.Count==0,"Equipped fireball is visible even without targets");
        Check(focus.GetComponentsInChildren<Renderer>().Length==4,"Ready fireball has a core and visible embers");
        Transform front=Target(new Vector3(0,.375f,3));
        Transform nearby=Target(new Vector3(.9f,.375f,3));
        Transform far=Target(new Vector3(3,.375f,3));
        Call("UpdateNewWeapons",.01f);
        Check(bullets.Count==1,"Fireball emits exactly one projectile");
        object ball=bullets[0]; var type=ball.GetType();
        type.GetField("Critical").SetValue(ball,(bool?)false);
        Call("UpdateNewWeapons",.1f);
        Check(bullets.Count==1,"Fireball respects cooldown");
        Call("MoveBullets",.5f);
        Check(bullets.Count==0 && Health[front]==88 && Health[nearby]==88 && Health[far]==100,"Swept fireball explodes once on nearest contact, splashes nearby only");
        Check(((IDictionary)Get("burns")).Count==0,"Basic fireball does not ignite");
        Call("ClearCombatObjects"); blocks.Clear();
        front=Target(new Vector3(.8f,.375f,12.5f));
        Call("FireFireball",new Vector3(0,.5f,0),Vector3.forward);
        ball=bullets[0]; type.GetField("Critical").SetValue(ball,(bool?)false);
        Call("MoveBullets",2f);
        Check(bullets.Count==0 && Health[front]==88,"Max range explosion damages a nearby target without contact");
        Call("ClearCombatObjects"); blocks.Clear();
        blocks.Add(new Bounds(new Vector3(0,0,2),new Vector3(4,4,.5f)));
        front=Target(new Vector3(0,.375f,2.6f));
        Call("FireFireball",new Vector3(0,.5f,0),Vector3.forward);
        Call("MoveBullets",1f);
        Check(bullets.Count==0 && Health[front]==100,"Wall stops projectile and blocks splash damage");
        blocks.Clear(); Call("ClearCombatObjects");
        front=Target(new Vector3(0,.375f,3));
        Equipment.Weapon(8).Upgrade();
        Call("FireFireball",new Vector3(0,.5f,0),Vector3.forward);
        ball=bullets[0]; type.GetField("Critical").SetValue(ball,(bool?)true);
        Call("MoveBullets",1f);
        Check(Health[front]==66,"Upgraded fireball deals seventeen damage and ordinary double crit");
        Call("FireFireball",new Vector3(0,.5f,0),Vector3.forward);
        Call("ShowDefeat"); Check(bullets.Count==0 && !focus.gameObject.activeSelf,"Defeat removes in-flight fireballs and the ready focus");
        Call("ClearCombatObjects");
        Check(((IList)Get("impacts")).Count==0 && !focus.gameObject.activeSelf,"Cleanup removes blast and ready visuals");
        Fresh(8); blocks.Clear();
        bool fired=false;
        for(int frame=0;frame<400;frame++)
        {
            Call("TickCombat",.02f); fired|=bullets.Count>0;
            if(game.Kills>0) break;
        }
        Check(fired && game.Kills>0,"Ordinary combat tick automatically fires and kills from a fireball-only start");
        Call("ClearCombatObjects");
        var equipment=new RunEquipment(); equipment.ChooseStarter(0); equipment.ChooseStarter(1); equipment.ChooseStarter(2); equipment.Collect(100);
        Check(equipment.Buy(WeaponCatalog.ShopId(8),1) && equipment.HasWeapon(8) && !equipment.HasWeapon(1),"Fireball can replace a full weapon slot");
        Check(equipment.Buy(WeaponCatalog.ShopId(8)) && equipment.Buy(WeaponCatalog.ShopId(8)) && !equipment.Buy(WeaponCatalog.ShopId(8)),"Fireball shop upgrades cap at three");
    }

    static void CheckBlades()
    {
        Fresh(1);
        var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles")); blocks.Clear();
        Check((int)Get("meleeDamage")==10 && Mathf.Abs((float)Get("AttackRange")-2.2f)<.001f,"Blade scene values match new balance");
        Transform near=Target(new Vector3(2,.375f,0));
        Transform far=Target(new Vector3(3.1f,.375f,0));
        Transform basic=Target(new Vector3(-2,.375f,0),10);
        Call("UpdateMelee",0f);
        int after=Health[near];
        Check((after==90 || after==80) && Health[far]==100 && !Health.ContainsKey(basic),"Short blades hit for ten or critical twenty, kill basic enemies and respect reduced range");
        Call("UpdateMelee",0f);
        Check(Health[near]==after,"Both blades share the same enemy hit cooldown");
        Check(Mathf.Abs(((Dictionary<Transform,float>)Get("meleeReadyAt"))[near]-Time.time-1.25f)<.001f,"Base blade hit cooldown remains 1.25 seconds");
        Call("ClearCombatObjects");
        near=Target(new Vector3(2,.375f,0));
        blocks.Add(new Bounds(new Vector3(1,0,0),new Vector3(.2f,4,2)));
        Call("UpdateMelee",0f); Check(Health[near]==100,"Blades cannot hit through cover");
        blocks.Clear(); Call("ClearCombatObjects");
        Call("UpdateMelee",.5f); Check(Mathf.Abs((float)Get("bladeOrbitAngle")-90)<.001f,"Blade rotation remains two seconds per revolution");
        for(int tier=1;tier<=3;tier++)
        {
            Check(Equipment.Machete.Damage(WeaponCatalog.Damage[1])==10+(tier-1)*2,"Blade upgrades give 10/12/14 damage");
            Call("UpdateBladeLength");
            var arms=(List<Transform>)Get("macheteArms");
            Vector3 tip=((PlayerController)Get("player")).transform.InverseTransformPoint(arms[0].TransformPoint(new Vector3(0,.1f,2.5f)));
            Check(Mathf.Abs(new Vector2(tip.x,tip.z).magnitude-(float)Get("AttackRange"))<.001f,"Visible blade tip follows upgraded attack reach");
            if(tier<3) Equipment.Machete.Upgrade();
        }
    }

    static void CheckHitFeedback()
    {
        Fresh(1);
        var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles")); blocks.Clear();
        Transform target=Target(new Vector3(2,.375f,0),12);
        var kind=Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic),"Melee");
        Call("DamageEnemyCritical",Enemies.IndexOf(target),10,kind,(bool?)false);
        var numbers=(IList)Get("floatingDamage");
        Check(Health[target]==2 && numbers.Count==1,"Twelve-health enemy survives with damage popup");
        var avatar=target.GetComponent<EnemyAvatar>();
        Check((float)typeof(EnemyAvatar).GetField("hitFlashUntil",F).GetValue(avatar)>Time.time,"Melee triggers hit flash");
        var bodies=(Renderer[])typeof(EnemyAvatar).GetField("bodies",F).GetValue(avatar);
        var tint=new MaterialPropertyBlock(); bodies[0].GetPropertyBlock(tint);
        Check(tint.GetColor("_BaseColor")==Color.white,"Hit tint is white");
        avatar.Chill(Time.time+5);
        typeof(EnemyAvatar).GetField("hitFlashUntil",F).SetValue(avatar,Time.time-.01f);
        typeof(EnemyAvatar).GetMethod("Update",F).Invoke(avatar,null);
        bodies[0].GetPropertyBlock(tint);
        Check(tint.GetColor("_BaseColor")==new Color(.3f,.8f,1),"Hit flash restores frost tint");
        ((Dictionary<Transform,float>)Get("meleeReadyAt"))[target]=Time.time+1.25f;
        Call("UpdateMelee",0f); Check(numbers.Count==1 && Health[target]==2,"Cooldown contact cannot create false hit feedback");
        Call("DamageEnemyCritical",Enemies.IndexOf(target),10,kind,(bool?)true);
        Check(!Health.ContainsKey(target) && numbers.Count==2 && ((IList)Get("floatingScores")).Count==1,"Lethal hit creates damage number and kill score popup");
        Call("UpdateFloatingScores",1f); Check(numbers.Count==0,"Damage numbers expire");
        Check(((IList)Get("floatingScores")).Count==0,"Kill score expires");
        Call("ClearCombatObjects");
        Debug.Log("HIT_FEEDBACK_PASSED: melee survivor, damage popups, flash/frost restore, cooldown, lethal score and lifetime.");
    }

    static void CheckPauseAndLook()
    {
        Fresh(1);
        var camera=(PlaygroundCamera)Get("viewCamera"); bool third=camera.ThirdPerson; camera.ThirdPerson=true;
        void Look(float stick,float delta,bool dragging,float dt)=>typeof(PlaygroundCamera).GetMethod("ApplyLook",F).Invoke(camera,new object[] {stick,delta,dragging,dt});
        float yaw=camera.Yaw;
        Look(0,100,false,.01f); Check(camera.Yaw==yaw,"Free mouse movement does not rotate camera");
        Look(0,100,true,.01f); float moved=Mathf.DeltaAngle(yaw,camera.Yaw);
        Check(moved>0,"RMB mouse input rotates camera");
        yaw=camera.Yaw; Look(0,100,true,.1f);
        Check(Mathf.Abs(Mathf.DeltaAngle(yaw,camera.Yaw)-moved)<.001f,"Mouse rotation does not depend on frame duration");
        float remaining=(float)((WaveSession)Get("session")).Remaining;
        Vector3 position=((PlayerController)Get("player")).transform.position;
        Call("TogglePause"); yaw=camera.Yaw;
        Look(1,100,true,1); Call("Tick",1f);
        Check((bool)Get("Paused") && Time.timeScale==0 && camera.Yaw==yaw && (float)((WaveSession)Get("session")).Remaining==remaining
            && ((PlayerController)Get("player")).transform.position==position && !((PlayerController)Get("player")).enabled,"Pause freezes clock, movement and camera");
        Call("ResumeGame"); Check(!(bool)Get("Paused") && Time.timeScale==1 && ((PlayerController)Get("player")).enabled,"Resume restores combat controls");
        Call("TogglePause"); Call("StopRun",false);
        Check(!(bool)Get("Paused") && Time.timeScale==1 && (bool)Get("choosingStarter") && (int)Get("frontScreen")==0 && Enemies.Count==0,"Stop clears run and returns to weapon selection");
        game.BeginRun(1); Call("TogglePause"); Call("StopRun",true);
        Check((int)Get("frontScreen")==1 && !(bool)Get("Paused") && Time.timeScale==1 && Enemies.Count==0,"Return to menu clears run and restores time");
        camera.ThirdPerson=third; Fresh(0);
        Debug.Log("PAUSE_LOOK_PASSED: mouse drag scaling, pause/resume, stop and return menu.");
    }

    static void CheckTorch()
    {
        Fresh(7);
        var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles")); blocks.Clear();
        Transform front=Target(new Vector3(0,.375f,1.4f));
        Transform behind=Target(new Vector3(0,.375f,-1.4f));
        Call("UpdateNewWeapons",.1f);
        Check(Health[front]==100,"Torch windup does not hit early");
        Call("UpdateNewWeapons",.15f);
        int after=Health[front];
        Check((after==88 || after==76) && Mathf.Abs(front.position.z-2.3f)<.01f && Health[behind]==100,"Torch sweep deals low damage and pushes only the front sector");
        Call("UpdateNewWeapons",.1f);
        Check(Health[front]==after,"Torch cannot hit an enemy twice in one swing");
        Call("ClearCombatObjects");
        Transform basic=Target(new Vector3(0,.375f,2.6f),10);
        Call("HitNewMelee",7,Vector3.forward);
        Check(!Health.ContainsKey(basic),"Level one torch kills a basic enemy in one hit at expanded reach");
        Call("ClearCombatObjects");
        Check(!((LineRenderer)Get("torchTrail")).enabled && !((Transform[])Get("newWeaponVisuals"))[7].gameObject.activeSelf,"Torch trail and model clear with combat");
        for(int tier=1;tier<=3;tier++)
        {
            Call("ClearCombatObjects");
            float angle=85*Mathf.Deg2Rad;
            Transform side=Target(new Vector3(Mathf.Sin(angle)*1.5f,.375f,Mathf.Cos(angle)*1.5f));
            Call("HitNewMelee",7,Vector3.forward);
            Check((Health[side]<100)==(tier==3),"Tier three sweep reaches eighty-five degrees; lower tiers do not");
            Call("ClearCombatObjects");
            front=Target(new Vector3(0,.375f,1.4f));
            Call("HitNewMelee",7,Vector3.forward);
            Check(Mathf.Abs(front.position.z-(2.3f+.3f*(tier-1)))<.01f,"Torch upgrade increases knockback distance");
            Check(Equipment.Weapon(7).Damage(WeaponCatalog.Damage[7])==12+(tier-1)*2 && Equipment.Weapon(7).Interval(1)==1 && Equipment.Weapon(7).Range(2.8f)==2.8f,"Torch upgrades prioritize angle and push rather than reach or attack speed");
            if(tier<3) Equipment.Weapon(7).Upgrade();
        }
        Call("ClearCombatObjects");
        front=Target(new Vector3(0,.375f,1.4f));
        blocks.Add(new Bounds(new Vector3(0,0,2.1f),new Vector3(2,4,.2f)));
        Call("HitNewMelee",7,Vector3.forward);
        Check(front.position.z<=1.625f,"Torch push cannot pass through an obstacle");
        Call("ClearCombatObjects"); blocks.Clear();
        front=Target(new Vector3(0,.375f,1.4f));
        blocks.Add(new Bounds(new Vector3(0,0,.7f),new Vector3(2,4,.2f)));
        Call("HitNewMelee",7,Vector3.forward);
        Check(Health[front]==100,"Torch cannot hit through cover");
        blocks.Clear(); Call("ClearCombatObjects");
    }

    static void CombatChecks()
    {
        WaveSessionChecks.Run();
        ArchitectureChecks.Run(game);
        ItemSystemChecks.Run(game);
        CheckWaveLoot();
        CheckEnemyCrowds();
        CheckPlayerContact();
        CheckRifle();
        CheckThrownSpear();
        CheckFireball();
        CheckTorch();
        CheckBlades();
        CheckHitFeedback();
        CheckPauseAndLook();
        Transform front,side;
        CheckHammer();
        Fresh(7); front=Target(new Vector3(0,.375f,1.5f));
        Call("HitNewMelee",7,Vector3.forward);
        Check(Health[front]<100 && front.position.z>1.5f && ((IDictionary)Get("burns")).Contains(front),"Torch damages, ignites and pushes back");
        Fresh(6); front=Target(new Vector3(0,.375f,2));
        Call("UpdateNewWeapons",.01f);
        var bullets=(IList)Get("bullets"); object bullet; System.Type type;
        Check(bullets.Count==0,"Frost field never fires projectiles");
        Check(Health[front]==100 && !((IDictionary)Get("chilled")).Contains(front),"Falling ice must not damage before landing");
        Transform weak=Target(new Vector3(1,.375f,1),20);
        Call("UpdateNewWeapons",.35f);
        Check(!Health.ContainsKey(weak),"Level one ice landing can kill a 20-health enemy");
        int frostHealth=Health[front];
        Check(((IDictionary)Get("chilled")).Contains(front) && (frostHealth==80 || frostHealth==60),"Ice landing deals twenty damage, or forty on crit, and chills");
        Call("UpdateNewWeapons",.1f);
        Check(Health[front]==frostHealth,"Frost field hits each enemy only once per release");
        Transform late=Target(new Vector3(-1,.375f,1));
        Call("UpdateNewWeapons",.1f);
        Check(Health[late]==96 || Health[late]==92,"Late entrants take four frost damage, or eight on crit");
        Call("ClearCombatObjects"); Check(((IDictionary)Get("chilled")).Count==0 && Get("frostField")==null,"Frost clears between waves");
        Fresh(4);
        for(int tier=1;tier<=4;tier++)
        {
            Call("ClearCombatObjects");
            Call("FireNewProjectile",4,new Vector3(0,.5f,0),Vector3.forward);
            var volley=(IList)Get("bullets");
            Check(volley.Count==tier+1,"Crossbow tier determines two to five arrows");
            bool aimed=false;
            foreach(object arrow in volley)
            {
                var arrowType=arrow.GetType();
                Vector3 dir=(Vector3)arrowType.GetField("Direction").GetValue(arrow);
                aimed|=Vector3.Angle(dir,Vector3.forward)<.01f;
                Check((int)arrowType.GetField("Damage").GetValue(arrow)==12,"Crossbow upgrades preserve per-arrow damage");
            }
            Check(aimed,"Every volley includes one arrow aligned to the target");
            Check(Equipment.MaxLevel(WeaponCatalog.ShopId(4))==3,"Shop supports the fourth crossbow tier");
            Check(Equipment.Weapon(4).Upgrade()==(tier<4),"Crossbow caps at tier four");
        }
        Fresh(4); bool sawCrit=false,sawNormal=false;
        for(int i=0;i<100;i++)
        {
            Call("FireNewProjectile",4,new Vector3(0,.5f,0),Vector3.forward);
            bullets=(IList)Get("bullets"); bullet=bullets[bullets.Count-1]; type=bullet.GetType();
            bool crit=(bool)type.GetField("Critical").GetValue(bullet);
            Check((int)type.GetField("Pierce").GetValue(bullet)==0,"Critical bolts do not gain free piercing");
            sawCrit|=crit; sawNormal|=!crit;
        }
        Check(sawCrit && sawNormal,"Crossbow produces both critical and normal bolts");
        Fresh(4); front=Target(new Vector3(0,.375f,2)); side=Target(new Vector3(1,.375f,2));
        Call("FireNewProjectile",4,new Vector3(0,.5f,0),Vector3.forward);
        bullet=((IList)Get("bullets"))[0]; Call("ResolveBulletHit",bullet,front,front.position);
        Check(Health[front]<100 && Health[side]==100,"Crossbow has no gun splash");
    }
    static void CheckRifle()
    {
        Fresh(0);
        Transform first=Target(new Vector3(.4f,.375f,2),1),nextTarget=Target(new Vector3(1.5f,.375f,3)),neighbor=Target(new Vector3(-1,.375f,2));
        Call("UpdateRifle",0f); Check(((IList)Get("bullets")).Count==1,"Burst fires its first round immediately");
        Call("MoveBullets",.1f);
        Check(!Health.ContainsKey(first) && Health[nextTarget]==100 && Health[neighbor]==100,"Rifle kills only the direct target, without splash");
        Call("UpdateRifle",.05f); Check(((IList)Get("bullets")).Count==0,"Burst waits between rounds");
        Call("UpdateRifle",.06f); Check(((IList)Get("bullets")).Count==1,"Second round retargets after a kill");
        Call("UpdateRifle",.11f); Check(((IList)Get("bullets")).Count==2,"Burst emits three rounds total");
        Call("UpdateRifle",.2f); Check(((IList)Get("bullets")).Count==2,"Rifle pauses after the third round");
        Call("UpdateRifle",.29f); Check(((IList)Get("bullets")).Count==3,"Next burst begins after the burst interval");
        Call("ClearCombatObjects"); Check((int)Get("rifleRounds")==0 && ((IList)Get("bullets")).Count==0,"Wave cleanup cancels the burst and all bullets");
        Fresh(0); Transform target=Target(new Vector3(.4f,.375f,2));
        Call("Shoot"); object bullet=((IList)Get("bullets"))[0];
        bullet.GetType().GetField("Critical").SetValue(bullet,false);
        Call("MoveBullets",.1f);
        Check(Health[target]==94 && target.position.z>2,"Rifle deals six base damage with gentle knockback");
        var avatar=target.GetComponent<EnemyAvatar>();
        Check((float)typeof(EnemyAvatar).GetField("hitFlashUntil",F).GetValue(avatar)>Time.time,"Rifle hit flashes the enemy");
    }
    static void CheckPlayerContact()
    {
        Fresh(0);
        Transform enemy=Target(new Vector3(.8f,.375f,0));
        int hp=game.Health.Current;
        // A zero-delta tick is a pause and intentionally skips contact processing.
        Call("MoveEnemies",.0001f); Check(game.Health.Current==hp,"Visible gap outside player body does not cause contact damage");
        enemy.position=new Vector3(.65f,.375f,.65f);
        Call("MoveEnemies",.0001f); Check(game.Health.Current==hp,"Diagonal near miss does not cause contact damage");
        enemy.position=new Vector3(.6f,.375f,0);
        Call("MoveEnemies",.0001f); Check(game.Health.Current<hp,"Actual body contact still causes damage");
        Fresh(0); enemy=Target(new Vector3(0,.375f,3));
        Call("MoveEnemies",.1f); Check(Mathf.Abs(enemy.position.z-2.72f)<.001f,"First wave enemies move at 2.8 meters per second");
        game.Run.Session.Advance(game.Run.Session.WaveDuration); game.Run.Session.Continue();
        enemy.position=new Vector3(0,.375f,3);
        Call("MoveEnemies",.1f); Check(Mathf.Abs(enemy.position.z-2.71f)<.001f,"Second wave accelerates gently to 2.9 meters per second");
        for(int wave=3;wave<=WaveSession.TotalWaves;wave++)
        {
            game.Run.Session.Advance(game.Run.Session.WaveDuration); game.Run.Session.Continue();
            enemy.position=new Vector3(0,.375f,3);
            Call("MoveEnemies",.1f);
            float expected=Mathf.Min(4.2f,2.8f+(wave-1)*.1f);
            Check(Mathf.Abs(enemy.position.z-(3-expected*.1f))<.001f,"Enemy speed increases per wave and caps at 4.2");
        }
        Fresh(0); enemy=Target(new Vector3(0,.375f,3));
        Call("MoveEnemies",.1f); Check(Mathf.Abs(enemy.position.z-2.72f)<.001f,"New runs reset to the first-wave speed");
    }
    static void CheckThrownSpear()
    {
        Fresh(3);
        var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles")); blocks.Clear();
        Transform a=Target(new Vector3(.5f,.375f,3)),b=Target(new Vector3(.5f,.375f,5)),side=Target(new Vector3(3,.375f,3));
        Call("UpdateNewWeapons",.01f); Call("UpdateNewWeapons",.1f);
        Check(Health[a]==100 && Health[b]==100,"Spear windup does not damage enemies");
        Call("UpdateNewWeapons",.03f); Call("UpdateNewWeapons",.3f);
        Check(Health[a]<100 && Health[b]<100 && Health[side]==100,"Thrown spear sweeps through multiple enemies without hitting off-line enemies");
        int hpA=Health[a],hpB=Health[b]; Call("UpdateNewWeapons",.1f);
        Check(Health[a]==hpA && Health[b]==hpB,"Each enemy is hit only once per throw");
        Call("UpdateNewWeapons",3f); Check((int)Get("spearPhase")==3,"Spear stops at the arena wall");
        Call("UpdateNewWeapons",.1f); Check((int)Get("spearPhase")==3,"Spear remains embedded briefly");
        Call("UpdateNewWeapons",.11f); Check((int)Get("spearPhase")==0,"Spear returns to hand");
        Call("UpdateNewWeapons",.1f); Check((int)Get("spearPhase")==0,"Cooldown starts after return");
        Call("ClearCombatObjects"); Check((int)Get("spearPhase")==0 && ((HashSet<Transform>)Get("spearHit")).Count==0,"Spear flight and hit state clear between waves");
        a=Target(new Vector3(.5f,.375f,2)); b=Target(new Vector3(.5f,.375f,5));
        blocks.Add(new Bounds(new Vector3(.5f,0,4),new Vector3(2,20,1)));
        GameTestAccess.Set(game,"spearTip",new Vector3(.5f,.65f,0));
        GameTestAccess.Set(game,"spearDirection",Vector3.forward);
        Call("MoveThrownSpear",100f);
        Check(Health[a]<100 && Health[b]==100 && (int)Get("spearPhase")==3,"Obstacle blocks spear hits behind it even in a long frame");
        blocks.Clear(); Call("ClearCombatObjects");
    }
    static void CheckWaveLoot()
    {
        Fresh(0);
        Target(new Vector3(0,.375f,4)); Target(new Vector3(2,.375f,4)); Target(new Vector3(-2,.375f,4));
        Call("SpawnMaterial",new Vector3(0,.25f,6)); Call("SpawnMaterial",new Vector3(2,.25f,6));
        int balance=Equipment.Materials,score=(int)Get("lootScore"),kills=(int)Get("kills");
        ((WaveSession)Get("session")).Advance(((WaveSession)Get("session")).WaveDuration); Call("CompleteWave");
        Check(Enemies.Count==0 && ((IList)Get("pickups")).Count==0 && ((IList)Get("lootFlights")).Count==5,"Wave cleanup combines three enemy drops with two arena pickups");
        Call("UpdateVictoryLoot",5f);
        Check(Equipment.Materials==balance+7 && (int)Get("lootScore")==score+5 && (int)Get("kills")==kills,"Two arena drops award materials, three cleanup drops award only score, plus five completion materials");
        Call("UpdateVictoryLoot",5f);
        Check(Equipment.Materials==balance+7 && (int)Get("lootScore")==score+5,"End-wave loot is credited exactly once");
    }
    static void CheckEnemyCrowds()
    {
        game.NewGame(); game.BeginRun(6);
        Check(Enemies.Count==4,"First wave starts with four enemies");
        Check((int)typeof(SurvivorPlayground).GetProperty("SpawnBatch",F).GetValue(game)==1
            && (int)typeof(SurvivorPlayground).GetProperty("EnemyLimit",F).GetValue(game)==14
            && Math.Abs((float)typeof(SurvivorPlayground).GetProperty("WaveSpawnInterval",F).GetValue(game)-1.5f)<.001f,"First wave uses a gentle spawn budget");
        for(int i=0;i<40;i++) Call("SpawnEnemy");
        for(int i=0;i<Enemies.Count;i++) for(int j=i+1;j<Enemies.Count;j++)
            Check(Vector3.Distance(Enemies[i].position,Enemies[j].position)>=.849f,"Occupied portals never stack new enemies");
        Fresh(6);
        // This fixture tests enemy separation in open space, independently of random arena cover.
        ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"))).Clear();
        Transform a=Target(new Vector3(0,.375f,4)),b=Target(new Vector3(0,.375f,4)),c=Target(new Vector3(0,.375f,4));
        Call("SeparateEnemies");
        Check(Vector3.Distance(a.position,b.position)>.8f && Vector3.Distance(a.position,c.position)>.8f && Vector3.Distance(b.position,c.position)>.8f,"Coincident enemies separate without invalid coordinates");
        Call("StunEnemy",a,.8f); Vector3 fixedPosition=a.position; b.position=a.position;
        Call("SeparateEnemies");
        Check(a.position==fixedPosition && Vector3.Distance(a.position,b.position)>.8f,"Crowd separation respects stunned enemies");
        Call("ClearCombatObjects");
    }
    static void CheckHammer()
    {
        Fresh(5);
        Target(new Vector3(.6f,.375f,0));
        Transform clusterA=Target(new Vector3(-1.6f,.375f,-.4f)),clusterB=Target(new Vector3(-1.6f,.375f,.4f)),clusterC=Target(new Vector3(-.9f,.375f,0));
        Call("UpdateNewWeapons",.01f);
        Vector3 selected=(Vector3)Get("hammerCenter");
        Check(selected.x<0,"Hammer prefers a nearby cluster over the nearest isolated enemy");
        foreach(Transform enemy in new[]{clusterA,clusterB,clusterC})
        {
            Vector3 gap=enemy.position-selected; gap.y=0;
            Check(gap.magnitude<=1.5f,"Selected center catches the clustered enemies");
        }
        selected.y=0; Check(selected.magnitude<=2.001f,"Cluster targeting keeps the close landing limit");
        Fresh(5);
        Target(new Vector3(.6f,.375f,0));
        Transform fartherA=Target(new Vector3(-2.8f,.375f,-.4f),30),fartherB=Target(new Vector3(-2.8f,.375f,.4f),30);
        Call("UpdateNewWeapons",.01f);
        selected=(Vector3)Get("hammerCenter"); selected.y=0;
        Check(selected.magnitude>1.2f && selected.magnitude<=2.001f,"Hammer extends its landing to reach a larger nearby group");
        Call("UpdateNewWeapons",.52f);
        Check(!Health.ContainsKey(fartherA) && !Health.ContainsKey(fartherB),"Extended landing kills both grouped enemies");
        Fresh(5);
        Transform center=Target(new Vector3(0,.375f,2),30),outer=Target(new Vector3(1.7f,.375f,2)),far=Target(new Vector3(6,.375f,2)),touching=Target(new Vector3(0,.375f,0));
        Vector3 originalScale=center.localScale,outerPosition=outer.position;
        Call("SlamHammer",new Vector3(0,.035f,2));
        Check(!Health.ContainsKey(center) && Health[outer]==100 && Health[far]==100,"Hammer kills a weak center target and spares the outer area");
        var stunned=(Dictionary<Transform,float>)Get("stunned");
        Check(!stunned.ContainsKey(center) && stunned.ContainsKey(outer) && !stunned.ContainsKey(far),"Stun is limited to surviving enemies in the impact area");
        Check(Vector3.Distance(outer.position,outerPosition)>.39f && outer.position.x>outerPosition.x,"Outer enemies are pushed 0.4 meters away from impact");
        Check(Mathf.Abs(stunned[outer]-Time.time-1f)<.001f,"Hammer stun lasts one second after knockback");
        outerPosition=outer.position;
        var health=(PlayerHealth)Get("health"); int hp=health.Current;
        Call("MoveEnemies",.1f);
        Check(outer.position==outerPosition && health.Current==hp,"Stunned enemies neither move nor deal contact damage");
        var avatar=center.GetComponent<EnemyAvatar>();
        Transform visual=(Transform)typeof(EnemyAvatar).GetField("visual",F).GetValue(avatar);
        Vector3 rest=(Vector3)typeof(EnemyAvatar).GetField("restScale",F).GetValue(avatar);
        Check(visual.localScale.y<rest.y*.3f && center.localScale==originalScale,"Crush flattens the visual without changing the gameplay root");
        Check((bool)typeof(EnemyAvatar).GetField("flatCorpse",F).GetValue(avatar),"Center hit leaves a flattened corpse");
        stunned[touching]=Time.time-1;
        Call("MoveEnemies",.01f); Check(health.Current<hp,"Contact damage returns after the stun expires");
        Check(((IList)Get("groundCracks")).Count==8,"Ground slam creates radial fissures");
        Call("UpdateHammerEffects",1.2f); Check(((IList)Get("groundCracks")).Count==0,"Fissures expire and clean up");
        Transform dead=Target(new Vector3(0,.375f,2),1);
        Call("SlamHammer",new Vector3(0,.035f,2));
        Check(!Health.ContainsKey(dead) && dead!=null && (bool)typeof(EnemyAvatar).GetField("flatCorpse",F).GetValue(dead.GetComponent<EnemyAvatar>()),"Crushed kills leave a brief flattened corpse");
        Check(typeof(RunEquipment).GetProperty("GhostBonus")==null,"Ghost kill growth has been removed");
        Fresh(5); center=Target(new Vector3(0,.375f,2));
        Call("UpdateNewWeapons",.24f); Check(Health[center]==100,"Windup deals no early damage");
        Vector3 landing=(Vector3)Get("hammerCenter"),playerPosition=((PlayerController)Get("player")).transform.position;
        landing.y=playerPosition.y;
        Check(Vector3.Distance(landing,playerPosition)<=2.001f,"Landing stays within 2 meters of the player");
        Call("UpdateNewWeapons",.14f); Check(Health[center]==62 && stunned.ContainsKey(center),"Fixed damage occurs on landing and stuns a surviving center target");
        Transform late=Target(new Vector3(0,.375f,1.2f));
        Call("UpdateNewWeapons",.6f); Check(Health[late]==100,"Recovery never repeats the slam");
        Call("ClearCombatObjects");
        Check(stunned.Count==0 && ((IList)Get("groundCracks")).Count==0,"Round cleanup removes hammer effects");
        Fresh(5);
        Transform expandedCenter=Target(new Vector3(1.4f,.375f,2),30),expandedOuter=Target(new Vector3(-2.6f,.375f,2)),outside=Target(new Vector3(0,.375f,4.9f));
        Call("SlamHammer",new Vector3(0,.035f,2));
        Check(!Health.ContainsKey(expandedCenter) && stunned.ContainsKey(expandedOuter) && !stunned.ContainsKey(outside),"Expanded 1.5 meter center and 2.8 meter shockwave have distinct boundaries");
        Call("ClearCombatObjects");
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Weapon verification timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); if(game==null) return;
            next=double.MaxValue;
            if(stage==0)
            {
                // These fixtures assume the authored default wave balance, independent of local test profiles.
                originalConfiguration=Get("waveConfiguration");
                validationConfiguration=ScriptableObject.CreateInstance<WaveConfiguration>();
                GameTestAccess.Set(game,"waveConfiguration",validationConfiguration);
                game.NewGame();
            }
            else if(stage==1) { ScreenCapture.CaptureScreenshot("Logs/WeaponStarter.png"); }
            else if(stage==2) { CombatChecks(); game.NewGame(); game.BeginRun(3); Equipment.Collect(500); Equipment.Buy(25); Equipment.Buy(24); }
            else if(stage==3) { ScreenCapture.CaptureScreenshot("Logs/WeaponCombat.png"); }
            else if(stage==4)
            {
                ((WaveSession)Get("session")).Advance(((WaveSession)Get("session")).WaveDuration); Call("CompleteWave");
                GameTestAccess.Set(game,"victoryContinueRequested",true);
                Call("UpdateVictoryLoot",5f);
                var shop=(WaveShop)Get("shop"); int offer=-1;
                for(int roll=0;roll<100 && offer<0;roll++)
                {
                    shop.Open(1);
                    for(int i=0;i<shop.Count;i++) { int id=WeaponCatalog.WeaponId(shop.Item(i)); if(id>=0 && !Equipment.HasWeapon(id)) { offer=i; replacementWeapon=id; break; } }
                }
                Check(offer>=0,"A full loadout can still find new weapon offers");
                replacementBalance=Equipment.Materials;
                GameTestAccess.Set(game,"phaseChangedFrame",Time.frameCount-1);
                Call("BuyEquipment",offer);
                Check((int)Get("replacementOffer")==offer && Equipment.Materials==replacementBalance,"Replacement UI opens before charging");
            }
            else if(stage==5) { ScreenCapture.CaptureScreenshot("Logs/WeaponReplacement.png"); }
            else if(stage==6)
            {
                Call("FinishReplacement",3);
                Check(Equipment.HasWeapon(replacementWeapon) && !Equipment.HasWeapon(3) && Equipment.EquippedCount==3
                    && Equipment.Materials==replacementBalance-20 && (int)Get("replacementOffer")==-1,"Replacement UI completes transaction once");
            }
            else if(stage==7) { ScreenCapture.CaptureScreenshot("Logs/WeaponShop.png"); }
            else if(stage==8)
            {
                SessionState.SetBool("HammerChecks.AsyncShaders",ShaderUtil.allowAsyncCompilation);
                ShaderUtil.allowAsyncCompilation=false;
                Fresh(5); Target(new Vector3(0,.375f,2),200); Target(new Vector3(1.7f,.375f,2),200); Target(new Vector3(-1.7f,.375f,2),200);
                game.enabled=false; ((PlayerController)Get("player")).enabled=false;
                Call("UpdateNewWeapons",.25f);
            }
            else if(stage==9) { ScreenCapture.CaptureScreenshot("Logs/Hammer-Raised.png"); }
            else if(stage==10) { Call("UpdateNewWeapons",.13f); ScreenCapture.CaptureScreenshot("Logs/Hammer-Slam.png"); }
            else if(stage==11) { Fresh(3); game.enabled=false; ((PlayerController)Get("player")).enabled=false; Call("UpdateNewWeapons",.01f); }
            else if(stage==12) { ScreenCapture.CaptureScreenshot("Logs/Spear-Held.png"); }
            else if(stage==13) { Fresh(0); game.enabled=false; ((PlayerController)Get("player")).enabled=false; Call("UpdateRifle",0f); }
            else if(stage==14) { ScreenCapture.CaptureScreenshot("Logs/Rifle-Held.png"); }
            else if(stage==15) { Fresh(4); Target(new Vector3(0,.375f,6)); Call("UpdateNewWeapons",.08f); game.enabled=false; }
            else if(stage==16) { ScreenCapture.CaptureScreenshot("Logs/Crossbow-Held.png"); game.enabled=true; }
            else if(stage==17)
            {
                Fresh(8); game.enabled=false; ((PlayerController)Get("player")).enabled=false;
                var blocks=(List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles")); blocks.Clear();
                Target(new Vector3(0,.375f,6)); Target(new Vector3(.9f,.375f,6)); Target(new Vector3(-.9f,.375f,6));
                foreach(Transform enemy in Enemies) enemy.GetComponent<EnemyAvatar>()?.Face(Vector3.back);
                Call("UpdateNewWeapons",.01f); Call("MoveBullets",.27f);
            }
            else if(stage==18) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Flight.png"); }
            else if(stage==19) { Call("MoveBullets",.2f); Call("UpdateImpacts",.10f); }
            else if(stage==20) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Impact.png"); }
            else if(stage==21) { game.NewGame(); game.enabled=true; GameTestAccess.Set(game,"starterSelection",8); }
            else if(stage==22) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Starters.png"); }
            else if(stage==23) { Fresh(8); game.enabled=false; ((PlayerController)Get("player")).enabled=false; Call("UpdateNewWeapons",.01f); }
            else if(stage==24) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Equipped.png"); }
            else if(stage==25) { Call("UpdateNewWeapons",.65f); }
            else if(stage==26) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Orbit.png"); }
            else if(stage==27)
            {
                Target(new Vector3(0,.375f,4));
                ((float[])Get("newCooldown"))[8]=.08f;
                Call("UpdateNewWeapons",0f);
            }
            else if(stage==28) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Charging.png"); }
            else if(stage==29) { Call("UpdateNewWeapons",.09f); }
            else if(stage==30) { ScreenCapture.CaptureScreenshot("Logs/Fireball-Release.png"); }
            else if(stage==31) { Fresh(7); game.enabled=false; ((PlayerController)Get("player")).enabled=false; Call("UpdateNewWeapons",.01f); }
            else if(stage==32) { ScreenCapture.CaptureScreenshot("Logs/Torch-Idle.png"); }
            else if(stage==33)
            {
                Target(new Vector3(0,.375f,1.4f)); Target(new Vector3(.9f,.375f,1.2f));
                foreach(Transform enemy in Enemies) enemy.GetComponent<EnemyAvatar>()?.Face(Vector3.back);
                Call("UpdateNewWeapons",.08f);
            }
            else if(stage==34) { ScreenCapture.CaptureScreenshot("Logs/Torch-Windup.png"); }
            else if(stage==35) { Call("UpdateNewWeapons",.17f); }
            else if(stage==36) { ScreenCapture.CaptureScreenshot("Logs/Torch-Sweep.png"); }
            else if(stage==37) { Fresh(1); game.enabled=false; ((PlayerController)Get("player")).enabled=false; Call("UpdateMelee",0f); }
            else if(stage==38) { ScreenCapture.CaptureScreenshot("Logs/Blades-Redesign.png"); }
            else if(stage==39) { Call("UpdateMelee",.25f); }
            else if(stage==40) { ScreenCapture.CaptureScreenshot("Logs/Blades-Orbit.png"); }
            else if(stage==41)
            {
                Fresh(1); game.enabled=true; ((PlayerController)Get("player")).enabled=false;
                ((List<Bounds>)typeof(ArenaObstacles).GetField("blocks",F).GetValue(Get("obstacles"))).Clear();
                GameTestAccess.Set(game,"nextSpawn",1000f); Time.timeScale=0;
                Transform normal=Target(new Vector3(1.7f,.375f,1.2f),12);
                Transform crit=Target(new Vector3(-1.7f,.375f,1.2f),100);
                var kind=Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic),"Melee");
                Call("DamageEnemyCritical",Enemies.IndexOf(normal),10,kind,(bool?)false);
                Call("DamageEnemyCritical",Enemies.IndexOf(crit),10,kind,(bool?)true);
                foreach(Transform enemy in Enemies) ((Dictionary<Transform,float>)Get("meleeReadyAt"))[enemy]=Time.time+10;
            }
            else if(stage==42) { ScreenCapture.CaptureScreenshot("Logs/Hit-Feedback.png"); }
            else if(stage==43) { Time.timeScale=1; Call("TogglePause"); }
            else if(stage==44) { ScreenCapture.CaptureScreenshot("Logs/Pause-Menu.png"); }
            else if(stage==45) { Call("ResumeGame"); }
            else
            {
                ShaderUtil.allowAsyncCompilation=SessionState.GetBool("HammerChecks.AsyncShaders",true);
                Debug.Log("WEAPON_EXPANSION_PASSED: nine starters, blade damage/range/cooldown/wall/orbit/visual reach, fireball contact/range/wall/crit/cleanup, replacement transactions, filters, spear lane, hammer slam, crush and stun, torch burn/knockback, ice chill, crossbow crit piercing and no splash.");
                SessionState.SetBool("WeaponExpansionChecks",false); EditorApplication.update-=Tick;
                RestoreConfiguration();
                if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(0); else EditorApplication.ExitPlaymode(); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception exception)
        {
            if(stage>=8) ShaderUtil.allowAsyncCompilation=SessionState.GetBool("HammerChecks.AsyncShaders",true);
            Debug.LogException(exception); SessionState.SetBool("WeaponExpansionChecks",false); EditorApplication.update-=Tick;
            RestoreConfiguration();
            if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(1); else EditorApplication.ExitPlaymode();
        }
    }
    static void RestoreConfiguration()
    {
        if(validationConfiguration==null) return;
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",originalConfiguration);
        UnityEngine.Object.Destroy(validationConfiguration); validationConfiguration=null;
    }
}
