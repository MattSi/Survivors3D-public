using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    private float lanternCooldown,lanternLife;
    private int lanternHealth;
    private bool lanternBlastPending;
    private Vector3 lanternBlastPosition;
    private Material lanternBlastMaterial;
    private const float LanternBlastRadius=2.2f;
    private Transform lanternBottle,lanternHealthBar;
    private readonly Dictionary<Transform,float> lanternAttacks=new Dictionary<Transform,float>();
    private int LanternTier=>equipment.Weapon(11).Level-1;
    private float LanternRadius=>5+LanternTier*.5f;
    private bool LanternActive=>lanternHealth>0 && lanternLife>0 && newWeaponVisuals[11]!=null;

    private bool LanternEligible(Transform enemy)
    {
        return enemy!=null && enemyHealth.ContainsKey(enemy) && !IsForestBoss(enemy)
            && !stoneArmors.Contains(enemy) && !chargers.ContainsKey(enemy)
            && !shooters.ContainsKey(enemy) && !bombers.ContainsKey(enemy)
            && !splitters.Contains(enemy) && !splitlings.ContainsKey(enemy);
    }
    private bool LanternCanAttract(Transform enemy,Vector3 spot)
    {
        if(!LanternEligible(enemy)) return false;
        Vector3 delta=enemy.position-spot; delta.y=0;
        return delta.sqrMagnitude<=LanternRadius*LanternRadius
            && (obstacles==null || obstacles.Clear(enemy.position,spot,Mathf.Max(.4f,EnemyRadius(enemy))));
    }
    private void UpdateBaitLantern(float dt)
    {
        if(!Finite(dt) || dt<=0) return;
        if(!equipment.HasWeapon(11)) {ClearBaitLantern(); return;}
        lanternCooldown=Mathf.Max(0,lanternCooldown-dt);
        if(LanternActive)
        {
            lanternLife-=dt;
            if(lanternLife<=0) {BreakBaitLantern(); return;}
            lanternBottle.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*3)*6);
            lanternHealthBar.localScale=new Vector3((float)lanternHealth/(30+LanternTier*10),1,1);
            // Pulse the bottle in its last second so expiry is readable without text.
            lanternBottle.localScale=Vector3.one*(lanternLife<1?1+Mathf.Sin(Time.time*18)*.08f:1);
            return;
        }
        if(lanternCooldown>0) return;
        Vector3 origin=player.transform.position; origin.y=.05f;
        Vector3 behind=-player.LastTravelDirection; behind.y=0;
        if(behind.sqrMagnitude<.001f) behind=Vector3.back;
        behind.Normalize();
        // A full footprint and clear drop path prevent planting through cover or borders.
        for(float distance=1.5f;distance>=.9f;distance-=.3f)
        {
            Vector3 spot=origin+behind*distance;
            if(Mathf.Abs(spot.x)>ArenaHalfSize-.7f || Mathf.Abs(spot.z)>ArenaHalfSize-.7f) continue;
            if(obstacles!=null && !obstacles.Clear(origin,spot,.65f)) continue;
            bool nearby=false;
            foreach(var enemy in enemies)
                if(enemy!=null && BossSurfaceDistance(enemy,spot)<=LanternRadius
                    && (obstacles==null || obstacles.Clear(spot,enemy.position))) {nearby=true; break;}
            if(!nearby) continue;
            Transform visual=NewWeaponVisual(11);
            visual.SetPositionAndRotation(spot,Quaternion.LookRotation(-behind));
            visual.gameObject.SetActive(true);
            lanternHealth=30+LanternTier*10; lanternLife=5+LanternTier*.5f;
            lanternCooldown=8-LanternTier; lanternAttacks.Clear();
            lanternHealthBar.localScale=Vector3.one; lanternBottle.localScale=Vector3.one;
            return;
        }
        lanternCooldown=.25f;
    }
    private bool MoveToBaitLantern(Transform enemy,EnemyAvatar avatar,float speed,float dt)
    {
        if(!LanternActive || !LanternCanAttract(enemy,newWeaponVisuals[11].position)) return false;
        Vector3 destination=newWeaponVisuals[11].position;
        Vector3 delta=destination-enemy.position; delta.y=0;
        if(avatar!=null) avatar.Face(delta);
        float reach=EnemyRadius(enemy)+.45f;
        float travel=Mathf.Min(speed*dt,Mathf.Max(0,delta.magnitude-reach));
        enemy.position+=delta.normalized*travel;
        if(delta.magnitude-travel<=reach+.08f
            && (!lanternAttacks.TryGetValue(enemy,out float readyAt) || Time.time>=readyAt))
        {
            lanternAttacks[enemy]=Time.time+1;
            lanternHealth=Mathf.Max(0,lanternHealth-10);
            lanternHealthBar.localScale=new Vector3((float)lanternHealth/(30+LanternTier*10),1,1);
            if(lanternHealth==0) BreakBaitLantern();
        }
        return true;
    }
    private void EndBaitLantern()
    {
        lanternHealth=0; lanternLife=0; lanternAttacks.Clear();
        if(newWeaponVisuals[11]!=null) newWeaponVisuals[11].gameObject.SetActive(false);
    }
    private void BreakBaitLantern()
    {
        if(newWeaponVisuals[11]==null || !newWeaponVisuals[11].gameObject.activeSelf) return;
        lanternBlastPosition=newWeaponVisuals[11].position;
        lanternBlastPending=true;
        EndBaitLantern();
    }
    // Resolve outside enemy iteration: explosion kills can remove enemies and spawn split offspring.
    private void ResolveBaitLanternBlast()
    {
        if(!lanternBlastPending) return;
        lanternBlastPending=false;
        Vector3 center=lanternBlastPosition;
        GameAudio.Play(AudioCue.Explosion);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            if(BossSurfaceDistance(enemies[i],center)>LanternBlastRadius
                || (obstacles!=null && !obstacles.Clear(center,enemies[i].position))) continue;
            DamageWeaponEnemy(i,equipment.Weapon(11).Damage(WeaponCatalog.Damage[11]),HitKind.Magic,11,false);
        }
        var impact=new Impact {Ring=Ring("Bait Lantern Burst",center+Vector3.up*.05f,LanternBlastRadius,lanternBlastMaterial,.1f),
            Duration=.4f,FragmentSize=.18f,Fragments=new Transform[10],Velocities=new Vector3[10]};
        for(int i=0;i<10;i++)
        {
            float angle=i*Mathf.PI*2/10;
            Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            impact.Fragments[i]=CreateVisual(PrimitiveType.Cube,"Bait Glass Shard",center+Vector3.up*.65f,Vector3.one*.18f,lanternBlastMaterial);
            impact.Velocities[i]=radial*4+Vector3.up*2;
        }
        impacts.Add(impact);
    }
    private void ClearBaitLantern() {EndBaitLantern(); lanternCooldown=0; lanternBlastPending=false;}

    private void BuildBaitLantern(Transform root)
    {
        var wood=HammerMaterial("Lantern Timber",new Color(.43f,.23f,.09f));
        var iron=HammerMaterial("Lantern Iron",new Color(.12f,.16f,.15f));
        var brass=HammerMaterial("Lantern Brass",new Color(.69f,.48f,.16f));
        var glow=HammerMaterial("Lantern Green Bait",new Color(.63f,.95f,.16f),true);
        lanternBlastMaterial=glow;
        foreach(int sign in new[]{-1,1})
        {
            HammerPart(root,"Broad Foot",new Vector3(sign*.4f,.1f,0),new Vector3(.28f,.2f,.85f),.04f,wood);
            HammerPart(root,"Timber Post",new Vector3(sign*.4f,.67f,0),new Vector3(.17f,1.2f,.18f),.025f,wood);
            foreach(float y in new[]{.3f,1.12f})
            {
                HammerPart(root,"Iron Wrap",new Vector3(sign*.4f,y,0),new Vector3(.2f,.13f,.21f),.02f,iron);
                HammerPart(root,"Brass Rivet",new Vector3(sign*.4f,y,-.115f),Vector3.one*.06f,.02f,brass);
            }
        }
        HammerPart(root,"Cross Beam",new Vector3(0,1.3f,0),new Vector3(1.12f,.2f,.24f),.04f,wood);
        HammerPart(root,"Hanging Hook",new Vector3(0,1.1f,0),new Vector3(.07f,.25f,.07f),.01f,iron);
        lanternBottle=new GameObject("Swinging Bait Bottle").transform;
        lanternBottle.SetParent(root,false); lanternBottle.localPosition=new Vector3(0,1.02f,0);
        HammerPart(lanternBottle,"Faceted Bait Glass",new Vector3(0,-.28f,0),new Vector3(.46f,.5f,.4f),.095f,glow);
        HammerPart(lanternBottle,"Bottle Neck",new Vector3(0,-.035f,0),new Vector3(.21f,.13f,.21f),.03f,brass);
        foreach(float y in new[]{-.13f,-.44f}) HammerPart(lanternBottle,"Bottle Band",new Vector3(0,y,0),new Vector3(.47f,.055f,.41f),.045f,iron);
        HammerPart(root,"Health Back",new Vector3(0,1.55f,0),new Vector3(.85f,.08f,.09f),.01f,iron);
        lanternHealthBar=new GameObject("Lantern Health").transform; lanternHealthBar.SetParent(root,false);
        lanternHealthBar.localPosition=new Vector3(0,1.55f,0);
        HammerPart(lanternHealthBar,"Health Fill",Vector3.zero,new Vector3(.8f,.09f,.1f),.01f,glow);
    }
}
