using System;

// Pure C# checks can run without launching the Unity Editor or waiting five minutes.
public static class WaveSessionChecks
{
    public static void Run()
    {
        CheckEquipment();
        CheckShop();
        CheckShopRequirements();
        CheckWeaponExpansion();
        CheckWeapons();
        Require(FlameHit.Contains(5f, 0f, 5f, 22f) && !FlameHit.Contains(5.01f, 0f, 5f, 22f),
            "Flame damage stops at its finite range");
        Require(FlameHit.Contains(3f, 0.5f, 5f, 22f) && !FlameHit.Contains(3f, 2f, 5f, 22f)
            && !FlameHit.Contains(-1f, 0f, 5f, 22f), "Flame damage stays inside the forward cone");
        Require(FixedBladeHits.InSweptArc(0, 2, 3, 0.375f, 360, -10, 10),
            "Orbit damage remains continuous across the 360-degree seam");
        Require(FixedBladeHits.InSweptArc(-2, 0, 3, 0.375f, 270, -5, 5)
            && !FixedBladeHits.InSweptArc(2, 0, 3, 0.375f, 270, -5, 5),
            "Orbit hits follow the actual blade direction");
        Require(FixedBladeHits.InSweptArc(0, 2, 2.5f, 0.375f, 0, -65, 65)
            && FixedBladeHits.InSweptArc(0, -2, 2.5f, 0.375f, 180, -65, 65)
            && FixedBladeHits.InSweptArc(2, 0, 2.5f, 0.375f, 90, -65, 65)
            && FixedBladeHits.InSweptArc(-2, 0, 2.5f, 0.375f, -90, -65, 65),
            "Auto-aimed melee hits enemies approaching from all four directions");
        Require(FixedBladeHits.InSweptArc(0.1f, 0.1f, 2.5f, 0.375f, 180, -65, -50),
            "Enemies already touching the handle are not in an inner dead zone");
        Require(!FixedBladeHits.InSweptArc(0, 4, 2.5f, 0.375f, 0, -65, 65)
            && !FixedBladeHits.InSweptArc(0, -2, 2.5f, 0.375f, 0, -65, 65),
            "Targets outside range or behind the actual slash are not hit");
        Require(FixedBladeHits.InSweptArc(0, 2, 2.5f, 0.375f, 0, -65, 65),
            "A long frame sweeps the whole attack rather than skipping its target");
        var session = new WaveSession(new WaveRules {FinalBoss=false});
        Require(session.Wave == 1 && session.Remaining == 45, "First wave starts at 45 seconds");
        Require(!session.Continue() && session.Wave == 1, "Cannot skip an active wave");
        for (int wave = 1; wave <= WaveSession.TotalWaves; wave++)
        {
            double expectedDuration = wave <= 3 ? 45 : wave <= 7 ? 50 : wave <= 12 ? 55 : 60;
            Require(session.WaveDuration == expectedDuration && session.Remaining == expectedDuration,
                "Each wave uses its configured duration tier");
            Require(!session.Advance(expectedDuration-0.5) && session.Remaining == 0.5, "Combat lasts until the deadline");
            Require(session.Advance(0.5) && session.Remaining == 0, "Deadline completes exactly once");
            Require(!session.Advance(500) && session.Remaining == 0, "Non-combat phases do not advance time");
            if (wave < WaveSession.TotalWaves)
            {
                Require(session.State == WaveSession.Phase.Rest, "Non-final waves enter rest");
                session.Defeat();
                Require(session.State == WaveSession.Phase.Rest, "No defeat during rest");
                Require(session.Continue() && session.Wave == wave + 1 && session.Remaining == session.WaveDuration,
                    "Continue begins exactly one fresh wave");
            }
        }
        Require(session.State == WaveSession.Phase.Victory && !session.Continue(), "Twentieth wave wins; no twenty-first wave");
        session.Defeat();
        Require(session.State == WaveSession.Phase.Victory, "Victory is final until restart");
        session.Reset();
        session.Advance(20);
        session.Defeat();
        Require(session.State == WaveSession.Phase.Defeat && !session.Advance(100) && !session.Continue(),
            "Death stops progress and cannot continue to a new wave");
        session.Reset();
        Require(session.Wave == 1 && session.Remaining == 45 && session.State == WaveSession.Phase.Combat,
            "Restart resets all progression");
        Require(session.Advance(80) && session.State == WaveSession.Phase.Rest && session.Wave == 1,
            "A long frame cannot skip rest or consume the next wave");
    }

    private static void CheckShop()
    {
        var equipment=new RunEquipment(); equipment.ChooseStarter(0);
        var shop=new WaveShop(equipment,new Random(42)); shop.Open(1);
        Require(shop.Count==4,"Four shop cards");
        Require(!shop.Buy(0) && !shop.Refresh(),"No free purchases or refreshes");
        equipment.Collect(10000);
        for(int wave=1;wave<=20;wave++)
        {
            shop.Open(wave);
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<shop.Count;i++)
            {
                int item=shop.Item(i);
                Require(item>=0 && seen.Add(item),"No duplicate cards");
                Require(!WeaponCatalog.Passive(item) || ItemLibrary.At(item).UnlockWave<=wave,"Items respect wave unlocks");
            }
            int keep=shop.Item(0),price=shop.Price(0); shop.ToggleLock(0);
            int balance=equipment.Materials,cost=shop.RefreshCost;
            Require(shop.Refresh() && equipment.Materials==balance-cost && shop.RefreshCost>cost,"Refresh charges and escalates");
            Require(shop.Item(0)==keep && shop.Price(0)==price && shop.Locked(0),"Locked stock survives refresh");
            shop.Open(wave);
            Require(shop.Item(0)==keep && shop.Price(0)==price,"Locked stock survives new wave");
            balance=equipment.Materials;
            int replace=-1;
            for(int w=0;w<WeaponCatalog.Count;w++) if(equipment.HasWeapon(w)) { replace=w; break; }
            Require(shop.Buy(0,replace) && equipment.Materials==balance-price && !shop.Locked(0),"Buying locked stock charges once");
            Require(!shop.Buy(0),"Cannot buy sold card twice");
        }
        shop.Reset(); shop.Open(20);
        foreach(RunItem item in Enum.GetValues(typeof(RunItem)))
        {
            int id=ItemLibrary.ShopId((int)item); while(equipment.Level(id)<equipment.MaxLevel(id)) Require(equipment.Buy(id),"Acquire all item stacks");
            int balance=equipment.Materials; Require(!equipment.Buy(id) && equipment.Materials==balance,"Stack cap costs nothing");
        }
        Require(ItemLibrary.All.Length==ItemLibrary.Count && equipment.Interest(19)==40 && equipment.Interest(20)==0,"Item catalog count and capped interest deadline");
        equipment.Reset();
        foreach(RunItem item in Enum.GetValues(typeof(RunItem))) Require(equipment.Stacks(item)==0,"Reset clears passive inventory");
    }

    private static void CheckShopRequirements()
    {
        for(int starter=0;starter<3;starter++)
        {
            var equipment=new RunEquipment(); equipment.ChooseStarter(starter);
            equipment.Collect(100000);
            var shop=new WaveShop(equipment,new Random(120+starter));
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int roll=0;roll<200;roll++)
            {
                shop.Open(11);
                Require(shop.Refresh(),"Filtered shop can refresh");
                for(int slot=0;slot<shop.Count;slot++)
                {
                    int item=shop.Item(slot); seen.Add(item);
                    if(item==0 || (item>=6 && item<=9)) Require(starter==0,"Bullet upgrades require a gun");
                    if(item==1 || item==14) Require(starter==1,"Melee-only upgrades require a machete");
                    if(item>=10 && item<=12) Require(starter==2,"Burn upgrades require an ignition source");
                }
            }
            foreach(int common in new[] {2,3,4,5,13,15,16,17,18,19,20,21})
                Require(seen.Contains(common),"New weapons and usable general-purpose items stay in the pool");
            // Acquiring a gun and incendiary rounds enables both bullet and burn synergies.
            if(starter!=0) Require(equipment.Buy(3),"Acquire a gun mid-run");
            Require(equipment.Buy(8),"Acquire incendiary rounds");
            seen.Clear();
            for(int roll=0;roll<200;roll++)
            {
                shop.Open(11);
                for(int slot=0;slot<shop.Count;slot++) seen.Add(shop.Item(slot));
            }
            foreach(int unlocked in new[] {0,6,7,8,9,10,11,12})
                Require(seen.Contains(unlocked),"Newly acquired weapons and ignition unlock their upgrades");
        }
    }

    private static void CheckEquipment()
    {
        var equipment = new RunEquipment();
        Require(!equipment.Buy(0) && equipment.Materials == 0, "No purchase without materials");
        equipment.Collect(-1);
        equipment.Collect(12);
        Require(equipment.Buy(0) && equipment.Materials == 0 && equipment.Level(0) == 1,
            "Exact-price purchase charges once and grants equipment");
        Require(equipment.FireMultiplier < 1f && equipment.RangeMultiplier == 1f && equipment.SpeedMultiplier == 1f,
            "Magazine changes only fire interval");
        equipment.Collect(200);
        Require(equipment.Buy(1) && equipment.Buy(2) && equipment.RangeMultiplier > 1f && equipment.SpeedMultiplier > 1f,
            "Blade and boots grant their effects");
        Require(equipment.Buy(0) && equipment.Buy(0), "Magazine upgrades reach level three");
        int balance = equipment.Materials;
        Require(!equipment.Buy(0) && !equipment.Buy(-1) && !equipment.Buy(RunEquipment.ItemCount) && equipment.Materials == balance,
            "Max-level and invalid purchases never consume currency");
        equipment.Reset();
        Require(equipment.Materials == 0 && equipment.FireMultiplier == 1f && equipment.RangeMultiplier == 1f && equipment.SpeedMultiplier == 1f,
            "A new run clears currency and all equipment effects");
    }

    private static void CheckWeaponExpansion()
    {
        for(int starter=0;starter<WeaponCatalog.Count;starter++)
        {
            var equipment=new RunEquipment(); equipment.ChooseStarter(starter); equipment.Collect(100000);
            Require(equipment.EquippedCount==1 && equipment.HasWeapon(starter),"Every weapon can be a starter");
            var shop=new WaveShop(equipment,new Random(321+starter));
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int roll=0;roll<150;roll++)
            {
                shop.Open(11);
                for(int slot=0;slot<shop.Count;slot++)
                {
                    int item=shop.Item(slot); seen.Add(item);
                    if(item==0 || (item>=6 && item<=9)) Require(WeaponCatalog.Ammo(starter),"Only guns and crossbows receive ammo items");
                    if(item==1 || item==14) Require(WeaponCatalog.Melee(starter),"All melee weapons support melee items");
                    if(item>=10 && item<=12) Require(starter==2 || starter==7,"Only ignition sources receive burn items");
                }
            }
            for(int id=0;id<WeaponCatalog.Count;id++) Require(seen.Contains(WeaponCatalog.ShopId(id)),"Every weapon remains available for acquisition or upgrades");
            for(int id=0;id<WeaponCatalog.Count && equipment.EquippedCount<3;id++) if(!equipment.HasWeapon(id)) Require(equipment.Buy(WeaponCatalog.ShopId(id)),"Fill three slots");
            int incoming=0; while(equipment.HasWeapon(incoming)) incoming++;
            int balance=equipment.Materials;
            Require(!equipment.Buy(WeaponCatalog.ShopId(incoming)) && equipment.Materials==balance,"Full slots do not charge without a replacement");
            Require(!equipment.Buy(WeaponCatalog.ShopId(incoming),incoming) && equipment.Materials==balance,"Cannot replace an unequipped weapon");
            Require(equipment.Buy(WeaponCatalog.ShopId(incoming),starter) && !equipment.HasWeapon(starter) && equipment.HasWeapon(incoming)
                && equipment.EquippedCount==3 && equipment.Materials==balance-20,"Replacement charges once and preserves three slots");
        }

    }

    private static void CheckWeapons()
    {
        var equipment=new RunEquipment();
        Require(!equipment.HasWeapon(0) && !equipment.HasWeapon(1) && !equipment.HasWeapon(2),"No weapons before choice");
        equipment.ChooseStarter(0);
        Require(equipment.HasWeapon(0) && !equipment.HasWeapon(1),"Only starter is equipped");
        equipment.Collect(500);
        Require(equipment.Buy(3) && equipment.Gun.Level==2,"Upgrade owned weapon");
        Require(equipment.Buy(4) && equipment.HasWeapon(1) && equipment.Machete.Level==1,"Acquire missing weapon first");
        Require(equipment.Buy(4) && equipment.Machete.Level==2 && equipment.Gun.Level==2,"Independent weapon upgrades");
        Require(equipment.Buy(5) && equipment.HasWeapon(2),"Acquire fire breath");
        Require(equipment.Buy(3) && !equipment.Buy(3),"Weapon tier cap");
        equipment.Reset(); Require(!equipment.HasWeapon(0) && equipment.Gun.Level==1,"Restart resets weapons and ownership");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("WAVE_CHECK_FAILED: " + message);
    }
}
