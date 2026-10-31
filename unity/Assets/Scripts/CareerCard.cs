using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// A trading-card style summary of a player, shown full screen and saved as a PNG the player can
    /// share. Built from code like every other screen; the club colour paints the top of the card.
    /// </summary>
    public class CareerCard : MonoBehaviour
    {
        public class Data
        {
            public string Name;
            public int Number;
            public string Position;
            public string Team;
            /// <summary>e.g. "2027 시즌 · 24세" or "통산 15시즌".</summary>
            public string Subtitle;
            public Dictionary<Stat, int> Ratings = new Dictionary<Stat, int>();
            public List<string> Lines = new List<string>();
            public List<string> Titles = new List<string>();
            /// <summary>A gold banner across the card, e.g. "명예의 전당".</summary>
            public string Banner;
            public string FileName = "card";
        }

        RectTransform _card;
        GameObject _buttons;
        Text _status;
        string _fileName;

        public static void Show(Transform canvas, Data data, Action onClose = null)
        {
            var dim = UiKit.Dim(canvas, "Career Card");
            var host = dim.gameObject.AddComponent<CareerCard>();
            host._fileName = data.FileName;
            host.Build(dim, data, onClose);
        }

        void Build(RectTransform dim, Data data, Action onClose)
        {
            var color = UiKit.TeamColor(data.Team);
            _card = UiKit.Panel(dim, "Card", UiKit.Card);
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.56f);
            _card.sizeDelta = new Vector2(860, 1360);
            UiAnim.PopIn(_card);

            // Club-colour top half with the badge, number and name.
            var top = UiKit.Panel(_card, "Top", Color.white);
            top.GetComponent<Image>().sprite = UiKit.GradientSprite(UiKit.Shade(color, 1.15f), UiKit.Shade(color, 0.5f));
            top.GetComponent<Image>().type = Image.Type.Simple;
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 470);
            var emblem = ArtLibrary.Emblem(top, data.Team, 150);
            emblem.anchorMin = emblem.anchorMax = new Vector2(0, 1);
            emblem.pivot = new Vector2(0, 1);
            emblem.anchoredPosition = new Vector2(40, -40);
            emblem.sizeDelta = new Vector2(150, 150);
            if (data.Number > 0)
            {
                var number = UiKit.Label(top, data.Number.ToString(), 190, TextAnchor.UpperRight, new Color(1, 1, 1, 0.9f));
                number.fontStyle = FontStyle.Bold;
                Place(number.rectTransform, new Vector2(0.4f, 0.35f), new Vector2(0.95f, 0.98f));
            }
            var name = UiKit.Label(top, data.Name, 84, TextAnchor.LowerLeft);
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -4);
            Place(name.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.45f));
            var sub = UiKit.Label(top, $"{data.Position} · {data.Team}\n<size=28>{data.Subtitle}</size>", 34, TextAnchor.UpperLeft, new Color(1, 1, 1, 0.85f));
            Place(sub.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 0.18f));

            // Ratings, lines and titles below.
            var body = UiKit.Empty(_card, "Body");
            Place(body, Vector2.zero, Vector2.one);
            body.offsetMax = new Vector2(-40, -500);
            body.offsetMin = new Vector2(40, 40);
            UiKit.Column(body, 0, 10);
            if (data.Banner != null)
            {
                var banner = UiKit.Panel(body, "Banner", new Color(1f, 0.82f, 0.3f));
                UiKit.Height(banner, 64);
                var text = UiKit.Label(banner, data.Banner, 34, TextAnchor.MiddleCenter, new Color(0.15f, 0.12f, 0.05f));
                text.fontStyle = FontStyle.Bold;
                UiKit.Stretch(text.rectTransform);
            }
            foreach (var pair in data.Ratings)
            {
                var row = UiKit.Empty(body, Stats.Name(pair.Key));
                UiKit.Height(row, 46);
                UiKit.Row(row, 14).childForceExpandWidth = false;
                UiKit.Width(UiKit.Label(row, Stats.Name(pair.Key), 28, TextAnchor.MiddleLeft, UiKit.Muted), 130);
                var bar = UiKit.Bar(row, color.grayscale > 0.6f ? UiKit.Accent : UiKit.Shade(color, 1.3f));
                UiKit.Width(bar.parent, 0, 1);
                UiKit.SetBar(bar, pair.Value / 100f);
                var value = UiKit.Label(row, pair.Value.ToString(), 32, TextAnchor.MiddleRight);
                value.fontStyle = FontStyle.Bold;
                UiKit.Width(value, 70);
            }
            foreach (var line in data.Lines)
                UiKit.Height(UiKit.Label(body, line, 32, TextAnchor.MiddleLeft), 48);
            if (data.Titles.Count > 0)
                UiKit.Height(UiKit.Label(body, "<color=#FFD24D>★</color> " + string.Join("  ★ ", data.Titles), 28, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.6f)), 0, 1);
            var footer = UiKit.Label(_card, "프로야구 선수 키우기", 22, TextAnchor.LowerRight, UiKit.Muted);
            Place(footer.rectTransform, new Vector2(0.05f, 0), new Vector2(0.95f, 0.03f));

            // Buttons sit under the card and are hidden while the picture is taken.
            var buttons = UiKit.Empty(dim, "Buttons");
            _buttons = buttons.gameObject;
            buttons.anchorMin = new Vector2(0.08f, 0.02f);
            buttons.anchorMax = new Vector2(0.92f, 0.08f);
            buttons.offsetMin = buttons.offsetMax = Vector2.zero;
            UiKit.Row(buttons, 20);
            UiKit.Button(buttons, "이미지로 저장", () => StartCoroutine(Capture()), new Color(1f, 0.82f, 0.3f)).GetComponentInChildren<Text>().color = new Color(0.15f, 0.12f, 0.05f);
            UiKit.Button(buttons, "닫기", () => { Destroy(gameObject); onClose?.Invoke(); }, UiKit.Muted);
            _status = UiKit.Label(dim, "", 24, TextAnchor.MiddleCenter, UiKit.Muted);
            Place(_status.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.11f));
        }

        static void Place(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Saves just the card as a PNG in the app's folder. Sharing to apps comes with the phone build.</summary>
        IEnumerator Capture()
        {
            _buttons.SetActive(false);
            _status.text = "";
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            _buttons.SetActive(true);

            var corners = new Vector3[4];
            _card.GetWorldCorners(corners); // screen-space overlay: world corners are screen pixels
            int x = Mathf.Clamp((int)corners[0].x, 0, shot.width - 1);
            int y = Mathf.Clamp((int)corners[0].y, 0, shot.height - 1);
            int w = Mathf.Clamp((int)(corners[2].x - corners[0].x), 1, shot.width - x);
            int h = Mathf.Clamp((int)(corners[2].y - corners[0].y), 1, shot.height - y);
            var crop = new Texture2D(w, h, TextureFormat.RGB24, false);
            crop.SetPixels(shot.GetPixels(x, y, w, h));
            crop.Apply();
            Destroy(shot);

            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "Cards");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"{_fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                File.WriteAllBytes(path, crop.EncodeToPNG());
                _status.text = $"저장했어요: {path}";
                if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer)
                    Application.OpenURL("file://" + dir);
            }
            catch (Exception e)
            {
                _status.text = "저장하지 못했어요: " + e.Message;
            }
            Destroy(crop);
        }
    }
}
