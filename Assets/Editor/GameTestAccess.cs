using System;
using System.Reflection;

/// <summary>Test-only access across the explicit runtime modules, not a runtime service locator.</summary>
internal static class GameTestAccess
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private static FieldInfo Field(Type type,string name)
    {
        for(;type!=null;type=type.BaseType) { var field=type.GetField(name,Flags); if(field!=null) return field; }
        return null;
    }
    private static object[] Targets(SurvivorPlayground game)=>new object[] {game,game.Flow,typeof(SurvivorPlayground).GetProperty("UI",Flags).GetValue(game),game.Run};
    internal static object Get(SurvivorPlayground game,string name)
    {
        foreach(object target in Targets(game))
        {
            var type=target.GetType(); var field=Field(type,name); if(field!=null) return field.GetValue(target);
            var property=type.GetProperty(name,Flags); if(property!=null) return property.GetValue(target);
        }
        throw new MissingMemberException(name);
    }
    internal static void Set(SurvivorPlayground game,string name,object value)
    {
        foreach(object target in Targets(game))
        {
            var type=target.GetType(); string member=target is RunState?char.ToUpperInvariant(name[0])+name.Substring(1):name;
            var field=Field(type,member); if(field!=null) { field.SetValue(target,value); return; }
            var property=type.GetProperty(member,Flags); if(property!=null && property.CanWrite) { property.SetValue(target,value); return; }
        }
        throw new MissingMemberException(name);
    }
    internal static void Call(SurvivorPlayground game,string name,params object[] args)
    {
        foreach(object target in Targets(game))
        {
            var method=target.GetType().GetMethod(name,Flags);
            if(method!=null) { method.Invoke(target,args); return; }
        }
        throw new MissingMethodException(name);
    }
}
