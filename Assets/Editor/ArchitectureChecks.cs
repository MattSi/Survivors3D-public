using System;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class ArchitectureChecks
{
    internal static void Run(SurvivorPlayground game)
    {
        Action<bool,string> check=(ok,message)=> { if(!ok) throw new Exception("ARCHITECTURE_CHECK: "+message); };
        object Get(string name)=>GameTestAccess.Get(game,name);
        void Call(string name,params object[] args)=>GameTestAccess.Call(game,name,args);
        check(ReferenceEquals(game.Run.Equipment,Get("equipment")) && ReferenceEquals(game.Run.Session,Get("session")),"RunState is the single owner of run rules");
        check(game.Health!=null && Get("player")!=null && (int)Get("bulletDamage")==6,"Inherited scene configuration remains intact");
        Call("OpenMainMenu"); int count=game.EnemyCount;
        game.BeginRun(0); check(game.EnemyCount==count,"Menu cannot accidentally start a wave");
        Call("OpenSettings"); check((int)Get("frontScreen")==2,"Settings are controlled by flow");
        Call("CloseSettings"); check((int)Get("frontScreen")==1,"Settings return to the main menu");
        game.NewGame(); check(game.EnemyCount==0 && (bool)Get("choosingStarter"),"New game waits for weapon choice");
        game.BeginRun(3); count=game.EnemyCount; game.BeginRun(5);
        check(game.EnemyCount==count && game.Run.Equipment.HasWeapon(3) && !game.Run.Equipment.HasWeapon(5),"Duplicate start requests do not create another wave");
        float scale=Time.timeScale; Call("ToggleCodex"); check(Time.timeScale==0,"Codex pauses the run");
        game.enabled=false; check(Time.timeScale==scale && !((InputAction)Get("restartAction")).enabled,"Disabling the host restores pause and disables UI input");
        game.enabled=true; check(((InputAction)Get("restartAction")).enabled,"Re-enabling restores input");
        game.Run.Equipment.Collect(50); game.Health.TakeDamage(100);
        check(game.Run.Session.State==WaveSession.Phase.Defeat && !((PlayerController)Get("player")).enabled,"Death transitions through flow and stops movement");
        game.NewGame();
        check(game.Score==0 && game.Run.Equipment.Materials==0 && game.BulletCount==0 && game.EnemyCount==0 && game.Health.Current==100,"Restart resets run data and combat objects together");
        Call("OpenMainMenu"); game.NewGame(); game.BeginRun(5);
        check(game.EnemyCount==4 && game.Run.Equipment.HasWeapon(5),"Returning from the menu starts a clean run");
        Debug.Log("ARCHITECTURE_CHECKS_PASSED: shared run data, serialized references, menu/settings, duplicate starts, input lifecycle, defeat and restart.");
    }
}
