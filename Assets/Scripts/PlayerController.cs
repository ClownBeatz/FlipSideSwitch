// PlayerController.cs
// AI-ASSISTED: Generated with Cursor, tuned manually by developer.
// Attach to: ARC GameObject
// Requires: Rigidbody2D, CapsuleCollider2D, PlayerInput on the same GameObject
//
// CHANGES:
//   - Added ApplyBoost() / the boost coroutine now lives here, not on BoostOrb.
//     This means Destroy(boostOrb) can no longer cancel the timer prematurely.
//   - Boost outline system unchanged.

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 6f;

    [Header("Jumping")]
    [SerializeField] protected float jumpForce = 11f;
    [SerializeField] protected float gravityScale = 2.8f;
    [SerializeField] protected float fallMultiplier = 1.5f;
    [SerializeField] protected float coyoteTime = 0.15f;
    [SerializeField] protected float jumpBufferTime = 0.1f;

    [Header("Ground Detection")]
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected float groundCheckDistance = 0.05f;

    [Header("Boost Outline")]
    [SerializeField] private float outlineOffsetWorld = 0.055f;
    [SerializeField] private float outlinePulseSpeed = 4.5f;
    [SerializeField] private float outlineAlphaMin = 0.45f;
    [SerializeField] private float outlineAlphaMax = 1.00f;

    // ------------------------------------------------------------------ //
    // Components
    // ------------------------------------------------------------------ //
    protected Rigidbody2D rb;
    protected CapsuleCollider2D capsuleCollider;
    private Animator animator;
    protected SpriteRenderer spriteRenderer;

    // ------------------------------------------------------------------ //
    // State
    // ------------------------------------------------------------------ //
    protected Vector2 moveInput;
    protected bool isGrounded;
    protected float coyoteTimer;
    protected float jumpBufferTimer;
    private bool isActive = false;

    // Boost outline
    private SpriteRenderer[] outlineRenderers;
    private bool boostOutlineActive;
    private Color outlineColor = Color.white;

    // Active boost coroutine — stored so a new orb can cancel a running boost
    private Coroutine boostCoroutine;

    private static readonly Vector2[] OutlineDirs =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    // ------------------------------------------------------------------ //
    // Unity lifecycle
    // ------------------------------------------------------------------ //
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        InitBoostOutline();
    }

    protected virtual void Update()
    {
        if (!isActive) return;

        CheckGrounded();
        HandleCoyoteTime();
        HandleJumpBuffer();
        ApplyFallGravity();

        if (animator != null)
        {
            animator.SetBool("isRunning", Mathf.Abs(rb.linearVelocity.x) > 0.1f);
            animator.SetBool("isJumping", !isGrounded);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!isActive) return;
        Move();
    }

    protected virtual void LateUpdate()
    {
        if (!boostOutlineActive || outlineRenderers == null) return;

        Sprite currentSprite = spriteRenderer?.sprite;
        bool flipped = spriteRenderer?.flipX ?? false;

        float pulse = (Mathf.Sin(Time.time * outlinePulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(outlineAlphaMin, outlineAlphaMax, pulse);

        for (int i = 0; i < outlineRenderers.Length; i++)
        {
            SpriteRenderer sr = outlineRenderers[i];
            if (sr == null) continue;

            sr.sprite = currentSprite;
            sr.flipX = flipped;

            Color c = outlineColor;
            c.a = alpha;
            sr.color = c;

            Vector2 dir = OutlineDirs[i];
            if (flipped && (dir == Vector2.left || dir == Vector2.right))
                dir = -dir;

            sr.transform.localPosition = (Vector3)(dir * outlineOffsetWorld);
        }
    }

    // ------------------------------------------------------------------ //
    // Boost Outline — init
    // ------------------------------------------------------------------ //
    private void InitBoostOutline()
    {
        outlineRenderers = new SpriteRenderer[OutlineDirs.Length];

        for (int i = 0; i < OutlineDirs.Length; i++)
        {
            GameObject go = new GameObject($"BoostOutline_{i}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = (Vector3)(OutlineDirs[i] * outlineOffsetWorld);
            go.transform.localScale = Vector3.one;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                sr.sprite = spriteRenderer.sprite;
                sr.sortingLayerName = spriteRenderer.sortingLayerName;
                sr.sortingOrder = spriteRenderer.sortingOrder - 1;
            }

            sr.color = Color.clear;
            outlineRenderers[i] = sr;
        }
    }

    // ------------------------------------------------------------------ //
    // Active / Inactive
    // ------------------------------------------------------------------ //
    public void SetActive(bool active)
    {
        isActive = active;

        if (!isActive)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            moveInput = Vector2.zero;

            if (animator != null)
            {
                animator.SetBool("isRunning", false);
                animator.SetBool("isJumping", false);
            }

            SetOutlineRenderersVisible(false);
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = gravityScale;
            SetOutlineRenderersVisible(boostOutlineActive);
        }
    }

    private void SetOutlineRenderersVisible(bool visible)
    {
        if (outlineRenderers == null) return;
        foreach (SpriteRenderer sr in outlineRenderers)
            if (sr != null) sr.enabled = visible;
    }

    public bool IsActive() => isActive;

    // ------------------------------------------------------------------ //
    // Input forwarding
    // ------------------------------------------------------------------ //
    public void SetMoveInput(Vector2 input) => moveInput = input;
    public void SetJumpBuffer(float time) => jumpBufferTimer = time;

    // ------------------------------------------------------------------ //
    // Input callbacks
    // ------------------------------------------------------------------ //
    public virtual void OnMove(InputValue value)
    {
        if (isActive)
        {
            moveInput = value.Get<Vector2>();
        }
        else if (CharacterSwitcher.Instance != null)
        {
            PlayerController active = CharacterSwitcher.Instance.GetActiveCharacter();
            if (active != null && active != this)
                active.SetMoveInput(value.Get<Vector2>());
        }
    }

    public virtual void OnJump(InputValue value)
    {
        if (isActive)
        {
            if (value.isPressed) jumpBufferTimer = jumpBufferTime;
        }
        else if (CharacterSwitcher.Instance != null)
        {
            PlayerController active = CharacterSwitcher.Instance.GetActiveCharacter();
            if (active != null && active != this && value.isPressed)
                active.SetJumpBuffer(jumpBufferTime);
        }
    }

    public void OnSwitch(InputValue value)
    {
        if (value.isPressed && CharacterSwitcher.Instance != null)
            CharacterSwitcher.Instance.ToggleActiveCharacter();
    }

    // ------------------------------------------------------------------ //
    // Movement
    // ------------------------------------------------------------------ //
    protected virtual void Move()
    {
        rb.linearVelocity = new Vector2(GetHorizontalInput() * moveSpeed, rb.linearVelocity.y);

        if (spriteRenderer != null)
        {
            if (moveInput.x > 0.1f) spriteRenderer.flipX = false;
            else if (moveInput.x < -0.1f) spriteRenderer.flipX = true;
        }
    }

    protected virtual float GetHorizontalInput() => moveInput.x;

    // ------------------------------------------------------------------ //
    // Ground Detection
    // ------------------------------------------------------------------ //
    private void CheckGrounded()
    {
        Vector2 castSize = new Vector2(capsuleCollider.bounds.size.x * 0.9f, groundCheckDistance);
        Vector2 origin = new Vector2(capsuleCollider.bounds.center.x, capsuleCollider.bounds.min.y);

        RaycastHit2D hit = Physics2D.BoxCast(
            origin, castSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hit.collider != null;
    }

    private void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;
    }

    private void HandleJumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void ApplyFallGravity()
    {
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                                 * (fallMultiplier - 1f) * Time.deltaTime;
    }

    // ------------------------------------------------------------------ //
    // Boost system
    // ------------------------------------------------------------------ //
    public float GetMoveSpeed() => moveSpeed;
    public float GetJumpForce() => jumpForce;
    public void SetMoveSpeed(float s) => moveSpeed = s;
    public void SetJumpForce(float f) => jumpForce = f;

    /// <summary>
    /// Called by BoostOrb. The coroutine runs on THIS MonoBehaviour so it
    /// survives the orb being destroyed.  A second orb collected before the
    /// first expires will cancel the old timer and start fresh.
    /// </summary>
    public void ApplyBoost(float speedMult, float jumpMult, float duration, Color outlineCol)
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);

        boostCoroutine = StartCoroutine(BoostCoroutine(speedMult, jumpMult, duration, outlineCol));
    }

    private IEnumerator BoostCoroutine(float speedMult, float jumpMult,
                                        float duration, Color outlineCol)
    {
        // Snapshot originals
        float origSpeed = moveSpeed;
        float origJump = jumpForce;

        if (speedMult != 1f) SetMoveSpeed(origSpeed * speedMult);
        if (jumpMult != 1f) SetJumpForce(origJump * jumpMult);

        SetBoostTint(outlineCol);

        yield return new WaitForSeconds(duration);   // ← timer completes even after orb dies

        SetMoveSpeed(origSpeed);
        SetJumpForce(origJump);
        ClearBoostTint();

        boostCoroutine = null;
        Debug.Log($"{(CompareTag("ARC") ? "ARC" : "ECHO")} boost expired.");
    }

    public void SetBoostTint(Color tint)
    {
        outlineColor = tint;
        boostOutlineActive = true;
        SetOutlineRenderersVisible(true);
    }

    public void ClearBoostTint()
    {
        boostOutlineActive = false;
        SetOutlineRenderersVisible(false);
        if (outlineRenderers != null)
            foreach (SpriteRenderer sr in outlineRenderers)
                if (sr != null) sr.color = Color.clear;
    }

    // ------------------------------------------------------------------ //
    // Death
    // ------------------------------------------------------------------ //
    public virtual void Die()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.SFX.Death);
        LevelManager.Instance.ResetLevel();
    }
}