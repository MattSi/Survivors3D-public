using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameFlowController
{
    private readonly CombatController combat;
    private readonly RunState run;
    private GameUI ui;
    internal int frontScreen,phaseChangedFrame,replacementOffer=-1;
    internal bool choosingStarter,victoryAwaitingInput;
    internal bool Paused { get; private set; }
    private float pausePreviousScale=1;
    internal void TogglePause()
    {
        if(Paused) { ResumeGame(); return; }
        if(frontScreen!=0 || choosingStarter || ui.codexOpen || session.State!=WaveSession.Phase.Combat) return;
        pausePreviousScale=Time.timeScale; Paused=true; Time.timeScale=0;
        combat.SetPlayerEnabled(false); ui.pauseSelection=0; MarkInputHandled();
        GameAudio.Play(AudioCue.Back);
    }
    internal void ResumeGame()
    {
        if(!Paused) return;
        Paused=false; Time.timeScale=pausePreviousScale;
        GameAudio.Play(AudioCue.Confirm);
        combat.SetPlayerEnabled(session.State==WaveSession.Phase.Combat); MarkInputHandled();
    }
    internal void StopRun(bool mainMenu)
    {
        ResumeGame(); Restart();
        if(mainMenu) OpenMainMenu();
    }
    internal bool ShowingVictory=>victoryAwaitingInput;
    internal bool ShowingShop=>!ShowingVictory&&(session.State==WaveSession.Phase.Rest||session.State==WaveSession.Phase.Victory);
    private WaveSession session=>run.Session;
    private RunEquipment equipment=>run.Equipment;
    private WaveShop shop=>run.Shop;
    public GameFlowController(CombatController combat,RunState run) { this.combat=combat; this.run=run; }
    internal void Attach(GameUI ui) { this.ui=ui; }
    internal void RequestVictoryContinue()=>combat.RequestVictoryContinue();
    internal void MarkInputHandled()=>phaseChangedFrame=Time.frameCount;
    internal void CancelReplacement() { replacementOffer=-1; MarkInputHandled(); GameAudio.Play(AudioCue.Back); }
    internal void ToggleLock(int index) { if(ShowingShop && session.State==WaveSession.Phase.Rest && index>=0 && index<shop.Count && !shop.Sold(index)) { shop.ToggleLock(index); GameAudio.Play(AudioCue.Toggle); } }
    internal void Tick(float dt)
    {
        if(ui.UpdateMenus()) return;
        if(session.State==WaveSession.Phase.Defeat) combat.CullExpiredPickups();
        if(session.State!=WaveSession.Phase.Combat)
        {
            if(ShowingVictory)
            {
                if(Time.frameCount>phaseChangedFrame && ui.victoryConfirmAction.WasPressedThisFrame()) RequestVictoryContinue();
                combat.UpdateFloatingScores(dt); combat.UpdateImpacts(dt); combat.UpdateVictoryLoot(dt); return;
            }
            if(ShowingShop) ui.NavigateShop();
            if(ui.restartAction.WasPressedThisFrame()) ConfirmPhase();
            return;
        }
        run.SurvivalTime+=session.IsBossWave?dt:Mathf.Min(dt,(float)session.Remaining);
        if(session.Advance(dt)) { CompleteWave(); return; }
        combat.TickCombat(dt);
        // Finish outside weapon iterations: settlement synchronously clears combat collections.
        if(combat.BossDefeated && session.CompleteBoss()) CompleteWave();
    }
    internal void Restart()
    {
        ResumeGame();
        frontScreen=0;
        if(ui.codexOpen) ui.ToggleCodex();
        victoryAwaitingInput=false; replacementOffer=-1;
        run.Reset(combat.LoadWaveRules()); combat.ResetArena();
        choosingStarter=true; ui.starterSelection=0;
        GameAudio.Current?.SetMusic(0);
        combat.SetPlayerEnabled(false);
        phaseChangedFrame=Time.frameCount;
    }

    internal void OnDeath()
    {
        session.Defeat();
        phaseChangedFrame = Time.frameCount;
        combat.ShowDefeat();
    }

    internal void CompleteWave()
    {
        if(ShowingVictory || session.State==WaveSession.Phase.Combat || session.State==WaveSession.Phase.Defeat) return;
        replacementOffer=-1;
        combat.FinishWave();
        victoryAwaitingInput = true;
        ui.shopSelection = shop.Count + 1;
        ui.navigateAt = Time.unscaledTime + 0.25f;
        phaseChangedFrame = Time.frameCount;
    }

    internal void ConfirmPhase()
    {
        if (ShowingVictory || Time.frameCount <= phaseChangedFrame) return;
        if (session.State == WaveSession.Phase.Victory && ui.shopSelection != shop.Count + 1) return;
        if (session.State == WaveSession.Phase.Rest && ui.shopSelection <= shop.Count)
        {
            if (ui.shopSelection == shop.Count) RefreshShop();
            else BuyEquipment(ui.shopSelection);
            return;
        }
        if (session.Continue()) combat.StartWave();
        else if (session.State == WaveSession.Phase.Victory || session.State == WaveSession.Phase.Defeat) Restart();
    }

    internal void BeginRun(int weaponSlot)
    {
        if(frontScreen!=0 || !choosingStarter || weaponSlot<0 || weaponSlot>=WeaponCatalog.StarterCount || ui.codexOpen) return;
        equipment.ChooseStarter(weaponSlot); choosingStarter=false;
        combat.ApplyEquipment(); combat.StartWave(); phaseChangedFrame=Time.frameCount;
    }

    internal void NewGame()
    {
        Restart(); frontScreen=0;
    }

    internal void OpenMainMenu()
    {
        if(ui.codexOpen) ui.ToggleCodex();
        frontScreen=1; ui.frontSelection=0; combat.SetPlayerEnabled(false);
        GameAudio.Current?.SetMusic(0);
        phaseChangedFrame=Time.frameCount;
    }

    internal void OpenSettings()
    {
        frontScreen=2; ui.frontSelection=0; phaseChangedFrame=Time.frameCount;
    }

    internal void CloseSettings()
    {
        combat.SaveCameraPreferences(); GameAudio.Current?.SavePreferences();
        GameAudio.Play(AudioCue.Back);
        OpenMainMenu(); ui.frontSelection=1;
    }

    internal void QuitGame()
    {
        combat.SaveCameraPreferences();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }

    internal void EnterVictoryShop()
    {
        if(!ShowingVictory || !combat.VictoryReady) return;
        victoryAwaitingInput=false; combat.ClearCombatObjects(); shop.Open(session.Wave);
        ui.shopSelection=shop.Count+1; phaseChangedFrame=Time.frameCount; ui.navigateAt=Time.unscaledTime+.25f;
    }

    internal void BuyEquipment(int item)
    {
        if (ShowingVictory || session.State != WaveSession.Phase.Rest || Time.frameCount <= phaseChangedFrame) return;
        if(item<0 || item>=shop.Count || shop.Sold(item) || shop.Item(item)<0 || equipment.Materials<shop.Price(item))
        { GameAudio.Play(AudioCue.Error); return; }
        int weapon=WeaponCatalog.WeaponId(shop.Item(item));
        if(weapon>=0 && !equipment.HasWeapon(weapon) && equipment.EquippedCount>=RunEquipment.WeaponSlotCount)
        { replacementOffer=item; ui.replacementSelection=0; ui.navigateAt=Time.unscaledTime+.2f; phaseChangedFrame=Time.frameCount; return; }
        if (shop.Buy(item)) { combat.ApplyEquipment(); GameAudio.Play(AudioCue.Purchase); }
        else GameAudio.Play(AudioCue.Error);
    }

    internal void RefreshShop()
    {
        if (ShowingVictory || session.State != WaveSession.Phase.Rest || Time.frameCount <= phaseChangedFrame) return;
        if (shop.Refresh()) { ui.shopSelection = shop.Count; GameAudio.Play(AudioCue.Refresh); }
        else GameAudio.Play(AudioCue.Error);
    }

    internal void FinishReplacement(int weapon)
    {
        if(replacementOffer<0) return;
        if(shop.Buy(replacementOffer,weapon)) { combat.ClearNewWeaponState(); combat.ApplyEquipment(); GameAudio.Play(AudioCue.Purchase); }
        else GameAudio.Play(AudioCue.Error);
        replacementOffer=-1; phaseChangedFrame=Time.frameCount;
    }
}
