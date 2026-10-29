using System;
using System.Collections.Generic;

public sealed class WaveShop
{
    public const int RefreshPrice=3;
    private readonly RunEquipment equipment;
    private readonly Random random;
    private readonly List<int> offers=new List<int>();
    private readonly HashSet<int> sold=new HashSet<int>();
    private readonly HashSet<int> locked=new HashSet<int>();
    private readonly List<int> prices=new List<int>();
    private int wave=1,refreshes;
    public int Count=>offers.Count;
    public int Item(int slot)=>offers[slot];
    public int Price(int slot)=>prices[slot];
    public bool Sold(int slot)=>sold.Contains(slot);
    public bool Locked(int slot)=>locked.Contains(slot);
    public int RefreshCost=>RefreshPrice+refreshes;
    public bool CanRefresh { get { if(equipment.Materials<RefreshCost || EligibleItems().Count==0) return false; for(int i=0;i<4;i++) if(!locked.Contains(i)) return true; return false; } }
    public WaveShop(RunEquipment equipment,Random random=null) { this.equipment=equipment; this.random=random??new Random(); }
    public void Reset() { offers.Clear(); prices.Clear(); sold.Clear(); locked.Clear(); wave=1; refreshes=0; }
    public void Open(int completedWave=1) { wave=completedWave; refreshes=0; Roll(); }
    public void ToggleLock(int slot) { if(slot<0 || slot>=Count || Sold(slot)) return; if(!locked.Add(slot)) locked.Remove(slot); }
    public bool Buy(int slot, int replace=-1)
    {
        if(slot<0 || slot>=Count || Sold(slot) || !IsEligible(Item(slot)) || equipment.Materials<Price(slot)) return false;
        // Locked cards keep their original price, even if another copy increased the stack price.
        int difference=equipment.Price(Item(slot))-Price(slot);
        if(difference>0) equipment.Collect(difference);
        if(!equipment.Buy(Item(slot),replace)) { if(difference>0) equipment.TrySpend(difference); return false; }
        if(difference<0) equipment.TrySpend(-difference);
        sold.Add(slot); locked.Remove(slot);
        for(int i=0;i<Count;i++) if(!Sold(i) && !IsEligible(Item(i))) { offers[i]=-1; prices[i]=0; sold.Add(i); locked.Remove(i); }
        return true;
    }
    public bool Refresh() { if(!CanRefresh || !equipment.TrySpend(RefreshCost)) return false; refreshes++; Roll(); return true; }
    private List<int> EligibleItems()
    {
        var items=new List<int>();
        for(int i=0;i<RunEquipment.ItemCount;i++)
            if(IsEligible(i)) items.Add(i);
        return items;
    }
    private bool IsEligible(int item)
    {
        if(item<0 || item>=RunEquipment.ItemCount || equipment.Level(item)>=equipment.MaxLevel(item)) return false;
        if(item==0) return equipment.HasAmmo;
        if(item==1) return equipment.HasMelee;
        // Boots and new weapons remain available regardless of the current loadout.
        if(item<6 || WeaponCatalog.WeaponId(item)>=0) return true;
        if(ItemLibrary.At(item).UnlockWave>wave) return false;
        // Tags express synergies, not requirements: e.g. a crit bonus also helps guns.
        switch((RunItem)ItemLibrary.Index(item))
        {
            case RunItem.SlowReleaseValve:
                return equipment.HasWeapon(10);
            case RunItem.ArmorPiercing:
            case RunItem.Ricochet:
            case RunItem.IncendiaryRounds:
            case RunItem.SplitCore:
                return equipment.HasAmmo;
            case RunItem.Whetstone:
            case RunItem.HuntingBadge:
                return equipment.HasCriticalWeapon;
            case RunItem.RevengeCharm:
                return equipment.EquippedCount>0 || equipment.Stacks(RunItem.MagneticCoil)>0;
            case RunItem.BloodPendant:
                return equipment.HasMelee;
            case RunItem.Fuse:
            case RunItem.ConcentratedFuel:
            case RunItem.CharBomb:
                return equipment.HasBurn;
            default:
                return true;
        }
    }
    private int Weight(int item)
    {
        if(item<3) return 1;
        if(WeaponCatalog.WeaponId(item)>=0) return equipment.HasWeapon(WeaponCatalog.WeaponId(item))?3:2;
        foreach(string tag in ItemLibrary.At(item).Tags.Split(' ')) if(equipment.Tags.Contains(tag)) return 4;
        return 2;
    }
    private void Roll()
    {
        var keep=new Dictionary<int,int>(); var keepPrices=new Dictionary<int,int>();
        foreach(int i in locked) if(i<Count && !Sold(i) && IsEligible(Item(i))) { keep[i]=Item(i); keepPrices[i]=Price(i); }
        offers.Clear(); prices.Clear(); sold.Clear(); locked.Clear();
        List<int> pool=EligibleItems();
        foreach(int item in keep.Values) pool.Remove(item);
        for(int slot=0;slot<4;slot++)
        {
            int item;
            if(keep.TryGetValue(slot,out item)) locked.Add(slot);
            else
            {
                if(pool.Count==0) { offers.Add(-1); prices.Add(0); sold.Add(slot); continue; }
                int total=0; foreach(int candidate in pool) total+=Weight(candidate);
                int roll=random.Next(total); item=pool[0];
                foreach(int candidate in pool) { roll-=Weight(candidate); if(roll<0) { item=candidate; break; } }
                pool.Remove(item);
            }
            offers.Add(item); prices.Add(keepPrices.ContainsKey(slot)?keepPrices[slot]:equipment.Price(item));
        }
    }
}
