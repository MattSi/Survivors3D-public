using System.Collections.Generic;
using UnityEngine;

public sealed partial class EnemyAvatar : MonoBehaviour
{
    private Transform visual;
    private AvatarPulse pulse;
    private Vector3 restScale,restPosition;
    private float footOffset,flattenStarted,flattenDuration;
    private bool flattened,flatCorpse;
    private float chargePose;
    private bool charging,isCharred;
    private bool heavy;
    private float emergeStarted,emergeDuration;
    public void Emerge(float duration) {emergeStarted=Time.time; emergeDuration=duration; ApplyEmergence();}
    private void ApplyEmergence()
    {
        if(visual==null) return;
        float t=Mathf.Clamp01((Time.time-emergeStarted)/Mathf.Max(.01f,emergeDuration));
        float height=Mathf.Lerp(.25f,1,Mathf.SmoothStep(0,1,t));
        visual.localScale=Vector3.Scale(restScale,new Vector3(1,height,1));
        visual.localPosition=restPosition+Vector3.up*(footOffset*(1-height));
        if(t>=1) emergeDuration=0;
    }
    private Transform throwArm,heldBomb;
    private Transform shootArm,shootMuzzle,shootBreech;
    private Quaternion shootArmRest;
    private float shootGlow;
    private readonly List<(Renderer body,int slot)> shooterGlowSlots=new List<(Renderer,int)>();
    private MaterialPropertyBlock shooterGlowBlock;
    public Vector3 ShooterMuzzlePosition=>shootMuzzle!=null?shootMuzzle.position:transform.position+Vector3.up*.25f;
    public void ShootPose(float amount,Vector3 direction)
    {
        if(shootArm==null || shootMuzzle==null || shootBreech==null) return;
        shootArm.localRotation=shootArmRest;
        direction.y=0;
        if(direction.sqrMagnitude<.001f) direction=faceDirection;
        Vector3 aim=(direction.normalized+Vector3.down*Mathf.Lerp(.65f,0,Mathf.Clamp01(amount))).normalized;
        shootArm.rotation=Quaternion.FromToRotation(shootMuzzle.position-shootBreech.position,aim)*shootArm.rotation;
        shootGlow=Mathf.Clamp01(amount); ApplyTint();
    }
    private Quaternion throwArmRest;
    public Vector3 BombHandPosition=>heldBomb!=null?heldBomb.position:transform.position+Vector3.up*.6f;
    public void ThrowPose(float amount,bool holding)
    {
        if(throwArm!=null) throwArm.localRotation=throwArmRest*Quaternion.Euler(-110*Mathf.Clamp01(amount),0,0);
        if(heldBomb!=null) heldBomb.gameObject.SetActive(holding);
    }
    private Vector3 faceDirection=Vector3.forward;
    private readonly List<(Renderer body,int slot)> glowSlots=new List<(Renderer,int)>();
    private MaterialPropertyBlock chargeGlow;
    public void ChargePose(float amount,bool dash)
    {
        chargePose=amount; charging=dash;
        if(visual!=null && !flattened)
        {
            if(amount>0) ApplyChargePose();
            else { visual.localScale=restScale; visual.localPosition=restPosition; }
            Face(faceDirection);
        }
        ApplyTint();
    }
    private void ApplyChargePose()
    {
        float height=1-chargePose*(charging?.12f:.23f);
        visual.localScale=Vector3.Scale(restScale,new Vector3(1+chargePose*.08f,height,1));
        visual.localPosition=restPosition+Vector3.up*(footOffset*(1-height));
    }
    public void Flatten(float duration,bool corpse=false)
    {
        flattened=true; flatCorpse=corpse; flattenStarted=Time.time; flattenDuration=duration; ApplyFlatten();
    }
    private void ApplyFlatten()
    {
        if(visual==null) return;
        float t=flatCorpse?0:Mathf.Clamp01((Time.time-flattenStarted)/Mathf.Max(.01f,flattenDuration));
        float height=Mathf.Lerp(.2f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.4f)/.6f)));
        visual.localScale=Vector3.Scale(restScale,new Vector3(Mathf.Lerp(1.45f,1,t),height,Mathf.Lerp(1.45f,1,t)));
        visual.localPosition=restPosition+Vector3.up*(footOffset*(1-height));
        if(!flatCorpse && t>=1) flattened=false;
    }
    private float chillUntil;
    private bool frosted,poisoned;
    public void SetPoisoned(bool value) { if(poisoned==value) return; poisoned=value; ApplyTint(); }
    private MaterialPropertyBlock frost;
    private float hitFlashUntil;
    public void FlashHit() { hitFlashUntil=Time.time+.12f; ApplyTint(); }
    private void ApplyTint()
    {
        if(bodies==null) return;
        bool hit=Time.time<hitFlashUntil;
        if(frost==null) frost=new MaterialPropertyBlock();
        frost.SetColor("_BaseColor",hit?Color.white:new Color(.3f,.8f,1));
        foreach(Renderer body in bodies)
        {
            bool lower=poisoned && body.bounds.center.y<transform.position.y+.3f;
            frost.SetColor("_BaseColor",hit?Color.white:frosted?new Color(.3f,.8f,1):new Color(.62f,.78f,.22f));
            body.SetPropertyBlock(hit || frosted || lower?frost:null);
        }
        if(chargeGlow==null) chargeGlow=new MaterialPropertyBlock();
        chargeGlow.Clear();
        if(hit || frosted) chargeGlow.SetColor("_BaseColor",hit?Color.white:new Color(.3f,.8f,1));
        chargeGlow.SetColor("_EmissionColor",!isCharred && chargePose>0
            ? new Color(1,.36f,.025f)*(charging?1.2f:chargePose*3f) : Color.black);
        foreach(var slot in glowSlots) slot.body.SetPropertyBlock(chargeGlow,slot.slot);
        if(shooterGlowBlock==null) shooterGlowBlock=new MaterialPropertyBlock();
        shooterGlowBlock.Clear();
        if(hit || frosted) shooterGlowBlock.SetColor("_BaseColor",hit?Color.white:new Color(.3f,.8f,1));
        shooterGlowBlock.SetColor("_EmissionColor",isCharred?Color.black:new Color(1,.36f,.025f)*(.2f+shootGlow*2.5f));
        foreach(var slot in shooterGlowSlots) slot.body.SetPropertyBlock(shooterGlowBlock,slot.slot);
        ApplyBossGlow(hit);
    }
    public void Chill(float until)
    {
        chillUntil=until; frosted=true;
        ApplyTint();
    }

    private void Update()
    {
        if(flattened) ApplyFlatten();
        else if(emergeDuration>0) ApplyEmergence();
        else if(chargePose>0 && visual!=null) ApplyChargePose();
        else if(heavy && visual!=null)
        {
            float step=Mathf.Sin(Time.time*5f+(GetInstanceID()&255));
            float height=1+step*.018f;
            visual.localScale=Vector3.Scale(restScale,new Vector3(1,height,1));
            visual.localPosition=restPosition+Vector3.up*(footOffset*(1-height));
        }
        else if (pulse != null) pulse.Apply(Time.time);
        if(frosted && Time.time>=chillUntil)
        { frosted=false; ApplyTint(); }
        if(hitFlashUntil>0 && Time.time>=hitFlashUntil) { hitFlashUntil=0; ApplyTint(); }
    }

    private Renderer[] bodies;
    private Quaternion facingCorrection;

    public void Initialize(GameObject model, Dictionary<string, Material> palette,float height=1.02f,bool heavy=false)
    {
        this.heavy=heavy;
        if (model == null) { bodies=new[] {GetComponent<Renderer>()}; return; }
        visual = Instantiate(model, transform, false).transform;
        foreach(Transform part in visual.GetComponentsInChildren<Transform>())
        {
            if(part.name=="ThrowArm") {throwArm=part; throwArmRest=part.localRotation;}
            if(part.name=="HeldBomb") heldBomb=part;
            if(part.name=="ShootArm") {shootArm=part; shootArmRest=part.localRotation;}
            if(part.name=="Muzzle") shootMuzzle=part;
            if(part.name=="Breech") shootBreech=part;
        }
        bodies = visual.GetComponentsInChildren<Renderer>();
        if (bodies.Length == 0) return;
        foreach (Renderer body in bodies)
        {
            Material[] slots = body.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                string name = slots[i] != null ? slots[i].name : "Enemy_Stone";
                if (!palette.TryGetValue(name, out Material material))
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = ColorFor(name) };
                    material.SetFloat("_Smoothness", 0.05f);
                    if(name=="Charger_Horn" || name=="Charger_Eye") material.EnableKeyword("_EMISSION");
                    if(name=="Bomber_Spark") {material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",new Color(1,.5f,.02f));}
                    if(name=="Shooter_Core") material.EnableKeyword("_EMISSION");
                    if(name=="Splitter_Crack" || name=="Splitter_Eye") {material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",ColorFor(name)*.35f);}
                    palette.Add(name, material);
                }
                slots[i] = material;
                if(name=="Charger_Horn" || name=="Charger_Eye") glowSlots.Add((body,i));
                if(name=="Shooter_Core") shooterGlowSlots.Add((body,i));
            }
            body.sharedMaterials = slots;
        }
        facingCorrection = Quaternion.identity;
        foreach (Transform part in visual.GetComponentsInChildren<Transform>())
        {
            if (part.name != "FaceAnchor") continue;
            Vector3 front = visual.InverseTransformPoint(part.position);
            front.y = 0f;
            facingCorrection = Quaternion.FromToRotation(front.normalized, Vector3.forward);
            break;
        }
        visual.localRotation = facingCorrection;
        Bounds bounds = Bounds();
        visual.localScale *= height / Mathf.Max(bounds.size.y, 0.01f);
        bounds = Bounds();
        visual.position += Vector3.up * (transform.position.y - 0.375f - bounds.min.y);
        GetComponent<Renderer>().forceRenderingOff = true;
        pulse = new AvatarPulse(visual, Bounds(), (GetInstanceID() & 255) * 0.618f);
        restScale=visual.localScale; restPosition=visual.localPosition;
        footOffset=(Bounds().min.y-visual.position.y)/transform.lossyScale.y;
        ShootPose(0,faceDirection);
        InitializeBossParts();
    }

    public void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (visual != null && direction.sqrMagnitude > 0.001f)
        {
            faceDirection=direction;
            visual.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(chargePose*(charging?28:18),0,0) * facingCorrection;
        }
    }

    public void Char(Material material)
    {
        if(bossArms!=null) return; // Burning still damages stone; retain its attack-readable face and core.
        isCharred=true;
        if (bodies == null) return;
        foreach (Renderer body in bodies)
        {
            Material[] slots = body.sharedMaterials;
            for (int i = 0; i < slots.Length; i++) slots[i] = material;
            body.sharedMaterials = slots;
        }
        ApplyTint();
    }

    private Bounds Bounds()
    {
        Bounds result = bodies[0].bounds;
        foreach (Renderer body in bodies) result.Encapsulate(body.bounds);
        return result;
    }

    private static Color ColorFor(string name)
    {
        if(name=="Boss_Stone") return new Color(.30f,.34f,.31f);
        if(name=="Boss_Slab") return new Color(.39f,.42f,.37f);
        if(name=="Boss_Moss") return new Color(.35f,.42f,.19f);
        if(name=="Boss_Wood") return new Color(.32f,.21f,.14f);
        if(name=="Boss_Dark") return new Color(.16f,.19f,.20f);
        if(name=="Boss_Core" || name=="Boss_Seam") return new Color(1,.49f,.06f);
        if(name=="Shooter_Sage") return new Color(.32f,.40f,.34f);
        if(name=="Shooter_Bone") return new Color(.78f,.75f,.61f);
        if(name=="Shooter_Core" || name=="Shooter_Eye") return new Color(1,.48f,.08f);
        if(name=="Splitter_Teal") return new Color(.24f,.43f,.45f);
        if(name=="Splitter_Shadow") return new Color(.11f,.24f,.27f);
        if(name=="Splitter_Crack") return new Color(.78f,.91f,.26f);
        if(name=="Splitter_Eye") return new Color(.96f,.98f,.48f);
        if(name=="Bomber_Plum") return new Color(.20f,.10f,.29f);
        if(name=="Bomber_Edge") return new Color(.29f,.16f,.38f);
        if(name=="Bomber_Belt") return new Color(.66f,.59f,.36f);
        if(name=="Bomber_Pouch") return new Color(.20f,.13f,.08f);
        if(name=="Bomber_Eye") return new Color(1,.48f,.08f);
        if(name=="Bomber_Spark") return new Color(1,.65f,.10f);
        if(name=="StoneArmor_Slab") return new Color(.46f,.46f,.41f);
        if(name=="StoneArmor_Body") return new Color(.33f,.36f,.36f);
        if(name=="StoneArmor_Moss") return new Color(.27f,.34f,.17f);
        if(name=="StoneArmor_Eye") return new Color(1,.48f,.08f);
        if(name=="Charger_Rust") return new Color(.56f,.19f,.095f);
        if(name=="Charger_Shadow") return new Color(.35f,.095f,.045f);
        if(name=="Charger_Horn") return new Color(.82f,.75f,.55f);
        if(name=="Charger_Eye") return new Color(1,.48f,.08f);
        if (name.Contains("Dark")) return new Color(0.075f, 0.10f, 0.13f);
        if (name.Contains("Bone")) return new Color(0.7f, 0.68f, 0.53f);
        if (name.Contains("Eye")) return new Color(0.95f, 0.27f, 0.08f);
        if (name.Contains("Moss")) return new Color(0.24f, 0.34f, 0.28f);
        return new Color(0.32f, 0.38f, 0.40f);
    }
}
