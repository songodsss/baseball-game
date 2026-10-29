using UnityEngine;
using UnityEngine.Events;
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
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        public static RectTransform Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
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
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            var text = Label(rt, label, 34, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        public static void SetButtonLabel(Button button, string label) =>
            button.GetComponentInChildren<Text>().text = label;

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
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            return fill;
        }

        public static void SetBar(RectTransform fill, float fraction) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
    }
}
