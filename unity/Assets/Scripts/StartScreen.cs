using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// The screen the app opens on: a sunny ballpark, the game's name, and "touch the screen".
    /// Any tap goes on to home. Only shown at launch; coming back from a player goes straight home.
    /// </summary>
    public class StartScreen : MonoBehaviour
    {
        public static void Show() => new GameObject("StartScreen").AddComponent<StartScreen>();

        bool _leaving;

        void Start()
        {
            var canvas = UiKit.Canvas(transform, "StartCanvas");
            var back = UiKit.Empty(canvas.transform, "Background");
            UiKit.Stretch(back);
            Scenery.Sky(back);
            Scenery.Cloud(back, 0.2f, 0.88f, 1.2f, 14);
            Scenery.Cloud(back, 0.78f, 0.78f, 0.9f, 9);
            Scenery.Cloud(back, 0.45f, 0.66f, 0.7f, 18);

            // A painted title picture if there is one (Resources/Art/Backgrounds/start).
            var painted = ArtLibrary.Background("start");
            if (painted != null)
            {
                var pic = UiKit.Empty(back, "Painted").gameObject.AddComponent<Image>();
                pic.sprite = painted;
                pic.raycastTarget = false;
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = painted.rect.width / painted.rect.height;
            }
            // Otherwise the legends fill the screen, and if there are none, hills and the stadium.
            else if (!LegendReel.Build(back)) DrawnHills(back);

            var root = UiKit.Empty(back, "Root");
            UiKit.Stretch(root);
            root.gameObject.AddComponent<SafeArea>();

            // Over the painted key art the title goes in its empty sky and the icon stays out of the way.
            bool keyArt = painted != null;
            var icon = keyArt ? null : ArtLibrary.Get("App/app_icon");
            if (icon != null)
            {
                var img = UiKit.Empty(root, "Icon").gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.87f);
                img.rectTransform.sizeDelta = new Vector2(200, 200);
                UiAnim.PopIn(img, 0.45f);
            }

            // A big gold title with a thick dark rim, poster style.
            var title = UiKit.Label(root, TitleScreen.GameTitle, 110, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.3f));
            title.fontStyle = FontStyle.Bold;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Outlined(title, 5);
            var glow = title.gameObject.AddComponent<Shadow>();
            glow.effectColor = new Color(0.35f, 0.12f, 0f, 0.9f);
            glow.effectDistance = new Vector2(0, -9);
            Place(title.rectTransform, keyArt ? 0.88f : 0.775f, 150);

            var sub = UiKit.Label(root, "프로 야구 선수 육성", 36, TextAnchor.MiddleCenter);
            Outlined(sub, 2);
            Place(sub.rectTransform, keyArt ? 0.825f : 0.72f, 60);

            // The hint sits on a navy pill so it reads over any picture.
            var pill = UiKit.Panel(root, "Hint", Scenery.Plate);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, keyArt ? 0.05f : 0.07f);
            pill.sizeDelta = new Vector2(560, 100);
            var hint = UiKit.Label(pill, "화면을 터치하세요", 42, TextAnchor.MiddleCenter);
            hint.fontStyle = FontStyle.Bold;
            UiKit.Stretch(hint.rectTransform);
            pill.gameObject.AddComponent<Scenery.Blink>();

            var version = UiKit.Label(root, $"v{Application.version}", 24, TextAnchor.LowerRight, UiKit.Muted);
            version.rectTransform.anchorMin = new Vector2(0.5f, 0);
            version.rectTransform.anchorMax = new Vector2(1, 0);
            version.rectTransform.pivot = new Vector2(1, 0);
            version.rectTransform.sizeDelta = new Vector2(0, 40);
            version.rectTransform.anchoredPosition = new Vector2(-24, 12);

            // The whole screen is the button.
            var tap = UiKit.Empty(back, "Tap");
            UiKit.Stretch(tap);
            tap.gameObject.AddComponent<Image>().color = Color.clear;
            tap.gameObject.AddComponent<Button>().onClick.AddListener(Go);

            GameAudio.PlayMusic(GameAudio.MusicHome);
            GameAudio.PlayAmbience(null);
        }

        void Go()
        {
            if (_leaving) return;
            _leaving = true;
            UiSound.Tap();
            TitleScreen.Show();
            Destroy(gameObject);
        }

        static void Outlined(Text label, float width)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.16f, 0.32f, 0.85f);
            outline.effectDistance = new Vector2(width, -width);
        }

        static void Place(RectTransform rt, float y01, float height)
        {
            rt.anchorMin = new Vector2(0, y01);
            rt.anchorMax = new Vector2(1, y01);
            rt.sizeDelta = new Vector2(-60, height);
            rt.anchoredPosition = Vector2.zero;
        }

        static void DrawnHills(RectTransform back)
        {
            Hill(back, -0.15f, 0.62f, 0.34f, Scenery.Grass * 0.92f + new Color(0, 0, 0, 0.08f));
            Hill(back, 0.45f, 1.2f, 0.3f, Scenery.Grass);
            Hill(back, -0.2f, 1.4f, 0.2f, Color.Lerp(Scenery.Grass, Color.white, 0.12f));
            var stadium = ArtLibrary.Get("Buildings/stadium");
            if (stadium != null)
            {
                var pic = UiKit.Empty(back, "Stadium").gameObject.AddComponent<Image>();
                pic.sprite = stadium;
                pic.preserveAspect = true;
                pic.raycastTarget = false;
                pic.rectTransform.anchorMin = new Vector2(0.14f, 0.1f);
                pic.rectTransform.anchorMax = new Vector2(0.86f, 0.36f);
                pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;
            }
        }

        /// <summary>A big soft hill: a wide circle whose top shows above the bottom edge.</summary>
        static void Hill(RectTransform back, float left01, float width01, float top01, Color color)
        {
            var hill = UiKit.Panel(back, "Hill", color, rounded: false);
            var image = hill.GetComponent<Image>();
            image.sprite = UiKit.CircleSprite;
            image.raycastTarget = false;
            hill.anchorMin = new Vector2(left01, -top01 * 1.6f);
            hill.anchorMax = new Vector2(left01 + width01, top01);
            hill.offsetMin = hill.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// The league's legends fill the start screen: one at a time, full height, centred on the player
    /// (their pictures have them on the right), slowly pushing in and crossfading to the next, with
    /// their name large over the lower part. Shades top and bottom keep the title and hint readable.
    /// </summary>
    public class LegendReel : MonoBehaviour
    {
        const float Hold = 4.5f, FadeTime = 1.1f;
        /// <summary>Where the player stands across the picture (0 = left edge, 1 = right edge).</summary>
        const float Subject = 0.7f;

        System.Collections.Generic.List<(BaseballSim.Career.Legend legend, Sprite art)> _legends;
        Image _front, _back;
        Text _caption;
        CanvasGroup _captionGroup;
        int _index;
        float _clock;
        bool _fading;

        /// <summary>Adds the reel behind everything else on <paramref name="back"/>; false if there are no legend pictures.</summary>
        public static bool Build(RectTransform back)
        {
            var legends = BaseballSim.Career.Legends.All
                .Select(l => (legend: l, art: ArtLibrary.Get("Legends/" + l.Id)))
                .Where(x => x.art != null).ToList();
            if (legends.Count == 0) return false;

            var stage = UiKit.Panel(back, "Legends", new Color(0.03f, 0.05f, 0.1f), rounded: false);
            UiKit.Stretch(stage);
            stage.GetComponent<Image>().raycastTarget = false;
            var reel = stage.gameObject.AddComponent<LegendReel>();
            reel._legends = legends;
            reel._back = reel.Picture(stage);
            reel._front = reel.Picture(stage);

            Shade(stage, new Vector2(0, 0.6f), Vector2.one, new Color(0.03f, 0.08f, 0.2f, 0f), new Color(0.03f, 0.08f, 0.2f, 0.85f));
            Shade(stage, Vector2.zero, new Vector2(1, 0.45f), new Color(0.02f, 0.03f, 0.08f, 0.92f), new Color(0.02f, 0.03f, 0.08f, 0f));

            var caption = UiKit.Empty(stage, "Caption");
            caption.anchorMin = new Vector2(0, 0.17f);
            caption.anchorMax = new Vector2(1, 0.33f);
            caption.offsetMin = new Vector2(48, 0);
            caption.offsetMax = new Vector2(-48, 0);
            reel._captionGroup = caption.gameObject.AddComponent<CanvasGroup>();
            reel._caption = UiKit.Label(caption, "", 72, TextAnchor.LowerLeft);
            UiKit.Stretch(reel._caption.rectTransform);
            var outline = reel._caption.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            reel._index = new System.Random(System.Environment.TickCount).Next(legends.Count);
            reel.Show(reel._front, reel._index);
            reel._back.color = new Color(1, 1, 1, 0);
            return true;
        }

        static void Shade(RectTransform stage, Vector2 min, Vector2 max, Color bottom, Color top)
        {
            var shade = UiKit.Panel(stage, "Shade", Color.white, rounded: false);
            shade.GetComponent<Image>().sprite = UiKit.GradientSprite(top, bottom);
            shade.GetComponent<Image>().raycastTarget = false;
            shade.anchorMin = min;
            shade.anchorMax = max;
            shade.offsetMin = shade.offsetMax = Vector2.zero;
        }

        Image Picture(RectTransform stage)
        {
            var image = UiKit.Empty(stage, "Picture").gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Full height of the screen, slid sideways so the player is in the middle.</summary>
        void Fit(Image image, float zoom)
        {
            var stage = (RectTransform)transform;
            var art = image.sprite;
            if (art == null) return;
            float h = Mathf.Max(stage.rect.height, stage.rect.width * art.rect.height / art.rect.width) * zoom;
            float w = h * art.rect.width / art.rect.height;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            float x = (0.5f - Subject) * w;
            float limit = Mathf.Max(0, (w - stage.rect.width) / 2); // never show past the picture's edge
            rt.anchoredPosition = new Vector2(Mathf.Clamp(x, -limit, limit), h * 0.04f);
        }

        void Show(Image image, int index)
        {
            var (legend, art) = _legends[index];
            image.sprite = art;
            image.color = Color.white;
            image.transform.SetSiblingIndex(1); // over the other picture, under the shades and caption
            _caption.text = $"<size=30><color=#FFD668><b>LEGEND</b> · \"{legend.Nickname}\"</color></size>\n<b>{legend.Name}</b>\n" +
                            $"<size=28><color=#BAC4D8>{legend.Years} · {legend.Record}</color></size>";
            _clock = 0;
        }

        void Update()
        {
            _clock += Time.unscaledDeltaTime;
            Fit(_front, 1f + 0.07f * Mathf.Clamp01(_clock / (Hold + FadeTime))); // a slow push-in
            _captionGroup.alpha = Mathf.Clamp01(_clock / 0.6f) * (_fading ? 1f - Mathf.Clamp01((_clock - Hold) / (FadeTime * 0.5f)) : 1f);
            if (_legends.Count < 2 || _clock < Hold) return;

            if (!_fading)
            {
                _back.sprite = _legends[(_index + 1) % _legends.Count].art;
                _back.transform.SetSiblingIndex(1);
                _fading = true;
            }
            Fit(_back, 1f);
            float k = Mathf.Clamp01((_clock - Hold) / FadeTime);
            _back.color = new Color(1, 1, 1, k);
            if (k < 1f) return;

            _index = (_index + 1) % _legends.Count;
            (_front, _back) = (_back, _front);
            _fading = false;
            Show(_front, _index);
            _back.color = new Color(1, 1, 1, 0);
        }
    }
}
