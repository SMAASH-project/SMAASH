using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class HealthBar : NetworkBehaviour
{
    public Slider slider;
    public Gradient gradient;
    public Image fill;

    public bool isTaken = false;

    // Keep these as plain Networked properties (auto-properties) so Fusion can back them.
    [Networked] public int MaxHealth { get; private set; }
    [Networked] public int CurrentHealth { get; private set; }

    // Local cached values for change detection fallback
    private int _lastMaxHealth = int.MinValue;
    private int _lastCurrentHealth = int.MinValue;

    // Public API (call these from gameplay code)
    public void SetMaxHealth(int health)
    {
        if (Object == null || !Object.IsValid) // not networked (e.g., in editor preview)
        {
            ApplyLocal(health, health);
            return;
        }

        if (Object.HasStateAuthority)
        {
            MaxHealth = health;
            CurrentHealth = health;
        }
        else if (Object.HasInputAuthority)
        {
            RPC_RequestSetMaxHealth(health);
        }
        else
        {
            Debug.LogWarning("HealthBar: No authority to set max health.");
        }
    }

    public void SetHealth(int health)
    {
        if (Object == null || !Object.IsValid)
        {
            ApplyLocal(MaxHealth, health);
            return;
        }

        if (Object.HasStateAuthority)
        {
            CurrentHealth = health;
        }
        else if (Object.HasInputAuthority)
        {
            RPC_RequestSetHealth(health);
        }
        else
        {
            Debug.LogWarning("HealthBar: No authority to set health.");
        }
    }

    // RPCs to request state changes from the StateAuthority
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetMaxHealth(int health)
    {
        MaxHealth = health;
        CurrentHealth = health;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetHealth(int health)
    {
        CurrentHealth = health;
    }

    // Version-safe change detection: runs in the network loop and updates UI when a value changed.
    public override void FixedUpdateNetwork()
    {
        // Only act when the network object exists
        if (Object == null || !Object.IsValid)
            return;

        if (MaxHealth != _lastMaxHealth || CurrentHealth != _lastCurrentHealth)
        {
            _lastMaxHealth = MaxHealth;
            _lastCurrentHealth = CurrentHealth;
            ApplyUI();
        }
    }

    private void OnEnable()
    {
        ApplyUI(); // ensure UI matches initial values if spawned late
    }

    private void ApplyLocal(int max, int current)
    {
        if (slider) slider.maxValue = max;
        if (slider) slider.value = current;
        if (fill && gradient != null) fill.color = gradient.Evaluate(slider ? slider.normalizedValue : 1f);
        isTaken = max > 0;
    }

    private void ApplyUI()
    {
        ApplyLocal(MaxHealth, CurrentHealth);
    }

    // (Optional) If you still have the old OnMaxChanged/OnCurrentChanged methods, it's safe to leave them
    // but they're not used by this fallback approach.
}