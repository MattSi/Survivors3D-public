using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StartMenuPreview
{
    private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    private static int stage;
    private static double next,deadline;
    private static bool mode;
    private static float tilt,distance;
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("StartMenuPreview",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("StartMenuPreview",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+3; deadline=next+35; EditorApplication.update+=Tick;
        };
    }
    private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Start menu preview timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); if(game==null) return;
            var type=typeof(SurvivorPlayground); var camera=UnityEngine.Object.FindFirstObjectByType<PlaygroundCamera>();
            if(stage==0)
            {
                Check((int)GameTestAccess.Get(game,"frontScreen")==1 && game.EnemyCount==0,"Boot waits in main menu");
                game.BeginRun(0); Check(game.EnemyCount==0,"Weapon cannot start behind main menu");
                ScreenCapture.CaptureScreenshot("ArtSource/World/MainMenu-Unity.png");
            }
            else if(stage==1)
            {
                GameTestAccess.Call(game,"OpenSettings");
                mode=camera.ThirdPerson; tilt=camera.Tilt; distance=camera.Distance;
                camera.Tilt=camera.MaxTilt; camera.Distance=camera.MaxDistance;
                Check(game.EnemyCount==0,"Settings do not start waves");
            }
            else if(stage==2) ScreenCapture.CaptureScreenshot("ArtSource/World/Settings-Unity.png");
            else if(stage==3)
            {
                GameTestAccess.Call(game,"CloseSettings");
                float expected=camera.Tilt; camera.Tilt=camera.MinTilt; camera.LoadPreferences();
                Check(camera.Tilt==expected,"Camera settings persist and reload");
                camera.ThirdPerson=mode; camera.Tilt=tilt; camera.Distance=distance; camera.SavePreferences();
                game.NewGame(); Check(game.EnemyCount==0,"New game opens weapon choice without enemies");
            }
            else if(stage==4) ScreenCapture.CaptureScreenshot("ArtSource/World/NewGame-Unity.png");
            else if(stage==5)
            {
                game.BeginRun(1); Check(game.EnemyCount>0,"Weapon choice starts combat");
                Check(type.GetMethod("DrawCameraControls",Flags)==null,"No combat camera settings panel");
                ScreenCapture.CaptureScreenshot("ArtSource/World/Combat-NoCameraPanel.png");
            }
            else
            {
                SessionState.SetBool("StartMenuPreview",false);
                Debug.Log("START_MENU_PASSED: main menu, settings isolation, camera persistence, new game and combat without camera panel.");
                EditorApplication.Exit(0); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception e) { Debug.LogException(e); SessionState.SetBool("StartMenuPreview",false); EditorApplication.Exit(1); }
    }
}
