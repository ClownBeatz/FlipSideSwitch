// MainMenu.cs
// Attach to: An empty GameObject called "MainMenuManager" in the MainMenu scene.
//
// Builds the whole FLIPSIDE title screen at runtime, so the scene does not need
// to be edited:
//   - Split screen backdrop: ARC's bright world on top, ECHO's neon world below
//     (mirrored), with a glowing split line between them
//   - FLIPSIDE title cut in half by the split: dark ink on top, neon below
//   - Styled buttons: PLAY, LEVEL SELECT, CONTROLS, QUIT (mouse and keyboard)
//   - Level select (all 7 levels, cleared levels are marked)
//   - Controls and rules screen
//   - Short intro animation and drifting particles
//
// The old Inspector slots are kept so the scene still loads cleanly. The old
// objects on the Canvas are hidden and replaced.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    // Inspector (kept for compatibility with the existing scene)
    // ------------------------------------------------------------------ //
    [Header("UI Elements (old layout, hidden at runtime)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Image fadeOverlay;

    [Header("Scene")]
    [SerializeField] private string firstLevelSceneName = "Level_01";
    [SerializeField] private int levelCount = 7;

    [Header("Text")]
    [SerializeField] private string tagline = "TWO WORLDS. ONE MIND.";

    // ------------------------------------------------------------------ //
    // Runtime
    // ------------------------------------------------------------------ //
    private const float GroundLine = 0.371f; // skyline ground height in the background art (from bottom)

    private Canvas canvas;
    private RectTransform root;
    private RectTransform topHalf, bottomHalf;
    private RectTransform arcBg, echoBg;
    private RectTransform titleTop, titleBottom;
    private RectTransform arcChar, echoChar;
    private RectTransform splitLine, sweep;
    private TextMeshProUGUI taglineText;
    private CanvasGroup taglineGroup;
    private Material titleGlowMat;

    private readonly List<FlipsideButton> mainButtons = new List<FlipsideButton>();
    private RectTransform mainButtonStack;
    private GameObject levelSelectPanel;
    private GameObject controlsPanel;
    private GameObject lastMainSelection;
    private FlipsideButton firstLevelTile;
    private FlipsideButton controlsBack;

    private bool introDone;
    private float nextGlitch = 4f;
    private float glitchTime;

    private class Mote { public RectTransform rt; public Image img; public Vector2 vel; public float phase; public RectTransform area; public Color colour; }
    private readonly List<Mote> motes = new List<Mote>();

    // ------------------------------------------------------------------ //
    private void Start()
    {
        Time.timeScale = 1f;

        canvas = titleText != null ? titleText.canvas : null;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        canvas = canvas.rootCanvas;
        FlipsideTheme.ConfigureScaler(canvas);

        // Hide the old placeholder layout
        foreach (Transform child in canvas.transform)
            child.gameObject.SetActive(false);

        EnsureEventSystem();

        root = FlipsideTheme.CreateRect("FlipsideMenu", canvas.transform);
        FlipsideTheme.Stretch(root);

        BuildBackground();
        BuildTitle();
        BuildMainButtons();
        BuildFooter();
        levelSelectPanel = BuildLevelSelect();
        controlsPanel = BuildControls();

        StartCoroutine(Intro());
    }

    // ------------------------------------------------------------------ //
    // Background
    // ------------------------------------------------------------------ //
    private void BuildBackground()
    {
        // Top: ARC's light world
        Image top = FlipsideTheme.CreateImage("TopHalf", root, null, FlipsideTheme.Hex("F4F9FF"));
        topHalf = top.rectTransform;
        topHalf.anchorMin = new Vector2(0f, 0.5f); topHalf.anchorMax = Vector2.one;
        topHalf.offsetMin = topHalf.offsetMax = Vector2.zero;
        top.gameObject.AddComponent<RectMask2D>();

        Sprite arcSprite = FlipsideTheme.LoadSprite("ArcBackground");
        if (arcSprite != null)
        {
            Image img = FlipsideTheme.CreateImage("ArcSkyline", topHalf, arcSprite, Color.white);
            arcBg = img.rectTransform;
            arcBg.anchorMin = arcBg.anchorMax = new Vector2(0.5f, 0f);
            arcBg.pivot = new Vector2(0.5f, GroundLine);
        }

        // Bottom: ECHO's neon world, mirrored downwards
        Image bottom = FlipsideTheme.CreateImage("BottomHalf", root, null, FlipsideTheme.Hex("0E0B16"));
        bottomHalf = bottom.rectTransform;
        bottomHalf.anchorMin = Vector2.zero; bottomHalf.anchorMax = new Vector2(1f, 0.5f);
        bottomHalf.offsetMin = bottomHalf.offsetMax = Vector2.zero;
        bottom.gameObject.AddComponent<RectMask2D>();

        Sprite echoSprite = FlipsideTheme.LoadSprite("EchoBackground");
        if (echoSprite != null)
        {
            Image img = FlipsideTheme.CreateImage("EchoSkyline", bottomHalf, echoSprite, Color.white);
            echoBg = img.rectTransform;
            echoBg.anchorMin = echoBg.anchorMax = new Vector2(0.5f, 1f);
            echoBg.pivot = new Vector2(0.5f, GroundLine);
            echoBg.localScale = new Vector3(1f, -1f, 1f);
        }

        // Soft gradient over each half so the title and buttons read clearly
        Image topWash = FlipsideTheme.CreateImage("TopWash", topHalf, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(Color.white, 0.85f));
        FlipsideTheme.Place(topWash.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700f, 520f));
        Image bottomWash = FlipsideTheme.CreateImage("BottomWash", bottomHalf, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(FlipsideTheme.Hex("0A0712"), 0.85f));
        FlipsideTheme.Place(bottomWash.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 700f));

        // Particles
        RectTransform arcMotes = FlipsideTheme.CreateRect("ArcMotes", topHalf);
        FlipsideTheme.Stretch(arcMotes);
        RectTransform echoMotes = FlipsideTheme.CreateRect("EchoMotes", bottomHalf);
        FlipsideTheme.Stretch(echoMotes);
        for (int i = 0; i < 22; i++) AddMote(arcMotes, FlipsideTheme.ArcBlue, +1f);
        for (int i = 0; i < 26; i++) AddMote(echoMotes, FlipsideTheme.EchoNeon, -1f);

        // Characters: ARC stands on the split, ECHO hangs below it (mirrored)
        Sprite arcIdle = FlipsideTheme.LoadSprite("ArcIdle", true);
        if (arcIdle != null)
        {
            Image a = FlipsideTheme.CreateImage("ARC", topHalf, arcIdle, Color.white);
            a.preserveAspect = true;
            arcChar = FlipsideTheme.Place(a.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-720f, -6f), new Vector2(170f, 252f));
        }
        Sprite echoIdle = FlipsideTheme.LoadSprite("EchoIdle", true);
        if (echoIdle != null)
        {
            Image e = FlipsideTheme.CreateImage("ECHO", bottomHalf, echoIdle, Color.white);
            e.preserveAspect = true;
            echoChar = FlipsideTheme.Place(e.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(720f, 6f), new Vector2(170f, 252f));
            echoChar.localScale = new Vector3(1f, -1f, 1f);

            Image glow = FlipsideTheme.CreateImage("EchoGlow", bottomHalf, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.25f));
            FlipsideTheme.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(720f, -130f), new Vector2(360f, 360f));
            glow.transform.SetSiblingIndex(e.transform.GetSiblingIndex());
        }

        // Split line
        splitLine = FlipsideTheme.CreateRect("SplitLine", root);
        splitLine.anchorMin = new Vector2(0f, 0.5f); splitLine.anchorMax = new Vector2(1f, 0.5f);
        splitLine.sizeDelta = new Vector2(0f, 70f);

        Image ice = FlipsideTheme.CreateImage("IceGlow", splitLine, FlipsideTheme.VerticalFade, FlipsideTheme.WithAlpha(FlipsideTheme.ArcBlue, 0.6f));
        RectTransform ir = FlipsideTheme.Stretch(ice.rectTransform); ir.offsetMin = new Vector2(0f, 35f); ir.offsetMax = new Vector2(0f, 18f);
        Image neon = FlipsideTheme.CreateImage("NeonGlow", splitLine, FlipsideTheme.VerticalFade, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.45f));
        RectTransform nr = FlipsideTheme.Stretch(neon.rectTransform); nr.offsetMin = new Vector2(0f, -18f); nr.offsetMax = new Vector2(0f, -35f);
        Image core = FlipsideTheme.CreateImage("Core", splitLine, null, Color.white);
        core.rectTransform.anchorMin = new Vector2(0f, 0.5f); core.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        core.rectTransform.sizeDelta = new Vector2(0f, 3f);
        Image sw = FlipsideTheme.CreateImage("Sweep", splitLine, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(Color.white, 0.9f));
        sweep = sw.rectTransform;
        sweep.anchorMin = sweep.anchorMax = new Vector2(0f, 0.5f);
        sweep.sizeDelta = new Vector2(420f, 18f);
    }

    private void AddMote(RectTransform area, Color colour, float dir)
    {
        Image img = FlipsideTheme.CreateImage("Mote", area, FlipsideTheme.Dot, Color.clear);
        float size = Random.Range(6f, 16f);
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
        img.rectTransform.sizeDelta = new Vector2(size, size);
        img.rectTransform.anchoredPosition = new Vector2(Random.Range(0f, 1920f), Random.Range(0f, 540f));
        motes.Add(new Mote
        {
            rt = img.rectTransform,
            img = img,
            area = area,
            colour = colour,
            vel = new Vector2(Random.Range(-8f, 8f), dir * Random.Range(10f, 34f)),
            phase = Random.Range(0f, 10f)
        });
    }

    // ------------------------------------------------------------------ //
    // Title
    // ------------------------------------------------------------------ //
    private void BuildTitle()
    {
        // Same word twice, each clipped by its half: ink on the light side, neon on the dark side
        TextMeshProUGUI top = FlipsideTheme.CreateText("TitleTop", topHalf, "FLIPSIDE", FlipsideTheme.TextRole.Title, FlipsideTheme.ArcInk, TextAlignmentOptions.Midline);
        titleTop = FlipsideTheme.Place(top.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 260f));

        TextMeshProUGUI bottom = FlipsideTheme.CreateText("TitleBottom", bottomHalf, "FLIPSIDE", FlipsideTheme.TextRole.Title, FlipsideTheme.Hex("D9A8FF"), TextAlignmentOptions.Midline);
        titleBottom = FlipsideTheme.Place(bottom.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 260f));
        FlipsideTheme.AddGlow(bottom, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.9f), 0.75f, 0.18f);
        titleGlowMat = bottom.fontSharedMaterial;

        // Tagline in ARC's world
        taglineText = FlipsideTheme.CreateText("Tagline", root, tagline, FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.WithAlpha(FlipsideTheme.ArcInk, 0.75f));
        taglineText.fontSize = 26;
        taglineText.characterSpacing = 34;
        FlipsideTheme.Place(taglineText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1200f, 40f));
        taglineGroup = taglineText.gameObject.AddComponent<CanvasGroup>();
    }

    // ------------------------------------------------------------------ //
    // Buttons
    // ------------------------------------------------------------------ //
    private void BuildMainButtons()
    {
        float h = 4 * FlipsideTheme.ButtonHeight + 3 * FlipsideTheme.ButtonSpacing;
        mainButtonStack = FlipsideTheme.CreateRect("MainButtons", root);
        FlipsideTheme.Place(mainButtonStack, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -178f), new Vector2(FlipsideTheme.ButtonWidth, h));
        var v = mainButtonStack.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = FlipsideTheme.ButtonSpacing;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        mainButtons.Add(FlipsideButton.Create(mainButtonStack, "PLAY", FlipsideButton.Variant.Primary, OnPlay));
        mainButtons.Add(FlipsideButton.Create(mainButtonStack, "LEVEL SELECT", FlipsideButton.Variant.Secondary, OpenLevelSelect));
        mainButtons.Add(FlipsideButton.Create(mainButtonStack, "CONTROLS", FlipsideButton.Variant.Secondary, OpenControls));
        mainButtons.Add(FlipsideButton.Create(mainButtonStack, "QUIT", FlipsideButton.Variant.Secondary, OnQuit));

        foreach (FlipsideButton b in mainButtons)
        {
            b.gameObject.AddComponent<CanvasGroup>().alpha = 0f;
            b.Button.interactable = false;
        }
    }

    private void BuildFooter()
    {
        RectTransform footer = FlipsideTheme.CreateRect("Footer", root);
        FlipsideTheme.Place(footer, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 28f), new Vector2(420f, 40f));
        var h = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24f;
        h.childAlignment = TextAnchor.MiddleRight;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        FlipsideTheme.CreatePrompt(footer, new[] { "ENTER" }, "Select", FlipsideTheme.TextMuted);
        FlipsideTheme.CreatePrompt(footer, new[] { "ESC" }, "Back", FlipsideTheme.TextMuted);
    }

    // ------------------------------------------------------------------ //
    // Level select
    // ------------------------------------------------------------------ //
    private GameObject BuildLevelSelect()
    {
        RectTransform panel = FlipsideTheme.CreateRect("LevelSelect", root);
        FlipsideTheme.Stretch(panel);
        FlipsideTheme.CreateBackdrop(panel, 0.82f);

        RectTransform card = FlipsideTheme.CreateCard("Card", panel, new Vector2(1000f, 640f));
        card.gameObject.AddComponent<UIPopIn>();

        TextMeshProUGUI eyebrow = FlipsideTheme.CreateText("Eyebrow", card, "CHOOSE WHERE TO START", FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.ArcBlue);
        FlipsideTheme.Place(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(900f, 30f));
        TextMeshProUGUI header = FlipsideTheme.CreateText("Header", card, "LEVEL SELECT", FlipsideTheme.TextRole.Header, FlipsideTheme.TextPrimary);
        FlipsideTheme.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(900f, 76f));
        FlipsideTheme.AddGlow(header, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.55f), 0.7f, 0.2f);

        RectTransform grid = FlipsideTheme.CreateRect("Grid", card);
        FlipsideTheme.Place(grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -184f), new Vector2(860f, 320f));
        var g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(200f, 150f);
        g.spacing = new Vector2(20f, 20f);
        g.childAlignment = TextAnchor.UpperCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;

        for (int i = 1; i <= levelCount; i++)
        {
            int level = i;
            string sceneName = $"Level_{level:00}";
            FlipsideButton tile = FlipsideButton.Create(grid, $"{level:00}", FlipsideButton.Variant.Secondary,
                () => LoadLevel(sceneName), 200f, 150f);

            bool cleared = PlayerPrefs.GetInt(LevelCompleteUI.CompletedKeyPrefix + sceneName, 0) == 1;
            string status = cleared
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(FlipsideTheme.Success)}>CLEARED</color>"
                : $"<color=#{ColorUtility.ToHtmlStringRGB(FlipsideTheme.TextMuted)}>LEVEL</color>";
            tile.Label.text = $"<size=64>{level:00}</size>\n<size=16><cspace=8>{status}</cspace></size>";
            tile.Label.lineSpacing = -10f;

            if (level == 1) firstLevelTile = tile;
        }

        FlipsideButton back = FlipsideButton.Create(card, "BACK", FlipsideButton.Variant.Secondary, CloseOverlays, 240f);
        FlipsideTheme.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(240f, FlipsideTheme.ButtonHeight));

        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // ------------------------------------------------------------------ //
    // Controls
    // ------------------------------------------------------------------ //
    private GameObject BuildControls()
    {
        RectTransform panel = FlipsideTheme.CreateRect("Controls", root);
        FlipsideTheme.Stretch(panel);
        FlipsideTheme.CreateBackdrop(panel, 0.82f);

        RectTransform card = FlipsideTheme.CreateCard("Card", panel, new Vector2(940f, 700f));
        card.gameObject.AddComponent<UIPopIn>();

        TextMeshProUGUI eyebrow = FlipsideTheme.CreateText("Eyebrow", card, "HOW TO PLAY", FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.ArcBlue);
        FlipsideTheme.Place(eyebrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(860f, 30f));
        TextMeshProUGUI header = FlipsideTheme.CreateText("Header", card, "CONTROLS", FlipsideTheme.TextRole.Header, FlipsideTheme.TextPrimary);
        FlipsideTheme.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(860f, 76f));
        FlipsideTheme.AddGlow(header, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.55f), 0.7f, 0.2f);

        RectTransform rows = FlipsideTheme.CreateRect("Rows", card);
        FlipsideTheme.Place(rows, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(620f, 210f));
        var v = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 16f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;

        FlipsideTheme.CreatePrompt(rows, new[] { "A", "D", "/", "LEFT", "RIGHT" }, "Move", FlipsideTheme.TextPrimary, 24f);
        FlipsideTheme.CreatePrompt(rows, new[] { "SPACE" }, "Jump", FlipsideTheme.TextPrimary, 24f);
        FlipsideTheme.CreatePrompt(rows, new[] { "TAB" }, "Switch between ARC and ECHO", FlipsideTheme.TextPrimary, 24f);
        FlipsideTheme.CreatePrompt(rows, new[] { "ESC" }, "Pause", FlipsideTheme.TextPrimary, 24f);

        Image divider = FlipsideTheme.CreateImage("Divider", card, FlipsideTheme.BrandGradient, FlipsideTheme.WithAlpha(Color.white, 0.5f));
        FlipsideTheme.Place(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(700f, 2f));

        string arc = ColorUtility.ToHtmlStringRGB(FlipsideTheme.ArcBlue);
        string echo = ColorUtility.ToHtmlStringRGB(FlipsideTheme.EchoPink);
        TextMeshProUGUI rules = FlipsideTheme.CreateText("Rules", card,
            $"<color=#{arc}>ARC</color> lives in the light. <color=#{echo}>ECHO</color> lives in the dark.\n" +
            "Both must reach their exit to clear a level.\n" +
            "Each has a timer. The one you are not controlling runs at quarter speed.",
            FlipsideTheme.TextRole.Body, FlipsideTheme.TextMuted);
        rules.fontSize = 24;
        rules.lineSpacing = 18f;
        FlipsideTheme.Place(rules.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(820f, 140f));

        controlsBack = FlipsideButton.Create(card, "BACK", FlipsideButton.Variant.Secondary, CloseOverlays, 240f);
        FlipsideTheme.Place((RectTransform)controlsBack.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(240f, FlipsideTheme.ButtonHeight));

        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // ------------------------------------------------------------------ //
    // Intro animation
    // ------------------------------------------------------------------ //
    private IEnumerator Intro()
    {
        // Start state
        splitLine.localScale = new Vector3(0f, 1f, 1f);
        SetTitleOffset(1f);
        taglineGroup.alpha = 0f;
        Vector2 arcRest = arcChar != null ? arcChar.anchoredPosition : Vector2.zero;
        Vector2 echoRest = echoChar != null ? echoChar.anchoredPosition : Vector2.zero;
        if (arcChar != null) arcChar.anchoredPosition = arcRest + new Vector2(0f, -280f);
        if (echoChar != null) echoChar.anchoredPosition = echoRest + new Vector2(0f, 280f);

        yield return new WaitForSecondsRealtime(0.25f);

        // Split line opens from the centre
        yield return Animate(0.45f, k => splitLine.localScale = new Vector3(FlipsideTheme.EaseOutCubic(k), 1f, 1f));

        // Title halves slide together, characters rise out of the split
        yield return Animate(0.6f, k =>
        {
            SetTitleOffset(1f - FlipsideTheme.EaseOutCubic(k));
            float c = FlipsideTheme.EaseOutBack(k);
            if (arcChar != null) arcChar.anchoredPosition = Vector2.LerpUnclamped(arcRest + new Vector2(0f, -280f), arcRest, c);
            if (echoChar != null) echoChar.anchoredPosition = Vector2.LerpUnclamped(echoRest + new Vector2(0f, 280f), echoRest, c);
        });

        StartCoroutine(Animate(0.5f, k => taglineGroup.alpha = k));

        // Buttons stagger in
        for (int i = 0; i < mainButtons.Count; i++)
        {
            StartCoroutine(SlideIn(mainButtons[i]));
            yield return new WaitForSecondsRealtime(0.07f);
        }
        yield return new WaitForSecondsRealtime(0.25f);

        foreach (FlipsideButton b in mainButtons) b.Button.interactable = true;
        Select(mainButtons[0].gameObject);
        introDone = true;
    }

    private IEnumerator SlideIn(FlipsideButton b)
    {
        CanvasGroup cg = b.GetComponent<CanvasGroup>();
        Transform label = b.Label != null ? b.Label.transform : null;
        yield return Animate(0.35f, k =>
        {
            float e = FlipsideTheme.EaseOutCubic(k);
            cg.alpha = e;
            if (label != null) label.localPosition = new Vector3(0f, -16f * (1f - e), 0f);
        });
    }

    private void SetTitleOffset(float amount)
    {
        if (titleTop != null) titleTop.anchoredPosition = new Vector2(0f, 120f * amount);
        if (titleBottom != null) titleBottom.anchoredPosition = new Vector2(0f, -120f * amount);
    }

    private IEnumerator Animate(float duration, System.Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    // ------------------------------------------------------------------ //
    // Per frame
    // ------------------------------------------------------------------ //
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float time = Time.unscaledTime;

        LayoutBackgrounds();

        // Sweep along the split
        if (sweep != null)
        {
            float w = root.rect.width;
            sweep.anchoredPosition = new Vector2(Mathf.Lerp(-300f, w + 300f, Mathf.Repeat(time / 3.8f, 1f)), 0f);
        }

        // Idle motion once the intro has finished
        if (introDone)
        {
            float bob = (Mathf.Sin(time * 1.6f) * 0.5f + 0.5f) * 8f;
            if (arcChar != null) arcChar.anchoredPosition = new Vector2(-720f, -6f + bob);
            if (echoChar != null) echoChar.anchoredPosition = new Vector2(720f, 6f - bob);

            // Occasional glitch: the two halves slip apart for a moment
            if (time > nextGlitch)
            {
                glitchTime = 0.18f;
                nextGlitch = time + Random.Range(4.5f, 7.5f);
            }
            if (glitchTime > 0f)
            {
                glitchTime -= dt;
                float g = Random.Range(-1f, 1f) * 10f * (glitchTime / 0.18f);
                titleTop.anchoredPosition = new Vector2(g, 0f);
                titleBottom.anchoredPosition = new Vector2(-g, 0f);
            }
            else
            {
                titleTop.anchoredPosition = Vector2.zero;
                titleBottom.anchoredPosition = Vector2.zero;
            }
        }

        // Neon pulse on the lower title
        if (titleGlowMat != null)
        {
            float p = 0.75f + 0.25f * Mathf.Sin(time * 2.1f);
            titleGlowMat.SetColor("_UnderlayColor", FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.9f * p));
        }

        // Particles
        foreach (Mote m in motes)
        {
            Rect r = m.area.rect;
            Vector2 p = m.rt.anchoredPosition + m.vel * dt;
            if (p.y > r.height + 10f) p.y = -10f;
            if (p.y < -10f) p.y = r.height + 10f;
            if (p.x > r.width + 10f) p.x = -10f;
            if (p.x < -10f) p.x = r.width + 10f;
            m.rt.anchoredPosition = p;
            float a = 0.25f + 0.35f * (0.5f + 0.5f * Mathf.Sin(time * 1.3f + m.phase));
            m.img.color = FlipsideTheme.WithAlpha(m.colour, a);
        }

        // ESC closes the open screen
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseOverlays();

        // Keep keyboard focus on something when using arrows
        if (introDone && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            Keyboard.current != null && (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame ||
                                         Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame))
        {
            if (levelSelectPanel.activeSelf && firstLevelTile != null) Select(firstLevelTile.gameObject);
            else if (controlsPanel.activeSelf && controlsBack != null) Select(controlsBack.gameObject);
            else Select(mainButtons[0].gameObject);
        }
    }

    // Size the skyline art so it always covers its half with the ground on the split
    private void LayoutBackgrounds()
    {
        const float aspect = 1024f / 485f;
        const float above = 1f - GroundLine;
        if (arcBg != null && topHalf != null)
        {
            Rect r = topHalf.rect;
            float h = Mathf.Max(r.height / above, r.width / aspect);
            arcBg.sizeDelta = new Vector2(h * aspect, h);
            arcBg.anchoredPosition = Vector2.zero;
        }
        if (echoBg != null && bottomHalf != null)
        {
            Rect r = bottomHalf.rect;
            float h = Mathf.Max(r.height / above, r.width / aspect);
            echoBg.sizeDelta = new Vector2(h * aspect, h);
            echoBg.anchoredPosition = Vector2.zero;
        }
    }

    // ------------------------------------------------------------------ //
    // Actions
    // ------------------------------------------------------------------ //
    private void OnPlay() => LoadLevel(firstLevelSceneName);

    private void LoadLevel(string sceneName)
    {
        SetAllInteractable(false);
        SceneFader.LoadScene(sceneName);
    }

    private void OpenLevelSelect()
    {
        lastMainSelection = mainButtons[1].gameObject;
        levelSelectPanel.SetActive(true);
        if (firstLevelTile != null) Select(firstLevelTile.gameObject);
    }

    private void OpenControls()
    {
        lastMainSelection = mainButtons[2].gameObject;
        controlsPanel.SetActive(true);
        if (controlsBack != null) Select(controlsBack.gameObject);
    }

    private void CloseOverlays()
    {
        bool wasOpen = levelSelectPanel.activeSelf || controlsPanel.activeSelf;
        levelSelectPanel.SetActive(false);
        controlsPanel.SetActive(false);
        if (wasOpen && lastMainSelection != null) Select(lastMainSelection);
    }

    private void OnQuit()
    {
        SetAllInteractable(false);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetAllInteractable(bool state)
    {
        foreach (Button b in root.GetComponentsInChildren<Button>(true)) b.interactable = state;
    }

    private static void Select(GameObject go)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
