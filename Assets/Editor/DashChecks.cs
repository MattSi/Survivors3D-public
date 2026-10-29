using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

[InputControlLayout(stateType=typeof(GamepadState))]
public class DashTestGamepad : Gamepad
{
    public float Low,High;
    public override void SetMotorSpeeds(float lowFrequency,float highFrequency) {Low=lowFrequency; High=highFrequency;}
}

public static class DashChecks
{
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static SurvivorPlayground game;
    static PlayerController player;
    static Keyboard keyboard;
    static DashTestGamepad pad;
    static double next;
    static int stage;
    static InputSettings originalInput,testInput;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static object Call(string name,params object[] args)=>typeof(CombatController).GetMethod(name,Flags).Invoke(game,args);
    static void PlayerCall(string name,params object[] args)=>typeof(PlayerController).GetMethod(name,Flags).Invoke(player,args);
    static void Check(bool ok,string message) {if(!ok) throw new Exception("DASH_CHECK: "+message);}
    // Editor callbacks in a hidden batch window otherwise select the editor input buffer.
    static void UpdateInput()=>typeof(InputSystem).GetMethod("Update",BindingFlags.Static|BindingFlags.NonPublic,null,new[] {typeof(InputUpdateType)},null)
        .Invoke(null,new object[] {InputUpdateType.Dynamic});
    static void KeyState(params Key[] keys) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); UpdateInput();}
    static void PadState(Vector2 stick,bool shoulder=false)
    {
        var state=new GamepadState {leftStick=stick};
        if(shoulder) state=state.WithButton(GamepadButton.LeftShoulder);
        InputSystem.QueueStateEvent(pad,state); UpdateInput();
    }
    static void Poll(float dt)=>PlayerCall("PollMovement",dt);
    static void Move(Vector3 move,bool dash,float dt)=>PlayerCall("TickMovement",move,dash,dt);
    static void Position(Vector3 at)
    {
        var controller=player.GetComponent<CharacterController>();
        controller.enabled=false; player.transform.SetPositionAndRotation(at,Quaternion.identity); controller.enabled=true;
        Physics.SyncTransforms();
    }
    static void Fresh()
    {
        Time.timeScale=1; player.enabled=true; PlayerCall("OnApplicationFocus",true);
        player.ResetDash(); player.SpeedMultiplier=1; game.Health.ResetHealth();
        Position(new Vector3(0,.5f,0));
        ((PlaygroundCamera)Get("viewCamera")).ThirdPerson=false;
        KeyState(); PadState(Vector2.zero); Poll(.001f);
    }
    [MenuItem("Tools/Survivors/Verify Short Dash")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("DashChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("DashChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Verify()
    {
        game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); player=(PlayerController)Get("player");
        originalInput=InputSystem.settings; testInput=UnityEngine.Object.Instantiate(originalInput);
        InputSystem.settings=testInput;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.updateMode=InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        keyboard=InputSystem.AddDevice<Keyboard>();
        InputSystem.RegisterLayout<DashTestGamepad>(); pad=InputSystem.AddDevice<DashTestGamepad>();
        var profile=ScriptableObject.CreateInstance<WaveConfiguration>(); profile.Rules.TestMode=EnemyTestMode.NormalOnly;
        object original=Get("waveConfiguration"); GameTestAccess.Set(game,"waveConfiguration",profile);
        try
        {
            game.NewGame(); game.BeginRun(0); game.enabled=false; Call("ClearCombatObjects"); Fresh();
            int balance=game.Run.Equipment.Materials,slots=game.Run.Equipment.EquippedCount;
            Check(player.DashCharges==2,"Starts with full 2 dash charges");
            KeyState(Key.D,Key.Space); Poll(.05f);
            Check(player.IsDashing && player.IsDashProtected && player.transform.position.x>.8f,
                $"Space starts an immediate directional dash (active={player.IsDashing}, protected={player.IsDashProtected}, position={player.transform.position}, input={player.MoveInput}, pressed={((InputAction)typeof(PlayerController).GetField("dashAction",Flags).GetValue(player)).IsPressed()}, cooldown={player.DashCooldownRemaining})");
            Check(player.DashCharges==1,"First dash consumes 1 charge, 1 charge remains");
            Check(game.Run.Equipment.Materials==balance && game.Run.Equipment.EquippedCount==slots,"Dash is free and consumes no weapon slot");
            Check(pad.Low==0 && pad.High==0,"Keyboard dash does not rumble an idle controller");
            Poll(.1f); Check(!player.IsDashing && Mathf.Abs(player.transform.position.x-2.5f)<.02f,$"Dash covers 2.5 metres in 0.15 seconds (active={player.IsDashing}, position={player.transform.position})");
            Check(player.IsDashProtected,"Final movement frame is protected");
            KeyState(Key.Space); Poll(.01f); Check(!player.IsDashProtected,"Protection expires on the next ordinary movement tick");
            float remaining=player.DashCooldownRemaining;
            Poll(remaining-.01f); Check(player.DashCooldownRemaining>0 && !player.IsDashing,"Cooldown is not ready early");
            Poll(.02f); Check(player.DashCooldownRemaining==0 && player.DashReadyFlash>0 && player.DashCharges==2,"Cooldown recovery flashes the HUD and restores to 2 charges");
            Poll(.1f); Check(!player.IsDashing,"Holding Space never repeats after cooldown");
            KeyState(); Poll(.01f); KeyState(Key.Space); Poll(.05f);
            Check(player.IsDashing && player.transform.position.x>3.3f,"A fresh press dashes along the last movement direction at rest");

            Fresh();
            KeyState(Key.D,Key.Space); Poll(.15f);
            Check(player.DashCharges==1 && !player.IsDashing,"First dash completed, 1 charge remains");
            KeyState(); Poll(.01f); KeyState(Key.D,Key.Space); Poll(.15f);
            Check(player.DashCharges==0 && !player.IsDashing,"Second dash completed, 0 charges remain");
            Check(Mathf.Abs(player.transform.position.x-5.0f)<.04f,$"Chained double dash covers 5.0 metres in total (position={player.transform.position.x})");

            // Invariant 1: 3rd press while at 0 charges is rejected (no movement, not dashing)
            Vector3 posBeforeThird=player.transform.position;
            KeyState(); Poll(.01f); KeyState(Key.Space); Poll(.05f);
            Check(player.DashCharges==0 && !player.IsDashing && (player.transform.position-posBeforeThird).sqrMagnitude<.0001f,
                "Third dash attempt at 0 charges is rejected without moving");

            // Invariant 2: Stepwise 0 -> 1 -> 2 recovery timing and HUD ready flash
            KeyState();
            float cd1=player.DashCooldownRemaining;
            Poll(cd1-.01f);
            Check(player.DashCharges==0 && player.DashCooldownRemaining>0,"Still at 0 charges before first cadence completes");
            Poll(.02f);
            Check(player.DashCharges==1 && player.DashReadyFlash>0 && player.DashCooldownRemaining>0,
                $"Charge recovers 0 -> 1 with flash and starts 2nd cooldown (charges={player.DashCharges}, flash={player.DashReadyFlash}, cd={player.DashCooldownRemaining})");
            float cd2=player.DashCooldownRemaining;
            Poll(cd2-.01f);
            Check(player.DashCharges==1 && player.DashCooldownRemaining>0,"Still at 1 charge before second cadence completes");
            Poll(.02f);
            Check(player.DashCharges==2 && player.DashCooldownRemaining==0 && player.DashReadyFlash>0,
                "Charge recovers 1 -> 2 with flash and cooldown resets to 0");

            // Invariant 3: Mid-recovery usage without corrupting cadence timer
            Fresh();
            KeyState(Key.D,Key.Space); Poll(.15f); KeyState();
            Check(player.DashCharges==1,"First dash leaves 1 charge");
            Poll(2.0f);
            float midCooldown=player.DashCooldownRemaining;
            Check(player.DashCharges==1 && midCooldown>1.0f && midCooldown<3.0f,
                $"Mid-recovery has 1 charge and partial cooldown (cd={midCooldown})");
            KeyState(Key.D,Key.Space); Poll(.15f); KeyState();
            Check(player.DashCharges==0,"Mid-recovery dash spent the 2nd charge");
            Check(player.DashCooldownRemaining<midCooldown && player.DashCooldownRemaining<3.0f,
                $"Mid-recovery dash does not reset cadence timer to full (cd={player.DashCooldownRemaining})");
            Poll(player.DashCooldownRemaining+.01f);
            Check(player.DashCharges==1 && player.DashCooldownRemaining>3.0f,
                "Cadence timer smoothly transitions to recovering the subsequent charge");
            Fresh(); player.SpeedMultiplier=3; Move(Vector3.right,true,1f);
            Check(Mathf.Abs(player.transform.position.x-2.5f)<.02f,"Long frames and movement upgrades cannot extend dash distance");
            Fresh(); Move(new Vector3(1,0,1),true,.15f);
            Vector3 offset=player.transform.position-new Vector3(0,.5f,0);
            Check(Mathf.Abs(offset.magnitude-2.5f)<.02f,"Diagonal dash has the same total distance");
            Fresh(); KeyState(Key.Space); Poll(.05f); Check(player.transform.position.z>.8f,"Before moving, idle dash uses the default forward direction");
            Fresh(); var camera=(PlaygroundCamera)Get("viewCamera"); camera.ThirdPerson=true;
            typeof(PlaygroundCamera).GetField("<Yaw>k__BackingField",Flags).SetValue(camera,90f);
            PadState(Vector2.up); Poll(.01f); float start=player.transform.position.x;
            PadState(Vector2.zero,true); Poll(.05f);
            Check(player.IsDashing && player.transform.position.x>start+.8f,"LB/L1 uses the last camera-relative world movement direction");
            Check(pad.Low>0 && pad.High>0,"Controller dash requests a small rumble pulse");
            typeof(PlaygroundCamera).GetField("<Yaw>k__BackingField",Flags).SetValue(camera,180f);
            float z=player.transform.position.z; Poll(.05f);
            Check(Mathf.Abs(player.transform.position.z-z)<.01f,"Rotating the camera does not bend an ongoing dash");
            typeof(PlayerController).GetField("rumbleUntil",Flags).SetValue(player,Time.unscaledTime-1);
            Poll(.01f); Check(pad.Low==0 && pad.High==0,"Rumble stops after its short pulse");
            Fresh(); PadState(Vector2.right,true); Poll(.05f); InputSystem.DisableDevice(pad);
            Check(pad.Low==0 && pad.High==0,"Disabling the controller stops owned rumble"); InputSystem.EnableDevice(pad);

            Fresh(); Move(Vector3.right,true,.04f); game.Health.ShieldCharges=1;
            Check(!game.Health.TakeDamage(8) && game.Health.Current==100 && game.Health.ShieldCharges==1,"Dash avoids damage without consuming the emergency shield");
            Call("ReleaseNeedle",player.transform.position+Vector3.right*2,Vector3.left); Call("UpdateEnemyNeedles",.5f);
            Check(game.Health.Current==100 && ((System.Collections.IList)Get("enemyNeedles")).Count==0,"Actual shooter projectile is consumed harmlessly during dash");
            Move(Vector3.zero,false,.2f); Move(Vector3.zero,false,.01f);
            Check(!game.Health.TakeDamage(8) && game.Health.ShieldCharges==0,"Shield still works after dash ends");
            player.ResetDash(); Check(game.Health.IsInvulnerable,"Resetting dash cannot erase damage/shield invulnerability");

            Fresh(); var wall=new GameObject("Dash Test Thin Wall");
            wall.transform.position=new Vector3(1.5f,1,0); wall.AddComponent<BoxCollider>().size=new Vector3(.08f,2,4);
            Physics.SyncTransforms(); Move(Vector3.right,true,.8f);
            Check(player.transform.position.x<1.4f && !player.IsDashing,"Swept movement cannot tunnel through a thin wall on long frames");
            wall.SetActive(false); UnityEngine.Object.Destroy(wall);
            Fresh(); Position(new Vector3(17,.5f,0)); Move(Vector3.right,true,.8f);
            Check(player.transform.position.x<=17.401f && !player.IsDashing,"Arena boundary stops the dash");
            Fresh(); game.Run.Session.Rules.TestMode=EnemyTestMode.StoneArmorOnly; Call("SpawnEnemy");
            var enemies=(List<Transform>)Get("enemies"); var stone=enemies[enemies.Count-1]; stone.position=new Vector3(1.5f,.375f,0);
            Physics.SyncTransforms(); Move(Vector3.right,true,.8f);
            Check(player.transform.position.x<.85f && !player.IsDashing,"Solid stone enemy blocks the dash");
            Call("ClearCombatObjects"); game.Run.Session.Rules.TestMode=EnemyTestMode.NormalOnly;

            Fresh(); PadState(Vector2.right,true); Poll(.05f); remaining=player.DashCooldownRemaining;
            GameTestAccess.Call(game,"TogglePause"); Poll(.5f);
            Check(!player.enabled && Mathf.Abs(player.DashCooldownRemaining-remaining)<.001f && pad.Low==0,"Pause freezes cooldown and stops feedback");
            GameTestAccess.Call(game,"ResumeGame"); Poll(.05f); Check(player.DashCooldownRemaining<remaining,"Resume continues the existing cooldown");
            Fresh(); GameTestAccess.Call(game,"ToggleCodex"); KeyState(Key.Space); Poll(.5f);
            Check(!player.IsDashing && player.DashCooldownRemaining==0,"Codex ignores dash input");
            GameTestAccess.Call(game,"ToggleCodex"); Poll(.01f); Check(!player.IsDashing,"Codex input is not queued into combat");
            KeyState(); Poll(.01f); KeyState(Key.Space); Poll(.04f); remaining=player.DashCooldownRemaining;
            PlayerCall("OnApplicationFocus",false); Poll(.5f);
            Check(Mathf.Abs(player.DashCooldownRemaining-remaining)<.001f && !player.IsDashProtected,"Focus loss freezes movement and removes active protection");
            PlayerCall("OnApplicationFocus",true);
            Fresh(); player.enabled=false; KeyState(Key.Space); Poll(.1f); player.enabled=true; UpdateInput(); Poll(.01f);
            Check(!player.IsDashing,"Holding the button across menu enable does not trigger a dash");
            Fresh(); Move(Vector3.right,true,.04f); Call("FinishWave");
            Check(!player.IsDashing && !player.IsDashProtected && player.DashCooldownRemaining==0,"Settlement clears dash movement and protection");
            game.NewGame(); game.BeginRun(0); game.enabled=false; Call("ClearCombatObjects"); Fresh();
            Move(Vector3.right,true,.04f); Call("ShowDefeat"); Check(!player.IsDashProtected && !player.enabled,"Defeat clears dash state");
            game.NewGame(); game.BeginRun(0); game.enabled=false; Call("ClearCombatObjects"); Fresh();
            Move(Vector3.right,true,.04f); Call("StartWave");
            Check(player.DashCooldownRemaining==0 && !player.IsDashing && player.DashCharges==2,"Every new wave replenishes all dash charges");
            Call("ClearCombatObjects"); Fresh();
            Debug.Log("DASH_CHECKS_PASSED: free ability, double charges, 3rd press rejection, stepwise 0->1->2 cadence & HUD flash, mid-recovery preservation, keyboard/LB, press edges, last/camera direction, fixed distance, cooldown flash, haptic lifecycle, projectile/shield immunity, walls/stone/borders, pause/codex/focus, settlement/death/reset.");
        }
        finally {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
    }
    static void PrepareCapture()
    {
        game.enabled=true;
        // Keep combat running for a genuine frame capture, with no new spawns or rifle fire.
        GameTestAccess.Set(game,"nextSpawn",100f); GameTestAccess.Set(game,"nextShot",100f);
        var camera=(PlaygroundCamera)Get("viewCamera"); camera.ThirdPerson=false;
        Position(new Vector3(0,.5f,0)); player.ResetDash(); KeyState(); Poll(.001f);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0) {Verify(); PrepareCapture(); next=EditorApplication.timeSinceStartup+.4;}
            else if(stage==1) {ScreenCapture.CaptureScreenshot("ArtSource/Player/Dash-Ready.png"); next=EditorApplication.timeSinceStartup+.3;}
            else if(stage==2) {KeyState(Key.D,Key.Space); Poll(.04f); ScreenCapture.CaptureScreenshot("ArtSource/Player/Dash-Active.png"); next=EditorApplication.timeSinceStartup+.3;}
            else if(stage==3) {KeyState(); ScreenCapture.CaptureScreenshot("ArtSource/Player/Dash-Cooldown.png"); next=EditorApplication.timeSinceStartup+.3;}
            else {Finish(0); return;}
            stage++;
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("DashChecks",false);
        if(player!=null) player.ResetDash();
        if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
        if(pad!=null) InputSystem.RemoveDevice(pad);
        if(originalInput!=null) InputSystem.settings=originalInput;
        if(testInput!=null) UnityEngine.Object.Destroy(testInput);
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}
