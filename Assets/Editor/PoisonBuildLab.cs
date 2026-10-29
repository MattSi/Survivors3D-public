using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class PoisonBuildLab : EditorWindow
{
    internal static readonly string[] Groups={"毒液Ⅱ", "毒液Ⅱ＋寒霜Ⅰ", "完整：毒液Ⅱ＋寒霜Ⅰ＋缓释阀", "同价：毒液Ⅱ＋寒霜Ⅱ＋移速", "诱饵：毒液Ⅱ＋寒霜Ⅰ＋灯笼Ⅰ"};
    internal static readonly string[] Scenarios={"普通追兵", "石甲追兵", "射手／投弹混合", "最终 Boss"};
    [SerializeField] int group=2,scenario,seed=31415;
    [SerializeField] string route="短弧回引";
    Vector2 scroll;
    static SurvivorPlayground game;
    static WaveConfiguration profile;
    static object original;
    static int activeGroup,activeScenario,activeSeed,initialSpent;
    [MenuItem("Tools/Survivors/Poison Build Lab")]
    public static void Open()=>GetWindow<PoisonBuildLab>("毒径牧群实验");
    void OnInspectorUpdate()=>Repaint();
    void OnGUI()
    {
        scroll=EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("毒径牧群",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("走短弧或马蹄形，让追兵踏入身后的毒池；靠近旧冰区转向，给减速争取更多毒伤。毒池重叠不叠伤，冲刺不加快喷洒。\n缓释阀：毒池多留 3 秒；喷洒 0.8 → 1.0 秒。全速跑时池间距也会增大。",MessageType.Info);
        group=EditorGUILayout.Popup("配装",group,Groups);
        scenario=EditorGUILayout.Popup("场景",scenario,Scenarios);
        seed=EditorGUILayout.IntField("种子",seed); route=EditorGUILayout.TextField("路线记录",route);
        if(GUILayout.Button(EditorApplication.isPlaying?"重新开始实验":"进入实验 Play Mode"))
        {
            SessionState.SetInt("PoisonBuild.Group",group); SessionState.SetInt("PoisonBuild.Scenario",scenario); SessionState.SetInt("PoisonBuild.Seed",seed);
            if(EditorApplication.isPlaying) StartExperiment();
            else if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
                SessionState.SetBool("PoisonBuild.Pending",true); EditorApplication.EnterPlaymode();
            }
        }
        if(game!=null)
        {
            var eq=game.Run.Equipment; var m=game.Metrics;
            EditorGUILayout.LabelField($"当前：{Groups[activeGroup]} / {Scenarios[activeScenario]}",EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"初始花费 {initialSpent}/100 · HP {game.Health.Current} · 本波 {m.Seconds:F1} 秒");
            EditorGUILayout.LabelField($"喷洒间隔 {eq.PoisonSprayInterval:F1}s · 毒池持续 {eq.PoisonPoolDuration:F1}s");
            EditorGUILayout.LabelField($"毒伤 {m.For(CombatMetrics.Source.Poison).Damage} / 击杀 {m.For(CombatMetrics.Source.Poison).Kills}");
            EditorGUILayout.LabelField($"寒霜伤害 {m.For(CombatMetrics.Source.Frost).Damage} · 减速期间毒伤 {m.PoisonDamageWhileSlowed}");
            EditorGUILayout.LabelField($"受伤 {m.DamageTaken} / {m.HitsTaken} 次");
            EditorGUILayout.HelpBox("前三组逐项添加组件；完整组与第四组均花 70 材料。固定初始预算 100，未花材料保留。每波重置统计，死亡和结算保留；导出前记录路线。",MessageType.None);
            if(GUILayout.Button("导出当前记录")) Export();
        }
        EditorGUILayout.EndScrollView();
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PoisonBuild.Pending",false))
            {SessionState.SetBool("PoisonBuild.Pending",false); EditorApplication.update+=WaitForGame;}
            if(state==PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update-=WaitForGame;
                if(game!=null) GameTestAccess.Set(game,"waveConfiguration",original);
                if(profile!=null) DestroyImmediate(profile);
                game=null; profile=null; original=null;
            }
        };
    }
    static void WaitForGame()
    {
        var found=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        if(found==null || found.Run==null || GameTestAccess.Get(found,"machete")==null) return;
        EditorApplication.update-=WaitForGame; StartExperiment();
    }
    internal static WaveConfiguration CreateProfile(int scenario)
    {
        var config=ScriptableObject.CreateInstance<WaveConfiguration>();
        var baseline=Resources.Load<WaveConfiguration>("Config/DefaultWaveConfiguration");
        config.Rules=baseline!=null?baseline.Snapshot():new WaveRules();
        config.name="Poison Build Lab (temporary)"; config.Rules.StartWave=scenario==3?config.Rules.SafeFinalWave:8;
        config.Rules.StartingMaterials=0; config.Rules.FinalBoss=true; config.Rules.Endless=false;
        config.Rules.TestMode=scenario==0?EnemyTestMode.NormalOnly:scenario==1?EnemyTestMode.StoneArmorOnly:EnemyTestMode.Mixed;
        return config;
    }
    internal static int Equip(RunEquipment equipment,int group)
    {
        equipment.Collect(100); int spent=0;
        void Buy(int id) {int cost=equipment.Price(id); if(!equipment.Buy(id)) throw new Exception("Invalid poison build purchase"); spent+=cost;}
        Buy(WeaponCatalog.ShopId(10));
        if(group>0) Buy(WeaponCatalog.ShopId(6));
        if(group==4) Buy(WeaponCatalog.ShopId(11));
        if(group==2) Buy(ItemLibrary.ShopId((int)RunItem.SlowReleaseValve));
        if(group==3) {Buy(WeaponCatalog.ShopId(6)); Buy(2);}
        return spent;
    }
    static void StartExperiment()
    {
        var found=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        if(found==null) {Debug.LogWarning("Open ForestCamp first."); return;}
        if(game!=found) original=GameTestAccess.Get(found,"waveConfiguration"); game=found;
        if(profile!=null) {GameTestAccess.Set(game,"waveConfiguration",original); DestroyImmediate(profile);}
        activeGroup=SessionState.GetInt("PoisonBuild.Group",2); activeScenario=SessionState.GetInt("PoisonBuild.Scenario",0); activeSeed=SessionState.GetInt("PoisonBuild.Seed",31415);
        profile=CreateProfile(activeScenario); GameTestAccess.Set(game,"waveConfiguration",profile);
        UnityEngine.Random.InitState(activeSeed); Time.timeScale=1; game.enabled=true;
        game.NewGame(); game.BeginRun(10); initialSpent=Equip(game.Run.Equipment,activeGroup); GameTestAccess.Call(game,"ApplyEquipment");
    }
    void Export()
    {
        Directory.CreateDirectory("Logs"); const string path="Logs/PoisonBuildLab.csv";
        if(!File.Exists(path)) File.WriteAllText(path,"group,scenario,seed,route,initial_spent,wave,seconds,hp,poison_damage,frost_damage,slowed_poison_damage,taken\n");
        var m=game.Metrics; string label="\""+(route??"").Replace("\"","\"\"")+"\"";
        File.AppendAllText(path,FormattableString.Invariant($"{activeGroup},{activeScenario},{activeSeed},{label},{initialSpent},{game.Run.Session.Wave},{m.Seconds:F2},{game.Health.Current},{m.For(CombatMetrics.Source.Poison).Damage},{m.For(CombatMetrics.Source.Frost).Damage},{m.PoisonDamageWhileSlowed},{m.DamageTaken}\n"));
        Debug.Log("Poison build record: "+Path.GetFullPath(path));
    }
}
