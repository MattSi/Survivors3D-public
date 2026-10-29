using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal sealed class PoisonPool
    {
        public Transform Root,Surface,Drop;
        public Transform[] Bubbles;
        public Mesh Mesh;
        public Vector3 Center,Origin;
        public float Age,Duration,Radius;
        public int Damage;
        public bool Active;
    }
    protected internal sealed class PoisonStatus
    {
        public float Remaining=1,NextTick=.5f,Age;
        public int Damage;
        public Transform Bubble;
    }
    protected internal readonly List<PoisonPool> poisonPools=new List<PoisonPool>();
    protected internal readonly Dictionary<Transform,PoisonStatus> poisonedEnemies=new Dictionary<Transform,PoisonStatus>();
    private Material poisonLiquid,poisonRim,poisonBright;
    private Transform poisonNozzle,poisonWindow,poisonValve;
    private float poisonSprayClock;
    private float PoisonRadius=>equipment.Weapon(10).Range(.8f);
    private float PoisonDuration=>equipment.PoisonPoolDuration;
    private const float PoisonLandingTime=.18f;

    protected internal void BuildPoisonBackpack(Transform root)
    {
        Material wood=HammerMaterial("Poison Tank Wood",new Color(.54f,.33f,.16f));
        Material iron=HammerMaterial("Poison Tank Iron",new Color(.21f,.24f,.22f));
        Material strap=HammerMaterial("Poison Tank Straps",new Color(.72f,.59f,.36f));
        poisonLiquid=HammerMaterial("Poison Yellow Green",new Color(.56f,.74f,.12f),true);
        poisonRim=HammerMaterial("Poison Dark Rim",new Color(.19f,.28f,.06f),true);
        poisonBright=HammerMaterial("Poison Bright Bubbles",new Color(.77f,.94f,.3f),true);
        HammerPart(root,"Wood Reservoir",Vector3.zero,new Vector3(.52f,.58f,.3f),.06f,wood);
        HammerPart(root,"Iron Cap",new Vector3(0,.34f,0),new Vector3(.24f,.11f,.2f),.018f,iron);
        for(int sign=-1;sign<=1;sign+=2)
        {
            HammerPart(root,"Tank Iron Band",new Vector3(0,sign*.2f,0),new Vector3(.57f,.07f,.35f),.012f,iron);
            HammerPart(root,"Shoulder Strap",new Vector3(sign*.29f,0,.16f),new Vector3(.065f,.62f,.07f),.012f,strap);
        }
        HammerPart(root,"Level Window Frame",new Vector3(0,.015f,-.16f),new Vector3(.19f,.33f,.045f),.012f,iron);
        HammerPart(root,"Poison Level Window",new Vector3(0,-.015f,-.186f),new Vector3(.12f,.23f,.025f),.006f,poisonLiquid);
        poisonWindow=root.GetChild(root.childCount-1);
        HammerPart(root,"Liquid Highlight",new Vector3(-.027f,-.01f,-.202f),new Vector3(.018f,.14f,.012f),.003f,poisonBright);
        HammerPart(root,"Outlet Elbow",new Vector3(.3f,-.14f,-.02f),new Vector3(.2f,.09f,.1f),.012f,iron);
        HammerPart(root,"Rear Spray Nozzle",new Vector3(.38f,-.2f,-.12f),new Vector3(.11f,.11f,.25f),.018f,iron);
        poisonNozzle=root.GetChild(root.childCount-1);
        poisonValve=new GameObject("Slow-release Valve").transform;
        poisonValve.SetParent(root,false); poisonValve.localPosition=new Vector3(.32f,.2f,-.22f);
        var brass=HammerMaterial("Valve Brass",new Color(.85f,.61f,.18f));
        HammerPart(poisonValve,"Valve Stem",Vector3.zero,new Vector3(.07f,.07f,.14f),.01f,iron);
        for(int i=0;i<4;i++)
        {
            HammerPart(poisonValve,"Valve Wheel",new Vector3(0,0,-.08f),new Vector3(.28f,.045f,.045f),.009f,brass);
            poisonValve.GetChild(poisonValve.childCount-1).localRotation=Quaternion.Euler(0,0,i*45);
        }
        poisonValve.gameObject.SetActive(equipment.Stacks(RunItem.SlowReleaseValve)>0);
    }

    private void PosePoisonBackpack(Transform visual)
    {
        Vector3 forward=player.LastTravelDirection;
        Quaternion facing=Quaternion.LookRotation(forward);
        float windup=equipment.PoisonSprayInterval-.15f;
        float pulse=poisonSprayClock>windup?Mathf.Sin((poisonSprayClock-windup)/.15f*Mathf.PI)*.035f:0;
        visual.SetPositionAndRotation(player.transform.position-forward*(.34f+pulse)+Vector3.up*.28f,facing);
        if(poisonWindow!=null) poisonWindow.localScale=new Vector3(1,1+pulse*4,1);
        if(poisonValve!=null) poisonValve.gameObject.SetActive(equipment.Stacks(RunItem.SlowReleaseValve)>0);
    }

    protected internal void UpdatePoisonBackpack(float dt,Transform visual)
    {
        PosePoisonBackpack(visual);
        if(!Finite(dt) || dt<=0) return;
        // Fixed short steps preserve spray/tick cadence and brief overlap across long frames.
        for(float remaining=dt;remaining>0;)
        {
            float step=Mathf.Min(.02f,remaining); remaining-=step;
            poisonSprayClock+=step;
            if(poisonSprayClock>=equipment.PoisonSprayInterval-.00001f)
            { poisonSprayClock=Mathf.Max(0,poisonSprayClock-equipment.PoisonSprayInterval); SprayPoison(); }
            UpdatePoisonPools(step);
            UpdatePoisonDamage(step);
        }
        PosePoisonBackpack(visual);
    }

    private void SprayPoison()
    {
        Vector3 origin=player.transform.position,direction=-player.LastTravelDirection;
        float distance=Mathf.Min(1.15f,DistanceToBorder(origin,direction));
        if(obstacles!=null) distance=Mathf.Min(distance,obstacles.HitDistance(origin,direction,.08f)-.05f);
        if(distance<.12f) return;
        Vector3 center=origin+direction*distance; center.y=.025f;
        Vector3 nozzle=poisonNozzle.position-direction*.12f;
        // The backpack may clip adjacent cover; never draw the spray emerging through it.
        if(obstacles!=null && !obstacles.Clear(origin,nozzle,.04f)) nozzle=origin;
        GameAudio.Play(AudioCue.Poison);
        SpawnPoisonPool(center,nozzle);
    }

    protected internal void SpawnPoisonPool(Vector3 center,Vector3 origin)
    {
        PoisonPool pool=poisonPools.Find(p=>!p.Active);
        if(pool==null && poisonPools.Count>=8)
        {
            pool=poisonPools[0]; foreach(var other in poisonPools) if(other.Age>pool.Age) pool=other;
        }
        if(pool==null)
        {
            pool=new PoisonPool(); poisonPools.Add(pool);
            pool.Root=new GameObject("Player Poison Pool").transform; pool.Root.SetParent(transform,false);
            pool.Surface=new GameObject("Poison Ground",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            pool.Surface.SetParent(pool.Root,false);
            pool.Mesh=new Mesh {name="Clipped Poison Puddle",subMeshCount=2}; hammerMeshes.Add(pool.Mesh);
            pool.Surface.GetComponent<MeshFilter>().sharedMesh=pool.Mesh;
            var renderer=pool.Surface.GetComponent<MeshRenderer>(); renderer.sharedMaterials=new[] {poisonRim,poisonLiquid};
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            pool.Bubbles=new Transform[3];
            for(int i=0;i<3;i++)
            {
                var bubble=CreateVisual(PrimitiveType.Cube,"Poison Pool Bubble",Vector3.zero,Vector3.one*.075f,poisonBright);
                bubble.SetParent(pool.Root,false); pool.Bubbles[i]=bubble;
            }
            pool.Drop=CreateVisual(PrimitiveType.Cube,"Poison Spray Drop",Vector3.zero,Vector3.one*.18f,poisonLiquid);
            pool.Drop.SetParent(pool.Root,false);
        }
        pool.Center=center; pool.Origin=origin; pool.Radius=PoisonRadius; pool.Duration=PoisonDuration;
        pool.Damage=equipment.Weapon(10).Damage(3); pool.Age=-PoisonLandingTime; pool.Active=true;
        pool.Root.position=center; pool.Root.gameObject.SetActive(true);
        // Clip a two-tone polygon to cover so bright ground poison cannot visibly spill through walls.
        const int count=16; var vertices=new Vector3[1+count*2]; var rim=new List<int>(); var inner=new List<int>();
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count; Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            float radius=pool.Radius*(i%3==0?1:.94f);
            radius=Mathf.Min(radius,DistanceToBorder(center,radial));
            if(obstacles!=null) radius=Mathf.Min(radius,Mathf.Max(0,obstacles.HitDistance(center,radial,0)-.025f));
            vertices[1+i]=radial*radius; vertices[1+count+i]=radial*(radius*.84f)+Vector3.up*.005f;
            int j=(i+1)%count;
            inner.AddRange(new[] {0,1+count+j,1+count+i});
            rim.AddRange(new[] {1+i,1+j,1+count+j,1+i,1+count+j,1+count+i});
        }
        pool.Mesh.Clear(); pool.Mesh.subMeshCount=2; pool.Mesh.vertices=vertices;
        pool.Mesh.SetTriangles(rim,0); pool.Mesh.SetTriangles(inner,1); pool.Mesh.RecalculateNormals(); pool.Mesh.RecalculateBounds();
        AnimatePoisonPool(pool);
    }

    private void AnimatePoisonPool(PoisonPool pool)
    {
        bool landed=pool.Age>=0;
        pool.Surface.gameObject.SetActive(landed); pool.Drop.gameObject.SetActive(!landed);
        float fade=Mathf.Clamp01((pool.Duration-pool.Age)/.35f);
        pool.Surface.localScale=Vector3.one*fade;
        if(!landed)
        {
            float t=Mathf.Clamp01(1+pool.Age/PoisonLandingTime);
            pool.Drop.position=Vector3.Lerp(pool.Origin,pool.Center,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.32f);
            pool.Drop.rotation=Quaternion.Euler(t*120,t*180,25);
        }
        for(int i=0;i<pool.Bubbles.Length;i++)
        {
            Transform bubble=pool.Bubbles[i]; bubble.gameObject.SetActive(landed);
            float angle=i*2.1f; Vector3 point=pool.Center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(pool.Radius*.42f);
            bool clear=obstacles==null || obstacles.Clear(pool.Center,point);
            bubble.localScale=Vector3.one*(clear?(.04f+.035f*Mathf.Sin(pool.Age*6+i))*fade:0);
            bubble.position=point+Vector3.up*(.04f+Mathf.Repeat(pool.Age*.3f+i*.1f,.16f));
        }
    }

    private void UpdatePoisonPools(float dt)
    {
        foreach(var pool in poisonPools)
        {
            if(!pool.Active) continue;
            pool.Age+=dt;
            if(pool.Age>=pool.Duration) { pool.Active=false; pool.Root.gameObject.SetActive(false); continue; }
            AnimatePoisonPool(pool);
        }
    }

    private void UpdatePoisonDamage(float dt)
    {
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i]; int damage=0;
            foreach(var pool in poisonPools)
            {
                if(!pool.Active || pool.Age<0) continue;
                Vector3 delta=enemy.position-pool.Center; delta.y=0;
                float radius=pool.Radius*Mathf.Clamp01((pool.Duration-pool.Age)/.35f)+EnemyRadius(enemy)*(IsForestBoss(enemy)?1:.25f);
                if(delta.sqrMagnitude<=radius*radius && (obstacles==null || obstacles.Clear(pool.Center,enemy.position)))
                    damage=Mathf.Max(damage,pool.Damage);
            }
            if(!poisonedEnemies.TryGetValue(enemy,out var status))
            {
                if(damage==0) continue;
                status=new PoisonStatus {Damage=damage}; poisonedEnemies.Add(enemy,status);
                enemy.GetComponent<EnemyAvatar>()?.SetPoisoned(true);
                status.Bubble=CreateVisual(PrimitiveType.Cube,"Poisoned Enemy Bubble",enemy.position,Vector3.one*.08f,poisonBright);
            }
            if(damage>0) { status.Remaining=1; status.Damage=damage; }
            else status.Remaining-=dt;
            status.Age+=dt; status.NextTick-=dt;
            if(status.Remaining>=-.00001f && status.NextTick<=.00001f)
            {
                status.NextTick+=.5f;
                DamageEnemyCritical(i,status.Damage,HitKind.Poison,false);
                if(!enemyHealth.ContainsKey(enemy)) continue;
            }
            if(status.Remaining<=.00001f) { RemovePoison(enemy); continue; }
            status.Bubble.position=enemy.position+new Vector3(Mathf.Sin(status.Age*4)*.3f,-.2f+Mathf.Repeat(status.Age*.3f,.35f),-.18f);
        }
    }

    private void RemovePoison(Transform enemy)
    {
        if(!poisonedEnemies.TryGetValue(enemy,out var status)) return;
        if(status.Bubble!=null) { status.Bubble.gameObject.SetActive(false); Destroy(status.Bubble.gameObject); }
        if(enemy!=null) enemy.GetComponent<EnemyAvatar>()?.SetPoisoned(false);
        poisonedEnemies.Remove(enemy);
    }
    protected internal void ClearPoison()
    {
        foreach(var pool in poisonPools) { pool.Active=false; if(pool.Root!=null) pool.Root.gameObject.SetActive(false); }
        // Iterate a snapshot because removal also restores the avatar tint.
        if(poisonedEnemies.Count>0) foreach(var enemy in new List<Transform>(poisonedEnemies.Keys)) RemovePoison(enemy);
        poisonSprayClock=0;
        if(newWeaponVisuals[10]!=null) newWeaponVisuals[10].gameObject.SetActive(false);
    }
}
