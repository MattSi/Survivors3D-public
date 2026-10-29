using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WeaponEvolutionChecks
{
    private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    private static SurvivorPlayground game;
    private static int stage;
    private static double next, deadline;

    private static object Get(string name) => GameTestAccess.Get(game, name);
    private static void Call(string name, params object[] args) => GameTestAccess.Call(game, name, args);
    private static List<Transform> Enemies => (List<Transform>)Get("enemies");
    private static Dictionary<Transform, int> Health => (Dictionary<Transform, int>)Get("enemyHealth");
    private static RunEquipment Equipment => (RunEquipment)Get("equipment");

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception("EVOLUTION_CHECK_FAILED: " + message);
    }

    [MenuItem("Tools/Survivors/Verify Weapon Evolution")]
    public static void Run()
    {
        VerifyDataAndShop();
        if (EditorApplication.isPlaying) throw new Exception("Run this check outside Play Mode.");
        EditorSceneManager.OpenScene("Assets/Scenes/ForestCamp.unity");
        SessionState.SetBool("WeaponEvolutionChecks", true);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        VerifyDataAndShop();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("WeaponEvolutionChecks", false)) return;
            stage = 0;
            next = EditorApplication.timeSinceStartup + 1.5;
            deadline = next + 45;
            EditorApplication.update += Tick;
        };
    }

    public static void VerifyDataAndShop()
    {
        // 1. Gun Evolution Prerequisites & Catalyst Validation
        RunEquipment eq = new RunEquipment();
        eq.ChooseStarter(0);
        Check(!eq.CanEvolve(0), "Gun Lv1 cannot evolve");
        Check(!eq.IsEvolved(0), "Gun Lv1 is not evolved");

        // Give catalyst before Lv3
        eq.Collect(1000);
        eq.Buy(ItemLibrary.ShopId((int)RunItem.ArmorPiercing));
        Check(!eq.CanEvolve(0), "Gun Lv1 with AP cannot evolve");

        eq.Gun.Upgrade(); // Lv2
        Check(!eq.CanEvolve(0), "Gun Lv2 with AP cannot evolve");

        eq.Gun.Upgrade(); // Lv3
        Check(eq.Gun.Level == 3, "Gun is Lv3");
        Check(eq.CanEvolve(0), "Gun Lv3 with AP CAN evolve");

        // Test alternative catalyst: SplitCore
        RunEquipment eqSplit = new RunEquipment();
        eqSplit.ChooseStarter(0);
        eqSplit.Collect(1000);
        eqSplit.Gun.Upgrade(); eqSplit.Gun.Upgrade();
        Check(!eqSplit.CanEvolve(0), "Gun Lv3 without AP or SplitCore cannot evolve");
        eqSplit.Buy(ItemLibrary.ShopId((int)RunItem.SplitCore));
        Check(eqSplit.CanEvolve(0), "Gun Lv3 with SplitCore CAN evolve");

        // Irrelevant items do NOT allow evolution
        RunEquipment eqIrrelevant = new RunEquipment();
        eqIrrelevant.ChooseStarter(0);
        eqIrrelevant.Collect(1000);
        eqIrrelevant.Gun.Upgrade(); eqIrrelevant.Gun.Upgrade();
        eqIrrelevant.Buy(ItemLibrary.ShopId((int)RunItem.SavingsJar));
        Check(!eqIrrelevant.CanEvolve(0), "Gun Lv3 with SavingsJar cannot evolve");

        // 2. Gun Evolution RunEquipment Flow
        int gunShopId = WeaponCatalog.ShopId(0);
        Check(eq.CanEvolve(0), "eq can evolve gun");
        Check(eq.MaxLevel(gunShopId) == 3, "MaxLevel expands to 3 when evolution is eligible");
        Check(eq.Level(gunShopId) == 2, "Level is 2 before evolution purchase");
        Check(eq.Price(gunShopId) == 40, "Evolution cost is exactly 40 materials");

        int matsBefore = eq.Materials;
        Check(eq.Buy(gunShopId), "Can purchase gun evolution card from equipment");
        Check(eq.Materials == matsBefore - 40, "Materials deducted by 40");
        Check(eq.Gun.Evolved, "Gun.Evolved is true");
        Check(eq.IsEvolved(0), "IsEvolved(0) is true");
        Check(!eq.CanEvolve(0), "CanEvolve(0) is false after evolution");
        Check(eq.Level(gunShopId) == 3 && eq.MaxLevel(gunShopId) == 3, "Level equals MaxLevel after evolution");
        Check(!eq.Buy(gunShopId), "Cannot buy gun evolution card again");

        // 3. Machete Evolution Prerequisites & Catalyst Validation
        RunEquipment eqBlade = new RunEquipment();
        eqBlade.ChooseStarter(1);
        eqBlade.Collect(1000);
        Check(!eqBlade.CanEvolve(1), "Machete Lv1 cannot evolve");

        eqBlade.Machete.Upgrade(); eqBlade.Machete.Upgrade(); // Lv3
        Check(!eqBlade.CanEvolve(1), "Machete Lv3 without catalyst cannot evolve");

        eqBlade.Buy(ItemLibrary.ShopId((int)RunItem.Whetstone));
        Check(eqBlade.CanEvolve(1), "Machete Lv3 with Whetstone CAN evolve");

        RunEquipment eqPendant = new RunEquipment();
        eqPendant.ChooseStarter(1);
        eqPendant.Collect(1000);
        eqPendant.Machete.Upgrade(); eqPendant.Machete.Upgrade();
        eqPendant.Buy(ItemLibrary.ShopId((int)RunItem.BloodPendant));
        Check(eqPendant.CanEvolve(1), "Machete Lv3 with BloodPendant CAN evolve");

        // 4. Machete Evolution RunEquipment Flow
        int bladeShopId = WeaponCatalog.ShopId(1);
        Check(eqBlade.Price(bladeShopId) == 40, "Machete evolution price is 40 materials");
        Check(eqBlade.Buy(bladeShopId), "Can purchase machete evolution card");
        Check(eqBlade.Machete.Evolved, "Machete.Evolved is true");
        Check(eqBlade.IsEvolved(1), "IsEvolved(1) is true");
        Check(!eqBlade.Buy(bladeShopId), "Cannot buy machete evolution again");

        // 5. Naming & Descriptions
        Check(WeaponCatalog.EvolvedName(0).Length > 0, "Void Gatling has evolved name");
        Check(WeaponCatalog.EvolvedName(1).Length > 0, "Death's Whirlwind has evolved name");
        Check(WeaponCatalog.EvolvedDescription(0).Length > 0, "Void Gatling has evolved description");
        Check(WeaponCatalog.EvolvedDescription(1).Length > 0, "Death's Whirlwind has evolved description");

        // 6. WaveShop Generation, Locking, Refresh & Purchase Flow
        CheckWaveShopEvolutionFlow();
    }

    private static void CheckWaveShopEvolutionFlow()
    {
        // 1. Gun Evolution in WaveShop
        RunEquipment eq = new RunEquipment();
        eq.ChooseStarter(0);
        eq.Collect(1000);
        eq.Gun.Upgrade(); eq.Gun.Upgrade();
        eq.Buy(ItemLibrary.ShopId((int)RunItem.ArmorPiercing));
        Check(eq.CanEvolve(0), "Gun is ready to evolve");

        int gunShopId = WeaponCatalog.ShopId(0);
        WaveShop shop = null;
        int evoSlot = -1;
        for (int seed = 0; seed < 100 && evoSlot < 0; seed++)
        {
            var candidate = new WaveShop(eq, new System.Random(seed));
            candidate.Open(1);
            for (int slot = 0; slot < candidate.Count; slot++)
            {
                if (candidate.Item(slot) == gunShopId)
                {
                    shop = candidate;
                    evoSlot = slot;
                    break;
                }
            }
        }
        Check(evoSlot >= 0, "WaveShop rolled gun evolution card");
        Check(shop.Price(evoSlot) == 40, "WaveShop gun evolution price is 40");
        Check(!shop.Sold(evoSlot), "Slot is initially unsold");
        Check(!shop.Locked(evoSlot), "Slot is initially unlocked");

        // Test Locking
        shop.ToggleLock(evoSlot);
        Check(shop.Locked(evoSlot), "Slot is locked after ToggleLock");

        // Test Refresh retention
        int matsBeforeRefresh = eq.Materials;
        Check(shop.Refresh(), "Shop refreshed");
        Check(eq.Materials == matsBeforeRefresh - 3, "Refresh cost deducted");
        Check(shop.Locked(evoSlot), "Locked slot remains locked after refresh");
        Check(shop.Item(evoSlot) == gunShopId, "Locked item remains gun evolution card");
        Check(shop.Price(evoSlot) == 40, "Locked price remains 40");

        // Test Purchase via WaveShop
        int matsBeforeBuy = eq.Materials;
        Check(shop.Buy(evoSlot), "Purchased gun evolution through WaveShop");
        Check(eq.Materials == matsBeforeBuy - 40, "Materials deducted by 40 on purchase");
        Check(shop.Sold(evoSlot), "Slot marked as sold in WaveShop");
        Check(!shop.Locked(evoSlot), "Slot unlocked after purchase");
        Check(eq.Gun.Evolved, "Gun is evolved on equipment");
        Check(eq.IsEvolved(0), "IsEvolved(0) is true");

        // Verify subsequent shop roll never offers gun again
        for (int seed = 0; seed < 50; seed++)
        {
            var subsequent = new WaveShop(eq, new System.Random(seed));
            subsequent.Open(2);
            for (int s = 0; s < subsequent.Count; s++)
            {
                Check(subsequent.Item(s) != gunShopId, "Evolved gun never rolled again in WaveShop");
            }
        }

        // 2. Machete Evolution in WaveShop
        RunEquipment eqBlade = new RunEquipment();
        eqBlade.ChooseStarter(1);
        eqBlade.Collect(1000);
        eqBlade.Machete.Upgrade(); eqBlade.Machete.Upgrade();
        eqBlade.Buy(ItemLibrary.ShopId((int)RunItem.Whetstone));
        Check(eqBlade.CanEvolve(1), "Machete ready to evolve");

        int bladeShopId = WeaponCatalog.ShopId(1);
        WaveShop bladeShop = null;
        int bladeSlot = -1;
        for (int seed = 0; seed < 100 && bladeSlot < 0; seed++)
        {
            var candidate = new WaveShop(eqBlade, new System.Random(seed));
            candidate.Open(1);
            for (int slot = 0; slot < candidate.Count; slot++)
            {
                if (candidate.Item(slot) == bladeShopId)
                {
                    bladeShop = candidate;
                    bladeSlot = slot;
                    break;
                }
            }
        }
        Check(bladeSlot >= 0, "WaveShop rolled machete evolution card");
        Check(bladeShop.Price(bladeSlot) == 40, "WaveShop blade evolution price is 40");
        Check(bladeShop.Buy(bladeSlot), "Purchased machete evolution through WaveShop");
        Check(eqBlade.Machete.Evolved, "Machete is evolved on equipment");
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        if (EditorApplication.timeSinceStartup > deadline)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool("WeaponEvolutionChecks", false);
            EditorApplication.isPlaying = false;
            throw new Exception("EVOLUTION_CHECK_TIMEOUT at stage " + stage);
        }
        if (game == null)
        {
            game = UnityEngine.Object.FindAnyObjectByType<SurvivorPlayground>();
            if (game == null) return;
        }

        switch (stage)
        {
            case 0:
                CheckGatlingInCombat();
                stage = 1;
                next = EditorApplication.timeSinceStartup + 0.2;
                break;
            case 1:
                CheckDeathsWhirlwindInCombat();
                stage = 2;
                next = EditorApplication.timeSinceStartup + 0.2;
                break;
            case 2:
                EditorApplication.update -= Tick;
                SessionState.SetBool("WeaponEvolutionChecks", false);
                EditorApplication.isPlaying = false;
                Debug.Log("<color=green>ALL_WEAPON_EVOLUTION_CHECKS_PASSED!</color>");
                break;
        }
    }

    private static void CheckGatlingInCombat()
    {
        game.NewGame();
        game.BeginRun(0);
        Call("ClearCombatObjects");

        Equipment.Collect(1000);
        Equipment.Gun.Upgrade();
        Equipment.Gun.Upgrade();
        Equipment.Buy(ItemLibrary.ShopId((int)RunItem.ArmorPiercing));
        Equipment.Buy(WeaponCatalog.ShopId(0)); // Evolve!
        Check(Equipment.Gun.Evolved, "Gun is evolved in combat runner");

        Call("SpawnEnemy");
        Transform enemy = Enemies[Enemies.Count - 1];
        enemy.position = new Vector3(0, 0.375f, 4f);
        Health[enemy] = 200;

        Call("UpdateRifle", 0.01f);
        var bullets = (IList)Get("bullets");
        Check(bullets.Count >= 1, "Gatling fired projectile");

        object bullet = bullets[0];
        var bulletType = bullet.GetType();
        int pierce = (int)bulletType.GetField("Pierce").GetValue(bullet);
        Check(pierce == 999, "Gatling bullet has 999 piercing");

        // Test on-kill fragments (fixed P1: IEnumerable cast and direct Get("pendingEffects"))
        Health[enemy] = 1;
        Call("DamageWeaponEnemy", Enemies.IndexOf(enemy), 10, Kind("Gun"), 0, false);
        var pendingEffects = (IEnumerable)Get("pendingEffects");
        bool foundFragments = false;
        foreach (object effect in pendingEffects)
        {
            var kindField = effect.GetType().GetField("Kind");
            if (kindField != null && kindField.GetValue(effect).ToString() == "Fragments")
            {
                foundFragments = true;
                break;
            }
        }
        Check(foundFragments, "Gatling kill triggered fragmentation effect");

        // Clean state and reset rifle cooldown for penetration test
        Call("ClearCombatObjects");
        GameTestAccess.Set(game, "nextShot", 0f);

        // Player is at (0, 0, 0) facing Vector3.forward; RifleHand is at (0.6f, 0.12f, 0f).
        // Place two targets lined up exactly along RifleHand's forward firing ray (x = 0.6f).
        Call("SpawnEnemy");
        Transform target1 = Enemies[Enemies.Count - 1]; target1.position = new Vector3(0.6f, 0.375f, 2.5f); Health[target1] = 100;
        Call("SpawnEnemy");
        Transform target2 = Enemies[Enemies.Count - 1]; target2.position = new Vector3(0.6f, 0.375f, 5.5f); Health[target2] = 100;

        Call("UpdateRifle", 0.01f);
        var bulletsList = (IList)Get("bullets");
        Check(bulletsList.Count == 1, "Exactly one new bullet spawned for penetration test");
        object liveBullet = bulletsList[0];
        liveBullet.GetType().GetField("Critical").SetValue(liveBullet, (bool?)false);
        int expectedDamage = Equipment.Gun.Damage(WeaponCatalog.Damage[0]); // 6 + 2*5 = 16

        // Move bullets: 0.25s * 30 m/s = 7.5m, fully piercing target1 (2.5m) and target2 (5.5m)
        Call("MoveBullets", 0.25f);

        // Assert BOTH targets took full damage without falloff
        Check(Health[target1] == 100 - expectedDamage, $"Target 1 took full damage (expected {100 - expectedDamage}, actual {Health[target1]})");
        Check(Health[target2] == 100 - expectedDamage, $"Target 2 took full damage without falloff (expected {100 - expectedDamage}, actual {Health[target2]})");

        int remainingPierce = (int)liveBullet.GetType().GetField("Pierce").GetValue(liveBullet);
        Check(remainingPierce == 999 - 2, $"Pierce count decremented twice on actual penetration (actual: {remainingPierce})");

        // Contrast check: Unevolved gun with ArmorPiercing DOES decay by 0.75x on pierce
        Equipment.Gun.Reset();
        Equipment.Gun.Upgrade(); Equipment.Gun.Upgrade(); // Lv3 unevolved
        Check(!Equipment.Gun.Evolved, "Gun is unevolved for contrast test");
        Call("ClearCombatObjects");
        GameTestAccess.Set(game, "nextShot", 0f);

        Call("SpawnEnemy");
        Transform normTarget1 = Enemies[Enemies.Count - 1]; normTarget1.position = new Vector3(0.6f, 0.375f, 2.5f); Health[normTarget1] = 100;
        Call("SpawnEnemy");
        Transform normTarget2 = Enemies[Enemies.Count - 1]; normTarget2.position = new Vector3(0.6f, 0.375f, 5.5f); Health[normTarget2] = 100;

        Call("UpdateRifle", 0.01f);
        var normBullets = (IList)Get("bullets");
        Check(normBullets.Count == 1, "One unevolved bullet spawned");
        object normBullet = normBullets[0];
        normBullet.GetType().GetField("Critical").SetValue(normBullet, (bool?)false);

        Call("MoveBullets", 0.25f);
        int decayedDamage = Mathf.Max(1, Mathf.CeilToInt(expectedDamage * 0.75f)); // 16 * 0.75 = 12
        Check(Health[normTarget1] == 100 - expectedDamage, $"Unevolved target 1 took full 16 damage (actual {Health[normTarget1]})");
        Check(Health[normTarget2] == 100 - decayedDamage, $"Unevolved target 2 took decayed 12 damage (actual {Health[normTarget2]})");

        Call("ClearCombatObjects");
    }

    private static void CheckDeathsWhirlwindInCombat()
    {
        game.NewGame();
        game.BeginRun(1);
        Call("ClearCombatObjects");

        Equipment.Collect(1000);
        Equipment.Machete.Upgrade();
        Equipment.Machete.Upgrade();
        Equipment.Buy(ItemLibrary.ShopId((int)RunItem.Whetstone));
        Equipment.Buy(WeaponCatalog.ShopId(1)); // Evolve!
        Check(Equipment.Machete.Evolved, "Machete is evolved in combat runner");

        Call("ApplyEquipment");
        float attackRange = (float)Get("AttackRange");
        Check(Mathf.Abs(attackRange - 3.6f) < 0.01f, "Evolved machete AttackRange is 3.6m");

        var arms = (List<Transform>)Get("macheteArms");
        Check(arms.Count == 4, "Evolved machete has exactly 4 blade arms");
        for (int i = 0; i < 4; i++)
        {
            Check(arms[i].gameObject.activeSelf, $"Blade arm {i} is active");
        }

        // Test orbit speed: 300 deg/s -> in 0.5s rotates 150 deg
        Call("UpdateMelee", 0.5f);
        float angle = (float)Get("bladeOrbitAngle");
        Check(Mathf.Abs(angle - 150f) < 0.05f, "Evolved blade rotates at 300 deg/sec");

        // Test blood frenzy on crit
        float initialSpeed = ((PlayerController)Get("player")).SpeedMultiplier;
        Call("SpawnEnemy");
        Transform enemy = Enemies[Enemies.Count - 1];
        enemy.position = new Vector3(2f, 0.375f, 0f);
        Health[enemy] = 500;

        Call("DamageWeaponEnemy", Enemies.IndexOf(enemy), 10, Kind("Melee"), 1, true); // Forced crit!
        Call("ApplyEquipment");
        float boostedSpeed = ((PlayerController)Get("player")).SpeedMultiplier;
        Check(boostedSpeed > initialSpeed * 1.2f, "Melee crit triggered blood frenzy speed surge");

        Call("ClearCombatObjects");
    }

    private static object Kind(string name) => Enum.Parse(typeof(CombatController).GetNestedType("HitKind", BindingFlags.NonPublic), name);
}
