using System;

public enum EnemyKind { None=-1, Normal, Charger, StoneArmor, Bomber, Splitter, Splitling, Shooter }
public enum EnemyTestMode { Mixed, NormalOnly, ChargerOnly, StoneArmorOnly, BomberOnly, SplitterOnly, ShooterOnly }

[Serializable]
public sealed class WaveRow
{
    public int Wave=1,InitialEnemies=4,EnemyLimit=14;
    public float HealthMultiplier=1;
    public float Duration=45,SpawnRate=2f/3f,EnemySpeed=2.8f;
    public WaveRow Copy()=>(WaveRow)MemberwiseClone();
}
[Serializable]
public sealed class EnemyScheduleRow
{
    public EnemyKind Kind=EnemyKind.Charger;
    public int FromWave=3,Every=6,MaxAlive=2,MaxAttacks=1;
    public bool Enabled=true;
    public EnemyScheduleRow Copy()=>(EnemyScheduleRow)MemberwiseClone();
}

/// <summary>Serializable, Unity-independent encounter rules. Each run uses a private copy.</summary>
[Serializable]
public sealed class WaveRules
{
    public bool Endless;
    public bool FinalBoss=true;
    public int BossHealth=3600;
    public int FinalWave=20,StartWave=1,StartingMaterials;
    public EnemyTestMode TestMode;
    public float EndOfWaveSpawnMultiplier=1.25f;
    public int MaxEnemies=240,MaxInitialEnemies=64;
    public float MaxSpawnRate=12,MaxEnemySpeed=4.2f;
    public int ExtraInitialPerWave=2,ExtraLimitPerWave=4;
    public float ExtraHealthMultiplierPerWave=.1f;
    public float ExtraSpawnRatePerWave=.18f,ExtraSpeedPerWave;
    public WaveRow[] Waves=DefaultWaves();
    public EnemyScheduleRow[] Enemies=DefaultEnemies();
    public EnemyDefinition[] EnemyDefinitions=EnemyDefinition.Defaults();

    static WaveRow[] DefaultWaves()
    {
        var rows=new WaveRow[20];
        for(int i=0;i<rows.Length;i++) rows[i]=new WaveRow {Wave=i+1,Duration=i<3?45:i<7?50:i<12?55:60,
            InitialEnemies=4+i*2,EnemyLimit=14+i*4,SpawnRate=2f/3f+i*.18f,EnemySpeed=Math.Min(4.2f,2.8f+i*.1f)};
        return rows;
    }
    static EnemyScheduleRow Stage(EnemyKind kind,int wave,int every,int alive,int attacks)=>
        new EnemyScheduleRow {Kind=kind,FromWave=wave,Every=every,MaxAlive=alive,MaxAttacks=attacks};
    static EnemyScheduleRow[] DefaultEnemies()=>new[] {
        Stage(EnemyKind.Charger,3,6,2,1),Stage(EnemyKind.Charger,8,5,3,2),Stage(EnemyKind.Charger,13,5,4,2),
        Stage(EnemyKind.StoneArmor,5,7,2,0),Stage(EnemyKind.StoneArmor,10,7,3,0),Stage(EnemyKind.StoneArmor,15,7,4,0),
        Stage(EnemyKind.Bomber,8,9,2,2),Stage(EnemyKind.Bomber,13,9,3,3),
        Stage(EnemyKind.Splitter,11,11,2,0),Stage(EnemyKind.Splitter,16,11,3,0),
        Stage(EnemyKind.Shooter,6,8,2,1),Stage(EnemyKind.Shooter,13,8,3,2)};
    public WaveRules Copy()
    {
        var result=(WaveRules)MemberwiseClone();
        result.Waves=Array.ConvertAll(Waves??Array.Empty<WaveRow>(),row=>row?.Copy());
        result.Enemies=Array.ConvertAll(Enemies??Array.Empty<EnemyScheduleRow>(),row=>row?.Copy());
        result.EnemyDefinitions=Array.ConvertAll(EnemyDefinitions??Array.Empty<EnemyDefinition>(),row=>row?.Copy());
        return result;
    }
    public int SafeFinalWave=>Math.Max(1,Math.Min(1000000,FinalWave));
    public int SafeStartWave=>Math.Max(1,Math.Min(Endless?1000000:SafeFinalWave,StartWave));
    public int PopulationLimit=>Math.Max(1,Math.Min(240,MaxEnemies));
    static int IntLimit(double value,int max)=> (int)Math.Max(0,Math.Min(max,value));
    static float Number(float value,float fallback,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));
    public WaveRow At(int wave)
    {
        wave=Math.Max(1,wave);
        WaveRow selected=null,first=null; int lastWave=0;
        foreach(var row in Waves??Array.Empty<WaveRow>())
        {
            if(row==null || row.Wave<1) continue;
            if(first==null || row.Wave<first.Wave) first=row;
            lastWave=Math.Max(lastWave,row.Wave);
            if(row.Wave<=wave && (selected==null || row.Wave>=selected.Wave)) selected=row;
        }
        var value=(selected??first??new WaveRow()).Copy();
        // Missing middle waves reuse the previous row; waves beyond the table grow from its last row.
        int extra=lastWave>0?Math.Max(0,wave-lastWave):0;
        value.Wave=wave;
        value.Duration=Number(value.Duration,45,1,3600);
        value.EnemyLimit=Math.Max(1,IntLimit((double)value.EnemyLimit+(double)extra*Math.Max(0,ExtraLimitPerWave),PopulationLimit));
        value.InitialEnemies=IntLimit((double)value.InitialEnemies+(double)extra*Math.Max(0,ExtraInitialPerWave),Math.Min(value.EnemyLimit,Math.Max(0,MaxInitialEnemies)));
        value.HealthMultiplier=Number(value.HealthMultiplier,1,.1f,1000)+extra*Number(ExtraHealthMultiplierPerWave,0,0,100);
        value.HealthMultiplier=Number(value.HealthMultiplier,1,.1f,1000);
        value.SpawnRate=Number(value.SpawnRate+extra*Number(ExtraSpawnRatePerWave,0,0,100),2f/3f,.05f,Number(MaxSpawnRate,12,.05f,30));
        value.EnemySpeed=Number(value.EnemySpeed+extra*Number(ExtraSpeedPerWave,0,0,10),2.8f,.1f,Number(MaxEnemySpeed,4.2f,.1f,10));
        return value;
    }
    public double Duration(int wave)=>At(wave).Duration;
    public int BaseHealth(EnemyKind kind)
    {
        int health=EnemyDefinition.DefaultHealth(kind);
        foreach(var row in EnemyDefinitions??Array.Empty<EnemyDefinition>())
            if(row!=null && row.Kind==kind) health=row.BaseHealth;
        return Math.Max(1,Math.Min(1000000,health));
    }
    public int Health(EnemyKind kind,WaveRow wave)=>
        (int)Math.Ceiling(Math.Min(1000000,BaseHealth(kind)*Number(wave?.HealthMultiplier??1,1,.1f,1000)));
    public float SpawnRate(int wave,double remaining,double duration,float danger)
    {
        float progress=(float)Math.Max(0,Math.Min(1,1-remaining/Math.Max(1,duration)));
        return Number(At(wave).SpawnRate*(1+(Number(EndOfWaveSpawnMultiplier,1.25f,1,3)-1)*progress)*Number(danger,1,1,3),
            1,.05f,Number(MaxSpawnRate,12,.05f,30));
    }
    public EnemyScheduleRow Schedule(EnemyKind kind,int wave,bool includeLocked=false)
    {
        EnemyScheduleRow selected=null,first=null;
        foreach(var row in Enemies??Array.Empty<EnemyScheduleRow>())
        {
            if(row==null || row.Kind!=kind || row.FromWave<1) continue;
            if(first==null || row.FromWave<first.FromWave) first=row;
            if(row.FromWave<=wave && (selected==null || row.FromWave>=selected.FromWave)) selected=row;
        }
        return selected??(includeLocked?first:null);
    }
    public int AttackLimit(EnemyKind kind,int wave)
    {
        var row=Schedule(kind,wave,TestMode!=EnemyTestMode.Mixed);
        return row!=null && row.Enabled?Math.Max(0,Math.Min(PopulationLimit,row.MaxAttacks)):0;
    }
    public EnemyKind SelectEnemy(int wave,int ordinal,int chargers,int stones,int bombers,int splitters=0,int shooters=0)
    {
        if(TestMode==EnemyTestMode.NormalOnly) return EnemyKind.Normal;
        EnemyKind forced=TestMode==EnemyTestMode.ChargerOnly?EnemyKind.Charger:TestMode==EnemyTestMode.StoneArmorOnly?EnemyKind.StoneArmor:TestMode==EnemyTestMode.BomberOnly?EnemyKind.Bomber:TestMode==EnemyTestMode.SplitterOnly?EnemyKind.Splitter:TestMode==EnemyTestMode.ShooterOnly?EnemyKind.Shooter:EnemyKind.None;
        for(int i=1;i<=(int)EnemyKind.Shooter;i++)
        {
            var kind=(EnemyKind)i;
            if(kind==EnemyKind.Splitling) continue; // Offspring never arrive through portals.
            if(forced!=EnemyKind.None && kind!=forced) continue;
            var row=Schedule(kind,wave,forced!=EnemyKind.None);
            int alive=kind==EnemyKind.Charger?chargers:kind==EnemyKind.StoneArmor?stones:kind==EnemyKind.Bomber?bombers:kind==EnemyKind.Splitter?splitters:shooters;
            if(row==null || !row.Enabled || alive>=Math.Max(0,Math.Min(PopulationLimit,row.MaxAlive))) continue;
            if(forced!=EnemyKind.None || (ordinal>0 && ordinal%Math.Max(1,row.Every)==0)) return kind;
        }
        return forced!=EnemyKind.None?EnemyKind.None:EnemyKind.Normal;
    }
}
