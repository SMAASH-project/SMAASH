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

    private int extraJumps = 1;
    
    private NetworkCharacterController _cc;

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

    bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
}