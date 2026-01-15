/*

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Fusion;

public class MeleeAttack : NetworkBehaviour
{
    public Animator animator;
    public GameObject attackBtn;

    public Transform attackPoint;
    public Transform attackPointOpposite;
    public float attackRange = 1f;
    public LayerMask enemyLayer;
    public SpriteRenderer spriteRenderer;

    public int attackDamage = 40;
    public int attackNum;

    private InputActionAsset inputAsset;
    private InputActionMap player;

    int damage = 20;
    public bool canAttack = true;
    public bool isDead = false;

    public void Start()
    {
        attackNum = 1;
    }

    private void Awake()
    {
        inputAsset = GetComponent<PlayerInput>()?.actions;
        player = inputAsset?.FindActionMap("Player");
    }

    public override void Spawned()
    {
        // Subscribe input only for local player
        if (Object.HasInputAuthority && player != null)
        {
            player.FindAction("Attack").started += Attack;
            player.Enable();
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.FindAction("Attack").started -= Attack;
            player.Disable();
        }
    }

    public void Dead()
    {
        if (player != null)
        {
            player.FindAction("Attack").started -= Attack;
            player.Disable();
        }
    }

    public void Attack(InputAction.CallbackContext context)
    {
        if (this.enabled && context.performed && Object.HasInputAuthority && canAttack)
        {
            animator.SetBool("isJumping", false);
            animator.SetTrigger("Attack1");

            StartCoroutine(AttackCooldownStart2());

            if (spriteRenderer.flipX)
            {
                Collider2D hitEnemyOpposite = Physics2D.OverlapCircle(attackPointOpposite.position, attackRange, enemyLayer);
                if (hitEnemyOpposite != null && hitEnemyOpposite.TryGetComponent<NetworkObject>(out _))
                {
                    hitEnemyOpposite.GetComponent<PlayerHealth>()?.TakeDamageCaller(damage);
                }
            }
            else
            {
                Collider2D hitEnemy = Physics2D.OverlapCircle(attackPoint.position, attackRange, enemyLayer);
                if (hitEnemy != null && hitEnemy.TryGetComponent<NetworkObject>(out _))
                {
                    hitEnemy.GetComponent<PlayerHealth>()?.TakeDamageCaller(damage);
                }
            }
        }
    }

    IEnumerator AttackCooldownStart2()
    {
        yield return null;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;

        yield return new WaitForSeconds(clipLength);

        canAttack = true;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null || attackPointOpposite == null) return;

        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.DrawWireSphere(attackPointOpposite.position, attackRange);
    }
*/

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
        
        if (hitEnemy != null)
        {
            // Send damage request to the health script
            if (hitEnemy.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamageCaller(damage);
                animator.SetBool("isAttacking", false);
                animator.ResetTrigger("Attack1");
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