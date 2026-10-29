using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShopPreview
{
    public static void CaptureThirdPerson()
    {
        SessionState.SetBool("CaptureThirdPerson",true);
        Capture();
    }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("CaptureShop",true);
        EditorApplication.EnterPlaymode();
    }
    private static int stage;
    private static double started;
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged += state => {
            if(state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CaptureShop",false)) return;
            stage=0; started=EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        };
    }
    private static void Update()
    {
        var game=Object.FindFirstObjectByType<SurvivorPlayground>();
        if(game == null || Time.timeSinceLevelLoad < 0.5f) return;
        if(stage == 0)
        {
            game.NewGame();
            game.BeginRun(0);
            if(SessionState.GetBool("CaptureThirdPerson",false))
            {
                game.enabled=false;
                var camera=Object.FindFirstObjectByType<PlaygroundCamera>();
                if(!camera.ThirdPerson || camera.Tilt!=28 || camera.Distance!=7) throw new System.Exception("Third person default incorrect");
                camera.ThirdPerson=false;
                if(camera.Tilt!=55 || camera.Distance!=25.5f) throw new System.Exception("Top down preset missing");
                camera.ThirdPerson=true;
                stage=1; started=EditorApplication.timeSinceStartup;
                return;
            }
            var type=typeof(SurvivorPlayground);
            var equipment=(RunEquipment)GameTestAccess.Get(game,"equipment");
            equipment.Collect(52);
            GameTestAccess.Set(game,"shop",new WaveShop(equipment,new System.Random(0)));
            var session=(WaveSession)GameTestAccess.Get(game,"session");
            session.Advance(session.WaveDuration);
            type.GetMethod("CompleteWave",Flags).Invoke(game,null);
            type.GetMethod("ConfirmPhase",Flags).Invoke(game,null);
            if(session.State != WaveSession.Phase.Rest || !(bool)type.GetProperty("ShowingVictory",Flags).GetValue(game))
                throw new System.Exception("Victory banner was skipped");
            stage=1; started=EditorApplication.timeSinceStartup;
        }
        if(stage == 1 && EditorApplication.timeSinceStartup-started > 2)
        {
            bool third=SessionState.GetBool("CaptureThirdPerson",false);
            if(!third) { typeof(SurvivorPlayground).GetMethod("UpdateVictoryLoot",Flags).Invoke(game,new object[]{5f}); typeof(SurvivorPlayground).GetMethod("EnterVictoryShop",Flags).Invoke(game,null); }
            if(!third && !(bool)typeof(SurvivorPlayground).GetProperty("ShowingShop",Flags).GetValue(game)) return;
            ScreenCapture.CaptureScreenshot(third ? "ArtSource/World/ThirdPerson-Unity.png" : "ArtSource/World/Shop-Unity.png");
            stage=2; started=EditorApplication.timeSinceStartup;
        }
        if(stage == 2 && EditorApplication.timeSinceStartup-started > 3)
        {
            if(SessionState.GetBool("CaptureThirdPerson",false))
            {
                SessionState.SetBool("CaptureThirdPerson",false);
                SessionState.SetBool("CaptureShop",false);
                Debug.Log("THIRD_PERSON_PREVIEW_PASSED: camera defaults and preset switching.");
                EditorApplication.Exit(0);
                return;
            }
            var type=typeof(SurvivorPlayground);
            var equipment=(RunEquipment)GameTestAccess.Get(game,"equipment");
            var shop=(WaveShop)GameTestAccess.Get(game,"shop");
            int before=equipment.Materials, price=equipment.Price(shop.Item(0));
            type.GetMethod("BuyEquipment",Flags).Invoke(game,new object[] {0});
            if(!shop.Sold(0) || equipment.Materials != before-price) throw new System.Exception("Shop purchase failed");
            before=equipment.Materials;
            type.GetMethod("RefreshShop",Flags).Invoke(game,null);
            if(equipment.Materials != before-WaveShop.RefreshPrice) throw new System.Exception("Shop refresh failed");
            GameTestAccess.Set(game,"shopSelection",shop.Count+1);
            type.GetMethod("ConfirmPhase",Flags).Invoke(game,null);
            var session=(WaveSession)GameTestAccess.Get(game,"session");
            if(session.State != WaveSession.Phase.Combat || session.Wave != 2) throw new System.Exception("Shop continue failed");
            for(int wave=2;wave<=WaveSession.TotalWaves;wave++) { session.Advance(session.WaveDuration); if(wave<WaveSession.TotalWaves) session.Continue(); }
            if(session.IsBossWave) session.CompleteBoss();
            type.GetMethod("CompleteWave",Flags).Invoke(game,null);
            if(!(bool)type.GetProperty("ShowingVictory",Flags).GetValue(game)) throw new System.Exception("Final victory banner missing");
            type.GetMethod("UpdateVictoryLoot",Flags).Invoke(game,new object[]{5f});
            type.GetMethod("EnterVictoryShop",Flags).Invoke(game,null);
            if(!(bool)type.GetProperty("ShowingShop",Flags).GetValue(game)) throw new System.Exception("Final victory shop missing");
            Debug.Log("VICTORY_FLOW_PASSED: manual banner, loot settlement, final-wave shop.");
            Debug.Log("SHOP_FLOW_PASSED: purchase, currency, refresh, next wave.");
            SessionState.SetBool("CaptureShop",false);
            Debug.Log("SHOP_PREVIEW_COMPLETE");
            EditorApplication.Exit(0);
        }
    }
}
