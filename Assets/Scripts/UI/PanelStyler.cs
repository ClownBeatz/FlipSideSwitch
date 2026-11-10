// PanelStyler.cs
// Restyles the existing Pause and Level Complete panels at runtime so they match
// the FLIPSIDE look. Called from PauseManager.Start and LevelCompleteUI.Awake.
// Only layout and visuals change. Button wiring stays in the original scripts.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class PanelStyler
{
    // ------------------------------------------------------------------ //
    // Pause
    // ------------------------------------------------------------------ //
    public static void StylePause(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI sub,
        Button resume, Button restart, Button menu, Button quit)
    {
        if (panel == null) return;
        RectTransform root = panel.transform as RectTransform;
        if (root == null) return;

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas != null) FlipsideTheme.ConfigureScaler(canvas.rootCanvas);

        // Panel becomes the full-screen dim backdrop
        FlipsideTheme.Stretch(root);
        Image bg = panel.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = null;
            bg.color = FlipsideTheme.WithAlpha(FlipsideTheme.EchoDeep, 0.72f);
            bg.raycastTarget = true;
        }

        RectTransform card = FlipsideTheme.CreateCard("PauseCard", root, new Vector2(620f, 620f));
        card.gameObject.AddComponent<UIPopIn>();

        TextMeshProUGUI eyebrow = FlipsideTheme.CreateText("Eyebrow", card, "TIME SLOWED", FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.ArcBlue);
        TopAnchor(eyebrow.rectTransform, -52f, 30f);

        if (title != null)
        {
            title.transform.SetParent(card, false);
            FlipsideTheme.StyleText(title, FlipsideTheme.TextRole.Header, FlipsideTheme.TextPrimary);
            title.fontSize = 76;
            title.text = "PAUSED";
            title.alignment = TextAlignmentOptions.Center;
            title.margin = Vector4.zero;
            TopAnchor(title.rectTransform, -84f, 88f);
            FlipsideTheme.AddGlow(title, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.55f), 0.7f, 0.2f);
        }

        if (sub != null) sub.gameObject.SetActive(false); // replaced by the eyebrow line

        RectTransform stack = FlipsideTheme.CreateRect("Buttons", card);
        FlipsideTheme.Place(stack, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(FlipsideTheme.ButtonWidth, 300f));
        var v = stack.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = FlipsideTheme.ButtonSpacing;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        AddToStack(stack, resume, FlipsideButton.Variant.Primary, "RESUME");
        AddToStack(stack, restart, FlipsideButton.Variant.Secondary, "RESTART LEVEL");
        AddToStack(stack, menu, FlipsideButton.Variant.Secondary, "MAIN MENU");
        AddToStack(stack, quit, FlipsideButton.Variant.Secondary, "QUIT GAME");

        // Footer reminder
        RectTransform footer = FlipsideTheme.CreateRect("Footer", card);
        FlipsideTheme.Place(footer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(400f, 40f));
        var h = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 26f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        FlipsideTheme.CreatePrompt(footer, new[] { "ESC" }, "Resume", FlipsideTheme.TextMuted);
    }

    private static void AddToStack(RectTransform stack, Button btn, FlipsideButton.Variant variant, string label)
    {
        if (btn == null) return;
        btn.transform.SetParent(stack, false);
        FlipsideTheme.AddLayout(btn.gameObject, FlipsideTheme.ButtonWidth, FlipsideTheme.ButtonHeight);
        FlipsideButton.Restyle(btn, variant, label);
    }

    // ------------------------------------------------------------------ //
    // Level complete
    // ------------------------------------------------------------------ //
    public class LevelCompleteParts
    {
        public Image backdrop;
        public TextMeshProUGUI eyebrow;
        public TextMeshProUGUI arcTime;
        public TextMeshProUGUI echoTime;
        public Button menuButton;
    }

    public static LevelCompleteParts StyleLevelComplete(RectTransform panel, TextMeshProUGUI header, TextMeshProUGUI sub,
        Button next, Button retry, bool isLastLevel)
    {
        var parts = new LevelCompleteParts();
        if (panel == null) return parts;

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas != null) FlipsideTheme.ConfigureScaler(canvas.rootCanvas);

        // Backdrop as a sibling right behind the card (the card itself slides in)
        parts.backdrop = FlipsideTheme.CreateBackdrop(panel.parent, 0.65f);
        parts.backdrop.name = "LevelCompleteBackdrop";
        parts.backdrop.transform.SetSiblingIndex(panel.GetSiblingIndex());
        parts.backdrop.color = FlipsideTheme.WithAlpha(FlipsideTheme.EchoDeep, 0f);

        // Card
        FlipsideTheme.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740f, 580f));
        Image bg = panel.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = FlipsideTheme.Rounded;
            bg.type = Image.Type.Sliced;
            bg.color = FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.96f);
            bg.raycastTarget = true;
        }
        FlipsideTheme.DecorateCard(panel);

        parts.eyebrow = FlipsideTheme.CreateText("Eyebrow", panel, "", FlipsideTheme.TextRole.Eyebrow, FlipsideTheme.ArcBlue);
        TopAnchor(parts.eyebrow.rectTransform, -50f, 30f);

        if (header != null)
        {
            FlipsideTheme.StyleText(header, FlipsideTheme.TextRole.Header, FlipsideTheme.TextPrimary);
            header.text = isLastLevel ? "ALL LEVELS CLEAR" : "LEVEL COMPLETE";
            header.fontSize = 62;
            header.enableAutoSizing = true;
            header.fontSizeMax = 62;
            header.fontSizeMin = 36;
            header.alignment = TextAlignmentOptions.Center;
            header.margin = Vector4.zero;
            TopAnchor(header.rectTransform, -84f, 80f);
            header.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -124f);
            FlipsideTheme.AddGlow(header, FlipsideTheme.WithAlpha(FlipsideTheme.EchoNeon, 0.6f), 0.7f, 0.2f);
        }

        if (sub != null)
        {
            FlipsideTheme.StyleText(sub, FlipsideTheme.TextRole.Body, FlipsideTheme.TextMuted);
            sub.fontSize = 26;
            sub.alignment = TextAlignmentOptions.Center;
            sub.margin = Vector4.zero;
            TopAnchor(sub.rectTransform, -170f, 40f);
        }

        // Time left for each character
        RectTransform stats = FlipsideTheme.CreateRect("Stats", panel);
        FlipsideTheme.Place(stats, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(560f, 110f));
        parts.arcTime = BuildStat(stats, "ARC", FlipsideTheme.ArcBlue, -142f);
        parts.echoTime = BuildStat(stats, "ECHO", FlipsideTheme.EchoNeon, 142f);

        // Buttons
        RectTransform row = FlipsideTheme.CreateRect("Buttons", panel);
        FlipsideTheme.Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(640f, FlipsideTheme.ButtonHeight));
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = FlipsideTheme.ButtonSpacing;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        if (next != null)
        {
            next.transform.SetParent(row, false);
            FlipsideTheme.AddLayout(next.gameObject, 250f, FlipsideTheme.ButtonHeight);
            FlipsideButton.Restyle(next, FlipsideButton.Variant.Primary, isLastLevel ? "MAIN MENU" : "NEXT LEVEL");
            EnsureCanvasGroup(next.gameObject);
        }
        if (retry != null)
        {
            retry.transform.SetParent(row, false);
            FlipsideTheme.AddLayout(retry.gameObject, 170f, FlipsideTheme.ButtonHeight);
            FlipsideButton.Restyle(retry, FlipsideButton.Variant.Secondary, "RETRY");
            EnsureCanvasGroup(retry.gameObject);
        }
        if (!isLastLevel)
        {
            FlipsideButton menu = FlipsideButton.Create(row, "MENU", FlipsideButton.Variant.Secondary, null, 170f);
            EnsureCanvasGroup(menu.gameObject);
            parts.menuButton = menu.Button;
        }

        return parts;
    }

    private static TextMeshProUGUI BuildStat(RectTransform parent, string name, Color accent, float x)
    {
        Image box = FlipsideTheme.CreateImage("Stat" + name, parent, FlipsideTheme.Rounded, FlipsideTheme.WithAlpha(Color.white, 0.05f), true);
        FlipsideTheme.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(264f, 110f));
        Image line = FlipsideTheme.CreateImage("Outline", box.transform, FlipsideTheme.Outline, FlipsideTheme.WithAlpha(accent, 0.45f), true);
        FlipsideTheme.Stretch(line.rectTransform);

        TextMeshProUGUI label = FlipsideTheme.CreateText("Label", box.transform, name + "  TIME LEFT", FlipsideTheme.TextRole.Key, accent);
        label.fontSize = 16;
        label.characterSpacing = 10;
        TopAnchor(label.rectTransform, -16f, 24f);

        TextMeshProUGUI value = FlipsideTheme.CreateText("Value", box.transform, "0:00.00", FlipsideTheme.TextRole.Timer, FlipsideTheme.TextPrimary);
        value.fontSize = 44;
        TopAnchor(value.rectTransform, -44f, 54f);
        return value;
    }

    public static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        int cs = Mathf.FloorToInt((seconds * 100f) % 100f);
        return $"<mspace=0.56em>{m}:{s:00}.{cs:00}</mspace>";
    }

    // ------------------------------------------------------------------ //
    private static void TopAnchor(RectTransform rt, float top, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(32f, top - height);
        rt.offsetMax = new Vector2(-32f, top);
        rt.localScale = Vector3.one;
    }

    private static void EnsureCanvasGroup(GameObject go)
    {
        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
    }
}
