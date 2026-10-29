using UnityEngine;

public partial class CombatController
{
    protected internal const float ForestBossHeight=3.2f,ForestBossRadius=1.05f;
    protected internal const float ForestBossRifleDamageMultiplier=.5f;
    protected internal enum ForestBossPhase { Arrival, Chase, Slam, ChargeWindup, Charge, Volley, Recover, Enrage, Summon, Retreat, Rest }
    private enum BossVolleyMode { Fan, Predictive }
    private const float BossVolleyLockTime=.35f,BossFollowupWindup=.6f;
    private const int BossMinionLimit=6;
    private const float BossNeedleSpeed=12f,BossNeedleRange=26f;
    protected internal Transform forestBoss;
    protected internal ForestBossPhase forestBossPhase;
    protected internal float bossTimer,bossWindup,bossTravel,bossStagger;
    protected internal bool bossEnraged;
    private bool bossChargeHit;
    private int bossAttackIndex,bossVolleyShots,bossMaxHealth;
    private Vector3 bossAim,bossSlamCenter,bossDeathPosition;
    private Transform bossWarning;
    private BossVolleyMode bossVolleyMode;
    private int bossVolleyIndex;
    private bool bossVolleyLocked,bossFollowupSlam,bossEnrageSummon;
    private bool bossComboActive;
    private int bossComboCharges;
    private float bossSummonTimer;
    private Vector3 bossPreviousPlayerPosition,bossPlayerVelocity;
    private readonly System.Collections.Generic.List<Transform> bossSummonWarnings=new System.Collections.Generic.List<Transform>();
    internal bool BossDefeated { get; private set; }
    private bool IsForestBoss(Transform enemy)=>enemy!=null && enemy==forestBoss;
    private int ForestBossHealth=>forestBoss!=null && enemyHealth.TryGetValue(forestBoss,out int hp)?Mathf.Max(0,hp):0;
    private EnemyAvatar BossAvatar=>forestBoss!=null?forestBoss.GetComponent<EnemyAvatar>():null;
    private float BossSlamRadius=>bossEnraged?3.4f:3f;

    private void StartForestBoss()
    {
        ClearForestBoss();
        obstacles?.PrepareBossArena();
        foreach(var portal in portalVisuals) if(portal!=null) portal.SetActive(false);
        var controller=player.GetComponent<CharacterController>(); controller.enabled=false;
        player.transform.position=new Vector3(0,.5f,-5); controller.enabled=true;
        forestBoss=CreateVisual(PrimitiveType.Cube,"Forest Stone King",new Vector3(0,EnemySize/2,5),Vector3.one*EnemySize,enemyMaterial);
        enemies.Add(forestBoss);
        bossMaxHealth=Mathf.Clamp(session.Rules.BossHealth,100,1000000);
        enemyHealth.Add(forestBoss,bossMaxHealth);
        forestBoss.gameObject.AddComponent<EnemyAvatar>().Initialize(Resources.Load<GameObject>("Characters/ForestStoneKing"),enemyModelMaterials,ForestBossHeight,true);
        var body=forestBoss.gameObject.AddComponent<CapsuleCollider>();
        body.radius=ForestBossRadius/EnemySize; body.height=ForestBossHeight/EnemySize;
        body.center=Vector3.up*((ForestBossHeight/2-EnemySize/2)/EnemySize);
        forestBossPhase=ForestBossPhase.Arrival; bossTimer=2f;
        bossAssaultTimer=BossAssaultDuration;
        bossSummonTimer=6f; bossPreviousPlayerPosition=player.transform.position;
        bossAim=Vector3.back; BossAvatar.Face(bossAim); BossAvatar.Emerge(1.4f);
        Physics.SyncTransforms();
    }

    protected internal void TickForestBoss(float dt)
    {
        if(!Finite(dt) || dt<=0 || forestBoss==null || BossDefeated || health.IsDead) return;
        Vector3 delta=player.transform.position-forestBoss.position; delta.y=0;
        Vector3 movement=player.transform.position-bossPreviousPlayerPosition; movement.y=0;
        bossPlayerVelocity=Vector3.ClampMagnitude(movement/dt,7f);
        bossPreviousPlayerPosition=player.transform.position;
        bossSummonTimer-=dt;
        if(!bossReturning && forestBossPhase!=ForestBossPhase.Arrival && forestBossPhase!=ForestBossPhase.Enrage
            && forestBossPhase!=ForestBossPhase.Retreat && forestBossPhase!=ForestBossPhase.Rest) bossAssaultTimer-=dt;
        // Enrage happens once between moves, never replacing an already committed attack.
        if(!bossEnraged && ForestBossHealth<=bossMaxHealth/2
            && forestBossPhase==ForestBossPhase.Chase)
        {
            bossEnraged=true; bossEnrageSummon=true; forestBossPhase=ForestBossPhase.Enrage; bossTimer=1.5f;
            bossAssaultTimer=Mathf.Min(bossAssaultTimer,BossAssaultDuration);
            HideBossWarning(); GameAudio.Play(AudioCue.WaveStart);
            BossAvatar.BossPose(.25f,1);
            return; // Preserve the full awakening cue and the preceding recovery window.
        }
        float slow=chilled.TryGetValue(forestBoss,out float until) && Time.time<until?1-FrostSlow:1;
        bossTimer-=dt;
        switch(forestBossPhase)
        {
            case ForestBossPhase.Arrival:
            case ForestBossPhase.Enrage:
                BossAvatar.BossPose(.25f,bossEnraged?1:0);
                if(bossTimer<=0)
                {
                    if(bossEnrageSummon) BeginBossSummon(4);
                    else { forestBossPhase=ForestBossPhase.Chase; bossTimer=.25f; }
                }
                return;
            case ForestBossPhase.Chase:
                if(bossAssaultTimer<=0) { BeginBossRetreat(); break; }
                if(bossSummonTimer<=0 && enemies.Count-1<BossMinionLimit) { BeginBossSummon(3); break; }
                BossAvatar.Face(delta); BossAvatar.BossPose(0,bossEnraged?.55f:0);
                if(delta.magnitude>2.2f)
                {
                    Vector3 step=delta.normalized*Mathf.Min((bossEnraged?4.6f:3.6f)*slow*dt,delta.magnitude-2.2f);
                    forestBoss.position+=step; ClampBossPosition();
                }
                if(bossTimer<=0) BeginBossAttack(player.transform.position-forestBoss.position);
                break;
            case ForestBossPhase.Slam:
                BossAvatar.Face(bossAim);
                BossAvatar.BossPose(Mathf.Lerp(.2f,1,1-Mathf.Clamp01(bossTimer/bossWindup)),1);
                if(bossTimer<=0)
                {
                    HideBossWarning(); BossAvatar.BossPose(0,bossEnraged?.55f:0);
                    impacts.Add(new Impact {Ring=Ring("Stone King Slam",bossSlamCenter,BossSlamRadius,spawnMaterial,.2f),Duration=.45f});
                    GameAudio.Play(AudioCue.Hammer); viewCamera?.Shake(.15f,.25f);
                    Vector3 gap=player.transform.position-bossSlamCenter; gap.y=0;
                    bool continueCombo=bossComboCharges>0;
                    RecoverForestBoss(bossFollowupSlam?2f:1.1f);
                    if(gap.magnitude<=BossSlamRadius+playerContactHalfSize
                        && (obstacles==null || obstacles.Clear(bossSlamCenter,player.transform.position))) health.TakeDamage(bossEnraged?38:32);
                    if(continueCombo && !health.IsDead) ContinueBossCombo();
                }
                break;
            case ForestBossPhase.ChargeWindup:
                BossAvatar.Face(bossAim); BossAvatar.ChargePose(.8f*(1-Mathf.Clamp01(bossTimer/bossWindup)),false);
                BossAvatar.BossPose(.2f,1);
                if(bossTimer<=0)
                {
                    // Keep the entire locked lane visible during travel.
                    forestBossPhase=ForestBossPhase.Charge; bossTimer=2f; bossChargeHit=false;
                    GameAudio.Play(AudioCue.Dash);
                }
                break;
            case ForestBossPhase.Charge:
                BossAvatar.ChargePose(.75f,true); BossAvatar.BossPose(.15f,1);
                Vector3 origin=forestBoss.position;
                float clearance=BossClearance(origin,bossAim);
                float travel=Mathf.Max(0,Mathf.Min((bossEnraged?22f:18f)*slow*dt,bossTravel,clearance));
                forestBoss.position+=bossAim*travel; bossTravel-=travel;
                bool hit=!bossChargeHit && BossCrossesPlayer(origin,bossAim,travel);
                if(hit) bossChargeHit=true;
                if(bossTravel<=.01f || clearance<=travel+.01f || bossTimer<=0) BeginBossSlam(true);
                Physics.SyncTransforms();
                if(hit) health.TakeDamage(bossEnraged?32:26);
                break;
            case ForestBossPhase.Volley:
                if(!bossVolleyLocked)
                {
                    AimBossVolley();
                    if(bossTimer<=BossVolleyLockTime)
                    {
                        // A long frame must still leave the complete committed dodge window.
                        bossVolleyLocked=true; bossTimer=BossVolleyLockTime;
                    }
                }
                BossAvatar.Face(bossAim); BossAvatar.BossPose(.35f,1);
                if(bossTimer<=0)
                {
                    // At most one fan per frame. Each subsequent fan gets a visible gap.
                    ReleaseBossFan(); bossVolleyShots--;
                    if(bossVolleyShots<=0)
                    {
                        if(bossComboCharges>0) ContinueBossCombo();
                        else RecoverForestBoss(.9f);
                    }
                    else {bossTimer=.65f; bossVolleyLocked=false;}
                }
                break;
            case ForestBossPhase.Summon:
                BossAvatar.BossPose(.75f,1);
                if(bossTimer<=0) { ReleaseBossMinions(); RecoverForestBoss(.6f); }
                break;
            case ForestBossPhase.Recover:
                BossAvatar.ChargePose(0,false); BossAvatar.BossPose(0,bossEnraged?.55f:0);
                if(bossTimer<=0) { forestBossPhase=ForestBossPhase.Chase; bossTimer=.25f; }
                break;
            case ForestBossPhase.Retreat:
                TickBossRetreat(dt,slow);
                break;
            case ForestBossPhase.Rest:
                TickBossRest(dt);
                break;
        }
        if(forestBoss!=null) Physics.SyncTransforms();
    }

    private void BeginBossAttack(Vector3 delta)
    {
        delta.y=0;
        HideBossWarning(); bossAim=delta.sqrMagnitude>.001f?delta.normalized:Vector3.forward;
        int attack=bossAttackIndex++%3;
        if(bossEnraged && attack==0) attack=2;
        if(delta.magnitude>22 || (attack==0 && delta.magnitude>4.5f)) attack=1;
        bossComboActive=bossEnraged; bossComboCharges=bossEnraged?1:0;
        bossWindup=bossEnraged?.85f:1f; bossTimer=bossWindup;
        if(attack==0)
        {
            BeginBossSlam(false);
        }
        else if(attack==1)
        {
            BeginBossCharge();
        }
        else
        {
            forestBossPhase=ForestBossPhase.Volley; bossVolleyShots=bossEnraged?2:1;
            bossVolleyMode=bossVolleyIndex++%2==0?BossVolleyMode.Fan:BossVolleyMode.Predictive;
            bossVolleyLocked=false; AimBossVolley();
            // The lifted arms and glowing core telegraph the fan without drawing ground trajectories.
            BossAvatar.BossPose(.35f,1);
        }
        // Starting a move never consumes its warning time, even on a long frame.
        BossAvatar.Face(bossAim);
        if(attack!=1) GameAudio.Play(AudioCue.FireballCast);
    }

    private void ContinueBossCombo()
    {
        bossComboCharges--; bossComboActive=true;
        BeginBossCharge();
    }
    private void BeginBossCharge()
    {
        HideBossWarning();
        Vector3 delta=player.transform.position-forestBoss.position; delta.y=0;
        Vector3 aim=delta+Vector3.ClampMagnitude(bossPlayerVelocity*.3f,1.8f);
        bossAim=aim.sqrMagnitude>.001f?aim.normalized:Vector3.forward;
        bossWindup=bossEnraged?.7f:.85f; bossTimer=bossWindup;
        forestBossPhase=ForestBossPhase.ChargeWindup;
        bossTravel=Mathf.Min(delta.magnitude+2,bossEnraged?16f:14f,BossClearance(forestBoss.position,bossAim));
        bossWarning=CreateChargeWarning(forestBoss.position,bossAim,bossTravel);
        bossWarning.name="Stone King Charge Warning";
        bossWarning.localScale=new Vector3((ForestBossRadius+playerContactHalfSize)/.38f,1,bossTravel);
        BossAvatar.Face(bossAim); BossAvatar.ChargePose(0,false); BossAvatar.BossPose(.2f,1);
        GameAudio.Play(AudioCue.FireballCast);
    }
    private void BeginBossSlam(bool followup)
    {
        HideBossWarning(); bossFollowupSlam=followup;
        forestBossPhase=ForestBossPhase.Slam;
        bossWindup=followup?BossFollowupWindup:bossEnraged?1f:1.25f; bossTimer=bossWindup;
        // The follow-up covers the landing point, with no snap turn or tracking ring.
        bossSlamCenter=forestBoss.position+(followup?Vector3.zero:bossAim*.9f); bossSlamCenter.y=.055f;
        bossWarning=Ring("Stone King Slam Warning",bossSlamCenter,BossSlamRadius,spawnMaterial,.1f).transform;
        BossAvatar.ChargePose(0,false); BossAvatar.BossPose(.2f,1);
        if(followup) GameAudio.Play(AudioCue.FireballCast);
    }
    private void AimBossVolley()
    {
        Vector3 target=player.transform.position;
        if(bossVolleyMode==BossVolleyMode.Predictive)
        {
            Vector3 gap=target-forestBoss.position; gap.y=0;
            float lead=Mathf.Clamp(BossVolleyLockTime+gap.magnitude/BossNeedleSpeed,.4f,1f);
            target+=Vector3.ClampMagnitude(bossPlayerVelocity*lead,5f);
            target.x=Mathf.Clamp(target.x,-17.4f,17.4f); target.z=Mathf.Clamp(target.z,-17.4f,17.4f);
        }
        Vector3 direction=target-forestBoss.position; direction.y=0;
        if(direction.sqrMagnitude>.001f) bossAim=direction.normalized;
    }
    private void ReleaseBossFan()
    {
        Vector3 muzzle=forestBoss.position+bossAim*(ForestBossRadius+.2f); muzzle.y=.65f;
        float spacing=bossVolleyMode==BossVolleyMode.Predictive?12:18;
        for(int i=0;i<5;i++)
        {
            ReleaseNeedle(muzzle,Quaternion.Euler(0,(i-2)*spacing,0)*bossAim);
            var needle=enemyNeedles[enemyNeedles.Count-1];
            needle.Speed=BossNeedleSpeed; needle.Remaining=BossNeedleRange;
            needle.Damage=bossEnraged?14:12;
        }
        GameAudio.Play(AudioCue.Crossbow);
    }
    private bool BossMinionPositionAvailable(Vector3 at)
    {
        Vector3 gap=at-player.transform.position; gap.y=0;
        if(gap.sqrMagnitude<9 || Mathf.Abs(at.x)>ArenaHalfSize-EnemySize || Mathf.Abs(at.z)>ArenaHalfSize-EnemySize) return false;
        if(obstacles!=null && obstacles.HitDistance(at,Vector3.forward,EnemySize/2)<=0) return false;
        foreach(var enemy in enemies)
        {
            gap=at-enemy.position; gap.y=0;
            float spacing=EnemyRadius(enemy)+EnemySize/2+.2f;
            if(gap.sqrMagnitude<spacing*spacing) return false;
        }
        foreach(var warning in bossSummonWarnings)
            if(warning!=null && (warning.position-at).sqrMagnitude<2.25f) return false;
        return true;
    }
    private void BeginBossSummon(int count)
    {
        HideBossWarning(); ClearBossSummonWarnings(); bossEnrageSummon=false;
        bossSummonTimer=bossEnraged?Random.Range(7f,9f):Random.Range(10f,12f);
        count=Mathf.Min(count,Mathf.Max(0,BossMinionLimit-(enemies.Count-1)));
        float start=Random.Range(0f,360f);
        for(int i=0;i<24 && bossSummonWarnings.Count<count;i++)
        {
            Vector3 at=forestBoss.position+Quaternion.Euler(0,start+i*137.5f,0)*Vector3.forward*(4f+i%3);
            at.y=.055f;
            if(!BossMinionPositionAvailable(at)) continue;
            bossSummonWarnings.Add(Ring("Stone King Minion Warning",at,.7f,spawnMaterial,.09f).transform);
        }
        forestBossPhase=ForestBossPhase.Summon; bossTimer=bossWindup=1f;
        BossAvatar.ChargePose(0,false); BossAvatar.BossPose(.75f,1); GameAudio.Play(AudioCue.WaveStart);
    }
    private void ReleaseBossMinions()
    {
        var positions=new System.Collections.Generic.List<Vector3>();
        foreach(var warning in bossSummonWarnings) if(warning!=null) positions.Add(warning.position);
        ClearBossSummonWarnings();
        foreach(var at in positions)
        {
            // Recheck the locked location: walking into a warning cancels that birth, never relocates it.
            int limit=forestBossPhase==ForestBossPhase.Rest?bossHordeLimit:BossMinionLimit;
            if(enemies.Count-1>=limit || !BossMinionPositionAvailable(at)) continue;
            Vector3 position=at; position.y=EnemySize/2;
            var enemy=CreateVisual(PrimitiveType.Cube,"Stone King Minion",position,Vector3.one*EnemySize,enemyMaterial);
            enemies.Add(enemy); enemyHealth.Add(enemy,session.Rules.Health(EnemyKind.Normal,WaveSettings));
            enemy.gameObject.AddComponent<EnemyAvatar>().Initialize(Resources.Load<GameObject>(EnemyModels[Random.Range(0,EnemyModels.Length)]),enemyModelMaterials,1.02f);
            waveSpawnCount++;
            if(forestBossPhase==ForestBossPhase.Rest) bossHordeRemaining--;
        }
    }
    private void ClearBossSummonWarnings()
    {
        foreach(var warning in bossSummonWarnings)
            if(warning!=null) {warning.gameObject.SetActive(false); Destroy(warning.gameObject);}
        bossSummonWarnings.Clear();
    }
    private void RecoverForestBoss(float duration)
    {
        HideBossWarning(); forestBossPhase=ForestBossPhase.Recover; bossTimer=duration; bossStagger=0;
        bossComboActive=false;
        bossReturning=false;
        BossAvatar?.ChargePose(0,false);
    }
    private void StaggerForestBoss(float duration)
    {
        if(forestBossPhase!=ForestBossPhase.Recover || bossStagger>0) return;
        bossStagger=Mathf.Min(.4f,Mathf.Max(0,duration)); bossTimer+=bossStagger;
    }
    private void ClampBossPosition()
    {
        Vector3 p=forestBoss.position; float limit=ArenaHalfSize-ForestBossRadius;
        p.x=Mathf.Clamp(p.x,-limit,limit); p.z=Mathf.Clamp(p.z,-limit,limit); forestBoss.position=p;
    }
    private float BossClearance(Vector3 origin,Vector3 direction)
    {
        float limit=ArenaHalfSize-ForestBossRadius,distance=float.PositiveInfinity;
        if(direction.x>0) distance=Mathf.Min(distance,(limit-origin.x)/direction.x);
        else if(direction.x<0) distance=Mathf.Min(distance,(-limit-origin.x)/direction.x);
        if(direction.z>0) distance=Mathf.Min(distance,(limit-origin.z)/direction.z);
        else if(direction.z<0) distance=Mathf.Min(distance,(-limit-origin.z)/direction.z);
        if(obstacles!=null) distance=Mathf.Min(distance,obstacles.HitDistance(origin,direction,ForestBossRadius));
        return Mathf.Max(0,distance-.01f);
    }
    private bool BossCrossesPlayer(Vector3 origin,Vector3 direction,float travel)
    {
        Vector3 offset=origin-player.transform.position;
        var local=new Vector3(Vector3.Dot(offset,player.transform.right),0,Vector3.Dot(offset,player.transform.forward));
        var aim=new Vector3(Vector3.Dot(direction,player.transform.right),0,Vector3.Dot(direction,player.transform.forward));
        float size=2*(playerContactHalfSize+ForestBossRadius);
        var bounds=new Bounds(Vector3.zero,new Vector3(size,2,size));
        return bounds.Contains(local) || (bounds.IntersectRay(new Ray(local,aim),out float hit) && hit<=travel);
    }
    private void HideBossWarning()
    {
        if(bossWarning!=null) { bossWarning.gameObject.SetActive(false); Destroy(bossWarning.gameObject); }
        bossWarning=null;
    }
    private void CancelForestBossAttacks()
    {
        HideBossWarning();
        ClearBossSummonWarnings(); bossEnrageSummon=false;
        bossComboActive=false; bossComboCharges=0;
        bossReturning=false; bossHordeRemaining=0; bossHordeTimer=0;
        BossAvatar?.ChargePose(0,false); BossAvatar?.BossPose(0,bossEnraged?.55f:0);
        if(forestBoss!=null) {var body=forestBoss.GetComponent<CapsuleCollider>(); if(body!=null) body.enabled=false;}
    }
    private void DefeatForestBoss()
    {
        bossDeathPosition=forestBoss.position;
        CancelForestBossAttacks(); ClearShooters(); crushed.Remove(forestBoss);
        BossDefeated=true;
    }
    private void CreateBossRubble(Vector3 at)
    {
        var effect=new Impact {Ring=Ring("Stone King Shattered",new Vector3(at.x,.06f,at.z),3.2f,spawnMaterial,.18f),
            Duration=1.1f,FragmentSize=.48f,Fragments=new Transform[14],Velocities=new Vector3[14]};
        for(int i=0;i<effect.Fragments.Length;i++)
        {
            float angle=i*Mathf.PI*2/effect.Fragments.Length;
            Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            Material mat=enemyModelMaterials.TryGetValue(i%3==0?"Boss_Wood":i%3==1?"Boss_Stone":"Boss_Moss",out var palette)?palette:enemyMaterial;
            effect.Fragments[i]=CreateVisual(PrimitiveType.Cube,"King Rubble",at+radial*.6f+Vector3.up*(.3f+i%3*.5f),Vector3.one*.48f,mat);
            effect.Velocities[i]=radial*(1.6f+i%3*.35f)+Vector3.up*(2+i%3);
        }
        impacts.Add(effect);
    }
    private void ClearForestBoss()
    {
        CancelForestBossAttacks(); forestBoss=null; BossDefeated=false; bossEnraged=false;
        bossAttackIndex=bossVolleyShots=bossMaxHealth=0; bossTimer=bossStagger=0;
        bossVolleyIndex=0; bossVolleyLocked=bossFollowupSlam=false;
        bossSummonTimer=0; bossPlayerVelocity=Vector3.zero; bossPreviousPlayerPosition=Vector3.zero;
        bossAssaultTimer=0; bossHordeLimit=0; bossHordeOrdinal=0; bossRetreatTarget=Vector3.zero;
    }
    // Existing small enemies retain their established range behavior. Boss attacks can hit its surface.
    private float BossSurfaceDistance(Transform enemy,Vector3 origin)
    {
        Vector3 gap=enemy.position-origin; gap.y=0;
        return Mathf.Max(0,gap.magnitude-(IsForestBoss(enemy)?ForestBossRadius:0));
    }
    private Vector3 BossSurfacePoint(Transform enemy,Vector3 origin)
    {
        Vector3 point=enemy.position;
        if(IsForestBoss(enemy)) { Vector3 gap=origin-point; gap.y=0; point+=gap.normalized*Mathf.Min(ForestBossRadius,gap.magnitude); }
        return point;
    }
    bool ICombatView.BossActive=>forestBoss!=null && !BossDefeated;
    int ICombatView.BossHealth=>ForestBossHealth;
    int ICombatView.BossMaxHealth=>bossMaxHealth;
    bool ICombatView.BossEnraged=>bossEnraged;
    string ICombatView.BossMove=>forestBossPhase==ForestBossPhase.Arrival?"AWAKENING":forestBossPhase==ForestBossPhase.Slam?(bossComboActive?"COMBO SLAM":"GROUND SLAM"):
        forestBossPhase==ForestBossPhase.ChargeWindup || forestBossPhase==ForestBossPhase.Charge?(bossReturning?"RETURN CHARGE":bossComboActive?"COMBO CHARGE":"CHARGE"):forestBossPhase==ForestBossPhase.Volley?(bossVolleyMode==BossVolleyMode.Predictive?"PREDICTIVE VOLLEY":"BONE FAN"):
        forestBossPhase==ForestBossPhase.Retreat?"RETREATING":forestBossPhase==ForestBossPhase.Rest?"RESTING · ARMOR 50%":
        forestBossPhase==ForestBossPhase.Summon?"SUMMONING":
        forestBossPhase==ForestBossPhase.Recover?"RECOVERING":forestBossPhase==ForestBossPhase.Enrage?"ENRAGED":"";
}
