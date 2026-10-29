using System.Collections.Generic;
using System.Globalization;

/// <summary>Level comparison before item modifiers; does not mutate the equipped weapon.</summary>
public static class WeaponUpgradePreview
{
    public static string Describe(int id,RunEquipment equipment,bool chinese)
    {
        WeaponSlot current=equipment.Weapon(id);
        if(current.Level>=current.LevelLimit) return chinese?"已满级":"Maximum level";
        var next=new WeaponSlot(current.Type);
        while(next.Level<=current.Level) next.Upgrade();
        int level=current.Level, tier=level-1;
        var lines=new List<string> {chinese?"升级基础数值（道具加成另行叠加）：":"Base upgrade values (item bonuses apply separately):"};
        string Number(float value)=>value.ToString("0.###",CultureInfo.InvariantCulture);
        void Stat(string cn,string en,float before,float after,string unit="")
        {
            if(System.Math.Abs(before-after)<.00001f) return;
            lines.Add((chinese?cn:en)+": "+Number(before)+unit+" → "+Number(after)+unit);
        }
        Stat(id==2?"每次直接伤害":"伤害",id==2?"Direct tick damage":"Damage",current.Damage(WeaponCatalog.Damage[id]),next.Damage(WeaponCatalog.Damage[id]));
        if(id==11)
        {
            Stat("耐久","Health",30+tier*10,30+level*10);
            Stat("引诱半径","Lure radius",5+tier*.5f,5+level*.5f,"m");
            Stat("持续时间","Lifetime",5+tier*.5f,5+level*.5f,"s");
            Stat("放置冷却","Cooldown",8-tier,8-level,"s");
            lines.Add(chinese?"自动在身后放置，最多一盏；破碎或到期爆燃，半径 2.2 米。不伤自己、不暴击；精英与首领仅免疫引诱。":"One automatic lantern behind you. Break/expiry causes a 2.2m blast. No self-damage or crits; elites and bosses resist attraction only.");
        }
        else if(id==10)
        {
            Stat("毒池半径","Pool radius",current.Range(.8f),next.Range(.8f),"m");
            Stat("毒池持续","Pool duration",3+tier*.5f,3+level*.5f,"s");
            lines.Add(chinese?"喷洒间隔 0.8 秒；毒伤间隔 0.5 秒；离池中毒 1 秒。不暴击，重叠不叠伤。":"Spray every 0.8s; poison ticks every 0.5s; lingers 1s. No crits or overlap stacking.");
            if(equipment.Stacks(RunItem.SlowReleaseValve)>0)
                lines.Add(chinese?$"缓释阀生效：喷洒间隔 1.0 秒；实际毒池持续 {Number(equipment.PoisonPoolDuration)} → {Number(equipment.PoisonPoolDuration+.5f)} 秒。":$"Valve active: spray every 1.0s; effective pool duration {Number(equipment.PoisonPoolDuration)} → {Number(equipment.PoisonPoolDuration+.5f)}s.");
        }
        else if(id==6)
        {
            Stat("领域半径","Field radius",2.5f+tier*.4f,2.5f+level*.4f,"m");
            Stat("持续时间","Duration",1.6f+tier*.4f,1.6f+level*.4f,"s");
            Stat("减速","Slow",35+tier*5,35+level*5,"%");
            lines.Add(chinese?"冷却保持 2.8 秒；后来进入伤害保持 4。":"Cooldown stays 2.8s; late-entry damage stays 4.");
        }
        else
        {
            if(id==5)
            {
                Stat("冲击半径","Shock radius",2.8f*(1+tier*.1f),2.8f*(1+level*.1f),"m");
                lines.Add(chinese?"中心固定伤害，不暴击；伤害半径 1.5 米，落点最远 2 米。存活敌人眩晕 1 秒。":"Fixed center damage; cannot crit. Damage radius 1.5m, slam reach 2m. Survivors are stunned for 1s.");
            }
            if(id==7)
            {
                Stat("横扫角度","Sweep angle",140+tier*20,140+level*20,"°");
                Stat("击退距离","Knockback",.9f+tier*.3f,.9f+level*.3f,"m");
                lines.Add(chinese?"范围保持 2.8 米；间隔保持 1 秒。":"Reach stays 2.8m; interval stays 1s.");
            }
            else
            {
                string cn=id==1?"攻击半径":id==3 || id==5?"选敌距离":"射程";
                string en=id==1?"Attack radius":id==3 || id==5?"Targeting range":"Reach";
                Stat(cn,en,current.Range(WeaponCatalog.Range[id]),next.Range(WeaponCatalog.Range[id]),"m");
            }
            Stat(id==1?"同敌命中冷却":id==3 || id==9?"回手后冷却":"攻击间隔",id==1?"Same-target cooldown":id==3 || id==9?"Cooldown after return":"Attack interval",current.Interval(WeaponCatalog.Interval[id]),next.Interval(WeaponCatalog.Interval[id]),"s");
            if(id==4)
            {
                Stat("箭数","Arrow count",level+1,level+2);
                Stat("展开角度","Fan angle",15+tier*10,15+level*10,"°");
                lines.Add(chinese?"每箭伤害保持 12；间隔保持 1.2 秒。":"Damage per arrow stays 12; interval stays 1.2s.");
            }
            if(id==8) Stat("爆炸半径","Blast radius",current.Range(1.5f),next.Range(1.5f),"m");
            if(id==1) lines.Add(chinese?"旋转保持每 2 秒一圈。":"Orbit stays one revolution per 2s.");
        }
        return string.Join("\n",lines);
    }
}
