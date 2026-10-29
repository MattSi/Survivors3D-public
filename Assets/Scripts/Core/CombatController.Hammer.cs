using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal readonly Dictionary<Transform,float> stunned=new Dictionary<Transform,float>();
    protected internal readonly Dictionary<Transform,Transform> stunStars=new Dictionary<Transform,Transform>();
    protected internal readonly HashSet<Transform> crushed=new HashSet<Transform>();
    protected internal readonly List<Transform> expiredStuns=new List<Transform>();
    protected internal readonly List<Mesh> hammerMeshes=new List<Mesh>();
    protected internal readonly List<Material> hammerMaterials=new List<Material>();
    protected internal sealed class GroundCrack { public LineRenderer Line; public float Age; }
    protected internal readonly List<GroundCrack> groundCracks=new List<GroundCrack>();
    protected internal Material hammerWood,hammerIron,hammerLeather,crackMaterial,starMaterial;
    protected internal Mesh starMesh;
    protected internal Vector3 hammerCenter,hammerLiftStart,hammerAim=Vector3.forward;
    protected internal float hammerAge,hammerReach;
    protected internal bool hammerActive,hammerLanded;
    protected internal const float HammerImpactTime=.37f;
    protected internal const float HammerMaxReach=2f;
    protected internal float HammerCenterRadius => 1.5f*equipment.RangeMultiplier;
    protected internal float HammerShockRadius => 2.8f*equipment.RangeMultiplier*(1+.1f*(equipment.Weapon(5).Level-1));

    protected internal Material HammerMaterial(string name,Color color,bool unlit=false)
    {
        var material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")) {name=name,color=color};
        if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.15f);
        hammerMaterials.Add(material); return material;
    }
    protected internal void BuildHammer(Transform root)
    {
        hammerWood=HammerMaterial("Hammer Warm Timber",new Color(.48f,.23f,.065f));
        hammerIron=HammerMaterial("Hammer Iron Bands",new Color(.14f,.125f,.12f));
        hammerLeather=HammerMaterial("Hammer Leather Grip",new Color(.23f,.13f,.075f));
        HammerPart(root,"Stone Hammer Head",new Vector3(0,1.15f,0),new Vector3(1.3f,.63f,.62f),.1f,NewWeaponMaterial(5));
        HammerPart(root,"Timber Handle",new Vector3(0,.5f,0),new Vector3(.15f,1.18f,.15f),.025f,hammerWood);
        HammerPart(root,"Leather Grip",new Vector3(0,.14f,0),new Vector3(.19f,.38f,.19f),.025f,hammerLeather);
        for(int i=0;i<4;i++) HammerPart(root,"Grip Wrap",new Vector3(0,.01f+i*.09f,0),new Vector3(.205f,.025f,.205f),.01f,hammerIron);
        for(int sign=-1;sign<=1;sign+=2)
        {
            HammerPart(root,"Iron Reinforcing Band",new Vector3(sign*.4f,1.15f,0),new Vector3(.16f,.66f,.65f),.07f,hammerIron);
            for(int y=-1;y<=1;y+=2)
                HammerPart(root,"Band Rivet",new Vector3(sign*.4f,1.15f+y*.16f,-.34f),new Vector3(.065f,.065f,.04f),.01f,hammerLeather);
        }
        // Narrow raised strips suggest the broad timber grain used by the forest props.
        for(int i=0;i<3;i++) WeaponPart(root,"Wood Grain",new Vector3(-.045f+i*.04f,.65f,-.077f),new Vector3(.008f,.54f-i*.1f,.004f),hammerLeather);
        HammerPart(root,"Handle End Cap",new Vector3(0,-.085f,0),new Vector3(.2f,.09f,.2f),.02f,hammerIron);
    }
    protected internal void HammerPart(Transform root,string name,Vector3 position,Vector3 size,float bevel,Material material)
    {
        var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        part.transform.SetParent(root,false); part.transform.localPosition=position;
        Mesh mesh=BeveledBox(size,bevel); hammerMeshes.Add(mesh);
        part.GetComponent<MeshFilter>().sharedMesh=mesh; part.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    protected internal static Mesh BeveledBox(Vector3 size,float bevel)
    {
        Vector3 h=size*.5f; bevel=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.9f);
        var vertices=new List<Vector3>(); var triangles=new List<int>();
        System.Action<Vector3[]> face=points=> {
            Vector3 center=Vector3.zero; foreach(Vector3 p in points) center+=p;
            if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),center)<0) System.Array.Reverse(points);
            int first=vertices.Count; vertices.AddRange(points);
            for(int i=1;i<points.Length-1;i++) { triangles.Add(first); triangles.Add(first+i); triangles.Add(first+i+1); }
        };
        for(int axis=0;axis<3;axis++) for(int sign=-1;sign<=1;sign+=2)
        {
            int b=(axis+1)%3,c=(axis+2)%3; var points=new Vector3[4];
            for(int i=0;i<4;i++) { Vector3 p=Vector3.zero; p[axis]=sign*h[axis]; p[b]=(i<2?-1:1)*(h[b]-bevel); p[c]=(i==0 || i==3?-1:1)*(h[c]-bevel); points[i]=p; }
            face(points);
        }
        for(int a=0;a<3;a++) for(int b=a+1;b<3;b++) for(int sa=-1;sa<=1;sa+=2) for(int sb=-1;sb<=1;sb+=2)
        {
            int c=3-a-b; var points=new Vector3[4];
            for(int i=0;i<4;i++) { Vector3 p=Vector3.zero; bool edge=i<2; p[a]=sa*(h[a]-(edge?0:bevel)); p[b]=sb*(h[b]-(edge?bevel:0)); p[c]=(i==0 || i==3?-1:1)*(h[c]-bevel); points[i]=p; }
            face(points);
        }
        for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
        {
            Vector3 corner=Vector3.Scale(h,new Vector3(x,y,z)); var points=new Vector3[3];
            for(int a=0;a<3;a++) { Vector3 p=corner; for(int b=0;b<3;b++) if(a!=b) p[b]-=Mathf.Sign(p[b])*bevel; points[a]=p; } face(points);
        }
        var mesh=new Mesh {name="Chamfered Hammer Part"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    protected internal void UpdateHammer(float dt,Transform visual)
    {
        newCooldown[5]-=dt;
        Vector3 origin=player.transform.position;
        if(!hammerActive && newCooldown[5]<=0)
        {
            if(TryFindHammerCenter(origin,out Vector3 selectedCenter))
            {
                hammerCenter=selectedCenter;
                Vector3 delta=hammerCenter-origin; delta.y=0;
                hammerAim=delta.sqrMagnitude<.001f?Vector3.forward:delta.normalized;
                hammerReach=Mathf.Min(delta.magnitude,HammerMaxReach);
                hammerLiftStart=origin+hammerAim*.45f+Vector3.up*.9f;
                hammerAge=0; hammerActive=true; hammerLanded=false; newCooldown[5]=NewInterval(5);
            }
        }
        if(!hammerActive)
        { visual.SetPositionAndRotation(origin+Vector3.Cross(Vector3.up,hammerAim)*.6f,Quaternion.LookRotation(hammerAim)*Quaternion.Euler(0,0,-18)); return; }
        hammerAge+=dt;
        if(!hammerLanded) { hammerCenter=origin+hammerAim*hammerReach; hammerCenter.y=.035f; }
        Vector3 raised=hammerLiftStart+Vector3.up*.8f;
        Vector3 head; float angle;
        if(hammerAge<.25f) { float t=Mathf.SmoothStep(0,1,hammerAge/.25f); head=Vector3.Lerp(hammerLiftStart,raised,t); angle=Mathf.Lerp(0,-30,t); }
        else if(hammerAge<HammerImpactTime) { float t=Mathf.Clamp01((hammerAge-.25f)/.12f); head=Vector3.Lerp(raised,hammerCenter+Vector3.up*.34f,t*t); angle=Mathf.Lerp(-30,78,t*t); }
        else
        {
            if(!hammerLanded) { hammerLanded=true; SlamHammer(hammerCenter); }
            float t=Mathf.SmoothStep(0,1,(hammerAge-.5f)/.25f);
            head=Vector3.Lerp(hammerCenter+Vector3.up*.34f,origin+Vector3.up*1.15f,t); angle=Mathf.Lerp(78,0,t);
            if(hammerAge>=.75f) hammerActive=false;
        }
        Quaternion rotation=Quaternion.LookRotation(hammerAim)*Quaternion.Euler(angle,0,0);
        visual.SetPositionAndRotation(head-rotation*(Vector3.up*1.15f),rotation);
    }
    protected internal bool TryFindHammerCenter(Vector3 origin,out Vector3 center)
    {
        Vector3 best=origin; best.y=.035f;
        int bestKills=-1,bestHits=-1;
        float bestDistance=float.PositiveInfinity;
        bool nearby=false;
        foreach(Transform enemy in enemies)
        {
            Vector3 gap=enemy.position-origin; gap.y=0;
            if(BossSurfaceDistance(enemy,origin)<=NewRange(5) && (obstacles==null || obstacles.Clear(origin,enemy.position))) { nearby=true; break; }
        }
        if(!nearby) { center=best; return false; }
        // Sample the reachable ground as well as enemy directions, including gaps between enemies.
        System.Action<Vector3> consider=candidate=> {
            Vector3 offset=candidate-origin; offset.y=0;
            candidate=origin+Vector3.ClampMagnitude(offset,HammerMaxReach); candidate.y=.035f;
            if(obstacles!=null && !obstacles.Clear(origin,candidate,EnemySize/2f)) return;
            int kills=0,hits=0;
            foreach(Transform enemy in enemies)
            {
                Vector3 gap=enemy.position-candidate; gap.y=0;
                float distanceToBody=BossSurfaceDistance(enemy,candidate);
                if(distanceToBody>HammerShockRadius || (obstacles!=null && !obstacles.Clear(candidate,enemy.position))) continue;
                hits++;
                if(distanceToBody<=HammerCenterRadius) kills++;
            }
            offset=candidate-origin; offset.y=0; float distance=offset.sqrMagnitude;
            if(kills>bestKills || (kills==bestKills && (hits>bestHits || (hits==bestHits && distance<bestDistance))))
            { best=candidate; bestKills=kills; bestHits=hits; bestDistance=distance; }
        };
        consider(origin);
        foreach(Transform enemy in enemies)
        {
            Vector3 gap=enemy.position-origin; gap.y=0;
            if(BossSurfaceDistance(enemy,origin)<=NewRange(5)) consider(BossSurfacePoint(enemy,origin));
        }
        for(int ring=1;ring<=5;ring++) for(int step=0;step<32;step++)
        {
            float angle=step*Mathf.PI/16;
            consider(origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(ring*HammerMaxReach/5));
        }
        center=best; return bestHits>0;
    }
    protected internal void SlamHammer(Vector3 center)
    {
        GameAudio.Play(AudioCue.Hammer);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i]; Vector3 delta=enemy.position-center; delta.y=0;
            float distanceToBody=BossSurfaceDistance(enemy,center);
            if(distanceToBody>HammerShockRadius || (obstacles!=null && !obstacles.Clear(center,enemy.position))) continue;
            if(distanceToBody<=HammerCenterRadius)
            {
                crushed.Add(enemy);
                DamageWeaponEnemy(i,equipment.Weapon(5).Damage(WeaponCatalog.Damage[5]),HitKind.Melee,5,false);
                crushed.Remove(enemy);
                if(enemyHealth.ContainsKey(enemy)) StunEnemy(enemy,1f);
            }
            else
            {
                PushEnemyApart(enemy,delta.normalized,.4f);
                StunEnemy(enemy,1f);
            }
        }
        CreateGroundCracks(center);
        if(viewCamera!=null) viewCamera.Shake(.08f,.18f);
    }
    protected internal void StunEnemy(Transform enemy,float duration)
    {
        // Brief recovery stagger rewards the hammer without cancelling telegraphs or stun-locking the boss.
        if(IsForestBoss(enemy)) { StaggerForestBoss(duration); return; }
        InterruptCharger(enemy);
        InterruptBomber(enemy);
        InterruptShooter(enemy);
        stunned[enemy]=Time.time+duration;
        if(stunStars.ContainsKey(enemy)) return;
        if(starMaterial==null) starMaterial=HammerMaterial("Stun Gold",new Color(1,.82f,.15f),true);
        if(starMesh==null)
        {
            var v=new Vector3[11]; var t=new int[30];
            for(int i=0;i<10;i++) { float angle=i*Mathf.PI/5; v[i+1]=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(i%2==0?.12f:.055f); t[i*3]=0; t[i*3+1]=i+1; t[i*3+2]=(i+1)%10+1; }
            starMesh=new Mesh {name="Stun Star"}; starMesh.vertices=v; starMesh.triangles=t; starMesh.RecalculateNormals(); hammerMeshes.Add(starMesh);
            starMaterial.SetFloat("_Cull",0);
        }
        Transform root=new GameObject("Stun Stars").transform; root.SetParent(enemy,false); root.localPosition=Vector3.up*1.05f;
        for(int i=0;i<3;i++)
        {
            var star=new GameObject("Star",typeof(MeshFilter),typeof(MeshRenderer)); star.transform.SetParent(root,false);
            float angle=i*Mathf.PI*2/3; star.transform.localPosition=new Vector3(Mathf.Cos(angle)*.35f,0,Mathf.Sin(angle)*.35f);
            star.transform.localRotation=Quaternion.Euler(35,i*120,0); star.GetComponent<MeshFilter>().sharedMesh=starMesh; star.GetComponent<MeshRenderer>().sharedMaterial=starMaterial;
        }
        stunStars[enemy]=root;
    }
    protected internal void RemoveStun(Transform enemy)
    {
        stunned.Remove(enemy);
        if(stunStars.TryGetValue(enemy,out Transform stars)) { if(stars!=null) Destroy(stars.gameObject); stunStars.Remove(enemy); }
    }
    protected internal void CreateGroundCracks(Vector3 center)
    {
        if(crackMaterial==null) crackMaterial=HammerMaterial("Ground Crack",new Color(.12f,.08f,.035f),true);
        for(int spoke=0;spoke<8;spoke++)
        {
            float angle=spoke*Mathf.PI/4; Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            float reach=Mathf.Min(HammerShockRadius,DistanceToBorder(center,direction));
            if(obstacles!=null) reach=Mathf.Min(reach,obstacles.HitDistance(center,direction,.05f));
            var line=Ring("Ground Fissure",center,0,crackMaterial,.065f); line.loop=false; line.positionCount=5;
            for(int p=0;p<5;p++) line.SetPosition(p,direction*(reach*p/4)+Vector3.Cross(Vector3.up,direction)*(p>0 && p<4?(p%2==0?.12f:-.1f):0));
            groundCracks.Add(new GroundCrack {Line=line});
        }
        impacts.Add(new Impact {Ring=Ring("Hammer Shockwave",center,HammerShockRadius,NewWeaponMaterial(5),.09f),Duration=.35f});
    }
    protected internal void UpdateHammerEffects(float dt)
    {
        expiredStuns.Clear();
        foreach(var pair in stunned)
        {
            if(pair.Key==null || Time.time>=pair.Value) { expiredStuns.Add(pair.Key); continue; }
            if(stunStars.TryGetValue(pair.Key,out Transform stars) && stars!=null)
            {
                stars.Rotate(0,210*dt,0);
                // Shrink over the last 0.3s: the control window is visibly closing.
                stars.localScale=Vector3.one*Mathf.Clamp01((pair.Value-Time.time)/.3f);
            }
        }
        foreach(Transform enemy in expiredStuns) RemoveStun(enemy);
        for(int i=groundCracks.Count-1;i>=0;i--)
        {
            var crack=groundCracks[i]; crack.Age+=dt;
            if(crack.Age>=1.1f) { Destroy(crack.Line.gameObject); groundCracks.RemoveAt(i); continue; }
            crack.Line.startWidth=crack.Line.endWidth=.065f*(1-Mathf.SmoothStep(0,1,(crack.Age-.65f)/.45f));
        }
    }
    protected internal void ClearHammerState()
    {
        if(viewCamera!=null) viewCamera.StopShake();
        hammerActive=hammerLanded=false; hammerAge=0; stunned.Clear(); crushed.Clear();
        foreach(Transform stars in stunStars.Values) if(stars!=null) Destroy(stars.gameObject); stunStars.Clear();
        foreach(var crack in groundCracks) if(crack.Line!=null) Destroy(crack.Line.gameObject); groundCracks.Clear();
    }
    protected internal void DisposeHammer()
    {
        foreach(Material material in hammerMaterials) if(material!=null) Destroy(material);
        foreach(Mesh mesh in hammerMeshes) if(mesh!=null) Destroy(mesh);
    }
}
