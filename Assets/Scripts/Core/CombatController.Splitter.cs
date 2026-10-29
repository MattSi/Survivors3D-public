using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    private const float SplitterRadius=.5f,SplitlingRadius=.24f;
    protected internal readonly HashSet<Transform> splitters=new HashSet<Transform>();
    protected internal readonly Dictionary<Transform,float> splitlings=new Dictionary<Transform,float>();
    private sealed class SplitBirth { public Vector3 Origin; public int Health; public int Remaining=2; }
    private readonly Queue<SplitBirth> splitBirths=new Queue<SplitBirth>();
    private int pendingSplitlings;
    private int ReservedEnemyCount=>enemies.Count+splitters.Count+pendingSplitlings;
    private bool SplitlingEmerging(Transform enemy)=>splitlings.TryGetValue(enemy,out float until) && Time.time<until;

    private void QueueSplitOffspring(Transform enemy)
    {
        splitlings.Remove(enemy);
        if(!splitters.Remove(enemy)) return;
        // Only combat deaths split. Settlement, restart and defeat clear without creating enemies.
        if(session.State!=WaveSession.Phase.Combat || health.IsDead) return;
        splitBirths.Enqueue(new SplitBirth {Origin=enemy.position,Health=session.Rules.Health(EnemyKind.Splitling,WaveSettings)});
        pendingSplitlings+=2;
        if(enemyModelMaterials.TryGetValue("Splitter_Crack",out Material glow))
        {
            var impact=new Impact {Ring=Ring("Splitter Burst",enemy.position,.65f,glow,.07f),Duration=.35f,
                FragmentSize=.12f,Fragments=new Transform[4],Velocities=new Vector3[4]};
            for(int i=0;i<4;i++)
            {
                Vector3 direction=new Vector3(Mathf.Cos(i*Mathf.PI/2),0,Mathf.Sin(i*Mathf.PI/2));
                impact.Fragments[i]=CreateVisual(PrimitiveType.Cube,"Splitter Chip",enemy.position+Vector3.up*.2f,
                    Vector3.one*.12f,enemyModelMaterials["Splitter_Teal"]);
                impact.Velocities[i]=direction*1.8f+Vector3.up*1.5f;
            }
            impacts.Add(impact);
        }
    }

    private void UpdateSplitBirths()
    {
        if(session.State!=WaveSession.Phase.Combat || health.IsDead) return;
        int count=splitBirths.Count;
        for(int i=0;i<count;i++)
        {
            var birth=splitBirths.Dequeue();
            while(birth.Remaining>0 && enemies.Count<EnemyLimit)
            {
                if(!FindSplitBirthPosition(birth.Origin,out Vector3 position)) break;
                Transform child=CreateVisual(PrimitiveType.Cube,"Splitter Offspring",position,Vector3.one*EnemySize,enemyMaterial);
                child.gameObject.AddComponent<EnemyAvatar>().Initialize(Resources.Load<GameObject>("Characters/EnemySplitling"),enemyModelMaterials,.58f);
                enemies.Add(child); enemyHealth.Add(child,birth.Health);
                splitlings.Add(child,Time.time+.4f);
                child.GetComponent<EnemyAvatar>().Emerge(.4f);
                child.GetComponent<EnemyAvatar>().Face(player.transform.position-child.position);
                birth.Remaining--; pendingSplitlings--;
            }
            // Crowded deaths keep their reservation and retry locally; no wall teleporting.
            if(birth.Remaining>0) splitBirths.Enqueue(birth);
        }
    }

    private bool FindSplitBirthPosition(Vector3 origin,out Vector3 position)
    {
        for(int ring=0;ring<3;ring++) for(int step=0;step<16;step++)
        {
            float angle=step*Mathf.PI/8+ring*.3f;
            position=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(.38f+ring*.35f);
            position.y=EnemySize/2;
            float edge=ArenaHalfSize-SplitlingRadius;
            if(Mathf.Abs(position.x)>edge || Mathf.Abs(position.z)>edge) continue;
            if(obstacles!=null && (!obstacles.Clear(origin,position,SplitlingRadius) || obstacles.HitDistance(position,Vector3.forward,SplitlingRadius)<=0)) continue;
            Vector3 playerGap=position-player.transform.position; playerGap.y=0;
            if(playerGap.sqrMagnitude<.8f*.8f) continue;
            bool occupied=false;
            foreach(Transform other in enemies)
            {
                Vector3 delta=other.position-position; delta.y=0;
                float gap=EnemyRadius(other)+SplitlingRadius+.1f;
                if(delta.sqrMagnitude<gap*gap) {occupied=true; break;}
            }
            if(!occupied) return true;
        }
        position=origin; return false;
    }

    private void ClearSplitters()
    {
        splitters.Clear(); splitlings.Clear(); splitBirths.Clear(); pendingSplitlings=0;
    }
}
