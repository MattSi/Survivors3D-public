using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class EnemyHealthChecks
{
    static void Check(bool condition,string message) {if(!condition) throw new Exception("ENEMY_HEALTH_CHECK: "+message);}
    public static void Verify(SurvivorPlayground game)
    {
        object original=GameTestAccess.Get(game,"waveConfiguration");
        var profile=ScriptableObject.CreateInstance<WaveConfiguration>();
        GameTestAccess.Set(game,"waveConfiguration",profile);
        var hp=(Dictionary<Transform,int>)GameTestAccess.Get(game,"enemyHealth");
        var enemies=(List<Transform>)GameTestAccess.Get(game,"enemies");
        Transform Start(int wave,EnemyTestMode mode,int weapon=5)
        {
            profile.Rules.StartWave=wave; profile.Rules.TestMode=mode;
            game.NewGame(); game.BeginRun(weapon); game.enabled=false;
            ((PlayerController)GameTestAccess.Get(game,"player")).enabled=false;
            GameTestAccess.Call(game,"ClearCombatObjects");
            GameTestAccess.Call(game,"SpawnEnemy");
            Check(enemies.Count==1,"Requested enemy spawns in configured test mode");
            enemies[0].position=new Vector3(0,.375f,2); return enemies[0];
        }
        try
        {
            var stone=Start(1,EnemyTestMode.StoneArmorOnly);
            Check(hp[stone]==120,"Wave one uses stone-owned base health");
            GameTestAccess.Call(game,"SlamHammer",new Vector3(0,.035f,2));
            Check(hp[stone]==82,"A level-one hammer removes 38 health instead of executing a stone");
            Check(((Dictionary<Transform,float>)GameTestAccess.Get(game,"stunned")).ContainsKey(stone),"Surviving center target is stunned");
            stone=Start(10,EnemyTestMode.StoneArmorOnly); Check(hp[stone]==120,"Same stone stats at later test waves");
            profile.Rules.EnemyDefinitions[2].BaseHealth=200;
            Check(game.Run.Session.Rules.BaseHealth(EnemyKind.StoneArmor)==120 && hp[stone]==120,"Editing base attributes does not mutate a running session");
            stone=Start(1,EnemyTestMode.StoneArmorOnly); Check(hp[stone]==200,"A new run picks up edited stone health");
            var normal=Start(1,EnemyTestMode.NormalOnly); Check(hp[normal]==10,"Stone edits do not affect ordinary enemies");
            profile.Rules.Waves[0].HealthMultiplier=1.5f;
            stone=Start(1,EnemyTestMode.StoneArmorOnly); Check(hp[stone]==300,"Explicit wave multiplier scales the enemy base attribute");
            profile.Rules.EnemyDefinitions[4].BaseHealth=500; profile.Rules.EnemyDefinitions[5].BaseHealth=19;
            var mother=Start(1,EnemyTestMode.SplitterOnly);
            Check(hp[mother]==750,"Mother base health is independent of children");
            GameTestAccess.Call(game,"KillEnemy",0);
            typeof(CombatController).GetMethod("UpdateSplitBirths",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            Check(enemies.Count==2 && hp[enemies[0]]==29 && hp[enemies[1]]==29,"Children use their own health definition and current multiplier");
            string json=JsonUtility.ToJson(profile.Rules);
            var restored=JsonUtility.FromJson<WaveRules>(json);
            Check(restored.BaseHealth(EnemyKind.StoneArmor)==200 && restored.Health(EnemyKind.Splitling,restored.At(1))==29,"Enemy attributes survive serialization");
            var legacy=JsonUtility.FromJson<WaveRules>("{\"Waves\":[{\"Wave\":1,\"EnemyHealth\":5,\"Duration\":9}]}");
            Check(legacy.Health(EnemyKind.StoneArmor,legacy.At(1))==120 && legacy.At(1).Duration==9,"Legacy wave health does not override enemy defaults or discard pacing");
            profile.Rules=new WaveRules();
            typeof(WeaponExpansionChecks).GetField("game",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,game);
            typeof(WeaponExpansionChecks).GetMethod("CheckHammer",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            Check(WeaponUpgradePreview.Describe(5,game.Run.Equipment,false).Contains("38 → 43"),"Hammer upgrade preview shows its actual damage");
            var shop=new WaveShop(game.Run.Equipment);
            shop.Open(5);
            Check((bool)typeof(WaveShop).GetMethod("IsEligible",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shop,new object[] {6+(int)RunItem.RevengeCharm}),"Fixed hammer damage permits revenge buffs in the shop");
            Debug.Log("ENEMY_HEALTH_CHECKS_PASSED: independent stats, early stone hammer survival, explicit multipliers, child health, snapshots, serialization, legacy data and hammer regression.");
        }
        finally {GameTestAccess.Set(game,"waveConfiguration",original); UnityEngine.Object.Destroy(profile);}
    }
}
