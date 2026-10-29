using System;

/// <summary>Enemy-owned base attributes, independent of encounter order or unlock wave.</summary>
[Serializable]
public sealed class EnemyDefinition
{
    public EnemyKind Kind;
    public int BaseHealth=10;
    public EnemyDefinition Copy()=>(EnemyDefinition)MemberwiseClone();
    public static int DefaultHealth(EnemyKind kind)
    {
        switch(kind)
        {
            case EnemyKind.Charger: return 20;
            case EnemyKind.StoneArmor: return 120;
            case EnemyKind.Bomber: return 24;
            case EnemyKind.Splitter: return 45;
            case EnemyKind.Splitling: return 14;
            case EnemyKind.Shooter: return 18;
            default: return 10;
        }
    }
    public static EnemyDefinition[] Defaults()
    {
        var rows=new EnemyDefinition[(int)EnemyKind.Shooter+1];
        for(int i=0;i<rows.Length;i++) rows[i]=new EnemyDefinition {Kind=(EnemyKind)i,BaseHealth=DefaultHealth((EnemyKind)i)};
        return rows;
    }
}
