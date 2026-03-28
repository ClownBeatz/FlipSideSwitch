// ExitDoor.cs
// Attach to: Each exit door GameObject (one in ARC's level, one in ECHO's level)
// Requires: A Collider2D set to "Is Trigger" on the same GameObject
//
// VISUAL UPGRADES:
//   ON ACTIVATION:
//     - Instant bright-white flash (one frame) for impact
//     - Quickly lerps from white → activatedColour over 0.25 s
//     - Enters a continuous gentle pulse — colour oscillates between
//       activatedColour and a lighter/brighter version — so the player can see
//       at a glance which doors are done
//
//   SCALE PUNCH:
//     - Door sprite briefly scales up then snaps back (satisfying "clunk")
//
//   RESET:
//     - Returns immediately to white (neutral) and stops the pulse coroutine

using UnityEngine;
using System.Collections;

public class ExitDoor : MonoBehaviour
{
    public enum DoorOwner { ARC, ECHO }

    [Header("Settings")]
    [SerializeField] private DoorOwner owner;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField] private Color activatedColour = new Color(0.25f, 1f, 0.45f, 1f); // Vibrant green

    [Header("Activation Punch")]
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.20f;

    [Header("Idle Pulse (after activation)")]
    [SerializeField] private float pulseSpeed = 2.5f;  // Hz
    [SerializeField] private float pulseBrightness = 0.35f; // How much lighter the pulse peak is

    private bool isActivated = false;
    private Coroutine activationRoutine;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    // ------------------------------------------------------------------ //
    // Trigger
    // ------------------------------------------------------------------ //
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        bool correctCharacter = (owner == DoorOwner.ARC && other.CompareTag("ARC"))
                             || (owner == DoorOwner.ECHO && other.CompareTag("ECHO"));

        if (correctCharacter) ActivateDoor();
    }

    // ------------------------------------------------------------------ //
    // Activation
    // ------------------------------------------------------------------ //
    private void ActivateDoor()
    {
        isActivated = true;

        if (activationRoutine != null) StopCoroutine(activationRoutine);
        activationRoutine = StartCoroutine(ActivationSequence());

        LevelManager.Instance.DoorActivated(owner);

        if (owner == DoorOwner.ARC)
        {
            TutorialManager tm = FindFirstObjectByType<TutorialManager>();
            if (tm != null) tm.NotifyARCExitReached();
        }

        Debug.Log($"{owner}'s exit door activated.");
    }

    private IEnumerator ActivationSequence()
    {
        // ── 1. Impact frame ──────────────────────────────────────────────
        if (doorSprite != null) doorSprite.color = Color.white;

        // ── 2. Scale punch ───────────────────────────────────────────────
        StartCoroutine(ScalePunch());

        yield return null; // hold white for exactly one frame

        // ── 3. Lerp from white → activated colour ────────────────────────
        float elapsed = 0f;
        float settleDur = 0.25f;

        while (elapsed < settleDur)
        {
            elapsed += Time.deltaTime;
            if (doorSprite != null)
                doorSprite.color = Color.Lerp(Color.white, activatedColour, elapsed / settleDur);
            yield return null;
        }

        if (doorSprite != null) doorSprite.color = activatedColour;

        // ── 4. Continuous gentle pulse ────────────────────────────────────
        while (true)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            Color peak = new Color(
                Mathf.Min(1f, activatedColour.r + pulseBrightness),
                Mathf.Min(1f, activatedColour.g + pulseBrightness),
                Mathf.Min(1f, activatedColour.b + pulseBrightness),
                1f);

            if (doorSprite != null)
                doorSprite.color = Color.Lerp(activatedColour, peak, pulse * 0.6f);

            yield return null;
        }
    }

    private IEnumerator ScalePunch()
    {
        float elapsed = 0f;
        float halfDur = punchDuration * 0.5f;

        // Scale up
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            transform.localScale = Vector3.LerpUnclamped(originalScale,
                originalScale * punchScale, EaseOut(t));
            yield return null;
        }

        elapsed = 0f;

        // Scale back down with slight overshoot
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            transform.localScale = Vector3.LerpUnclamped(originalScale * punchScale,
                originalScale, EaseOut(t));
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // ------------------------------------------------------------------ //
    // Reset
    // ------------------------------------------------------------------ //
    public void ResetDoor()
    {
        isActivated = false;

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        transform.localScale = originalScale;
        if (doorSprite != null) doorSprite.color = Color.white;
    }

    // ------------------------------------------------------------------ //
    private float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}