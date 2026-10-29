using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>Read-only combat presentation data; UI cannot attack, spawn or mutate health through this interface.</summary>
internal interface ICombatView
{
    int Health { get; }
    bool BossActive { get; }
    int BossHealth { get; }
    int BossMaxHealth { get; }
    bool BossEnraged { get; }
    string BossMove { get; }
    Vector3 PlayerPosition { get; }
    Vector2 MoveInput { get; }
    float DashCooldownRemaining { get; }
    float DashReadyFlash { get; }
    bool IsDashing { get; }
    int DashCharges { get; }
    int MaxDashCharges { get; }
    PlaygroundCamera Camera { get; }
    float VictoryAge { get; }
    bool VictoryContinueRequested { get; }
    IReadOnlyList<CombatController.FloatingScore> FloatingScores { get; }
    IReadOnlyList<CombatController.FloatingScore> FloatingDamage { get; }
    IReadOnlyList<CombatController.LootFlight> LootFlights { get; }
    float WeaponRange(int id);
    int WeaponDamage(int id);
}
