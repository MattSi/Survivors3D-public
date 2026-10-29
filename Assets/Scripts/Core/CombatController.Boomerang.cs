using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    // One reusable blade: held, outbound, returning, harmless recall if cover blocks the catch.
    protected internal int boomerangPhase;
    protected internal float boomerangAge,boomerangTravel,boomerangSpin;
    protected internal Vector3 boomerangPosition,boomerangDirection;
    protected internal readonly HashSet<Transform> boomerangHit=new HashSet<Transform>();
    private LineRenderer boomerangArc;
    private const float BoomerangRadius=.45f;
    private Vector3 BoomerangHand=>player.transform.position+player.transform.right*.65f+Vector3.up*.3f;

    protected internal void BuildBoomerang(Transform root)
    {
        var wood=HammerMaterial("Boomerang Warm Wood",new Color(.5f,.26f,.12f));
        var iron=HammerMaterial("Boomerang Dark Iron",new Color(.18f,.19f,.21f));
        var wrap=HammerMaterial("Boomerang Tan Wrap",new Color(.76f,.58f,.33f));
        var steel=HammerMaterial("Boomerang Steel",new Color(.61f,.65f,.69f));
        var edge=HammerMaterial("Boomerang Bright Bevel",new Color(.86f,.89f,.9f));
        HammerPart(root,"Wood Grip",Vector3.zero,new Vector3(.48f,.15f,.22f),.025f,wood);
        for(int sign=-1;sign<=1;sign+=2)
        {
            HammerPart(root,"Tan Grip Wrap",new Vector3(sign*.055f,0,0),new Vector3(.065f,.17f,.25f),.015f,wrap);
            Transform wing=new GameObject(sign<0?"Left Swept Wing":"Right Swept Wing").transform;
            wing.SetParent(root,false); wing.localPosition=new Vector3(sign*.22f,0,0);
            wing.localRotation=Quaternion.Euler(0,sign<0?202:-22,0);
            HammerPart(wing,"Iron Collar",Vector3.zero,new Vector3(.095f,.21f,.32f),.02f,iron);
            BuildBoomerangWing(wing,steel,edge);
        }
        boomerangArc=new GameObject("Boomerang Spin Streak").AddComponent<LineRenderer>();
        boomerangArc.transform.SetParent(root,false); boomerangArc.useWorldSpace=false;
        boomerangArc.positionCount=12; boomerangArc.startWidth=.035f; boomerangArc.endWidth=.005f;
        boomerangArc.sharedMaterial=HammerMaterial("Boomerang Ivory Streak",new Color(.83f,.79f,.62f),true);
        boomerangArc.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        boomerangArc.receiveShadows=false;
        for(int i=0;i<12;i++)
        {
            float angle=(25+i*8)*Mathf.Deg2Rad;
            boomerangArc.SetPosition(i,new Vector3(Mathf.Cos(angle)*.88f,.015f,Mathf.Sin(angle)*.88f));
        }
        boomerangArc.enabled=false;
    }

    private void BuildBoomerangWing(Transform root,Material steel,Material edge)
    {
        // Separate flat faces and broad cutting bevels, matching the approved clipped-tip blade.
        Vector2[] outline={new Vector2(0,-.14f),new Vector2(.49f,-.18f),new Vector2(.66f,0),new Vector2(.57f,.17f),new Vector2(0,.14f)};
        Vector2 center=new Vector2(.29f,0);
        var vertices=new List<Vector3>(); var faces=new List<int>(); var bevels=new List<int>();
        void Face(List<int> triangles,params Vector3[] points)
        {
            int first=vertices.Count; vertices.AddRange(points);
            for(int i=1;i<points.Length-1;i++) { triangles.Add(first); triangles.Add(first+i); triangles.Add(first+i+1); }
        }
        Vector3 Point(int i,float y,bool inset)
        {
            Vector2 p=inset?Vector2.Lerp(outline[i],center,.22f):outline[i];
            return new Vector3(p.x,y,p.y);
        }
        for(int i=0;i<outline.Length;i++)
        {
            int j=(i+1)%outline.Length;
            Face(faces,new Vector3(center.x,.065f,0),Point(j,.065f,true),Point(i,.065f,true));
            Face(faces,new Vector3(center.x,-.065f,0),Point(i,-.065f,true),Point(j,-.065f,true));
            Face(bevels,Point(i,.065f,true),Point(j,.065f,true),Point(j,0,false),Point(i,0,false));
            Face(bevels,Point(i,0,false),Point(j,0,false),Point(j,-.065f,true),Point(i,-.065f,true));
        }
        var mesh=new Mesh {name="Boomerang Broad Beveled Wing",subMeshCount=2};
        mesh.SetVertices(vertices); mesh.SetTriangles(faces,0); mesh.SetTriangles(bevels,1);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); hammerMeshes.Add(mesh);
        var blade=new GameObject("Silver Blade",typeof(MeshFilter),typeof(MeshRenderer)); blade.transform.SetParent(root,false);
        blade.GetComponent<MeshFilter>().sharedMesh=mesh;
        blade.GetComponent<MeshRenderer>().sharedMaterials=new[] {steel,edge};
    }

    protected internal void UpdateBoomerang(float dt,Transform visual)
    {
        if(!Finite(dt) || dt<=0) return;
        // Short swept steps make the curved return stable through slow frames and dash movement.
        for(float remaining=dt;remaining>0;)
        {
            float step=Mathf.Min(remaining,1f/60f); remaining-=step;
            if(boomerangPhase==0)
            {
                newCooldown[9]=Mathf.Max(0,newCooldown[9]-step);
                Vector3 hand=BoomerangHand;
                Transform target=FindItemTarget(hand,NewRange(9));
                if(newCooldown[9]>0 || target==null) continue;
                // Do not launch from a hand socket that clips into adjacent cover.
                if(obstacles!=null && !obstacles.Clear(player.transform.position,hand,BoomerangRadius)) continue;
                boomerangPosition=hand; boomerangDirection=target.position-hand; boomerangDirection.y=0;
                if(boomerangDirection.sqrMagnitude<.00001f) boomerangDirection=player.transform.forward;
                GameAudio.Play(AudioCue.Boomerang);
                boomerangDirection.Normalize(); boomerangPhase=1; boomerangAge=boomerangTravel=0; boomerangHit.Clear();
            }
            boomerangAge+=step; boomerangSpin=(boomerangSpin+1080*step)%360;
            if(boomerangPhase==3)
            {
                if(boomerangAge>=.16f) CatchBoomerang();
                continue;
            }
            Vector3 direction=boomerangDirection;
            float travel=12*step;
            if(boomerangPhase==1) travel=Mathf.Min(travel,Mathf.Max(0,NewRange(9)-boomerangTravel));
            else
            {
                Vector3 toHand=BoomerangHand-boomerangPosition; toHand.y=0;
                if(toHand.magnitude<.22f) { CatchBoomerang(); continue; }
                if(boomerangAge>=3) { RecallBoomerang(); continue; }
                // A gentle lateral bend retains two-pass hits; the catch point follows live player movement.
                Vector3 bend=Vector3.Cross(Vector3.up,boomerangDirection)*(.65f*Mathf.Sin(Mathf.PI*Mathf.Clamp01(boomerangAge/.65f)));
                direction=(toHand+bend).normalized; travel=Mathf.Min(17*step,toHand.magnitude);
            }
            float wall=BoomerangWallDistance(boomerangPosition,direction);
            bool blocked=travel>=wall;
            travel=Mathf.Min(travel,Mathf.Max(0,wall-.015f));
            HitBoomerangSegment(boomerangPosition,direction,travel);
            boomerangPosition+=direction*travel;
            if(boomerangPhase==1)
            {
                boomerangTravel+=travel;
                if(blocked || boomerangTravel>=NewRange(9)-.001f)
                { boomerangPhase=2; boomerangAge=0; boomerangHit.Clear(); }
            }
            else if(blocked) RecallBoomerang();
        }
        visual.localScale=Vector3.one*(boomerangPhase==3?Mathf.Clamp01(1-boomerangAge/.16f):1);
        visual.SetPositionAndRotation(boomerangPhase==0?BoomerangHand:boomerangPosition,
            boomerangPhase==0?player.transform.rotation*Quaternion.Euler(0,0,-18):Quaternion.Euler(0,boomerangSpin,0));
        if(boomerangArc!=null) boomerangArc.enabled=boomerangPhase==1 || boomerangPhase==2;
    }

    private float BoomerangWallDistance(Vector3 origin,Vector3 direction)
    {
        float limit=ArenaHalfSize-BoomerangRadius;
        // Starting at a boundary still permits inward travel on the return leg.
        float wall=float.PositiveInfinity;
        if(direction.x>.00001f) wall=Mathf.Min(wall,(limit-origin.x)/direction.x);
        if(direction.x<-.00001f) wall=Mathf.Min(wall,(-limit-origin.x)/direction.x);
        if(direction.z>.00001f) wall=Mathf.Min(wall,(limit-origin.z)/direction.z);
        if(direction.z<-.00001f) wall=Mathf.Min(wall,(-limit-origin.z)/direction.z);
        if(obstacles!=null) wall=Mathf.Min(wall,obstacles.HitDistance(origin,direction,BoomerangRadius));
        return Mathf.Max(0,wall);
    }

    private void HitBoomerangSegment(Vector3 origin,Vector3 direction,float travel)
    {
        if(travel<=0) return;
        var ray=new Ray(origin,direction);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i];
            if(boomerangHit.Contains(enemy)) continue;
            Bounds bounds=EnemyHitBounds(enemy,BoomerangRadius);
            float distance=0;
            if(!bounds.Contains(origin) && (!bounds.IntersectRay(ray,out distance) || distance>travel)) continue;
            Vector3 contact=origin+direction*distance;
            if(obstacles!=null && !obstacles.Clear(contact,enemy.position)) continue;
            boomerangHit.Add(enemy);
            DamageEnemy(i,equipment.Weapon(9).Damage(WeaponCatalog.Damage[9]),HitKind.Melee);
        }
    }

    private void RecallBoomerang() { boomerangPhase=3; boomerangAge=0; boomerangHit.Clear(); }
    private void CatchBoomerang()
    {
        boomerangPhase=0; boomerangAge=boomerangTravel=0; boomerangHit.Clear(); newCooldown[9]=NewInterval(9);
    }
    protected internal void ClearBoomerang()
    {
        boomerangPhase=0; boomerangAge=boomerangTravel=boomerangSpin=0; boomerangHit.Clear(); newCooldown[9]=0;
        if(boomerangArc!=null) boomerangArc.enabled=false;
        if(newWeaponVisuals[9]!=null) { newWeaponVisuals[9].localScale=Vector3.one; newWeaponVisuals[9].gameObject.SetActive(false); }
    }
}
