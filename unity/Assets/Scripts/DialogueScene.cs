using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// A story event told like a visual novel: the scene's picture behind, the speaker's portrait in
    /// front, and a text box at the bottom with the speaker's name on a plate. The text types itself
    /// out; a tap finishes the line, the next tap moves on. Choices appear over the scene at the end.
    /// Portraits: my player and other ballplayers use the drawn player faces, legends their own
    /// picture, and everyone else a silhouette until they get one.
    /// </summary>
    public class DialogueScene : MonoBehaviour
    {
        const float CharsPerSecond = 42f;
        const float BoxHeight = 440f, BoxBottom = 48f;
        static readonly Color NarrationColor = new Color32(0xBA, 0xC4, 0xD8, 0xFF);
        static readonly Color LegendGold = new Color(1f, 0.82f, 0.3f);

        CareerState _career;
        Color _team;
        RectTransform _safe, _portraitHolder, _box, _plate, _choices;
        Text _name, _text, _aside, _next;
        Image _portrait;
        GameObject _skip, _tapCatcher;
        string _portraitFor;

        List<DialogueLine> _lines;
        int _index;
        Action _onDone;
        string _full;
        float _shown;
        bool _typing;

        // ---------- Building ----------

        /// <summary>Builds the scene filling root (a full-screen overlay), with the picture behind and the title at the top.</summary>
        public static DialogueScene Create(RectTransform root, CareerState career, Sprite background, string title)
        {
            var scene = root.gameObject.AddComponent<DialogueScene>();
            scene._career = career;
            scene._team = UiKit.TeamColor(career.MyTeam.Name);
            scene.Build(root, background, title);
            return scene;
        }

        void Build(RectTransform root, Sprite background, string title)
        {
            var black = root.gameObject.GetComponent<Image>();
            if (black == null) black = root.gameObject.AddComponent<Image>();
            black.color = new Color(0.03f, 0.04f, 0.07f, 1f);
            black.sprite = null;

            // The scene's picture, cropped to fill, a little dark; the club colour when there's none.
            var back = UiKit.Empty(root, "Scene");
            UiKit.Stretch(back);
            var pic = UiKit.Empty(back, "Picture");
            var image = pic.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            if (background != null)
            {
                image.sprite = background;
                var fit = pic.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = background.rect.width / background.rect.height;
            }
            else
            {
                UiKit.Stretch(pic);
                image.sprite = UiKit.GradientSprite(UiKit.Shade(_team, 0.8f), UiKit.Shade(_team, 0.18f));
            }
            var shade = UiKit.Empty(back, "Shade");
            UiKit.Stretch(shade);
            var shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.sprite = UiKit.GradientSprite(new Color(0, 0, 0, 0.35f), new Color(0, 0, 0, 0.8f));
            shadeImage.raycastTarget = false;

            _safe = UiKit.Empty(root, "Safe");
            UiKit.Stretch(_safe);
            _safe.gameObject.AddComponent<SafeArea>();

            _portraitHolder = UiKit.Empty(_safe, "Portrait");
            _portraitHolder.anchorMin = _portraitHolder.anchorMax = new Vector2(0.5f, 0);
            _portraitHolder.pivot = new Vector2(0.5f, 0);
            _portraitHolder.anchoredPosition = new Vector2(0, BoxBottom + BoxHeight - 170);
            _portrait = _portraitHolder.gameObject.AddComponent<Image>();
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            _portrait.enabled = false;

            // Taps anywhere move the story on.
            var tap = UiKit.Empty(_safe, "Tap");
            UiKit.Stretch(tap);
            var tapImage = tap.gameObject.AddComponent<Image>();
            tapImage.color = new Color(0, 0, 0, 0);
            tap.gameObject.AddComponent<Button>().onClick.AddListener(Tap);
            _tapCatcher = tap.gameObject;

            BuildBox();

            // Title chip at the top left, skip at the top right.
            var chip = UiKit.Panel(_safe, "Title", new Color(0.05f, 0.07f, 0.12f, 0.85f));
            chip.anchorMin = chip.anchorMax = new Vector2(0, 1);
            chip.pivot = new Vector2(0, 1);
            chip.anchoredPosition = new Vector2(28, -28);
            var chipText = UiKit.Label(chip, title, 32, TextAnchor.MiddleCenter);
            chipText.fontStyle = FontStyle.Bold;
            UiKit.Stretch(chipText.rectTransform);
            chip.sizeDelta = new Vector2(Mathf.Min(760, chipText.preferredWidth + 70), 76);
            var stripe = UiKit.Panel(chip, "Team", _team);
            stripe.anchorMin = new Vector2(0, 0);
            stripe.anchorMax = new Vector2(0, 1);
            stripe.offsetMin = new Vector2(8, 14);
            stripe.offsetMax = new Vector2(16, -14);

            var skip = UiKit.Button(_safe, "건너뛰기 ▶▶", Skip, new Color(0.18f, 0.22f, 0.3f, 0.9f));
            var skipRt = (RectTransform)skip.transform;
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(1, 1);
            skipRt.pivot = new Vector2(1, 1);
            skipRt.anchoredPosition = new Vector2(-28, -28);
            skipRt.sizeDelta = new Vector2(250, 76);
            skip.GetComponentInChildren<Text>().fontSize = 28;
            _skip = skip.gameObject;

            _choices = UiKit.Empty(_safe, "Choices");
            _choices.anchorMin = new Vector2(0, 0);
            _choices.anchorMax = new Vector2(1, 0);
            _choices.pivot = new Vector2(0.5f, 0);
            _choices.anchoredPosition = new Vector2(0, BoxBottom + BoxHeight + 40);
            var column = UiKit.Column(_choices, 0, 18);
            column.padding = new RectOffset(70, 70, 0, 0);
            column.childControlHeight = true;
            column.childForceExpandHeight = false;
            var fitter = _choices.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void BuildBox()
        {
            _box = UiKit.Panel(_safe, "TextBox", new Color(0.05f, 0.07f, 0.12f, 0.93f));
            _box.anchorMin = new Vector2(0, 0);
            _box.anchorMax = new Vector2(1, 0);
            _box.pivot = new Vector2(0.5f, 0);
            _box.offsetMin = new Vector2(28, BoxBottom);
            _box.offsetMax = new Vector2(-28, BoxBottom + BoxHeight);
            var frame = _box.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(_team.r, _team.g, _team.b, 0.85f);
            frame.effectDistance = new Vector2(3, -3);
            _box.GetComponent<Image>().raycastTarget = false;

            _plate = UiKit.Panel(_box, "NamePlate", _team);
            _plate.anchorMin = _plate.anchorMax = new Vector2(0, 1);
            _plate.pivot = new Vector2(0, 0.5f);
            _plate.anchoredPosition = new Vector2(40, 0);
            _plate.GetComponent<Image>().raycastTarget = false;
            _name = UiKit.Label(_plate, "", 38, TextAnchor.MiddleCenter);
            _name.fontStyle = FontStyle.Bold;
            UiKit.Stretch(_name.rectTransform);

            _aside = UiKit.Label(_box, "", 28, TextAnchor.UpperLeft, NarrationColor);
            _aside.fontStyle = FontStyle.Italic;
            var asideRt = _aside.rectTransform;
            asideRt.anchorMin = new Vector2(0, 1);
            asideRt.anchorMax = new Vector2(1, 1);
            asideRt.pivot = new Vector2(0.5f, 1);
            asideRt.offsetMin = new Vector2(48, -110);
            asideRt.offsetMax = new Vector2(-48, -56);

            _text = UiKit.Label(_box, "", 40, TextAnchor.UpperLeft);
            _text.lineSpacing = 1.15f;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            var textRt = _text.rectTransform;
            UiKit.Stretch(textRt);
            textRt.offsetMin = new Vector2(48, 60);
            textRt.offsetMax = new Vector2(-48, -60);

            _next = UiKit.Label(_box, "▼", 30, TextAnchor.LowerRight, NarrationColor);
            var nextRt = _next.rectTransform;
            UiKit.Stretch(nextRt);
            nextRt.offsetMin = new Vector2(0, 22);
            nextRt.offsetMax = new Vector2(-34, 0);
        }

        // ---------- Playing ----------

        /// <summary>Tells the lines one tap at a time, then calls onDone (the last line stays in the box).</summary>
        public void Play(List<DialogueLine> lines, Action onDone)
        {
            ClearChoices();
            _lines = lines ?? new List<DialogueLine>();
            _index = -1;
            _onDone = onDone;
            _skip.SetActive(_lines.Count > 1);
            _tapCatcher.SetActive(true);
            Advance();
        }

        /// <summary>Buttons over the scene, one per answer.</summary>
        public void ShowChoices(IList<string> labels, Action<int> pick)
        {
            _skip.SetActive(false);
            _tapCatcher.SetActive(false);
            _next.enabled = false;
            ClearChoices();
            for (int i = 0; i < labels.Count; i++)
            {
                int choice = i;
                var button = UiKit.Button(_choices, labels[i], () => { ClearChoices(); pick(choice); }, new Color(0.16f, 0.22f, 0.34f, 0.96f));
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(_team.r, _team.g, _team.b, 0.9f);
                outline.effectDistance = new Vector2(3, -3);
                UiKit.Height(button, 112);
                UiAnim.PopIn(button);
            }
        }

        /// <summary>The last box: what the answer changed (rich text) and a button to leave.</summary>
        public void ShowEnd(string changes, string buttonLabel, Action confirm)
        {
            if (!string.IsNullOrEmpty(changes))
            {
                ShowLine(new DialogueLine { Kind = SpeakerKind.None, Text = "" }, typed: false);
                _text.text = changes;
                _text.alignment = TextAnchor.MiddleCenter;
            }
            _skip.SetActive(false);
            _tapCatcher.SetActive(false);
            _next.enabled = false;
            ClearChoices();
            var button = UiKit.Button(_choices, buttonLabel, () => { ClearChoices(); confirm(); });
            UiKit.Height(button, 112);
            UiAnim.PopIn(button);
        }

        void ClearChoices()
        {
            for (int i = _choices.childCount - 1; i >= 0; i--) Destroy(_choices.GetChild(i).gameObject);
        }

        void Tap()
        {
            if (_typing)
            {
                _shown = _full.Length;
                return;
            }
            UiSound.Tap();
            Advance();
        }

        /// <summary>Straight to the last line, then to the choices.</summary>
        void Skip()
        {
            if (_lines == null || _lines.Count == 0) return;
            _index = _lines.Count - 2;
            _typing = false;
            Advance();
            _shown = _full.Length;
        }

        void Advance()
        {
            _index++;
            if (_index >= _lines.Count)
            {
                _typing = false;
                _tapCatcher.SetActive(false);
                _skip.SetActive(false);
                var done = _onDone;
                _onDone = null;
                done?.Invoke();
                return;
            }
            ShowLine(_lines[_index], typed: true);
        }

        void ShowLine(DialogueLine line, bool typed)
        {
            bool named = line.IsSpeech && line.Speaker != null;
            _plate.gameObject.SetActive(named);
            if (named)
            {
                _name.text = line.Speaker;
                _plate.GetComponent<Image>().color = line.Kind == SpeakerKind.Legend ? UiKit.Shade(LegendGold, 0.75f)
                    : line.Kind == SpeakerKind.Me ? _team : new Color(0.24f, 0.3f, 0.42f);
                _plate.sizeDelta = new Vector2(Mathf.Max(200, _name.preferredWidth + 70), 76);
            }

            // A named speaker's words go without quote marks: the plate says who's talking.
            string text = line.Text ?? "";
            if (named && text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"') text = text.Substring(1, text.Length - 2);
            _text.color = line.IsSpeech ? Color.white : NarrationColor;
            _text.alignment = TextAnchor.UpperLeft;
            _text.fontStyle = FontStyle.Normal;
            _aside.text = line.Aside ?? "";
            var textRt = _text.rectTransform;
            textRt.offsetMax = new Vector2(-48, string.IsNullOrEmpty(line.Aside) ? -60 : -118);

            SetPortrait(line);

            _full = text;
            _shown = typed ? 0 : text.Length;
            _typing = typed && text.Length > 0;
            _text.text = typed ? "" : text;
            _next.enabled = !_typing;
        }

        void Update()
        {
            if (_typing)
            {
                _shown += Time.unscaledDeltaTime * CharsPerSecond;
                int n = Mathf.Min(_full.Length, (int)_shown);
                _text.text = _full.Substring(0, n);
                if (n >= _full.Length)
                {
                    _typing = false;
                    _next.enabled = true;
                }
            }
            if (_next.enabled) _next.color = new Color(NarrationColor.r, NarrationColor.g, NarrationColor.b, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
        }

        // ---------- Portraits ----------

        void SetPortrait(DialogueLine line)
        {
            // Narration keeps the last speaker on screen, dimmed; an unnamed voice clears it.
            if (!line.IsSpeech)
            {
                if (_portrait.enabled) _portrait.color = new Color(0.5f, 0.5f, 0.55f, 1f);
                return;
            }
            if (line.Speaker == null)
            {
                _portrait.enabled = false;
                _portraitFor = null;
                return;
            }
            string key = line.Kind + ":" + line.Speaker;
            if (key == _portraitFor)
            {
                _portrait.color = Color.white;
                return;
            }
            _portraitFor = key;

            var (sprite, framed) = PortraitOf(line);
            var old = _portraitHolder.Find("Frame");
            if (old != null) Destroy(old.gameObject);
            _portrait.enabled = sprite != null && !framed;
            _portrait.color = Color.white;
            if (sprite == null) return;

            if (framed)
            {
                // A legend's picture is a wide scene: show the part with him in a framed card.
                var frame = UiKit.Panel(_portraitHolder, "Frame", Color.white);
                frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0);
                frame.pivot = new Vector2(0.5f, 0);
                frame.sizeDelta = new Vector2(660, 690);
                frame.anchoredPosition = new Vector2(0, 150);
                frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var outline = frame.gameObject.AddComponent<Outline>();
                outline.effectColor = LegendGold;
                outline.effectDistance = new Vector2(5, -5);
                var face = UiKit.Empty(frame, "Face");
                UiKit.Stretch(face);
                var faceImage = face.gameObject.AddComponent<Image>();
                faceImage.sprite = sprite;
                faceImage.preserveAspect = false;
                faceImage.raycastTarget = false;
                _portraitHolder.sizeDelta = new Vector2(660, 840);
                _portraitHolder.anchoredPosition = new Vector2(0, BoxBottom + BoxHeight - 150);
                StopAllCoroutines();
                StartCoroutine(Enter(frame, 1));
                return;
            }

            _portrait.sprite = sprite;
            bool standing = sprite != Silhouette;
            float h = standing ? 1120 : 760;
            float w = h * sprite.rect.width / sprite.rect.height;
            _portraitHolder.sizeDelta = new Vector2(w, h);
            // I stand a little to the left, the people I talk to a little to the right.
            float x = line.Kind == SpeakerKind.Me ? -150 : 120;
            _portraitHolder.anchoredPosition = new Vector2(x, BoxBottom + BoxHeight - (standing ? 330 : 120));
            _portrait.color = standing ? Color.white : new Color(0.62f, 0.66f, 0.74f);
            StopAllCoroutines();
            StartCoroutine(Enter(_portraitHolder, line.Kind == SpeakerKind.Me ? -1 : 1));
        }

        IEnumerator Enter(RectTransform rt, int side)
        {
            var group = rt.GetComponent<CanvasGroup>();
            if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
            var end = rt.anchoredPosition;
            var start = end + new Vector2(60 * side, 0);
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 0.2f)
            {
                float k = 1 - (1 - t) * (1 - t);
                rt.anchoredPosition = Vector2.Lerp(start, end, k);
                group.alpha = k;
                yield return null;
            }
            rt.anchoredPosition = end;
            group.alpha = 1;
        }

        (Sprite Sprite, bool Framed) PortraitOf(DialogueLine line)
        {
            switch (line.Kind)
            {
                case SpeakerKind.Me:
                    return (ArtLibrary.Portrait(_career.Me), false);
                case SpeakerKind.Legend:
                    var legend = LegendCrop(line.PortraitKey);
                    return legend != null ? (legend, true) : (Silhouette, false);
                case SpeakerKind.Player:
                    return (PersonArt(line) ?? PlayerFace(line.PortraitKey ?? line.Speaker) ?? Silhouette, false);
                default:
                    return (PersonArt(line) ?? Silhouette, false);
            }
        }

        /// <summary>
        /// Drawn pictures for the people of the stories, by name or role: Art/People/&lt;file&gt;.
        /// Anyone missing here (or whose picture isn't in yet) keeps the silhouette or a player face.
        /// </summary>
        static readonly Dictionary<string, string> People = new Dictionary<string, string>
        {
            // 마지막 순번
            { "최강훈", "choi_ganghun" }, { "윤도현", "yoon_dohyun" }, { "하윤", "hayun" }, { "한소라", "han_sora" },
            // 천재의 무게
            { "백무진", "baek_mujin" }, { "정우진", "jung_woojin" }, { "어머니", "mother" }, { "소이", "soi" },
            // 연습생 신화
            { "도봉구", "do_bonggu" }, { "김반석", "kim_banseok" }, { "할머니", "grandma" }, { "이다온", "lee_daon" },
            // Everyday people
            { "coach", "coach" }, { "감독", "manager" }, { "감독님", "teacher" }, { "트레이너", "trainer" }, { "기자", "reporter" },
            { "라파엘", "rafael" }, { "형", "aide" }, { "할아버지", "grandpa_fan" }, { "사장", "president" }, { "사장님", "president" },
            { "에이전트", "agent" }, { "아이", "kid" }, { "꼬마", "kid" }, { "연인", "girlfriend" }, { "아내", "girlfriend" },
        };

        static Sprite PersonArt(DialogueLine line)
        {
            string file;
            if ((line.PortraitKey != null && People.TryGetValue(line.PortraitKey, out file)) || (line.Speaker != null && People.TryGetValue(line.Speaker, out file)))
                return ArtLibrary.Get("People/" + file);
            return null;
        }

        static readonly Dictionary<string, Sprite> LegendCrops = new Dictionary<string, Sprite>();

        /// <summary>The right half of a legend's wide picture, where he stands.</summary>
        static Sprite LegendCrop(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (LegendCrops.TryGetValue(id, out var cached)) return cached;
            var full = ArtLibrary.Get("Legends/" + id);
            Sprite crop = null;
            if (full != null)
            {
                var r = full.rect;
                float w = Mathf.Min(r.width, r.height * 660f / 690f);
                crop = Sprite.Create(full.texture, new Rect(r.x + r.width - w - r.width * 0.02f, r.y, w, r.height), new Vector2(0.5f, 0.5f), 100);
            }
            LegendCrops[id] = crop;
            return crop;
        }

        static readonly string[] Faces =
        {
            "Portraits/player_01", "Portraits/player_02", "Portraits/player_03", "Portraits/player_04",
            "Portraits/pitcher_01", "Portraits/pitcher_02", "Portraits/pitcher_03", "Portraits/pitcher_04",
        };

        /// <summary>One of the drawn faces for another player, always the same for the same name and never my own.</summary>
        Sprite PlayerFace(string name)
        {
            var mine = ArtLibrary.Portrait(_career.Me);
            var faces = Faces.Select(ArtLibrary.Get).Where(s => s != null && s != mine).ToList();
            if (faces.Count == 0) return null;
            int hash = 23;
            foreach (char c in name ?? "") hash = unchecked(hash * 31 + c);
            return faces[(hash & 0x7fffffff) % faces.Count];
        }

        static Sprite _silhouette;

        /// <summary>A head-and-shoulders shape for people who have no picture yet.</summary>
        static Sprite Silhouette
        {
            get
            {
                if (_silhouette != null) return _silhouette;
                const int w = 256, h = 300;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        // Head, neck and a rounded pair of shoulders, antialiased over a pixel.
                        float head = Vector2.Distance(new Vector2(px, py), new Vector2(128, 206)) - 62;
                        float neck = Mathf.Max(Mathf.Abs(px - 128) - 26, Mathf.Abs(py - 135) - 20);
                        float sx = (px - 128) / 122f, sy = (py - 0) / 120f;
                        float shoulders = (Mathf.Sqrt(sx * sx + sy * sy) - 1) * 110;
                        float d = Mathf.Min(head, Mathf.Min(neck, shoulders));
                        byte a = (byte)(255 * Mathf.Clamp01(0.5f - d));
                        // A soft light from above-left so the shape isn't flat.
                        float light = Mathf.Clamp01(0.75f + 0.25f * (py / h) - 0.15f * (px / w));
                        byte v = (byte)(255 * light);
                        pixels[y * w + x] = new Color32(v, v, v, a);
                    }
                tex.SetPixels32(pixels);
                tex.Apply();
                _silhouette = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0), 100);
                return _silhouette;
            }
        }
    }
}
