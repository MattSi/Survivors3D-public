using UnityEngine;

[CreateAssetMenu(menuName="Survivors/Wave Configuration",fileName="WaveConfiguration")]
public sealed class WaveConfiguration : ScriptableObject
{
    public WaveRules Rules=new WaveRules();
    public WaveRules Snapshot()=>(Rules??new WaveRules()).Copy();
}
