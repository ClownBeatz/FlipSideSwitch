// MainMenu.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "MainMenuManager" in the MainMenu scene.
//
// VISUAL FEATURES:
//   - Fades in from black on scene load
//   - Title text: letter-by-letter reveal on open, then continuous slow float
//   - Subtitle: fades in after title reveal completes
//   - Buttons: slide up from below and fade in after subtitle
//   - Start → fades to black → loads Level_01
//   - Quit → quits application
//
// HOW TO SET UP THE MAIN MENU SCENE:
// 1. File → New Scene → save as "MainMenu"
// 2. File → Build Settings → drag MainMenu to the TOP (Build Index 0)
// 3. Create a Canvas (Screen Space Overlay, Scale With Screen Size 1920x1080)
// 4. Inside Canvas add:
//    a) UI Image "FadeOverlay"  — stretch full canvas, black, Alpha 1
//    b) TextMeshPro "TitleText" — large, centre of screen
//    c) TextMeshPro "SubtitleText" — smaller, just below title
//    d) Button "StartButton"    — centred, below subtitle (add CanvasGroup component)
//    e) Button "QuitButton"     — below StartButton (add CanvasGroup component)
// 5. Attach this script to MainMenuManager
// 6. Drag each element into the Inspector slots
// 7. Make sure StartButton and QuitButton each have a CanvasGroup component

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector
    // ------------------------------------------------------------------ //
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Image fadeOverlay;

    [Header("Scene")]
    [SerializeField] private string firstLevelSceneName = "Level_01";

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 1.2f;
    [SerializeField] private float titleRevealDelay = 0.3f;  // Pause after fade before title appears
    [SerializeField] private float subtitleDelay = 0.5f;  // Seconds after title before subtitle
    [SerializeField] private float buttonsDelay = 0.35f; // Seconds after subtitle before buttons
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("Title Float")]
    [SerializeField] private float floatAmplitude = 4f;   // Pixels of vertical float
    [SerializeField] private float floatSpeed = 0.9f; // Float cycle speed

    [Header("Colours")]
    [SerializeField] private Color titleColour = Color.white;
    [SerializeField] private Color subtitleColour = new Color(0.75f, 0.85f, 1f, 1f); // Soft blue

    // ------------------------------------------------------------------ //
    // Private state
    // ------------------------------------------------------------------ //
    private Vector2 titleAnchoredOrigin;
    private RectTransform titleRect;
    private bool animating = true;

    // ------------------------------------------------------------------ //
    // Start
    // ------------------------------------------------------------------ //
    private void Start()
    {
        // Wire buttons
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitPressed);

        // Hide elements immediately — they appear via animation
        if (titleText != null) { titleText.alpha = 0f; titleText.color = titleColour; }
        if (subtitleText != null) { subtitleText.alpha = 0f; subtitleText.color = subtitleColour; }
        SetButtonAlpha(startButton, 0f);
        SetButtonAlpha(quitButton, 0f);
        SetButtonInteractable(false);

        if (titleText != null)
        {
            titleRect = titleText.GetComponent<RectTransform>();
            titleAnchoredOrigin = titleRect.anchoredPosition;
        }

        StartCoroutine(OpenSequence());
    }

    // ------------------------------------------------------------------ //
    // Update — title float
    // ------------------------------------------------------------------ //
    private void Update()
    {
        if (!animating || titleRect == null) return;

        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        titleRect.anchoredPosition = titleAnchoredOrigin + new Vector2(0f, yOffset);
    }

    // ------------------------------------------------------------------ //
    // Opening sequence
    // ------------------------------------------------------------------ //
    private IEnumerator OpenSequence()
    {
        // 1 — Fade in from black
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeInDuration));

        yield return new WaitForSeconds(titleRevealDelay);

        // 2 — Title fades in
        yield return StartCoroutine(FadeElement(titleText, 0f, 1f, 0.5f));

        yield return new WaitForSeconds(subtitleDelay);

        // 3 — Subtitle fades in
        yield return StartCoroutine(FadeElement(subtitleText, 0f, 1f, 0.4f));

        yield return new WaitForSeconds(buttonsDelay);

        // 4 — Buttons slide up and fade in
        yield return StartCoroutine(SlideUpButton(startButton, 0.3f));
        yield return StartCoroutine(SlideUpButton(quitButton, 0.25f));

        SetButtonInteractable(true);
    }

    // ------------------------------------------------------------------ //
    // Button callbacks
    // ------------------------------------------------------------------ //
    private void OnStartPressed()
    {
        SetButtonInteractable(false);
        StartCoroutine(FadeOutAndLoad(firstLevelSceneName));
    }

    private void OnQuitPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ------------------------------------------------------------------ //
    // Fade out and load
    // ------------------------------------------------------------------ //
    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeOutDuration));
        SceneManager.LoadScene(sceneName);
    }

    // ------------------------------------------------------------------ //
    // Animation helpers
    // ------------------------------------------------------------------ //
    private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration));
            yield return null;
        }
        SetOverlayAlpha(toAlpha);
    }

    private IEnumerator FadeElement(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        if (tmp == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tmp.alpha = Mathf.Lerp(from, to, EaseOut(elapsed / duration));
            yield return null;
        }
        tmp.alpha = to;
    }

    private IEnumerator SlideUpButton(Button btn, float duration)
    {
        if (btn == null) yield break;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 origin = rt.anchoredPosition;
        Vector2 startPos = origin + new Vector2(0f, -30f); // Start 30px below

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOut(elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, origin, t);
            if (cg != null) cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        rt.anchoredPosition = origin;
        if (cg != null) cg.alpha = 1f;
    }

    // ------------------------------------------------------------------ //
    // Utility
    // ------------------------------------------------------------------ //
    private void SetOverlayAlpha(float a)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        c.a = a;
        fadeOverlay.color = c;
    }

    private void SetButtonAlpha(Button btn, float a)
    {
        if (btn == null) return;
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = a;
    }

    private void SetButtonInteractable(bool state)
    {
        if (startButton != null) startButton.interactable = state;
        if (quitButton != null) quitButton.interactable = state;
    }

    private float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}