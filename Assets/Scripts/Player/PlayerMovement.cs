using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Settings")]
    public float speed = 8f;
    public float jumpingPower = 20f;
    //public int extraJumpValue = 1;
    public bool isCountingDown;


    [Networked] private int extraJumps { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork()
    {
        // GetInput retrieves data from the Client to the Host automatically
        if (GetInput(out NetworkInputData data))
        {
            rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);

            if (data.jumpPressed)
            {
                Debug.Log("Jump pressed detected in FixedUpdateNetwork");
                /*
                if (IsGrounded() && extraJumps == 1)
                {
                    Jump();
                    extraJumps = 1;
                }
                else if (!IsGrounded() && extraJumps == 1)
                {
                    Jump();
                    extraJumps = 0;
                }
                */
                Jump();
            }
        }
        ApplyVisuals();
    }

    void Jump()
    {
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            extraJumps = 1;
        }else if (extraJumps > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            extraJumps = 0;
        }
    } 

    void ApplyVisuals()
    {
        if (animator)
        {
            animator.SetFloat("speed", Mathf.Abs(rb.velocity.x));
            animator.SetBool("isJumping", !IsGrounded());
        }
        // Flip sprite based on movement direction
        if (rb.velocity.x > 0.1f) spriteRenderer.flipX = false;
        else if (rb.velocity.x < -0.1f) spriteRenderer.flipX = true;
    }

    void FixedUpdate()
    {
        if (Object.HasInputAuthority && !isDead && !isCountingDown)
        {
            UpdateAnimations();
            FlipCharacterLocal();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!Object.HasInputAuthority || isDead || isCountingDown) return;

        moveInput = context.ReadValue<Vector2>();
        float horizontal = moveInput.x * speed;
        rb.velocity = new Vector2(horizontal, rb.velocity.y);
        if (animator) animator.SetFloat("speed", Mathf.Abs(horizontal));
        Debug.Log("Horizontal Input: " + horizontal); 
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!Object.HasInputAuthority || isDead || isCountingDown) return;

        if (IsGrounded()) extraJumps = extraJumpValue;

        if (context.performed && (extraJumps > 0 || IsGrounded()))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            if (!IsGrounded()) extraJumps--;
            IsJumping = true;
        }
        Debug.Log("Jump Input: " + context.performed);
    }

    void FlipCharacterLocal()
    {
        if (moveInput.x > 0.1f) spriteRenderer.flipX = false;
        else if (moveInput.x < -0.1f) spriteRenderer.flipX = true;
    }

    void UpdateAnimations()
    {
        isGrounded = IsGrounded();
        if (animator) animator.SetBool("isJumping", !isGrounded);
        if (isGrounded) IsJumping = false;
    }

    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    // Disable non-trigger colliders on remotes so players never push each other
    void SetOwnedCollisionState(bool enableSolid)
    {
        var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols)
        {
            if (c.isTrigger) { c.enabled = true; } // keep hitboxes
            else c.enabled = enableSolid;          // only the owner has solid colliders
        }
    }

    // Apply knockback on the owner
    [Rpc(RpcSources.All, RpcTargets.InputAuthority)]
    public void RPC_ApplyKnockback(Vector2 impulse)
    {
        if (!Object.HasInputAuthority) return;
        rb.velocity = new Vector2(0, rb.velocity.y);
        rb.AddForce(impulse, ForceMode2D.Impulse);
    }

    */
}