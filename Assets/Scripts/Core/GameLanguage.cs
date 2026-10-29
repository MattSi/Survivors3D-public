using System.Collections.Generic;
using UnityEngine;

public static class GameLanguage
{
    public const string PreferenceKey="Survivors.Language";
    public static bool Chinese { get { try { return PlayerPrefs.GetInt(PreferenceKey,0)==0; } catch { return true; } } }
    public static void Toggle() { try { PlayerPrefs.SetInt(PreferenceKey,Chinese?1:0); PlayerPrefs.Save(); } catch {} }
    private static Font font;
    public static Font Font => font!=null?font:font=Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
    private static readonly Dictionary<string,string> translations=new Dictionary<string,string>
    {
        {"AWAKENING","苏醒"},{"GROUND SLAM","重砸预警"},{"CHARGE","冲锋"},{"COMBO CHARGE","连招·冲锋"},{"COMBO SLAM","连招·重砸"},{"BONE FAN","扇形齐射"},{"PREDICTIVE VOLLEY","预判齐射"},{"SUMMONING","召唤小兵"},{"RECOVERING","攻击间隙"},{"ENRAGED","狂怒觉醒"},
        {"RETREATING","退向场边"},{"RESTING · ARMOR 50%","休息·减伤50%·兵潮"},{"RETURN CHARGE","回场冲锋"},
        {"FOREST STONE KING DEFEATED","林地石王已击败"},
        {"Burst Rifle","三连发步枪"},{"Spear","长矛"},{"Crossbow","十字弓"},{"Earthbreaker","裂地重锤"},{"Frost Field","寒霜领域"},{"Icicle","寒霜领域"},{"Torch","火把"},{"Fireball","火球"},{"Boomerang Blade","回旋飞刀"},{"Poison Backpack","毒液背罐"},{"Bait Lantern","诱饵灯笼"},
        {"Armor-Piercing Rounds","穿甲弹"},{"Ricochet Chip","弹射芯片"},{"Incendiary Rounds","燃烧弹"},{"Split Core","分裂核心"},
        {"Fuse","引火索"},{"Concentrated Fuel","浓缩燃油"},{"Char Bomb","焦化炸弹"},{"Whetstone","磨刀石"},{"Ricochet","弹射芯片"},
        {"Blood Pendant","嗜血吊坠"},{"Revenge Charm","复仇护符"},{"Emergency Shield","应急护盾"},{"First Aid Kit","急救包"},
        {"Savings Jar","储蓄罐"},{"Hunting Badge","狩猎徽章"},{"Danger Contract","危险契约"},{"Magnetic Coil","磁力线圈"},
        {"Slow-release Valve","缓释阀"},
        {"Poison pools last 3s longer, but spray every 1.0s instead of 0.8s. Damage and overlap rules stay the same.","毒池持续时间增加 3 秒，但喷洒间隔由 0.8 秒延长至 1.0 秒。毒伤不变，重叠不叠伤。"},
        {"Poison Backpack / Frost Field","毒液背罐 / 寒霜领域"},
        {"+1 pierce. Damage drops 25% after each pierced target.","每层增加 1 次穿透。每穿过一个目标，伤害降低 25%。"},
        {"+1 bounce to a nearby target. Each bounce retains 50% damage.","每层增加 1 次向附近目标的弹射。每次弹射保留 50% 伤害。"},
        {"+25% chance for a direct bullet hit to ignite.","每层增加 25% 子弹直接命中时点燃敌人的概率。"},
        {"Direct bullet kills launch 2 fragments at 35% damage. Fragments cannot split or trigger items.","子弹直接击杀时发射 2 枚碎片，造成 35% 伤害。碎片不会再次分裂或触发道具。"},
        {"Fire spreads to 2 nearby targets per stack. Spread flames cannot spread again.","每层使火焰额外传播至 2 个附近目标。传播产生的火焰不会再次传播。"},
        {"+40% burn damage, +1s burn duration; +15% breath cooldown.","每层增加 40% 灼烧伤害和 1 秒灼烧时间；喷火冷却时间增加 15%。"},
        {"Burning enemies explode on death: 15 damage in 2.5m. Item explosions cannot trigger more item effects.","燃烧中的敌人死亡时爆炸，对 2.5 米内目标造成 15 点伤害。道具爆炸不会继续触发道具效果。"},
        {"+10% critical chance; melee gets another +10%. Critical hits deal double damage.","每层增加 10% 暴击率，近战额外增加 10%。暴击造成双倍伤害。"},
        {"Melee hits have +15% chance to heal 1 HP. Shared 0.25s cooldown.","每层增加 15% 近战命中时恢复 1 点生命的概率。共享 0.25 秒冷却。"},
        {"Taking damage grants +20% damage for 5s. Hits refresh duration, not strength.","受伤后每层增加 20% 伤害，持续 5 秒。再次受伤只刷新持续时间，不叠加增幅。"},
        {"Block the first hit of each wave.","抵挡每一波受到的第一次攻击。"},
        {"Once per wave, heal 25 HP after surviving a hit at 30 HP or lower.","每波一次：受到非致命攻击后，若生命不高于 30 点，恢复 25 点生命。"},
        {"From wave 2, gain 20% of saved materials, capped at 40. No interest on the final campaign wave; continues in endless mode.","第 2 波起，获得持有材料的 20% 作为利息，最多 40。普通模式最后一波不产生利息；无尽模式持续生效。"},
        {"Critical kills have +30% chance to drop one extra material.","每层增加 30% 暴击击杀时额外掉落 1 个材料的概率。"},
        {"+20% spawn rate and enemy cap; +25% chance of extra material per kill.","每层增加 20% 敌人生成速度和数量上限；击杀时额外掉落材料的概率增加 25%。"},
        {"+1.5m pickup range. Every 15 collected materials charges a 12-damage lightning chain for the next direct hit (3 targets).","每层增加 1.5 米吸取范围。拾取 15 个材料可充能一次，下次直接命中释放闪电链，攻击 3 个目标，每层造成 12 点伤害。"},
        {"CHOOSE YOUR STARTING WEAPON","选择起始武器"},{"20 waves. One starting weapon. Find the rest in the shop.","挑战 20 波。选择一件起始武器，其余武器可在商店购买。"},
        {"Fast automatic shots with a small splash.\n\nBuild toward piercing, ricochets and fragments.","自动快速射击，附带小范围溅射。\n\n可搭配穿透、弹射和分裂效果。"},
        {"Two orbiting blades that hit nearby enemies.\n\nBuild toward critical hits and life steal.","两把旋转刀攻击周围的敌人。\n\n可搭配暴击和近战吸血。"},
        {"Short bursts of red fire that leave enemies burning.\n\nBuild toward spreading fire and death explosions.","喷出短距离红色火焰，持续灼烧敌人。\n\n可搭配火焰传播和死亡爆炸。"},
        {"CHOOSE / START","选择并开始"},{"BROWSE 16 ITEMS [TAB / VIEW]","浏览 16 个道具 [Tab / View]"},
        {"ITEM LIBRARY / 16 ITEMS","道具图鉴 / 16 个道具"},{"BACK [TAB / B]","返回 [Tab / B]"},
        {"PAIRS WITH","推荐搭配"},{"Rapid Magazine","快速弹匣"},{"Blade Reach Mod","刀刃范围升级"},{"Running Boots","疾跑靴"},
        {"Gun","枪械"},{"Orbiting Blades","旋转双刀"},{"Fire Breath","喷火"},
        {"Equip this weapon in its own slot. It attacks automatically alongside your other weapons.","装备到独立武器槽，与已有武器一起自动攻击。"},
        {"UPGRADE YOUR LOADOUT","升级你的装备"},{"Spend materials. Build your next wave.","使用材料购买装备，为下一波做好准备。"},
        {"Your final loadout. New run starts with fresh equipment.","本局最终装备。重新开始将清空装备与材料。"},
        {"NO MORE UPGRADES","暂无可用升级"},{"Purchased\n\nYour stats have been updated.","购买成功\n\n装备与属性已更新。"},
        {"Maximum upgrade reached.","已达到升级上限。"},{"RUN COMPLETE","挑战完成"},{"PURCHASED","已购买"},{"MAX LEVEL","已满级"},
        {"NEW WEAPON","新武器"},{"LOCKED / UNLOCK","已锁定 / 解锁"},{"LOCK [L / X]","锁定 [L / X]"},
        {"ALL UPGRADES MAXED\nReady for the next wave.","所有升级均已满级\n准备进入下一波。"},{"NEW RUN  >","重新开始  >"},
        {"ARROWS / STICK: SELECT   ENTER / A: BUY   L / X: LOCK   TAB / VIEW: CODEX","方向键 / 摇杆：选择   Enter / A：购买   L / X：锁定   Tab / View：图鉴"},
        {"ITEM LIBRARY [TAB / VIEW]","道具图鉴 [Tab / View]"},{"YOUR STATS","角色属性"},{"LOADOUT   /   3 SLOTS","武器装备 / 3 个槽位"},
        {"Health","生命"},{"Move speed","移动速度"},{"Damage","伤害"},{"Attacks / sec","每秒攻击"},{"Range","范围"},{"Hit cooldown","命中冷却"},{"Damage / tick","每次伤害"},
        {"VICTORY!","胜利！"},{"Opening shop...","正在进入商店……"},{"YOU DIED","你倒下了"},
        {"Take a break. Continue when ready.","休整一下，准备好后继续。"},{"ENTER / R / GAMEPAD A / CROSS","Enter / R / 手柄 A / ×：重新开始"},
        {"PLAY AGAIN  [ENTER / R / A / CROSS]","重新开始 [Enter / R / A / ×]"},{"ITEM CODEX [TAB]","道具图鉴 [Tab]"},
        {"CLOSE CAMERA","关闭镜头设置"},{"CAMERA","镜头设置"},{"RESET CAMERA","重置镜头"},
        {"VIEW: THIRD PERSON / SWITCH TO TOP DOWN","当前：第三人称 / 切换俯视"},{"VIEW: TOP DOWN / SWITCH TO THIRD PERSON","当前：俯视 / 切换第三人称"},
        {"STICK / OFFLINE","摇杆 / 未连接"},{"FIRING","喷火中"},{"COOLING","冷却中"},{"READY","就绪"}
    };
    // Variable UI values retain their numbers; longer phrases are replaced first.
    private static readonly string[,] phrases={
        {"SHOP FROM WAVE ","开始出售波次 "},{"MAX STACKS ","叠加上限 "},{"BASE PRICE ","基础价格 "},{"OWNED ","已拥有 "},
        {"Upgrades equipped blades.","升级已装备的双刀。"},{"Gun fire rate","枪械射速"},{"+20% of base rate","基础射速 +20%"},{"+15% of base melee range","基础近战范围 +15%"},
        {"Movement speed","移动速度"},{"+10% of base speed","基础移速 +10%"},{"+10% base range","基础范围 +10%"},{"-10% base attack interval","基础攻击间隔 -10%"},{"Bonus:","加成："},
        {"NOT EQUIPPED","未装备"},{"NEXT WAVE ","下一波 "},{"START WAVE ","开始波次 "},{"WAVES CLEARED","波全部通关"},{"CLEARED","已完成"},{"COMPLETE / REST","完成 / 休整"},{"COMPLETE","完成"},
        {"MATERIALS","材料"},{"REFRESH","刷新"},{"NEED ","需要 "},{"BUY ","购买 "},{"STACK ","层数 "},{"TIER ","等级 "},{"MOD ","升级 "},
        {"GUN","枪械"},{"BLADES","双刀"},{"BREATH","喷火"},{"WAVE ","波次 "},{"ALL ","全部 "},{"SCORE","分数"},{"KILLS","击杀"},{"kills","击杀"},{"HP","生命"},
        {"STICK ","摇杆 "},{"TILT ","俯角 "},{"DIST ","距离 "},{"s survived","秒存活"},{"VICTORY","胜利"},{"Damage","伤害"}
    };
    public static string Text(string text)
    {
        if(!Chinese || string.IsNullOrEmpty(text)) return text;
        if(translations.TryGetValue(text,out string result)) return result;
        for(int i=0;i<phrases.GetLength(0);i++) text=text.Replace(phrases[i,0],phrases[i,1]);
        // Composed synergy strings contain two independently translated item names.
        foreach(var pair in translations) if(text.Contains(pair.Key)) text=text.Replace(pair.Key,pair.Value);
        return text;
    }
}
