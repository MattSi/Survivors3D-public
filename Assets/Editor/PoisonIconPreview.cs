using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonIconPreview
{
    static int stage;
    static double next;
    static GameObject overlay;
    [MenuItem("Tools/Survivors/Preview Poison Backpack Icon")]
    public static void Capture()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("PoisonIconPreview",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PoisonIconPreview",false)) return;
            stage=0; next=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0)
            {
                var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
                game.NewGame(); game.enabled=false;
                overlay=new GameObject("Poison Icon Preview");
                overlay.AddComponent<PoisonIconPreviewOverlay>().Game=game;
                File.WriteAllBytes("ArtSource/Weapons/Poison-Backpack-Icon.png",PoisonBackpackIcon.Get().EncodeToPNG());
            }
            else if(stage==1) ScreenCapture.CaptureScreenshot("ArtSource/Weapons/Poison-Backpack-Icon-Preview.png");
            else { Debug.Log("POISON_ICON_PREVIEW_SAVED"); Finish(0); return; }
            stage++; next=EditorApplication.timeSinceStartup+1;
        }
        catch(Exception error) { Debug.LogException(error); Finish(1); }
    }
    static void Finish(int code)
    {
        SessionState.SetBool("PoisonIconPreview",false); EditorApplication.update-=Tick;
        if(overlay!=null) UnityEngine.Object.Destroy(overlay);
        if(Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(),"-executeMethod")>=0) EditorApplication.Exit(code);
        else EditorApplication.ExitPlaymode();
    }
}

public sealed class PoisonIconPreviewOverlay : MonoBehaviour
{
    public SurvivorPlayground Game;
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    void OnGUI()
    {
        if(Game==null) return;
        Font font=GameLanguage.Font;
        var ui=typeof(SurvivorPlayground).GetProperty("UI",F).GetValue(Game);
        var iconMethod=ui.GetType().GetMethod("UpgradeIcon",F);
        float scale=Mathf.Min(Screen.width/960f,Screen.height/640f);
        Matrix4x4 old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=Screen.width/scale,height=Screen.height/scale;
        void Panel(Rect r,Color c) { Color oldColor=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=oldColor; }
        void Text(Rect r,string text,int size,Color color)
        {
            var style=new GUIStyle(GUI.skin.label) {font=font,fontSize=size,fontStyle=FontStyle.Bold,normal={textColor=color},alignment=TextAnchor.MiddleLeft};
            GUI.Label(r,text,style);
        }
        Color green=new Color(.52f,.95f,.30f),muted=new Color(.64f,.65f,.61f);
        Panel(new Rect(0,0,width,height),new Color(.035f,.045f,.027f));
        float left=width/2-418;
        Text(new Rect(left,48,830,45),"毒液背罐",30,Color.white);
        Text(new Rect(left,98,830,30),"武器图标预览",16,muted);
        Panel(new Rect(left,166,300,300),new Color(.15f,.17f,.14f));
        GUI.DrawTexture(new Rect(left+22,188,256,256),PoisonBackpackIcon.Get());
        Text(new Rect(left+18,480,290,30),"方罐 · 铁箍 · 毒液窗",17,muted);
        float x=left+344;
        Panel(new Rect(x-2,166,494,116),green); Panel(new Rect(x,168,490,112),new Color(.055f,.065f,.045f));
        Panel(new Rect(x+14,182,70,70),new Color(.15f,.17f,.14f));
        GUI.DrawTexture(new Rect(x+14,182,70,70),PoisonBackpackIcon.Get());
        Text(new Rect(x+104,180,370,36),"毒液背罐",23,Color.white);
        Text(new Rect(x+104,222,370,30),"自动洒毒 · 持续伤害",16,green);
        Text(new Rect(x,316,490,30),"装备栏尺寸对照",17,muted);
        int[] items={WeaponCatalog.ShopId(9),WeaponCatalog.ShopId(7),WeaponCatalog.ShopId(6)};
        string[] names={"回旋飞刀","火把","寒霜领域","毒液背罐"};
        for(int i=0;i<4;i++)
        {
            Rect box=new Rect(x+i*122,370,56,56);
            if(i<3) iconMethod.Invoke(ui,new object[] {box,items[i],true});
            else { Panel(box,new Color(.15f,.17f,.14f)); GUI.DrawTexture(box,PoisonBackpackIcon.Get()); }
            Text(new Rect(box.x-8,442,118,26),names[i],15,i==3?green:Color.white);
        }
        Text(new Rect(left,558,840,28),"木铁材质与简洁轮廓，沿用现有武器图标风格",17,muted);
        GUI.matrix=old;
    }
}
