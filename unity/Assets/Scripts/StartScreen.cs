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

            // A painted title picture if there is one (Resources/Art/Backgrounds/start), else rolling
            // green hills along the bottom with the stadium sitting on them.
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
            else DrawnHills(back);

            var root = UiKit.Empty(back, "Root");
            UiKit.Stretch(root);
            root.gameObject.AddComponent<SafeArea>();

            var icon = ArtLibrary.Get("App/app_icon");
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

            var title = UiKit.Label(root, TitleScreen.GameTitle, 100, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            Outlined(title, 3);
            Place(title.rectTransform, 0.775f, 150);

            var sub = UiKit.Label(root, "프로 야구 선수 육성", 36, TextAnchor.MiddleCenter);
            Outlined(sub, 2);
            Place(sub.rectTransform, 0.72f, 60);

            LegendReel.Build(root);

            // The hint sits on a navy pill so it reads over the grass.
            var pill = UiKit.Panel(root, "Hint", Scenery.Plate);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0.07f);
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
    /// The league's legends on the start screen: their pictures take turns in a framed card, each
    /// drifting in slowly and crossfading to the next, with name, nickname and record underneath.
    /// </summary>
    public class LegendReel : MonoBehaviour
    {
        const float Hold = 4.5f, FadeTime = 0.9f;

        System.Collections.Generic.List<(BaseballSim.Career.Legend legend, Sprite art)> _legends;
        Image _front, _back;
        Text _caption;
        int _index;
        float _clock;
        bool _fading;

        public static void Build(RectTransform root)
        {
            var legends = BaseballSim.Career.Legends.All
                .Select(l => (legend: l, art: ArtLibrary.Get("Legends/" + l.Id)))
                .Where(x => x.art != null).ToList();
            if (legends.Count == 0) return;

            var frame = UiKit.Panel(root, "Legends", Scenery.Plate);
            frame.anchorMin = new Vector2(0.05f, 0.37f);
            frame.anchorMax = new Vector2(0.95f, 0.66f);
            frame.offsetMin = frame.offsetMax = Vector2.zero;
            frame.GetComponent<Image>().raycastTarget = false;

            var window = UiKit.Empty(frame, "Window");
            UiKit.Stretch(window);
            window.offsetMin = new Vector2(10, 10);
            window.offsetMax = new Vector2(-10, -10);
            window.gameObject.AddComponent<RectMask2D>();

            var reel = frame.gameObject.AddComponent<LegendReel>();
            reel._legends = legends;
            reel._back = reel.Picture(window);
            reel._front = reel.Picture(window);

            // A dark band along the bottom for the caption.
            var band = UiKit.Panel(window, "Band", Color.white, rounded: false);
            band.GetComponent<Image>().sprite = UiKit.GradientSprite(new Color(0, 0, 0, 0), new Color(0.03f, 0.06f, 0.14f, 0.85f));
            band.GetComponent<Image>().raycastTarget = false;
            band.anchorMin = Vector2.zero;
            band.anchorMax = new Vector2(1, 0.38f);
            band.offsetMin = band.offsetMax = Vector2.zero;

            var tag = UiKit.Panel(window, "Tag", new Color(1f, 0.78f, 0.25f, 0.95f));
            tag.anchorMin = tag.anchorMax = new Vector2(0, 1);
            tag.pivot = new Vector2(0, 1);
            tag.sizeDelta = new Vector2(150, 50);
            tag.anchoredPosition = new Vector2(16, -16);
            var tagText = UiKit.Label(tag, "<b>LEGEND</b>", 26, TextAnchor.MiddleCenter, new Color(0.15f, 0.1f, 0.02f));
            UiKit.Stretch(tagText.rectTransform);

            reel._caption = UiKit.Label(window, "", 40, TextAnchor.LowerLeft);
            var rt = reel._caption.rectTransform;
            UiKit.Stretch(rt);
            rt.offsetMin = new Vector2(24, 16);
            rt.offsetMax = new Vector2(-24, 0);

            reel._index = new System.Random(System.Environment.TickCount).Next(legends.Count);
            reel.Show(reel._front, reel._index);
            reel._back.color = new Color(1, 1, 1, 0);
            UiAnim.PopIn(frame, 0.4f);
        }

        Image Picture(RectTransform window)
        {
            var image = UiKit.Empty(window, "Picture").gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            var fit = image.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            return image;
        }

        void Show(Image image, int index)
        {
            var (legend, art) = _legends[index];
            image.sprite = art;
            image.color = Color.white;
            image.transform.localScale = Vector3.one;
            image.transform.SetSiblingIndex(1); // above the other picture, under the band and text
            _caption.text = $"<b>{legend.Name}</b> <size=30><color=#FFD668>\"{legend.Nickname}\"</color></size>\n" +
                            $"<size=26><color=#BAC4D8>{legend.Years} · {legend.Record}</color></size>";
            _clock = 0;
        }

        void Update()
        {
            _clock += Time.unscaledDeltaTime;
            // A slow push-in on the picture showing.
            _front.transform.localScale = Vector3.one * (1f + 0.06f * Mathf.Clamp01(_clock / (Hold + FadeTime)));
            if (_legends.Count < 2 || _clock < Hold) return;

            // Bring the next picture in over the current one.
            if (!_fading)
            {
                int next = (_index + 1) % _legends.Count;
                _back.sprite = _legends[next].art;
                _back.transform.localScale = Vector3.one;
                _back.transform.SetSiblingIndex(1);
                _fading = true;
            }
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
