using UnityEngine;

public partial class CombatController
{
    private Mesh fireballMesh;
    private Material fireballCore, fireballShell, fireballFocusMaterial;
    private float fireballFocusTime, fireballFocusKick;
    private Vector3 fireballFocusAim=Vector3.forward;

    private void EnsureFireballArt()
    {
        if(fireballMesh!=null) return;
        fireballCore=HammerMaterial("Fireball Yellow Core",new Color(1,.85f,.12f));
        fireballShell=HammerMaterial("Fireball Orange Flame",new Color(1,.3f,.025f));
        foreach(Material material in new[]{fireballCore,fireballShell})
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",material.color*.8f);
        }
        // Independent triangle vertices retain flat faces rather than smooth sphere shading.
        Vector3[] points={Vector3.up,Vector3.down,Vector3.left,Vector3.forward,Vector3.right,Vector3.back};
        int[] faces={0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};
        var vertices=new Vector3[faces.Length]; var triangles=new int[faces.Length];
        for(int i=0;i<faces.Length;i++) { vertices[i]=points[faces[i]]; triangles[i]=i%3==1?i+1:i%3==2?i-1:i; }
        fireballMesh=new Mesh {name="Flat Fire Crystal",vertices=vertices,triangles=triangles};
        fireballMesh.RecalculateNormals(); fireballMesh.RecalculateBounds();
    }

    private Transform FireCrystal(string name,Vector3 position,Vector3 scale,Material material)
    {
        Transform part=new GameObject(name).transform;
        part.SetParent(transform,false); part.position=position; part.localScale=scale;
        part.gameObject.AddComponent<MeshFilter>().sharedMesh=fireballMesh;
        var renderer=part.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return part;
    }

    protected internal void UpdateFireball(float dt)
    {
        fireballFocusTime+=dt;
        fireballFocusKick=Mathf.Max(0,fireballFocusKick-dt);
        newCooldown[8]-=dt;
        Vector3 origin=FireballOrigin();
        Transform target=FindItemTarget(origin,NewRange(8));
        float charge=target!=null && newCooldown[8]>0
            ? Mathf.SmoothStep(0,1,1-Mathf.Clamp01(newCooldown[8]/.22f)) : 0;
        if(newCooldown[8]<=0 && target!=null)
        {
            Vector3 aim=target.position-origin; aim.y=0;
            fireballFocusAim=aim.sqrMagnitude>.000001f?aim.normalized:player.transform.forward;
            FireFireball(origin,fireballFocusAim);
            newCooldown[8]=NewInterval(8);
            fireballFocusKick=.18f;
        }
        UpdateFireballFocus(charge);
    }

    private Vector3 FireballOrigin()
    {
        Vector3 origin=player.transform.position;
        float orbit=fireballFocusTime*Mathf.PI*2/2.6f;
        Vector3 shoulder=origin+player.transform.right*(.8f+Mathf.Cos(orbit)*.14f)
            +player.transform.forward*(Mathf.Sin(orbit)*.22f);
        // Keep the projectile on the player's side of nearby cover.
        return obstacles==null || obstacles.Clear(origin,shoulder,BulletRadius)?shoulder:origin;
    }

    private void UpdateFireballFocus(float charge)
    {
        EnsureFireballArt();
        Transform focus=newWeaponVisuals[8];
        if(focus==null)
        {
            focus=new GameObject("Fireball Focus").transform; focus.SetParent(transform,false);
            newWeaponVisuals[8]=focus;
            // Animate only the equipped focus; projectiles and explosions keep their own materials.
            fireballFocusMaterial=HammerMaterial("Fireball Focus Glow",fireballCore.color);
            fireballFocusMaterial.EnableKeyword("_EMISSION");
            Transform core=FireCrystal("Ready Fire Core",Vector3.zero,Vector3.one*.3f,fireballFocusMaterial);
            core.SetParent(focus,false);
            for(int i=0;i<3;i++)
            {
                Transform ember=FireCrystal("Orbiting Ember",Vector3.zero,Vector3.one*.10f,fireballShell);
                ember.SetParent(focus,false);
                float angle=i*Mathf.PI*2/3;
                ember.localPosition=new Vector3(Mathf.Cos(angle)*.4f,Mathf.Sin(angle)*.25f,0);
            }
        }
        focus.gameObject.SetActive(true);
        float kick=fireballFocusKick/.18f;
        focus.position=FireballOrigin()+Vector3.up*(.7f+Mathf.Sin(fireballFocusTime*4)*.06f)
            +fireballFocusAim*(.18f*kick*kick);
        focus.rotation=player.transform.rotation;
        float recharge=1-Mathf.Clamp01(newCooldown[8]/NewInterval(8));
        focus.localScale=Vector3.one*(Mathf.Lerp(.65f,1,recharge)*(1-.12f*charge)+.18f*kick);
        focus.GetChild(0).localRotation=Quaternion.Euler(20,fireballFocusTime*65,fireballFocusTime*35);
        for(int i=0;i<3;i++)
        {
            float angle=fireballFocusTime*Mathf.PI*2+(i*Mathf.PI*2/3);
            Transform ember=focus.GetChild(i+1);
            ember.localPosition=new Vector3(Mathf.Cos(angle)*.4f,Mathf.Sin(angle)*.18f,Mathf.Sin(angle)*.28f);
            ember.localRotation=Quaternion.Euler(fireballFocusTime*130,i*40,fireballFocusTime*180);
        }
        Color warm=Color.Lerp(new Color(1,.42f,.06f),new Color(1,.85f,.12f),recharge);
        fireballFocusMaterial.color=Color.Lerp(warm,new Color(1,.98f,.7f),Mathf.Clamp01(charge*.65f+kick*.5f));
        fireballFocusMaterial.SetColor("_EmissionColor",fireballFocusMaterial.color*(.3f+.5f*recharge+1.4f*charge+kick));
    }

    private void ResetFireballFocus()
    {
        fireballFocusTime=fireballFocusKick=0;
        fireballFocusAim=Vector3.forward;
        if(newWeaponVisuals[8]!=null) newWeaponVisuals[8].gameObject.SetActive(false);
    }

    protected internal void FireFireball(Vector3 origin,Vector3 direction)
    {
        GameAudio.Play(AudioCue.FireballCast);
        EnsureFireballArt();
        direction.y=0; direction=direction.sqrMagnitude>.000001f?direction.normalized:Vector3.forward;
        Transform root=new GameObject("Fireball Projectile").transform;
        root.SetParent(transform,false); root.SetPositionAndRotation(origin,Quaternion.LookRotation(direction));
        for(int i=0;i<6;i++)
        {
            Transform part=FireCrystal(i==0?"Fire Core":"Flame Tail",Vector3.zero,Vector3.one,i==0?fireballCore:fireballShell);
            part.SetParent(root,false);
            part.localPosition=new Vector3(0,.7f,-i*.22f);
            float size=i==0?.4f:.28f*(1-i/7f);
            part.localScale=new Vector3(size,size,size*(i==0?1.2f:1.8f));
            part.localRotation=Quaternion.Euler(0,0,i*37);
        }
        bullets.Add(new Bullet {Transform=root,Direction=direction,Weapon=8,
            Damage=equipment.Weapon(8).Damage(WeaponCatalog.Damage[8]),RemainingRange=NewRange(8),
            BlastRadius=equipment.Weapon(8).Range(1.5f),Critical=Random.value<equipment.CritChance(false)});
    }

    private void AnimateFireball(Bullet bullet,float dt)
    {
        for(int i=0;i<bullet.Transform.childCount;i++)
            bullet.Transform.GetChild(i).Rotate(0,0,(i%2==0?170:-120)*dt,Space.Self);
    }

    protected internal void ExplodeFireball(Bullet bullet)
    {
        GameAudio.Play(AudioCue.Explosion);
        EnsureFireballArt();
        // One area hit, including the direct target; never add a separate contact hit.
        Vector3 center=bullet.Transform.position;
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Vector3 delta=enemies[i].position-center; delta.y=0;
            if(BossSurfaceDistance(enemies[i],center)>bullet.BlastRadius ||
                (obstacles!=null && !obstacles.Clear(center,enemies[i].position))) continue;
            DamageEnemyCritical(i,bullet.Damage,HitKind.Magic,bullet.Critical);
        }
        Vector3 ground=new Vector3(center.x,.06f,center.z);
        var impact=new Impact {Ring=Ring("Fireball Blast",ground,bullet.BlastRadius,fireballShell,.08f),
            Duration=.35f,FragmentSize=.28f,Fragments=new Transform[12],Velocities=new Vector3[12]};
        impact.Ring.positionCount=12;
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI*2/12;
            Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            impact.Ring.SetPosition(i,radial*bullet.BlastRadius);
            impact.Fragments[i]=FireCrystal("Fireball Burst Shard",center+Vector3.up*.35f+radial*.12f,Vector3.one*.28f,i%3==0?fireballCore:fireballShell);
            impact.Velocities[i]=radial*(bullet.BlastRadius*2.5f)+Vector3.up*(i%3==0?2.5f:1.2f);
        }
        impacts.Add(impact);
    }

    private void ClearFireballs()
    {
        for(int i=bullets.Count-1;i>=0;i--) if(bullets[i].Weapon==8)
        { bullets[i].Transform.gameObject.SetActive(false); Destroy(bullets[i].Transform.gameObject); bullets.RemoveAt(i); }
        newCooldown[8]=0;
        ResetFireballFocus();
    }

    private void DisposeFireball()
    {
        if(fireballMesh!=null) Destroy(fireballMesh);
        // Materials are registered with, and released by, DisposeHammer.
    }
}
