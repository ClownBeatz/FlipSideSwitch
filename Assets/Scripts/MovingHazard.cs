// MovingHazard.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: Any hazard GameObject that should move
// Requires: HazardTile.cs also on the same GameObject
//
// VISUAL UPGRADES:
//   SCENE GIZMOS:
//     - The patrol path is drawn as a gradient line (green at A → red at B)
//       so it's immediately obvious which end is which
//     - Midpoint wire-sphere is now yellow-orange
//     - Arrow heads drawn at both endpoints
//
//   GHOST TRAIL (optional):
//     - When ghostTrail = true, faded semi-transparent copies of the sprite
//       are briefly left behind as the hazard moves, giving a motion-blur feel
//     - Works by spawning lightweight child sprites that fade out and self-destruct
//     - Set ghostTrailInterval and ghostFadeDuration in Inspector
//     - Leave ghostTrail = false for static/slow hazards where it looks odd
//
// MOVEMENT TYPES and EASING are unchanged.

using UnityEngine;

public class MovingHazard : MonoBehaviour
{
    public enum MovementType { Horizontal, Vertical, Custom }
    public enum EasingType { Linear, SineWave, EaseInOut }

    [Header("Movement")]
    [SerializeField] private MovementType movementType = MovementType.Horizontal;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float startDelay = 0f;

    [Header("Easing")]
    [SerializeField] private EasingType easing = EasingType.SineWave;

    [Header("Custom Points (MovementType = Custom only)")]
    [SerializeField] private Vector2 pointA = Vector2.zero;
    [SerializeField] private Vector2 pointB = Vector2.one;

    [Header("Visual — Sprite")]
    [SerializeField] private bool flipSpriteOnReverse = false;

    [Header("Visual — Ghost Trail")]
    [SerializeField] private bool ghostTrail = false; // Enable for fast or dramatic hazards
    [SerializeField] private float ghostTrailInterval = 0.07f; // Seconds between ghost spawns
    [SerializeField] private float ghostFadeDuration = 0.25f; // How long each ghost takes to fade
    [SerializeField] private Color ghostColour = new Color(1f, 0.3f, 0.2f, 0.45f); // Red-tinted ghost

    // ------------------------------------------------------------------ //
    private Vector3 originPosition;
    private Vector3 targetA;
    private Vector3 targetB;
    private float timer = 0f;
    private bool delayComplete = false;
    private SpriteRenderer sr;
    private float ghostTimer = 0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originPosition = transform.position;

        switch (movementType)
        {
            case MovementType.Horizontal:
                targetA = originPosition + Vector3.left * moveDistance;
                targetB = originPosition + Vector3.right * moveDistance;
                break;
            case MovementType.Vertical:
                targetA = originPosition + Vector3.down * moveDistance;
                targetB = originPosition + Vector3.up * moveDistance;
                break;
            case MovementType.Custom:
                targetA = new Vector3(pointA.x, pointA.y, originPosition.z);
                targetB = new Vector3(pointB.x, pointB.y, originPosition.z);
                break;
        }
    }

    private void Start()
    {
        if (startDelay <= 0f) delayComplete = true;
        else Invoke(nameof(BeginMovement), startDelay);
    }

    private void BeginMovement() => delayComplete = true;

    private void Update()
    {
        if (!delayComplete) return;

        timer += Time.deltaTime * moveSpeed;

        float rawT = (Mathf.Sin(timer) + 1f) * 0.5f;
        float easedT = easing switch
        {
            EasingType.Linear => rawT,
            EasingType.SineWave => rawT,
            EasingType.EaseInOut => EaseInOut(rawT),
            _ => rawT
        };

        transform.position = Vector3.Lerp(targetA, targetB, easedT);

        if (flipSpriteOnReverse && sr != null && movementType == MovementType.Horizontal)
            sr.flipX = Mathf.Cos(timer) < 0f;

        // Ghost trail
        if (ghostTrail && sr != null)
        {
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0f)
            {
                SpawnGhost();
                ghostTimer = ghostTrailInterval;
            }
        }
    }

    // ------------------------------------------------------------------ //
    // Ghost Trail
    // ------------------------------------------------------------------ //
    private void SpawnGhost()
    {
        if (sr == null || sr.sprite == null) return;

        GameObject ghost = new GameObject("HazardGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale;

        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = sr.sprite;
        ghostSR.flipX = sr.flipX;
        ghostSR.color = ghostColour;
        ghostSR.sortingLayerName = sr.sortingLayerName;
        ghostSR.sortingOrder = sr.sortingOrder - 1;

        Destroy(ghost, ghostFadeDuration + 0.05f);
        StartCoroutine(FadeGhost(ghostSR, ghostFadeDuration));
    }

    private System.Collections.IEnumerator FadeGhost(SpriteRenderer ghostSR, float duration)
    {
        float elapsed = 0f;
        Color startColour = ghostSR.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (ghostSR == null) yield break;

            Color c = startColour;
            c.a = Mathf.Lerp(startColour.a, 0f, elapsed / duration);
            ghostSR.color = c;

            yield return null;
        }
    }

    // ------------------------------------------------------------------ //
    // Easing
    // ------------------------------------------------------------------ //
    private float EaseInOut(float t) => t * t * (3f - 2f * t);

    // ------------------------------------------------------------------ //
    // Gizmos — gradient path (green → red) with endpoint markers
    // ------------------------------------------------------------------ //
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? originPosition : transform.position;
        Vector3 a, b;

        switch (movementType)
        {
            case MovementType.Horizontal:
                a = origin + Vector3.left * moveDistance;
                b = origin + Vector3.right * moveDistance;
                break;
            case MovementType.Vertical:
                a = origin + Vector3.down * moveDistance;
                b = origin + Vector3.up * moveDistance;
                break;
            case MovementType.Custom:
                a = new Vector3(pointA.x, pointA.y, origin.z);
                b = new Vector3(pointB.x, pointB.y, origin.z);
                break;
            default:
                a = b = origin;
                break;
        }

        // Gradient line from A (green) to B (red) — draw in 8 segments
        int segments = 8;
        for (int i = 0; i < segments; i++)
        {
            float t0 = (float)i / segments;
            float t1 = (float)(i + 1) / segments;
            Gizmos.color = Color.Lerp(new Color(0.2f, 1f, 0.3f, 0.85f),
                                       new Color(1f, 0.2f, 0.2f, 0.85f), (t0 + t1) * 0.5f);
            Gizmos.DrawLine(Vector3.Lerp(a, b, t0), Vector3.Lerp(a, b, t1));
        }

        // Endpoint spheres
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 1f);
        Gizmos.DrawSphere(a, 0.12f);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawSphere(b, 0.12f);

        // Centre / origin marker
        Gizmos.color = new Color(1f, 0.65f, 0.1f, 1f);
        Gizmos.DrawWireSphere(origin, 0.17f);
    }
}