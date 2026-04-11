using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Fusion;

public class MeleeAttack : NetworkBehaviour
{
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform attackPoint;
    public Transform attackPointOpposite;
    
    public float attackRange = 1f;
    public LayerMask enemyLayer;
    public int damage = 20;

    private InputActionAsset inputAsset;
    private InputActionMap playerMap;
    private bool canAttack = true;

    // Network synced attack state
    [Networked] public bool IsAttacking { get; set; }

    private void Awake()
    {
        inputAsset = GetComponent<PlayerInput>()?.actions;
        playerMap = inputAsset?.FindActionMap("Player");
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority && playerMap != null)
        {
            playerMap.FindAction("Attack").started += OnAttackInput;
            playerMap.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerMap != null)
        {
            playerMap.FindAction("Attack").started -= OnAttackInput;
            playerMap.Disable();
        }
    }

    private void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!canAttack) return;
        Debug.Log("Attack input received. Sending RPC to perform attack.");

        // Send RPC to state authority to process attack
        RPC_PerformAttack(spriteRenderer.flipX);
        
        StartCoroutine(AttackCooldown());
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAttack(bool isFacingLeft)
    {
        // Determine which attack point to use based on direction
        Transform activePoint = isFacingLeft ? attackPointOpposite : attackPoint;
        
        // Find enemies in range
        Collider2D hitEnemy = Physics2D.OverlapCircle(activePoint.position, attackRange, enemyLayer);
        Debug.Log("Melee attack hit: " + (hitEnemy != null ? hitEnemy.gameObject.name : "nothing"));
        
        if (hitEnemy != null)
        {
            // Send damage request to the health script
            if (hitEnemy.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamageCaller(damage);
            }
        }

        // Broadcast attack to all clients for animation
        RPC_BroadcastAttack();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastAttack()
    {
        StartCoroutine(PlayAttackAnimation());
    }

    private IEnumerator PlayAttackAnimation()
    {
        if (animator == null)
            yield break;

        animator.SetBool("isJumping", false);
        animator.ResetTrigger("Attack1");
        animator.SetTrigger("Attack1");

        // Let the Animator state machine handle returning to idle via exit time.
        yield return null;
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(0.5f); // Local cooldown
        canAttack = true;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null || attackPointOpposite == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.DrawWireSphere(attackPointOpposite.position, attackRange);
    }
}