using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Bright outdoor pieces drawn from soft circles: a sky, drifting clouds, grass, trees, bushes,
    /// flowers and a pond. Shared by the start screen and the home town. Nothing here takes taps.
    /// </summary>
    public static class Scenery
    {
        public static readonly Color SkyTop = new Color(0.42f, 0.72f, 0.98f);
        public static readonly Color SkyBottom = new Color(0.82f, 0.93f, 1f);
        public static readonly Color Grass = new Color(0.55f, 0.8f, 0.4f);
        public static readonly Color GrassLight = new Color(0.68f, 0.88f, 0.48f, 0.55f);
        public static readonly Color GrassDark = new Color(0.45f, 0.71f, 0.34f, 0.45f);
        public static readonly Color Path = new Color(0.86f, 0.76f, 0.58f);
        public static readonly Color PathEdge = new Color(0.72f, 0.6f, 0.42f);
        /// <summary>Navy for text plates and chips over the bright scenery.</summary>
        public static readonly Color Plate = new Color(0.1f, 0.18f, 0.34f, 0.72f);

        /// <summary>A full-size sky gradient at the back of <paramref name="parent"/>.</summary>
        public static RectTransform Sky(Transform parent)
        {
            var sky = UiKit.Panel(parent, "Sky", Color.white, rounded: false);
            sky.GetComponent<Image>().sprite = UiKit.GradientSprite(SkyTop, SkyBottom);
            sky.GetComponent<Image>().raycastTarget = false;
            UiKit.Stretch(sky);
            sky.SetAsFirstSibling();
            return sky;
        }

        /// <summary>A puffy cloud at a height (fraction of the parent from the bottom) that drifts sideways.</summary>
        public static void Cloud(RectTransform parent, float x01, float y01, float scale, float speed)
        {
            var cloud = UiKit.Empty(parent, "Cloud");
            cloud.anchorMin = cloud.anchorMax = new Vector2(x01, y01);
            cloud.sizeDelta = new Vector2(260, 110) * scale;
            var white = new Color(1f, 1f, 1f, 0.92f);
            Blob(cloud, white, 0.5f, 0.35f, 1f, 0.55f);
            Blob(cloud, white, 0.32f, 0.5f, 0.42f, 0.8f);
            Blob(cloud, white, 0.6f, 0.6f, 0.5f, 1f);
            Blob(cloud, white, 0.8f, 0.42f, 0.34f, 0.62f);
            cloud.gameObject.AddComponent<Drift>().Speed = speed;
        }

        /// <summary>A circle inside a box, placed in fractions of the box (centre and size).</summary>
        static Image Blob(RectTransform box, Color color, float cx, float cy, float w, float h)
        {
            var image = UiKit.Panel(box, "Blob", color, rounded: false).GetComponent<Image>();
            image.sprite = UiKit.CircleSprite;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(cx - w / 2, cy - h / 2);
            rt.anchorMax = new Vector2(cx + w / 2, cy + h / 2);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return image;
        }

        /// <summary>
        /// A circle on a map whose x is a fraction of its width and y is pixels down from its top.
        /// </summary>
        public static Image Dot(RectTransform map, Color color, float x01, float y, float w, float h)
        {
            var image = UiKit.Panel(map, "Dot", color, rounded: false).GetComponent<Image>();
            image.sprite = UiKit.CircleSprite;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(x01, 1);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(0, -y);
            return image;
        }

        /// <summary>A round tree standing with its foot at (x01, y).</summary>
        public static void Tree(RectTransform map, float x01, float y, float size, System.Random rng)
        {
            var root = UiKit.Empty(map, "Tree");
            root.anchorMin = root.anchorMax = new Vector2(x01, 1);
            root.pivot = new Vector2(0.5f, 0);
            root.sizeDelta = new Vector2(size, size * 1.3f);
            root.anchoredPosition = new Vector2(0, -y);
            float hue = (float)rng.NextDouble();
            var leaf = Color.Lerp(new Color(0.24f, 0.6f, 0.3f), new Color(0.38f, 0.7f, 0.28f), hue);
            if (rng.Next(6) == 0) leaf = new Color(0.95f, 0.66f, 0.75f); // the odd blossom tree
            Blob(root, new Color(0, 0, 0, 0.16f), 0.5f, 0.04f, 0.8f, 0.1f);          // shadow
            var trunk = UiKit.Panel(root, "Trunk", new Color(0.55f, 0.38f, 0.24f), rounded: false);
            trunk.GetComponent<Image>().raycastTarget = false;
            trunk.anchorMin = new Vector2(0.44f, 0.03f);
            trunk.anchorMax = new Vector2(0.56f, 0.4f);
            trunk.offsetMin = trunk.offsetMax = Vector2.zero;
            Blob(root, UiKit.Shade(leaf, 0.82f), 0.5f, 0.55f, 0.95f, 0.72f);
            Blob(root, leaf, 0.42f, 0.66f, 0.62f, 0.5f);
            Blob(root, leaf, 0.62f, 0.62f, 0.55f, 0.46f);
            Blob(root, Color.Lerp(leaf, Color.white, 0.3f), 0.4f, 0.74f, 0.26f, 0.2f); // light catching the top
        }

        /// <summary>A low bush, sometimes dotted with flowers.</summary>
        public static void Bush(RectTransform map, float x01, float y, float size, System.Random rng)
        {
            var leaf = Color.Lerp(new Color(0.3f, 0.62f, 0.3f), new Color(0.42f, 0.74f, 0.32f), (float)rng.NextDouble());
            Dot(map, new Color(0, 0, 0, 0.14f), x01, y + size * 0.3f, size * 1.1f, size * 0.25f);
            Dot(map, UiKit.Shade(leaf, 0.85f), x01, y, size, size * 0.7f);
            Dot(map, leaf, x01 - 0.012f, y - size * 0.12f, size * 0.6f, size * 0.45f);
            if (rng.Next(2) == 0) Flowers(map, x01, y - size * 0.05f, size * 0.7f, rng, 4);
        }

        static readonly Color[] Petals =
        {
            new Color(1f, 0.55f, 0.65f), new Color(1f, 0.88f, 0.35f), Color.white, new Color(0.75f, 0.6f, 1f), new Color(1f, 0.65f, 0.35f),
        };

        /// <summary>A little scatter of flowers around (x01, y).</summary>
        public static void Flowers(RectTransform map, float x01, float y, float spread, System.Random rng, int count)
        {
            var petal = Petals[rng.Next(Petals.Length)];
            for (int i = 0; i < count; i++)
            {
                float dx = ((float)rng.NextDouble() - 0.5f) * spread / 960f, dy = ((float)rng.NextDouble() - 0.5f) * spread * 0.5f;
                float s = 12 + rng.Next(8);
                Dot(map, petal, x01 + dx, y + dy, s, s);
                Dot(map, new Color(1f, 0.9f, 0.4f), x01 + dx, y + dy, s * 0.35f, s * 0.35f);
            }
        }

        /// <summary>A small pond with a sandy rim and a glint.</summary>
        public static void Pond(RectTransform map, float x01, float y, float w, float h)
        {
            Dot(map, new Color(0.8f, 0.72f, 0.55f), x01, y, w + 24, h + 20);
            Dot(map, new Color(0.35f, 0.68f, 0.92f), x01, y, w, h);
            Dot(map, new Color(0.55f, 0.82f, 1f), x01 - 0.02f, y - h * 0.12f, w * 0.6f, h * 0.45f);
            var glint = Dot(map, new Color(1f, 1f, 1f, 0.7f), x01 + 0.05f, y - h * 0.2f, w * 0.18f, h * 0.1f);
            glint.gameObject.AddComponent<Twinkle>();
        }

        /// <summary>Moves a cloud slowly sideways and wraps it round when it leaves the screen.</summary>
        class Drift : MonoBehaviour
        {
            public float Speed = 12;

            void Update()
            {
                var rt = (RectTransform)transform;
                var parent = (RectTransform)rt.parent;
                float half = parent.rect.width / 2 + rt.rect.width;
                var p = rt.anchoredPosition;
                p.x += Speed * Time.unscaledDeltaTime;
                float baseX = (rt.anchorMin.x - 0.5f) * parent.rect.width;
                if (baseX + p.x > half) p.x = -half - baseX;
                rt.anchoredPosition = p;
            }
        }

        /// <summary>A soft shimmer on the water.</summary>
        class Twinkle : MonoBehaviour
        {
            Image _image;
            void Awake() => _image = GetComponent<Image>();

            void Update()
            {
                var c = _image.color;
                c.a = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 1.6f);
                _image.color = c;
            }
        }

        /// <summary>Fades a graphic group in and out, for "touch the screen" style hints.</summary>
        public class Blink : MonoBehaviour
        {
            CanvasGroup _group;
            void Awake()
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            }
            void Update() => _group.alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
        }
    }
}
