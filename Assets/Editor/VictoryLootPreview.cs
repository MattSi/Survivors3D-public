using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VictoryLootPreview
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    static SurvivorPlayground game;
    static int stage,balance,score,expectedLoot;
    static double next,deadline;
    static Gamepad pad;
    static object Get(string name)=>GameTestAccess.Get(game,name);
    static void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
    static bool Victory=>(bool)typeof(SurvivorPlayground).GetProperty("ShowingVictory",F).GetValue(game);
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("VictoryLootPreview",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VictoryLootPreview",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+3; deadline=next+30; EditorApplication.update+=Tick;
        };
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Victory loot test timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); if(game==null) return;
            next=double.MaxValue; // InputSystem.Update can re-enter editor callbacks.
            var equipment=(RunEquipment)Get("equipment");
            if(stage==0)
            {
                // A rotated particle must keep its center at the scaled HUD position.
                var uiType=typeof(SurvivorPlayground).Assembly.GetType("GameUI");
                var particleMatrix=uiType.GetMethod("ParticleMatrix",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(float scale in new[]{.75f,1f,1.5f,2f})
                foreach(float angle in new[]{0f,45f,137f,270f})
                {
                    Vector2 center=new Vector2(860,54);
                    Matrix4x4 parent=Matrix4x4.Scale(Vector3.one*scale);
                    var matrix=(Matrix4x4)particleMatrix.Invoke(null,new object[]{parent,center,angle});
                    Check(Vector3.Distance(matrix.MultiplyPoint3x4(Vector3.zero),new Vector3(center.x*scale,center.y*scale,0))<.001f,
                        "Rotated victory particles preserve their HUD anchor at every scale");
                }
                game.NewGame(); game.BeginRun(0); balance=equipment.Materials; score=game.Score;
                Call("SpawnMaterial",new Vector3(0,0,0));
                var drops=(IList)Get("pickups"); object drop=drops[0];
                FieldInfo expires=drop.GetType().GetField("ExpiresAt");
                Check(Mathf.Abs((float)expires.GetValue(drop)-Time.time-6)<.01f,"Loot lifetime is six seconds");
                expires.SetValue(drop,Time.time); Call("UpdatePickups",0f);
                Check(drops.Count==0 && equipment.Materials==balance,"Expired loot disappears before pickup and gives no reward");
                for(int i=0;i<16;i++) Call("SpawnMaterial",new Vector3((i%4-1.5f)*2,0,(i/4-1.5f)*2));
                drop=drops[0]; expires.SetValue(drop,Time.time);
                int enemies=game.EnemyCount; expectedLoot=15+enemies;
                ((WaveSession)Get("session")).Advance(((WaveSession)Get("session")).WaveDuration); Call("CompleteWave");
                Check(Victory && enemies>0 && game.EnemyCount==0,"Victory explodes all remaining enemies");
                Check(((IList)Get("impacts")).Count==enemies,"Every remaining enemy has an explosion");
                var avatar=(PlayerAvatar)Get("playerAvatar");
                var visual=(Transform)typeof(PlayerAvatar).GetField("visual",F).GetValue(avatar);
                Vector3 rootPosition=avatar.transform.position;
                Vector3 restPosition=(Vector3)typeof(PlayerAvatar).GetField("restPosition",F).GetValue(avatar);
                avatar.SetCelebrationTime(.65f);
                typeof(PlayerAvatar).GetMethod("LateUpdate",F).Invoke(avatar,null);
                Check(visual.localPosition.y>restPosition.y+1f && avatar.transform.position==rootPosition,
                    "Victory jump lifts only the visual, leaving the gameplay root in place");
                avatar.SetCelebrationTime(1.5f);
                typeof(PlayerAvatar).GetMethod("LateUpdate",F).Invoke(avatar,null);
                Check(Vector3.Distance(visual.localPosition,restPosition)<.001f,"Victory lands back at the original height");
                Check(((IList)typeof(PlayerAvatar).GetField("arms",F).GetValue(avatar)).Count==8,
                    "Both arms include sleeves, trim, forearms and wrist wraps");
                avatar.SetCelebrationTime(0);
                typeof(PlayerAvatar).GetMethod("LateUpdate",F).Invoke(avatar,null);
                Check(((IList)Get("lootFlights")).Count==expectedLoot && equipment.Materials==balance,"Only surviving loot flies; no instant reward");
                next=EditorApplication.timeSinceStartup+1.15; stage++; return;
            }
            else if(stage==1) ScreenCapture.CaptureScreenshot("ArtSource/World/Victory-Loot.png");
            else if(stage==2)
            {
                Call("UpdateVictoryLoot",5f);
                Check(Victory && equipment.Materials==balance+15+5 && game.Score==score+expectedLoot,"Arena pickups award materials and score; cleanup awards only score; completion adds five materials");
                Call("CompleteWave");
                Call("UpdateVictoryLoot",5f); Check(equipment.Materials==balance+15+5,"Rewards are counted once");
                InputSystem.settings=UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                pad=InputSystem.AddDevice<Gamepad>();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update();
                Call("Update");
                Check(!Victory,"Gamepad A enters shop");
                InputSystem.RemoveDevice(pad); pad=null;
            }
            else if(stage==3)
            {
                ScreenCapture.CaptureScreenshot("ArtSource/World/Victory-Loot-Shop.png");
            }
            else if(stage==4)
            {
                game.NewGame(); game.BeginRun(0);
                int freshBalance=equipment.Materials;
                Call("SpawnMaterial",Vector3.zero);
                ((WaveSession)Get("session")).Advance(((WaveSession)Get("session")).WaveDuration); Call("CompleteWave");
                GameTestAccess.Set(game,"phaseChangedFrame",Time.frameCount-1);
                var keyboard=InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); InputSystem.Update();
                Call("Update");
                Check(Victory,"Early Space confirmation waits for loot");
                Call("UpdateVictoryLoot",1.4f);
                Check(Victory && ((IList)Get("lootFlights")).Count==0,"Early confirmation waits for the celebration even after loot arrives");
                Call("UpdateVictoryLoot",.2f); Check(!Victory,"Space enters shop after celebration and loot");
                var avatar=(PlayerAvatar)Get("playerAvatar");
                Check(!(bool)typeof(PlayerAvatar).GetField("celebrating",F).GetValue(avatar),"Shop entry resets the celebration pose");
                InputSystem.RemoveDevice(keyboard);
                Check(equipment.Materials==freshBalance+6,"A restarted run receives one pickup plus five completion materials");
                game.NewGame(); game.BeginRun(0);
                Call("ClearCombatObjects");
                int emptyBalance=equipment.Materials, emptyScore=game.Score;
                ((WaveSession)Get("session")).Advance(((WaveSession)Get("session")).WaveDuration); Call("CompleteWave");
                Call("UpdateVictoryLoot",2f);
                Check(equipment.Materials==emptyBalance+5 && game.Score==emptyScore,"An empty arena still grants five materials without score");
                Call("CompleteWave"); Call("UpdateVictoryLoot",5f);
                Check(equipment.Materials==emptyBalance+5,"Repeated completion cannot duplicate the fixed reward");
            }
            else
            {
                Debug.Log("VICTORY_LOOT_PASSED: six-second expiry, end-of-wave explosions, surviving loot animation, exactly-once material/score, manual gamepad shop entry.");
                SessionState.SetBool("VictoryLootPreview",false); EditorApplication.Exit(0); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception e)
        {
            if(pad!=null) InputSystem.RemoveDevice(pad);
            Debug.LogException(e); SessionState.SetBool("VictoryLootPreview",false); EditorApplication.Exit(1);
        }
    }
}
