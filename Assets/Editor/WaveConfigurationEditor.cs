using UnityEditor;
using UnityEngine;

public static class WaveConfigurationTools
{
    public const string DefaultPath="Assets/Resources/Config/DefaultWaveConfiguration.asset";
    public static WaveConfiguration EnsureDefault()
    {
        var asset=AssetDatabase.LoadAssetAtPath<WaveConfiguration>(DefaultPath);
        if(asset!=null) return asset;
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Config")) AssetDatabase.CreateFolder("Assets/Resources","Config");
        asset=ScriptableObject.CreateInstance<WaveConfiguration>();
        AssetDatabase.CreateAsset(asset,DefaultPath); AssetDatabase.SaveAssets(); return asset;
    }
    [MenuItem("Tools/Survivors/Wave Configuration")]
    public static void Open()=>WaveConfigurationWindow.ShowTable(EnsureDefault());
}

[CustomEditor(typeof(WaveConfiguration))]
public sealed class WaveConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("波次与敌人配置。修改在开始新局时生效；本局使用独立副本。",MessageType.Info);
        if(GUILayout.Button("打开配置表 / Open Table")) WaveConfigurationWindow.ShowTable((WaveConfiguration)target);
        DrawDefaultInspector();
    }
}

public sealed class WaveConfigurationWindow : EditorWindow
{
    [SerializeField] WaveConfiguration asset;
    Vector2 scroll;
    public static void ShowTable(WaveConfiguration config)
    {
        var window=GetWindow<WaveConfigurationWindow>("波次配置"); window.asset=config;
        window.minSize=new Vector2(900,600); window.Show();
    }
    void OnGUI()
    {
        asset=(WaveConfiguration)EditorGUILayout.ObjectField("配置资产",asset,typeof(WaveConfiguration),false);
        if(asset==null) { if(GUILayout.Button("打开默认配置")) asset=WaveConfigurationTools.EnsureDefault(); return; }
        EditorGUILayout.HelpBox("开始新局时读取配置。默认资产自动用于两个场景；其他配置可拖到 SurvivorPlayground 的 Wave Configuration。混合模式优先级：冲锋 → 石甲 → 投弹 → 分裂 → 骨针射手。ShooterOnly 可单独测试射手。",MessageType.Info);
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("保存")) AssetDatabase.SaveAssets();
            if(GUILayout.Button("复制为测试配置"))
            {
                string path=EditorUtility.SaveFilePanelInProject("另存配置","TestWaveConfiguration","asset","保存配置副本");
                if(!string.IsNullOrEmpty(path))
                {
                    var copy=Instantiate(asset); AssetDatabase.CreateAsset(copy,path); AssetDatabase.SaveAssets(); asset=copy;
                }
            }
            if(GUILayout.Button("在 Project 中定位")) EditorGUIUtility.PingObject(asset);
        }
        var serialized=new SerializedObject(asset); serialized.Update();
        var rules=serialized.FindProperty("Rules");
        scroll=EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("运行与测试",EditorStyles.boldLabel);
        Field(rules,"Endless","无尽模式"); Field(rules,"FinalWave","普通模式总波数");
        Field(rules,"FinalBoss","最终波林地石王（仅普通 Mixed 模式）"); Field(rules,"BossHealth","林地石王血量");
        Field(rules,"StartWave","起始波次"); Field(rules,"StartingMaterials","开局测试材料");
        Field(rules,"TestMode","敌人测试模式");
        EditorGUILayout.Space(); EditorGUILayout.LabelField("敌人基础属性：与出现波次无关",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("实际血量 = 敌人基础血量 × 波次血量倍率。倍率为 1 时，第一波测试与后期登场血量相同。Normal 共用三种普通外形；Splitling 是分裂小怪，独立设置血量，不在传送门刷出。",MessageType.None);
        var definitions=rules.FindPropertyRelative("EnemyDefinitions");
        int removeDefinition=-1;
        using(new EditorGUILayout.HorizontalScope()) {GUILayout.Label("敌人类型",GUILayout.Width(220)); GUILayout.Label("基础血量",GUILayout.Width(120));}
        for(int i=0;i<definitions.arraySize;i++)
        {
            var row=definitions.GetArrayElementAtIndex(i);
            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(row.FindPropertyRelative("Kind"),GUIContent.none,GUILayout.Width(220));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("BaseHealth"),GUIContent.none,GUILayout.Width(120));
                if(GUILayout.Button("删",GUILayout.Width(36))) removeDefinition=i;
            }
        }
        if(removeDefinition>=0) definitions.DeleteArrayElementAtIndex(removeDefinition);
        if(GUILayout.Button("添加敌人属性")) definitions.InsertArrayElementAtIndex(definitions.arraySize);
        EditorGUILayout.HelpBox("Mixed=正常混合；NormalOnly=普通怪；SplitterOnly=分裂怪及其子怪；其余 Only=只刷指定怪（忽略解锁波次和生成间隔，保留启用开关、存活及攻击上限）。从第 11 波开始可测试完整阵容。",MessageType.None);
        EditorGUILayout.LabelField("波次表：缺失波次沿用前一行；超出最后一行使用下方增长参数",EditorStyles.boldLabel);
        var waves=rules.FindPropertyRelative("Waves");
        using(new EditorGUILayout.HorizontalScope())
        {
            foreach(string label in new[] {"波次","时长/秒","开场人数","只/秒","人数上限","血量倍率","基础移速"}) GUILayout.Label(label,GUILayout.Width(100));
        }
        int remove=-1;
        for(int i=0;i<waves.arraySize;i++)
        {
            var row=waves.GetArrayElementAtIndex(i);
            using(new EditorGUILayout.HorizontalScope())
            {
                foreach(string field in new[] {"Wave","Duration","InitialEnemies","SpawnRate","EnemyLimit","HealthMultiplier","EnemySpeed"})
                    EditorGUILayout.PropertyField(row.FindPropertyRelative(field),GUIContent.none,GUILayout.Width(100));
                if(GUILayout.Button("删",GUILayout.Width(36))) remove=i;
            }
        }
        if(remove>=0) waves.DeleteArrayElementAtIndex(remove);
        if(GUILayout.Button("添加波次行"))
        {
            int at=waves.arraySize; waves.InsertArrayElementAtIndex(at);
            waves.GetArrayElementAtIndex(at).FindPropertyRelative("Wave").intValue=at==0?1:waves.GetArrayElementAtIndex(at-1).FindPropertyRelative("Wave").intValue+1;
        }
        EditorGUILayout.Space(); EditorGUILayout.LabelField("特殊敌人阶段表",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("同类型按生效波次取最近的一行，后续一直生效。每N只=每第N次成功生成替换普通怪。关闭一行可停止该类型；石甲/分裂的同时攻击填0。分裂存活上限只计算母体；小怪计入总人数，每只母体预留一个分裂名额。",MessageType.None);
        var enemies=rules.FindPropertyRelative("Enemies");
        using(new EditorGUILayout.HorizontalScope())
        {
            foreach(string label in new[] {"启用","类型","生效波次","每N只","存活上限","同时攻击"}) GUILayout.Label(label,GUILayout.Width(116));
        }
        remove=-1;
        for(int i=0;i<enemies.arraySize;i++)
        {
            var row=enemies.GetArrayElementAtIndex(i);
            using(new EditorGUILayout.HorizontalScope())
            {
                foreach(string field in new[] {"Enabled","Kind","FromWave","Every","MaxAlive","MaxAttacks"})
                    EditorGUILayout.PropertyField(row.FindPropertyRelative(field),GUIContent.none,GUILayout.Width(116));
                if(GUILayout.Button("删",GUILayout.Width(36))) remove=i;
            }
        }
        if(remove>=0) enemies.DeleteArrayElementAtIndex(remove);
        if(GUILayout.Button("添加敌人阶段")) enemies.InsertArrayElementAtIndex(enemies.arraySize);
        EditorGUILayout.Space(); EditorGUILayout.LabelField("末段加速与全局上限",EditorStyles.boldLabel);
        Field(rules,"EndOfWaveSpawnMultiplier","波末刷怪倍率"); Field(rules,"MaxEnemies","场上总人数硬上限（最多240）");
        Field(rules,"MaxInitialEnemies","开场人数硬上限"); Field(rules,"MaxSpawnRate","最终刷怪速度上限（只/秒）"); Field(rules,"MaxEnemySpeed","基础移速上限");
        EditorGUILayout.LabelField("超过波次表后：每波增加",EditorStyles.boldLabel);
        Field(rules,"ExtraInitialPerWave","开场人数增长"); Field(rules,"ExtraLimitPerWave","人数上限增长");
        Field(rules,"ExtraSpawnRatePerWave","基础刷怪速度增长"); Field(rules,"ExtraHealthMultiplierPerWave","血量倍率增长（加法）"); Field(rules,"ExtraSpeedPerWave","基础移速增长");
        EditorGUILayout.HelpBox("无尽模式继续保留每波商店；超过表尾后时长沿用最后一行，特殊敌人沿用最后阶段。重复波次或重复类型/阶段：靠后的一行生效。非法数值在运行时会限制到安全范围。",MessageType.None);
        EditorGUILayout.EndScrollView();
        serialized.ApplyModifiedProperties();
    }
    static void Field(SerializedProperty rules,string name,string label)=>EditorGUILayout.PropertyField(rules.FindPropertyRelative(name),new GUIContent(label));
}
