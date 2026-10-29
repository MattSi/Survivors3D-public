using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal List<int> EquippedWeapons()
    {
        var list=new List<int>(); for(int i=0;i<WeaponCatalog.Count;i++) if(equipment.HasWeapon(i)) list.Add(i); return list;
    }

    protected internal readonly Dictionary<Transform,float> chilled=new Dictionary<Transform,float>();
    protected internal readonly Transform[] newWeaponVisuals=new Transform[WeaponCatalog.Count];
    protected internal readonly Material[] newWeaponMaterials=new Material[WeaponCatalog.Count];
    protected internal readonly float[] newCooldown=new float[WeaponCatalog.Count], swingAge=new float[WeaponCatalog.Count];
    protected internal readonly bool[] swinging=new bool[WeaponCatalog.Count], swingHit=new bool[WeaponCatalog.Count];
    protected internal readonly Vector3[] newAim=new Vector3[WeaponCatalog.Count];
    protected internal float NewRange(int id) => id==6?FrostRadius:equipment.Weapon(id).Range(WeaponCatalog.Range[id])*(WeaponCatalog.Melee(id)?equipment.RangeMultiplier:1);
    protected internal float NewInterval(int id) => id==6?2.8f:equipment.Weapon(id).Interval(WeaponCatalog.Interval[id])*(WeaponCatalog.Ammo(id)?equipment.FireMultiplier:1);
    protected internal Material NewWeaponMaterial(int id)
    {
        if(newWeaponMaterials[id]!=null) return newWeaponMaterials[id];
        Color color=id==5?new Color(.48f,.51f,.56f):id==6?new Color(.25f,.8f,1):id==7?new Color(1,.35f,.05f):new Color(.85f,.7f,.35f);
        var material=new Material(bulletMaterial) {name=WeaponCatalog.Names[id]+" Material",color=color};
        material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*(id==5?0:id>=6?.7f:.1f));
        newWeaponMaterials[id]=material; return material;
    }
    protected internal void WeaponPart(Transform root,string name,Vector3 position,Vector3 scale,Material material)
    {
        Transform part=CreateVisual(PrimitiveType.Cube,name,Vector3.zero,scale,material);
        part.SetParent(root,false); part.localPosition=position;
    }
    protected internal Transform NewWeaponVisual(int id)
    {
        if(newWeaponVisuals[id]!=null) return newWeaponVisuals[id];
        Transform root=new GameObject(WeaponCatalog.Names[id]).transform; root.SetParent(transform,false);
        Material mat=NewWeaponMaterial(id);
        if(id==11)
        {
            BuildBaitLantern(root);
        }
        else if(id==3)
        {
            BuildThrowingSpear(root);
        }
        else if(id==4)
        {
            BuildFanCrossbow(root);
        }
        else if(id==5)
        {
            BuildHammer(root);
        }
        else if(id==10)
        {
            BuildPoisonBackpack(root);
        }
        else if(id==9)
        {
            BuildBoomerang(root);
        }
        else if(id==6)
        {
            BuildFrostCrystals(root,mat);
        }
        else
        {
            BuildTorch(root);
        }
        newWeaponVisuals[id]=root; return root;
    }
    protected internal void UpdateNewWeapons(float dt)
    {
        for(int id=3;id<WeaponCatalog.Count;id++)
        {
            if(id==11) continue; // Lantern is updated before enemy navigation.
            if(!equipment.HasWeapon(id)) { if(id==10) ClearPoison(); if(id==9) ClearBoomerang(); if(id==7) ClearTorchState(); if(id==8) ResetFireballFocus(); if(id==4) { crossbowDrawing=false; crossbowDrawAge=0; } if(id==6) ClearFrostField(); if(newWeaponVisuals[id]!=null) newWeaponVisuals[id].gameObject.SetActive(false); continue; }
            if(id==8) { UpdateFireball(dt); continue; }
            Transform visual=NewWeaponVisual(id); visual.gameObject.SetActive(true);
            if(id==4) { UpdateFanCrossbow(dt,visual); continue; }
            if(id==3) { UpdateThrowingSpear(dt,visual); continue; }
            if(id==5) { UpdateHammer(dt,visual); continue; }
            if(id==6) { UpdateFrostField(dt,visual); continue; }
            if(id==7) UpdateTorch(dt,visual);
            if(id==9) UpdateBoomerang(dt,visual);
            if(id==10) UpdatePoisonBackpack(dt,visual);
        }
    }
    protected internal void FireNewProjectile(int id,Vector3 origin,Vector3 direction)
    {
        if(id!=4) return;
        GameAudio.Play(AudioCue.Crossbow);
        direction.y=0;
        if(direction.sqrMagnitude<.000001f) direction=Vector3.forward;
        direction.Normalize();
        int count=equipment.Weapon(4).Level+1;
        float spread=15+(count-2)*10;
        for(int i=0;i<count;i++)
        {
            float angle=count==2?(i==0?0:(crossbowAlternate?-spread:spread)):
                count==4?(i-1)*spread/3f:Mathf.Lerp(-spread/2,spread/2,(float)i/(count-1));
            if(count==4 && crossbowAlternate) angle=-angle;
            Vector3 aim=Quaternion.AngleAxis(angle,Vector3.up)*direction;
            Transform arrow=new GameObject("Crossbow Bolt").transform; arrow.SetParent(transform,false);
            arrow.SetPositionAndRotation(origin,Quaternion.LookRotation(aim));
            BuildCrossbowArrow(arrow);
            bool critical=Random.value<equipment.CritChance(false);
            bullets.Add(new Bullet {Transform=arrow,Direction=aim,Weapon=4,Critical=critical,
                Damage=12,RemainingRange=NewRange(4),Pierce=equipment.Stacks(RunItem.ArmorPiercing),
                Bounces=equipment.Stacks(RunItem.Ricochet)});
        }
        crossbowAlternate=!crossbowAlternate;
    }
    private bool crossbowAlternate, crossbowDrawing;
    private float crossbowDrawAge, crossbowRecoil;
    private LineRenderer crossbowString;
    private readonly List<Transform> loadedBolts=new List<Transform>();
    private Material crossbowWood,crossbowIron,crossbowSteel;
    private void BuildCrossbowArrow(Transform root)
    {
        if(crossbowWood==null) CreateCrossbowMaterials();
        WeaponPart(root,"Wood Shaft",Vector3.zero,new Vector3(.045f,.045f,.65f),crossbowWood);
        WeaponPart(root,"Steel Tip",new Vector3(0,0,.34f),new Vector3(.1f,.07f,.18f),crossbowSteel);
        WeaponPart(root,"Fletching",new Vector3(0,0,-.25f),new Vector3(.17f,.025f,.14f),crossbowSteel);
    }
    private void CreateCrossbowMaterials()
    {
        crossbowWood=HammerMaterial("Crossbow Warm Wood",new Color(.5f,.26f,.1f));
        crossbowIron=HammerMaterial("Crossbow Dark Iron",new Color(.16f,.18f,.2f));
        crossbowSteel=HammerMaterial("Crossbow Steel",new Color(.72f,.76f,.72f));
    }
    private void BuildFanCrossbow(Transform root)
    {
        if(crossbowWood==null) CreateCrossbowMaterials();
        HammerPart(root,"Wood Stock",new Vector3(0,0,-.12f),new Vector3(.3f,.2f,.95f),.04f,crossbowWood);
        HammerPart(root,"Butt",new Vector3(0,-.04f,-.63f),new Vector3(.34f,.32f,.18f),.035f,crossbowWood);
        Material leather=HammerMaterial("Crossbow Leather",new Color(.27f,.13f,.07f));
        HammerPart(root,"Leather Grip",new Vector3(0,-.19f,-.2f),new Vector3(.16f,.3f,.2f),.025f,leather);
        for(int i=0;i<2;i++) HammerPart(root,"Iron Band",new Vector3(0,0,-.4f+i*.62f),new Vector3(.34f,.24f,.055f),.01f,crossbowIron);
        for(int sign=-1;sign<=1;sign+=2)
        {
            HammerPart(root,"Iron Bow",new Vector3(sign*.4f,0,.24f),new Vector3(.65f,.12f,.16f),.025f,crossbowIron);
            Transform limb=root.GetChild(root.childCount-1);
            limb.localRotation=Quaternion.Euler(0,sign*18,0);
        }
        crossbowString=new GameObject("Bowstring").AddComponent<LineRenderer>();
        crossbowString.transform.SetParent(root,false); crossbowString.useWorldSpace=false;
        crossbowString.positionCount=3; crossbowString.startWidth=crossbowString.endWidth=.02f;
        crossbowString.sharedMaterial=leather;
        for(int i=0;i<5;i++)
        {
            Transform arrow=new GameObject("Loaded Arrow").transform; arrow.SetParent(root,false);
            arrow.localPosition=new Vector3((i-2)*.09f,.15f,.15f); BuildCrossbowArrow(arrow); loadedBolts.Add(arrow);
        }
    }
    private void UpdateFanCrossbow(float dt,Transform visual)
    {
        newCooldown[4]-=dt; crossbowRecoil=Mathf.MoveTowards(crossbowRecoil,0,dt);
        Vector3 hand=player.transform.position+player.transform.right*.65f;
        Transform target=FindItemTarget(hand,NewRange(4));
        if(target!=null) { Vector3 aim=target.position-hand; aim.y=0; if(aim.sqrMagnitude>.001f) newAim[4]=aim.normalized; }
        if(newAim[4].sqrMagnitude<.001f) newAim[4]=player.transform.forward;
        if(!crossbowDrawing && newCooldown[4]<=0 && target!=null) { crossbowDrawing=true; crossbowDrawAge=0; }
        if(crossbowDrawing)
        {
            crossbowDrawAge+=dt;
            if(crossbowDrawAge>=.18f)
            {
                crossbowDrawing=false;
                if(target!=null) { FireNewProjectile(4,hand,newAim[4]); crossbowRecoil=.1f; newCooldown[4]=Mathf.Max(.05f,NewInterval(4)-.18f); }
            }
        }
        visual.SetPositionAndRotation(hand-newAim[4]*crossbowRecoil,Quaternion.LookRotation(newAim[4]));
        crossbowString.SetPosition(0,new Vector3(-.7f,0,.13f));
        crossbowString.SetPosition(1,new Vector3(0,0,.13f-(crossbowDrawing?Mathf.Clamp01(crossbowDrawAge/.18f)*.35f:0)));
        crossbowString.SetPosition(2,new Vector3(.7f,0,.13f));
        for(int i=0;i<loadedBolts.Count;i++) loadedBolts[i].gameObject.SetActive(i<equipment.Weapon(4).Level+1 && crossbowRecoil<.02f);
    }

    protected internal void HitNewMelee(int id,Vector3 aim)
    {
        if(id!=7) return;
        torchHit.Clear();
        HitTorchSector(aim,-TorchArc*.5f,TorchArc*.5f);
    }

    private Transform frostField;
    private float frostRemaining, frostFallAge;
    private bool frostLanded;
    private readonly List<Transform> fallingIce=new List<Transform>();
    private int FrostDamage=>equipment.Weapon(6).Damage(20);
    private Vector3 frostCenter;
    private readonly HashSet<Transform> frostHit=new HashSet<Transform>();
    private float FrostRadius=>2.5f+(equipment.Weapon(6).Level-1)*.4f;
    private float FrostSlow=>.35f+(equipment.Weapon(6).Level-1)*.05f;
    private void BuildFrostCrystals(Transform root,Material material)
    {
        for(int i=0;i<3;i++)
        {
            Transform crystal=CreateVisual(PrimitiveType.Cube,"Frost Crystal",Vector3.zero,Vector3.one,material);
            crystal.SetParent(root,false);
            crystal.localPosition=new Vector3((i-1)*.18f,i==1?.16f:0,0);
            crystal.localRotation=Quaternion.Euler(0,45,(i-1)*-18);
            crystal.localScale=new Vector3(.16f,i==1?.48f:.32f,.16f);
        }
    }
    private void UpdateFrostField(float dt,Transform visual)
    {
        visual.position=player.transform.position-player.transform.right*.65f+Vector3.up*(.4f+Mathf.Sin(Time.time*2)*.06f);
        visual.rotation=Quaternion.Euler(0,Time.time*25,0);
        newCooldown[6]-=dt;
        if(newCooldown[6]<=0)
        {
            ClearFrostField(); newCooldown[6]=2.8f;
            frostRemaining=1.6f+(equipment.Weapon(6).Level-1)*.4f;
            frostCenter=player.transform.position; frostCenter.y=.04f;
            frostField=new GameObject("Frost Field").transform; frostField.SetParent(transform,false);
            frostField.position=frostCenter;
            Transform ring=Ring("Frost Boundary",frostCenter,FrostRadius,NewWeaponMaterial(6),.06f).transform;
            ring.SetParent(frostField,true);
            for(int i=0;i<12;i++)
            {
                Transform cluster=new GameObject("Ground Ice").transform; cluster.SetParent(frostField,false);
                float angle=i*Mathf.PI*2/12;
                cluster.localPosition=new Vector3(Mathf.Cos(angle)*FrostRadius*.8f,5,Mathf.Sin(angle)*FrostRadius*.8f);
                fallingIce.Add(cluster);
                cluster.localScale=Vector3.one*.9f; BuildFrostCrystals(cluster,NewWeaponMaterial(6));
            }
        }
        if(frostField==null) return;
        bool impact=false;
        if(!frostLanded)
        {
            frostFallAge+=dt;
            float t=Mathf.Clamp01(frostFallAge/.35f);
            foreach(Transform crystal in fallingIce)
            {
                Vector3 p=crystal.localPosition; p.y=5*(1-t*t); crystal.localPosition=p;
            }
            if(t<1) return;
            frostLanded=true; impact=true;
            GameAudio.Play(AudioCue.Frost);
            impacts.Add(new Impact { Ring=Ring("Ice Landing",frostCenter,FrostRadius,NewWeaponMaterial(6),.12f),Duration=.3f });
            dt=Mathf.Max(0,frostFallAge-.35f);
        }
        frostRemaining-=dt;
        if(frostRemaining<=0) { ClearFrostField(); return; }
        frostField.localScale=Vector3.one*Mathf.Min(1,frostRemaining/.25f);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Transform enemy=enemies[i]; Vector3 delta=enemy.position-frostCenter; delta.y=0;
            if(BossSurfaceDistance(enemy,frostCenter)>FrostRadius || (obstacles!=null && !obstacles.Clear(frostCenter,enemy.position))) continue;
            chilled[enemy]=Time.time+.2f;
            enemy.GetComponent<EnemyAvatar>()?.Chill(chilled[enemy]);
            if(frostHit.Add(enemy)) DamageEnemy(i,impact?FrostDamage:4,HitKind.Ice);
        }
    }
    private void ClearFrostField()
    {
        if(frostField!=null) { frostField.gameObject.SetActive(false); Destroy(frostField.gameObject); }
        frostField=null; frostRemaining=frostFallAge=0; frostLanded=false; fallingIce.Clear(); frostHit.Clear();
    }
    protected internal void ClearNewWeaponState()
    {
        ClearBaitLantern(); ClearPoison(); ClearBoomerang(); ClearTorchState(); ResetFireballFocus(); ClearHammerState(); ClearFrostField(); crossbowDrawing=crossbowAlternate=false; crossbowDrawAge=crossbowRecoil=0;
        spearPhase=0; spearAge=0; spearHit.Clear();
        chilled.Clear();
        for(int id=3;id<WeaponCatalog.Count;id++)
        { newCooldown[id]=swingAge[id]=0; swinging[id]=swingHit[id]=false; if(newWeaponVisuals[id]!=null) newWeaponVisuals[id].gameObject.SetActive(false); }
    }
    protected internal void DisposeNewWeapons()
    {
        ClearBaitLantern(); ClearPoison(); DisposeHammer(); ClearFrostField(); DisposeFireball();
        foreach(Material material in newWeaponMaterials) if(material!=null) Destroy(material);
    }
}
