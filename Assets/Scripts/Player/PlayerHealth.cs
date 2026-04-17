using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    public Animator animator;
    public PlayerMovement playerMovement;
    public MeleeAttack meleeAttack;

    [Header("Settings")]
    public int maxHealth = 100;

    [Header("Out Of Bounds")]
    [SerializeField] private bool enableYFallDeath = true;
    [SerializeField] private float fallDeathY = -20f;
    
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    private int CurrentHealth { get; set; }

    [Networked] public bool isDead { get; set; }
    [Networked, OnChangedRender(nameof(OnDisplayNameChanged))]
    private NetworkString<_32> DisplayName { get; set; }

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

        if (Object.HasInputAuthority)
        {
            string localDisplayName = PlayerPrefs.GetString("display_name", "Player");
            RPC_SubmitDisplayName(localDisplayName);
        }

        ApplyDisplayNameToUi();

        // Initialize the UI immediately
        UpdateVisuals();
    }

    // Called by MeleeAttack.cs
    public void TakeDamageCaller(int damage)
    {
        if (isDead) return;
        RPC_RequestDamage(damage);
    }

    public void RequestOutOfBoundsDeath()
    {
        if (isDead) return;
        RPC_RequestOutOfBoundsDeath();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage)
    {
        if (isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            KillPlayer();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOutOfBoundsDeath()
    {
        if (isDead) return;
        KillPlayer();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || isDead || !enableYFallDeath)
            return;

        if (transform.position.y <= fallDeathY)
            KillPlayer();
    }

    private void KillPlayer()
    {
        if (isDead) return;
        isDead = true;
        CurrentHealth = 0;
        RPC_BroadcastDeath(Object.InputAuthority.PlayerId);
    }
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastDeath(int deadPlayerId)
    {
        if (animator) animator.SetBool("isDead", true);
        if (meleeAttack) meleeAttack.enabled = false;
        if (playerMovement) playerMovement.enabled = false;
        
        var rb = GetComponent<Rigidbody2D>();
        Debug.Log("Rigidbody: " + rb);
        if (rb) rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (NetworkHandler.Instance != null)
        {
            NetworkHandler.Instance.HandleMatchEnded(deadPlayerId);
        }
    }

    // Fusion 2 call whenever CurrentHealth changes over the network
    public void OnHealthChanged()
    {
        UpdateVisuals();
    }

    // Fusion 2 call whenever DisplayName changes over the network
    public void OnDisplayNameChanged()
    {
        ApplyDisplayNameToUi();
    }

    private void UpdateVisuals()
    {
        if (myUIBar != null)
        {
            myUIBar.SetMaxHealth(maxHealth);
            myUIBar.SetHealth(CurrentHealth);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SubmitDisplayName(string playerDisplayName)
    {
        string safeName = string.IsNullOrWhiteSpace(playerDisplayName) ? "Player" : playerDisplayName.Trim();
        if (safeName.Length > 32)
            safeName = safeName.Substring(0, 32);

        DisplayName = safeName;
    }

    private void ApplyDisplayNameToUi()
    {
        if (myUIBar == null)
            return;

        string nameToShow = DisplayName.ToString();
        if (string.IsNullOrWhiteSpace(nameToShow) && Object.HasInputAuthority)
            nameToShow = PlayerPrefs.GetString("display_name", "Player");

        if (string.IsNullOrWhiteSpace(nameToShow))
            nameToShow = "Player";

        myUIBar.SetPlayerName(nameToShow);
    }
}