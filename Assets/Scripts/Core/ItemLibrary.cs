public enum RunItem
{
    ArmorPiercing, Ricochet, IncendiaryRounds, SplitCore, Fuse, ConcentratedFuel, CharBomb,
    Whetstone, BloodPendant, RevengeCharm, EmergencyShield, FirstAid, SavingsJar,
    HuntingBadge, DangerContract, MagneticCoil, SlowReleaseValve
}

public sealed class ItemDefinition
{
    public readonly string Name, Description, Tags, Synergy;
    public readonly int Price, MaxStacks, UnlockWave;
    public ItemDefinition(string name,string description,int price,int max,int wave,string tags,string synergy)
    { Name=name; Description=description; Price=price; MaxStacks=max; UnlockWave=wave; Tags=tags; Synergy=synergy; }
}

/// <summary>The single source of truth for shop stock, codex text and stacking limits.</summary>
public static class ItemLibrary
{
    public const int Offset=6;
    public const int Count=17;
    // Preserve the original item/weapon shop IDs; new passives follow the eleven weapons.
    public static int ShopId(int index)=>index<16?index+Offset:index+14;
    public static int Index(int shopId)=>shopId<22?shopId-Offset:shopId-14;
    public static bool IsShopItem(int shopId)=>(shopId>=6 && shopId<22) || (shopId>=30 && shopId<30+Count-16);
    public static readonly ItemDefinition[] All = {
        new ItemDefinition("Armor-Piercing Rounds","+1 pierce. Damage drops 25% after each pierced target.",18,3,1,"Gun","Ricochet / Split Core"),
        new ItemDefinition("Ricochet Chip","+1 bounce to a nearby target. Each bounce retains 50% damage.",30,2,4,"Gun","Incendiary Rounds / Hunting Badge"),
        new ItemDefinition("Incendiary Rounds","+25% chance for a direct bullet hit to ignite.",20,3,1,"Gun Fire","Fuse / Char Bomb"),
        new ItemDefinition("Split Core","Direct bullet kills launch 2 fragments at 35% damage. Fragments cannot split or trigger items.",70,1,11,"Gun","Armor-Piercing Rounds / Ricochet Chip"),
        new ItemDefinition("Fuse","Fire spreads to 2 nearby targets per stack. Spread flames cannot spread again.",25,2,5,"Fire","Incendiary Rounds / Char Bomb"),
        new ItemDefinition("Concentrated Fuel","+40% burn damage, +1s burn duration; +15% breath cooldown.",24,3,2,"Fire","Fuse / Char Bomb"),
        new ItemDefinition("Char Bomb","Burning enemies explode on death: 15 damage in 2.5m. Item explosions cannot trigger more item effects.",65,1,10,"Fire","Fuse / Concentrated Fuel"),
        new ItemDefinition("Whetstone","+10% critical chance; melee gets another +10%. Critical hits deal double damage.",18,3,1,"Melee Crit","Blood Pendant / Hunting Badge"),
        new ItemDefinition("Blood Pendant","Melee hits have +15% chance to heal 1 HP. Shared 0.25s cooldown.",28,3,4,"Melee","Whetstone / Revenge Charm"),
        new ItemDefinition("Revenge Charm","Taking damage grants +20% damage for 5s. Hits refresh duration, not strength.",25,3,3,"Melee Survival","Blood Pendant / First Aid Kit"),
        new ItemDefinition("Emergency Shield","Block the first hit of each wave.",25,1,1,"Survival","First Aid Kit / Danger Contract"),
        new ItemDefinition("First Aid Kit","Once per wave, heal 25 HP after surviving a hit at 30 HP or lower.",35,1,6,"Survival","Emergency Shield / Revenge Charm"),
        new ItemDefinition("Savings Jar","From wave 2, gain 20% of saved materials, capped at 40. No interest on the final campaign wave; continues in endless mode.",30,1,1,"Economy","Hunting Badge / Danger Contract"),
        new ItemDefinition("Hunting Badge","Critical kills have +30% chance to drop one extra material.",30,2,4,"Crit Economy","Whetstone / Magnetic Coil"),
        new ItemDefinition("Danger Contract","+20% spawn rate and enemy cap; +25% chance of extra material per kill.",20,2,2,"Economy","Savings Jar / Magnetic Coil"),
        new ItemDefinition("Magnetic Coil","+1.5m pickup range. Every 15 collected materials charges a 12-damage lightning chain for the next direct hit (3 targets).",35,2,5,"Economy Gun","Hunting Badge / Danger Contract"),
        new ItemDefinition("Slow-release Valve","Poison pools last 3s longer, but spray every 1.0s instead of 0.8s. Damage and overlap rules stay the same.",30,1,4,"Poison","Poison Backpack / Frost Field")
    };
    public static ItemDefinition Get(RunItem item) => All[(int)item];
    public static ItemDefinition At(int shopId) => All[Index(shopId)];
}
