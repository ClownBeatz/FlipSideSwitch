// CameraFollow.cs
// AI-ASSISTED: Generated with Cursor, tuned manually by developer.
// Attach to: CameraARC and CameraECHO (one copy per camera)
//
// FIX: followY now actually works.
//   Root cause: desiredY was being smoothed once via SmoothDamp (yVelocity),
//   then smoothed a SECOND time inside Vector3.SmoothDamp — double-smoothing
//   killed all vertical movement. X and Y are now handled on separate axes
//   so each uses its own velocity reference and smooth speed.

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Horizontal Follow")]
    [SerializeField] private float smoothSpeed = 5f;        // Snappiness of X follow
    [SerializeField] private float xOffset = 2f;        // Static look-ahead bias
    [SerializeField] private float lookAheadDist = 1.5f;     // Extra look-ahead based on speed
    [SerializeField] private float lookAheadSmooth = 3f;       // How fast the look-ahead eases in/out

    [Header("Vertical Follow")]
    [SerializeField] private float yOffset = 1f;        // Y height above character
    [SerializeField] private bool followY = false;     // True = Y tracks character
    [SerializeField] private float ySmoothSpeed = 3f;        // Softer Y tracking speed

    [Header("Bounds (leave maxX = 0 to disable)")]
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 0f;

    // ------------------------------------------------------------------ //
    private float xVelocity = 0f;
    private float yVelocity = 0f;
    private float currentLookAhead = 0f;
    private Vector3 prevTargetPos;

    private void Start()
    {
        if (target != null) prevTargetPos = target.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: No target assigned!");
            return;
        }

        // ── Predictive look-ahead ─────────────────────────────────────────
        float horizSpeed = (target.position.x - prevTargetPos.x) / Time.deltaTime;
        prevTargetPos = target.position;

        float speedFraction = Mathf.Clamp(Mathf.Abs(horizSpeed) / 8f, 0f, 1f);
        float lookAheadTarget = Mathf.Sign(horizSpeed) * lookAheadDist * speedFraction;
        currentLookAhead = Mathf.Lerp(currentLookAhead, lookAheadTarget,
                                       Time.deltaTime * lookAheadSmooth);

        // ── Smooth X independently ────────────────────────────────────────
        float desiredX = target.position.x + xOffset + currentLookAhead;
        if (maxX != 0f) desiredX = Mathf.Clamp(desiredX, minX, maxX);

        float smoothedX = Mathf.SmoothDamp(
            transform.position.x, desiredX, ref xVelocity, 1f / smoothSpeed);

        // ── Smooth Y independently ────────────────────────────────────────
        // FIX: Y is no longer double-smoothed. It is either:
        //   followY = true  → one SmoothDamp pass using ySmoothSpeed
        //   followY = false → locked to current Y (unchanged)
        float smoothedY;
        if (followY)
        {
            smoothedY = Mathf.SmoothDamp(
                transform.position.y,
                target.position.y + yOffset,
                ref yVelocity,
                1f / ySmoothSpeed);
        }
        else
        {
            smoothedY = transform.position.y;
        }

        transform.position = new Vector3(smoothedX, smoothedY, transform.position.z);
    }
}