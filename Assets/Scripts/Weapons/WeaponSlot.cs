public sealed class WeaponSlot
{
    public enum WeaponType { Gun, Machete, Flamethrower, Spear, Crossbow, Earthbreaker, Icicle, Torch, Fireball, Boomerang, PoisonBackpack, BaitLantern }
    public WeaponType Type { get; }
    public int Level { get; private set; } = 1;
    public const int MaxLevel = 3;
    public int LevelLimit => Type==WeaponType.Crossbow?4:MaxLevel;
    public WeaponSlot(WeaponType type) => Type = type;
    public int Damage(int baseDamage) => Type==WeaponType.BaitLantern?baseDamage+(Level-1)*8:baseDamage + (Type==WeaponType.Crossbow?0:(Level - 1) * (Type==WeaponType.Flamethrower || Type==WeaponType.PoisonBackpack?1:Type==WeaponType.Torch || Type==WeaponType.Machete?2:5));
    public float Interval(float baseInterval) => Type==WeaponType.BaitLantern?baseInterval-(Level-1):baseInterval * (Type==WeaponType.Crossbow || Type==WeaponType.Torch || Type==WeaponType.PoisonBackpack?1f:(1f - (Level - 1) * 0.1f));
    public float Range(float baseRange) => Type==WeaponType.PoisonBackpack?baseRange+(Level-1)*.15f:baseRange * (Type==WeaponType.Torch?1f:1f + (Level - 1) * 0.1f);
    public bool Evolved { get; private set; }
    public void Evolve() => Evolved = true;
    public bool Upgrade()
    {
        if (Level >= LevelLimit) return false;
        Level++;
        return true;
    }
    public void Reset() { Level = 1; Evolved = false; }
}
