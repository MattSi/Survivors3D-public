public static class WeaponCatalog
{
    public const int Count=12;
    public const int StarterCount=Count;
    public static int ShopId(int weapon) => weapon==11 ? 31 : weapon<3 ? weapon+3 : weapon+19;
    public static int WeaponId(int item) => item==31 ? 11 : item>=3 && item<=5 ? item-3 : item>=22 && item<=29 ? item-19 : -1;
    public static bool Passive(int item) => ItemLibrary.IsShopItem(item);
    public static bool Melee(int weapon) => weapon==1 || weapon==3 || weapon==5 || weapon==7 || weapon==9;
    public static bool Ammo(int weapon) => weapon==0 || weapon==4;
    public static readonly string[] Names={"Burst Rifle","Orbiting Blades","Fire Breath","Spear","Crossbow","Earthbreaker","Frost Field","Torch","Fireball","Boomerang Blade","Poison Backpack","Bait Lantern"};
    public static readonly int[] Damage={6,10,3,24,12,38,20,12,12,14,3,24};
    public static readonly float[] Interval={.7f,1.25f,2f,.75f,1.2f,1.5f,2.8f,1f,1.2f,1.1f,.8f,8f};
    public static readonly float[] Range={14,2.2f,5,8f,16,3,2.5f,2.8f,12f,7f,.8f,5f};
    public static readonly string[] Description={
        "三连发步枪，每发 6 点基础伤害，连发间隔 0.1 秒，每轮间隔 0.7 秒。目标死亡后自动换目标。无默认溅射，支持弹药强化。","近身环绕双刀：10/12/14 伤害，基础范围 2.2 米，每 2 秒旋转一圈。支持近战吸血与范围强化。","扇形喷火，每次直接伤害 3/4/5，点燃并持续灼烧。等级提升范围并缩短冷却。",
        "手持长矛，蓄力后投向敌群，直线贯穿且伤害不衰减。撞墙或障碍物后停留 0.2 秒回手，再冷却。支持近战强化，不使用弹药升级。",
        "四级扇形齐射：2/3/4/5 支箭，展开 15/25/35/45 度。每支 12 点伤害，1.2 秒一轮，可多箭命中同一敌人。普通暴击，穿透由弹药道具提供。",
        "举锤砸向身前 2 米内。中心造成 38/43/48 伤害，不暴击；击杀时压扁，存活则眩晕 1 秒。外围 2.8 米冲击先击退 0.4 米，再眩晕 1 秒。",
        "每 2.8 秒召唤冰晶从天而降，0.35 秒后砸地造成 20/25/30 伤害。领域范围 2.5/2.9/3.3 米，持续 1.6/2/2.4 秒，减速 35/40/45%。后来进入的敌人仅受一次 4 点冻伤。不眩晕。",
        "近身防卫火把：12/14/16 伤害，横扫 140/160/180 度，击退 0.9/1.2/1.5 米。范围 2.8 米，每秒攻击，附加燃烧，受墙体阻挡。支持近战和燃烧强化。",
        "单发直线火球，命中、撞墙或到达射程后爆炸。基础伤害 12，爆炸半径 1.5 米，间隔 1.2 秒。支持通用暴击，不燃烧、不穿透。",
        "自动投掷回旋飞刀，贯穿敌群；飞出和返程对每个敌人各命中一次，伤害 14/19/24。返程追随当前位置，可走位改变路线。飞出距离 7/7.7/8.4 米，回手后冷却 1.1/0.99/0.88 秒。撞障碍提前返程，回程受阻时无伤害收回。支持近战强化，不使用弹药升级。",
        "自动向最近移动方向的后方洒毒，每 0.8 秒一池。每 0.5 秒造成 3/4/5 伤害，离池后中毒持续 1 秒。毒池半径 0.8/0.95/1.1 米，持续 3/3.5/4 秒；重叠不叠伤，不伤自己，不暴击，不使用近战、弹药或燃烧强化。",
        "附近有敌人时，自动在身后放置一盏诱饵灯笼。引诱半径 5/5.5/6 米，耐久 30/40/50，持续 5/5.5/6 秒，冷却 8/7/6 秒（从放置起计）。追兵每秒攻击一次，每次扣 10 耐久。被打碎或到期时爆燃：半径 2.2 米，造成 24/32/40 伤害。墙体阻断引诱和爆炸；精英与首领免疫引诱，但可被爆炸命中。不伤自己、不暴击、不附加燃烧。"
    };
    public static readonly string[] EnglishDescription={
        "Three-round bursts: 6 base damage per round, 0.1s between rounds, 0.7s between burst starts. Retargets after kills. No default splash. Supports ammo upgrades.","Close-range orbiting blades: 10/12/14 damage, 2.2m base reach, one revolution every 2s. Supports melee lifesteal and reach.","Cone of fire: 3/4/5 damage per direct tick plus lasting burns. Levels increase reach and reduce cooldown.",
        "Held spear thrown through enemy lines without damage falloff. Stops at walls or obstacles, returns after 0.2s, then cools down. Supports melee upgrades, not ammo.","Four tiers fire 2/3/4/5 arrows in a 15/25/35/45 degree fan. 12 damage per arrow, 1.2s per volley. Multiple arrows can hit one enemy. Normal crits; piercing comes from ammo items.",
        "Slams within 2m for 38/43/48 center damage, without crits. Kills flatten enemies; survivors are stunned for 1s. Outer 2.8m shockwave knocks enemies back 0.4m, then stuns for 1s.",
        "Ice falls every 2.8s, landing after 0.35s for 20/25/30 damage. Frost radius 2.5/2.9/3.3m, duration 1.6/2/2.4s, slow 35/40/45%. Late entrants take 4 damage once. No stun or ammo upgrades.",
        "Defensive sweep: 12/14/16 damage, 140/160/180 degree arc, 0.9/1.2/1.5m knockback. 2.8m reach, one attack per second with burn. Blocked by cover. Supports melee and burn upgrades.",
        "One straight fireball every 1.2s. Explodes on impact or at max range: 12 damage, 1.5m blast. General crits; no burn, piercing or ammo upgrades.",
        "Automatically throws through crowds, hitting each enemy once outbound and once on return for 14/19/24 damage. Returns to your current position: move to steer it. Reach 7/7.7/8.4m; cooldown after catch 1.1/0.99/0.88s. Cover turns it back; blocked returns recall harmlessly. Supports melee upgrades, not ammo.",
        "Automatically sprays behind your last movement direction every 0.8s. Deals 3/4/5 damage every 0.5s; poison lingers 1s after leaving. Pool radius 0.8/0.95/1.1m, duration 3/3.5/4s. Overlap does not stack. No self-damage, crits, melee, ammo or burn upgrades.",
        "Automatically places one lantern behind you when enemies are nearby. Lure radius 5/5.5/6m, health 30/40/50, lifetime 5/5.5/6s, cooldown 8/7/6s from placement. Pursuers attack once per second for 10 health. Breaks or expires in a 2.2m blast for 24/32/40 damage. Cover blocks attraction and blasts. Elites and bosses resist attraction but take blast damage. No self-damage, crits or burn."
    };
    public static string EvolvedName(int weapon) => weapon switch {
        0 => GameLanguage.Chinese ? "虚空加特林" : "Void Gatling",
        1 => GameLanguage.Chinese ? "血月剑刃" : "Death's Whirlwind",
        _ => Names[weapon]
    };
    public static string EvolvedDescription(int weapon) => weapon switch {
        0 => GameLanguage.Chinese ? "【超武】取消装填与连发间歇，以极高射速持续扫射！子弹100%直线贯穿且伤害不衰减，击杀敌人时向四周爆发4枚裂解碎片。" : "[EVOLVED] Continuous high-velocity barrage! 100% piercing rounds with no damage falloff; kills erupt into 4 fragmentation bullets.",
        1 => GameLanguage.Chinese ? "【超武】化为4把巨大血色死神镰刃，范围扩展至3.6米！每1.5秒激荡360°血月剑气扫荡7.5米范围，暴击触发嗜血狂暴加速。" : "[EVOLVED] 4 crimson scythes sweeping 3.6m! Emits 360° blood wave shockwaves every 1.5s (7.5m radius) and grants vampiric haste on crits.",
        _ => Description[weapon]
    };
}
