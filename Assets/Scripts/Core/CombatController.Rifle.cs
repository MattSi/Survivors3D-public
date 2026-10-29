using UnityEngine;

public partial class CombatController
{
    private Transform rifleVisual,rifleFlash;
    private Material rifleGlow;
    private Vector3 rifleAim=Vector3.forward;
    private int rifleRounds;
    private float rifleRoundDelay,rifleFlashAge,rifleRecoil;
    private Vector3 RifleHand=>player.transform.position+player.transform.right*.6f+Vector3.up*.12f;

    private void BuildRifle()
    {
        rifleVisual=new GameObject("Burst Rifle").transform; rifleVisual.SetParent(transform,false);
        Material wood=HammerMaterial("Rifle Warm Wood",new Color(.65f,.32f,.09f));
        Material metal=HammerMaterial("Rifle Dark Iron",new Color(.18f,.20f,.22f));
        Material steel=HammerMaterial("Rifle Steel Edges",new Color(.5f,.54f,.57f));
        Material leather=HammerMaterial("Rifle Leather",new Color(.28f,.13f,.07f));
        HammerPart(rifleVisual,"Wooden Stock",new Vector3(0,-.03f,-.38f),new Vector3(.19f,.25f,.47f),.035f,wood);
        HammerPart(rifleVisual,"Receiver",new Vector3(0,.045f,.02f),new Vector3(.23f,.23f,.46f),.03f,metal);
        HammerPart(rifleVisual,"Wooden Foregrip",new Vector3(0,-.035f,.34f),new Vector3(.2f,.15f,.32f),.025f,wood);
        HammerPart(rifleVisual,"Short Barrel",new Vector3(0,.065f,.48f),new Vector3(.13f,.13f,.46f),.02f,steel);
        HammerPart(rifleVisual,"Muzzle Collar",new Vector3(0,.065f,.69f),new Vector3(.2f,.2f,.12f),.025f,metal);
        HammerPart(rifleVisual,"Magazine",new Vector3(0,-.17f,.07f),new Vector3(.15f,.22f,.18f),.025f,metal);
        HammerPart(rifleVisual,"Leather Grip",new Vector3(0,-.17f,-.16f),new Vector3(.14f,.23f,.14f),.02f,leather);
        HammerPart(rifleVisual,"Front Sight",new Vector3(0,.18f,.53f),new Vector3(.045f,.09f,.065f),.008f,metal);
        for(int i=0;i<2;i++) HammerPart(rifleVisual,"Iron Band",new Vector3(0,-.035f,.24f+i*.19f),new Vector3(.22f,.17f,.035f),.012f,metal);
        rifleGlow=HammerMaterial("Rifle Flash",new Color(1,.73f,.18f));
        rifleGlow.EnableKeyword("_EMISSION"); rifleGlow.SetColor("_EmissionColor",new Color(3,1.5f,.15f));
        rifleFlash=new GameObject("Muzzle Flash").transform; rifleFlash.SetParent(rifleVisual,false); rifleFlash.localPosition=new Vector3(0,.065f,.83f);
        WeaponPart(rifleFlash,"Flash Core",Vector3.zero,new Vector3(.09f,.09f,.22f),rifleGlow);
        WeaponPart(rifleFlash,"Flash Cross",Vector3.zero,new Vector3(.23f,.035f,.07f),rifleGlow);
        rifleFlash.gameObject.SetActive(false);
    }
    private Material gatlingGlow,gatlingBulletMaterial;
    private Transform gatlingAssembly;

    protected internal void UpdateRifle(float dt)
    {
        if(!equipment.HasWeapon(0)) { ClearRifle(); return; }
        if(rifleVisual==null) BuildRifle();
        rifleVisual.gameObject.SetActive(true);
        nextShot-=dt; rifleRoundDelay-=dt;
        rifleFlashAge=Mathf.Max(0,rifleFlashAge-dt); rifleRecoil=Mathf.MoveTowards(rifleRecoil,0,dt*1.6f);
        if(equipment.Gun.Evolved)
        {
            EnsureGatlingVisuals(dt);
            float gatlingInterval=0.075f*equipment.FireMultiplier;
            if(FindItemTarget(RifleHand,GunRange)==null)
            {
                nextShot=Mathf.Max(nextShot,0f);
            }
            else
            {
                int maxBurst=3;
                while(nextShot<=0f && maxBurst>0)
                {
                    if(FireRifleRound())
                    {
                        nextShot+=gatlingInterval;
                        maxBurst--;
                    }
                    else break;
                }
                if(nextShot<-gatlingInterval) nextShot=-gatlingInterval;
            }
        }
        else
        {
            if(gatlingAssembly!=null) gatlingAssembly.gameObject.SetActive(false);
            if(rifleRounds==0 && nextShot<=0 && FindItemTarget(RifleHand,GunRange)!=null)
            { rifleRounds=3; rifleRoundDelay=0; nextShot=GunInterval; }
            // Emit at most one round per frame so a slow frame never collapses the burst into one hit.
            if(rifleRounds>0 && rifleRoundDelay<=0)
            {
                if(FireRifleRound()) { rifleRounds--; rifleRoundDelay=.1f*equipment.FireMultiplier; }
                else { rifleRounds=0; }
            }
        }
        Transform target=FindItemTarget(RifleHand,GunRange);
        if(target!=null) { Vector3 delta=target.position-RifleHand; delta.y=0; if(delta.sqrMagnitude>.000001f) rifleAim=delta.normalized; }
        else rifleAim=(player.transform.forward+player.transform.right*.55f).normalized;
        rifleVisual.SetPositionAndRotation(RifleHand-rifleAim*rifleRecoil,Quaternion.LookRotation(rifleAim));
        rifleFlash.gameObject.SetActive(rifleFlashAge>0);
    }
    private bool FireRifleRound()
    {
        Vector3 origin=RifleHand;
        Transform target=FindItemTarget(origin,GunRange);
        if(target==null) return false;
        if(rifleVisual==null) BuildRifle();
        Vector3 direction=target.position-origin; direction.y=0;
        if(direction.sqrMagnitude<.000001f) direction=player.transform.forward;
        direction.Normalize(); rifleAim=direction;
        bool evo=equipment.Gun.Evolved;
        Material bulletMat=evo?(gatlingBulletMaterial??(gatlingBulletMaterial=HammerMaterial("Gatling Bullet",new Color(.3f,.9f,1f)))):bulletMaterial;
        Material glowMat=evo?(gatlingGlow??(gatlingGlow=CreateGatlingGlow())):rifleGlow;
        Transform visual=CreateVisual(PrimitiveType.Sphere,"Bullet",origin,Vector3.one*(evo?.16f:.13f),bulletMat);
        var trail=visual.gameObject.AddComponent<TrailRenderer>(); trail.time=evo?.09f:.07f; trail.minVertexDistance=.03f;
        trail.startWidth=evo?.08f:.06f; trail.endWidth=.015f; trail.sharedMaterial=glowMat;
        trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        int pierce=evo?999:equipment.Stacks(RunItem.ArmorPiercing);
        bullets.Add(new Bullet {Transform=visual,Direction=direction,Damage=equipment.Gun.Damage(bulletDamage),
            Pierce=pierce,Bounces=equipment.Stacks(RunItem.Ricochet),RemainingRange=GunRange});
        GameAudio.Play(AudioCue.Rifle);
        rifleFlashAge=.055f; rifleRecoil=evo?.045f:.09f;
        return true;
    }
    private Material CreateGatlingGlow()
    {
        var mat=HammerMaterial("Void Gatling Glow",new Color(.2f,.85f,1f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor",new Color(.5f,2.5f,4.5f));
        return mat;
    }
    private void EnsureGatlingVisuals(float dt)
    {
        if(rifleVisual==null) return;
        if(gatlingAssembly==null)
        {
            gatlingAssembly=new GameObject("Gatling Assembly").transform;
            gatlingAssembly.SetParent(rifleVisual,false);
            gatlingAssembly.localPosition=new Vector3(0,.065f,.48f);
            Material barrelMat=HammerMaterial("Gatling Barrels Metal",new Color(.16f,.18f,.24f));
            Material coreGlow=CreateGatlingGlow();
            for(int i=0;i<4;i++)
            {
                float rad=i*90f*Mathf.Deg2Rad;
                Vector3 off=new Vector3(Mathf.Cos(rad)*.055f,Mathf.Sin(rad)*.055f,.12f);
                HammerPart(gatlingAssembly,$"Barrel_{i}",off,new Vector3(.045f,.045f,.38f),.01f,barrelMat);
            }
            HammerPart(gatlingAssembly,"Core Dynamo",Vector3.zero,new Vector3(.14f,.14f,.16f),.02f,coreGlow);
        }
        gatlingAssembly.gameObject.SetActive(equipment.Gun.Evolved);
        if(equipment.Gun.Evolved)
        {
            gatlingAssembly.Rotate(0,0,900f*dt);
        }
    }
    protected internal void ClearRifle()
    {
        rifleRounds=0; rifleRoundDelay=rifleFlashAge=rifleRecoil=0;
        rifleAim=Vector3.forward;
        if(gatlingAssembly!=null) gatlingAssembly.gameObject.SetActive(false);
        if(rifleVisual!=null) rifleVisual.gameObject.SetActive(false);
        if(rifleFlash!=null) rifleFlash.gameObject.SetActive(false);
    }
}
