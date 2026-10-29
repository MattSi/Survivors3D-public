using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    // Held, windup, flying, embedded. One reusable weapon visual per equipped spear.
    protected internal int spearPhase;
    protected internal float spearAge;
    protected internal Vector3 spearDirection=Vector3.forward,spearTip;
    protected internal readonly HashSet<Transform> spearHit=new HashSet<Transform>();
    protected internal void BuildThrowingSpear(Transform root)
    {
        Material wood=HammerMaterial("Spear Warm Wood",new Color(.65f,.32f,.09f));
        Material iron=HammerMaterial("Spear Iron",new Color(.19f,.21f,.23f));
        Material leather=HammerMaterial("Spear Leather",new Color(.28f,.13f,.07f));
        Material steel=HammerMaterial("Spear Steel",new Color(.68f,.72f,.75f));
        HammerPart(root,"Wooden Shaft",new Vector3(0,0,-1.35f),new Vector3(.11f,.11f,1.9f),.018f,wood);
        HammerPart(root,"Iron Socket",new Vector3(0,0,-.48f),new Vector3(.19f,.16f,.25f),.025f,iron);
        HammerPart(root,"Butt Cap",new Vector3(0,0,-2.3f),new Vector3(.15f,.15f,.14f),.02f,iron);
        for(int i=0;i<6;i++) HammerPart(root,"Grip Wrap",new Vector3(0,0,-1.25f-i*.075f),new Vector3(.145f,.145f,.06f),.015f,leather);
        for(int i=0;i<2;i++) HammerPart(root,"Socket Rivet",new Vector3(0,.09f,-.43f-i*.11f),new Vector3(.055f,.035f,.055f),.008f,steel);
        Vector3[] points={Vector3.zero,new Vector3(-.22f,0,-.35f),new Vector3(0,0,-.62f),new Vector3(.22f,0,-.35f),new Vector3(0,.095f,-.33f),new Vector3(0,-.095f,-.33f)};
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        for(int side=0;side<2;side++) for(int edge=0;edge<4;edge++)
        {
            int first=vertices.Count;
            vertices.Add(points[4+side]); vertices.Add(points[side==0?(edge+1)%4:edge]); vertices.Add(points[side==0?edge:(edge+1)%4]);
            triangles.Add(first); triangles.Add(first+1); triangles.Add(first+2);
        }
        var mesh=new Mesh {name="Ridged Spearhead"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); hammerMeshes.Add(mesh);
        var head=new GameObject("Steel Spearhead",typeof(MeshFilter),typeof(MeshRenderer)); head.transform.SetParent(root,false);
        head.GetComponent<MeshFilter>().sharedMesh=mesh; head.GetComponent<MeshRenderer>().sharedMaterial=steel;
    }
    protected internal Vector3 SpearHand => player.transform.position+player.transform.right*.5f+Vector3.up*.15f;
    protected internal bool AimThrowingSpear(Vector3 origin,out Vector3 aim)
    {
        aim=player.transform.forward; int best=0; float nearest=float.PositiveInfinity;
        foreach(Transform target in enemies)
        {
            Vector3 delta=target.position-origin; delta.y=0;
            float distance=delta.magnitude;
            if(distance>NewRange(3) || distance<.001f || (obstacles!=null && !obstacles.Clear(origin,target.position,.12f))) continue;
            Vector3 direction=delta/distance;
            float reach=DistanceToBorder(origin,direction);
            if(obstacles!=null) reach=Mathf.Min(reach,obstacles.HitDistance(origin,direction,.12f));
            int count=0;
            foreach(Transform enemy in enemies)
            {
                Vector3 gap=enemy.position-origin; gap.y=0; float along=Vector3.Dot(gap,direction);
                if(along>=0 && along<=reach && (gap-direction*along).sqrMagnitude<=.25f) count++;
            }
            if(count>best || (count==best && distance<nearest)) { best=count; nearest=distance; aim=direction; }
        }
        return best>0;
    }
    protected internal void UpdateThrowingSpear(float dt,Transform visual)
    {
        spearAge+=dt;
        if(spearPhase==0)
        {
            newCooldown[3]-=dt;
            if(newCooldown[3]<=0 && AimThrowingSpear(SpearHand,out Vector3 aim))
            { spearDirection=aim; spearAge=0; spearPhase=1; }
        }
        if(spearPhase<=1)
        {
            Quaternion rotation=spearPhase==0?Quaternion.LookRotation(player.transform.forward)*Quaternion.Euler(-65,0,0):Quaternion.LookRotation(spearDirection);
            visual.SetPositionAndRotation(SpearHand+(spearPhase==0?Vector3.up*.4f:Vector3.zero)+rotation*Vector3.forward*(1.25f-(spearPhase==1?Mathf.Clamp01(spearAge/.12f)*.35f:0)),rotation);
            if(spearPhase==1 && spearAge>=.12f)
            { GameAudio.Play(AudioCue.Spear); spearPhase=2; spearAge=0; spearHit.Clear(); spearTip=SpearHand; MoveThrownSpear(.9f); }
        }
        else if(spearPhase==2) MoveThrownSpear(20f*dt);
        else if(spearAge>=.2f)
        { spearPhase=0; spearAge=0; newCooldown[3]=NewInterval(3); spearHit.Clear(); }
        if(spearPhase>=2) visual.SetPositionAndRotation(spearTip,Quaternion.LookRotation(spearDirection));
        else if(spearPhase==0) visual.SetPositionAndRotation(SpearHand+Vector3.up*.4f+Quaternion.LookRotation(player.transform.forward)*Quaternion.Euler(-65,0,0)*Vector3.forward*1.25f,Quaternion.LookRotation(player.transform.forward)*Quaternion.Euler(-65,0,0));
    }
    protected internal void MoveThrownSpear(float travel)
    {
        float wall=DistanceToBorder(spearTip,spearDirection);
        if(obstacles!=null) wall=Mathf.Min(wall,obstacles.HitDistance(spearTip,spearDirection,.12f));
        travel=Mathf.Min(travel,Mathf.Max(0,wall));
        var ray=new Ray(spearTip,spearDirection);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i];
            if(spearHit.Contains(enemy)) continue;
            var bounds=EnemyHitBounds(enemy,.12f);
            if(!bounds.IntersectRay(ray,out float hit) || hit>travel || hit>=wall) continue;
            spearHit.Add(enemy); DamageEnemy(i,equipment.Weapon(3).Damage(WeaponCatalog.Damage[3]),HitKind.Melee);
        }
        spearTip+=spearDirection*travel;
        if(travel>=wall-.001f) { spearPhase=3; spearAge=0; }
    }
}
