using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Short fixed-step movement comparisons, not a campaign win-rate measurement.</summary>
public static class PoisonBuildProbe
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const float Step=.05f,Duration=20;
    static SurvivorPlayground game;
    static PlayerController player;
    static WaveConfiguration profile;
    static object original;
    static MethodInfo combatTick,movement;
    static int sample=-1,lastFrame;
    static float age,oldCapture;
    static double ready;
    static int Group=>sample/9;
    static int Scenario=>sample/3%3;
    static int Policy=>sample%3;
    [MenuItem("Tools/Survivors/Probe Poison Build Movement")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("PoisonBuildProbe",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PoisonBuildProbe",false)) return;
            ready=EditorApplication.timeSinceStartup+2; sample=-1; EditorApplication.update+=Tick;
        };
    }
    static void StartSample()
    {
        if(profile!=null) {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
        profile=PoisonBuildLab.CreateProfile(Scenario); GameTestAccess.Set(game,"waveConfiguration",profile);
        UnityEngine.Random.InitState(31415); game.NewGame(); game.BeginRun(10); game.enabled=false; player.enabled=false;
        PoisonBuildLab.Equip(game.Run.Equipment,Group); GameTestAccess.Call(game,"ApplyEquipment"); age=0;
    }
    static Vector3 Direction()
    {
        if(Policy==2) return Vector3.zero;
        Vector3 at=player.transform.position; at.y=0;
        Vector3 radial=at.sqrMagnitude<.01f?Vector3.right:at.normalized;
        float radius=Policy==0?3.2f:12;
        return (Vector3.Cross(Vector3.up,radial)+radial*Mathf.Clamp((radius-at.magnitude)*.8f,-1,1)).normalized;
    }
    static void Record()
    {
        var m=game.Metrics; var p=m.For(CombatMetrics.Source.Poison); var f=m.For(CombatMetrics.Source.Frost);
        string row=FormattableString.Invariant($"{Group},{Scenario},{Policy},{m.Seconds:F2},{game.Health.Current},{p.Damage},{p.Kills},{f.Damage},{f.Kills},{m.DamageTaken},{m.PoisonDamageWhileSlowed}\n");
        File.AppendAllText("Logs/PoisonBuildProbe.csv",row); Debug.Log("POISON_BUILD_PROBE "+row.Trim());
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready || lastFrame==Time.frameCount) return;
        lastFrame=Time.frameCount;
        try
        {
            if(sample<0)
            {
                game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>(); player=(PlayerController)GameTestAccess.Get(game,"player");
                original=GameTestAccess.Get(game,"waveConfiguration"); combatTick=typeof(CombatController).GetMethod("TickCombat",F);
                movement=typeof(PlayerController).GetMethod("TickMovement",F);
                oldCapture=Time.captureDeltaTime; Time.captureDeltaTime=Step;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/PoisonBuildProbe.csv","group,scenario,policy,seconds,hp,poison_damage,poison_kills,frost_damage,frost_kills,taken,slowed_poison_damage\n");
                sample=0; StartSample(); return;
            }
            // Use actual movement: direct transform writes would leave the spray direction stale.
            player.enabled=true;
            typeof(PlayerController).GetMethod("OnApplicationFocus",F).Invoke(player,new object[]{true});
            movement.Invoke(player,new object[]{Direction(),false,Step}); player.enabled=false;
            game.Run.Session.Advance(Step); combatTick.Invoke(game,new object[]{Step}); age+=Step;
            if(age<Duration && !game.Health.IsDead) return;
            Record(); sample++;
            if(sample>=36) {Debug.Log("POISON_BUILD_PROBE_PASSED: 36 short probes; no dash or obstacle planning."); Finish(0);}
            else StartSample();
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("PoisonBuildProbe",false); Time.captureDeltaTime=oldCapture;
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original); if(profile!=null) UnityEngine.Object.Destroy(profile);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
