using UnityEngine;
using Fusion;

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
    public float jumpingPower = 12f;
    public int extraJumpValue = 1;
    public bool isCountingDown;

    [Networked] private int extraJumps { get; set; }

    public override void Spawned()
    {
        rb.simulated = true;
        if (Object.HasInputAuthority) extraJumps = extraJumpValue;
    }

    public override void FixedUpdateNetwork()
    {
        // GetInput only works on the Host and the Client who owns this player
        if (GetInput(out NetworkInputData data) && !isCountingDown)
        {
            // 1. Movement
            rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);

            // 2. Jumping
            if (data.jumpPressed)
            {
                if (IsGrounded()) { Jump(); extraJumps = extraJumpValue; }
                else if (extraJumps > 0) { Jump(); extraJumps--; }
            }
        }
        ApplyVisuals();
    }

    void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpingPower);

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

    bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
}