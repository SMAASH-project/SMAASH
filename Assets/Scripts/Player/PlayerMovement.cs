using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Button jumpButton;

    [Header("Settings")]
    public float speed = 8f;
    public float jumpingPower = 20f;
    public bool isCountingDown;
    [Min(0)] public int maxAirJumps = 1;


    [Networked] private int extraJumps { get; set; }

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        localInputHandler = GetComponent<LocalInputHandler>();

        if (Object.HasInputAuthority && (jumpButtonOwner == null || jumpButtonOwner == this))
        {
            jumpButtonOwner = this;
            isJumpButtonOwner = true;
        }

        extraJumps = maxAirJumps;

        SetupJumpButton();
    }

    void SetupJumpButton()
    {
        if (!Object.HasInputAuthority || !isJumpButtonOwner)
            return;

        if (jumpButton == null)
        {
            GameObject jumpObject = GameObject.Find("Jump");
            if (jumpObject != null)
                jumpButton = jumpObject.GetComponent<Button>();
        }

        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveListener(OnJumpButtonPressed);

            jumpButtonRelay = jumpButton.GetComponent<JumpButtonPressRelay>();
            if (jumpButtonRelay == null)
                jumpButtonRelay = jumpButton.gameObject.AddComponent<JumpButtonPressRelay>();

            jumpButtonRelay.Pressed -= OnJumpButtonPressed;
            jumpButtonRelay.Pressed += OnJumpButtonPressed;
            jumpButtonRelay.Released -= OnJumpButtonReleased;
            jumpButtonRelay.Released += OnJumpButtonReleased;
        }
    }

    void OnDestroy()
    {
        if (jumpButtonOwner == this)
            jumpButtonOwner = null;

        if (jumpButtonRelay != null)
        {
            jumpButtonRelay.Pressed -= OnJumpButtonPressed;
            jumpButtonRelay.Released -= OnJumpButtonReleased;
        }

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpButtonPressed);
    }

    public override void FixedUpdateNetwork()
    {
        if(isCountingDown) return;

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null && playerHealth.isDead)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            UpdateNetworkedAnimationValues();
            return;
        }

        if (!Object.HasStateAuthority)
            return;

        if (GetInput(out NetworkInputData data))
        {
            rb.velocity = new Vector2(data.moveInput.x * speed, rb.velocity.y);

            if (data.jumpPressed)
                Jump();
        }

        UpdateNetworkedAnimationValues();
    }

    void OnJumpButtonPressed()
    {
        if (!Object.HasInputAuthority || !isJumpButtonOwner)
            return;

        if (isJumpButtonHeld)
            return;

        isJumpButtonHeld = true;

        if (localInputHandler != null)
            localInputHandler.QueueJumpFromUI();
    }

    void OnJumpButtonReleased()
    {
        isJumpButtonHeld = false;
    }

    void Jump()
    {
        if (IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            extraJumps = maxAirJumps;
            return;
        }

        if (extraJumps > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            extraJumps--;
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