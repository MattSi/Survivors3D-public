using UnityEngine;

public partial class CombatController
{
    private const float BossRestDuration=8f;
    private float bossAssaultTimer,bossHordeTimer;
    private int bossHordeRemaining,bossHordeLimit,bossHordeOrdinal,bossOpenSide;
    private bool bossReturning;
    private Vector3 bossRetreatTarget;
    private float BossAssaultDuration=>bossEnraged?14f:18f;
    private static readonly Vector3[] BossArenaSides={Vector3.forward,Vector3.right,Vector3.back,Vector3.left};

    private void BeginBossRetreat()
    {
        HideBossWarning(); ClearBossSummonWarnings();
        bossComboCharges=0; bossComboActive=false; bossReturning=false;
        // Choose a reachable edge away from the player, favouring a short retreat.
        float best=float.NegativeInfinity;
        bossRetreatTarget=forestBoss.position;
        foreach(var side in BossArenaSides)
        {
            Vector3 at=side*15f; at.y=forestBoss.position.y;
            Vector3 route=at-forestBoss.position;
            if(obstacles!=null && obstacles.HitDistance(forestBoss.position,route.normalized,ForestBossRadius)<route.magnitude) continue;
            float score=(at-player.transform.position).magnitude-route.magnitude*.4f;
            if(score<=best) continue;
            best=score; bossRetreatTarget=at;
        }
        forestBossPhase=ForestBossPhase.Retreat; bossTimer=6f;
        BossAvatar.Face(bossRetreatTarget-forestBoss.position); BossAvatar.ChargePose(.3f,true); BossAvatar.BossPose(0,.3f);
        GameAudio.Play(AudioCue.Dash);
    }

    private void TickBossRetreat(float dt,float slow)
    {
        Vector3 delta=bossRetreatTarget-forestBoss.position;
        BossAvatar.Face(delta); BossAvatar.ChargePose(.3f,true); BossAvatar.BossPose(0,.3f);
        float clearance=BossClearance(forestBoss.position,delta.normalized);
        float travel=Mathf.Min(9f*slow*dt,delta.magnitude,clearance);
        forestBoss.position+=delta.normalized*travel;
        ClampBossPosition();
        if((bossRetreatTarget-forestBoss.position).sqrMagnitude<.04f || clearance<=.01f || bossTimer<=0) BeginBossRest();
    }

    private void BeginBossRest()
    {
        HideBossWarning(); ClearBossSummonWarnings();
        forestBossPhase=ForestBossPhase.Rest; bossTimer=BossRestDuration;
        bossHordeRemaining=bossEnraged?32:24; bossHordeLimit=bossEnraged?24:18; bossHordeOrdinal=0;
        // The nearest side stays open for this entire wave. Reinforcements use the other three.
        float nearest=float.PositiveInfinity;
        for(int i=0;i<BossArenaSides.Length;i++)
        {
            float gap=(BossArenaSides[i]*15.5f-player.transform.position).sqrMagnitude;
            if(gap<nearest) { nearest=gap; bossOpenSide=i; }
        }
        BossAvatar.Face(player.transform.position-forestBoss.position);
        BossAvatar.ChargePose(.25f,false); BossAvatar.BossPose(0,.15f);
        QueueBossHordeBatch(); GameAudio.Play(AudioCue.WaveStart);
    }

    private void QueueBossHordeBatch()
    {
        bossHordeTimer=1f;
        int count=Mathf.Min(bossEnraged?5:4,bossHordeRemaining,Mathf.Max(0,bossHordeLimit-(enemies.Count-1)));
        for(int attempt=0;attempt<54 && bossSummonWarnings.Count<count;attempt++)
        {
            int ordinal=bossHordeOrdinal++;
            int side=(bossOpenSide+1+ordinal%3)%4;
            Vector3 normal=BossArenaSides[side],tangent=Vector3.Cross(Vector3.up,normal);
            float lane=((ordinal/3)%9-4)*1.6f;
            Vector3 at=normal*15.5f+tangent*lane; at.y=.055f;
            if(!BossMinionPositionAvailable(at)) continue;
            bossSummonWarnings.Add(Ring("Stone King Horde Entrance",at,.7f,spawnMaterial,.09f).transform);
        }
    }

    private void TickBossRest(float dt)
    {
        BossAvatar.ChargePose(.25f,false); BossAvatar.BossPose(0,.15f);
        if(bossTimer<=0)
        {
            // End reinforcements before warning the return; a delayed frame cannot spill births into it.
            ClearBossSummonWarnings(); bossHordeRemaining=0;
            bossComboCharges=0; bossComboActive=false; bossReturning=true;
            bossAssaultTimer=BossAssaultDuration; bossSummonTimer=6f;
            BeginBossCharge(); bossTimer=bossWindup=1f;
            return;
        }
        if(bossHordeRemaining<=0) return;
        bossHordeTimer-=dt;
        if(bossHordeTimer>0) return;
        ReleaseBossMinions();
        // Reserve the next batch only if its full one-second warning fits before the return.
        if(bossHordeRemaining>0 && bossTimer>1f) QueueBossHordeBatch();
    }
}
