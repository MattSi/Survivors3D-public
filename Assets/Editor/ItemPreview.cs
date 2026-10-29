using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ItemPreview
{
    private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    private static int stage;
    private static double next,deadline;
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("ItemPreview",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ItemPreview",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; deadline=next+30; EditorApplication.update+=Tick;
        };
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Item preview timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
            if(game==null) return;
            var type=typeof(SurvivorPlayground);
            if(stage==0) { game.NewGame(); }
            if(stage==0) ScreenCapture.CaptureScreenshot("ArtSource/World/Starter-Unity.png");
            else if(stage==1)
            {
                game.BeginRun(2);
                var session=(WaveSession)GameTestAccess.Get(game,"session");
                for(int wave=1;wave<=12;wave++) { session.Advance(session.WaveDuration); if(wave<12) session.Continue(); }
                type.GetMethod("CompleteWave",Flags).Invoke(game,null);
                type.GetMethod("UpdateVictoryLoot",Flags).Invoke(game,new object[]{5f});
                type.GetMethod("EnterVictoryShop",Flags).Invoke(game,null);
                var equipment=(RunEquipment)GameTestAccess.Get(game,"equipment"); equipment.Collect(120);
                for(int seed=0;seed<100;seed++)
                {
                    var shop=new WaveShop(equipment,new System.Random(seed)); shop.Open(12);
                    bool all=true; for(int i=0;i<4;i++) all &= shop.Item(i)>=6;
                    if(!all) continue;
                    shop.ToggleLock(0); GameTestAccess.Set(game,"shop",shop); break;
                }
                next=EditorApplication.timeSinceStartup+3; stage++; return;
            }
            else if(stage==2) ScreenCapture.CaptureScreenshot("ArtSource/World/ItemShop-Unity.png");
            else if(stage==3)
            {
                type.GetMethod("ToggleCodex",Flags).Invoke(game,null);
                GameTestAccess.Set(game,"codexSelection",6);
            }
            else if(stage==4) ScreenCapture.CaptureScreenshot("ArtSource/World/ItemCodex-Unity.png");
            else { type.GetMethod("ToggleCodex",Flags).Invoke(game,null); SessionState.SetBool("ItemPreview",false); Debug.Log("ITEM_PREVIEWS_PASSED"); EditorApplication.Exit(0); }
            stage++; next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception exception) { SessionState.SetBool("ItemPreview",false); Debug.LogException(exception); EditorApplication.Exit(1); }
    }
}
