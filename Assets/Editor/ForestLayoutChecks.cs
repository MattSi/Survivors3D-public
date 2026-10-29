using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestLayoutChecks
{
    static double ready;
    static bool capture;
    static ArenaObstacles arena;
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    [MenuItem("Tools/Survivors/Verify Forest Layout")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("ForestLayoutChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ForestLayoutChecks",false)) return;
            capture=false; ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Check(bool value,string message) {if(!value) throw new Exception("FOREST_LAYOUT: "+message);}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        try
        {
            if(capture) {Capture(); Finish(0); return;}
            var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
            game.NewGame(); game.BeginRun(11); game.enabled=false;
            UnityEngine.Object.FindFirstObjectByType<PlayerController>().enabled=false;
            arena=UnityEngine.Object.FindFirstObjectByType<ArenaObstacles>();
            var saved=UnityEngine.Random.state;
            List<Bounds> anchors=null; bool variation=false; Vector3 firstBay=Vector3.zero;
            try
            {
                for(int seed=0;seed<40;seed++)
                {
                    UnityEngine.Random.InitState(seed); arena.Generate();
                    Check(arena.Blocks.Count==10,"Default layout has eight anchors and two supply props");
                    if(anchors==null) {anchors=new List<Bounds>(arena.Blocks); firstBay=arena.Blocks[8].center;}
                    for(int i=0;i<8;i++) Check(arena.Blocks[i]==anchors[i],"Fixed landmarks must not drift across seeds");
                    variation|=arena.Blocks[8].center!=firstBay;
                    Check(arena.Clear(new Vector3(-17,0,0),new Vector3(17,0,0),1.5f),"East-west shortcut");
                    Check(arena.Clear(new Vector3(0,0,-17),new Vector3(0,0,17),1.5f),"North-south shortcut");
                    var loop=new[]{new Vector3(-15,0,-15),new Vector3(15,0,-15),new Vector3(15,0,15),new Vector3(-15,0,15)};
                    for(int i=0;i<4;i++) Check(arena.Clear(loop[i],loop[(i+1)%4],1.2f),"Continuous outer retreat lane");
                    foreach(var block in arena.Blocks)
                    {
                        Vector3 closest=block.ClosestPoint(Vector3.zero);
                        Check(closest.magnitude>=5.5f,"Central clearing");
                    }
                    // Traverse each group and its inner/outer approaches, not only the open center.
                    foreach(float z in new[]{-4.7f,4.7f})
                    {
                        Route(new Vector3(-16,0,z),new Vector3(16,0,z));
                        Route(new Vector3(16,0,z),new Vector3(-16,0,z));
                    }
                    typeof(CombatController).GetMethod("CreateSpawnPoints",F).Invoke(game,null);
                    var portals=(List<Vector3>)GameTestAccess.Get(game,"spawnPoints");
                    Check(portals.Count==8,"All spawn portals fit");
                    foreach(var portal in portals) Route(portal,Vector3.zero);
                }
                Check(variation,"Supply bays vary across seeds");
                ForestSceneChecks.CheckObstacles();
                arena.PrepareBossArena(); Check(arena.Blocks.Count==0,"Boss arena clears cover");
                UnityEngine.Random.InitState(31415); arena.Generate();
                Check(arena.Blocks.Count==10,"Restart restores layout");
                typeof(CombatController).GetMethod("ClearCombatObjects",F).Invoke(game,null);
                typeof(CombatController).GetMethod("CreateSpawnPoints",F).Invoke(game,null);
            }
            finally {UnityEngine.Random.state=saved;}
            Debug.Log("FOREST_LAYOUT_CHECKS_PASSED: 40 seeds, fixed landmarks, random supplies, clearing, cross routes, outer loop, enemy navigation, eight portals, player/projectile collision, boss cleanup and regeneration.");
            capture=true; ready=EditorApplication.timeSinceStartup+.6;
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Route(Vector3 from,Vector3 to)
    {
        Vector3 start=from;
        typeof(ArenaObstacles).GetField("nextRouteUpdate",F).SetValue(arena,0f);
        arena.UpdateRoutes(to);
        for(int i=0;i<1600 && Vector3.Distance(from,to)>.12f;i++)
        {
            from=arena.Move(from,to,.08f);
            foreach(var block in arena.Blocks) Check(!block.Contains(from),"Enemy route enters cover");
        }
        Check(Vector3.Distance(from,to)<=.12f,$"Enemy route {start} -> {to} stalled at {from}");
    }
    static void Capture()
    {
        var camera=Camera.main; var oldPosition=camera.transform.position; var oldRotation=camera.transform.rotation;
        bool oldOrtho=camera.orthographic; float oldSize=camera.orthographicSize;
        var target=new RenderTexture(1400,1100,24); var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
        var texture=new Texture2D(1400,1100,TextureFormat.RGB24,false);
        try
        {
            camera.orthographic=true; camera.orthographicSize=23;
            camera.transform.position=new Vector3(0,46,-27); camera.transform.LookAt(Vector3.zero);
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1400,1100),0,0); texture.Apply();
            System.IO.Directory.CreateDirectory("Logs"); System.IO.File.WriteAllBytes("Logs/ForestLayout.png",texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget; RenderTexture.active=oldActive;
            camera.transform.SetPositionAndRotation(oldPosition,oldRotation); camera.orthographic=oldOrtho; camera.orthographicSize=oldSize;
            UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(target);
        }
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("ForestLayoutChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
