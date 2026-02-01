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

    private void Awake()
    {
        inputAsset = GetComponent<PlayerInput>()?.actions;
        playerMap = inputAsset?.FindActionMap("Player");
    }

    public override void Spawned()
    {
        // Only the player sitting at the keyboard should trigger the Attack input
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

        // Trigger local animation immediately for responsiveness
        animator.SetBool("isAttacking", true);
        animator.SetBool("isJumping", false);
        animator.SetTrigger("Attack1");

        // Determine which attack point to use based on sprite direction
        Transform activePoint = spriteRenderer.flipX ? attackPointOpposite : attackPoint;
        
        // Find enemies in range
        Collider2D hitEnemy = Physics2D.OverlapCircle(activePoint.position, attackRange, enemyLayer);
        Debug.Log("Melee attack hit: " + (hitEnemy != null ? hitEnemy.gameObject.name : "nothing"));
        
        if (hitEnemy != null)
        {
            // Send damage request to the health script
            if (hitEnemy.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamageCaller(damage);
                animator.SetBool("isAttacking", true);
                animator.SetTrigger("Attack1");
            }
        }

        StartCoroutine(AttackCooldown());
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        
        // Wait one frame for the animator to transition to the attack state
        yield return null;
        
        animator.ResetTrigger("Attack1");
        
        // Get the correct animation length.
        // If we are in transition, the current state info might still be 'Idle'.
        // We need the 'Next' state info to get the actual Attack clip length.
        float animLength = 0f;
        if (animator.IsInTransition(0))
        {
            animLength = animator.GetNextAnimatorStateInfo(0).length;
        }
        else
        {
            animLength = animator.GetCurrentAnimatorStateInfo(0).length;
        }
        
        // Wait for the actual animation length
        yield return new WaitForSeconds(animLength);
        
        animator.SetBool("isAttacking", false);
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