using UnityEngine;
using Photon.Pun;
using UnityEngine.InputSystem;
using System.Collections;

public class MeleeAttack : MonoBehaviour
{
    PhotonView view;

    public Animator animator;
    private float horizontal;

    //public Animator attack_anim;
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
        view = GetComponent<PhotonView>(); 

    }

/*
    public void Update()
    {
        if(view.IsMine)
        {
            if (this.enabled == true && horizontal > 0f)
            {
                //spriteRenderer.flipX = false;
                //attackPoint.transform.position = new UnityEngine.Vector2(this.transform.position.x + 1f, this.transform.position.y);
                
                //view.RPC("OnDirectionChange_RIGHT", RpcTarget.Others);

                //attackVector = attackPoint;
            }
            else if (this.enabled == true && horizontal < 0f)
            {
                //spriteRenderer.flipX = true;
                //attackPoint.transform.position = new UnityEngine.Vector2(this.transform.position.x - 1f, this.transform.position.y);
                //view.RPC("OnDirectionChange_LEFT", RpcTarget.Others);

                attackVector = attackPointOpposite;
            }
        }
    }

    */

    private void Awake()
    {
        //Megkeresi a sajat action mapjet
        inputAsset = this.GetComponent<PlayerInput>().actions;
        player = inputAsset.FindActionMap("Player");
    }

    private void OnEnable()
    {
        //Inditaskor hozzaadja
        player.FindAction("Attack").started += Attack;
        player.Enable();
    }

    private void OnDisable()
    {
        //Eltavolitja
        player.FindAction("Attack").started -= Attack;
        player.Disable();
    }

    public void Dead(){
        player.FindAction("Attack").started -= Attack;
        player.Disable();
    }


    [PunRPC]
    public void Attack(InputAction.CallbackContext context)
    {
        if(this.enabled == true && context.performed && view.IsMine && canAttack == true)
        {
            animator.SetTrigger("Attack1");

            StartCoroutine(AttackCooldownStart2());

            //Valtozoba tarolja azt a collidert (masik jatekost), ami a koron belul van
            if(spriteRenderer.flipX == true){
                Collider2D hitEnemyOpposite = Physics2D.OverlapCircle(attackPointOpposite.position, attackRange, enemyLayer);
                PhotonView targetPhotonView = hitEnemyOpposite.GetComponent<PhotonView>();
                if(targetPhotonView != null){
                    hitEnemyOpposite.GetComponent<PlayerHealth>().TakeDamageCaller(damage);
                }
                //hitEnemyOpposite.GetComponent<TakeDmg>().TakeDamageCaller(damage);

            }else if(spriteRenderer.flipX == false){
                Collider2D hitEnemy = Physics2D.OverlapCircle(attackPoint.position, attackRange, enemyLayer);
                PhotonView targetPhotonView = hitEnemy.GetComponent<PhotonView>();
                if(targetPhotonView != null){
                    hitEnemy.GetComponent<PlayerHealth>().TakeDamageCaller(damage);
                }
                //hitEnemy.GetComponent<TakeDmg>().TakeDamageCaller(damage);
            }
        }
    }

    

    IEnumerator AttackCooldownStart2(){
        canAttack = false;
        yield return new WaitForSeconds(1);
        canAttack = true;
    }


    //Lerajzolja a kort a jobb lathatosagert az editorban
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        if (attackPointOpposite == null)
        {
            return;
        }

        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        Gizmos.DrawWireSphere(attackPointOpposite.position, attackRange);
    }
}
