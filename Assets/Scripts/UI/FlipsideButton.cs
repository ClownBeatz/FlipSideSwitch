// FlipsideButton.cs
// Styled button with hover, press and keyboard selection states.
// Works while the game is paused (all animation uses unscaled time).
//
// Use FlipsideButton.Create(...) for new buttons, or FlipsideButton.Restyle(...)
// to turn an existing scene Button into a themed one. Existing onClick
// listeners are kept.

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FlipsideButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    public enum Variant { Primary, Secondary, Icon }

    private Button button;
    private Image background;
    private Image outline;
    private Image glow;
    private Image accentBar;
    private TextMeshProUGUI label;
    private Variant variant;

    private bool hovered;
    private bool selected;
    private bool pressed;
    private bool selectOnHover = true;
    private bool deselectOnClick;

    private float highlight;      // 0..1 animated
    private float pressAmount;    // 0..1 animated
    private Vector3 baseScale = Vector3.one;

    private Color bgNormal, bgHover, outlineNormal, outlineHover, labelNormal, labelHover;

    public Button Button => button;
    public TextMeshProUGUI Label => label;

    // ------------------------------------------------------------------ //
    // Factory
    // ------------------------------------------------------------------ //
    public static FlipsideButton Create(Transform parent, string text, Variant variant,
        UnityEngine.Events.UnityAction onClick, float width = FlipsideTheme.ButtonWidth,
        float height = FlipsideTheme.ButtonHeight)
    {
        RectTransform rt = FlipsideTheme.CreateRect("Button_" + text.Replace(" ", ""), parent);
        rt.sizeDelta = new Vector2(width, height);
        var img = rt.gameObject.AddComponent<Image>();
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        TextMeshProUGUI tmp = FlipsideTheme.CreateText("Label", rt, text, FlipsideTheme.TextRole.Button, Color.white);
        FlipsideTheme.Stretch(tmp.rectTransform);

        FlipsideTheme.AddLayout(rt.gameObject, width, height);

        FlipsideButton fb = Restyle(btn, variant);
        if (onClick != null) btn.onClick.AddListener(onClick);
        return fb;
    }

    public static FlipsideButton Restyle(Button btn, Variant variant, string newLabel = null)
    {
        if (btn == null) return null;
        FlipsideButton fb = btn.GetComponent<FlipsideButton>();
        if (fb == null) fb = btn.gameObject.AddComponent<FlipsideButton>();
        fb.Build(btn, variant, newLabel);
        return fb;
    }

    public FlipsideButton HudMode()
    {
        selectOnHover = false;
        deselectOnClick = true;
        return this;
    }

    // ------------------------------------------------------------------ //
    // Build visuals
    // ------------------------------------------------------------------ //
    private void Build(Button btn, Variant v, string newLabel)
    {
        button = btn;
        variant = v;
        button.transition = Selectable.Transition.None;

        background = btn.targetGraphic as Image;
        if (background == null) background = btn.GetComponent<Image>();
        if (background == null) background = btn.gameObject.AddComponent<Image>();
        background.sprite = FlipsideTheme.Rounded;
        background.type = Image.Type.Sliced;
        background.raycastTarget = true;
        background.pixelsPerUnitMultiplier = v == Variant.Icon ? 0.5f : 1f;
        btn.targetGraphic = background;

        label = btn.GetComponentInChildren<TextMeshProUGUI>(true);

        // Glow (sits behind label, extends past the edges)
        glow = FlipsideTheme.CreateImage("HoverGlow", transform, FlipsideTheme.Glow, Color.clear);
        RectTransform g = FlipsideTheme.Stretch(glow.rectTransform);
        g.offsetMin = new Vector2(-40f, -34f);
        g.offsetMax = new Vector2(40f, 34f);
        glow.transform.SetAsFirstSibling();

        outline = FlipsideTheme.CreateImage("Outline", transform, FlipsideTheme.Outline, Color.clear, true);
        FlipsideTheme.Stretch(outline.rectTransform);
        outline.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
        outline.transform.SetSiblingIndex(1);

        if (v != Variant.Icon)
        {
            accentBar = FlipsideTheme.CreateImage("AccentBar", transform, FlipsideTheme.Rounded, Color.clear, true);
            RectTransform a = accentBar.rectTransform;
            a.anchorMin = new Vector2(0f, 0.5f);
            a.anchorMax = new Vector2(0f, 0.5f);
            a.pivot = new Vector2(0f, 0.5f);
            a.anchoredPosition = new Vector2(10f, 0f);
            a.sizeDelta = new Vector2(4f, 22f);
            accentBar.pixelsPerUnitMultiplier = 8f;
            accentBar.transform.SetSiblingIndex(2);
        }

        if (label != null)
        {
            if (!string.IsNullOrEmpty(newLabel)) label.text = newLabel;
            else label.text = label.text.Trim().ToUpperInvariant();

            FlipsideTheme.StyleText(label, v == Variant.Icon ? FlipsideTheme.TextRole.Header : FlipsideTheme.TextRole.Button, Color.white);
            if (v == Variant.Icon) { label.fontSize = 30; label.characterSpacing = 0; }
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            FlipsideTheme.Stretch(label.rectTransform);
            label.transform.SetAsLastSibling();
        }

        switch (v)
        {
            case Variant.Primary:
                bgNormal = FlipsideTheme.Hex("7B3FD9");
                bgHover = FlipsideTheme.Hex("9B5CFF");
                outlineNormal = FlipsideTheme.WithAlpha(FlipsideTheme.EchoPink, 0.55f);
                outlineHover = FlipsideTheme.WithAlpha(Color.white, 0.95f);
                labelNormal = Color.white;
                labelHover = Color.white;
                break;
            case Variant.Secondary:
                bgNormal = FlipsideTheme.WithAlpha(FlipsideTheme.InkSoft, 0.92f);
                bgHover = FlipsideTheme.Hex("2A2150");
                outlineNormal = FlipsideTheme.WithAlpha(FlipsideTheme.ArcBlue, 0.22f);
                outlineHover = FlipsideTheme.WithAlpha(FlipsideTheme.ArcBlue, 0.95f);
                labelNormal = FlipsideTheme.TextPrimary;
                labelHover = Color.white;
                break;
            default: // Icon
                bgNormal = FlipsideTheme.WithAlpha(FlipsideTheme.Ink, 0.70f);
                bgHover = FlipsideTheme.WithAlpha(FlipsideTheme.InkSoft, 0.95f);
                outlineNormal = FlipsideTheme.WithAlpha(Color.white, 0.18f);
                outlineHover = FlipsideTheme.WithAlpha(FlipsideTheme.EchoPink, 0.95f);
                labelNormal = FlipsideTheme.TextPrimary;
                labelHover = Color.white;
                break;
        }

        // Click feedback
        button.onClick.AddListener(OnClicked);

        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        Apply(true);
    }

    private void OnClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.SFX.ButtonClick);

        if (deselectOnClick && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ------------------------------------------------------------------ //
    // State handlers
    // ------------------------------------------------------------------ //
    public void OnPointerEnter(PointerEventData e)
    {
        hovered = true;
        if (selectOnHover && button != null && button.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { if (button != null && button.interactable) pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }

    private void OnDisable()
    {
        hovered = selected = pressed = false;
        highlight = 0f;
        pressAmount = 0f;
        transform.localScale = baseScale;
        Apply(true);
    }

    // ------------------------------------------------------------------ //
    // Animate
    // ------------------------------------------------------------------ //
    private void Update()
    {
        if (button == null) return;
        Apply(false);
    }

    private void Apply(bool instant)
    {
        bool interactable = button == null || button.interactable;
        float targetHighlight = interactable && (hovered || selected) ? 1f : 0f;
        float targetPress = interactable && pressed ? 1f : 0f;

        float dt = Time.unscaledDeltaTime;
        highlight = instant ? targetHighlight : Mathf.MoveTowards(highlight, targetHighlight, dt * 7f);
        pressAmount = instant ? targetPress : Mathf.MoveTowards(pressAmount, targetPress, dt * 14f);

        float h = FlipsideTheme.EaseOutCubic(highlight);

        if (background != null) background.color = Color.Lerp(bgNormal, bgHover, h);
        if (outline != null) outline.color = Color.Lerp(outlineNormal, outlineHover, h);
        if (label != null) label.color = Color.Lerp(labelNormal, labelHover, h);

        if (glow != null)
        {
            Color gc = variant == Variant.Secondary ? FlipsideTheme.ArcBlue : FlipsideTheme.EchoNeon;
            float baseGlow = variant == Variant.Primary ? 0.18f : 0f;
            float pulse = 0.04f * Mathf.Sin(Time.unscaledTime * 3f) * h;
            glow.color = FlipsideTheme.WithAlpha(gc, baseGlow + 0.30f * h + pulse);
        }

        if (accentBar != null)
        {
            Color ac = variant == Variant.Primary ? Color.white : FlipsideTheme.ArcBlue;
            accentBar.color = FlipsideTheme.WithAlpha(ac, h);
            accentBar.rectTransform.sizeDelta = new Vector2(4f, Mathf.Lerp(8f, 26f, h));
        }

        float scale = 1f + 0.035f * h - 0.05f * pressAmount;
        transform.localScale = baseScale * scale;
    }
}
