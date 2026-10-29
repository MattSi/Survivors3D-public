using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal readonly List<Vector3> spawnPoints = new List<Vector3>();
    protected internal Material spawnMaterial;
    protected internal readonly List<GameObject> portalVisuals = new List<GameObject>();
    protected internal sealed class Impact
    {
        public LineRenderer Ring;
        public float Age, Duration = 0.18f, FragmentSize = .2f;
        public Transform[] Fragments;
        public Vector3[] Velocities;
    }
    protected internal readonly List<Impact> impacts = new List<Impact>();

    protected internal LineRenderer Ring(string name, Vector3 position, float radius, Material material, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
        line.sharedMaterial = material; line.startWidth = line.endWidth = width;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        for(int i=0;i<40;i++)
        {
            float angle=i*Mathf.PI*2/40;
            line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
        }
        return line;
    }

    protected internal void CreateSpawnPoints()
    {
        foreach (GameObject visual in portalVisuals) { visual.SetActive(false); Destroy(visual); }
        portalVisuals.Clear(); spawnPoints.Clear();
        if (spawnMaterial == null)
        {
            spawnMaterial = new Material(bulletMaterial) { name="Enemy Portal", color=new Color(1,0.12f,0.06f) };
            spawnMaterial.EnableKeyword("_EMISSION");
            spawnMaterial.SetColor("_EmissionColor", new Color(1.5f,0.06f,0.01f));
        }
        for (int attempt=0; attempt<500 && spawnPoints.Count<8; attempt++)
            TryAddSpawnPoint(new Vector3(Random.Range(-16.5f,16.5f),0,Random.Range(-16.5f,16.5f)));
        // The outer lane is always free of arena obstacles, providing a bounded fallback.
        int offset=Random.Range(0,64);
        for (int i=0;i<64 && spawnPoints.Count<8;i++)
        {
            int slot=(i+offset)%64;
            float along=-16.5f+(slot%16)*2.2f;
            int side=slot/16;
            TryAddSpawnPoint(side<2 ? new Vector3(side==0?-16.5f:16.5f,0,along)
                : new Vector3(along,0,side==2?-16.5f:16.5f));
        }
        foreach (Vector3 point in spawnPoints)
        {
            portalVisuals.Add(Ring("Enemy Spawn Portal",point+Vector3.up*0.06f,0.8f,spawnMaterial,0.12f).gameObject);
            portalVisuals.Add(Ring("Portal Inner Ring",point+Vector3.up*0.065f,0.5f,spawnMaterial,0.05f).gameObject);
        }
    }

    protected internal void TryAddSpawnPoint(Vector3 point)
    {
        if(point.sqrMagnitude<100) return;
        foreach(Vector3 existing in spawnPoints) if((point-existing).sqrMagnitude<16) return;
        if(obstacles != null)
            foreach(Bounds original in obstacles.Blocks)
            {
                Bounds padded=original; padded.Expand(new Vector3(2,0,2));
                if(padded.Contains(point)) return;
            }
        spawnPoints.Add(point);
    }

    protected internal void ExplodeBullet(Vector3 center, int directIndex, int damage)
    {
        ResolveBulletHit(new Bullet { Damage=damage },enemies[directIndex],center);
    }

    protected internal void UpdateImpacts(float dt)
    {
        UpdateHammerEffects(dt);
        for(int i=impacts.Count-1;i>=0;i--)
        {
            Impact impact=impacts[i]; impact.Age+=dt;
            if(impact.Age>=impact.Duration) { DestroyImpact(impact); impacts.RemoveAt(i); continue; }
            float progress=impact.Age/impact.Duration;
            impact.Ring.transform.localScale=Vector3.one*Mathf.Lerp(0.25f,1,progress);
            impact.Ring.startWidth=impact.Ring.endWidth=0.08f*(1-progress);
            if(impact.Fragments==null) continue;
            for(int fragment=0;fragment<impact.Fragments.Length;fragment++)
            {
                impact.Velocities[fragment]+=Vector3.down*(7f*dt);
                Transform piece=impact.Fragments[fragment];
                piece.position+=impact.Velocities[fragment]*dt;
                piece.Rotate(new Vector3(180,240,120)*dt);
                piece.localScale=Vector3.one*(impact.FragmentSize*(1-progress));
            }
        }
    }
    protected internal void ClearImpacts()
    {
        foreach(Impact impact in impacts) DestroyImpact(impact);
        impacts.Clear();
    }

    protected internal void DestroyImpact(Impact impact)
    {
        if(impact.Ring!=null) Destroy(impact.Ring.gameObject);
        if(impact.Fragments!=null)
            foreach(Transform fragment in impact.Fragments)
                if(fragment!=null) Destroy(fragment.gameObject);
    }

    protected internal void ExplodeRemainingEnemy(Vector3 position)
    {
        var impact=new Impact {
            Ring=Ring("Enemy End Explosion",position,1.2f,spawnMaterial,0.08f),
            Duration=0.6f, Fragments=new Transform[6], Velocities=new Vector3[6]
        };
        for(int i=0;i<impact.Fragments.Length;i++)
        {
            float angle=(i*60f+Random.Range(-15f,15f))*Mathf.Deg2Rad;
            impact.Fragments[i]=CreateVisual(PrimitiveType.Cube,"Enemy Explosion Fragment",
                position,Vector3.one*0.2f,i%2==0?spawnMaterial:enemyMaterial);
            impact.Velocities[i]=new Vector3(Mathf.Cos(angle)*2f,Random.Range(1.5f,3f),Mathf.Sin(angle)*2f);
        }
        impacts.Add(impact);
    }
}
