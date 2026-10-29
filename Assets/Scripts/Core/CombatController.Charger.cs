using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CombatController
{
    protected internal enum ChargePhase { Chase, Windup, Dash, Recover }
    protected internal sealed class Charger
    {
        public ChargePhase Phase;
        public float Timer=1.5f, Remaining, DustTimer;
        public Vector3 Direction;
        public bool HitPlayer;
        public Transform Warning;
    }
    protected internal readonly Dictionary<Transform,Charger> chargers=new Dictionary<Transform,Charger>();
    private int waveSpawnCount;
    private Material chargeWarningMaterial;
    private Mesh chargeWarningMesh;
    private const float ChargeWindup=.9f, ChargeRecovery=1.1f, ChargeSpeed=10f;

    // Replace regular arrivals: no extra spawn budget, and at most four chargers alive late-game.
    protected internal bool NextEnemyIsCharger => NextEnemyKind==EnemyKind.Charger;

    private bool ChargeLaneAvailable()
    {
        int committed=0;
        foreach(var state in chargers.Values)
            if(state.Phase==ChargePhase.Windup || state.Phase==ChargePhase.Dash) committed++;
        return committed<session.Rules.AttackLimit(EnemyKind.Charger,session.Wave);
    }

    protected internal bool TickCharger(Transform enemy,EnemyAvatar avatar,Vector3 target,float slow,float dt)
    {
        if(!chargers.TryGetValue(enemy,out Charger state)) return false;
        Vector3 delta=target-enemy.position; delta.y=0;
        if(state.Phase==ChargePhase.Chase)
        {
            state.Timer-=dt;
            if(state.Timer>0 || delta.magnitude<2.5f || delta.magnitude>7.5f || !ChargeLaneAvailable()
                || (obstacles!=null && !obstacles.Clear(enemy.position,target,EnemySize/2))) return false;
            state.Phase=ChargePhase.Windup; state.Timer=ChargeWindup;
            state.Direction=delta.normalized; state.HitPlayer=false;
            state.Remaining=Mathf.Min(9,delta.magnitude+2);
            state.Warning=CreateChargeWarning(enemy.position,state.Direction,Mathf.Min(state.Remaining,ChargeClearance(enemy.position,state.Direction)));
        }
        avatar?.Face(state.Direction);
        if(state.Phase==ChargePhase.Windup)
        {
            state.Timer-=dt;
            float progress=1-Mathf.Clamp01(state.Timer/ChargeWindup);
            avatar?.ChargePose(.2f+progress*.8f,false);
            if(state.Timer<=0)
            {
                HideChargeWarning(state);
                state.Phase=ChargePhase.Dash; state.Timer=1.6f; state.DustTimer=0;
            }
            return true;
        }
        if(state.Phase==ChargePhase.Recover)
        {
            state.Timer-=dt; avatar?.ChargePose(0,false);
            if(state.Timer<=0) { state.Phase=ChargePhase.Chase; state.Timer=2f; }
            return true;
        }
        avatar?.ChargePose(1,true);
        Vector3 origin=enemy.position;
        float clearance=ChargeClearance(origin,state.Direction);
        float travel=Mathf.Min(ChargeSpeed*slow*dt,state.Remaining,clearance);
        enemy.position=origin+state.Direction*travel;
        state.Remaining-=travel; state.Timer-=dt;
        // Sweep the entire movement segment so a fast dash cannot skip the player on a long frame.
        if(!state.HitPlayer && ChargeCrossesPlayer(origin,state.Direction,travel,target))
        {
            state.HitPlayer=true; health.TakeDamage(15);
            if(health.IsDead) return true;
        }
        state.DustTimer-=dt;
        if(state.DustTimer<=0)
        {
            state.DustTimer=.12f;
            Transform puff=CreateVisual(PrimitiveType.Cube,"Charge Dust",enemy.position-state.Direction*.35f+Vector3.down*.22f,
                Vector3.one*.14f,enemyMaterial);
            ash.Add(puff.gameObject); Destroy(puff.gameObject,.25f);
        }
        if(clearance<=travel+.01f || state.Remaining<=.01f || state.Timer<=0) RecoverCharger(enemy,state);
        return true;
    }

    private float ChargeClearance(Vector3 origin,Vector3 direction)
    {
        float limit=ArenaHalfSize-EnemySize/2, distance=float.PositiveInfinity;
        if(direction.x>0) distance=Mathf.Min(distance,(limit-origin.x)/direction.x);
        else if(direction.x<0) distance=Mathf.Min(distance,(-limit-origin.x)/direction.x);
        if(direction.z>0) distance=Mathf.Min(distance,(limit-origin.z)/direction.z);
        else if(direction.z<0) distance=Mathf.Min(distance,(-limit-origin.z)/direction.z);
        if(obstacles!=null) distance=Mathf.Min(distance,obstacles.HitDistance(origin,direction,EnemySize/2));
        foreach(Transform stone in stoneArmors)
        {
            Bounds body=EnemyHitBounds(stone,EnemySize/2);
            if(body.Contains(origin)) return 0;
            if(body.IntersectRay(new Ray(origin,direction),out float hit)) distance=Mathf.Min(distance,hit);
        }
        return Mathf.Max(0,distance-.01f);
    }

    private bool ChargeCrossesPlayer(Vector3 origin,Vector3 direction,float travel,Vector3 target)
    {
        Vector3 offset=origin-target;
        Vector3 local=new Vector3(Vector3.Dot(offset,player.transform.right),0,Vector3.Dot(offset,player.transform.forward));
        Vector3 rayDirection=new Vector3(Vector3.Dot(direction,player.transform.right),0,Vector3.Dot(direction,player.transform.forward));
        var body=new Bounds(Vector3.zero,new Vector3(2*(playerContactHalfSize+EnemySize/2),2,2*(playerContactHalfSize+EnemySize/2)));
        return body.Contains(local) || (body.IntersectRay(new Ray(local,rayDirection),out float hit) && hit<=travel);
    }

    protected internal void InterruptCharger(Transform enemy)
    {
        if(chargers.TryGetValue(enemy,out Charger state)) RecoverCharger(enemy,state);
    }
    private void RecoverCharger(Transform enemy,Charger state)
    {
        state.Phase=ChargePhase.Recover; state.Timer=ChargeRecovery;
        HideChargeWarning(state); enemy.GetComponent<EnemyAvatar>()?.ChargePose(0,false);
    }
    private static void HideChargeWarning(Charger state)
    {
        if(state.Warning!=null) { state.Warning.gameObject.SetActive(false); Destroy(state.Warning.gameObject); state.Warning=null; }
    }
    private bool ChargerCommitted(Transform enemy) => chargers.TryGetValue(enemy,out Charger state) && state.Phase!=ChargePhase.Chase;
    private void RemoveCharger(Transform enemy)
    {
        if(chargers.TryGetValue(enemy,out Charger state)) HideChargeWarning(state);
        enemy.GetComponent<EnemyAvatar>()?.ChargePose(0,false);
        chargers.Remove(enemy);
    }
    private void ClearChargers()
    {
        foreach(var pair in chargers) { HideChargeWarning(pair.Value); if(pair.Key!=null) pair.Key.GetComponent<EnemyAvatar>()?.ChargePose(0,false); }
        chargers.Clear();
    }

    private Transform CreateChargeWarning(Vector3 origin,Vector3 direction,float length)
    {
        if(chargeWarningMaterial==null)
        {
            chargeWarningMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name="Charger Telegraph" };
            chargeWarningMaterial.SetColor("_BaseColor",new Color(1,.38f,.04f,.38f));
            chargeWarningMaterial.SetFloat("_Surface",1);
            chargeWarningMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            chargeWarningMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            chargeWarningMaterial.SetInt("_ZWrite",0);
            chargeWarningMaterial.SetInt("_Cull",(int)CullMode.Off);
            chargeWarningMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            chargeWarningMaterial.renderQueue=(int)RenderQueue.Transparent;
            chargeWarningMesh=new Mesh { name="Charge Direction Arrow" };
            chargeWarningMesh.vertices=new[] {new Vector3(-.38f,0,0),new Vector3(.38f,0,0),new Vector3(-.38f,0,.82f),
                new Vector3(.38f,0,.82f),new Vector3(-.65f,0,.82f),new Vector3(.65f,0,.82f),new Vector3(0,0,1)};
            chargeWarningMesh.triangles=new[] {0,2,1,1,2,3,4,6,5}; chargeWarningMesh.RecalculateBounds();
        }
        var go=new GameObject("Charger Ground Warning");
        go.transform.SetParent(transform,false);
        go.transform.SetPositionAndRotation(new Vector3(origin.x,.045f,origin.z),Quaternion.LookRotation(direction));
        go.transform.localScale=new Vector3(1,1,length);
        go.AddComponent<MeshFilter>().sharedMesh=chargeWarningMesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=chargeWarningMaterial;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        return go.transform;
    }
}
