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
    public bool isCountingDown;

    private int extraJumps = 1;
    
    // Network synced animation states
    [Networked] public bool IsFacingLeft { get; set; }
    [Networked] public float NetworkSpeed { get; set; }
    [Networked] public bool NetworkIsJumping { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork()
    {
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.isDead)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            UpdateNetworkedAnimationValues();
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);

            if (data.jumpPressed)
            {
                Debug.Log("Jump pressed detected in FixedUpdateNetwork");
                Jump();
            }
        }
        UpdateNetworkedAnimationValues();
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

    void UpdateNetworkedAnimationValues()
    {
        // Only state authority updates networked values
        if (Object.HasStateAuthority)
        {
            NetworkSpeed = Mathf.Abs(rb.velocity.x);
            NetworkIsJumping = !IsGrounded();
            
            if (rb.velocity.x > 0.1f)
                IsFacingLeft = false;
            else if (rb.velocity.x < -0.1f)
                IsFacingLeft = true;
        }
    }

    // Render is called on all clients/host for rendering, synced with networked values
    public override void Render()
    {
        // Apply animations from networked values (visible to all)
        if (animator)
        {
            animator.SetFloat("speed", NetworkSpeed);
            animator.SetBool("isJumping", NetworkIsJumping);
        }
        
        spriteRenderer.flipX = IsFacingLeft;
    }

    bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
}