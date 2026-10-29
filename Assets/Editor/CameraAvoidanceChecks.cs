using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CameraAvoidanceChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    static double next,deadline;
    static int stage;
    static PlaygroundCamera camera;
    static PlayerAvatar avatar;
    static GameObject obstacle;
    static readonly Vector3 origin=Vector3.zero;
    static void Set(string name,object value)=>typeof(PlaygroundCamera).GetField(name,F).SetValue(camera,value);
    static float Read(string name)=>(float)typeof(PlaygroundCamera).GetField(name,F).GetValue(camera);
    static void Check(bool pass,string message) { if(!pass) throw new Exception(message); }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("CameraAvoidanceChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CameraAvoidanceChecks",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+3; deadline=next+30; EditorApplication.update+=Tick;
        };
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Camera avoidance timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            if(stage==0)
            {
                camera=UnityEngine.Object.FindFirstObjectByType<PlaygroundCamera>();
                avatar=UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>();
                foreach(Collider existing in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if(!existing.transform.IsChildOf(avatar.transform) && existing.transform!=avatar.transform) existing.enabled=false;
                avatar.GetComponent<PlayerController>().enabled=false;
                avatar.transform.position=origin+Vector3.up*.5f;
                camera.ThirdPerson=true; camera.Tilt=28; camera.Distance=7;
                Set("focus",origin); Set("displayedDistance",7f); Set("<Yaw>k__BackingField",0f);
                obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name="Camera regression crate";
                obstacle.transform.position=origin+new Vector3(0,1.1f,-1.3f);
                obstacle.transform.localScale=new Vector3(2,2.2f,1);
                var geometry=obstacle.AddComponent<CameraObstacleBounds>();
                geometry.VisualBounds=obstacle.GetComponent<Renderer>().bounds;
                geometry.VisualRenderers=new[]{obstacle.GetComponent<Renderer>()};
                Physics.SyncTransforms();
            }
            else if(stage==1)
            {
                Vector3 expected=origin+Vector3.up*1.35f-Quaternion.Euler(28,0,0)*Vector3.forward*7;
                Check(Vector3.Distance(camera.transform.position,expected)<.01f,"Distant camera stays fixed despite crate blocking sight line");
                Check(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(28,0,0))<.01f,"No automatic pitch change");
                var geometry=obstacle.GetComponent<CameraObstacleBounds>();
                Check(geometry.Opacity<.3f,"Occluding crate fades");
                Check(obstacle.GetComponent<Collider>().enabled,"Fade keeps gameplay collision");
                Check(obstacle.GetComponent<Renderer>().sharedMaterial.GetFloat("_ZWrite")==0,"Transparent obstacle does not occlude with depth writes");
                obstacle.transform.position=camera.transform.position;
                geometry.VisualBounds=obstacle.GetComponent<Renderer>().bounds;
                Physics.SyncTransforms();
            }
            else if(stage==2)
            {
                Bounds bounds=obstacle.GetComponent<CameraObstacleBounds>().VisualBounds;
                bounds.Expand(.4f);
                Check(!bounds.Contains(camera.transform.position),"Camera itself remains outside the solid crate");
                Check(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(28,0,0))<.01f,"Collision correction preserves pitch");
                obstacle.transform.position=new Vector3(10,1,0);
                obstacle.GetComponent<CameraObstacleBounds>().VisualBounds=obstacle.GetComponent<Renderer>().bounds;
                Physics.SyncTransforms();
            }
            else if(stage==3)
            {
                Vector3 expected=origin+Vector3.up*1.35f-Quaternion.Euler(28,0,0)*Vector3.forward*7;
                Check(Vector3.Distance(camera.transform.position,expected)<.01f,"Camera offset recovers after physical collision");
                Check(obstacle.GetComponent<CameraObstacleBounds>().Opacity==1,"Offscreen scenery restores original material");
                obstacle.GetComponent<CameraObstacleBounds>().SetObscuring(true,1);
                camera.ThirdPerson=false;
                typeof(PlaygroundCamera).GetMethod("ApplyView",F).Invoke(camera,null);
                Check(obstacle.GetComponent<CameraObstacleBounds>().Opacity==1,"Top-down view restores scenery");
                obstacle.GetComponent<CameraObstacleBounds>().SetObscuring(true,1);
                camera.enabled=false;
                Check(obstacle.GetComponent<CameraObstacleBounds>().Opacity==1,"Disabling camera restores scenery");
                UnityEngine.Object.DestroyImmediate(obstacle);
                Debug.Log("CAMERA_AVOIDANCE_PASSED: stable occluded view, obstacle transparency, physical camera protection, recovery and material cleanup.");
                SessionState.SetBool("CameraAvoidanceChecks",false); EditorApplication.Exit(0); return;
            }
            stage++; next=EditorApplication.timeSinceStartup+3;
        }
        catch(Exception e) { Debug.LogException(e); SessionState.SetBool("CameraAvoidanceChecks",false); EditorApplication.Exit(1); }
    }
}
