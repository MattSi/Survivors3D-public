using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CombatController
{
    protected internal sealed class Shooter
    {
        public bool WindingUp,Locked;
        public float Timer=1.5f,RetreatCycle;
        public Vector3 Direction;
        public LineRenderer Warning;
    }
    protected internal sealed class EnemyNeedle
    {
        public Transform Visual;
        public Vector3 Direction;
        public float Remaining=NeedleRange,Speed=NeedleSpeed;
        public int Damage=8;
    }
    protected internal readonly Dictionary<Transform,Shooter> shooters=new Dictionary<Transform,Shooter>();
    protected internal readonly List<EnemyNeedle> enemyNeedles=new List<EnemyNeedle>();
    protected internal const float ShooterWindup=.8f,ShooterLockTime=.3f,NeedleSpeed=8.5f,NeedleRange=12f,NeedleRadius=.12f;
    private Material needleMaterial,needleGlowMaterial;
    private Mesh needleMesh;
    private static readonly float[] ShooterRetreatAngles={0,45,-45,90,-90};

    private bool ShooterSlotAvailable()
    {
        int attacks=enemyNeedles.Count;
        foreach(var state in shooters.Values) if(state.WindingUp) attacks++;
        return attacks<session.Rules.AttackLimit(EnemyKind.Shooter,session.Wave);
    }
    private bool ShooterVisible(Transform enemy)
    {
        Camera camera=viewCamera!=null?viewCamera.GetComponent<Camera>():Camera.main;
        if(camera==null) return false;
        Vector3 point=camera.WorldToViewportPoint(enemy.position+Vector3.up*.35f);
        return point.z>camera.nearClipPlane && point.z<camera.farClipPlane
            && point.x>.06f && point.x<.94f && point.y>.06f && point.y<.94f;
    }
    private Vector3 ShooterMuzzle(Transform enemy,EnemyAvatar avatar,Vector3 direction)=>
        avatar!=null?avatar.ShooterMuzzlePosition:enemy.position+Vector3.up*.25f+direction*.45f;

    protected internal bool TickShooter(Transform enemy,EnemyAvatar avatar,Vector3 target,float slow,float dt)
    {
        if(!shooters.TryGetValue(enemy,out Shooter state)) return false;
        Vector3 delta=target-enemy.position; delta.y=0;
        float distance=delta.magnitude;
        bool clear=obstacles==null || obstacles.Clear(enemy.position,target,NeedleRadius);
        if(state.WindingUp)
        {
            if(!ShooterVisible(enemy)) {InterruptShooter(enemy); return true;}
            if(!state.Locked)
            {
                avatar?.Face(delta);
                avatar?.ShootPose(1,delta.normalized);
                Vector3 aim=target-ShooterMuzzle(enemy,avatar,delta.normalized); aim.y=0;
                state.Direction=aim.normalized;
                state.Timer-=dt;
                if(state.Timer<=ShooterLockTime)
                {
                    // Even a long frame must leave a full, visibly committed dodge window.
                    state.Locked=true; state.Timer=ShooterLockTime;
                }
            }
            else state.Timer-=dt;
            avatar?.Face(state.Direction);
            avatar?.ShootPose(Mathf.Clamp01((ShooterWindup-state.Timer)/.5f),state.Direction);
            UpdateShooterWarning(enemy,avatar,state);
            if(state.Timer<=0)
            {
                Vector3 muzzle=ShooterMuzzle(enemy,avatar,state.Direction);
                // Cover between the body and its offset muzzle must not allow a shot through a wall.
                if(clear && (obstacles==null || obstacles.Clear(enemy.position,muzzle,NeedleRadius)))
                    ReleaseNeedle(muzzle,state.Direction);
                HideShooterWarning(state);
                state.WindingUp=false; state.Locked=false; state.Timer=Random.Range(2f,2.4f);
                avatar?.ShootPose(0,state.Direction);
            }
            return true;
        }
        state.Timer-=dt;
        avatar?.ShootPose(0,delta.normalized);
        if(state.Timer<=0 && distance>=4.5f && distance<=9 && clear && ShooterVisible(enemy) && ShooterSlotAvailable())
        {
            state.WindingUp=true; state.Locked=false; state.Timer=ShooterWindup; state.Direction=delta.normalized;
            EnsureNeedleAssets();
            var warning=new GameObject("Bone Needle Aim"); warning.transform.SetParent(transform,false);
            state.Warning=warning.AddComponent<LineRenderer>();
            state.Warning.positionCount=2; state.Warning.sharedMaterial=needleGlowMaterial;
            state.Warning.startWidth=state.Warning.endWidth=.025f;
            state.Warning.shadowCastingMode=ShadowCastingMode.Off; state.Warning.receiveShadows=false;
            UpdateShooterWarning(enemy,avatar,state);
            return true;
        }
        // Pause briefly after each shot so melee builds can close the gap.
        if(state.Timer>1.8f) return true;
        if(distance>8.5f || (!clear && distance>=4.5f))
        {
            float travel=WaveEnemySpeed*.7f*slow*dt;
            enemy.position=obstacles!=null?obstacles.Move(enemy.position,target,travel):Vector3.MoveTowards(enemy.position,target,travel);
        }
        else if(distance<4.5f)
        {
            // A short retreat, then a long pause; never kite the player indefinitely.
            float available=Mathf.Max(0,.6f-state.RetreatCycle);
            state.RetreatCycle=Mathf.Repeat(state.RetreatCycle+dt,2.4f);
            float travel=WaveEnemySpeed*.55f*slow*Mathf.Min(dt,available);
            Vector3 away=distance>.001f?-delta.normalized:-player.transform.forward;
            float best=0; Vector3 step=Vector3.zero;
            foreach(float angle in ShooterRetreatAngles)
            {
                Vector3 direction=Quaternion.Euler(0,angle,0)*away;
                float move=Mathf.Min(travel,Mathf.Max(0,DistanceToBorder(enemy.position,direction)-EnemySize/2));
                if(obstacles!=null) move=Mathf.Min(move,Mathf.Max(0,obstacles.HitDistance(enemy.position,direction,EnemySize/2)-.01f));
                Vector3 end=enemy.position+direction*move;
                float gain=(end-target).sqrMagnitude-(enemy.position-target).sqrMagnitude;
                if(gain>best) {best=gain; step=direction*move;}
            }
            enemy.position+=step;
        }
        else state.RetreatCycle=0;
        return true;
    }
    private void UpdateShooterWarning(Transform enemy,EnemyAvatar avatar,Shooter state)
    {
        if(state.Warning==null) return;
        Vector3 muzzle=ShooterMuzzle(enemy,avatar,state.Direction);
        float length=Mathf.Min(NeedleRange,DistanceToBorder(muzzle,state.Direction));
        if(obstacles!=null) length=Mathf.Min(length,obstacles.HitDistance(muzzle,state.Direction,NeedleRadius));
        Vector3 start=new Vector3(muzzle.x,.055f,muzzle.z);
        state.Warning.SetPosition(0,start);
        state.Warning.SetPosition(1,start+state.Direction*Mathf.Max(0,length));
        state.Warning.startWidth=state.Warning.endWidth=state.Locked?.045f:.02f;
    }
    private void ReleaseNeedle(Vector3 muzzle,Vector3 direction)
    {
        EnsureNeedleAssets();
        var go=new GameObject("Enemy Bone Needle",typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(transform,false); go.transform.SetPositionAndRotation(muzzle,Quaternion.LookRotation(direction));
        go.transform.localScale=Vector3.one*1.25f;
        go.GetComponent<MeshFilter>().sharedMesh=needleMesh;
        var renderer=go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial=needleMaterial;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        // A small local light keeps the glow visible even when camera bloom is disabled.
        var glow=go.AddComponent<Light>();
        glow.type=LightType.Point; glow.color=new Color(1,.48f,.12f);
        glow.range=1.4f; glow.intensity=1.2f; glow.shadows=LightShadows.None;
        var trail=go.AddComponent<TrailRenderer>();
        trail.sharedMaterial=needleGlowMaterial; trail.time=.1f; trail.minVertexDistance=.035f;
        trail.startWidth=.15f; trail.endWidth=0;
        trail.shadowCastingMode=ShadowCastingMode.Off; trail.receiveShadows=false;
        enemyNeedles.Add(new EnemyNeedle {Visual=go.transform,Direction=direction});
    }
    protected internal void UpdateEnemyNeedles(float dt)
    {
        if(!Finite(dt) || dt<=0) return;
        for(int i=enemyNeedles.Count-1;i>=0;i--)
        {
            EnemyNeedle needle=enemyNeedles[i];
            Vector3 origin=needle.Visual.position;
            float travel=Mathf.Min(needle.Speed*dt,needle.Remaining);
            float wall=DistanceToBorder(origin,needle.Direction);
            if(obstacles!=null) wall=Mathf.Min(wall,obstacles.HitDistance(origin,needle.Direction,NeedleRadius));
            // Hostile needles ignore every enemy: only the player and arena cover stop them.
            // Sweep in the player's local horizontal plane, matching the existing compact body hitbox.
            Vector3 offset=origin-player.transform.position;
            var local=new Vector3(Vector3.Dot(offset,player.transform.right),0,Vector3.Dot(offset,player.transform.forward));
            var direction=new Vector3(Vector3.Dot(needle.Direction,player.transform.right),0,Vector3.Dot(needle.Direction,player.transform.forward));
            var bounds=new Bounds(Vector3.zero,new Vector3(2*(playerContactHalfSize+NeedleRadius),2,2*(playerContactHalfSize+NeedleRadius)));
            float hit=float.PositiveInfinity;
            if(bounds.Contains(local)) hit=0;
            else if(bounds.IntersectRay(new Ray(local,direction),out float entry)) hit=entry;
            bool hitPlayer=hit<=travel && hit<wall;
            float move=Mathf.Max(0,Mathf.Min(travel,wall,hit));
            needle.Visual.position=origin+needle.Direction*move; needle.Remaining-=move;
            if(!hitPlayer && wall>travel && needle.Remaining>.001f) continue;
            RemoveNeedle(needle); enemyNeedles.RemoveAt(i);
            // Remove before damage: lethal damage synchronously clears all remaining enemy projectiles.
            if(hitPlayer) {health.TakeDamage(needle.Damage); if(health.IsDead) return;}
        }
    }
    protected internal void InterruptShooter(Transform enemy)
    {
        if(!shooters.TryGetValue(enemy,out Shooter state)) return;
        HideShooterWarning(state); state.WindingUp=false; state.Locked=false; state.Timer=1.5f;
        enemy.GetComponent<EnemyAvatar>()?.ShootPose(0,state.Direction);
    }
    private static void HideShooterWarning(Shooter state)
    {
        if(state.Warning!=null) {state.Warning.gameObject.SetActive(false); Destroy(state.Warning.gameObject); state.Warning=null;}
    }
    private void RemoveShooter(Transform enemy)
    {
        InterruptShooter(enemy); shooters.Remove(enemy);
    }
    private static void RemoveNeedle(EnemyNeedle needle)
    {
        if(needle.Visual!=null) {needle.Visual.gameObject.SetActive(false); Destroy(needle.Visual.gameObject);}
    }
    private void ClearShooters()
    {
        foreach(var pair in shooters)
        {
            HideShooterWarning(pair.Value);
            if(pair.Key!=null) pair.Key.GetComponent<EnemyAvatar>()?.ShootPose(0,pair.Value.Direction);
        }
        shooters.Clear();
        foreach(var needle in enemyNeedles) RemoveNeedle(needle);
        enemyNeedles.Clear();
    }
    private void EnsureNeedleAssets()
    {
        if(needleMaterial!=null) return;
        needleMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name="Enemy Needle Bone",color=new Color(.88f,.84f,.68f)};
        needleMaterial.SetFloat("_Smoothness",.05f);
        needleMaterial.EnableKeyword("_EMISSION");
        needleMaterial.SetColor("_EmissionColor",new Color(1,.48f,.12f)*1.6f);
        needleGlowMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")) {name="Enemy Needle Amber",color=new Color(1,.40f,.055f)};
        // Flat triangular faces form a short square shaft and pointed bone tip along local +Z.
        var vertices=new List<Vector3>(); var indices=new List<int>();
        Vector3[] rear={new Vector3(-.08f,-.08f,-.25f),new Vector3(.08f,-.08f,-.25f),new Vector3(.08f,.08f,-.25f),new Vector3(-.08f,.08f,-.25f)};
        void Triangle(Vector3 a,Vector3 b,Vector3 c) {int n=vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); indices.Add(n); indices.Add(n+1); indices.Add(n+2);}
        for(int n=0;n<4;n++)
        {
            Vector3 a=rear[n],b=rear[(n+1)%4],c=b+Vector3.forward*.3f,d=a+Vector3.forward*.3f;
            Triangle(a,b,c); Triangle(a,c,d); Triangle(d,c,Vector3.forward*.23f);
        }
        Triangle(rear[2],rear[1],rear[0]); Triangle(rear[3],rear[2],rear[0]);
        needleMesh=new Mesh {name="Bone Needle"}; needleMesh.SetVertices(vertices); needleMesh.SetTriangles(indices,0);
        needleMesh.RecalculateNormals(); needleMesh.RecalculateBounds();
    }
    private void DisposeShooters()
    {
        ClearShooters();
        if(needleMaterial!=null) Destroy(needleMaterial);
        if(needleGlowMaterial!=null) Destroy(needleGlowMaterial);
        if(needleMesh!=null) Destroy(needleMesh);
    }
}
