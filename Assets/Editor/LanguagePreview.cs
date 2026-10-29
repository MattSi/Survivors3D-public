using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LanguagePreview
{
    private const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
    private static int stage,previousLanguage;
    private static double next,deadline;
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("LanguagePreview",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("LanguagePreview",false)) return;
            previousLanguage=PlayerPrefs.GetInt(GameLanguage.PreferenceKey,0);
            PlayerPrefs.SetInt(GameLanguage.PreferenceKey,0);
            stage=0; next=EditorApplication.timeSinceStartup+3; deadline=next+50;
            EditorApplication.update+=Tick;
        };
    }
    private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Language preview timed out");
            if(EditorApplication.timeSinceStartup<next) return;
            var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); if(game==null) return;
            var type=typeof(SurvivorPlayground);
            if(stage==0)
            {
                game.NewGame();
                Check(GameLanguage.Font!=null,"Bundled CJK font is available");
                foreach(var item in ItemLibrary.All)
                {
                    Check(GameLanguage.Text(item.Name)!=item.Name,"Chinese name: "+item.Name);
                    Check(GameLanguage.Text(item.Description)!=item.Description,"Chinese description: "+item.Name);
                    string translated=GameLanguage.Text(item.Name+item.Description+item.Synergy);
                    GameLanguage.Font.RequestCharactersInTexture(translated,20);
                    foreach(char c in translated) if(c>=0x4e00 && c<=0x9fff) Check(GameLanguage.Font.HasCharacter(c),"Missing glyph: "+c);
                }
                var ids=new System.Collections.Generic.HashSet<int>();
                for(int i=0;i<ItemLibrary.Count;i++) Check(ids.Add(ItemIcons.Get(i).GetInstanceID()),"Each item has its own icon");
                ScreenCapture.CaptureScreenshot("ArtSource/World/Starter-Chinese.png");
            }
            else if(stage==1) { type.GetMethod("ToggleCodex",Flags).Invoke(game,null); GameTestAccess.Set(game,"codexSelection",14); }
            else if(stage==2) ScreenCapture.CaptureScreenshot("ArtSource/World/Codex-Chinese.png");
            else if(stage==3)
            {
                PlayerPrefs.SetInt(GameLanguage.PreferenceKey,1);
                Check(GameLanguage.Text(ItemLibrary.All[0].Name)==ItemLibrary.All[0].Name,"English switch restores original text");
            }
            else if(stage==4) ScreenCapture.CaptureScreenshot("ArtSource/World/Codex-English.png");
            else if(stage==5)
            {
                PlayerPrefs.SetInt(GameLanguage.PreferenceKey,0);
                type.GetMethod("ToggleCodex",Flags).Invoke(game,null); game.BeginRun(2);
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
            else if(stage==6) ScreenCapture.CaptureScreenshot("ArtSource/World/Shop-Chinese.png");
            else if(stage==7) { game.enabled=false; game.gameObject.AddComponent<ItemIconSheet>(); }
            else if(stage==8) ScreenCapture.CaptureScreenshot("ArtSource/World/ItemIcons-Chinese.png");
            else { Finish(0); return; }
            stage++; next=EditorApplication.timeSinceStartup+2;
        }
        catch(Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        PlayerPrefs.SetInt(GameLanguage.PreferenceKey,previousLanguage);
        SessionState.SetBool("LanguagePreview",false);
        if(code==0) Debug.Log("LANGUAGE_PREVIEW_PASSED: sixteen distinct icons, translated definitions, CJK glyph coverage and English switch.");
        EditorApplication.Exit(code);
    }
}

public sealed class ItemIconSheet : MonoBehaviour
{
    private void OnGUI()
    {
        GUI.depth=-100;
        Matrix4x4 previous=GUI.matrix; float scale=PlaygroundCamera.HudScale;
        GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        float width=Screen.width/scale,height=Screen.height/scale;
        Color previousColor=GUI.color; GUI.color=new Color(0.035f,0.045f,0.035f,1);
        GUI.DrawTexture(new Rect(0,0,width,height),Texture2D.whiteTexture);
        GUI.color=previousColor;
        var style=new GUIStyle(GUI.skin.label) { font=GameLanguage.Font,fontSize=24,alignment=TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0,16,width,40),$"道具图标 / {ItemLibrary.Count} ITEMS",style);
        float w=(width-60)/4,h=(height-80)/((ItemLibrary.Count+3)/4);
        for(int i=0;i<ItemLibrary.Count;i++)
        {
            float x=30+(i%4)*w,y=70+(i/4)*h;
            GUI.DrawTexture(new Rect(x+w/2-40,y,80,80),ItemIcons.Get(i));
            style.fontSize=16;
            GUI.Label(new Rect(x,y+82,w,28),GameLanguage.Text(ItemLibrary.All[i].Name),style);
        }
        GUI.matrix=previous;
    }
}
