using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Isolated component comparisons; never edits the campaign asset.</summary>
public sealed class MeleeBuildLab : EditorWindow
{
    internal static readonly string[] Groups={"双刀＋范围", "双刀＋范围＋吸血", "完整：双刀＋范围＋吸血＋重锤", "自由配装（双刀开局）"};
    internal static readonly string[] Scenarios={"普通追兵", "石甲压力", "射手／投弹混合", "最终 Boss"};
    const string Pending="MeleeBuildLab.Pending";
    public const int Budget=100;
    [SerializeField] int group=2,scenario=2,seed=31415;
    [SerializeField] string tactic="贴边";
    Vector2 scroll;
    internal static SurvivorPlayground Active;
    static WaveConfiguration profile;
    static object original;
    static int activeGroup,activeScenario,activeSeed,spent;
    static bool preparing;

    [MenuItem("Tools/Survivors/Melee Build Lab")]
    public static void Open()=>GetWindow<MeleeBuildLab>("贴身构筑实验");
    void OnInspectorUpdate()=>Repaint();
    void OnGUI()
    {
        scroll=EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("贴身绞杀 · 第一轮实验",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("购买顺序：范围 → 吸血／重锤 → 双刀等级／暴击",EditorStyles.wordWrappedLabel);
        EditorGUILayout.HelpBox("沿敌群外缘接战；重锤控制后靠近，星星缩小时撤出。绿色地面脉冲代表实际回血。\n初始预算 100；前三组逐项添加组件，实际花费不同。自由组可按同样花费购买对照装备。",MessageType.Info);
        group=EditorGUILayout.Popup("装备组",group,Groups);
        scenario=EditorGUILayout.Popup("敌人场景",scenario,Scenarios);
        seed=EditorGUILayout.IntField("随机种子",seed);
        tactic=EditorGUILayout.TextField("走位记录",tactic);
        if(GUILayout.Button(EditorApplication.isPlaying?"重新开始此实验":"进入实验 Play Mode"))
        {
            SessionState.SetInt("MeleeBuildLab.Group",group); SessionState.SetInt("MeleeBuildLab.Scenario",scenario);
            SessionState.SetInt("MeleeBuildLab.Seed",seed);
            if(EditorApplication.isPlaying) StartExperiment();
            else if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
                SessionState.SetBool(Pending,true); EditorApplication.EnterPlaymode();
            }
        }
        if(Active!=null)
        {
            var m=Active.Metrics;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"当前：{Groups[activeGroup]} / {Scenarios[activeScenario]}",EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"种子 {activeSeed} · 已花 {spent} / {Budget} · HP {Active.Health.Current} · {m.Seconds:F1}s");
            foreach(CombatMetrics.Source source in Enum.GetValues(typeof(CombatMetrics.Source)))
            {
                var row=m.For(source);
                string name=source==CombatMetrics.Source.Blades?"双刀":source==CombatMetrics.Source.Hammer?"重锤":source==CombatMetrics.Source.Poison?"毒液":source==CombatMetrics.Source.Frost?"寒霜":"其他伤害";
                EditorGUILayout.LabelField($"{name}：伤害 {row.Damage} / 命中 {row.Hits} / 击杀 {row.Kills}");
            }
            EditorGUILayout.LabelField($"受伤 {m.DamageTaken}（{m.HitsTaken} 次） · 吸血 {m.LifeStolen} · 急救 {m.FirstAidHealing}");
            EditorGUILayout.LabelField($"眩晕期间双刀命中 {m.BladeHitsDuringStun} / 伤害 {m.BladeDamageDuringStun}");
            EditorGUILayout.HelpBox("每波重置，结算／死亡时保留。伤害不含过量伤害，回血不含溢出。种子固定初始条件，战斗随机调用会随走位分化。",MessageType.None);
            if(GUILayout.Button("追加记录到 Logs/MeleeBuildLab.csv")) Export(tactic);
            if(activeGroup==3) DrawPurchases();
        }
        EditorGUILayout.EndScrollView();
    }
    void DrawPurchases()
    {
        EditorGUILayout.HelpBox("自由组准备阶段暂停战斗与移动；按真实价格购买，最多三武器。开始后关闭购买按钮。",MessageType.None);
        using(new EditorGUI.DisabledScope(!preparing))
        {
            var eq=Active.Run.Equipment;
            var shop=new WaveShop(eq);
            var eligible=typeof(WaveShop).GetMethod("IsEligible",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            shop.Open(8);
            for(int item=0;item<RunEquipment.ItemCount;item++)
            {
                if(!(bool)eligible.Invoke(shop,new object[]{item})) continue;
                int weapon=WeaponCatalog.WeaponId(item);
                if(weapon>=0 && !eq.HasWeapon(weapon) && eq.EquippedCount>=3) continue;
                string name=weapon>=0?WeaponCatalog.Description[weapon].Split('，','：','。')[0]:item<3?new[]{"弹匣","范围强化","移速"}[item]:ItemLibrary.At(item).Name;
                int cost=eq.Price(item);
                using(new EditorGUI.DisabledScope(cost>eq.Materials))
                    if(GUILayout.Button($"{name} · {cost}"))
                        if(eq.Buy(item)) {spent+=cost; GameTestAccess.Call(Active,"ApplyEquipment");}
            }
            if(GUILayout.Button("开始自由组战斗"))
            {
                Active.Health.ShieldCharges=eq.Stacks(RunItem.EmergencyShield);
                GameTestAccess.Set(Active,"firstAidReady",eq.Stacks(RunItem.FirstAid)>0);
                preparing=false; Active.enabled=true; GameTestAccess.Call(Active,"SetPlayerEnabled",true);
            }
        }
    }
    [InitializeOnLoadMethod] static void Register()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false); EditorApplication.update+=WaitForGame;}
            if(state==PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update-=WaitForGame;
                if(Active!=null) GameTestAccess.Set(Active,"waveConfiguration",original);
                if(profile!=null) DestroyImmediate(profile);
                Active=null; profile=null; original=null; preparing=false;
            }
        };
    }
    static void WaitForGame()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        if(game==null || game.Run==null || GameTestAccess.Get(game,"machete")==null) return;
        EditorApplication.update-=WaitForGame; StartExperiment();
    }
    internal static WaveConfiguration CreateProfile(int scenario)
    {
        var config=ScriptableObject.CreateInstance<WaveConfiguration>();
        var baseline=Resources.Load<WaveConfiguration>("Config/DefaultWaveConfiguration");
        config.Rules=baseline!=null?baseline.Snapshot():new WaveRules();
        config.name="Melee Build Lab (runtime only)";
        config.Rules.StartWave=scenario==3?config.Rules.SafeFinalWave:8;
        config.Rules.Endless=false; config.Rules.StartingMaterials=0; config.Rules.FinalBoss=true;
        config.Rules.TestMode=scenario==0?EnemyTestMode.NormalOnly:scenario==1?EnemyTestMode.StoneArmorOnly:EnemyTestMode.Mixed;
        return config;
    }
    internal static int Equip(RunEquipment equipment,int group)
    {
        equipment.Collect(Budget); int spent=0;
        void Buy(int item) {int cost=equipment.Price(item); if(!equipment.Buy(item)) throw new Exception("Invalid melee preset"); spent+=cost;}
        if(group<3) Buy(1);
        if(group==1 || group==2) Buy(6+(int)RunItem.BloodPendant);
        if(group==2) Buy(WeaponCatalog.ShopId(5));
        return spent;
    }
    static void StartExperiment()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<SurvivorPlayground>();
        if(game==null) {Debug.LogWarning("Open ForestCamp first."); return;}
        if(Active!=game) original=GameTestAccess.Get(game,"waveConfiguration");
        Active=game; activeGroup=SessionState.GetInt("MeleeBuildLab.Group",2);
        activeScenario=SessionState.GetInt("MeleeBuildLab.Scenario",2); activeSeed=SessionState.GetInt("MeleeBuildLab.Seed",31415);
        if(profile!=null) {GameTestAccess.Set(game,"waveConfiguration",original); DestroyImmediate(profile);}
        profile=CreateProfile(activeScenario); GameTestAccess.Set(game,"waveConfiguration",profile);
        Time.timeScale=1; UnityEngine.Random.InitState(activeSeed);
        game.enabled=true; game.NewGame(); game.BeginRun(1);
        spent=Equip(game.Run.Equipment,activeGroup); GameTestAccess.Call(game,"ApplyEquipment");
        preparing=activeGroup==3;
        if(preparing) {game.enabled=false; GameTestAccess.Call(game,"SetPlayerEnabled",false);}
    }
    static void Export(string tactic)
    {
        Directory.CreateDirectory("Logs"); const string path="Logs/MeleeBuildLab.csv";
        if(!File.Exists(path)) File.WriteAllText(path,"group,scenario,seed,tactic,spent,wave,seconds,hp,blade_damage,hammer_damage,other_damage,blade_hits,blade_kills,hammer_hits,hammer_kills,damage_taken,hits_taken,lifesteal,first_aid,controlled_blade_hits,controlled_blade_damage\n");
        var m=Active.Metrics; var b=m.For(CombatMetrics.Source.Blades); var h=m.For(CombatMetrics.Source.Hammer);
        string label="\""+(tactic??"").Replace("\"","\"\"")+"\"";
        File.AppendAllText(path,FormattableString.Invariant($"{activeGroup},{activeScenario},{activeSeed},{label},{spent},{Active.Run.Session.Wave},{m.Seconds:F2},{Active.Health.Current},{b.Damage},{h.Damage},{m.For(CombatMetrics.Source.Other).Damage},{b.Hits},{b.Kills},{h.Hits},{h.Kills},{m.DamageTaken},{m.HitsTaken},{m.LifeStolen},{m.FirstAidHealing},{m.BladeHitsDuringStun},{m.BladeDamageDuringStun}\n"));
        Debug.Log("Melee build record appended: "+Path.GetFullPath(path));
    }
}
