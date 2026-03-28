// LevelCompleteUI.cs
// Attach to: A Panel GameObject called "LevelCompletePanel" inside your Canvas.
//            This panel should start INACTIVE (untick it in the Inspector).
//
// VISUAL UPGRADES:
//   SLIDE-IN:
//     - Panel drops in from above with a bounce overshoot — like it "lands" with weight
//     - CanvasGroup alpha fades in simultaneously
//
//   HEADER TEXT:
//     - "LEVEL COMPLETE" scales in from 0 → 1 with an elastic pop after the panel lands
//
//   BUTTONS:
//     - Fade in slightly delayed after the header pops
//
// HOW TO SET UP IN UNITY:
// 1. Inside your game scene Canvas, create a Panel called "LevelCompletePanel"
//    - Set it to fill the whole screen
//    - Set background colour to something semi-transparent (e.g. black at 70% alpha)
//    - UNTICK the GameObject so it starts hidden
// 2. Inside LevelCompletePanel add:
//    a) TextMeshPro Text — "LEVEL COMPLETE" (large, centred)          → headerText
//    b) TextMeshPro Text — flavour line                               → subText
//    c) Button "NextButton"  — "NEXT LEVEL"
//    d) Button "RetryButton" — "RETRY"
// 3. Add a CanvasGroup component to LevelCompletePanel
// 4. Attach this script to LevelCompletePanel
// 5. Drag elements into Inspector slots
// 6. In LevelManager, drag LevelCompletePanel into "Level Complete Panel" slot

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI subText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button retryButton;

    [Header("Animation")]
    [SerializeField] private float slideInDuration = 0.45f;   // Time for panel to reach position
    [SerializeField] private float bounceAmount = 0.12f;   // Overshoot fraction (0 = no bounce)
    [SerializeField] private float headerPopDelay = 0.30f;   // Seconds after slide lands
    [SerializeField] private float headerPopDur = 0.22f;
    [SerializeField] private float buttonsFadeDelay = 0.48f;  // Seconds after slide lands
    [SerializeField] private float buttonsFadeDur = 0.18f;

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;         // Add CanvasGroup to panel

    private static readonly string[] CompletionLines =
    {
        "Both worlds aligned.",
        "Perfect symmetry.",
        "Two sides, one solution.",
        "The mirror holds.",
        "Harmony achieved.",
        "You thought for two.",
        "Neither left behind.",
        "A flawless reflection.",
    };

    private RectTransform rectTransform;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        shownPosition = rectTransform.anchoredPosition;
        hiddenPosition = shownPosition + new Vector2(0f, Screen.height + 100f);

        if (nextButton != null) nextButton.onClick.AddListener(OnNextPressed);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
    }

    // ------------------------------------------------------------------ //
    // Show — called by LevelManager
    // ------------------------------------------------------------------ //
    public void Show()
    {
        gameObject.SetActive(true);

        if (subText != null)
            subText.text = CompletionLines[Random.Range(0, CompletionLines.Length)];

        if (UIManager.Instance != null)
            UIManager.Instance.FlashLevelComplete();

        StartCoroutine(AnimateIn());
    }

    // ------------------------------------------------------------------ //
    // Animation sequence
    // ------------------------------------------------------------------ //
    private IEnumerator AnimateIn()
    {
        // Hide header and buttons at start
        if (headerText != null) headerText.transform.localScale = Vector3.zero;
        SetButtonsAlpha(0f);

        // Initialise panel position and alpha
        rectTransform.anchoredPosition = hiddenPosition;
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        // ── 1. Panel slides down with bounce easing ──────────────────────
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideInDuration);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(
                hiddenPosition, shownPosition, BounceEaseOut(t, bounceAmount));

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(t * 2f); // fade in faster than slide

            yield return null;
        }

        rectTransform.anchoredPosition = shownPosition;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // ── 2. Header pops in after a beat ───────────────────────────────
        yield return new WaitForSeconds(headerPopDelay);
        if (headerText != null) StartCoroutine(PopScale(headerText.transform, headerPopDur));

        // ── 3. Buttons fade in ───────────────────────────────────────────
        yield return new WaitForSeconds(buttonsFadeDelay - headerPopDelay);
        yield return StartCoroutine(FadeInButtons(buttonsFadeDur));
    }

    private IEnumerator PopScale(Transform t, float duration)
    {
        float half = duration * 0.5f;
        float elapsed = 0f;

        // Scale from 0 → overshoot
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.18f, EaseOut(elapsed / half));
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        elapsed = 0f;

        // Scale back to 1
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.18f, 1f, EaseOut(elapsed / half));
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    private IEnumerator FadeInButtons(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetButtonsAlpha(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        SetButtonsAlpha(1f);
    }

    private void SetButtonsAlpha(float a)
    {
        SetGraphicAlpha(nextButton?.GetComponent<CanvasGroup>(), a);
        SetGraphicAlpha(retryButton?.GetComponent<CanvasGroup>(), a);
        // Fallback: if buttons don't have CanvasGroups just show them
        if (nextButton != null) nextButton.interactable = a >= 0.99f;
        if (retryButton != null) retryButton.interactable = a >= 0.99f;
    }

    private void SetGraphicAlpha(CanvasGroup cg, float a)
    {
        if (cg != null) cg.alpha = a;
    }

    // ------------------------------------------------------------------ //
    // Button callbacks
    // ------------------------------------------------------------------ //
    private void OnNextPressed()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    private void OnRetryPressed()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------------ //
    // Easing
    // ------------------------------------------------------------------ //
    // Bounce ease-out: overshoots by (bounceAmount * 100)% then settles
    private float BounceEaseOut(float t, float bounce)
    {
        t = Mathf.Clamp01(t);
        // Simple spring: ease to 1 + overshoot that decays
        float baseEase = EaseOut(t);
        float overshoot = bounce * Mathf.Sin(t * Mathf.PI) * (1f - t);
        return baseEase + overshoot;
    }

    private float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}