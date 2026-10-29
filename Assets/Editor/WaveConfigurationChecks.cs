using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WaveConfigurationChecks
{
    static double ready;
    [MenuItem("Tools/Survivors/Verify Wave Configuration")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        WaveConfigurationTools.EnsureDefault();
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("WaveConfigurationChecks",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("WaveConfigurationChecks",false)) return;
            ready=EditorApplication.timeSinceStartup+2; EditorApplication.update+=Tick;
        };
    }
    static void Check(bool value,string message) {if(!value) throw new Exception("WAVE_CONFIG_INTEGRATION: "+message);}
    static void Verify()
    {
        WaveRulesChecks.Run();
        var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        var original=GameTestAccess.Get(game,"waveConfiguration");
        var profile=ScriptableObject.CreateInstance<WaveConfiguration>();
        GameTestAccess.Set(game,"waveConfiguration",profile);
        try
        {
            typeof(BomberChecks).GetMethod("Verify",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
            var rules=profile.Rules;
            rules.StartWave=8; rules.StartingMaterials=500; rules.TestMode=EnemyTestMode.NormalOnly;
            rules.Waves[7].Duration=4; rules.Waves[7].InitialEnemies=2; rules.EnemyDefinitions[0].BaseHealth=77;
            game.NewGame(); game.BeginRun(0); game.enabled=false;
            Check(game.Run.Session.Wave==8 && game.Run.Session.Remaining==4 && game.Run.Equipment.Materials==500,"Scene uses configured starting wave, duration and test materials");
            var enemies=(List<Transform>)GameTestAccess.Get(game,"enemies");
            var hp=(Dictionary<Transform,int>)GameTestAccess.Get(game,"enemyHealth");
            Check(enemies.Count==2 && hp[enemies[0]]==77,"Configured population and health reach real spawned enemies");
            rules.Waves[7].Duration=9;
            Check(game.Run.Session.WaveDuration==4,"Changing the source profile cannot change the active run");
            game.NewGame(); Check(game.Run.Session.WaveDuration==9,"New runs pick up profile edits");
            rules.StartWave=1; rules.TestMode=EnemyTestMode.ChargerOnly;
            game.NewGame(); game.BeginRun(0);
            Check(game.EnemyCount==2 && ((System.Collections.IDictionary)GameTestAccess.Get(game,"chargers")).Count==2,"Forced-type testing produces only the selected enemy within its cap");
            profile.Rules=new WaveRules {Endless=true,StartWave=20,TestMode=EnemyTestMode.NormalOnly};
            game.NewGame(); game.BeginRun(0);
            var session=game.Run.Session; session.Advance(session.Remaining);
            Check(session.State==WaveSession.Phase.Rest,"Wave twenty is not a final victory in endless mode");
            GameTestAccess.Call(game,"CompleteWave");
            GameTestAccess.Set(game,"victoryContinueRequested",true); GameTestAccess.Call(game,"UpdateVictoryLoot",5f);
            GameTestAccess.Set(game,"phaseChangedFrame",Time.frameCount-1);
            GameTestAccess.Set(game,"shopSelection",game.Run.Shop.Count+1);
            GameTestAccess.Call(game,"ConfirmPhase");
            Check(session.Wave==21 && session.State==WaveSession.Phase.Combat,"Endless settlement and shop continue into wave twenty-one");
            Check(Math.Abs(game.Run.Session.Rules.At(21).HealthMultiplier-1.1f)<.0001f,"Endless adds health multiplier after the last authored row");
            EnemyHealthChecks.Verify(game);
            Debug.Log("WAVE_CONFIGURATION_CHECKS_PASSED: default parity, all enemy regressions, profile snapshots, configured spawns, single-enemy tests, custom campaign, endless shop and safe growth.");
        }
        finally {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        int code=0;
        try {Verify();} catch(Exception error) {Debug.LogException(error); code=1;}
        EditorApplication.update-=Tick; SessionState.SetBool("WaveConfigurationChecks",false);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
