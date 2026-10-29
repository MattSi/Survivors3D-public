/// <summary>Actual health removed/restored, per wave. Settlement removals are not combat kills.</summary>
public sealed class CombatMetrics
{
    public enum Source { Blades, Hammer, Other, Poison, Frost }
    public sealed class Totals
    {
        public int Hits { get; internal set; }
        public int Damage { get; internal set; }
        public int Kills { get; internal set; }
    }
    private readonly Totals[] sources={new Totals(),new Totals(),new Totals(),new Totals(),new Totals()};
    public Totals For(Source source)=>sources[(int)source];
    public float Seconds { get; private set; }
    public int DamageTaken { get; private set; }
    public int HitsTaken { get; private set; }
    public int LifeStolen { get; private set; }
    public int FirstAidHealing { get; private set; }
    public int BladeHitsDuringStun { get; private set; }
    public int BladeDamageDuringStun { get; private set; }
    public int PoisonDamageWhileSlowed { get; private set; }
    public void PoisonSlowDamage(int damage)=>PoisonDamageWhileSlowed+=System.Math.Max(0,damage);
    public void Advance(float dt) { if(dt>0 && !float.IsInfinity(dt)) Seconds+=dt; }
    public void Hit(Source source,int damage,bool killed,bool stunned)
    {
        var row=For(source); row.Hits++; row.Damage+=System.Math.Max(0,damage); if(killed) row.Kills++;
        if(source==Source.Blades && stunned) { BladeHitsDuringStun++; BladeDamageDuringStun+=System.Math.Max(0,damage); }
    }
    public void Hurt(int damage) { if(damage>0) {HitsTaken++; DamageTaken+=damage;} }
    public void Heal(int actual,bool firstAid=false)
    { if(firstAid) FirstAidHealing+=System.Math.Max(0,actual); else LifeStolen+=System.Math.Max(0,actual); }
    public void Reset()
    {
        foreach(var row in sources) { row.Hits=row.Damage=row.Kills=0; }
        Seconds=0; DamageTaken=HitsTaken=LifeStolen=FirstAidHealing=BladeHitsDuringStun=BladeDamageDuringStun=0;
        PoisonDamageWhileSlowed=0;
    }
}
