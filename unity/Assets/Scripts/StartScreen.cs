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

            // Rolling green hills along the bottom, with the stadium sitting on them.
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
                pic.rectTransform.anchorMin = new Vector2(0.12f, 0.12f);
                pic.rectTransform.anchorMax = new Vector2(0.88f, 0.42f);
                pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;
            }

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
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
                img.rectTransform.sizeDelta = new Vector2(240, 240);
                UiAnim.PopIn(img, 0.45f);
            }

            var title = UiKit.Label(root, TitleScreen.GameTitle, 100, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            Outlined(title, 3);
            Place(title.rectTransform, 0.66f, 150);

            var sub = UiKit.Label(root, "프로 야구 선수 육성", 36, TextAnchor.MiddleCenter);
            Outlined(sub, 2);
            Place(sub.rectTransform, 0.6f, 60);

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
}
