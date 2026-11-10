// HUDStyler.cs
// Restyles the in-game HUD at runtime so every level shares one look.
// Added automatically by UIManager, nothing to set up in the scene.
//
// What it does:
//   - Timer cards for ARC (top left) and ECHO (bottom left) with icon, name,
//     status chip and the existing timer text and frost icon moved inside
//   - Clear active character indicator: highlighted card plus a glowing frame
//     around the active half of the screen
//   - Level label as a pill at the top centre, tinted when it says who to guide
//   - Pause button as a small round button in the top right
//   - Control prompts (TAB Switch, ESC Pause) in the bottom right
//   - A permanent glowing split line between the two worlds
//   - Restyled tutorial panel in Level 01
//   - Bloom on ECHO's neon world (see FlipsidePostFX)
//
// It only moves and recolours existing UI. Timer logic is untouched.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class HUDStyler : MonoBehaviour
{
    private UIManager ui;
    private Canvas canvas;
    private RectTransform hudRoot;

    private CharacterCard arcCard;
    private CharacterCard echoCard;
    private Image arcFrame;
    private Image echoFrame;
    private TextMeshProUGUI levelLabel;
    private Image labelPill;
    private Image labelStrip;
    private RectTransform sweep;
    private string lastLabelText;
    private bool lastArcActive = true;
    private float frameBlend = 1f; // 1 = ARC active, 0 = ECHO active

    private class CharacterCard
    {
        public RectTransform root;
        public CanvasGroup group;
        public Image outline;
        public Image iconGlow;
        public Image chipBg;
        public TextMeshProUGUI chipText;
        public Color accent;
        public float punch;
    }

    // ------------------------------------------------------------------ //
    private void Start()
    {
        ui = GetComponent<UIManager>();
        if (ui == null) { enabled = false; return; }

        Graphic anyHud = ui.LevelLabel != null ? (Graphic)ui.LevelLabel : ui.FlashOverlay;
        if (anyHud == null) { enabled = false; return; }

        canvas = anyHud.canvas != null ? anyHud.canvas.rootCanvas : null;
        if (canvas == null) { enabled = false; return; }

        FlipsideTheme.ConfigureScaler(canvas);

        hudRoot = FlipsideTheme.CreateRect("FlipsideHUD", canvas.transform);
        FlipsideTheme.Stretch(hudRoot);
        hudRoot.SetAsFirstSibling(); // behind pause, level complete and flash overlay

        BuildSplitLine();
        BuildFrames();
        BuildCards();
        BuildLevelLabel();
        BuildPauseButton();
        BuildPrompts();
        StyleTutorial();
        StyleFlashLine();

        lastArcActive = ArcActive();
        frameBlend = lastArcActive ? 1f : 0f;

        FlipsidePostFX.ApplyToLevel();
    }

    private static bool ArcActive() =>
        CharacterSwitcher.Instance == null || CharacterSwitcher.Instance.ArcIsActive();

    // ------------------------------------------------------------------ //
    // Split line
    // ------------------------------------------------------------------ //
    private void BuildSplitLine()
    {
        RectTransform line = FlipsideTheme.CreateRect("SplitLine", hudRoot);
        line.anchorMin = new Vector2(0f, 0.5f);
        line.anchorMax = new Vector2(1f, 0.5f);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.sizeDelta = new Vector2(0f, 60f);

        Image iceGlow = FlipsideTheme.CreateImage("IceGlow", line, FlipsideTheme.VerticalFade, FlipsideTheme.WithAlpha(FlipsideTheme.ArcBlue, 0.45f));
        RectTransform a = FlipsideTheme.Stretch(iceGlow.rectTransform);
        a.offsetMin = new Vector2(0f, 30f); a.offsetMax = new Vector2(0f, 14f);

        Image neonGlow = FlipsideTheme.CreateImage("NeonGlow", line, FlipsideTheme.VerticalFade, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.4f));
        RectTransform b = FlipsideTheme.Stretch(neonGlow.rectTransform);
        b.offsetMin = new Vector2(0f, -14f); b.offsetMax = new Vector2(0f, -30f);

        Image core = FlipsideTheme.CreateImage("Core", line, null, FlipsideTheme.WithAlpha(Color.white, 0.92f));
        RectTransform c = core.rectTransform;
        c.anchorMin = new Vector2(0f, 0.5f); c.anchorMax = new Vector2(1f, 0.5f);
        c.sizeDelta = new Vector2(0f, 2f);

        Image sweepImg = FlipsideTheme.CreateImage("Sweep", line, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(Color.white, 0.55f));
        sweep = sweepImg.rectTransform;
        sweep.anchorMin = sweep.anchorMax = new Vector2(0f, 0.5f);
        sweep.sizeDelta = new Vector2(360f, 14f);
    }

    // ------------------------------------------------------------------ //
    // Active half frames
    // ------------------------------------------------------------------ //
    private void BuildFrames()
    {
        arcFrame = FlipsideTheme.CreateImage("ActiveFrameARC", hudRoot, FlipsideTheme.Outline, Color.clear, true);
        RectTransform a = arcFrame.rectTransform;
        a.anchorMin = new Vector2(0f, 0.5f); a.anchorMax = Vector2.one;
        a.offsetMin = new Vector2(8f, 6f); a.offsetMax = new Vector2(-8f, -8f);

        echoFrame = FlipsideTheme.CreateImage("ActiveFrameECHO", hudRoot, FlipsideTheme.Outline, Color.clear, true);
        RectTransform e = echoFrame.rectTransform;
        e.anchorMin = Vector2.zero; e.anchorMax = new Vector2(1f, 0.5f);
        e.offsetMin = new Vector2(8f, 8f); e.offsetMax = new Vector2(-8f, -6f);
    }

    // ------------------------------------------------------------------ //
    // Character cards
    // ------------------------------------------------------------------ //
    private void BuildCards()
    {
        LevelTimer timer = LevelTimer.Instance != null ? LevelTimer.Instance : FindFirstObjectByType<LevelTimer>();

        arcCard = BuildCard("ARC", FlipsideTheme.ArcBlue, true,
            timer != null ? timer.ArcTimerText : null,
            timer != null ? timer.ArcFrostIcon : null,
            FindImage("ARC_Icon"));

        echoCard = BuildCard("ECHO", FlipsideTheme.EchoNeon, false,
            timer != null ? timer.EchoTimerText : null,
            timer != null ? timer.EchoFrostIcon : null,
            FindImage("ECHO_Icon"));
    }

    private CharacterCard BuildCard(string name, Color accent, bool top, TextMeshProUGUI timerText, Image frost, Image icon)
    {
        var card = new CharacterCard { accent = accent };

        Image bg = FlipsideTheme.CreateImage("Card" + name, hudRoot, FlipsideTheme.Rounded, FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.82f), true);
        card.root = bg.rectTransform;
        Vector2 anchor = top ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        FlipsideTheme.Place(card.root, anchor, anchor, top ? new Vector2(28f, -28f) : new Vector2(28f, 28f), new Vector2(350f, 100f));
        card.group = bg.gameObject.AddComponent<CanvasGroup>();
        card.group.interactable = false;
        card.group.blocksRaycasts = false;

        card.outline = FlipsideTheme.CreateImage("Outline", card.root, FlipsideTheme.Outline, FlipsideTheme.WithAlpha(accent, 0.2f), true);
        FlipsideTheme.Stretch(card.outline.rectTransform);

        // Icon with soft glow
        card.iconGlow = FlipsideTheme.CreateImage("IconGlow", card.root, FlipsideTheme.Glow, FlipsideTheme.WithAlpha(accent, 0.35f));
        FlipsideTheme.Place(card.iconGlow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(110f, 110f));

        if (icon != null)
        {
            icon.transform.SetParent(card.root, false);
            FlipsideTheme.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 0f), new Vector2(62f, 62f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        // Name
        TextMeshProUGUI nameText = FlipsideTheme.CreateText("Name", card.root, name, FlipsideTheme.TextRole.Eyebrow, accent, TextAlignmentOptions.Left);
        nameText.fontSize = 20;
        nameText.characterSpacing = 18;
        FlipsideTheme.Place(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(98f, -12f), new Vector2(120f, 28f));

        // Status chip
        card.chipBg = FlipsideTheme.CreateImage("Chip", card.root, FlipsideTheme.Rounded, FlipsideTheme.WithAlpha(accent, 0.9f), true);
        card.chipBg.pixelsPerUnitMultiplier = 3f;
        FlipsideTheme.Place(card.chipBg.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(98f + nameText.GetPreferredValues(name).x + 12f, -14f), new Vector2(84f, 24f));
        card.chipText = FlipsideTheme.CreateText("ChipText", card.chipBg.transform, "ACTIVE", FlipsideTheme.TextRole.Key, FlipsideTheme.Ink);
        card.chipText.fontSize = 14;
        card.chipText.characterSpacing = 6;
        FlipsideTheme.Stretch(card.chipText.rectTransform);

        // Timer text (driven by LevelTimer)
        if (timerText != null)
        {
            timerText.transform.SetParent(card.root, false);
            FlipsideTheme.StyleText(timerText, FlipsideTheme.TextRole.Timer, timerText.color);
            timerText.alignment = TextAlignmentOptions.Left;
            timerText.margin = Vector4.zero;
            FlipsideTheme.Place(timerText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(98f, 34f), new Vector2(200f, 54f));
        }

        // Frost icon (shown by LevelTimer while this timer is slowed)
        if (frost != null)
        {
            bool wasActive = frost.gameObject.activeSelf;
            frost.transform.SetParent(card.root, false);
            FlipsideTheme.Place(frost.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-18f, 34f), new Vector2(34f, 34f));
            frost.preserveAspect = true;
            frost.raycastTarget = false;
            frost.gameObject.SetActive(wasActive);
        }

        return card;
    }

    // ------------------------------------------------------------------ //
    // Level label
    // ------------------------------------------------------------------ //
    private void BuildLevelLabel()
    {
        levelLabel = ui.LevelLabel;
        if (levelLabel == null) return;

        labelPill = FlipsideTheme.CreateImage("LevelPill", hudRoot, FlipsideTheme.Rounded, FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.82f), true);
        RectTransform pill = labelPill.rectTransform;
        FlipsideTheme.Place(pill, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(300f, 60f));

        var layout = labelPill.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var fit = labelPill.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        Image outline = FlipsideTheme.CreateImage("Outline", pill, FlipsideTheme.Outline, FlipsideTheme.WithAlpha(Color.white, 0.14f), true);
        outline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        FlipsideTheme.Stretch(outline.rectTransform);

        labelStrip = FlipsideTheme.CreateImage("Strip", pill, FlipsideTheme.BrandGradient, Color.white);
        labelStrip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        RectTransform s = labelStrip.rectTransform;
        s.anchorMin = new Vector2(0f, 0f); s.anchorMax = new Vector2(1f, 0f);
        s.pivot = new Vector2(0.5f, 0f);
        s.offsetMin = new Vector2(24f, 0f); s.offsetMax = new Vector2(-24f, 3f);

        levelLabel.transform.SetParent(pill, false);
        FlipsideTheme.StyleText(levelLabel, FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.TextPrimary);
        levelLabel.fontSize = 26;
        levelLabel.characterSpacing = 16;
        levelLabel.alignment = TextAlignmentOptions.Center;
        levelLabel.margin = Vector4.zero;
        levelLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        lastLabelText = null;
    }

    // ------------------------------------------------------------------ //
    // Pause button
    // ------------------------------------------------------------------ //
    private void BuildPauseButton()
    {
        Transform t = FindDeep(canvas.transform, "PauseButton");
        if (t == null) return;
        Button btn = t.GetComponent<Button>();
        if (btn == null) return;

        t.SetParent(hudRoot, false);
        FlipsideTheme.Place((RectTransform)t, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(60f, 60f));
        FlipsideButton.Restyle(btn, FlipsideButton.Variant.Icon, "II").HudMode();

        // Wire it in code so it works in every level (only Level 01 had it set up in the scene)
        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
            btn.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        btn.onClick.AddListener(() =>
        {
            PauseManager pm = PauseManager.Instance != null ? PauseManager.Instance : FindFirstObjectByType<PauseManager>();
            if (pm != null) pm.TogglePause();
        });
    }

    // ------------------------------------------------------------------ //
    // Control prompts
    // ------------------------------------------------------------------ //
    private void BuildPrompts()
    {
        Image bg = FlipsideTheme.CreateImage("ControlPrompts", hudRoot, FlipsideTheme.Rounded, FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.62f), true);
        RectTransform root = bg.rectTransform;
        FlipsideTheme.Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(300f, 56f));

        var h = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(14, 18, 9, 9);
        h.spacing = 22f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        FlipsideTheme.CreatePrompt(root, new[] { "TAB" }, "Switch", FlipsideTheme.TextPrimary);
        FlipsideTheme.CreatePrompt(root, new[] { "ESC" }, "Pause", FlipsideTheme.TextPrimary);
    }

    // ------------------------------------------------------------------ //
    // Tutorial panel (Level 01)
    // ------------------------------------------------------------------ //
    private void StyleTutorial()
    {
        TutorialManager tut = FindFirstObjectByType<TutorialManager>();
        if (tut == null || tut.TutorialPanel == null) return;

        RectTransform panel = tut.TutorialPanel.transform as RectTransform;
        if (panel == null) return;

        FlipsideTheme.Place(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(1040f, 150f));

        // Keep it above the HUD but behind the pause and level complete screens
        if (panel.parent == hudRoot.parent)
            panel.SetSiblingIndex(hudRoot.GetSiblingIndex() + 1);
        Image img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = FlipsideTheme.Rounded;
            img.type = Image.Type.Sliced;
            img.color = FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.9f);
            img.raycastTarget = false;
        }
        FlipsideTheme.DecorateCard(panel);

        if (tut.MainText != null)
        {
            FlipsideTheme.StyleText(tut.MainText, FlipsideTheme.TextRole.Header, FlipsideTheme.TextPrimary);
            tut.MainText.fontSize = 40;
            tut.MainText.characterSpacing = 8;
            tut.MainText.alignment = TextAlignmentOptions.Center;
            tut.MainText.margin = Vector4.zero;
            RectTransform m = tut.MainText.rectTransform;
            m.anchorMin = new Vector2(0f, 1f); m.anchorMax = new Vector2(1f, 1f);
            m.pivot = new Vector2(0.5f, 1f);
            m.offsetMin = new Vector2(30f, -78f); m.offsetMax = new Vector2(-30f, -20f);
            m.SetAsLastSibling();
        }
        if (tut.SubText != null)
        {
            FlipsideTheme.StyleText(tut.SubText, FlipsideTheme.TextRole.Body, FlipsideTheme.TextMuted);
            tut.SubText.fontSize = 24;
            tut.SubText.alignment = TextAlignmentOptions.Top;
            tut.SubText.margin = Vector4.zero;
            RectTransform s = tut.SubText.rectTransform;
            s.anchorMin = new Vector2(0f, 0f); s.anchorMax = new Vector2(1f, 1f);
            s.offsetMin = new Vector2(40f, 10f); s.offsetMax = new Vector2(-40f, -84f);
            s.SetAsLastSibling();
        }
    }

    // ------------------------------------------------------------------ //
    // The switch flash line (UIManager flickers it on every TAB press)
    // ------------------------------------------------------------------ //
    private void StyleFlashLine()
    {
        GameObject line = ui.DividingLine;
        if (line == null) return;

        RectTransform rt = line.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 46f);
        }
        Image img = line.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = FlipsideTheme.VerticalFade;
            img.type = Image.Type.Simple;
            img.color = new Color(1f, 1f, 1f, 0.95f);
            img.raycastTarget = false;
        }
        ui.CacheDividingLineColour();
        line.SetActive(false); // the permanent split line replaces the resting state
    }

    // ------------------------------------------------------------------ //
    // Per frame
    // ------------------------------------------------------------------ //
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool arcActive = ArcActive();

        if (arcActive != lastArcActive)
        {
            lastArcActive = arcActive;
            (arcActive ? arcCard : echoCard).punch = 1f;
        }

        frameBlend = Mathf.MoveTowards(frameBlend, arcActive ? 1f : 0f, dt * 5f);
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f);

        if (arcFrame != null) arcFrame.color = FlipsideTheme.WithAlpha(FlipsideTheme.ArcBlue, 0.75f * frameBlend * pulse);
        if (echoFrame != null) echoFrame.color = FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.75f * (1f - frameBlend) * pulse);

        UpdateCard(arcCard, frameBlend, dt);
        UpdateCard(echoCard, 1f - frameBlend, dt);

        // Light sweep along the split line
        if (sweep != null)
        {
            float width = hudRoot.rect.width;
            float t = Mathf.Repeat(Time.unscaledTime / 4.5f, 1f);
            sweep.anchoredPosition = new Vector2(Mathf.Lerp(-200f, width + 200f, t), 0f);
        }

        // Tint the label when it tells the player who to guide
        if (levelLabel != null && levelLabel.text != lastLabelText)
        {
            lastLabelText = levelLabel.text;
            string upper = lastLabelText.ToUpperInvariant();
            Color c = upper.Contains("ECHO") ? FlipsideTheme.EchoPink
                    : upper.Contains("ARC") ? FlipsideTheme.ArcBlue
                    : FlipsideTheme.TextPrimary;
            levelLabel.color = c;
            if (labelPill != null)
                LayoutRebuilder.MarkLayoutForRebuild(labelPill.rectTransform);
        }
    }

    private void UpdateCard(CharacterCard card, float active, float dt)
    {
        if (card == null || card.root == null) return;

        card.punch = Mathf.MoveTowards(card.punch, 0f, dt * 3.5f);
        float punch = Mathf.Sin(card.punch * Mathf.PI) * 0.08f;

        float scale = Mathf.Lerp(0.92f, 1f, active) + punch;
        card.root.localScale = new Vector3(scale, scale, 1f);
        card.group.alpha = Mathf.Lerp(0.72f, 1f, active);

        if (card.outline != null) card.outline.color = FlipsideTheme.WithAlpha(card.accent, Mathf.Lerp(0.15f, 0.95f, active));
        if (card.iconGlow != null) card.iconGlow.color = FlipsideTheme.WithAlpha(card.accent, Mathf.Lerp(0.08f, 0.45f, active));

        bool isActive = active > 0.5f;
        if (card.chipText != null)
        {
            string want = isActive ? "ACTIVE" : "SLOWED";
            if (card.chipText.text != want) card.chipText.text = want;
            card.chipText.color = isActive ? FlipsideTheme.Ink : FlipsideTheme.ArcIce;
        }
        if (card.chipBg != null)
            card.chipBg.color = isActive ? FlipsideTheme.WithAlpha(card.accent, 0.95f) : FlipsideTheme.WithAlpha(Color.white, 0.12f);
    }

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //
    private Image FindImage(string objectName)
    {
        Transform t = FindDeep(canvas.transform, objectName);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.Trim() == objectName) return child;
        return null;
    }
}
