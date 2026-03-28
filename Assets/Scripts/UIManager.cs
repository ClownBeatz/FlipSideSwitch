// UIManager.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "UIManager" in every game scene.
//
// VISUAL UPGRADES:
//   - Death flash hits white for one frame (impact), then settles into red — feels punchier
//   - Both cameras shake on death with damped falloff
//   - Dividing line: triple-flicker with colour shift on each blink, then smoothly fades out
//   - Level label does a scale-punch when text changes (set via SetLevelLabel)
//
// HOW TO SET UP IN UNITY:
// 1. Create a Canvas (right-click Hierarchy → UI → Canvas)
//    Set Canvas: Render Mode = Screen Space - Overlay
// 2. Inside the Canvas create:
//    a) A TextMeshPro Text called "LevelLabel" — top centre of screen
//    b) A UI Image called "FlashOverlay" — stretch to fill entire canvas,
//       set colour to white, set Alpha to 0, tick Raycast Target OFF
//    c) A UI Image called "DividingLine" — thin horizontal strip at screen centre
//       (e.g., height 4px, full width, white colour)
// 3. Attach this script to the UIManager GameObject
// 4. Drag each UI element into the matching slot in the Inspector
// 5. Drag CameraARC and CameraECHO into the "Cameras To Shake" list

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private GameObject dividingLine;

    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.45f;
    [SerializeField] private Color deathFlashColour = new Color(1f, 0.12f, 0.12f, 0.65f);
    [SerializeField] private Color levelCompleteFlashColour = new Color(1f, 1f, 0.88f, 0.80f);

    [Header("Camera Shake — drag both cameras here")]
    [SerializeField] private Camera[] camerasToShake;
    [SerializeField] private float deathShakeDuration = 0.40f;
    [SerializeField] private float deathShakeMagnitude = 0.20f;

    [Header("Label Punch")]
    [SerializeField] private float labelPunchScale = 1.22f;   // Scale overshoot when label changes
    [SerializeField] private float labelPunchDuration = 0.18f;

    // Internal shake state
    private Vector3[] cameraLocalOrigins;
    private Coroutine shakeCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine labelPunchCoroutine;

    // ------------------------------------------------------------------ //
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        UpdateLevelLabel();
        SetFlashAlpha(0f);

        // Cache camera rest positions so shake can restore them
        if (camerasToShake != null)
        {
            cameraLocalOrigins = new Vector3[camerasToShake.Length];
            for (int i = 0; i < camerasToShake.Length; i++)
                if (camerasToShake[i] != null)
                    cameraLocalOrigins[i] = camerasToShake[i].transform.localPosition;
        }
    }

    // ------------------------------------------------------------------ //
    // Level Label
    // ------------------------------------------------------------------ //
    private void UpdateLevelLabel()
    {
        if (levelLabel == null) return;
        string sceneName = SceneManager.GetActiveScene().name;
        levelLabel.text = sceneName.Replace("_", " ").ToUpper();
    }

    public void SetLevelLabel(string text)
    {
        if (levelLabel == null) return;
        levelLabel.text = text;
        PunchLabel();
    }

    // Short scale-punch so the player notices the label changed
    private void PunchLabel()
    {
        if (labelPunchCoroutine != null) StopCoroutine(labelPunchCoroutine);
        labelPunchCoroutine = StartCoroutine(LabelPunch());
    }

    private IEnumerator LabelPunch()
    {
        if (levelLabel == null) yield break;

        float elapsed = 0f;
        float halfDur = labelPunchDuration * 0.5f;

        // Scale up
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            levelLabel.transform.localScale = Vector3.LerpUnclamped(Vector3.one,
                Vector3.one * labelPunchScale, EaseOut(t));
            yield return null;
        }

        elapsed = 0f;

        // Scale back down with slight undershoot
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDur;
            levelLabel.transform.localScale = Vector3.LerpUnclamped(Vector3.one * labelPunchScale,
                Vector3.one, EaseOut(t));
            yield return null;
        }

        levelLabel.transform.localScale = Vector3.one;
    }

    // ------------------------------------------------------------------ //
    // Screen Flash
    // ------------------------------------------------------------------ //
    public void FlashDeath()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(DeathFlash());
        TriggerCameraShake(deathShakeDuration, deathShakeMagnitude);
    }

    public void FlashLevelComplete()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(SimpleFlash(levelCompleteFlashColour));
    }

    // Death flash: slam to pure white for one frame → cross-fade to red → slow fade out
    private IEnumerator DeathFlash()
    {
        if (flashOverlay == null) yield break;

        // Impact frame — pure white
        flashOverlay.color = Color.white;
        SetFlashAlpha(1f);
        yield return null;   // hold exactly one frame

        // Transition white → red quickly
        float crossTime = 0.06f;
        float elapsed = 0f;
        while (elapsed < crossTime)
        {
            elapsed += Time.deltaTime;
            flashOverlay.color = Color.Lerp(Color.white, deathFlashColour, elapsed / crossTime);
            yield return null;
        }

        flashOverlay.color = deathFlashColour;

        // Hold briefly at peak red
        yield return new WaitForSeconds(0.05f);

        // Slow, quadratic fade out
        elapsed = 0f;
        float fadeOut = flashDuration;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOut;
            SetFlashAlpha(Mathf.Lerp(deathFlashColour.a, 0f, t * t));
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    private IEnumerator SimpleFlash(Color colour)
    {
        if (flashOverlay == null) yield break;

        flashOverlay.color = colour;
        float halfDur = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(0f, colour.a, EaseOut(elapsed / halfDur)));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            SetFlashAlpha(Mathf.Lerp(colour.a, 0f, elapsed / flashDuration));
            yield return null;
        }

        SetFlashAlpha(0f);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (flashOverlay == null) return;
        Color c = flashOverlay.color;
        c.a = alpha;
        flashOverlay.color = c;
    }

    // ------------------------------------------------------------------ //
    // Camera Shake
    // ------------------------------------------------------------------ //
    public void TriggerCameraShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(CameraShake(duration, magnitude));
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        if (camerasToShake == null || cameraLocalOrigins == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Damped magnitude — full at start, zero at end
            float progress = elapsed / duration;
            float dampedMag = magnitude * (1f - EaseOut(progress));

            for (int i = 0; i < camerasToShake.Length; i++)
            {
                if (camerasToShake[i] == null) continue;
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    0f) * dampedMag;
                camerasToShake[i].transform.localPosition = cameraLocalOrigins[i] + offset;
            }

            yield return null;
        }

        // Restore exactly to rest positions
        for (int i = 0; i < camerasToShake.Length; i++)
            if (camerasToShake[i] != null)
                camerasToShake[i].transform.localPosition = cameraLocalOrigins[i];
    }

    // ------------------------------------------------------------------ //
    // Dividing Line
    // ------------------------------------------------------------------ //
    public void ShowDividingLine(bool show)
    {
        if (dividingLine != null) dividingLine.SetActive(show);
    }

    // Triple-flicker with colour shift on each blink, then graceful fade-out
    public void FlashDividingLine()
    {
        if (dividingLine != null)
            StartCoroutine(DividingLineFlash());
    }

    private IEnumerator DividingLineFlash()
    {
        if (dividingLine == null) yield break;

        Image lineImg = dividingLine.GetComponent<Image>();
        Color restColour = lineImg != null ? lineImg.color : Color.white;

        Color[] blinks =
        {
            new Color(1.0f, 1.0f, 1.0f, 1f),   // pure white
            new Color(0.5f, 0.8f, 1.0f, 1f),   // icy blue
            new Color(1.0f, 1.0f, 0.6f, 0.9f), // soft yellow
        };

        foreach (Color blink in blinks)
        {
            if (lineImg != null) lineImg.color = blink;
            dividingLine.SetActive(true);
            yield return new WaitForSeconds(0.055f);
            dividingLine.SetActive(false);
            yield return new WaitForSeconds(0.030f);
        }

        // Final on — fade back to rest colour, then out
        dividingLine.SetActive(true);
        if (lineImg != null) lineImg.color = restColour;

        float elapsed = 0f;
        float fadeDur = 0.55f;
        Color startCol = restColour;

        while (elapsed < fadeDur)
        {
            elapsed += Time.deltaTime;
            if (lineImg != null)
            {
                Color c = startCol;
                c.a = Mathf.Lerp(1f, 0f, EaseOut(elapsed / fadeDur));
                lineImg.color = c;
            }
            yield return null;
        }

        dividingLine.SetActive(false);
        if (lineImg != null) lineImg.color = restColour; // restore for next time
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //
    private float EaseOut(float t) => 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));
}