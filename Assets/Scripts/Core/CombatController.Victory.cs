using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class CombatController
{
    internal const int WaveCompletionMaterials=5;
    bool victoryRewardPending;
    protected internal const float PickupLifetime=6f;
    protected internal const float VictoryAnimationDuration=1.5f;
    protected internal float victoryAge;
    protected internal sealed class LootFlight { public Vector2 Start; public float Age,Duration,Delay; public bool ScoreOnly; }
    protected internal readonly List<LootFlight> lootFlights=new List<LootFlight>();
    protected internal bool victoryContinueRequested;
    protected internal void CullExpiredPickups()
    {
        for(int i=pickups.Count-1;i>=0;i--)
            if(Time.time>=pickups[i].ExpiresAt)
            { Destroy(pickups[i].Transform.gameObject); pickups.RemoveAt(i); }
    }
    protected internal void BeginVictoryLoot()
    {
        CullExpiredPickups(); lootFlights.Clear(); victoryContinueRequested=false;
        Camera camera=viewCamera!=null?viewCamera.GetComponent<Camera>():Camera.main;
        victoryAge=0; victoryRewardPending=true;
        if(playerAvatar!=null) playerAvatar.BeginCelebration(camera!=null
            ? camera.transform.position-player.transform.position : Vector3.back);
        foreach(Pickup pickup in pickups)
        {
            Vector3 point=camera!=null?camera.WorldToViewportPoint(pickup.Transform.position):new Vector3(.5f,.5f,1);
            Vector2 start=new Vector2(Mathf.Clamp01(point.x),Mathf.Clamp01(1-point.y));
            if(point.z<0) start=new Vector2(start.x,0.98f);
            lootFlights.Add(new LootFlight { Start=start,ScoreOnly=pickup.ScoreOnly,Duration=.65f+Vector2.Distance(start,new Vector2(.9f,.05f))*.4f,Delay=Mathf.Min(1.2f,lootFlights.Count*.035f) });
            Destroy(pickup.Transform.gameObject);
        }
        pickups.Clear();
    }
    protected internal void UpdateVictoryLoot(float dt)
    {
        victoryAge+=dt;
        if(playerAvatar!=null) playerAvatar.SetCelebrationTime(victoryAge);
        for(int i=lootFlights.Count-1;i>=0;i--)
        {
            LootFlight flight=lootFlights[i]; flight.Age+=dt;
            if(flight.Age<flight.Delay+flight.Duration) continue;
            if(flight.ScoreOnly) { lootScore++; GameAudio.Play(AudioCue.Pickup); }
            else CollectMaterial(1,false);
            lootFlights.RemoveAt(i);
        }
        if(victoryRewardPending && lootFlights.Count==0 && victoryAge>=VictoryAnimationDuration)
        {
            victoryRewardPending=false;
            equipment.Collect(WaveCompletionMaterials);
            GameAudio.Play(AudioCue.Purchase);
        }
        if(victoryContinueRequested && lootFlights.Count==0 && victoryAge>=VictoryAnimationDuration) SettlementReady?.Invoke();
    }

}
