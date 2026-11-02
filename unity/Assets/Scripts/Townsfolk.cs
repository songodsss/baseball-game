using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Little people and cars that go back and forth along a home-screen street, so the town looks
    /// lived in. Drawn from simple shapes; each one turns round at the ends of its street.
    /// </summary>
    public class Townsfolk : MonoBehaviour
    {
        static readonly Color[] Shirts =
        {
            new Color(0.9f, 0.35f, 0.35f), new Color(0.3f, 0.55f, 0.9f), new Color(0.95f, 0.75f, 0.25f),
            new Color(0.35f, 0.75f, 0.5f), new Color(0.7f, 0.45f, 0.85f), new Color(0.95f, 0.95f, 0.95f),
            new Color(0.95f, 0.55f, 0.25f),
        };
        static readonly Color[] Skins =
        {
            new Color(1f, 0.86f, 0.74f), new Color(0.93f, 0.76f, 0.6f), new Color(0.78f, 0.58f, 0.42f), new Color(0.6f, 0.42f, 0.3f),
        };
        static readonly Color[] Paints =
        {
            new Color(0.92f, 0.3f, 0.3f), new Color(0.25f, 0.5f, 0.9f), new Color(0.95f, 0.95f, 0.95f),
            new Color(0.98f, 0.8f, 0.2f), new Color(0.2f, 0.2f, 0.24f),
        };

        bool _vertical;
        float _lateral, _t, _speed, _bob, _phase;
        int _dir = 1;
        RectTransform _rt, _body;

        /// <summary>A person on the sidewalk <paramref name="lateral"/> px off the street's centre line.</summary>
        public static void Walker(RectTransform street, bool vertical, float lateral, int seed)
        {
            var rng = new System.Random(seed);
            var rt = Root(street, "Walker", new Vector2(16, 30));
            var body = UiKit.Empty(rt, "Body");
            UiKit.Stretch(body);
            Piece(body, "Legs", new Color(0.2f, 0.22f, 0.3f), new Vector2(0.2f, 0f), new Vector2(0.8f, 0.4f), false);
            Piece(body, "Shirt", Shirts[rng.Next(Shirts.Length)], new Vector2(0f, 0.3f), new Vector2(1f, 0.72f), true);
            Piece(body, "Head", Skins[rng.Next(Skins.Length)], new Vector2(0.1f, 0.62f), new Vector2(0.9f, 1.08f), false, circle: true);
            var me = rt.gameObject.AddComponent<Townsfolk>();
            me.Setup(rt, body, vertical, lateral, rng, 18f, 34f, 2.5f);
        }

        /// <summary>A car in the lane <paramref name="lateral"/> px off the centre line, driving along the street.</summary>
        public static void Car(RectTransform street, float lateral, int seed)
        {
            var rng = new System.Random(seed);
            var rt = Root(street, "Car", new Vector2(46, 22));
            var body = UiKit.Empty(rt, "Body");
            UiKit.Stretch(body);
            Piece(body, "Shell", Paints[rng.Next(Paints.Length)], Vector2.zero, Vector2.one, true);
            Piece(body, "Glass", new Color(0.6f, 0.8f, 0.95f, 0.95f), new Vector2(0.55f, 0.2f), new Vector2(0.8f, 0.8f), true);
            Piece(body, "Light", new Color(1f, 0.95f, 0.6f), new Vector2(0.9f, 0.15f), new Vector2(1f, 0.4f), false);
            var me = rt.gameObject.AddComponent<Townsfolk>();
            me.Setup(rt, body, false, lateral, rng, 60f, 95f, 0.6f);
        }

        static RectTransform Root(RectTransform street, string name, Vector2 size)
        {
            var rt = UiKit.Empty(street, name);
            rt.sizeDelta = size;
            return rt;
        }

        static void Piece(RectTransform parent, string name, Color color, Vector2 min, Vector2 max, bool rounded, bool circle = false)
        {
            var p = UiKit.Panel(parent, name, color, rounded && !circle);
            var image = p.GetComponent<Image>();
            image.raycastTarget = false;
            if (circle)
            {
                image.sprite = UiKit.CircleSprite;
                image.type = Image.Type.Simple;
            }
            p.anchorMin = min;
            p.anchorMax = max;
            p.offsetMin = p.offsetMax = Vector2.zero;
        }

        void Setup(RectTransform rt, RectTransform body, bool vertical, float lateral, System.Random rng, float minSpeed, float maxSpeed, float bob)
        {
            _rt = rt;
            _body = body;
            _vertical = vertical;
            _lateral = lateral;
            _t = (float)rng.NextDouble();
            _dir = rng.Next(2) == 0 ? 1 : -1;
            _speed = minSpeed + (float)rng.NextDouble() * (maxSpeed - minSpeed);
            _bob = bob;
            _phase = (float)rng.NextDouble() * 6f;
            Place();
        }

        void Update()
        {
            var parent = _rt.parent as RectTransform;
            float length = _vertical ? parent.rect.height : parent.rect.width;
            if (length < 1f) return;
            _t += _dir * _speed * Time.unscaledDeltaTime / length;
            if (_t > 0.97f) { _t = 0.97f; _dir = -1; }
            if (_t < 0.03f) { _t = 0.03f; _dir = 1; }
            Place();
        }

        void Place()
        {
            _rt.anchorMin = _rt.anchorMax = _vertical ? new Vector2(0.5f, _t) : new Vector2(_t, 0.5f);
            _rt.anchoredPosition = _vertical ? new Vector2(_lateral, 0) : new Vector2(0, _lateral);
            // Face the way it is going, and bounce a little in step.
            _body.localScale = new Vector3(_dir, 1, 1);
            _body.anchoredPosition = new Vector2(0, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 9f + _phase)) * _bob);
        }
    }
}
