// PauseManager.cs
// AI-ASSISTED: Generated with Cursor, reviewed by developer.
// Attach to: An empty GameObject called "PauseManager" in every level scene.
//
// SLOW-MOTION PAUSE:
// Instead of freezing time completely, pressing Escape slows time to 3% speed.
// The world creeps forward almost imperceptibly — feels cinematic, not frozen.
// The pause panel fades in over the slow-motion world.
// Unpausing: time eases back to full speed (not instant — smooth ramp-up).
//
// PAUSE PANEL CONTENTS:
//   Resume         — eases time back to full speed, hides panel
//   Restart Level  — resets timeScale, reloads current scene
//   Main Menu      — resets timeScale, loads scene 0
//   Quit           — resets timeScale, quits application
//
// HOW TO SET UP:
// 1. Add "Pause" action to FlipsideInputActions → Player map → bind to Escape
// 2. Create PauseManager empty GameObject → attach this script
// 3. Inside Canvas, create PausePanel (Panel, starts UNTICKED/hidden):
//      - CanvasGroup component on the panel
//      - TextMeshPro: "PAUSED"
//      - TextMeshPro (small): "— TIME DILATED —" (subtitle flavour text)
//      - Button: ResumeButton   → label "RESUME"
//      - Button: RestartButton  → label "RESTART LEVEL"
//      - Button: MenuButton     → label "MAIN MENU"
//      - Button: QuitButton     → label "QUIT GAME"
// 4. Drag PausePanel into the Inspector slot
// 5. Drag each button into its Inspector slot
//
// NOTE: Because time is slow (not zero), animations still play — they just
// crawl. Audio pitch is also adjusted down to match the slow-motion feel.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;     // The whole pause panel (starts hidden)
    [SerializeField] private CanvasGroup pauseCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowMotionScale = 0.03f;  // 3% speed — barely moving
    [SerializeField] private float resumeRampSpeed = 2.5f;   // How fast time eases back to 1.0
    [SerializeField] private float panelFadeDuration = 0.25f; // Fade in/out speed

    [Header("Audio Pitch on Pause")]
    [SerializeField] private float pausedPitch = 0.4f;   // Pitch shift on all audio sources

    [Header("Visual — Pause Title")]
    [SerializeField] private TextMeshProUGUI pauseTitleText;  // "PAUSED"
    [SerializeField] private TextMeshProUGUI pauseSubText;    // "— TIME DILATED —"

    // ------------------------------------------------------------------
    private bool isPaused = false;
    private Coroutine resumeCoroutine;
    private Coroutine fadeCoroutine;

    // ------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Match the FLIPSIDE look (see PanelStyler.cs). Visual only.
        PanelStyler.StylePause(pausePanel, pauseTitleText, pauseSubText,
            resumeButton, restartButton, menuButton, quitButton);

        // Wire buttons
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Hide panel at start
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.blocksRaycasts = false; // ADD THIS
        }
    }

    // ------------------------------------------------------------------
    // Input — called via Broadcast Messages from PlayerInput on ARC
    // Add a "Pause" action in FlipsideInputActions → Player map → Escape
    // ------------------------------------------------------------------
    public void OnPause(InputValue value)
    {
        if (value.isPressed)
        {
            if (SceneFader.IsFading) return; // already leaving the level
            if (isPaused) Resume();
            else Pause();
        }
    }

    // ------------------------------------------------------------------
    // Pause
    // ------------------------------------------------------------------
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        // Slow time to near-zero — world barely crawls
        Time.timeScale = slowMotionScale;
        // Adjust audio pitch to match slow motion
        SetAllAudioPitch(pausedPitch);

        // Show panel with fade
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseCanvasGroup != null) pauseCanvasGroup.blocksRaycasts = true;
        SetButtonsInteractable(true);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePanel(0f, 1f, panelFadeDuration));

        // Keyboard navigation starts on Resume
        if (resumeButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    // ------------------------------------------------------------------
    // Resume
    // ------------------------------------------------------------------
    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        SetButtonsInteractable(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        // Fade panel out
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePanel(1f, 0f, panelFadeDuration,
            onComplete: () =>
            {
                if (pausePanel != null) pausePanel.SetActive(false);
                if (pauseCanvasGroup != null) pauseCanvasGroup.blocksRaycasts = false; // ADD THIS
            }));

        // Ease time back to full speed — not instant, feels better
        if (resumeCoroutine != null) StopCoroutine(resumeCoroutine);
        resumeCoroutine = StartCoroutine(RampTimeScale(1f));

        // Restore audio pitch
        SetAllAudioPitch(1f);
    }

    // ------------------------------------------------------------------
    // Restart / Menu / Quit
    // ------------------------------------------------------------------
    public void RestartLevel()
    {
        FreezeForSceneChange();
        SceneFader.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        FreezeForSceneChange();
        SceneFader.LoadScene(0);
    }

    // Holds the world still during the short fade to black.
    // SceneFader sets the time scale back to 1 right before the new scene loads.
    private void FreezeForSceneChange()
    {
        if (resumeCoroutine != null) StopCoroutine(resumeCoroutine);
        isPaused = false;
        Time.timeScale = 0f;
        SetAllAudioPitch(1f);
        SetButtonsInteractable(false);
    }

    public void QuitGame()
    {
        ResetTimeScale();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ResetTimeScale()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }

    // ------------------------------------------------------------------
    // Time ramp — eases time scale from current value to target
    // Uses unscaled time because scaled time is almost zero when paused
    // ------------------------------------------------------------------
    private IEnumerator RampTimeScale(float targetScale)
    {
        float startScale = Time.timeScale;
        float elapsed = 0f;
        float duration = 1f / resumeRampSpeed;

        while (elapsed < duration)
        {
            // Use unscaledDeltaTime — we can't use deltaTime while time is near-zero
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Time.timeScale = Mathf.Lerp(startScale, targetScale, EaseOut(t));
            yield return null;
        }

        Time.timeScale = targetScale;
    }

    // ------------------------------------------------------------------
    // Panel fade
    // ------------------------------------------------------------------
    private IEnumerator FadePanel(float from, float to, float duration,
        System.Action onComplete = null)
    {
        if (pauseCanvasGroup == null) yield break;

        float elapsed = 0f;
        // Use unscaledDeltaTime — time is slowed, but fade should still feel responsive
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            pauseCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        pauseCanvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    // ------------------------------------------------------------------
    // Audio pitch adjustment
    // Finds all AudioSources in the scene and shifts their pitch
    // ------------------------------------------------------------------
    private void SetAllAudioPitch(float pitch)
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource src in sources)
            if (src != null) src.pitch = pitch;
    }

    private void SetButtonsInteractable(bool state)
    {
        if (resumeButton != null) resumeButton.interactable = state;
        if (restartButton != null) restartButton.interactable = state;
        if (menuButton != null) menuButton.interactable = state;
        if (quitButton != null) quitButton.interactable = state;
    }

    private float EaseOut(float t) => 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));

    public bool IsPaused() => isPaused;

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }
}