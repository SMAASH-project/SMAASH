using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class PlayerMovement : MonoBehaviour
{
    PhotonView view;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public GameObject attackPoint;
    public GameObject attackPointOpposite;

    [Header("Movement")]
    public float speed = 8f;
    public float jumpingPower = 10f;
    public int extraJumpValue = 2;

    private Vector2 moveInput;
    private bool isJumpPressed;
    private bool isGrounded;
    private int extraJumps;
    public bool isDead;

    void Start()
    {
        view = GetComponent<PhotonView>();
        isDead = false;
        extraJumps = extraJumpValue;

        // For WebGL: detect focus
        //Application.focusChanged += OnAppFocusChanged;

        Debug.Log(">>> PlayerMovement START - New Input Version <<<");
    }

    void FixedUpdate()
    {
        if (view.IsMine && !isDead)
        {
            MovePlayer();
            FlipCharacter();
            UpdateAnimations();
        }
    }

    // Called automatically by new Input System when Move action is triggered
    public void OnMove(InputAction.CallbackContext context)
    {
         // Ignore input if window not focused
        moveInput = context.ReadValue<Vector2>();
    }

    // Called automatically by new Input System when Jump action is triggered
    public void OnJump(InputAction.CallbackContext context)
    {
        if (view.IsMine && !isDead)
        {
            if (IsGrounded()) extraJumps = extraJumpValue;

            if (context.performed && extraJumps > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
                extraJumps--;
            }
            else if (context.performed && extraJumps == 0 && IsGrounded())
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            }
        }
    }

    void MovePlayer()
    {
        float horizontal = moveInput.x * speed;
        rb.velocity = new Vector2(horizontal, rb.velocity.y);
        animator.SetFloat("speed", Math.Abs(horizontal));
    }

    void FlipCharacter()
    {
        if (moveInput.x > 0.1f)
        {
            spriteRenderer.flipX = false;
            view.RPC("OnDirectionChange_RIGHT", RpcTarget.Others);
        }
        else if (moveInput.x < -0.1f)
        {
            spriteRenderer.flipX = true;
            view.RPC("OnDirectionChange_LEFT", RpcTarget.Others);
        }
    }

    void UpdateAnimations()
    {
        isGrounded = IsGrounded();
        animator.SetBool("isJumping", !isGrounded);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
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
}