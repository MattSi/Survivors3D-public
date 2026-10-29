using UnityEngine;

/// <summary>Composition root and scene-compatible entry point. No menu rendering or combat rules live here.</summary>
public sealed class SurvivorPlayground : CombatController
{
    public GameFlowController Flow { get; private set; }
    internal GameUI UI { get; private set; }
    public GameAudio Audio { get; private set; }
    private void Awake()
    {
        Audio=GetComponent<GameAudio>() ?? gameObject.AddComponent<GameAudio>();
        Run=new RunState();
        Flow=new GameFlowController(this,Run);
        UI=new GameUI(Flow,Run,this);
        Flow.Attach(UI);
        PlayerDied+=Flow.OnDeath;
        SettlementReady+=Flow.EnterVictoryShop;
    }
    private void Start() { InitializeCombat(); Flow.Restart(); Flow.OpenMainMenu(); }
    private void OnEnable() { if(Audio!=null) Audio.enabled=true; UI?.Enable(); }
    private void OnDisable() { UI?.Disable(); if(Audio!=null) Audio.enabled=false; }
    private void Update()=>Flow.Tick(Time.deltaTime);
    private void OnGUI()=>UI?.Render();
    private void OnDestroy()
    {
        if(Flow!=null) { PlayerDied-=Flow.OnDeath; SettlementReady-=Flow.EnterVictoryShop; }
        UI?.Dispose(); DisposeCombat();
    }
    // Retain the entry points used by scene builders and existing play-mode tools.
    public void Restart()=>Flow.Restart();
    public void NewGame()=>Flow.NewGame();
    public void BeginRun(int weaponSlot)=>Flow.BeginRun(weaponSlot);
    private bool ShowingVictory=>Flow.ShowingVictory;
    private int phaseChangedFrame { get=>Flow.phaseChangedFrame; set=>Flow.phaseChangedFrame=value; }
}
