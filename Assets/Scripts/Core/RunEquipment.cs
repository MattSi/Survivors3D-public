public sealed class RunEquipment
{
    public const int ItemCount = WeaponCatalog.Count + 3 + ItemLibrary.Count;
    public const int WeaponSlotCount = 3;
    public WeaponSlot Gun { get; } = new WeaponSlot(WeaponSlot.WeaponType.Gun);
    public WeaponSlot Machete { get; } = new WeaponSlot(WeaponSlot.WeaponType.Machete);
    public WeaponSlot Flame { get; } = new WeaponSlot(WeaponSlot.WeaponType.Flamethrower);
    private readonly int[] levels = new int[3];
    private readonly int[] stacks = new int[ItemLibrary.Count];
    private readonly bool[] owned = new bool[WeaponCatalog.Count];
    private readonly WeaponSlot[] extra={new WeaponSlot(WeaponSlot.WeaponType.Spear),new WeaponSlot(WeaponSlot.WeaponType.Crossbow),new WeaponSlot(WeaponSlot.WeaponType.Earthbreaker),new WeaponSlot(WeaponSlot.WeaponType.Icicle),new WeaponSlot(WeaponSlot.WeaponType.Torch),new WeaponSlot(WeaponSlot.WeaponType.Fireball),new WeaponSlot(WeaponSlot.WeaponType.Boomerang),new WeaponSlot(WeaponSlot.WeaponType.PoisonBackpack),new WeaponSlot(WeaponSlot.WeaponType.BaitLantern)};
    public int EquippedCount { get { int n=0; foreach(bool value in owned) if(value) n++; return n; } }
    public bool HasAmmo => HasWeapon(0)||HasWeapon(4);
    public bool HasMelee => HasWeapon(1)||HasWeapon(3)||HasWeapon(5)||HasWeapon(7)||HasWeapon(9);
    public bool HasCriticalWeapon { get { for(int id=0;id<owned.Length;id++) if(owned[id] && id!=5 && id!=10 && id!=11) return true; return false; } }
    public bool HasBurn => HasWeapon(2)||HasWeapon(7)||(HasAmmo && Stacks(RunItem.IncendiaryRounds)>0);
    public int Materials { get; private set; }
    public bool HasWeapon(int slot) => slot>=0 && slot<WeaponCatalog.Count && owned[slot];
    public int Stacks(RunItem item) => stacks[(int)item];
    public void ChooseStarter(int slot) { if(slot>=0 && slot<WeaponCatalog.Count && EquippedCount<WeaponSlotCount) owned[slot]=true; }
    public WeaponSlot Weapon(int slot) => slot==0?Gun:slot==1?Machete:slot==2?Flame:extra[slot-3];
    public bool CanEvolve(int weaponId)
    {
        if(!HasWeapon(weaponId)) return false;
        var slot=Weapon(weaponId);
        if(slot.Level<slot.LevelLimit || slot.Evolved) return false;
        if(weaponId==0) return Stacks(RunItem.ArmorPiercing)>0 || Stacks(RunItem.SplitCore)>0;
        if(weaponId==1) return Stacks(RunItem.Whetstone)>0 || Stacks(RunItem.BloodPendant)>0;
        return false;
    }
    public bool IsEvolved(int weaponId) => HasWeapon(weaponId) && Weapon(weaponId).Evolved;
    public bool Evolve(int weaponId)
    {
        if(!CanEvolve(weaponId)) return false;
        Weapon(weaponId).Evolve();
        return true;
    }
    public int Level(int item) => WeaponCatalog.Passive(item) ? stacks[ItemLibrary.Index(item)] : WeaponCatalog.WeaponId(item)>=0 ? (HasWeapon(WeaponCatalog.WeaponId(item)) ? (IsEvolved(WeaponCatalog.WeaponId(item)) ? 3 : Weapon(WeaponCatalog.WeaponId(item)).Level-1) : -1) : levels[item];
    public int MaxLevel(int item) => WeaponCatalog.Passive(item) ? ItemLibrary.At(item).MaxStacks : WeaponCatalog.WeaponId(item)>=0 ? (Weapon(WeaponCatalog.WeaponId(item)).LevelLimit-1 + (CanEvolve(WeaponCatalog.WeaponId(item)) || IsEvolved(WeaponCatalog.WeaponId(item)) ? 1 : 0)) : 3;
    public int Price(int item) => WeaponCatalog.Passive(item) ? ItemLibrary.At(item).Price+Level(item)*10 : WeaponCatalog.WeaponId(item)>=0 ? (CanEvolve(WeaponCatalog.WeaponId(item)) ? 40 : 20+System.Math.Max(0,Level(item))*5) : (item==0?12:item==1?15:10)+Level(item)*5;
    public float FireMultiplier => 1f/(1f+levels[0]*0.2f);
    public float RangeMultiplier => 1f+levels[1]*0.15f;
    public float SpeedMultiplier => 1f+levels[2]*0.1f;
    public float CritChance(bool melee) => System.Math.Min(0.8f,0.05f+Stacks(RunItem.Whetstone)*(melee?0.2f:0.1f));
    public float BurnMultiplier => 1f+Stacks(RunItem.ConcentratedFuel)*0.4f;
    public float BurnDuration => 2f+Stacks(RunItem.ConcentratedFuel);
    public float PoisonSprayInterval => Stacks(RunItem.SlowReleaseValve)>0?1.0f:.8f;
    public float PoisonPoolDuration => 3+(Weapon(10).Level-1)*.5f+Stacks(RunItem.SlowReleaseValve)*3;
    public float PickupRange => 3.5f+Stacks(RunItem.MagneticCoil)*1.5f;
    public float DangerMultiplier => 1f+Stacks(RunItem.DangerContract)*0.2f;
    public void Collect(int amount) { if(amount>0) Materials+=amount; }
    public bool TrySpend(int amount) { if(amount<0 || Materials<amount) return false; Materials-=amount; return true; }
    public int Interest(int wave,int finalWave=20,bool endless=false) => Stacks(RunItem.SavingsJar)>0 && wave>1 && (endless || wave<finalWave) ? System.Math.Min(40,Materials/5) : 0;
    public bool Buy(int item, int replace=-1)
    {
        int weapon=WeaponCatalog.WeaponId(item);
        bool full=weapon>=0 && !HasWeapon(weapon) && EquippedCount>=WeaponSlotCount;
        if(full && !HasWeapon(replace)) return false;
        if(item<0 || item>=ItemCount || Level(item)>=MaxLevel(item) || !TrySpend(Price(item))) return false;
        if(WeaponCatalog.Passive(item)) stacks[ItemLibrary.Index(item)]++;
        else if(weapon>=0)
        {
            if(full) { owned[replace]=false; Weapon(replace).Reset(); }
            if(owned[weapon])
            {
                if(CanEvolve(weapon)) Weapon(weapon).Evolve();
                else Weapon(weapon).Upgrade();
            }
            else owned[weapon]=true;
        }
        else levels[item]++;
        return true;
    }
    public string Tags
    {
        get {
            string tags=(HasAmmo?"Gun ":"")+(HasMelee?"Melee ":"")+(HasBurn?"Fire ":"")+(HasWeapon(10)?"Poison ":"");
            for(int i=0;i<stacks.Length;i++) if(stacks[i]>0) tags+=ItemLibrary.All[i].Tags+" ";
            return tags;
        }
    }
    public void Reset()
    {
        Materials=0; Gun.Reset(); Machete.Reset(); Flame.Reset();
        foreach(WeaponSlot weapon in extra) weapon.Reset();
        for(int i=0;i<3;i++) levels[i]=0;
        for(int i=0;i<owned.Length;i++) owned[i]=false;
        for(int i=0;i<stacks.Length;i++) stacks[i]=0;
    }
}
