using UnityEngine;

public sealed partial class EnemyAvatar
{
    private Transform[] bossArms;
    private Quaternion[] bossArmRest;
    private float bossGlow;
    private MaterialPropertyBlock bossGlowBlock;
    private readonly System.Collections.Generic.List<(Renderer body,int slot,bool seam)> bossGlowSlots=new System.Collections.Generic.List<(Renderer,int,bool)>();
    private void InitializeBossParts()
    {
        Transform left=null,right=null;
        foreach(var part in visual.GetComponentsInChildren<Transform>())
        {
            if(part.name=="BossLeftArm") left=part;
            if(part.name=="BossRightArm") right=part;
        }
        if(left==null || right==null) return;
        bossArms=new[]{left,right}; bossArmRest=new[]{left.localRotation,right.localRotation};
        foreach(Renderer body in bodies)
        {
            var slots=body.sharedMaterials;
            for(int i=0;i<slots.Length;i++)
            {
                if(slots[i].name!="Boss_Core" && slots[i].name!="Boss_Seam") continue;
                slots[i].EnableKeyword("_EMISSION");
                bossGlowSlots.Add((body,i,slots[i].name=="Boss_Seam"));
            }
        }
        BossPose(0,0);
    }
    public void BossPose(float lift,float glow)
    {
        if(bossArms==null) return;
        bossGlow=Mathf.Clamp01(glow);
        Vector3 axis=Vector3.Cross(Vector3.up,faceDirection.normalized);
        for(int i=0;i<bossArms.Length;i++)
        {
            bossArms[i].localRotation=bossArmRest[i];
            bossArms[i].rotation=Quaternion.AngleAxis(-115*Mathf.Clamp01(lift),axis)*bossArms[i].rotation;
        }
        ApplyBossGlow(Time.time<hitFlashUntil);
    }
    private void ApplyBossGlow(bool hit)
    {
        if(bossArms==null) return;
        if(bossGlowBlock==null) bossGlowBlock=new MaterialPropertyBlock();
        foreach(var slot in bossGlowSlots)
        {
            bossGlowBlock.Clear();
            bossGlowBlock.SetColor("_BaseColor",hit?Color.white:slot.seam?new Color(.25f,.16f,.06f):new Color(1,.5f,.08f));
            bossGlowBlock.SetColor("_EmissionColor",new Color(1,.38f,.025f)*(slot.seam?bossGlow*2:.5f+bossGlow*2));
            slot.body.SetPropertyBlock(bossGlowBlock,slot.slot);
        }
    }
}
