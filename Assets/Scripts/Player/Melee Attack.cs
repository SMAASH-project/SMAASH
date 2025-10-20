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
}
