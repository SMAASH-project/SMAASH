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

    private int extraJumps;
    private bool isJumpButtonHeld;
    private bool isJumpButtonOwner;
    private JumpButtonPressRelay jumpButtonRelay;
    private LocalInputHandler localInputHandler;
    private static PlayerMovement jumpButtonOwner;
    
    // Network synced animation states
    [Networked] public bool IsFacingLeft { get; set; }
    [Networked] public float NetworkSpeed { get; set; }
    [Networked] public bool NetworkIsJumping { get; set; }

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

    bool IsGrounded() => Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

    private class JumpButtonPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event System.Action Pressed;
        public event System.Action Released;

        public void OnPointerDown(PointerEventData eventData)
        {
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Released?.Invoke();
        }
    }
}