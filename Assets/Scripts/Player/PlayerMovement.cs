using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerMovement : MonoBehaviour
{
    PhotonView view;

    public Rigidbody2D rb;
    public bool isDead;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public GameObject attackPoint;
    public GameObject attackPointOpposite;

    public Animator animator;
    public Joystick joystick;
    public SpriteRenderer spriteRenderer;

    private float horizontal;
    private float speed = 8f;
    private float jumpingPower = 10f;
    private int extraJumps;
    private int extraJumpValue = 2;
    bool isDead;


    void Start()
    {
        isDead = false;
        extraJumps = extraJumpValue;
        joystick = GameObject.Find("Floating Joystick").GetComponent<Joystick>();
        view = GetComponent<PhotonView>();
        isDead = false;
    }

    void FixedUpdate()
    {
        if(view.IsMine && isDead == false){
            horizontalMove();
            flipCharacter();
            checkJumpAnimation();
            //updateAttackPoints();
        }
    }

    void horizontalMove(){
        
        if(joystick.Horizontal >= .2f)
        {
            horizontal = speed;
        }
        else if(joystick.Horizontal <= -.2f)
        {
            horizontal = -speed;
        }
        else
        {
            horizontal = 0f;
        }

        rb.velocity = new Vector2(horizontal, rb.velocity.y);
        animator.SetFloat("speed", Math.Abs(horizontal));
    }

    void flipCharacter(){

        if (horizontal > 0f)
        {
            spriteRenderer.flipX = false;
            view.RPC("OnDirectionChange_RIGHT", RpcTarget.Others);
        }
        else if (horizontal < 0f)
        {
            spriteRenderer.flipX = true;
            view.RPC("OnDirectionChange_LEFT", RpcTarget.Others);
        }
    }

    void checkJumpAnimation(){

        if (!IsGrounded())
        {
            animator.SetBool("isJumping", true);
        }
        else
        {
            animator.SetBool("isJumping", false);
        }
    }
    [PunRPC]
    void OnDirectionChange_LEFT()
    {
        spriteRenderer.flipX = true;
    }
    [PunRPC]
    void OnDirectionChange_RIGHT()
    {
        spriteRenderer.flipX = false;
    }  

    public void Jump(InputAction.CallbackContext context)
    {
        if(view.IsMine && isDead == false)
        {
            if(IsGrounded())
            {
                extraJumps = extraJumpValue;
            }

            if(context.performed && extraJumps > 0)
            {
                rb.velocity = Vector2.up * jumpingPower;
                extraJumps--;
            }
            else if(context.performed && extraJumps == 0 && IsGrounded())
            {
                rb.velocity = Vector2.up * jumpingPower;
            }
        }
    }

    public void Die(){
        isDead = true;
        this.enabled = false;
        animator.SetBool("isDead", true);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer); 
    }

    */
}