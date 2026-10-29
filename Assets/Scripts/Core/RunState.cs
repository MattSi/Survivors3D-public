/// <summary>Authoritative data for one run; rule objects remain independent of presentation.</summary>
public sealed class RunState
{
    public WaveSession Session { get; }=new WaveSession();
    public RunEquipment Equipment { get; }=new RunEquipment();
    public WaveShop Shop { get; private set; }
    public int Kills { get; internal set; }
    public int LootScore { get; internal set; }
    public float SurvivalTime { get; internal set; }
    public int Score=>Kills*10+LootScore;
    public RunState() { Shop=new WaveShop(Equipment); }
    internal void Reset(WaveRules rules=null)
    {
        if(rules!=null) Session.Configure(rules);
        Equipment.Reset(); Shop.Reset(); Session.Reset();
        Equipment.Collect(System.Math.Max(0,System.Math.Min(1000000,Session.Rules.StartingMaterials)));
        Kills=LootScore=0; SurvivalTime=0;
    }
}
