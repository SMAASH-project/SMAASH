using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
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

    // owner state
    private Vector2 moveInput;
    private bool isGrounded;
    private int extraJumps;
    public bool isDead;
    public bool isCountingDown;
    public bool IsJumping { get; private set; }

    private PlayerInput playerInput;
    private Vector3 lastPosForRemoteAnim;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
    }

    public override void Spawned()
    {
        isDead = false;
        extraJumps = extraJumpValue;
        IsJumping = false;

        // Owner simulates; remotes are visuals only (use NetworkTransform for sync)
        bool isOwner = Object.HasInputAuthority;

        rb.simulated = isOwner;
        rb.isKinematic = !isOwner;
        rb.interpolation = isOwner ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
        rb.collisionDetectionMode = isOwner ? CollisionDetectionMode2D.Continuous : CollisionDetectionMode2D.Discrete;

        if (animator) animator.applyRootMotion = false;

        // Enable input only on the local player
        if (playerInput) playerInput.enabled = isOwner;

        SetOwnedCollisionState(isOwner);

        lastPosForRemoteAnim = transform.position;
    }

    void Update()
    {
        if (!Object) return;

        if (Object.HasInputAuthority)
        {
            // local animation speed is driven by rb velocity set in OnMove
            // other animation params updated in FixedUpdate
        }
        else
        {
            // Approximate remote animation speed from transform delta (NetworkTransform drives this)
            float horiz = (transform.position.x - lastPosForRemoteAnim.x) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (animator) animator.SetFloat("speed", Mathf.Abs(horiz));
            lastPosForRemoteAnim = transform.position;
        }
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
}