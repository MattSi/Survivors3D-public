using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A self-contained arena. Environment references are authored by the editor builder.</summary>
public partial class CombatController : MonoBehaviour, ICombatView
{
    protected internal ArenaObstacles obstacles;
    protected internal const float ArenaHalfSize = 18f;
    protected internal const float EnemySize = 0.75f;
    protected internal const float BulletRadius = 0.2f;
    [SerializeField] protected internal PlayerController player;
    [SerializeField] protected internal PlayerHealth health;
    [SerializeField] protected internal Renderer playerRenderer;
    [SerializeField] protected internal Material enemyMaterial;
    [SerializeField] protected internal Material bulletMaterial;
    [SerializeField] protected internal Material playerMaterial;
    [SerializeField] protected internal Material hitMaterial;
    [Header("Slot 1 / Gun")]
    [SerializeField, Min(1)] protected internal int bulletDamage = 6;
    [SerializeField, Min(1f)] protected internal float bulletRange = 14f;
    [SerializeField, Min(0.1f)] protected internal float shotInterval = 0.7f;
    [Header("Wave configuration (empty uses Resources/Config/DefaultWaveConfiguration)")]
    [SerializeField] protected internal WaveConfiguration waveConfiguration;
    internal WaveRules LoadWaveRules()
    {
        var config=waveConfiguration!=null?waveConfiguration:Resources.Load<WaveConfiguration>("Config/DefaultWaveConfiguration");
        return config!=null?config.Snapshot():new WaveRules();
    }
    private WaveRules cachedWaveRules;
    private WaveRow cachedWave;
    protected internal WaveRow WaveSettings
    {
        get
        {
            if(cachedWave==null || cachedWave.Wave!=session.Wave || cachedWaveRules!=session.Rules)
            {cachedWaveRules=session.Rules; cachedWave=cachedWaveRules.At(session.Wave);}
            return cachedWave;
        }
    }
    protected internal float WaveEnemySpeed=>WaveSettings.EnemySpeed;
    protected internal EnemyKind NextEnemyKind=>session.Rules.SelectEnemy(session.Wave,waveSpawnCount+1,chargers.Count,stoneArmors.Count,bombers.Count,splitters.Count,shooters.Count);
    [SerializeField, Range(.1f,.5f)] private float playerContactHalfSize=.3f;
    [SerializeField, Min(1f)] protected internal float bulletSpeed = 30f;
    [Header("Slot 2 / Machete")]
    [SerializeField, Min(1)] protected internal int meleeDamage = 10;
    [SerializeField, Min(0.4f)] protected internal float meleeInterval = 1.25f;
    [SerializeField, Min(1f)] protected internal float meleeRange = 2.2f;
    protected internal Transform machete;
    protected internal readonly List<Transform> macheteArms = new List<Transform>();
    protected internal Material bladeMaterial;
    protected internal Material gripMaterial;
    protected internal Mesh bladeMesh;
    protected internal const float BladeOrbitPeriod = 2f;
    protected internal float bladeOrbitAngle;
    protected internal readonly Dictionary<Transform, float> meleeReadyAt = new Dictionary<Transform, float>();
    protected internal float meleeFlashUntil;

    protected internal sealed class Bullet
    {
        public Transform Transform;
        public Vector3 Direction;
        public float RemainingRange;
        public int Damage;
        public int Pierce, Bounces;
        public float BlastRadius;
        public bool Secondary;
        public int Weapon;
        public bool? Critical;
        public readonly HashSet<Transform> HitTargets=new HashSet<Transform>();
    }

    protected internal readonly List<Transform> enemies = new List<Transform>();
    protected internal readonly Dictionary<string, Material> enemyModelMaterials = new Dictionary<string, Material>();
    protected internal static readonly string[] EnemyModels = { "Characters/EnemyImp", "Characters/EnemyHood", "Characters/EnemyRock" };
    protected internal readonly Dictionary<Transform, int> enemyHealth = new Dictionary<Transform, int>();
    protected internal readonly List<Bullet> bullets = new List<Bullet>();
    protected internal float nextShot;
    protected internal float nextSpawn;
    protected internal int InitialEnemies => Mathf.Min(WaveSettings.InitialEnemies,EnemyLimit);
    protected internal int SpawnBatch => 1;
    protected internal int EnemyLimit => Mathf.Min(session.Rules.PopulationLimit,Mathf.RoundToInt(WaveSettings.EnemyLimit*equipment.DangerMultiplier));
    protected internal float WaveSpawnInterval => 1/session.Rules.SpawnRate(session.Wave,session.Remaining,session.WaveDuration,equipment.DangerMultiplier);
    protected internal PlaygroundCamera viewCamera;
    protected internal float nextCritShakeAt;
    protected internal sealed class Pickup { public Transform Transform; public bool Attracted, ScoreOnly; public float ExpiresAt; }
    protected internal readonly List<Pickup> pickups = new List<Pickup>();
    protected internal Material pickupMaterial;
    protected internal float AttackRange => (equipment.Machete.Evolved ? 3.6f : equipment.Machete.Range(meleeRange)) * equipment.RangeMultiplier;
    protected internal float GunRange => equipment.Gun.Range(bulletRange);
    protected internal float GunInterval => equipment.Gun.Interval(shotInterval) * equipment.FireMultiplier;
    protected internal float MacheteInterval => equipment.Machete.Interval(meleeInterval);
    protected internal PlayerAvatar playerAvatar;
    public PlayerHealth Health => health;
    public int EnemyCount => enemies.Count;
    public int BulletCount => bullets.Count;
    public int Kills => kills;
    public int Score => kills * 10 + lootScore;
    public CombatMetrics Metrics { get; } = new CombatMetrics();

    public void Configure(PlayerController controller, PlayerHealth playerHealth, Renderer body,
        Material enemy, Material bullet, Material red, Material hit)
    {
        player = controller;
        health = playerHealth;
        playerRenderer = body;
        enemyMaterial = enemy;
        bulletMaterial = bullet;
        playerMaterial = red;
        hitMaterial = hit;
    }

    protected internal void InitializeCombat()
    {
        playerAvatar = player.GetComponent<PlayerAvatar>();
        if (playerAvatar == null) playerAvatar = player.gameObject.AddComponent<PlayerAvatar>();
        health.Died += ReportDeath;
        health.Damaged += OnPlayerDamaged;

        CreateMachete();
        CreateFlamethrower();
        pickupMaterial = new Material(enemyMaterial) { name = "Material Crystal", color = new Color(0.15f, 0.95f, 0.55f) };
        viewCamera = FindFirstObjectByType<PlaygroundCamera>();
        GameObject arena = GameObject.Find("Forest Camp Arena");
        if (arena != null) obstacles = arena.GetComponent<ArenaObstacles>() ?? arena.AddComponent<ArenaObstacles>();
    }

    protected internal void DisposeCombat()
    {
        ClearForestBoss();
        ClearSplitters();
        DisposeBombers();
        DisposeShooters();
        ClearChargers();
        if(chargeWarningMaterial!=null) Destroy(chargeWarningMaterial);
        if(chargeWarningMesh!=null) Destroy(chargeWarningMesh);
        DisposeNewWeapons();
        if (health != null) { health.Died -= ReportDeath; health.Damaged -= OnPlayerDamaged; }

        if (pickupMaterial != null) Destroy(pickupMaterial);
        if (bladeMaterial != null) Destroy(bladeMaterial);
        if (gripMaterial != null) Destroy(gripMaterial);
        if (bladeMesh != null) Destroy(bladeMesh);

        DisposeFlamethrower();
        if (spawnMaterial != null) Destroy(spawnMaterial);
        foreach (Material material in enemyModelMaterials.Values) Destroy(material);
    }

    protected internal void ClearCombatObjects()
    {
        ClearForestBoss();
        GameAudio.Current?.StopWorld();
        player.ResetDash();
        ClearSplitters();
        ClearBombers();
        ClearShooters();
        ClearStoneArmors();
        ClearChargers();
        ClearNewWeaponState();
        ClearRifle();
        if (playerAvatar != null) playerAvatar.EndCelebration();
        pendingEffects.Clear();
        ClearFlameState();
        ClearImpacts();
        floatingScores.Clear(); floatingDamage.Clear(); nextCritShakeAt=0;
        nextBloodWave=0; bloodFrenzyUntil=0;
        foreach (Transform enemy in enemies) Destroy(enemy.gameObject);
        foreach (Bullet bullet in bullets) Destroy(bullet.Transform.gameObject);
        enemies.Clear();
        enemyHealth.Clear();
        meleeReadyAt.Clear();
        bullets.Clear();
    }

    protected internal void StartWave()
    {
        Metrics.Reset();
        GameAudio.Current?.StopWorld();
        GameAudio.Current?.SetMusic(1); GameAudio.Play(AudioCue.WaveStart);
        player.ResetDash(); nextCritShakeAt=0;
        waveSpawnCount=0;
        BeginItemWave();
        CharacterController character = player.GetComponent<CharacterController>();
        character.enabled = false;
        player.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
        character.enabled = true;
        player.enabled = true;
        if (viewCamera != null && viewCamera.ThirdPerson && playerAvatar != null) playerAvatar.Face(Vector3.forward);
        SetPlayerHit(false);
        nextShot = 0f;
        nextSpawn = WaveSpawnInterval;
        bladeOrbitAngle = 0f;
        meleeReadyAt.Clear();
        machete.localRotation = Quaternion.identity;
        UpdateBladeLength();
        if(session.IsBossWave) StartForestBoss();
        else for (int i = 0; i < InitialEnemies; i++) SpawnEnemy();
    }

    protected internal void SetPlayerHit(bool hit)
    {
        playerRenderer.sharedMaterial = hit ? hitMaterial : playerMaterial;
        if (playerAvatar != null) playerAvatar.SetHit(hit);
    }

    protected internal void LateUpdate()
    {
        // A fixed arena keeps every spawn visible; constrain the existing movement component.
        Vector3 position = player.transform.position;
        Vector3 bounded = new Vector3(Mathf.Clamp(position.x, -17.4f, 17.4f), 0.5f,
            Mathf.Clamp(position.z, -17.4f, 17.4f));
        if ((position - bounded).sqrMagnitude > 0.000001f)
        {
            CharacterController character = player.GetComponent<CharacterController>();
            character.enabled = false;
            player.transform.position = bounded;
            character.enabled = true;
        }
    }

    protected internal void CreateMachete()
    {
        machete = new GameObject("Machete").transform;
        machete.SetParent(player.transform, false);
        Transform arm = new GameObject("Right Blade").transform;
        arm.SetParent(machete, false);
        arm.localRotation = Quaternion.Euler(0f, 90f, 0f);
        bladeMaterial = new Material(enemyMaterial) { name = "Machete Steel", color = new Color(.54f, .59f, .61f) };
        bladeMaterial.SetFloat("_Metallic", 0.2f);
        bladeMaterial.SetFloat("_Smoothness", 0.15f);
        gripMaterial = new Material(enemyMaterial) { name = "Machete Grip", color = new Color(0.12f, 0.075f, 0.04f) };
        Material wood = HammerMaterial("Blade Warm Timber", new Color(.46f,.25f,.105f));
        Material iron = HammerMaterial("Blade Charcoal Iron", new Color(.16f,.17f,.17f));
        Material wrap = HammerMaterial("Blade Tan Wrap", new Color(.63f,.48f,.29f));
        Material edge = HammerMaterial("Blade Sharpened Edge", new Color(.79f,.83f,.82f));
        HammerPart(arm,"Wood Grip",new Vector3(0,.1f,.76f),new Vector3(.23f,.23f,.46f),.025f,wood);
        HammerPart(arm,"Pommel",new Vector3(0,.1f,.54f),new Vector3(.28f,.27f,.08f),.02f,iron);
        for(int i=0;i<2;i++)
            HammerPart(arm,"Grip Wrap",new Vector3(0,.1f,.66f+i*.2f),new Vector3(.245f,.245f,.065f),.012f,wrap);
        HammerPart(arm,"Guard",new Vector3(0,.1f,1f),new Vector3(.62f,.25f,.14f),.025f,iron);

        // Separate face vertices retain flat normals. The inset steel face leaves a pale bevel.
        Vector2[] outline = { new Vector2(-.22f,1.04f),new Vector2(.22f,1.04f),
            new Vector2(.32f,2.12f),new Vector2(.08f,2.5f),new Vector2(-.22f,2.3f) };
        Vector2[] inset = { new Vector2(-.19f,1.07f),new Vector2(.14f,1.07f),
            new Vector2(.23f,2.1f),new Vector2(.05f,2.39f),new Vector2(-.19f,2.28f) };
        var vertices = new List<Vector3>();
        var steelTriangles = new List<int>();
        var edgeTriangles = new List<int>();
        void Face(Vector3 a,Vector3 b,Vector3 c,List<int> indices)
        {
            int start=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
            indices.Add(start); indices.Add(start+1); indices.Add(start+2);
        }
        Vector3 Point(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
        for(int side=0;side<2;side++)
        {
            float y=side==0?.19f:.01f;
            for(int i=1;i<inset.Length-1;i++)
            {
                if(side==0) Face(Point(inset[0],y),Point(inset[i+1],y),Point(inset[i],y),steelTriangles);
                else Face(Point(inset[0],y),Point(inset[i],y),Point(inset[i+1],y),steelTriangles);
            }
            for(int i=0;i<outline.Length;i++)
            {
                int j=(i+1)%outline.Length;
                Vector3 a=Point(outline[i],.1f),b=Point(outline[j],.1f),c=Point(inset[j],y),d=Point(inset[i],y);
                if(side==0) { Face(a,d,b,edgeTriangles); Face(b,d,c,edgeTriangles); }
                else { Face(a,b,d,edgeTriangles); Face(b,c,d,edgeTriangles); }
            }
        }
        bladeMesh = new Mesh { name="Short Beveled Machete", subMeshCount=2 };
        bladeMesh.SetVertices(vertices);
        bladeMesh.SetTriangles(steelTriangles,0); bladeMesh.SetTriangles(edgeTriangles,1);
        bladeMesh.RecalculateNormals(); bladeMesh.RecalculateBounds();
        GameObject blade = new GameObject("Blade", typeof(MeshFilter), typeof(MeshRenderer));
        blade.transform.SetParent(arm, false);
        blade.GetComponent<MeshFilter>().sharedMesh = bladeMesh;
        blade.GetComponent<MeshRenderer>().sharedMaterials = new[] {bladeMaterial,edge};
        macheteArms.Add(arm);
        Transform copy = Instantiate(arm, machete, false);
        copy.name = "Left Blade";
        copy.localRotation = Quaternion.Euler(0f, 270f, 0f);
        macheteArms.Add(copy);
        UpdateBladeLength();
    }

    private float nextBloodWave;
    private float bloodFrenzyUntil;
    private Material bloodWaveMaterial;

    protected internal void EnsureMacheteArms()
    {
        if (machete == null || macheteArms.Count == 0) return;
        if (equipment.Machete.Evolved)
        {
            while (macheteArms.Count < 4)
            {
                Transform extra = Instantiate(macheteArms[0], machete, false);
                extra.name = $"Extra Blade {macheteArms.Count + 1}";
                macheteArms.Add(extra);
            }
            for (int i = 0; i < macheteArms.Count; i++) macheteArms[i].gameObject.SetActive(i < 4);
        }
        else
        {
            for (int i = 2; i < macheteArms.Count; i++) macheteArms[i].gameObject.SetActive(false);
            if (macheteArms.Count >= 2)
            {
                macheteArms[0].gameObject.SetActive(true);
                macheteArms[1].gameObject.SetActive(true);
            }
        }
    }

    protected internal void UpdateBladeLength()
    {
        EnsureMacheteArms();
        for (int i = 0; i < macheteArms.Count; i++) SetBladePose(i);
    }

    protected internal void SetBladePose(int index)
    {
        if (index >= macheteArms.Count) return;
        Transform arm = macheteArms[index];
        float scale = (AttackRange - 0.5f) / 2f;
        float angle = index == 0 ? 90f : index == 1 ? 270f : index == 2 ? 0f : 180f;
        arm.localRotation = Quaternion.Euler(0f, angle, 0f);
        arm.localScale = new Vector3(1f, 1f, scale);
        arm.localPosition = arm.localRotation * new Vector3(0f, 0f, 0.5f - 0.5f * scale);
    }

    protected internal void UpdateMelee(float dt)
    {
        EnsureMacheteArms();
        Color bladeCol = equipment.Machete.Evolved ? new Color(0.95f, 0.14f, 0.2f) : new Color(.54f, .59f, .61f);
        bladeMaterial.color = Time.time < meleeFlashUntil ? Color.white : bladeCol;
        float orbitSpeed = equipment.Machete.Evolved ? 300f : (360f / BladeOrbitPeriod);
        float travel = orbitSpeed * dt;
        float previous = bladeOrbitAngle;
        bladeOrbitAngle = Mathf.Repeat(bladeOrbitAngle + travel, 360f);
        machete.localRotation = Quaternion.Euler(0f, bladeOrbitAngle, 0f);
        player.SpeedMultiplier = equipment.SpeedMultiplier * (Time.time < bloodFrenzyUntil ? 1.35f : 1f);
        int bladeCount = equipment.Machete.Evolved ? 4 : 2;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Transform enemy = enemies[i];
            if (meleeReadyAt.TryGetValue(enemy, out float readyAt) && Time.time < readyAt) continue;
            Vector3 offset = player.transform.InverseTransformPoint(enemy.position);
            bool hit = false;
            for (int blade = 0; blade < bladeCount; blade++)
            {
                float baseAngle = blade == 0 ? 90f : blade == 1 ? 270f : blade == 2 ? 0f : 180f;
                // Sweep the full angular distance for this frame, including wraparound at 360°.
                float middle = previous + baseAngle + travel / 2f;
                if (!FixedBladeHits.InSweptArc(offset.x, offset.z, AttackRange, EnemyRadius(enemy),
                    middle, -travel / 2f, travel / 2f)) continue;
                hit = true;
                break;
            }
            if (!hit || (obstacles != null && !obstacles.Clear(player.transform.position, enemy.position))) continue;
            GameAudio.Play(AudioCue.Blade);
            meleeReadyAt[enemy] = Time.time + (equipment.Machete.Evolved ? MacheteInterval * 0.75f : MacheteInterval);
            DamageWeaponEnemy(i, equipment.Machete.Damage(meleeDamage), HitKind.Melee, 1, null);
            meleeFlashUntil = Time.time + 0.12f;
            bladeMaterial.color = Color.white;
        }
        if (equipment.Machete.Evolved)
        {
            nextBloodWave -= dt;
            if (nextBloodWave <= 0f)
            {
                nextBloodWave = 1.5f;
                TriggerBloodWave();
            }
        }
    }

    private void TriggerBloodWave()
    {
        if (bloodWaveMaterial == null)
        {
            bloodWaveMaterial = HammerMaterial("Blood Wave", new Color(1f, 0.08f, 0.2f));
            bloodWaveMaterial.EnableKeyword("_EMISSION");
            bloodWaveMaterial.SetColor("_EmissionColor", new Color(3f, 0.25f, 0.4f));
        }
        Vector3 pos = player.transform.position + Vector3.up * 0.08f;
        impacts.Add(new Impact { Ring = Ring("Blood Wave", pos, 7.5f, bloodWaveMaterial, 0.14f), Duration = 0.45f });
        GameAudio.Play(AudioCue.Blade);
        int waveDmg = Mathf.RoundToInt(equipment.Machete.Damage(meleeDamage) * 1.25f);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Transform enemy = enemies[i];
            if ((enemy.position - player.transform.position).sqrMagnitude <= 7.5f * 7.5f && (obstacles == null || obstacles.Clear(player.transform.position, enemy.position)))
            {
                DamageWeaponEnemy(i, waveDmg, HitKind.Melee, 1, null);
            }
        }
    }

    protected internal void SpawnEnemy()
    {
        if(session.IsBossWave) return;
        var kind=NextEnemyKind;
        if(kind==EnemyKind.None) return;
        bool charger=kind==EnemyKind.Charger,stone=kind==EnemyKind.StoneArmor,bomber=kind==EnemyKind.Bomber,splitter=kind==EnemyKind.Splitter,shooter=kind==EnemyKind.Shooter;
        // Reserve the extra slot each mother needs when it becomes two offspring.
        if(ReservedEnemyCount+(splitter?2:1)>EnemyLimit) return;
        float spawnRadius=stone?.6f:splitter?SplitterRadius:EnemySize/2;
        // All enemies emerge from the visible portals; never spawn on top of the player.
        int first = Random.Range(0, spawnPoints.Count);
        Vector3 position = Vector3.zero;
        bool found = false;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            position = spawnPoints[(first + i) % spawnPoints.Count];
            if(obstacles!=null && obstacles.HitDistance(position,Vector3.forward,spawnRadius)<=0) continue;
            if ((position - player.transform.position).sqrMagnitude < 64f) continue;
            bool occupied=false;
            foreach(Transform other in enemies)
            {
                Vector3 gap=position-other.position; gap.y=0;
                float spacing=spawnRadius+EnemyRadius(other)+.1f;
                if(gap.sqrMagnitude<spacing*spacing) { occupied=true; break; }
            }
            if(occupied) continue;
            found = true; break;
        }
        if (!found) return;
        position.y = EnemySize / 2f;
        Transform enemy = CreateVisual(PrimitiveType.Cube, "Enemy", position,
            Vector3.one * EnemySize, enemyMaterial);
        enemies.Add(enemy);
        enemyHealth.Add(enemy, session.Rules.Health(kind,WaveSettings));
        waveSpawnCount++;
        GameObject model = Resources.Load<GameObject>(shooter?"Characters/EnemyShooter":splitter?"Characters/EnemySplitter":bomber?"Characters/EnemyBomber":stone?"Characters/EnemyStoneArmor":charger?"Characters/EnemyCharger":EnemyModels[Random.Range(0, EnemyModels.Length)]);
        enemy.gameObject.AddComponent<EnemyAvatar>().Initialize(model, enemyModelMaterials,stone?1.32f:splitter?1.15f:1.02f,stone);
        if(splitter) {enemy.name="Cracked Splitter"; splitters.Add(enemy);}
        if(shooter) {enemy.name="Bone Needle Shooter"; shooters.Add(enemy,new Shooter {Timer=1.5f+Random.Range(0f,.6f)});}
        if(stone) InitializeStoneArmor(enemy);
        if(bomber) {enemy.name="Hooded Bomber"; bombers.Add(enemy,new Bomber {Timer=1.5f+(waveSpawnCount%3)*.4f});}
        if(charger) { enemy.name="Charging Imp"; chargers.Add(enemy,new Charger {Timer=1.5f+(waveSpawnCount%3)*.35f}); }
    }

    protected internal void DamageEnemy(int index, int damage, HitKind kind=HitKind.Secondary) => DamageEnemyCritical(index,damage,kind,null);

    protected internal void DamageEnemyCritical(int index, int damage, HitKind kind, bool? forcedCritical)
        =>DamageWeaponEnemy(index,damage,kind,-1,forcedCritical);

    protected internal void DamageWeaponEnemy(int index,int damage,HitKind kind,int weapon,bool? forcedCritical)
    {
        if(index<0 || index>=enemies.Count) return;
        Transform enemy=enemies[index];
        bool melee=kind==HitKind.Melee;
        bool primary=kind==HitKind.Gun || melee || kind==HitKind.Flame || kind==HitKind.Ice || kind==HitKind.Magic || kind==HitKind.Poison;
        bool critical=primary && kind!=HitKind.Poison && (forcedCritical ?? Random.value<equipment.CritChance(melee));
        float multiplier=Time.time<revengeUntil ? 1+0.2f*equipment.Stacks(RunItem.RevengeCharm) : 1;
        if(IsForestBoss(enemy) && forestBossPhase==ForestBossPhase.Rest) multiplier*=.5f;
        int dealt=Mathf.Max(1,Mathf.RoundToInt(damage*multiplier*(critical?2:1)));
        if(kind==HitKind.Burn && burns.TryGetValue(enemy,out Burn burn))
        {
            // Accumulate the final fractional damage, including fuel and temporary buffs.
            float total=damage*equipment.BurnMultiplier*multiplier+burn.DamageRemainder;
            dealt=Mathf.FloorToInt(total+.00001f);
            burn.DamageRemainder=Mathf.Max(0,total-dealt);
            if(dealt<=0) return;
        }
        if(primary) OnPrimaryHit(enemy,melee?HitKind.Melee:kind);
        enemy.GetComponent<EnemyAvatar>()?.FlashHit();
        if(kind==HitKind.Gun || kind==HitKind.Melee) GameAudio.Play(AudioCue.EnemyHit);
        Metrics.Hit(weapon==1?CombatMetrics.Source.Blades:weapon==5?CombatMetrics.Source.Hammer:kind==HitKind.Poison?CombatMetrics.Source.Poison:kind==HitKind.Ice?CombatMetrics.Source.Frost:CombatMetrics.Source.Other,
            Mathf.Min(enemyHealth[enemy],dealt),dealt>=enemyHealth[enemy],
            stunned.TryGetValue(enemy,out float controlledUntil) && Time.time<controlledUntil);
        if(kind==HitKind.Poison && chilled.TryGetValue(enemy,out float slowUntil) && Time.time<slowUntil)
            Metrics.PoisonSlowDamage(Mathf.Min(enemyHealth[enemy],dealt));
        enemyHealth[enemy]-=dealt;
        AddDamageNumber(enemy.position,dealt,critical,kind,enemy.GetInstanceID());
        if(critical && Time.time>=nextCritShakeAt) { nextCritShakeAt=Time.time+.12f; viewCamera?.Shake(.045f,.12f); }
        if(melee && critical && equipment.Machete.Evolved) bloodFrenzyUntil = Time.time + 1.8f;
        if(enemyHealth[enemy]<=0)
        {
            Vector3 at=enemy.position;
            if(critical && Random.value<0.3f*equipment.Stacks(RunItem.HuntingBadge)) SpawnMaterial(at);
            if(kind!=HitKind.Secondary && burns.ContainsKey(enemy) && equipment.Stacks(RunItem.CharBomb)>0)
                pendingEffects.Enqueue(new ItemEffect { Kind=EffectKind.Explosion, Position=at, Damage=15 });
            if(kind==HitKind.Gun && (equipment.Stacks(RunItem.SplitCore)>0 || equipment.Gun.Evolved))
                pendingEffects.Enqueue(new ItemEffect { Kind=EffectKind.Fragments, Position=at, Damage=Mathf.Max(1,Mathf.CeilToInt(damage*(equipment.Gun.Evolved?0.5f:0.35f))) });
            KillEnemy(index);
        }
    }

    protected internal void KillEnemy(int index)
    {
        if(index<0 || index>=enemies.Count) return;
        Transform enemy = enemies[index];
        if(IsForestBoss(enemy)) DefeatForestBoss();
        QueueSplitOffspring(enemy);
        RemoveCharger(enemy);
        RemoveBomber(enemy);
        RemoveShooter(enemy);
        RemoveStoneArmor(enemy);
        RemoveStun(enemy);
        chilled.Remove(enemy);
        RemovePoison(enemy);
        AddFloatingScore(enemy.position);
        LeaveCharredRemains(enemy);
        SpawnMaterial(enemy.position);
        if(Random.value<0.25f*equipment.Stacks(RunItem.DangerContract)) SpawnMaterial(enemy.position);
        lanternAttacks.Remove(enemy);
        enemyHealth.Remove(enemy);
        meleeReadyAt.Remove(enemy);
        enemies.RemoveAt(index);
        if(crushed.Remove(enemy))
        {
            enemy.GetComponent<EnemyAvatar>()?.Flatten(.6f,true);
            ash.Add(enemy.gameObject); Destroy(enemy.gameObject,.6f);
        }
        else Destroy(enemy.gameObject);
        kills++;
    }

    protected internal void UpdatePickups(float dt)
    {
        CullExpiredPickups();
        Vector3 target = player.transform.position;
        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            Pickup pickup = pickups[i];
            float distance = Vector3.Distance(pickup.Transform.position, target);
            if (distance <= equipment.PickupRange) pickup.Attracted = true;
            if (pickup.Attracted)
                pickup.Transform.position = Vector3.MoveTowards(pickup.Transform.position, target, 12f * dt);
            else pickup.Transform.Rotate(0f, 70f * dt, 0f, Space.World);
            if (Vector3.Distance(pickup.Transform.position, target) > 0.45f) continue;
            CollectMaterial(1,true);
            Destroy(pickup.Transform.gameObject);
            pickups.RemoveAt(i);
        }
    }

    protected internal void ClearPickups(bool collect)
    {
        if (collect) CollectMaterial(pickups.Count,false);
        foreach (Pickup pickup in pickups) Destroy(pickup.Transform.gameObject);
        pickups.Clear();
    }

    protected internal void ApplyEquipment()
    {
        player.SpeedMultiplier = equipment.SpeedMultiplier * (Time.time < bloodFrenzyUntil ? 1.35f : 1f);
        if(machete!=null) machete.gameObject.SetActive(equipment.HasWeapon(1));
        UpdateBladeLength();
    }

    protected internal Transform CreateVisual(PrimitiveType type, string objectName, Vector3 position,
        Vector3 scale, Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = objectName;
        visual.transform.SetParent(transform);
        visual.transform.position = position;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        // The collider can be missing in stripped WebGL builds; visuals never need one.
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
        return visual.transform;
    }

    protected internal void MoveEnemies(float dt)
    {
        if(!Finite(dt) || dt<=0) return;
        Vector3 target = player.transform.position;
        Vector3 right = player.transform.right;
        Vector3 forward = player.transform.forward;
        if (obstacles != null) obstacles.UpdateRoutes(target);
        foreach (Transform enemy in enemies)
        {
            if(IsForestBoss(enemy)) continue;
            Vector3 direction = target - enemy.position;
            direction.y = 0f;
            if(stunned.TryGetValue(enemy,out float stunUntil) && Time.time<stunUntil) continue;
            EnemyAvatar avatar = enemy.GetComponent<EnemyAvatar>();
            if (avatar != null) avatar.Face(direction);
            float slow=chilled.TryGetValue(enemy,out float until) && Time.time<until?1f-FrostSlow:1f;
            if(TickBomber(enemy,avatar,target,slow,dt)) continue;
            if(TickShooter(enemy,avatar,target,slow,dt)) continue;
            if(TickCharger(enemy,avatar,target,slow,dt)) { if(health.IsDead) return; continue; }
            bool stone=stoneArmors.Contains(enemy);
            if(SplitlingEmerging(enemy)) continue;
            float moveSpeed=WaveEnemySpeed*slow*(stone?.55f:splitters.Contains(enemy)?.8f:splitlings.ContainsKey(enemy)?1.3f:1f);
            if(MoveToBaitLantern(enemy,avatar,moveSpeed,dt)) continue;
            float travel=moveSpeed*dt;
            if(stone) travel=Mathf.Min(travel,Mathf.Max(0,direction.magnitude-StoneContactDistance));
            enemy.position = obstacles != null ? obstacles.Move(enemy.position, target, travel,splitlings.ContainsKey(enemy)?SplitlingRadius:Mathf.Max(.4f,EnemyRadius(enemy)))
                : enemy.position + direction.normalized * Mathf.Min(travel, direction.magnitude);
        }
        ResolveBaitLanternBlast();
        // Births happen outside the crowd loop and do not move for time preceding their spawn.
        TickForestBoss(dt);
        if(health.IsDead) return;
        SeparateEnemies();
        if(stoneArmors.Count>0) Physics.SyncTransforms();
        foreach(Transform enemy in enemies)
        {
            if(IsForestBoss(enemy)) continue;
            if(SplitlingEmerging(enemy)) continue;
            if(ChargerCommitted(enemy)) continue;
            if(stunned.TryGetValue(enemy,out float stunUntil) && Time.time<stunUntil) continue;
            Vector3 offset = enemy.position - target;
            // Closest point on the player's compact body box to the enemy's circular footprint.
            float dx=Mathf.Max(0,Mathf.Abs(Vector3.Dot(offset,right))-playerContactHalfSize);
            float dz=Mathf.Max(0,Mathf.Abs(Vector3.Dot(offset,forward))-playerContactHalfSize);
            float radius=EnemyRadius(enemy);
            bool touching=stoneArmors.Contains(enemy)
                ? new Vector2(offset.x,offset.z).sqrMagnitude<=Mathf.Pow(StoneContactDistance+.08f,2)
                : dx*dx+dz*dz<=radius*radius+.0001f;
            if (touching)
            {
                health.TakeDamage(10);
                if (health.IsDead) return;
            }
        }
    }

    protected internal void Shoot()=>FireRifleRound();

    protected internal void MoveBullets(float dt)
    {
        if (!Finite(dt) || dt <= 0f) return;
        for(int b=bullets.Count-1;b>=0;b--)
        {
            Bullet bullet=bullets[b];
            float remainingTravel=Mathf.Min((bullet.Weapon==8?14f:bulletSpeed)*dt,bullet.RemainingRange);
            bool remove=!Finite(remainingTravel) || !Finite(bullet.RemainingRange);
            int steps=0;
            while(!remove && remainingTravel>0.0001f && steps++<12)
            {
                Vector3 origin=bullet.Transform.position;
                if (!Finite(origin) || !Finite(bullet.Direction) || bullet.Direction.sqrMagnitude < 0.000001f)
                { remove=true; break; }
                Ray ray=new Ray(origin,bullet.Direction);
                float wall=DistanceToBorder(origin,bullet.Direction);
                if(obstacles!=null) wall=Mathf.Min(wall,obstacles.HitDistance(origin,bullet.Direction,BulletRadius));
                int target=-1; float hitDistance=Mathf.Min(remainingTravel,wall);
                for(int i=0;i<enemies.Count;i++)
                {
                    if(bullet.HitTargets.Contains(enemies[i])) continue;
                    Bounds bound=EnemyHitBounds(enemies[i],BulletRadius);
                    // A ray starting inside a target hits immediately, never at a negative entry distance.
                    float d;
                    if(bound.Contains(origin)) d=0f;
                    else if(!bound.IntersectRay(ray,out d)) continue;
                    if(Finite(d) && d>=0f && d<hitDistance) { hitDistance=d; target=i; }
                }
                Vector3 destination=origin+bullet.Direction*hitDistance;
                if(!Finite(hitDistance) || hitDistance<0f || !Finite(destination)) { remove=true; break; }
                bullet.Transform.position=destination;
                bullet.RemainingRange-=hitDistance; remainingTravel-=hitDistance;
                if(target<0) { remove=wall<=hitDistance || bullet.RemainingRange<=0.001f; break; }
                Transform enemy=enemies[target]; bullet.HitTargets.Add(enemy);
                if(bullet.Weapon==8) { remove=true; break; }
                ResolveBulletHit(bullet,enemy,bullet.Transform.position);
                if(bullet.Pierce>0)
                {
                    bullet.Pierce--;
                    if(bullet.Weapon!=0 || !equipment.Gun.Evolved)
                        bullet.Damage=Mathf.Max(1,Mathf.CeilToInt(bullet.Damage*(bullet.Weapon==6?.5f:.75f)));
                }
                else if(bullet.Bounces>0)
                {
                    Transform next=FindItemTarget(bullet.Transform.position,8,bullet.HitTargets);
                    if(next==null) { remove=true; break; }
                    bullet.Bounces--; bullet.Damage=Mathf.Max(1,Mathf.CeilToInt(bullet.Damage*0.5f));
                    Vector3 direction=next.position-bullet.Transform.position; direction.y=0;
                    // Coincident ricochet targets are hit at distance zero on the next step.
                    if(direction.sqrMagnitude>=0.000001f) bullet.Direction=direction.normalized;
                }
                else { remove=true; break; }
            }
            if(remove || bullet.RemainingRange<=0.001f)
            {
                if(bullet.Weapon==8 && Finite(bullet.Transform.position)) ExplodeFireball(bullet);
                Destroy(bullet.Transform.gameObject); bullets.RemoveAt(b);
            }
            else if(bullet.Weapon==8) AnimateFireball(bullet,dt);
        }
    }

    protected internal const float EnemySpacing=EnemySize+0.1f;
    protected internal void SeparateEnemies()
    {
        // Resolve pairs symmetrically; stunned enemies remain planted while others go around them.
        for(int pass=0;pass<4;pass++)
        for(int i=0;i<enemies.Count;i++) for(int j=i+1;j<enemies.Count;j++)
        {
            Transform a=enemies[i],b=enemies[j];
            Vector3 delta=a.position-b.position; delta.y=0;
            float spacing=EnemyRadius(a)+EnemyRadius(b)+.1f;
            if(delta.sqrMagnitude>=spacing*spacing) continue;
            bool fixedA=IsForestBoss(a) || ChargerCommitted(a) || SplitlingEmerging(a) || (stunned.TryGetValue(a,out float untilA)&&Time.time<untilA);
            bool fixedB=IsForestBoss(b) || ChargerCommitted(b) || SplitlingEmerging(b) || (stunned.TryGetValue(b,out float untilB)&&Time.time<untilB);
            if(fixedA&&fixedB) continue;
            float distance=delta.magnitude;
            Vector3 direction=distance>.0001f?delta/distance:new Vector3(Mathf.Cos(i*2.4f+j),0,Mathf.Sin(i*2.4f+j));
            float shareA=fixedA?0:fixedB?1:stoneArmors.Contains(a)?(stoneArmors.Contains(b)?.5f:.2f):(stoneArmors.Contains(b)?.8f:.5f);
            float overlap=spacing-distance;
            if(!fixedA) PushEnemyApart(a,direction,overlap*shareA);
            if(!fixedB) PushEnemyApart(b,-direction,overlap*(1-shareA));
        }
    }
    protected internal void PushEnemyApart(Transform enemy,Vector3 direction,float distance)
    {
        if(IsForestBoss(enemy)) return;
        float radius=EnemyRadius(enemy);
        if(obstacles!=null) distance=Mathf.Min(distance,Mathf.Max(0,obstacles.HitDistance(enemy.position,direction,radius)-.01f));
        Vector3 position=enemy.position+direction*distance;
        position.x=Mathf.Clamp(position.x,-18+radius,18-radius);
        position.z=Mathf.Clamp(position.z,-18+radius,18-radius);
        enemy.position=position;
    }

    protected internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    protected internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

    protected internal static float DistanceToBorder(Vector3 origin, Vector3 direction)
    {
        // Stop the sphere's surface at the border's inner face, including during long frames.
        float limit = ArenaHalfSize - BulletRadius;
        if (Mathf.Abs(origin.x) >= limit || Mathf.Abs(origin.z) >= limit) return 0f;
        float distance = float.PositiveInfinity;
        if (direction.x > 0f) distance = Mathf.Min(distance, (limit - origin.x) / direction.x);
        else if (direction.x < 0f) distance = Mathf.Min(distance, (-limit - origin.x) / direction.x);
        if (direction.z > 0f) distance = Mathf.Min(distance, (limit - origin.z) / direction.z);
        else if (direction.z < 0f) distance = Mathf.Min(distance, (-limit - origin.z) / direction.z);
        return distance;
    }

    internal void TickCombat(float dt)
    {
        // Births happen outside weapon/damage loops, on the next combat tick.
        if(!Finite(dt) || dt<=0) return;
        Metrics.Advance(dt);
        UpdateSplitBirths();
        UpdateImpacts(dt);
        UpdateFloatingScores(dt);

        nextSpawn -= dt;
        if (nextSpawn <= 0f)
        {
            for (int i = 0; i < SpawnBatch && enemies.Count < EnemyLimit; i++) SpawnEnemy();
            nextSpawn = WaveSpawnInterval;
        }

        UpdateEnemyBombs(dt);
        if (health.IsDead) return;
        UpdateEnemyNeedles(dt);
        if (health.IsDead) return;
        UpdateBaitLantern(dt);
        MoveEnemies(dt);
        if (health.IsDead) return;
        if(equipment.HasWeapon(1)) UpdateMelee(dt);
        UpdateNewWeapons(dt);
        UpdateRifle(dt);
        MoveBullets(dt);
        UpdateFlamethrower(dt);
        UpdatePickups(dt);
        ProcessItemEffects();
        SetPlayerHit(health.IsHitInvulnerable && Mathf.Sin(Time.time * 35f) > 0f);
    }

    public RunState Run { get; protected set; }
    internal event System.Action PlayerDied,SettlementReady;
    private void ReportDeath()=>PlayerDied?.Invoke();
    internal void ResetArena()
    {
        Metrics.Reset();
        lootFlights.Clear(); victoryContinueRequested=false; victoryRewardPending=false;
        ClearCombatObjects(); ClearPickups(false); ResetItemState(); ApplyEquipment();
        health.ResetHealth();
        if(obstacles!=null) obstacles.Generate();
        CreateSpawnPoints();
    }
    internal void FinishWave()
    {
        bool defeatedBoss=BossDefeated;
        Vector3 bossAt=bossDeathPosition;
        var positions=new List<Vector3>(enemies.Count);
        foreach(Transform enemy in enemies) positions.Add(enemy.position);
        ClearCombatObjects();
        if(defeatedBoss)
        {
            CreateBossRubble(bossAt);
            viewCamera?.Shake(.16f,.35f); GameAudio.Play(AudioCue.Explosion);
        }
        foreach(Vector3 position in positions)
        {
            ExplodeRemainingEnemy(position); SpawnMaterial(position);
            pickups[pickups.Count-1].ScoreOnly=true;
        }
        BeginVictoryLoot(); StopFlames(); SetPlayerEnabled(false); SetPlayerHit(false);
        GameAudio.Current?.SetMusic(2); GameAudio.Play(AudioCue.WaveClear);
    }
    internal void ShowDefeat()
    {
        CancelForestBossAttacks();
        GameAudio.Current?.StopWorld();
        GameAudio.Current?.SetMusic(-1); GameAudio.Play(AudioCue.Defeat);
        player.ResetDash();
        ClearSplitters();
        ClearBombers();
        ClearShooters();
        ClearChargers();
        ClearBaitLantern(); ClearPoison(); ClearBoomerang(); ClearHammerState(); ClearFrostField(); ClearRifle(); ClearFireballs(); ClearTorchState(); SetPlayerEnabled(false); SetPlayerHit(true);
        StopFlames(); ClearImpacts(); floatingScores.Clear(); floatingDamage.Clear();
    }
    internal void SetPlayerEnabled(bool value)=>player.enabled=value;
    internal void SaveCameraPreferences() { if(viewCamera!=null) viewCamera.SavePreferences(); }
    internal void RequestVictoryContinue()=>victoryContinueRequested=true;
    internal bool VictoryReady=>lootFlights.Count==0 && victoryAge>=VictoryAnimationDuration;
    protected internal RunEquipment equipment => Run.Equipment;
    protected internal WaveShop shop => Run.Shop;
    protected internal WaveSession session => Run.Session;
    protected internal int kills { get=>Run.Kills; set=>Run.Kills=value; }
    protected internal int lootScore { get=>Run.LootScore; set=>Run.LootScore=value; }
    protected internal float survivalTime { get=>Run.SurvivalTime; set=>Run.SurvivalTime=value; }
    int ICombatView.Health=>health!=null?health.Current:0;
    Vector3 ICombatView.PlayerPosition=>player.transform.position;
    Vector2 ICombatView.MoveInput=>player != null ? player.MoveInput : Vector2.zero;
    float ICombatView.DashCooldownRemaining=>player!=null?player.DashCooldownRemaining:0;
    float ICombatView.DashReadyFlash=>player!=null?player.DashReadyFlash:0;
    bool ICombatView.IsDashing=>player!=null && player.IsDashing;
    int ICombatView.DashCharges=>player!=null?player.DashCharges:0;
    int ICombatView.MaxDashCharges=>player!=null?player.MaxCharges:PlayerController.MaxDashCharges;
    PlaygroundCamera ICombatView.Camera=>viewCamera;
    float ICombatView.VictoryAge=>victoryAge;
    bool ICombatView.VictoryContinueRequested=>victoryContinueRequested;
    IReadOnlyList<FloatingScore> ICombatView.FloatingScores=>floatingScores;
    IReadOnlyList<FloatingScore> ICombatView.FloatingDamage=>floatingDamage;
    IReadOnlyList<LootFlight> ICombatView.LootFlights=>lootFlights;
    float ICombatView.WeaponRange(int id)=>id>=3?NewRange(id):id==0?GunRange:id==1?AttackRange:FlameRange;
    int ICombatView.WeaponDamage(int id)=>id==6?FrostDamage:equipment.Weapon(id).Damage(id==0?bulletDamage:id==1?meleeDamage:id==2?flameDamage:WeaponCatalog.Damage[id]);
}

