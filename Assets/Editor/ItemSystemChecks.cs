using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ItemSystemChecks
{
    private static SurvivorPlayground game;
    private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    private static T Get<T>(string field)=>(T)GameTestAccess.Get(game,field);
    private static void Set(string field,object value)=>GameTestAccess.Set(game,field,value);
    private static void Call(string method,params object[] args)=>GameTestAccess.Call(game,method,args);
    private static void Check(bool condition,string message) { if(!condition) throw new Exception("ITEM_CHECK_FAILED: "+message); }
    private static RunEquipment Equipment=>Get<RunEquipment>("equipment");
    private static List<Transform> Enemies=>Get<List<Transform>>("enemies");
    private static Dictionary<Transform,int> HP=>Get<Dictionary<Transform,int>>("enemyHealth");
    private static void Fresh(int starter=0)
    {
        game.Restart(); game.BeginRun(starter); Equipment.Collect(10000);
        foreach(Transform enemy in Enemies) enemy.position=new Vector3(16,0.375f,16);
    }
    private static void Buy(RunItem item,int stacks=1) { for(int i=0;i<stacks;i++) Check(Equipment.Buy(ItemLibrary.ShopId((int)item)),"Buy "+item); }
    private static Transform Place(int index,float x,float z,int hp=10)
    { Transform t=Enemies[index]; t.position=new Vector3(x,0.375f,z); HP[t]=hp; return t; }
    private static object Kind(string name)=>Enum.Parse(typeof(CombatController).GetNestedType("HitKind",BindingFlags.NonPublic),name);
    private static void Damage(Transform target,int amount,string kind)=>Call("DamageEnemy",Enemies.IndexOf(target),amount,Kind(kind));
    private static void CheckSecondWaveBlades()
    {
        Fresh();
        var session=Get<WaveSession>("session");
        Check(session.Advance(session.WaveDuration),"Complete wave one"); Call("CompleteWave");
        Call("UpdateVictoryLoot",5f); Call("EnterVictoryShop"); Set("phaseChangedFrame",Time.frameCount-1);
        int bladeSlot=-1;
        for(int seed=0;seed<100 && bladeSlot<0;seed++)
        {
            var candidate=new WaveShop(Equipment,new System.Random(seed)); candidate.Open(1);
            for(int slot=0;slot<candidate.Count;slot++)
            {
                Check(candidate.Item(slot)!=1,"No blade range mod before owning blades");
                if(candidate.Item(slot)==4) { Set("shop",candidate); bladeSlot=slot; }
            }
        }
        Check(bladeSlot>=0,"Blade weapon offered after first wave");
        var shop=Get<WaveShop>("shop"); int balance=Equipment.Materials,price=shop.Price(bladeSlot);
        Call("BuyEquipment",bladeSlot);
        Transform blade=Get<Transform>("machete");
        Check(Equipment.HasWeapon(1) && blade.gameObject.activeInHierarchy && Equipment.Materials==balance-price,
            "Shop purchase equips visible blades and charges once");
        Set("shopSelection",shop.Count+1); Call("ConfirmPhase");
        Check(session.Wave==2 && blade.gameObject.activeInHierarchy && blade.GetComponentsInChildren<Renderer>().Length>=2,
            "Purchased blades remain visible in wave two");
        foreach(Transform enemy in Enemies) enemy.position=new Vector3(16,0.375f,16);
        Transform target=Place(0,1,0,1000); Quaternion before=blade.localRotation;
        Call("UpdateMelee",0.1f);
        Check(HP[target]<1000 && Quaternion.Angle(before,blade.localRotation)>1,
            "Purchased blades rotate and damage enemies in wave two");
        Debug.Log("SECOND_WAVE_BLADES_PASSED: shop acquisition, model activation, wave transition and melee damage.");
    }
    private static void CheckBalanceFixes()
    {
        var eligibility=typeof(WaveShop).GetMethod("IsEligible",Flags);
        Fresh(5);
        var shop=new WaveShop(Equipment);
        bool Eligible(RunItem item)=>(bool)eligibility.Invoke(shop,new object[] {ItemLibrary.ShopId((int)item)});
        Check(!Eligible(RunItem.Whetstone) && !Eligible(RunItem.HuntingBadge),"Hammer-only loadout excludes crit items");
        shop.Open(5);
        Check(!Eligible(RunItem.Whetstone) && !Eligible(RunItem.HuntingBadge) && Eligible(RunItem.BloodPendant),"Unlock waves do not enable useless hammer crit offers");
        Check(Eligible(RunItem.RevengeCharm),"Hammer fixed damage benefits from revenge buffs");
        Buy(RunItem.MagneticCoil);
        Check(Eligible(RunItem.RevengeCharm) && !Eligible(RunItem.Whetstone),"Hammer lightning restores damage buffs but cannot enable crits");
        Equipment.ChooseStarter(8);
        Check(Eligible(RunItem.Whetstone) && Eligible(RunItem.HuntingBadge) && Eligible(RunItem.RevengeCharm),"Mixed hammer/fireball loadout restores applicable items");
        for(int id=0;id<WeaponCatalog.Count;id++)
        {
            var equipment=new RunEquipment(); equipment.ChooseStarter(id);
            for(int level=1;level<equipment.Weapon(id).LevelLimit;level++)
            {
                string preview=WeaponUpgradePreview.Describe(id,equipment,false);
                Check(preview.Contains("→") && equipment.Weapon(id).Level==level,"Every upgrade previews changes without mutating equipment");
                if(id==1) Check(preview.Contains(level==1?"10 → 12":"12 → 14"),"Blade preview uses actual damage growth");
                if(id==2) Check(preview.Contains(level==1?"3 → 4":"4 → 5"),"Breath preview uses independent growth");
                if(id==5) Check(preview.Contains("Damage:") && preview.Contains("cannot crit"),"Hammer preview shows fixed damage and excludes crits");
                equipment.Weapon(id).Upgrade();
            }
            Check(WeaponUpgradePreview.Describe(id,equipment,false)=="Maximum level","Max level has no fictitious upgrade");
        }
        for(int stacks=0;stacks<=3;stacks++)
        {
            Fresh(0); if(stacks>0) Buy(RunItem.ConcentratedFuel,stacks);
            Transform target=Place(0,0,1,1000); Call("Ignite",target,false);
            object burn=Get<IDictionary>("burns")[target];
            for(int tick=0;tick<10;tick++)
            {
                burn.GetType().GetField("NextTick").SetValue(burn,0f);
                Call("UpdateFlamethrower",0f);
                Call("Ignite",target,false); // Refresh must retain fractional progress.
            }
            Check(HP[target]==1000-(20+8*stacks),"Ten burn ticks preserve all fuel stacks including refreshes");
        }
        Fresh(0); Buy(RunItem.ConcentratedFuel); Buy(RunItem.RevengeCharm);
        Transform buffed=Place(0,0,1,1000); Call("Ignite",buffed,false); Set("revengeUntil",Time.time+5);
        object activeBurn=Get<IDictionary>("burns")[buffed];
        for(int tick=0;tick<25;tick++)
        {
            activeBurn.GetType().GetField("NextTick").SetValue(activeBurn,0f);
            Call("UpdateFlamethrower",0f);
        }
        Check(HP[buffed]==916,"Fuel and revenge combine before fractional burn accumulation");
        Fresh(2);
        for(int level=1;level<=3;level++)
        {
            Check(Equipment.Flame.Damage(Get<int>("flameDamage"))==level+2,"Breath damage is 3/4/5");
            if(level<3) Equipment.Flame.Upgrade();
        }
        Debug.Log("BALANCE_FIXES_PASSED: per-weapon previews, hammer item eligibility, fractional burn with refresh/fuel/revenge, breath growth.");
    }

    public static void Run(SurvivorPlayground instance)
    {
        game=instance;
        var randomState=UnityEngine.Random.state;
        try
        {
            for(int starter=0;starter<3;starter++)
            {
                Fresh(starter);
                for(int w=0;w<3;w++) Check(Equipment.HasWeapon(w)==(w==starter),"Starter isolation");
                Check(Get<Transform>("machete").gameObject.activeSelf==(starter==1),"Blade visual ownership");
            }
            CheckBalanceFixes();
            CheckSecondWaveBlades();
            Fresh(); Buy(RunItem.ArmorPiercing);
            Transform direct=Place(0,.6f,2,6),pierced=Place(1,.6f,5,100);
            Call("Shoot"); Call("MoveBullets",0.3f);
            Check(!HP.ContainsKey(direct) && HP[pierced]<100,"Piercing damages second target");

            Fresh(); Buy(RunItem.Ricochet);
            direct=Place(0,.6f,2,6); Transform bounced=Place(1,3,2,100);
            Call("Shoot"); Call("MoveBullets",0.4f);
            Check(!HP.ContainsKey(direct) && HP[bounced]<100,"Bounce changes direction and damages target");

            Fresh(); Buy(RunItem.IncendiaryRounds,3); Buy(RunItem.Fuse); Buy(RunItem.ConcentratedFuel);
            direct=Place(0,0,1,1000); Transform spread=Place(1,1,1,1000);
            for(int i=0;i<100 && Get<IDictionary>("burns").Count==0;i++) Call("OnPrimaryHit",direct,Kind("Gun"));
            Check(Get<IDictionary>("burns").Contains(direct),"Incendiary hit ignites");
            Call("ProcessItemEffects"); Check(Get<IDictionary>("burns").Contains(spread),"Fuse spreads fire");
            object burn=Get<IDictionary>("burns")[direct];
            Check((float)burn.GetType().GetField("Until").GetValue(burn)>Time.time+2.5f,"Fuel extends burn");
            burn.GetType().GetField("NextTick").SetValue(burn,0f);
            Call("UpdateFlamethrower",0.01f);
            Check(HP[direct]==998,"Fractional fuel damage carries into following ticks");

            Fresh(); Buy(RunItem.CharBomb);
            direct=Place(0,0,1); spread=Place(1,1,1); Transform survivor=Place(2,2,1,100);
            Call("Ignite",direct,false); Call("Ignite",spread,false);
            Damage(direct,10,"Flame"); Call("ProcessItemEffects");
            Check(!HP.ContainsKey(spread) && HP[survivor]==85 && Get<ICollection>("pendingEffects").Count==0,"Char explosion has no recursive chain");

            Fresh(); Buy(RunItem.SplitCore);
            direct=Place(0,0,1); Place(1,3,1); Place(2,-3,1);
            Damage(direct,10,"Gun"); Call("ProcessItemEffects");
            IList bullets=Get<IList>("bullets"); Check(bullets.Count==2,"Split launches two fragments");
            foreach(object bullet in bullets) Check((bool)bullet.GetType().GetField("Secondary").GetValue(bullet),"Fragments cannot trigger item effects");

            Fresh(1); Buy(RunItem.Whetstone); Buy(RunItem.BloodPendant); Buy(RunItem.HuntingBadge,2);
            Check(Equipment.CritChance(true)>Equipment.CritChance(false),"Extra melee critical chance");
            game.Health.TakeDamage(10); direct=Place(0,0,1,1000);
            for(int i=0;i<100 && game.Health.Current==90;i++) Call("OnPrimaryHit",direct,Kind("Melee"));
            Check(game.Health.Current==91,"Melee life steal heals");
            for(int i=0;i<100;i++) Call("OnPrimaryHit",direct,Kind("Melee"));
            Check(game.Health.Current==91,"Life steal respects shared cooldown");
            bool extra=false;
            for(int trial=0;trial<50 && !extra;trial++)
            {
                if(Enemies.Count==0) Call("SpawnEnemy");
                int before=Get<IList>("pickups").Count;
                Damage(Enemies[0],10000,"Melee"); extra=Get<IList>("pickups").Count>before+1;
            }
            Check(extra,"Critical kills can grant hunting material");

            Fresh(); Buy(RunItem.RevengeCharm);
            game.Health.TakeDamage(10); direct=Place(0,0,1,100);
            Damage(direct,10,"Secondary"); Check(HP[direct]==88,"Revenge buffs damage after injury");
            Set("revengeUntil",0f); Damage(direct,10,"Secondary"); Check(HP[direct]==78,"Revenge expires");

            Fresh(); Buy(RunItem.EmergencyShield); Buy(RunItem.FirstAid);
            Call("BeginItemWave"); Check(!game.Health.TakeDamage(10) && game.Health.Current==100 && game.Health.ShieldCharges==0,"Shield blocks first hit");
            typeof(PlayerHealth).GetField("vulnerableAt",Flags).SetValue(game.Health,0f);
            Check(game.Health.TakeDamage(75) && game.Health.Current==50,"First aid heals surviving low-health hit");
            typeof(PlayerHealth).GetField("vulnerableAt",Flags).SetValue(game.Health,0f);
            game.Health.TakeDamage(25); Check(game.Health.Current==25,"First aid is once per wave");
            Call("BeginItemWave"); Check(game.Health.ShieldCharges==1,"Shield refreshes next wave");

            Fresh(); Buy(RunItem.SavingsJar); Buy(RunItem.DangerContract);
            var session=Get<WaveSession>("session"); session.Advance(session.WaveDuration); session.Continue();
            int balance=Equipment.Materials; Call("BeginItemWave"); Check(Equipment.Materials==balance+40,"Savings paid at next wave with cap");
            Check(Equipment.DangerMultiplier>1,"Danger contract raises pressure");
            extra=false;
            for(int trial=0;trial<50 && !extra;trial++)
            { if(Enemies.Count==0) Call("SpawnEnemy"); int before=Get<IList>("pickups").Count; Damage(Enemies[0],10000,"Secondary"); extra=Get<IList>("pickups").Count>before+1; }
            Check(extra,"Danger contract grants extra material");

            Fresh(); Buy(RunItem.MagneticCoil);
            Check(Equipment.PickupRange==5,"Coil increases pickup reach");
            direct=Place(0,0,1,100); survivor=Place(1,2,1,100);
            Call("CollectMaterial",15,true); Check(Get<int>("lightningCharges")==1,"Collecting charges coil");
            Call("OnPrimaryHit",direct,Kind("Gun")); Call("ProcessItemEffects");
            Check(HP[survivor]==88 && Get<int>("lightningCharges")==0 && Get<ICollection>("pendingEffects").Count==0,"Charged hit releases finite secondary lightning");
            Call("CollectMaterial",30,false); Check(Get<int>("lightningCharges")==0,"End-wave collection does not grant combat charge");
            Call("ToggleCodex"); Check(Time.timeScale==0,"Codex pauses combat"); Call("ToggleCodex"); Check(Time.timeScale==1,"Codex restores time");
            Debug.Log("ITEM_SYSTEM_PASSED: all sixteen items, three starter weapons, proc limits, economy, healing, shields, ownership and codex pause.");
        }
        finally { game.Restart(); game.BeginRun(0); UnityEngine.Random.state=randomState; }
    }
}
