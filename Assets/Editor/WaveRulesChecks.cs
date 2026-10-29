using System;

public static class WaveRulesChecks
{
    static void Check(bool condition,string message) {if(!condition) throw new Exception("WAVE_CONFIG_CHECK: "+message);}
    public static void Run()
    {
        var rules=new WaveRules();
        for(int wave=1;wave<=20;wave++)
        {
            var row=rules.At(wave);
            Check(row.InitialEnemies==4+(wave-1)*2 && row.EnemyLimit==14+(wave-1)*4,"Default population matches the previous twenty-wave balance");
            Check(Math.Abs(row.SpawnRate-(2f/3f+(wave-1)*.18f))<.0001,"Default spawn rate remains continuous");
            for(int ordinal=1;ordinal<=100;ordinal++)
            {
                EnemyKind expected=wave>=3 && ordinal%(wave<8?6:5)==0?EnemyKind.Charger:
                    wave>=5 && ordinal%7==0?EnemyKind.StoneArmor:wave>=8 && ordinal%9==0?EnemyKind.Bomber:
                    wave>=11 && ordinal%11==0?EnemyKind.Splitter:wave>=6 && ordinal%8==0?EnemyKind.Shooter:EnemyKind.Normal;
                Check(rules.SelectEnemy(wave,ordinal,0,0,0)==expected,"Default unlocks retain existing priority and append the shooter");
            }
        }
        var session=new WaveSession(new WaveRules {FinalWave=3,StartWave=2,FinalBoss=false});
        session.Advance(session.Remaining); Check(session.State==WaveSession.Phase.Rest && session.Continue(),"Custom campaign rests before its final wave");
        session.Advance(session.Remaining); Check(session.State==WaveSession.Phase.Victory && !session.Continue(),"Custom final wave wins");
        rules.Endless=true; rules.StartWave=20; session=new WaveSession(rules);
        for(int i=0;i<80;i++) {session.Advance(session.Remaining); Check(session.State==WaveSession.Phase.Rest && session.Continue(),"Endless passes wave twenty without victory");}
        Check(session.Wave==100 && session.Remaining==60,"Endless keeps counting and inherits final duration");
        var late=rules.At(1000000);
        Check(late.InitialEnemies<=rules.MaxInitialEnemies && late.EnemyLimit<=240 && late.SpawnRate<=rules.MaxSpawnRate && late.EnemySpeed<=rules.MaxEnemySpeed,"Very late waves retain finite safe caps");
        Check(rules.SpawnRate(1000000,0,60,1.4f)<=rules.MaxSpawnRate,"Danger and within-wave acceleration respect the final spawn cap");
        rules.Waves=new[] {new WaveRow {Wave=10,Duration=70,HealthMultiplier=2},new WaveRow {Wave=1,Duration=12},new WaveRow {Wave=5,Duration=30}};
        Check(rules.Duration(6)==30 && rules.Duration(2)==12 && rules.Duration(11)==70,"Unsorted sparse rows resolve by wave number");
        rules.Waves=new[] {new WaveRow {Wave=1,Duration=float.NaN,SpawnRate=float.PositiveInfinity,InitialEnemies=-5,EnemyLimit=-3,HealthMultiplier=-9,EnemySpeed=-1}};
        late=rules.At(1); Check(late.Duration>0 && late.SpawnRate>0 && late.EnemyLimit>=1 && late.InitialEnemies==0 && late.HealthMultiplier==.1f && late.EnemySpeed>0,"Invalid numeric entries cannot break spawning or timers");
        rules.Waves=null; rules.Enemies=null; Check(rules.Duration(1)>0 && rules.SelectEnemy(8,9,0,0,0)==EnemyKind.Normal,"Missing tables have a safe normal-only fallback");
        rules=new WaveRules(); rules.TestMode=EnemyTestMode.BomberOnly;
        Check(rules.SelectEnemy(1,1,0,0,0)==EnemyKind.Bomber && rules.SelectEnemy(1,2,0,0,2)==EnemyKind.None,"Single-type testing bypasses unlocks while respecting live caps");
        Check(rules.AttackLimit(EnemyKind.Bomber,1)==2,"Testing before unlock still permits configured attacks");
        rules.TestMode=EnemyTestMode.NormalOnly; Check(rules.SelectEnemy(20,315,0,0,0)==EnemyKind.Normal,"Normal-only mode excludes every special enemy");
        rules.TestMode=EnemyTestMode.SplitterOnly;
        Check(rules.SelectEnemy(1,1,0,0,0,0)==EnemyKind.Splitter && rules.SelectEnemy(1,1,0,0,0,2)==EnemyKind.None,"Splitter-only mode bypasses unlock but caps mothers");
        rules.TestMode=EnemyTestMode.ShooterOnly;
        Check(rules.SelectEnemy(1,1,0,0,0,0,0)==EnemyKind.Shooter && rules.SelectEnemy(1,1,0,0,0,0,2)==EnemyKind.None,"Shooter-only mode bypasses unlock but retains its population cap");
        Check(rules.AttackLimit(EnemyKind.Shooter,1)==1 && rules.AttackLimit(EnemyKind.Shooter,13)==2,"Shooter attack budget follows its configured stage");
        rules.TestMode=EnemyTestMode.Mixed;
        rules.Enemies=new[] {new EnemyScheduleRow {Kind=EnemyKind.Charger,FromWave=1,Every=1},new EnemyScheduleRow {Kind=EnemyKind.Charger,FromWave=5,Enabled=false}};
        Check(rules.SelectEnemy(4,1,0,0,0)==EnemyKind.Charger && rules.SelectEnemy(5,1,0,0,0)==EnemyKind.Normal,"Later disabled phases stop that enemy type");
        rules=new WaveRules(); session=new WaveSession(rules); rules.Waves[0].Duration=7; rules.Enemies[0].Every=1;
        Check(session.WaveDuration==45 && session.Rules.Enemies[0].Every==6,"Active runs have deep copies of both tables");
        rules.StartWave=999; rules.FinalWave=3; Check(new WaveSession(rules).Wave==3,"Finite start wave is bounded by its final wave");
        var equipment=new RunEquipment(); equipment.ChooseStarter(0); equipment.Collect(1000); equipment.Buy(18);
        Check(equipment.Interest(20,20,true)==40 && equipment.Interest(20,20,false)==0,"Savings interest continues in endless and stops on finite final waves");
        rules=new WaveRules();
        foreach(EnemyKind kind in new[] {EnemyKind.Normal,EnemyKind.Charger,EnemyKind.StoneArmor,EnemyKind.Bomber,EnemyKind.Splitter,EnemyKind.Splitling,EnemyKind.Shooter})
            Check(rules.Health(kind,rules.At(1))==rules.Health(kind,rules.At(20)),"Default campaign keeps each enemy's own health across waves");
        Check(rules.Health(EnemyKind.StoneArmor,rules.At(1))==120,"Early stone test uses full stone health");
        rules.EnemyDefinitions[2].BaseHealth=201; rules.Waves[0].HealthMultiplier=1.5f;
        Check(rules.Health(EnemyKind.StoneArmor,rules.At(1))==302 && rules.Health(EnemyKind.Normal,rules.At(1))==15,"Enemy health and wave multipliers compose with rounding");
        var copy=rules.Copy(); rules.EnemyDefinitions[2].BaseHealth=1;
        Check(copy.BaseHealth(EnemyKind.StoneArmor)==201,"Enemy definitions are deep-copied into run snapshots");
        rules.EnemyDefinitions=new[] {new EnemyDefinition {Kind=EnemyKind.StoneArmor,BaseHealth=-10},new EnemyDefinition {Kind=EnemyKind.Splitling,BaseHealth=int.MaxValue}};
        Check(rules.Health(EnemyKind.StoneArmor,rules.At(1))>=1 && rules.Health(EnemyKind.Splitling,rules.At(1000000))==1000000,"Invalid and late-game health remain bounded");
        rules.EnemyDefinitions=null;
        Check(rules.BaseHealth(EnemyKind.StoneArmor)==120,"Missing definitions use type-specific defaults");
    }
}
