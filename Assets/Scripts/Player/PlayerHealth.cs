using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    public const int MaxHealth = 100;
    public int Current { get; private set; } = MaxHealth;
    public bool IsDead => Current == 0;
    public bool IsHitInvulnerable => Time.time < vulnerableAt;
    public bool IsInvulnerable => IsHitInvulnerable || (controller!=null && controller.IsDashProtected);
    private PlayerController controller;
    private void Awake()=>controller=GetComponent<PlayerController>();
    public event Action Died;
    public event Action<int> Damaged;
    public int ShieldCharges { get; set; }
    public void Heal(int amount) { if(!IsDead && amount>0) Current=Mathf.Min(MaxHealth,Current+amount); }
    private float vulnerableAt;

    public bool TakeDamage(int amount)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return false;
        if(ShieldCharges>0) { ShieldCharges--; vulnerableAt=Time.time+0.7f; return false; }
        int lost=Mathf.Min(Current,amount);
        Current -= lost;
        vulnerableAt = Time.time + 0.7f;
        Damaged?.Invoke(lost);
        if (IsDead) Died?.Invoke();
        return true;
    }

    public void ResetHealth()
    {
        ShieldCharges=0;
        Current = MaxHealth;
        vulnerableAt = 0f;
    }
}
