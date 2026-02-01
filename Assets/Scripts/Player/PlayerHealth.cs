/*
using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    public Animator animator;
    public PlayerMovement playerMovement;
    public MeleeAttack meleeAttack;
    public ButtonCooldowns buttonCooldowns;

    public bool isDead = false;
    public HealthBar healthBar;
    public int maxHealth = 100;

    // Fusion 2: use OnChangedRender for render-time change detection
    // (OnChanged was removed in Fusion 2)
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    private int CurrentHealth { get; set; }

    public override void Spawned()
    {
        // Map UI: local player -> HealthBar1, opponent -> HealthBar2
        bool isLocal = Object.HasInputAuthority;
        string barName = isLocal ? "HealthBar1" : "HealthBar2";
        var barGO = GameObject.Find(barName);

        if (barGO != null)
        {
            healthBar = barGO.GetComponent<HealthBar>();
            ApplyUI(); // Ensure UI shows correct values immediately
        }
        else
        {
            Debug.LogWarning($"PlayerHealth: Could not find {barName} in scene.");
        }

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }
        else
        {
            // OnChangedRender is not invoked on initial spawn for clients,
            // so call ApplyUI here to initialize visuals.
            ApplyUI();
        }
    }

    public void TakeDamageCaller(int damage)
    {
        if (isDead) return;
        RPC_RequestDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage, RpcInfo info = default)
    {
        if (isDead) return;

        int newHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, damage));
        CurrentHealth = newHealth;

        if (newHealth <= 0)
        {
            RPC_ApplyDeath();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ApplyDeath()
    {
        if (isDead) return;

        isDead = true;
        if (animator) animator.SetBool("isDead", true);

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (meleeAttack) meleeAttack.enabled = false;
        if (playerMovement) playerMovement.enabled = false;

        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Player"),
            LayerMask.NameToLayer("Player"),
            true
        );

        ApplyUI();
    }

    // This method is triggered by Fusion 2 when CurrentHealth changes (Render-time).
    // It must be an instance (non-static) void method — matches OnChangedRender docs.
    public void OnHealthChanged()
    {
        ApplyUI();
    }

    private void ApplyUI()
    {
        if (healthBar == null) return;

        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(CurrentHealth);
    }
}

*/

using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    public Animator animator;
    public PlayerMovement playerMovement;
    public MeleeAttack meleeAttack;

    [Header("Settings")]
    public int maxHealth = 100;
    
    // The "Source of Truth" for health
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    private int CurrentHealth { get; set; }

    [Networked] public bool isDead { get; set; }

    private HealthBar myUIBar;

    public override void Spawned()
    {
        // ASSIGNMENT LOGIC:
        // PlayerId is unique to each player (1, 2, 3...). 
        // We use this to decide which fixed UI bar to control.
        if (UIManager.Instance != null)
        {
            // Host is usually 1 (Odd), Client is usually 2 (Even)
            if (Object.InputAuthority.PlayerId % 2 != 0)
                myUIBar = UIManager.Instance.healthBar1;
            else
                myUIBar = UIManager.Instance.healthBar2;
        }

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            isDead = false;
        }

        // Initialize the UI immediately
        UpdateVisuals();
    }

    // Called by MeleeAttack.cs
    public void TakeDamageCaller(int damage)
    {
        if (isDead) return;
        RPC_RequestDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage)
    {
        if (isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            isDead = true;
            RPC_BroadcastDeath();
        }
    }
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDeath()
    {
        if (animator) animator.SetBool("isDead", true);
        if (meleeAttack) meleeAttack.enabled = false;
        if (playerMovement) playerMovement.enabled = false;
        
        var rb = GetComponent<Rigidbody2D>();
        Debug.Log("Rigidbody: " + rb);
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // Fusion 2 call whenever CurrentHealth changes over the network
    public void OnHealthChanged()
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (myUIBar != null)
        {
            myUIBar.SetMaxHealth(maxHealth);
            myUIBar.SetHealth(CurrentHealth);
        }
    }
}