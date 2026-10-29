using System;

/// <summary>Round progression independent of rendering and frame rate.</summary>
public sealed class WaveSession
{
    public enum Phase { Combat, Rest, Victory, Defeat }
    public const int TotalWaves = 20;
    public WaveRules Rules { get; private set; }
    public bool Endless=>Rules.Endless;
    public int FinalWave=>Rules.SafeFinalWave;
    public double WaveDuration { get; private set; }
    public int Wave { get; private set; }
    public double Remaining { get; private set; }
    public Phase State { get; private set; }
    public bool IsBossWave=>Rules.FinalBoss && !Endless && Wave>=FinalWave && Rules.TestMode==EnemyTestMode.Mixed;

    public WaveSession(WaveRules rules=null) { Configure(rules??new WaveRules()); Reset(); }
    public void Configure(WaveRules rules) { Rules=(rules??new WaveRules()).Copy(); }

    public void Reset()
    {
        Wave = Rules.SafeStartWave;
        WaveDuration=Rules.Duration(Wave);
        Remaining = WaveDuration;
        State = Phase.Combat;
    }

    public bool Advance(double seconds)
    {
        if (State != Phase.Combat || seconds <= 0 || double.IsNaN(seconds)) return false;
        Remaining = Math.Max(0, Remaining - seconds);
        if(IsBossWave) return false;
        if (Remaining > 0) return false;
        State = !Endless && Wave >= FinalWave ? Phase.Victory : Phase.Rest;
        return true;
    }

    public bool Continue()
    {
        if (State != Phase.Rest) return false;
        Wave=Math.Min(int.MaxValue-1,Wave)+1;
        WaveDuration=Rules.Duration(Wave);
        Remaining = WaveDuration;
        State = Phase.Combat;
        return true;
    }

    public bool CompleteBoss()
    {
        if(State!=Phase.Combat || !IsBossWave) return false;
        Remaining=0; State=Phase.Victory; return true;
    }

    public void Defeat()
    {
        if (State == Phase.Combat) State = Phase.Defeat;
    }
}
