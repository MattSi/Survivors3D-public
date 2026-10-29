using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal sealed class FloatingScore
    {
        public Vector3 Position;
        public float Age;
        public float Drift;
        public string Text="+10";
        public bool Critical, Damage, Burn, Poison, Ice;
        public int TotalDamage;
        public int TargetId;
    }
    protected internal readonly List<FloatingScore> floatingScores = new List<FloatingScore>();
    protected internal readonly List<FloatingScore> floatingDamage=new List<FloatingScore>();

    protected internal void AddDamageNumber(Vector3 position,int damage,bool critical,HitKind kind,int targetId=0)
    {
        bool burn=kind==HitKind.Burn || kind==HitKind.Flame;
        bool poison=kind==HitKind.Poison;
        bool ice=kind==HitKind.Ice;
        Vector3 spawnPos=position+Vector3.up*1.15f;

        if((burn || poison) && !critical && targetId!=0)
        {
            int searchStart=Mathf.Max(0,floatingDamage.Count-16);
            for(int i=floatingDamage.Count-1;i>=searchStart;i--)
            {
                var existing=floatingDamage[i];
                if(existing.Damage && !existing.Critical && existing.TargetId==targetId
                    && ((burn && existing.Burn) || (poison && existing.Poison))
                    && existing.Age<0.28f)
                {
                    existing.TotalDamage+=damage;
                    existing.Text="-"+existing.TotalDamage;
                    existing.Position=spawnPos;
                    existing.Age=0f;
                    return;
                }
            }
        }

        if(floatingDamage.Count>=64) floatingDamage.RemoveAt(0);
        string text=critical?"-"+damage+"!":"-"+damage;
        float drift=(floatingDamage.Count%7-3)*11f+Random.Range(-4f,4f);
        floatingDamage.Add(new FloatingScore {
            Position=spawnPos,
            Text=text,
            Damage=true,
            Critical=critical,
            Burn=burn,
            Poison=poison,
            Ice=ice,
            Drift=drift,
            TotalDamage=damage,
            TargetId=targetId
        });
    }

    protected internal void AddDamageNumber(Vector3 position,int damage,bool critical,bool burn)
        => AddDamageNumber(position,damage,critical,burn?HitKind.Burn:HitKind.Gun);

    protected internal const float ScoreLifetime = 0.9f;
    protected internal const float DamageNumberLifetime = 0.65f;

    protected internal void AddFloatingScore(Vector3 position)
    {
        // Keep text above the head and stagger simultaneous nearby kills horizontally.
        floatingScores.Add(new FloatingScore { Position=position+Vector3.up*1.0f,
            Drift=Random.Range(-16f,16f) });
    }

    protected internal void UpdateFloatingScores(float dt)
    {
        for(int i=floatingDamage.Count-1;i>=0;i--)
        {
            floatingDamage[i].Age+=dt;
            if(floatingDamage[i].Age>=DamageNumberLifetime) floatingDamage.RemoveAt(i);
        }
        for(int i=floatingScores.Count-1;i>=0;i--)
        {
            floatingScores[i].Age+=dt;
            if(floatingScores[i].Age>=ScoreLifetime) floatingScores.RemoveAt(i);
        }
    }

}
