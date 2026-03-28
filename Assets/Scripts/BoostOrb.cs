// BoostOrb.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: Any orb/pickup GameObject
// Requires: Collider2D with Is Trigger ticked, SpriteRenderer
//
// FIX: Boost duration now works correctly.
//   Root cause: ApplyBoostCoroutine ran on the BoostOrb MonoBehaviour.
//   When the orb called Destroy(gameObject) the coroutine was killed
//   immediately — so stats were never restored and the boost lasted forever.
//   The coroutine is now started on the *recipient* PlayerController, which
//   persists for the whole level, so the timer always completes.

using UnityEngine;
using System.Collections;

public class BoostOrb : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Boost Type
    // ------------------------------------------------------------------ //
    public enum BoostType { Speed, Jump, Both }

    [Header("Boost Settings")]
    [SerializeField] private BoostType boostType = BoostType.Speed;
    [SerializeField] private float boostDuration = 8f;
    [SerializeField] private float speedMultiplier = 1.6f;
    [SerializeField] private float jumpMultiplier = 1.5f;

    [Header("Idle Animation")]
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private float rotateSpeed = 28f;
    [SerializeField] private float alphaMin = 0.65f;
    [SerializeField] private float alphaMax = 1.00f;
    [SerializeField] private float pulseSpeed = 2.8f;

    [Header("Collection Pop-Burst")]
    [SerializeField] private float burstScaleMax = 2.6f;
    [SerializeField] private float burstDuration = 0.18f;

    [Header("Boost Outline Colours")]
    [SerializeField] private Color speedTint = new Color(0.30f, 1.00f, 0.35f, 1f); // Green
    [SerializeField] private Color jumpTint = new Color(0.35f, 0.60f, 1.00f, 1f); // Blue
    [SerializeField] private Color bothTint = new Color(1.00f, 0.85f, 0.15f, 1f); // Gold

    // ------------------------------------------------------------------ //
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private bool collected = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        // Bob
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Rotate
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // Alpha pulse
        if (spriteRenderer != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(alphaMin, alphaMax, t);
            spriteRenderer.color = c;
        }
    }

    // ------------------------------------------------------------------ //
    // Trigger
    // ------------------------------------------------------------------ //
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        PlayerController collector = other.GetComponent<PlayerController>();
        if (collector == null) return;

        collected = true;
        StartCoroutine(CollectSequence(collector));
    }

    // ------------------------------------------------------------------ //
    // Collection sequence
    // ------------------------------------------------------------------ //
    private IEnumerator CollectSequence(PlayerController collector)
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        PlayerController recipient = GetRecipient(collector);

        if (recipient != null)
        {
            // FIX: ApplyBoost is a public method on PlayerController that starts
            //      its own coroutine on itself — independent of this orb's lifetime.
            Color outlineCol = boostType switch
            {
                BoostType.Speed => speedTint,
                BoostType.Jump => jumpTint,
                BoostType.Both => bothTint,
                _ => Color.white
            };

            recipient.ApplyBoost(
                boostType == BoostType.Speed || boostType == BoostType.Both ? speedMultiplier : 1f,
                boostType == BoostType.Jump || boostType == BoostType.Both ? jumpMultiplier : 1f,
                boostDuration,
                outlineCol);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.SFX.OrbCollect);

            string boosterName = recipient.CompareTag("ARC") ? "ARC" : "ECHO";
            string collectorName = collector.CompareTag("ARC") ? "ARC" : "ECHO";
            Debug.Log($"{collectorName} collected orb — {boosterName} gets {boostType} boost for {boostDuration}s!");
        }

        yield return StartCoroutine(PopBurst());

        Destroy(gameObject);
    }

    // ------------------------------------------------------------------ //
    // Pop-burst visual
    // ------------------------------------------------------------------ //
    private IEnumerator PopBurst()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = Color.white;
        transform.rotation = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / burstDuration;

            transform.localScale = Vector3.one * Mathf.Lerp(1f, burstScaleMax, EaseOut(t));

            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;

            yield return null;
        }
    }

    // ------------------------------------------------------------------ //
    private PlayerController GetRecipient(PlayerController collector)
    {
        if (CharacterSwitcher.Instance == null) return null;

        PlayerController arc = CharacterSwitcher.Instance.GetARC();
        PlayerController echo = CharacterSwitcher.Instance.GetECHO();

        if (collector == arc) return echo;
        if (collector == echo) return arc;
        return null;
    }

    private float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}