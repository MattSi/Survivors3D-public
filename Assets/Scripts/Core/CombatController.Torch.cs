using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    private const float TorchWindup=.14f, TorchSweep=.18f, TorchRecovery=.16f;
    private readonly HashSet<Transform> torchHit=new HashSet<Transform>();
    private readonly List<Transform> torchFlames=new List<Transform>(),torchEmbers=new List<Transform>();
    private LineRenderer torchTrail;
    private Transform torchSweepFire;
    private readonly List<Transform> torchSweepTongues=new List<Transform>();
    private readonly List<Vector3> torchSweepSizes=new List<Vector3>();
    private float torchSweepFade=.22f;
    private float torchTime;
    private float TorchArc=>140+20*(equipment.Weapon(7).Level-1);
    private float TorchPush=>.9f+.3f*(equipment.Weapon(7).Level-1);

    private void BuildTorch(Transform root)
    {
        Material wood=HammerMaterial("Torch Timber",new Color(.46f,.23f,.085f));
        Material iron=HammerMaterial("Torch Iron Collar",new Color(.17f,.18f,.18f));
        Material cloth=HammerMaterial("Torch Cloth",new Color(.69f,.51f,.28f));
        Material flame=NewWeaponMaterial(7);
        Material core=HammerMaterial("Torch Golden Flame",new Color(1,.83f,.12f),true);
        HammerPart(root,"Torch Timber Handle",new Vector3(0,.32f,0),new Vector3(.14f,.85f,.14f),.025f,wood);
        HammerPart(root,"Torch Wrapped Head",new Vector3(0,.78f,0),new Vector3(.28f,.26f,.25f),.035f,cloth);
        HammerPart(root,"Torch Iron Collar",new Vector3(0,.62f,0),new Vector3(.3f,.13f,.27f),.02f,iron);
        for(int i=0;i<2;i++)
        {
            HammerPart(root,"Torch Grip Wrap",new Vector3(0,.05f+i*.075f,0),new Vector3(.17f,.055f,.17f),.01f,cloth);
            HammerPart(root,"Torch Cross Binding",new Vector3(0,.77f,.14f),new Vector3(.29f,.06f,.025f),.006f,cloth);
            root.GetChild(root.childCount-1).localRotation=Quaternion.Euler(0,0,i==0?30:-30);
        }
        HammerPart(root,"Torch Collar Rivet",new Vector3(0,.62f,.145f),Vector3.one*.065f,.009f,iron);
        // Flat triangular faces form compact pointed flames rather than a rectangular fire block.
        Vector3[] points={new Vector3(0,1,0),new Vector3(0,-.2f,0),Vector3.left*.5f,Vector3.forward*.4f,Vector3.right*.5f,Vector3.back*.4f};
        int[] faces={0,2,3,0,3,4,0,4,5,0,5,2,1,3,2,1,4,3,1,5,4,1,2,5};
        var vertices=new Vector3[faces.Length]; var indices=new int[faces.Length];
        for(int i=0;i<faces.Length;i++) { vertices[i]=points[faces[i]]; indices[i]=i; }
        var mesh=new Mesh {name="Torch Flame Facets",vertices=vertices,triangles=indices};
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); hammerMeshes.Add(mesh);
        for(int i=0;i<4;i++)
        {
            Transform tongue=new GameObject(i==3?"Torch Yellow Core":"Torch Flame Tongue",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            tongue.SetParent(root,false); tongue.localPosition=new Vector3(i==3?0:(i-1)*.095f,.91f,i==3?-.045f:0);
            tongue.GetComponent<MeshFilter>().sharedMesh=mesh;
            tongue.GetComponent<MeshRenderer>().sharedMaterial=i==3?core:flame;
            tongue.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            torchFlames.Add(tongue);
        }
        for(int i=0;i<3;i++)
        {
            Transform ember=CreateVisual(PrimitiveType.Cube,"Torch Ember",Vector3.zero,Vector3.one*.04f,core);
            ember.SetParent(root,false); torchEmbers.Add(ember);
        }
        Material sweepOrange=HammerMaterial("Torch Sweep Orange",new Color(1,.38f,.035f),true);
        torchSweepFire=new GameObject("Torch Sweeping Flames").transform;
        torchSweepFire.SetParent(transform,false);
        for(int i=0;i<12;i++)
        {
            Transform tongue=new GameObject("Sweep Flame",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            tongue.SetParent(torchSweepFire,false);
            tongue.GetComponent<MeshFilter>().sharedMesh=mesh;
            tongue.GetComponent<MeshRenderer>().sharedMaterial=sweepOrange;
            tongue.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Transform inner=new GameObject("Sweep Yellow Core",typeof(MeshFilter),typeof(MeshRenderer)).transform;
            inner.SetParent(tongue,false); inner.localPosition=new Vector3(0,.015f,-.06f); inner.localScale=new Vector3(.58f,.65f,1.15f);
            inner.GetComponent<MeshFilter>().sharedMesh=mesh;
            inner.GetComponent<MeshRenderer>().sharedMaterial=core;
            inner.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            torchSweepTongues.Add(tongue); torchSweepSizes.Add(Vector3.zero);
        }
        torchSweepFire.gameObject.SetActive(false);
        torchTrail=Ring("Torch Sweep Trail",Vector3.zero,1,sweepOrange,.06f);
        torchTrail.loop=false; torchTrail.useWorldSpace=true; torchTrail.positionCount=14; torchTrail.enabled=false;
        torchTrail.startWidth=.035f; torchTrail.endWidth=.25f;
    }

    private void UpdateTorch(float dt,Transform visual)
    {
        torchTime+=dt; newCooldown[7]-=dt;
        Vector3 origin=player.transform.position;
        if(!swinging[7])
        {
            Transform target=FindItemTarget(origin,NewRange(7));
            Vector3 aim=target!=null?target.position-origin:player.transform.forward; aim.y=0;
            if(aim.sqrMagnitude>.001f) newAim[7]=aim.normalized;
            if(target!=null && newCooldown[7]<=0)
            { swinging[7]=true; swingAge[7]=0; torchHit.Clear(); newCooldown[7]=NewInterval(7); }
        }
        Vector3 forward=newAim[7].sqrMagnitude>.001f?newAim[7]:Vector3.forward;
        float yaw=15,tilt=20,reach=0;
        FadeTorchSweep(dt);
        if(swinging[7])
        {
            float previous=swingAge[7]; swingAge[7]+=dt;
            float age=swingAge[7],half=TorchArc*.5f;
            float progress=Mathf.Clamp01((age-TorchWindup)/TorchSweep);
            if(previous<TorchWindup && age>=TorchWindup) GameAudio.Play(AudioCue.Torch);
            if(age>=TorchWindup && previous<TorchWindup+TorchSweep)
            {
                float from=Mathf.Lerp(-half,half,Mathf.Clamp01((previous-TorchWindup)/TorchSweep));
                float to=Mathf.Lerp(-half,half,progress);
                HitTorchSector(forward,from,to);
                ShowTorchSweep(origin,forward,to-18,half);
            }
            if(age<TorchWindup)
            { float t=age/TorchWindup; yaw=Mathf.Lerp(15,-half-18,t); tilt=Mathf.Lerp(20,70,t); }
            else if(age<TorchWindup+TorchSweep)
            {
                yaw=Mathf.Lerp(-half,half,progress)-18; tilt=70; reach=.45f;

            }
            else
            { float t=Mathf.Clamp01((age-TorchWindup-TorchSweep)/TorchRecovery); yaw=Mathf.Lerp(half-18,15,t); tilt=Mathf.Lerp(70,20,t); reach=.45f*(1-t); }
            if(age>=TorchWindup+TorchSweep+TorchRecovery) swinging[7]=false;
        }
        Quaternion heading=Quaternion.LookRotation(forward)*Quaternion.Euler(0,yaw,0);
        float extension=1+.4f*Mathf.Clamp01(reach/.45f);
        visual.SetPositionAndRotation(origin+heading*(new Vector3(.42f,.05f,.15f+reach)*extension),heading*Quaternion.Euler(tilt,0,-10));
        visual.localScale=Vector3.one*(equipment.RangeMultiplier*extension);
        for(int i=0;i<torchFlames.Count;i++)
        {
            float flicker=(1+.12f*Mathf.Sin(torchTime*13+i*2.1f))*(torchSweepFade<.05f?1.75f:1);
            torchFlames[i].localScale=new Vector3(i==3?.25f:.3f,(i==1?.58f:i==3?.38f:.42f)*flicker,.27f);
            torchFlames[i].localRotation=Quaternion.Euler(0,i*55,Mathf.Sin(torchTime*7+i)*7+(i==0?15:i==2?-15:0));
        }
        for(int i=0;i<torchEmbers.Count;i++)
        {
            float t=Mathf.Repeat(torchTime*.8f+i/3f,1);
            torchEmbers[i].localPosition=new Vector3(Mathf.Sin(i*2+torchTime)*.11f,1.1f+t*.4f,Mathf.Cos(i*2+torchTime)*.08f);
            torchEmbers[i].localScale=Vector3.one*(.045f*(1-t));
        }
    }

    private Vector3 TorchSweepPoint(Vector3 origin,Vector3 forward,float angle)
    {
        Quaternion heading=Quaternion.LookRotation(forward)*Quaternion.Euler(0,angle,0);
        Vector3 tip=new Vector3(.42f,.05f,.6f)+Quaternion.Euler(70,0,-10)*(Vector3.up*(1.02f*equipment.RangeMultiplier));
        return origin+heading*(tip*1.4f);
    }

    private void ShowTorchSweep(Vector3 origin,Vector3 forward,float yaw,float half)
    {
        torchSweepFade=0;
        torchTrail.enabled=true; torchSweepFire.gameObject.SetActive(true);
        torchTrail.startWidth=.035f; torchTrail.endWidth=.25f*equipment.RangeMultiplier;
        float start=Mathf.Max(-half-18,yaw-105);
        for(int i=0;i<torchTrail.positionCount;i++)
            torchTrail.SetPosition(i,TorchSweepPoint(origin,forward,Mathf.Lerp(start,yaw,(float)i/(torchTrail.positionCount-1))));
        for(int i=0;i<torchSweepTongues.Count;i++)
        {
            float t=(float)i/(torchSweepTongues.Count-1);
            float angle=Mathf.Lerp(start,yaw,t);
            Transform tongue=torchSweepTongues[i];
            tongue.position=TorchSweepPoint(origin,forward,angle)+Vector3.up*.035f;
            tongue.rotation=Quaternion.LookRotation(Quaternion.AngleAxis(angle,Vector3.up)*forward)*Quaternion.Euler(0,i*29,-25);
            float size=Mathf.Lerp(.35f,1,t)*equipment.RangeMultiplier*1.2f;
            Vector3 scale=new Vector3(.38f,.5f+.18f*Mathf.Sin(i*2.3f),.26f)*size;
            tongue.localScale=scale; torchSweepSizes[i]=scale;
            // Bring the inner flame onto the visible face, rather than burying it inside the opaque shell.
            Vector3 towardView=((viewCamera!=null?viewCamera.transform.position:player.transform.position)-tongue.position).normalized;
            tongue.GetChild(0).position=tongue.position+towardView*(.15f*size)+Vector3.up*(.025f*size);
        }
    }

    private void FadeTorchSweep(float dt)
    {
        torchSweepFade+=dt;
        float remaining=Mathf.Clamp01(1-torchSweepFade/.22f);
        torchTrail.enabled=remaining>0;
        torchSweepFire.gameObject.SetActive(remaining>0);
        torchTrail.startWidth=.035f*remaining;
        torchTrail.endWidth=.25f*equipment.RangeMultiplier*remaining;
        for(int i=0;i<torchSweepTongues.Count;i++)
        {
            torchSweepTongues[i].localScale=torchSweepSizes[i]*remaining;
            if(remaining>0) torchSweepTongues[i].position+=Vector3.up*(dt*.35f);
        }
    }

    private void HitTorchSector(Vector3 aim,float from,float to)
    {
        Vector3 origin=player.transform.position;
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i]; if(torchHit.Contains(enemy)) continue;
            Vector3 delta=BossSurfacePoint(enemy,origin)-origin; delta.y=0;
            float angle=Vector3.SignedAngle(aim,delta,Vector3.up);
            if(delta.sqrMagnitude>NewRange(7)*NewRange(7) || angle<from || angle>to ||
                (obstacles!=null && !obstacles.Clear(origin,enemy.position))) continue;
            torchHit.Add(enemy); Ignite(enemy,true);
            DamageEnemy(i,equipment.Weapon(7).Damage(WeaponCatalog.Damage[7]),HitKind.Melee);
            if(!enemyHealth.ContainsKey(enemy)) continue;
            Vector3 push=delta.sqrMagnitude>.001f?delta.normalized:aim;
            float distance=TorchPush;
            if(obstacles!=null) distance=Mathf.Min(distance,Mathf.Max(0,obstacles.HitDistance(enemy.position,push,EnemyRadius(enemy))-.01f));
            // The entire enemy footprint remains within the arena, even beside a wall.
            float limit=ArenaHalfSize-EnemyRadius(enemy);
            if(Mathf.Abs(push.x)>.0001f) distance=Mathf.Min(distance,Mathf.Max(0,((push.x>0?limit:-limit)-enemy.position.x)/push.x));
            if(Mathf.Abs(push.z)>.0001f) distance=Mathf.Min(distance,Mathf.Max(0,((push.z>0?limit:-limit)-enemy.position.z)/push.z));
            if(Finite(distance) && !IsForestBoss(enemy)) enemy.position+=push*distance;
        }
    }

    private void ClearTorchState()
    {
        torchTime=0; torchHit.Clear(); swinging[7]=false; swingAge[7]=newCooldown[7]=0;
        torchSweepFade=.22f;
        if(torchSweepFire!=null) torchSweepFire.gameObject.SetActive(false);
        if(torchTrail!=null) torchTrail.enabled=false;
        if(newWeaponVisuals[7]!=null) newWeaponVisuals[7].gameObject.SetActive(false);
    }
}
