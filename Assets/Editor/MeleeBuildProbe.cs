using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Short movement probes, not human playtesting or a campaign completion test.</summary>
public static class MeleeBuildProbe
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const float Step=.05f,Duration=20;
    static SurvivorPlayground game;
    static PlayerController player;
    static WaveConfiguration profile;
    static object original;
    static MethodInfo combatTick;
    static int sample=-1,lastFrame;
    static float age,oldCapture;
    static double ready;
    static int Group=>sample/9;
    static int Scenario=>sample/3%3;
    static int Policy=>sample%3;
    [MenuItem("Tools/Survivors/Probe Melee Build Movement")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Run outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("MeleeBuildProbe",true); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MeleeBuildProbe",false)) return;
            ready=EditorApplication.timeSinceStartup+2; sample=-1; EditorApplication.update+=Tick;
        };
    }
    static void StartSample()
    {
        if(profile!=null) {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
        profile=MeleeBuildLab.CreateProfile(Scenario); GameTestAccess.Set(game,"waveConfiguration",profile);
        UnityEngine.Random.InitState(31415); game.NewGame(); game.BeginRun(1); game.enabled=false; player.enabled=false;
        var eq=game.Run.Equipment; MeleeBuildLab.Equip(eq,Group);
        // 63 materials, same as the full core: range + crit + tier two blades + boots.
        if(Group==3)
            foreach(int item in new[]{1,6+(int)RunItem.Whetstone,WeaponCatalog.ShopId(1),2})
                if(!eq.Buy(item)) throw new Exception("Cannot buy comparison equipment");
        GameTestAccess.Call(game,"ApplyEquipment"); age=0;
        game.Health.TakeDamage(10); // Same recoverable deficit; exclude it from incoming combat damage.
        game.Metrics.Reset();
    }
    static Vector3 Direction()
    {
        if(Policy==2) return Vector3.zero;
        Vector3 at=player.transform.position; at.y=0;
        if(Policy==1)
        {
            Vector3 radial=at.sqrMagnitude<.01f?Vector3.right:at.normalized;
            return (Vector3.Cross(Vector3.up,radial)+radial*Mathf.Clamp((12-at.magnitude)*.7f,-1,1)).normalized;
        }
        var enemies=(List<Transform>)GameTestAccess.Get(game,"enemies");
        Vector3 nearest=Vector3.zero; float distance=float.PositiveInfinity;
        foreach(var enemy in enemies)
        {
            Vector3 offset=enemy.position-player.transform.position; offset.y=0;
            if(offset.sqrMagnitude<distance) {distance=offset.sqrMagnitude; nearest=offset;}
        }
        if(float.IsInfinity(distance)) return Vector3.zero;
        float gap=Mathf.Sqrt(distance); Vector3 toward=nearest.normalized;
        return (Vector3.Cross(Vector3.up,toward)*.7f+toward*Mathf.Clamp((gap-2.35f)*1.5f,-1.5f,1)).normalized;
    }
    static void Record()
    {
        var m=game.Metrics; var b=m.For(CombatMetrics.Source.Blades); var h=m.For(CombatMetrics.Source.Hammer);
        string row=FormattableString.Invariant($"{Group},{Scenario},{Policy},{m.Seconds:F2},{game.Health.Current},{b.Damage},{b.Kills},{h.Damage},{h.Kills},{m.DamageTaken},{m.LifeStolen},{m.BladeDamageDuringStun}\n");
        File.AppendAllText("Logs/MeleeBuildProbe.csv",row);
        Debug.Log("MELEE_BUILD_PROBE "+row.Trim());
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<ready || lastFrame==Time.frameCount) return;
        lastFrame=Time.frameCount;
        try
        {
            if(sample<0)
            {
                game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
                player=(PlayerController)GameTestAccess.Get(game,"player"); original=GameTestAccess.Get(game,"waveConfiguration");
                combatTick=typeof(CombatController).GetMethod("TickCombat",F);
                oldCapture=Time.captureDeltaTime; Time.captureDeltaTime=Step;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/MeleeBuildProbe.csv","group,scenario,policy,seconds,hp,blade_damage,blade_kills,hammer_damage,hammer_kills,taken,lifesteal,controlled_blade_damage\n");
                sample=0; StartSample(); return;
            }
            Vector3 direction=Direction();
            player.GetComponent<CharacterController>().Move(direction*(5*player.SpeedMultiplier*Step));
            player.GetComponent<PlayerAvatar>()?.Face(direction);
            game.Run.Session.Advance(Step); combatTick.Invoke(game,new object[]{Step}); age+=Step;
            if(age<Duration && !game.Health.IsDead) return;
            Record(); sample++;
            if(sample>=36) {Debug.Log("MELEE_BUILD_PROBE_PASSED: 36 short probes; policies have no dash or obstacle planning."); Finish(0);}
            else StartSample();
        }
        catch(Exception error) {Debug.LogException(error); Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick; SessionState.SetBool("MeleeBuildProbe",false);
        Time.captureDeltaTime=oldCapture;
        if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original);
        if(profile!=null) UnityEngine.Object.Destroy(profile);
        if(Application.isBatchMode) EditorApplication.Exit(code); else EditorApplication.ExitPlaymode();
    }
}
