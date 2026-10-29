using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CombatController
{
    protected internal sealed class Bomber
    {
        public bool WindingUp,Reloading;
        public float Timer=1.5f;
        public Vector3 Target;
        public Transform Warning;
    }
    protected internal sealed class EnemyBomb
    {
        public Vector3 Origin,Target;
        public float Age;
        public Transform Visual,Warning;
    }
    protected internal readonly Dictionary<Transform,Bomber> bombers=new Dictionary<Transform,Bomber>();
    protected internal readonly List<EnemyBomb> enemyBombs=new List<EnemyBomb>();
    protected internal const float BombWindup=.65f,BombFlight=.9f,BombFuse=.75f,BombRadius=1.4f;
    private Material bombBodyMaterial,bombGlowMaterial,bombWarningMaterial;
    private Mesh bombWarningMesh;
    protected internal bool NextEnemyIsBomber=>NextEnemyKind==EnemyKind.Bomber;
    private bool BombSlotAvailable()
    {
        int count=enemyBombs.Count;
        foreach(var state in bombers.Values) if(state.WindingUp) count++;
        return count<session.Rules.AttackLimit(EnemyKind.Bomber,session.Wave);
    }
    protected internal bool TickBomber(Transform enemy,EnemyAvatar avatar,Vector3 target,float slow,float dt)
    {
        if(!bombers.TryGetValue(enemy,out Bomber state)) return false;
        Vector3 delta=target-enemy.position; delta.y=0;
        if(state.WindingUp)
        {
            state.Timer-=dt;
            avatar?.Face(state.Target-enemy.position);
            avatar?.ThrowPose(1-Mathf.Clamp01(state.Timer/BombWindup),true);
            if(state.Timer<=0)
            {
                EnsureBombMaterials();
                Vector3 origin=avatar!=null?avatar.BombHandPosition:enemy.position+Vector3.up*.6f;
                Transform ball=CreateVisual(PrimitiveType.Sphere,"Enemy Bomb",origin,Vector3.one*.25f,bombBodyMaterial);
                Transform fuse=CreateVisual(PrimitiveType.Cube,"Bomb Fuse",origin+Vector3.up*.16f,new Vector3(.04f,.09f,.04f),bombGlowMaterial);
                fuse.SetParent(ball,true);
                enemyBombs.Add(new EnemyBomb {Origin=origin,Target=state.Target,Visual=ball,Warning=state.Warning});
                state.Warning=null; state.WindingUp=false; state.Reloading=true; state.Timer=2.8f;
                avatar?.ThrowPose(1,false);
            }
            return true;
        }
        state.Timer-=dt;
        if(state.Timer<=.5f) state.Reloading=false;
        avatar?.ThrowPose(state.Reloading && state.Timer>2.5f?(state.Timer-2.5f)/.3f:0,!state.Reloading);
        float distance=delta.magnitude;
        if(state.Timer<=0 && distance>=4.5f && distance<=9f && BombSlotAvailable()
            && (obstacles==null || obstacles.Clear(enemy.position,target,.2f)))
        {
            state.WindingUp=true; state.Timer=BombWindup;
            state.Target=new Vector3(Mathf.Clamp(target.x,-ArenaHalfSize+BombRadius,ArenaHalfSize-BombRadius),.06f,
                Mathf.Clamp(target.z,-ArenaHalfSize+BombRadius,ArenaHalfSize-BombRadius));
            state.Warning=CreateBombWarning(state.Target);
            avatar?.ThrowPose(0,true);
            return true;
        }
        if(distance>7f)
        {
            float travel=WaveEnemySpeed*.8f*slow*dt;
            enemy.position=obstacles!=null?obstacles.Move(enemy.position,target,travel):Vector3.MoveTowards(enemy.position,target,travel);
        }
        else if(distance<4.5f)
        {
            // Pick a clear retreat step; never walk through a crate or outside the arena.
            Vector3 away=distance>.001f?-delta.normalized:-player.transform.forward;
            float best=0; Vector3 direction=Vector3.zero;
            foreach(float angle in new[] {0f,45f,-45f,90f,-90f})
            {
                Vector3 candidate=Quaternion.Euler(0,angle,0)*away;
                float travel=WaveEnemySpeed*.7f*slow*dt;
                if(obstacles!=null) travel=Mathf.Min(travel,Mathf.Max(0,obstacles.HitDistance(enemy.position,candidate,EnemySize/2)-.01f));
                Vector3 end=enemy.position+candidate*travel;
                if(Mathf.Abs(end.x)>ArenaHalfSize-EnemySize/2 || Mathf.Abs(end.z)>ArenaHalfSize-EnemySize/2) continue;
                float gain=(end-target).sqrMagnitude-delta.sqrMagnitude;
                if(gain>best) {best=gain; direction=candidate;}
            }
            if(direction.sqrMagnitude>0) PushEnemyApart(enemy,direction,WaveEnemySpeed*.7f*slow*dt);
        }
        return true;
    }
    protected internal void UpdateEnemyBombs(float dt)
    {
        if(!Finite(dt) || dt<=0) return;
        for(int i=enemyBombs.Count-1;i>=0;i--)
        {
            EnemyBomb bomb=enemyBombs[i]; bomb.Age+=dt;
            float flight=Mathf.Clamp01(bomb.Age/BombFlight);
            bomb.Visual.position=Vector3.Lerp(bomb.Origin,bomb.Target+Vector3.up*.12f,flight)+Vector3.up*(4*flight*(1-flight)*2.2f);
            bomb.Visual.Rotate(Vector3.up,dt*160,Space.World);
            if(bomb.Age>=BombFlight)
            {
                float pulse=(1+Mathf.Sin((bomb.Age-BombFlight)*30))*.5f;
                bomb.Warning.GetComponent<LineRenderer>().startWidth=bomb.Warning.GetComponent<LineRenderer>().endWidth=.075f+pulse*.04f;
            }
            if(bomb.Age<BombFlight+BombFuse) continue;
            GameAudio.Play(AudioCue.Explosion);
            Vector3 at=bomb.Target;
            RemoveEnemyBomb(bomb); enemyBombs.RemoveAt(i);
            var effect=new Impact {Ring=Ring("Enemy Bomb Explosion",at,BombRadius,bombGlowMaterial,.12f),Duration=.35f,
                Fragments=new Transform[6],Velocities=new Vector3[6],FragmentSize=.13f};
            for(int n=0;n<6;n++)
            {
                float angle=n*Mathf.PI/3;
                effect.Fragments[n]=CreateVisual(PrimitiveType.Cube,"Bomb Spark",at,Vector3.one*.13f,bombGlowMaterial);
                effect.Velocities[n]=new Vector3(Mathf.Cos(angle)*2,2,Mathf.Sin(angle)*2);
            }
            impacts.Add(effect);
            Vector3 delta=player.transform.position-at; delta.y=0;
            if(delta.sqrMagnitude<=BombRadius*BombRadius && (obstacles==null || obstacles.Clear(at,player.transform.position)))
            {
                health.TakeDamage(15);
                if(health.IsDead) return; // Defeat clears the remaining bombs through the game flow.
            }
        }
    }
    protected internal void InterruptBomber(Transform enemy)
    {
        if(!bombers.TryGetValue(enemy,out Bomber state)) return;
        HideBomberWarning(state); state.WindingUp=false; state.Reloading=false; state.Timer=1.5f;
        enemy.GetComponent<EnemyAvatar>()?.ThrowPose(0,true);
    }
    private static void HideBomberWarning(Bomber state)
    {
        if(state.Warning!=null) {state.Warning.gameObject.SetActive(false); Destroy(state.Warning.gameObject); state.Warning=null;}
    }
    private void RemoveBomber(Transform enemy)
    {
        if(!bombers.TryGetValue(enemy,out Bomber state)) return;
        HideBomberWarning(state); enemy.GetComponent<EnemyAvatar>()?.ThrowPose(0,false); bombers.Remove(enemy);
    }
    private static void RemoveEnemyBomb(EnemyBomb bomb)
    {
        if(bomb.Visual!=null) {bomb.Visual.gameObject.SetActive(false); Destroy(bomb.Visual.gameObject);}
        if(bomb.Warning!=null) {bomb.Warning.gameObject.SetActive(false); Destroy(bomb.Warning.gameObject);}
    }
    private void ClearBombers()
    {
        foreach(var pair in bombers)
        {
            HideBomberWarning(pair.Value);
            if(pair.Key!=null) pair.Key.GetComponent<EnemyAvatar>()?.ThrowPose(0,false);
        }
        bombers.Clear();
        foreach(var bomb in enemyBombs) RemoveEnemyBomb(bomb);
        enemyBombs.Clear();
    }
    private void EnsureBombMaterials()
    {
        if(bombBodyMaterial!=null) return;
        bombBodyMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name="Enemy Bomb Shell",color=new Color(.07f,.065f,.07f)};
        bombBodyMaterial.SetFloat("_Smoothness",.08f);
        bombGlowMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")) {name="Enemy Bomb Amber",color=new Color(1,.43f,.05f)};
        bombWarningMaterial=new Material(bombGlowMaterial) {name="Enemy Bomb Area"};
        bombWarningMaterial.SetColor("_BaseColor",new Color(1,.34f,.035f,.18f));
        bombWarningMaterial.SetFloat("_Surface",1);
        bombWarningMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
        bombWarningMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
        bombWarningMaterial.SetInt("_ZWrite",0); bombWarningMaterial.SetInt("_Cull",(int)CullMode.Off);
        bombWarningMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); bombWarningMaterial.renderQueue=(int)RenderQueue.Transparent;
        var vertices=new Vector3[41]; var triangles=new int[120];
        for(int i=0;i<40;i++)
        {
            float angle=i*Mathf.PI/20; vertices[i+1]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*BombRadius;
            triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=(i+1)%40+1;
        }
        bombWarningMesh=new Mesh {name="Bomb Warning Disk"}; bombWarningMesh.vertices=vertices; bombWarningMesh.triangles=triangles; bombWarningMesh.RecalculateBounds();
    }
    private Transform CreateBombWarning(Vector3 at)
    {
        EnsureBombMaterials();
        Transform warning=Ring("Enemy Bomb Warning",at,BombRadius,bombGlowMaterial,.075f).transform;
        var disk=new GameObject("Bomb Area Fill"); disk.transform.SetParent(warning,false);
        disk.AddComponent<MeshFilter>().sharedMesh=bombWarningMesh;
        var renderer=disk.AddComponent<MeshRenderer>(); renderer.sharedMaterial=bombWarningMaterial;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        return warning;
    }
    private void DisposeBombers()
    {
        ClearBombers();
        if(bombBodyMaterial!=null) Destroy(bombBodyMaterial);
        if(bombGlowMaterial!=null) Destroy(bombGlowMaterial);
        if(bombWarningMaterial!=null) Destroy(bombWarningMaterial);
        if(bombWarningMesh!=null) Destroy(bombWarningMesh);
    }
}
