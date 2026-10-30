using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Builds plain uGUI elements from code, so the prototype runs with no scenes or prefabs to set up.
    /// The art pass will replace these with designed prefabs.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Background = new Color(0.07f, 0.09f, 0.14f);
        public static readonly Color Card = new Color(0.13f, 0.16f, 0.23f);
        public static readonly Color Accent = new Color(0.25f, 0.55f, 0.95f);
        public static readonly Color Muted = new Color(0.62f, 0.67f, 0.76f);
        public static readonly Color Warning = new Color(0.95f, 0.55f, 0.25f);

        static Font _font;
        /// <summary>The bundled Korean game font (Jua, SIL OFL), or Unity's built-in font without it.</summary>
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.Load<Font>("Fonts/Jua-Regular");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ---------- Generated sprites ----------

        const int CornerRadius = 24;
        static Sprite _rounded, _circle;

        /// <summary>A white rounded rectangle, nine-sliced so it stretches to any size.</summary>
        public static Sprite RoundedSprite => _rounded != null ? _rounded : (_rounded = MakeRounded(CornerRadius * 2 + 4, CornerRadius));

        /// <summary>A white disc.</summary>
        public static Sprite CircleSprite => _circle != null ? _circle : (_circle = MakeRounded(128, 64, sliced: false));

        static Sprite MakeRounded(int size, int radius, bool sliced = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Distance outside the rounded shape, antialiased over one pixel.
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
                    byte a = (byte)(255 * Mathf.Clamp01(0.5f - d));
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            var border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>The button skin (Kenney UI Pack, a light grey glossy button tinted per button), or null.</summary>
        static Sprite ButtonSprite => ArtLibrary.Get("UI/button");

        /// <summary>A vertical gradient from top to bottom.</summary>
        public static Sprite GradientSprite(Color top, Color bottom)
        {
            const int h = 128;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++) tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (h - 1f)));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f));
        }

        public static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        public static Color TeamColor(string teamName) =>
            ColorUtility.TryParseHtmlString(BaseballSim.Career.League.TeamColors.TryGetValue(teamName, out var hex) ? hex : "#888888", out var c) ? c : Color.gray;

        // ---------- Screens ----------

        /// <summary>
        /// The event system that turns taps into button clicks, made once. It reads the new Input
        /// System when the project uses it, and the old Input Manager otherwise.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
#if ENABLE_INPUT_SYSTEM && HAS_INPUT_SYSTEM
            var module = typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule);
#else
            var module = typeof(StandaloneInputModule);
#endif
            var go = new GameObject("EventSystem", typeof(EventSystem), module);
            Object.DontDestroyOnLoad(go);
        }

        /// <summary>A full-screen canvas laid out for a 1080x1920 phone, drawn above lower sorting orders.</summary>
        public static GameObject Canvas(Transform parent, string name, int sortingOrder = 0)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        // ---------- Elements ----------

        /// <summary>
        /// A coloured box with rounded corners. Cards get a soft drop shadow; pass rounded: false
        /// for things that fill the screen.
        /// </summary>
        public static RectTransform Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = RoundedSprite;
                image.type = Image.Type.Sliced;
            }
            if (rounded && color == Card) AddShadow(image, 8, 0.35f);
            return (RectTransform)go.transform;
        }

        static void AddShadow(Graphic g, float distance, float alpha)
        {
            if (g.GetComponent<Shadow>() != null) return;
            var shadow = g.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, alpha);
            shadow.effectDistance = new Vector2(0, -distance);
        }

        /// <summary>The dimmed full-screen layer behind a popup; it fades in.</summary>
        public static RectTransform Dim(Transform parent, string name = "Overlay", bool animate = true)
        {
            var dim = Panel(parent, name, new Color(0, 0, 0, 0.75f), rounded: false);
            Stretch(dim);
            if (animate) UiAnim.FadeIn(dim);
            return dim;
        }

        /// <summary>A popup card on a Dim layer; it pops in.</summary>
        public static RectTransform PopupCard(RectTransform dim, Vector2 anchorMin, Vector2 anchorMax, bool animate = true)
        {
            var card = Panel(dim, "Card", Card);
            card.anchorMin = anchorMin;
            card.anchorMax = anchorMax;
            card.offsetMin = card.offsetMax = Vector2.zero;
            // A thin accent strip along the top edge.
            var strip = Panel(card, "Accent", Accent);
            strip.anchorMin = new Vector2(0.3f, 1);
            strip.anchorMax = new Vector2(0.7f, 1);
            strip.offsetMin = new Vector2(0, -8);
            strip.offsetMax = new Vector2(0, -2);
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            if (animate) UiAnim.PopIn(card);
            return card;
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// A screen's root: an optional full-screen background, and inside it the area clear of
        /// notches where the layout goes.
        /// </summary>
        public static RectTransform SafeRoot(Transform canvas, Color? background = null)
        {
            RectTransform back;
            if (background.HasValue)
            {
                back = Panel(canvas, "Background", Color.white, rounded: false);
                var c = background.Value;
                back.GetComponent<Image>().sprite = GradientSprite(Shade(c, 1.9f) + new Color(0, 0.02f, 0.06f), Shade(c, 0.6f));
            }
            else back = Empty(canvas, "Background");
            Stretch(back);
            var root = Empty(back, "Root");
            Stretch(root);
            root.gameObject.AddComponent<SafeArea>();
            return root;
        }

        /// <summary>
        /// Puts a picture behind a SafeRoot's layout, cropped to fill the screen and darkened so
        /// text stays readable. Does nothing when there's no picture.
        /// </summary>
        public static void Backdrop(RectTransform safeRoot, Sprite art, float darken = 0.6f)
        {
            if (art == null) return;
            var back = safeRoot.parent;
            var pic = Empty(back, "Backdrop");
            pic.SetAsFirstSibling();
            var image = pic.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.raycastTarget = false;
            var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
            var shade = Panel(back, "Shade", new Color(0, 0, 0, darken), rounded: false);
            Stretch(shade);
            shade.GetComponent<Image>().raycastTarget = false;
            shade.SetSiblingIndex(1);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, Color? color = null)
        {
            var rt = Panel(parent, "Button " + label, color ?? Accent);
            var image = rt.GetComponent<Image>();
            var skin = ButtonSprite;
            if (skin != null)
            {
                image.sprite = skin;
                // Draw the nine-slice edges at double size, so the corners match the rounded cards.
                image.pixelsPerUnitMultiplier = 0.5f;
            }
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(UiSound.Tap);
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            colors.colorMultiplier = 1.2f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            // Kenney's button already has its own raised edge.
            if (skin == null) AddShadow(image, 6, 0.3f);
            rt.gameObject.AddComponent<PressScale>();
            var text = Label(rt, label, 34, TextAnchor.MiddleCenter);
            text.fontStyle = FontStyle.Bold;
            AddShadow(text, 2, 0.4f);
            Stretch(text.rectTransform);
            return button;
        }

        public static void SetButtonLabel(Button button, string label) =>
            button.GetComponentInChildren<Text>().text = label;

        /// <summary>Puts an icon at the left of a button, when there's a picture for it.</summary>
        public static void ButtonIcon(Button button, Sprite icon, float size = 80)
        {
            if (icon == null) return;
            var rt = Empty(button.transform, "Icon");
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            rt.anchorMin = new Vector2(0, 0.15f);
            rt.anchorMax = new Vector2(0, 0.85f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(14, 0);
            rt.sizeDelta = new Vector2(size, 0);
            var text = button.GetComponentInChildren<Text>().rectTransform;
            text.offsetMin = new Vector2(size, text.offsetMin.y);
        }

        public static VerticalLayoutGroup Column(RectTransform rt, int padding, int spacing)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, int spacing)
        {
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement Height(Component c, float preferred, float flexible = 0)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>(); // not ??: Unity overrides == null
            le.preferredHeight = preferred;
            le.flexibleHeight = flexible;
            return le;
        }

        public static LayoutElement Width(Component c, float preferred, float flexible = 0)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>(); // not ??: Unity overrides == null
            le.preferredWidth = preferred;
            le.flexibleWidth = flexible;
            return le;
        }

        /// <summary>A horizontal bar; returns the fill so callers can set its width with SetBar.</summary>
        public static RectTransform Bar(Transform parent, Color fillColor)
        {
            var back = Panel(parent, "Bar", new Color(1, 1, 1, 0.08f));
            var fill = Panel(back, "Fill", fillColor);
            // A bright top half reads as a lit tube instead of a flat block.
            var gloss = Panel(fill, "Gloss", new Color(1, 1, 1, 0.18f));
            gloss.anchorMin = new Vector2(0, 0.5f);
            gloss.anchorMax = Vector2.one;
            gloss.offsetMin = gloss.offsetMax = Vector2.zero;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            return fill;
        }

        public static void SetBar(RectTransform fill, float fraction) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);

        /// <summary>A vertical scroll area; returns the content, which grows with what's added to it.</summary>
        public static RectTransform ScrollList(Transform parent, int spacing)
        {
            var viewport = Empty(parent, "Scroll");
            viewport.gameObject.AddComponent<RectMask2D>();
            // Something has to catch drags in the gaps between rows.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var content = Empty(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = Column(content, 0, spacing);
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        /// <summary>One table row of fixed-width cells; a width of 0 takes the remaining space.</summary>
        public static RectTransform TableRow(Transform parent, float[] widths, string[] cells, int size, Color color, Color? background = null)
        {
            var row = background.HasValue ? Panel(parent, "Row", background.Value) : Empty(parent, "Row");
            Height(row, size + 30);
            var layout = Row(row, 6);
            layout.padding = new RectOffset(12, 12, 0, 0);
            layout.childForceExpandWidth = false;
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = Label(row, cells[i], size, i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, color);
                Width(cell, widths[i], widths[i] == 0 ? 1 : 0);
            }
            return row;
        }
    }
}
