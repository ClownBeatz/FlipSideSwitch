// FlipsideTheme.cs
// Shared visual identity for every FLIPSIDE screen: palette, fonts, generated
// sprites and small UI builder helpers. Everything is created at runtime, so no
// scene or prefab edits are needed.
//
// Fonts: Chakra Petch (SIL Open Font License) loaded from Assets/Resources/Fonts.
// If the font files are missing the default TMP font is used instead.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class FlipsideTheme
{
    // ------------------------------------------------------------------ //
    // Palette
    // ------------------------------------------------------------------ //
    public static readonly Color ArcIce      = Hex("EAF6FF");
    public static readonly Color ArcBlue     = Hex("8FD3FF");
    public static readonly Color ArcInk      = Hex("1B2440");   // dark text on the light world
    public static readonly Color EchoNeon    = Hex("B66BFF");
    public static readonly Color EchoPink    = Hex("E07CFF");
    public static readonly Color EchoDeep    = Hex("140F24");

    public static readonly Color Ink         = Hex("0D0B18");   // panel base
    public static readonly Color InkSoft     = Hex("1A1630");
    public static readonly Color TextPrimary = Hex("F3F1FF");
    public static readonly Color TextMuted   = Hex("A7A3C2");
    public static readonly Color Gold        = Hex("FFD36B");   // key caps and highlights
    public static readonly Color Success     = Hex("74FFC1");
    public static readonly Color Danger      = Hex("FF5A78");

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // ------------------------------------------------------------------ //
    // Sizes (reference resolution 1920x1080)
    // ------------------------------------------------------------------ //
    public const float ButtonWidth   = 360f;
    public const float ButtonHeight  = 62f;
    public const float ButtonSpacing = 14f;
    public const float CardPadding   = 48f;

    // ------------------------------------------------------------------ //
    // Fonts
    // ------------------------------------------------------------------ //
    private static TMP_FontAsset displayFont;
    private static TMP_FontAsset bodyFont;
    private static bool fontsLoaded;

    public static TMP_FontAsset DisplayFont { get { LoadFonts(); return displayFont; } }
    public static TMP_FontAsset BodyFont    { get { LoadFonts(); return bodyFont; } }

    private static void LoadFonts()
    {
        if (fontsLoaded) return;
        fontsLoaded = true;

        TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
        displayFont = CreateDynamicFont("Fonts/ChakraPetch-Bold", fallback) ?? fallback;
        bodyFont    = CreateDynamicFont("Fonts/ChakraPetch-Medium", fallback) ?? fallback;
    }

    private static TMP_FontAsset CreateDynamicFont(string resourcePath, TMP_FontAsset fallback)
    {
        Font source = Resources.Load<Font>(resourcePath);
        if (source == null) return null;

        TMP_FontAsset asset;
        try
        {
            asset = TMP_FontAsset.CreateFontAsset(
                source, 64, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("FlipsideTheme: could not create font from " + resourcePath + ", using default. " + e.Message);
            return null;
        }
        if (asset == null) return null;

        asset.name = source.name + " (Runtime SDF)";

        // Use the same SDF shader as the default TMP font (Mobile SDF). That shader is
        // always in the build because the default font lives in a Resources folder.
        if (fallback != null && fallback.material != null && asset.material != null)
            asset.material.shader = fallback.material.shader;

        if (fallback != null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

        asset.TryAddCharacters(" ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?'\"-+/()[]%&");
        return asset;
    }

    // ------------------------------------------------------------------ //
    // Text styling
    // ------------------------------------------------------------------ //
    public enum TextRole { Title, Header, Eyebrow, Button, Body, Small, Timer, Key }

    public static void StyleText(TextMeshProUGUI t, TextRole role, Color colour)
    {
        if (t == null) return;

        t.color = colour;
        t.textWrappingMode = (role == TextRole.Body || role == TextRole.Small) ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.richText = true;
        t.enableAutoSizing = false;

        switch (role)
        {
            case TextRole.Title:
                t.font = DisplayFont; t.fontSize = 190; t.characterSpacing = 14; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Header:
                t.font = DisplayFont; t.fontSize = 64; t.characterSpacing = 10; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Eyebrow:
                t.font = DisplayFont; t.fontSize = 22; t.characterSpacing = 24; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Button:
                t.font = DisplayFont; t.fontSize = 26; t.characterSpacing = 12; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Body:
                t.font = BodyFont; t.fontSize = 28; t.characterSpacing = 1; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Small:
                t.font = BodyFont; t.fontSize = 20; t.characterSpacing = 4; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Timer:
                t.font = DisplayFont; t.fontSize = 46; t.characterSpacing = 0; t.fontStyle = FontStyles.Normal; break;
            case TextRole.Key:
                t.font = DisplayFont; t.fontSize = 18; t.characterSpacing = 6; t.fontStyle = FontStyles.Normal; break;
        }
    }

    // Soft glow behind text (neon look). Works on overlay canvases, no post needed.
    private static readonly Dictionary<string, Material> glowMaterials = new Dictionary<string, Material>();

    public static void AddGlow(TextMeshProUGUI t, Color glow, float softness = 0.55f, float dilate = 0.25f)
    {
        if (t == null || t.font == null || t.font.material == null) return;

        string key = t.font.GetInstanceID() + "_" + ColorUtility.ToHtmlStringRGBA(glow) + "_" + softness + "_" + dilate;
        if (!glowMaterials.TryGetValue(key, out Material mat) || mat == null)
        {
            mat = new Material(t.font.material) { name = "Flipside Glow " + key };
            // OUTLINE_ON + UNDERLAY_ON matches the TMP "Drop Shadow" material that ships in
            // Resources, so this shader variant is guaranteed to be in the build.
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", 0f);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", glow);
            mat.SetFloat("_UnderlaySoftness", softness);
            mat.SetFloat("_UnderlayDilate", dilate);
            mat.SetFloat("_UnderlayOffsetX", 0f);
            mat.SetFloat("_UnderlayOffsetY", 0f);
            glowMaterials[key] = mat;
        }
        t.fontSharedMaterial = mat;
    }

    // ------------------------------------------------------------------ //
    // Generated sprites
    // ------------------------------------------------------------------ //
    private static Sprite roundedSprite;
    private static Sprite outlineSprite;
    private static Sprite glowSprite;
    private static Sprite gradientSprite;
    private static Sprite verticalFadeSprite;
    private static Sprite dotSprite;

    /// Rounded rectangle (9-sliced), radius 16 px.
    public static Sprite Rounded => roundedSprite != null ? roundedSprite : (roundedSprite = MakeRoundedRect(64, 16f, 0f));
    /// Rounded rectangle outline (9-sliced), 2 px stroke.
    public static Sprite Outline => outlineSprite != null ? outlineSprite : (outlineSprite = MakeRoundedRect(64, 16f, 2.5f));
    /// Soft radial glow.
    public static Sprite Glow => glowSprite != null ? glowSprite : (glowSprite = MakeGlow(128));
    /// Horizontal gradient from ARC ice blue to ECHO neon.
    public static Sprite BrandGradient => gradientSprite != null ? gradientSprite : (gradientSprite = MakeBrandGradient());
    /// White at the centre line fading to clear above and below (for glowing lines).
    public static Sprite VerticalFade => verticalFadeSprite != null ? verticalFadeSprite : (verticalFadeSprite = MakeVerticalFade());
    /// Small soft dot for particles.
    public static Sprite Dot => dotSprite != null ? dotSprite : (dotSprite = MakeGlow(32));

    private static Sprite MakeRoundedRect(int size, float radius, float stroke)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = stroke > 0 ? "FlipsideOutline" : "FlipsideRounded"
        };

        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Signed distance to a rounded box
                float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(px, py), 0f);
                float dist = outside + inside - radius; // < 0 inside

                float a = Mathf.Clamp01(0.5f - dist);
                if (stroke > 0f)
                    a *= Mathf.Clamp01(dist + stroke + 0.5f);

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        float border = radius + 2f;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private static Sprite MakeGlow(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "FlipsideGlow"
        };
        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // smoothstep
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeBrandGradient()
    {
        const int w = 256;
        var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "FlipsideBrandGradient"
        };
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)(w - 1);
            Color c = t < 0.5f
                ? Color.Lerp(ArcIce, ArcBlue, t * 2f)
                : Color.Lerp(ArcBlue, EchoNeon, (t - 0.5f) * 2f);
            tex.SetPixel(x, 0, c);
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeVerticalFade()
    {
        const int h = 64;
        var tex = new Texture2D(1, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "FlipsideVerticalFade"
        };
        for (int y = 0; y < h; y++)
        {
            float d = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f; // 0 centre, 1 edge
            float a = Mathf.Pow(1f - d, 2.2f);
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
    }

    // Texture from Resources/FlipsideUI turned into a sprite.
    private static readonly Dictionary<string, Sprite> resourceSprites = new Dictionary<string, Sprite>();

    public static Sprite LoadSprite(string name, bool pixelArt = false)
    {
        if (resourceSprites.TryGetValue(name, out Sprite s) && s != null) return s;

        Sprite loaded = Resources.Load<Sprite>("FlipsideUI/" + name);
        if (loaded == null)
        {
            Texture2D tex = Resources.Load<Texture2D>("FlipsideUI/" + name);
            if (tex == null) return null;
            loaded = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        if (pixelArt && loaded.texture != null) loaded.texture.filterMode = FilterMode.Point;

        resourceSprites[name] = loaded;
        return loaded;
    }

    // ------------------------------------------------------------------ //
    // Builders
    // ------------------------------------------------------------------ //
    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image CreateImage(string name, Transform parent, Sprite sprite, Color colour, bool sliced = false)
    {
        RectTransform rt = CreateRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = colour;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, TextRole role, Color colour,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform rt = CreateRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(t, role, colour);
        t.alignment = align;
        t.text = text;
        return t;
    }

    /// Dark glass card with a brand gradient strip on top.
    public static RectTransform CreateCard(string name, Transform parent, Vector2 size)
    {
        Image bg = CreateImage(name, parent, Rounded, WithAlpha(Ink, 0.94f), true);
        RectTransform card = bg.rectTransform;
        Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        DecorateCard(card);
        return card;
    }

    /// Adds outline and gradient strip to an existing rect (used for restyled panels).
    public static void DecorateCard(RectTransform card)
    {
        Image outline = CreateImage("Outline", card, Outline, WithAlpha(Color.white, 0.10f), true);
        Stretch(outline.rectTransform);
        outline.transform.SetAsFirstSibling();

        Image strip = CreateImage("BrandStrip", card, BrandGradient, Color.white);
        RectTransform s = strip.rectTransform;
        s.anchorMin = new Vector2(0f, 1f);
        s.anchorMax = new Vector2(1f, 1f);
        s.pivot = new Vector2(0.5f, 1f);
        s.offsetMin = new Vector2(28f, -4f);
        s.offsetMax = new Vector2(-28f, 0f);

        Image stripGlow = CreateImage("BrandStripGlow", card, Glow, WithAlpha(EchoNeon, 0.18f));
        RectTransform g = stripGlow.rectTransform;
        g.anchorMin = new Vector2(0f, 1f);
        g.anchorMax = new Vector2(1f, 1f);
        g.pivot = new Vector2(0.5f, 0.5f);
        g.offsetMin = new Vector2(40f, -26f);
        g.offsetMax = new Vector2(-40f, 22f);
        stripGlow.transform.SetSiblingIndex(1);
    }

    /// Full-screen dim backdrop.
    public static Image CreateBackdrop(Transform parent, float alpha = 0.72f)
    {
        Image img = CreateImage("Backdrop", parent, null, WithAlpha(EchoDeep, alpha));
        Stretch(img.rectTransform);
        img.raycastTarget = true; // block clicks to whatever is behind
        return img;
    }

    /// Key cap like [TAB] for control prompts.
    public static RectTransform CreateKeyCap(Transform parent, string key, float minWidth = 56f)
    {
        Image cap = CreateImage("Key_" + key, parent, Rounded, WithAlpha(Color.white, 0.10f), true);
        Image capLine = CreateImage("Outline", cap.transform, Outline, WithAlpha(Gold, 0.65f), true);
        Stretch(capLine.rectTransform);

        TextMeshProUGUI label = CreateText("Label", cap.transform, key, TextRole.Key, Gold);
        Stretch(label.rectTransform);
        label.characterSpacing = 4;

        float width = Mathf.Max(minWidth, label.GetPreferredValues(key).x + 26f);
        RectTransform rt = cap.rectTransform;
        rt.sizeDelta = new Vector2(width, 38f);

        var le = cap.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 38f;
        return rt;
    }

    /// Row of key caps followed by a label, laid out horizontally.
    public static RectTransform CreatePrompt(Transform parent, string[] keys, string label, Color labelColour, float fontSize = 20f)
    {
        RectTransform row = CreateRect("Prompt_" + label, parent);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        var fit = row.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (string k in keys)
        {
            if (k == "/")
            {
                TextMeshProUGUI slash = CreateText("Or", row, "or", TextRole.Small, TextMuted);
                slash.fontSize = fontSize - 2f;
                AddLayout(slash.gameObject, slash.GetPreferredValues("or").x + 4f, 38f);
                continue;
            }
            CreateKeyCap(row, k);
        }

        TextMeshProUGUI text = CreateText("Label", row, label, TextRole.Small, labelColour, TextAlignmentOptions.Left);
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        AddLayout(text.gameObject, text.GetPreferredValues(label).x + 6f, 38f);
        return row;
    }

    public static LayoutElement AddLayout(GameObject go, float width, float height)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (width >= 0) le.preferredWidth = width;
        if (height >= 0) le.preferredHeight = height;
        return le;
    }

    /// Makes a canvas scale the same way on every screen: 1920x1080 reference, balanced match.
    public static void ConfigureScaler(Canvas canvas)
    {
        if (canvas == null) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    // ------------------------------------------------------------------ //
    // Easing
    // ------------------------------------------------------------------ //
    public static float EaseOutCubic(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
    public static float EaseInOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }
    public static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
