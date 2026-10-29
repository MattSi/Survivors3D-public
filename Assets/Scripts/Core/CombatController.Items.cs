using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal enum HitKind { Gun, Melee, Flame, Burn, Secondary, Ice, Magic, Poison }
    protected internal enum EffectKind { Explosion, Fragments, Spread, Lightning }
    protected internal sealed class ItemEffect { public EffectKind Kind; public Vector3 Position; public int Damage; public Transform Target; }
    protected internal readonly Queue<ItemEffect> pendingEffects=new Queue<ItemEffect>();
    protected internal float revengeUntil,lifeStealAt;
    protected internal bool firstAidReady;
    protected internal int collectedCharge,lightningCharges;

    protected internal void ResetItemState() { revengeUntil=lifeStealAt=0; firstAidReady=false; collectedCharge=lightningCharges=0; pendingEffects.Clear(); }
    protected internal void BeginItemWave()
    {
        equipment.Collect(equipment.Interest(session.Wave,session.FinalWave,session.Endless));
        health.ShieldCharges=equipment.Stacks(RunItem.EmergencyShield);
        firstAidReady=equipment.Stacks(RunItem.FirstAid)>0;
        revengeUntil=lifeStealAt=0;
    }
    protected internal void OnPlayerDamaged(int amount)
    {
        Metrics.Hurt(amount);
        GameAudio.Play(AudioCue.PlayerHurt);
        if(equipment.Stacks(RunItem.RevengeCharm)>0) revengeUntil=Time.time+5;
        if(firstAidReady && !health.IsDead && health.Current<=30) { firstAidReady=false; HealFromItem(25,true); }
    }
    protected internal void HealFromItem(int amount,bool firstAid)
    {
        int before=health.Current; health.Heal(amount);
        int restored=health.Current-before;
        Metrics.Heal(restored,firstAid);
        if(restored>0 && pickupMaterial!=null)
        {
            Vector3 at=player.transform.position; at.y=.08f;
            impacts.Add(new Impact {Ring=Ring("Recovery Pulse",at,.55f,pickupMaterial,.065f),Duration=.3f});
        }
    }
    protected internal void SpawnMaterial(Vector3 position)
    {
        Transform crystal=CreateVisual(PrimitiveType.Cube,"Material",new Vector3(position.x,0.25f,position.z),Vector3.one*0.3f,pickupMaterial);
        crystal.rotation=Quaternion.Euler(25,45,25);
        pickups.Add(new Pickup { Transform=crystal,ExpiresAt=Time.time+PickupLifetime });
    }
    protected internal void CollectMaterial(int amount,bool charge)
    {
        if(amount>0) GameAudio.Play(AudioCue.Pickup);
        equipment.Collect(amount);
        lootScore+=Mathf.Max(0,amount);
        if(!charge || equipment.Stacks(RunItem.MagneticCoil)==0) return;
        collectedCharge+=amount;
        while(collectedCharge>=15) { collectedCharge-=15; lightningCharges=Mathf.Min(3,lightningCharges+1); }
    }
    protected internal void OnPrimaryHit(Transform target,HitKind kind)
    {
        if(kind==HitKind.Melee && Time.time>=lifeStealAt && (equipment.Machete.Evolved || Random.value<0.15f*equipment.Stacks(RunItem.BloodPendant)))
        { HealFromItem(equipment.Machete.Evolved ? 2 : 1,false); lifeStealAt=Time.time+(equipment.Machete.Evolved ? 0.15f : 0.25f); }
        if(kind==HitKind.Gun && Random.value<0.25f*equipment.Stacks(RunItem.IncendiaryRounds)) Ignite(target,true);
        if(lightningCharges>0)
        {
            lightningCharges--;
            pendingEffects.Enqueue(new ItemEffect { Kind=EffectKind.Lightning, Position=target.position, Damage=12*equipment.Stacks(RunItem.MagneticCoil), Target=target });
        }
    }
    protected internal Transform FindItemTarget(Vector3 origin,float range,HashSet<Transform> excluded=null)
    {
        Transform best=null; float distance=range*range;
        foreach(Transform enemy in enemies)
        {
            if(excluded!=null && excluded.Contains(enemy)) continue;
            float d=(BossSurfacePoint(enemy,origin)-origin).sqrMagnitude;
            if(d>=distance || (obstacles!=null && !obstacles.Clear(origin,enemy.position,BulletRadius))) continue;
            best=enemy; distance=d;
        }
        return best;
    }
    protected internal void Ignite(Transform target,bool spread)
    {
        if(!enemyHealth.ContainsKey(target)) return;
        if(charred.Add(target))
        {
            target.GetComponent<Renderer>().sharedMaterial=charMaterial;
            target.GetComponent<EnemyAvatar>()?.Char(charMaterial);
        }
        if(!burns.TryGetValue(target,out Burn burn)) { burn=new Burn { NextTick=Time.time+0.5f }; burns.Add(target,burn); }
        burn.Until=Time.time+equipment.BurnDuration;
        if(spread && !burn.HasSpread && equipment.Stacks(RunItem.Fuse)>0)
        {
            burn.HasSpread=true;
            pendingEffects.Enqueue(new ItemEffect { Kind=EffectKind.Spread, Position=target.position, Target=target });
        }
    }
    protected internal void ResolveBulletHit(Bullet bullet,Transform target,Vector3 center)
    {
        int index=enemies.IndexOf(target);
        if(index<0) return;
        if(bullet.Weapon==6)
        {
            chilled[target]=Time.time+1.5f+(equipment.Weapon(6).Level-1)*.3f;
            target.GetComponent<EnemyAvatar>()?.Chill(chilled[target]);
        }
        // Stone armor resists direct rifle rounds; preserve the projectile's damage for later pierced targets.
        int damage=IsForestBoss(target) && bullet.Weapon==0 && !bullet.Secondary
            ? Mathf.Max(1,Mathf.RoundToInt(bullet.Damage*ForestBossRifleDamageMultiplier)) : bullet.Damage;
        DamageEnemyCritical(index,damage,bullet.Secondary?HitKind.Secondary:bullet.Weapon==6?HitKind.Ice:HitKind.Gun,bullet.Critical);
        if(bullet.Weapon==6)
            impacts.Add(new Impact { Ring=Ring("Frost Impact",center,.65f,NewWeaponMaterial(6),.08f),Duration=.45f });
        if(bullet.Weapon!=0) return;
        if(bullet.Secondary) return;
        if(enemyHealth.ContainsKey(target))
        {
            // Small rifle knockback must not move a charger off its already telegraphed lane.
            if(!ChargerCommitted(target) && (!stunned.TryGetValue(target,out float until) || Time.time>=until)) PushEnemyApart(target,bullet.Direction,.12f);
        }
    }

    protected internal void ProcessItemEffects()
    {
        // Process a bounded snapshot. Secondary damage cannot recursively trigger item effects.
        int count=Mathf.Min(64,pendingEffects.Count);
        for(int e=0;e<count;e++)
        {
            ItemEffect effect=pendingEffects.Dequeue();
            if(effect.Kind==EffectKind.Explosion)
            {
                for(int i=enemies.Count-1;i>=0;i--)
                    if((enemies[i].position-effect.Position).sqrMagnitude<=6.25f && (obstacles==null || obstacles.Clear(effect.Position,enemies[i].position)))
                        DamageEnemy(i,effect.Damage,HitKind.Secondary);
                impacts.Add(new Impact { Ring=Ring("Char Bomb",effect.Position,2.5f,fireMaterial,0.16f) });
            }
            else if(effect.Kind==EffectKind.Fragments)
            {
                var excluded=new HashSet<Transform>();
                int fragCount=equipment.Gun.Evolved?4:2;
                for(int i=0;i<fragCount;i++)
                {
                    Transform target=FindItemTarget(effect.Position,10,excluded);
                    if(target==null || bullets.Count>=300) break;
                    excluded.Add(target); Vector3 direction=target.position-effect.Position; direction.y=0;
                    if(direction.sqrMagnitude<0.000001f) direction=Vector3.forward;
                    bullets.Add(new Bullet { Transform=CreateVisual(PrimitiveType.Sphere,"Fragment",effect.Position,Vector3.one*(equipment.Gun.Evolved?0.26f:0.22f),bulletMaterial),
                        Direction=direction.normalized,Damage=effect.Damage,RemainingRange=10,Secondary=true });
                }
            }
            else
            {
                bool spreading=effect.Kind==EffectKind.Spread;
                var excluded=new HashSet<Transform>(); if(effect.Target!=null) excluded.Add(effect.Target);
                Vector3 origin=effect.Position;
                int budget=spreading?equipment.Stacks(RunItem.Fuse)*2:3;
                for(int i=0;i<budget;i++)
                {
                    Transform target=FindItemTarget(origin,spreading?3:5,excluded);
                    if(target==null) break;
                    excluded.Add(target); Vector3 end=target.position;
                    if(spreading) Ignite(target,false);
                    else
                    {
                        LineRenderer line=Ring("Coil Lightning",Vector3.zero,0,bulletMaterial,0.09f);
                        line.loop=false; line.useWorldSpace=true; line.positionCount=2;
                        line.SetPosition(0,origin); line.SetPosition(1,end);
                        Destroy(line.gameObject,0.12f);
                        DamageEnemy(enemies.IndexOf(target),effect.Damage,HitKind.Secondary);
                    }
                    origin=end;
                }
            }
        }
    }
}
