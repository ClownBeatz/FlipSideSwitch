// SceneFader.cs
// Persistent full-screen fade used for every scene change.
// Creates itself automatically when the game starts (no scene setup needed).
//
//   SceneFader.LoadScene(index)  fades to black, loads, then fades back in.
//   Any other scene load (for example the reset after a death) gets a quick fade in.
//
// The fade never blocks input and uses unscaled time, so it works while paused.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    private static SceneFader instance;

    private Canvas canvas;
    private Image overlay;
    private Image stripe;
    private Coroutine routine;
    private bool loadingViaFader;
    private string lastSceneName;

    private const float FadeOutTime = 0.35f;
    private const float FadeInTime = 0.55f;
    private const float QuickFadeInTime = 0.22f;

    // ------------------------------------------------------------------ //
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("FlipsideSceneFader");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFader>();
    }

    public static bool IsFading => instance != null && instance.routine != null && instance.loadingViaFader;

    public static void LoadScene(int buildIndex)
    {
        if (instance == null) { SceneManager.LoadScene(buildIndex); return; }
        instance.StartLoad(() => SceneManager.LoadScene(buildIndex));
    }

    public static void LoadScene(string sceneName)
    {
        if (instance == null) { SceneManager.LoadScene(sceneName); return; }
        instance.StartLoad(() => SceneManager.LoadScene(sceneName));
    }

    // ------------------------------------------------------------------ //
    private void Awake()
    {
        BuildOverlay();
        SetAlpha(1f); // first scene fades in from black
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void BuildOverlay()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        FlipsideTheme.ConfigureScaler(canvas);

        overlay = FlipsideTheme.CreateImage("Fade", transform, null, FlipsideTheme.Ink);
        FlipsideTheme.Stretch(overlay.rectTransform);
        overlay.raycastTarget = false;

        // Thin brand line across the middle, like the split, visible during fades
        stripe = FlipsideTheme.CreateImage("FadeLine", transform, FlipsideTheme.BrandGradient, Color.white);
        RectTransform s = stripe.rectTransform;
        s.anchorMin = new Vector2(0f, 0.5f);
        s.anchorMax = new Vector2(1f, 0.5f);
        s.pivot = new Vector2(0.5f, 0.5f);
        s.sizeDelta = new Vector2(0f, 2f);
        stripe.raycastTarget = false;
    }

    private void SetAlpha(float a)
    {
        if (overlay != null) overlay.color = FlipsideTheme.WithAlpha(FlipsideTheme.Ink, a);
        if (stripe != null)
        {
            // Line is brightest mid fade, a quick flash of the split
            float lineA = Mathf.Clamp01(a * 1.4f) * 0.8f;
            stripe.color = new Color(1f, 1f, 1f, lineA);
            stripe.rectTransform.localScale = new Vector3(Mathf.Lerp(0.2f, 1f, a), 1f, 1f);
        }
        if (canvas != null) canvas.enabled = a > 0.001f;
    }

    private void StartLoad(System.Action load)
    {
        if (routine != null && loadingViaFader) return; // already loading
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOutThenLoad(load));
    }

    private IEnumerator FadeOutThenLoad(System.Action load)
    {
        loadingViaFader = true;
        float start = overlay != null ? overlay.color.a : 0f;
        yield return Fade(start, 1f, FadeOutTime * (1f - start));
        Time.timeScale = 1f; // every scene starts unpaused
        load();
        // fade in happens in OnSceneLoaded
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        bool sameScene = scene.name == lastSceneName;
        bool fromFader = loadingViaFader;
        lastSceneName = scene.name;
        loadingViaFader = false;

        if (routine != null) StopCoroutine(routine);

        // Death resets reload the same scene: keep that snappy
        float duration = (sameScene && !fromFader) ? QuickFadeInTime : FadeInTime;
        float from = (sameScene && !fromFader) ? 0.6f : 1f;
        routine = StartCoroutine(FadeInRoutine(from, duration));
    }

    private IEnumerator FadeInRoutine(float from, float duration)
    {
        SetAlpha(from);
        yield return null; // let the first frame of the new scene render under the overlay
        yield return Fade(from, 0f, duration);
        routine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f) { SetAlpha(to); yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            SetAlpha(Mathf.Lerp(from, to, FlipsideTheme.EaseInOutCubic(t / duration)));
            yield return null;
        }
        SetAlpha(to);
    }
}
