using System.Collections.Generic;
using UnityEngine;

public partial class CombatController
{
    protected internal readonly HashSet<Transform> stoneArmors=new HashSet<Transform>();
    protected internal bool NextEnemyIsStoneArmor => NextEnemyKind==EnemyKind.StoneArmor;
    protected internal float EnemyRadius(Transform enemy)=>IsForestBoss(enemy)?ForestBossRadius:stoneArmors.Contains(enemy)?.6f:splitters.Contains(enemy)?SplitterRadius:splitlings.ContainsKey(enemy)?SplitlingRadius:EnemySize/2;
    private float StoneContactDistance
    {
        get
        {
            var body=player.GetComponent<CharacterController>();
            return .6f+body.radius*Mathf.Max(player.transform.lossyScale.x,player.transform.lossyScale.z);
        }
    }
    protected internal Bounds EnemyHitBounds(Transform enemy,float padding)
    {
        float radius=EnemyRadius(enemy),height=IsForestBoss(enemy)?ForestBossHeight:stoneArmors.Contains(enemy)?1.32f:splitters.Contains(enemy)?1.15f:splitlings.ContainsKey(enemy)?.58f:EnemySize;
        Vector3 center=enemy.position+Vector3.up*(height/2-EnemySize/2);
        return new Bounds(center,new Vector3(radius*2,height,radius*2)+Vector3.one*(padding*2));
    }
    private void InitializeStoneArmor(Transform enemy)
    {
        stoneArmors.Add(enemy); enemy.name="Stone Armored Rockling";
        // A real solid body makes this slow enemy block player movement as well as enemy spacing.
        var body=enemy.gameObject.AddComponent<CapsuleCollider>();
        body.radius=.6f/EnemySize; body.height=1.32f/EnemySize;
        body.center=Vector3.up*((.66f-EnemySize/2)/EnemySize);
    }
    private void RemoveStoneArmor(Transform enemy)
    {
        if(!stoneArmors.Remove(enemy)) return;
        var body=enemy.GetComponent<CapsuleCollider>();
        if(body!=null) { body.enabled=false; Destroy(body); }
    }
    private void ClearStoneArmors()
    {
        foreach(Transform enemy in stoneArmors)
            if(enemy!=null) { var body=enemy.GetComponent<CapsuleCollider>(); if(body!=null) body.enabled=false; }
        stoneArmors.Clear();
    }
}
