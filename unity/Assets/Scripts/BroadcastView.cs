using System.Collections;
using System.Collections.Generic;
using BaseballSim;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// The match screen's TV picture in 2D. Each play gets a picture cut from Resources/Art/Broadcast
    /// (drawn in grey uniforms with no numbers; the team colour comes from the frame and caption here),
    /// cross-faded with a slow push-in. A missing picture becomes a drawn title card, so the screen
    /// works before any art is in. A small diamond in the corner shows the runners and the ball's flight.
    /// </summary>
    public class BroadcastView : MonoBehaviour
    {
        // Picture names under Resources/Art/Broadcast/ (see art/todo-prompts.md).
        public const string Stadium = "stadium", Start = "play_ball", HalfStart = "inning", Pitch = "pitch",
            MyAtBat = "my_at_bat", MyPitch = "my_pitch", HomeRun = "home_run", Single = "single", Double = "double",
            Triple = "triple", Strikeout = "strikeout", StrikeoutMine = "strikeout_pitcher", Walk = "walk",
            GroundOut = "ground_out", DoublePlay = "double_play", FlyOut = "fly_out", LineOut = "line_out",
            Steal = "steal", PitchingChange = "pitching_change", WalkOff = "walk_off", Win = "win", Lose = "lose";

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        public float Speed = 1f;

        class Layer
        {
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Back, Picture;
            public Text Title, Sub;
            public RectTransform Caption;
        }

        Layer _front, _back;
        RectTransform _frame, _flash;
        Outline _edge;
        Text _batterPlate, _pitcherPlate;
        Image _batterBar, _pitcherBar;
        RectTransform _diamond;
        readonly List<RectTransform> _runners = new List<RectTransform>();
        Coroutine _fade, _push;

        // The small diamond, in its own pixels: home at the bottom.
        static readonly Vector2 HomeSpot = new Vector2(0, -95);
        static readonly Vector2[] BaseSpots = { new Vector2(55, -40), new Vector2(0, 15), new Vector2(-55, -40) };
        const float FenceDistance = 190f;

        public static BroadcastView Build(RectTransform screen, float speed)
        {
            var view = screen.gameObject.AddComponent<BroadcastView>();
            view.Speed = speed;
            view.Make(screen);
            return view;
        }

        void Make(RectTransform screen)
        {
            screen.gameObject.AddComponent<RectMask2D>();
            _frame = screen;
            _edge = screen.gameObject.AddComponent<Outline>();
            _edge.effectDistance = new Vector2(5, -5);
            _back = MakeLayer(screen, "Cut B");
            _front = MakeLayer(screen, "Cut A");

            _flash = UiKit.Panel(screen, "Flash", new Color(1, 1, 1, 0), rounded: false);
            UiKit.Stretch(_flash);
            _flash.GetComponent<Image>().raycastTarget = false;

            // Lower third: batter on the left, pitcher on the right.
            var plates = UiKit.Empty(screen, "Plates");
            plates.anchorMin = new Vector2(0, 0);
            plates.anchorMax = new Vector2(1, 0);
            plates.pivot = new Vector2(0.5f, 0);
            plates.sizeDelta = new Vector2(0, 64);
            plates.anchoredPosition = new Vector2(0, 12);
            (_batterBar, _batterPlate) = Plate(plates, 0.02f, 0.49f, TextAnchor.MiddleLeft);
            (_pitcherBar, _pitcherPlate) = Plate(plates, 0.51f, 0.98f, TextAnchor.MiddleRight);
            plates.gameObject.SetActive(false);

            BuildDiamond(screen);
        }

        Layer MakeLayer(RectTransform parent, string name)
        {
            var l = new Layer();
            l.Root = UiKit.Panel(parent, name, new Color(0, 0, 0, 0), rounded: false);
            UiKit.Stretch(l.Root);
            l.Group = l.Root.gameObject.AddComponent<CanvasGroup>();
            l.Group.alpha = 0;
            l.Group.blocksRaycasts = false;
            l.Back = UiKit.Panel(l.Root, "Back", Color.gray, rounded: false).GetComponent<Image>();
            UiKit.Stretch(l.Back.rectTransform);
            l.Picture = UiKit.Panel(l.Root, "Picture", Color.white, rounded: false).GetComponent<Image>();
            UiKit.Stretch(l.Picture.rectTransform);
            var fit = l.Picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;

            l.Caption = UiKit.Panel(l.Root, "Caption", new Color(0, 0, 0, 0.55f), rounded: false);
            l.Caption.anchorMin = new Vector2(0, 0.5f);
            l.Caption.anchorMax = new Vector2(1, 0.5f);
            l.Caption.sizeDelta = new Vector2(0, 220);
            l.Title = UiKit.Label(l.Caption, "", 96, TextAnchor.MiddleCenter, Color.white);
            UiKit.Stretch(l.Title.rectTransform);
            l.Title.rectTransform.offsetMin = new Vector2(20, 50);
            l.Title.fontStyle = FontStyle.Bold;
            var o = l.Title.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.85f);
            o.effectDistance = new Vector2(4, -4);
            l.Sub = UiKit.Label(l.Caption, "", 36, TextAnchor.LowerCenter, new Color(0.87f, 0.9f, 0.95f));
            UiKit.Stretch(l.Sub.rectTransform);
            l.Sub.rectTransform.offsetMin = new Vector2(20, 14);
            return l;
        }

        (Image, Text) Plate(RectTransform parent, float from, float to, TextAnchor anchor)
        {
            var bar = UiKit.Panel(parent, "Plate", new Color(0.1f, 0.12f, 0.18f, 0.9f));
            bar.anchorMin = new Vector2(from, 0);
            bar.anchorMax = new Vector2(to, 1);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            var text = UiKit.Label(bar, "", 30, anchor, Color.white);
            UiKit.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(18, 0);
            text.rectTransform.offsetMax = new Vector2(-18, 0);
            return (bar.GetComponent<Image>(), text);
        }

        void BuildDiamond(RectTransform screen)
        {
            var box = UiKit.Panel(screen, "Diamond", new Color(0.04f, 0.08f, 0.06f, 0.75f));
            box.anchorMin = box.anchorMax = new Vector2(1, 1);
            box.pivot = new Vector2(1, 1);
            box.sizeDelta = new Vector2(240, 240);
            box.anchoredPosition = new Vector2(-14, -14);
            box.gameObject.AddComponent<RectMask2D>();
            _diamond = box;

            var grass = UiKit.Panel(box, "Outfield", new Color(0.24f, 0.55f, 0.3f), rounded: false);
            grass.GetComponent<Image>().sprite = UiKit.CircleSprite;
            grass.anchorMin = grass.anchorMax = new Vector2(0.5f, 0.5f);
            grass.sizeDelta = new Vector2(FenceDistance * 2, FenceDistance * 2);
            grass.anchoredPosition = HomeSpot;
            var dirt = UiKit.Panel(box, "Infield", new Color(0.72f, 0.52f, 0.34f), rounded: false);
            dirt.anchorMin = dirt.anchorMax = new Vector2(0.5f, 0.5f);
            dirt.sizeDelta = new Vector2(92, 92);
            dirt.anchoredPosition = (HomeSpot + BaseSpots[1]) / 2;
            dirt.localRotation = Quaternion.Euler(0, 0, 45);
            foreach (var spot in new[] { HomeSpot, BaseSpots[0], BaseSpots[1], BaseSpots[2] })
            {
                var bag = Dot(box, spot, 14, Color.white);
                bag.localRotation = Quaternion.Euler(0, 0, 45);
                bag.GetComponent<Image>().sprite = null;
            }
        }

        RectTransform Dot(RectTransform parent, Vector2 at, float size, Color color)
        {
            var dot = UiKit.Panel(parent, "Dot", color, rounded: false);
            dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
            dot.GetComponent<Image>().raycastTarget = false;
            dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(size, size);
            dot.anchoredPosition = at;
            return dot;
        }

        // ---------- Cuts ----------

        /// <summary>The picture for how a plate appearance ended.</summary>
        public static string CutFor(GameEvent ev, bool pitcherMine)
        {
            switch (ev.Outcome)
            {
                case Outcome.HomeRun: return HomeRun;
                case Outcome.Triple: return Triple;
                case Outcome.Double: return Double;
                case Outcome.Single: return Single;
                case Outcome.Strikeout: return pitcherMine ? StrikeoutMine : Strikeout;
                case Outcome.Walk:
                case Outcome.HitByPitch: return Walk;
                case Outcome.GroundOut: return ev.DoublePlay ? DoublePlay : GroundOut;
                case Outcome.FlyOut: return FlyOut;
                default: return LineOut;
            }
        }

        /// <summary>
        /// Cross-fades to a new cut. With no picture for it, a title card in the team colour stands in;
        /// a cut with no title and no picture (the plain windup) keeps the current one.
        /// </summary>
        public void Cut(string id, string title, string sub, Color team, bool big = false)
        {
            var art = ArtLibrary.Get("Broadcast/" + id);
            if (art == null && string.IsNullOrEmpty(title)) return;
            var next = _back;
            _back = _front;
            _front = next;
            next.Root.SetAsLastSibling();
            _flash.SetAsLastSibling();
            _diamond.SetAsLastSibling();
            var plates = _frame.Find("Plates");
            if (plates != null) plates.SetAsLastSibling();

            next.Back.color = Color.Lerp(team, Color.black, 0.55f);
            next.Picture.gameObject.SetActive(art != null);
            if (art != null)
            {
                next.Picture.sprite = art;
                next.Picture.GetComponent<AspectRatioFitter>().aspectRatio = art.rect.width / Mathf.Max(1, art.rect.height);
            }
            // Over a picture the words sit low like a broadcast caption; on a bare card they fill the middle.
            next.Caption.gameObject.SetActive(!string.IsNullOrEmpty(title));
            next.Caption.anchorMin = new Vector2(0, art != null ? 0.12f : 0.5f);
            next.Caption.anchorMax = new Vector2(1, art != null ? 0.12f : 0.5f);
            next.Caption.pivot = new Vector2(0.5f, art != null ? 0 : 0.5f);
            next.Caption.GetComponent<Image>().color = art != null ? new Color(0, 0, 0, 0.55f) : new Color(0, 0, 0, 0.2f);
            next.Title.text = title ?? "";
            next.Title.fontSize = big ? 120 : 96;
            next.Title.color = big ? Gold : Color.white;
            next.Sub.text = sub ?? "";
            _edge.effectColor = team;

            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(CrossFade(next, _back, big));
            if (_push != null) StopCoroutine(_push);
            _push = StartCoroutine(PushIn(next.Picture.rectTransform, big));
        }

        IEnumerator CrossFade(Layer show, Layer hide, bool big)
        {
            float seconds = 0.25f;
            var flash = _flash.GetComponent<Image>();
            for (float t = 0; t < seconds; t += Time.deltaTime * Speed)
            {
                float k = t / seconds;
                show.Group.alpha = k;
                if (big) flash.color = new Color(1, 1, 1, 0.8f * (1 - k));
                yield return null;
            }
            show.Group.alpha = 1;
            hide.Group.alpha = 0;
            flash.color = new Color(1, 1, 1, 0);
            if (big) yield return Shake();
        }

        IEnumerator Shake()
        {
            var rt = _front.Root;
            for (float t = 0; t < 0.35f; t += Time.deltaTime * Speed)
            {
                rt.anchoredPosition = Random.insideUnitCircle * 14 * (1 - t / 0.35f);
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>A slow push-in on the picture, faster on the big moments.</summary>
        IEnumerator PushIn(RectTransform picture, bool big)
        {
            float seconds = 3f, to = big ? 1.14f : 1.07f;
            var drift = new Vector2(Random.Range(-20f, 20f), Random.Range(-10f, 10f));
            for (float t = 0; t < seconds; t += Time.deltaTime * Speed)
            {
                float k = Mathf.SmoothStep(0, 1, t / seconds);
                picture.localScale = Vector3.one * Mathf.Lerp(1f, to, k);
                picture.anchoredPosition = drift * k;
                yield return null;
            }
        }

        /// <summary>The name plates for the at-bat; mine glow gold.</summary>
        public void Plates(string batter, Color batting, string pitcher, Color fielding, bool mine)
        {
            var plates = _frame.Find("Plates");
            if (plates != null) plates.gameObject.SetActive(true);
            _batterPlate.text = $"<size=22><color=#BAC4D8>타자</color></size>  <b>{batter}</b>";
            _pitcherPlate.text = $"<b>{pitcher}</b>  <size=22><color=#BAC4D8>투수</color></size>";
            _batterBar.color = Color.Lerp(batting, Color.black, 0.45f);
            _pitcherBar.color = Color.Lerp(fielding, Color.black, 0.45f);
            if (mine) UiAnim.Pulse(_batterBar, 1.06f);
        }

        // ---------- The small diamond ----------

        public void SetRunners(Player[] bases, Player mine)
        {
            foreach (var r in _runners) if (r != null) Destroy(r.gameObject);
            _runners.Clear();
            if (bases == null) return;
            for (int b = 0; b < 3; b++)
                if (bases[b] != null) _runners.Add(Dot(_diamond, BaseSpots[b], 20, bases[b] == mine ? Gold : new Color(1f, 0.45f, 0.3f)));
        }

        static Vector2 SpotOf(int b) => b < 0 || b >= 3 ? HomeSpot : BaseSpots[b];

        /// <summary>Each runner runs base to base to where the play left him; an out fades away.</summary>
        public IEnumerator MoveRunners(List<RunnerMove> moves, Player[] before, Player mine)
        {
            if (moves == null || moves.Count == 0) yield break;
            foreach (var r in _runners) if (r != null) Destroy(r.gameObject);
            _runners.Clear();
            var paths = new List<(RectTransform Dot, List<Vector2> Path, bool Out)>();
            var moving = new HashSet<Player>();
            foreach (var m in moves)
            {
                moving.Add(m.Runner);
                var path = new List<Vector2> { m.From == RunnerMove.BattersBox ? HomeSpot : SpotOf(m.From) };
                bool isOut = m.To == RunnerMove.Out;
                int to = isOut ? m.From + 1 : m.To;
                for (int b = m.From + 1; b <= Mathf.Min(to, 3); b++) path.Add(SpotOf(b));
                var dot = Dot(_diamond, path[0], 20, m.Runner == mine ? Gold : new Color(1f, 0.45f, 0.3f));
                _runners.Add(dot);
                paths.Add((dot, path, isOut));
            }
            // Runners who stayed put.
            if (before != null)
                for (int b = 0; b < 3; b++)
                    if (before[b] != null && !moving.Contains(before[b]))
                        _runners.Add(Dot(_diamond, BaseSpots[b], 20, before[b] == mine ? Gold : new Color(1f, 0.45f, 0.3f)));

            const float perBase = 0.28f;
            int longest = 1;
            foreach (var p in paths) longest = Mathf.Max(longest, p.Path.Count - 1);
            for (float t = 0; t < perBase * longest; t += Time.deltaTime * Speed)
            {
                foreach (var p in paths)
                {
                    if (p.Dot == null) continue;
                    int legs = Mathf.Max(1, p.Path.Count - 1);
                    float along = Mathf.Min(legs, t / perBase);
                    int leg = Mathf.Min(legs - 1, (int)along);
                    p.Dot.anchoredPosition = p.Path.Count == 1 ? p.Path[0] : Vector2.Lerp(p.Path[leg], p.Path[leg + 1], along - leg);
                    if (p.Out) p.Dot.GetComponent<Image>().color = new Color(0.6f, 0.6f, 0.6f, 1 - along / legs);
                }
                yield return null;
            }
            // Runners who scored or were out leave the diamond.
            foreach (var p in paths)
                if (p.Dot != null && (p.Out || p.Path[p.Path.Count - 1] == HomeSpot && p.Path.Count > 1)) Destroy(p.Dot.gameObject);
        }

        /// <summary>The ball's flight from home plate to where it landed, leaving a short trail.</summary>
        public IEnumerator Flight(GameEvent ev)
        {
            float distance;
            switch (ev.Outcome)
            {
                case Outcome.HomeRun: distance = FenceDistance + 60; break;
                case Outcome.Triple: distance = 178; break;
                case Outcome.Double: distance = 160; break;
                case Outcome.Single: distance = 118; break;
                case Outcome.FlyOut: distance = 140; break;
                case Outcome.LineOut: distance = 95; break;
                case Outcome.GroundOut: distance = 72; break;
                default: yield break; // strikeouts and walks: no ball in play
            }
            // Same game, same flight: the direction comes from the play itself.
            int seed = ev.Inning * 31 + ev.OutsBefore * 7 + (ev.Batter?.Name?.GetHashCode() ?? 0) + ev.AwayScore * 3 + ev.HomeScore;
            float spread = ev.Outcome == Outcome.Triple || ev.Outcome == Outcome.Double ? 40f : 42f;
            float angle = ((seed & 0x7fffffff) % 1000 / 1000f * 2 - 1) * spread;
            if (ev.Outcome == Outcome.Triple) angle = angle < 0 ? -40 : 40;
            var end = HomeSpot + (Vector2)(Quaternion.Euler(0, 0, -angle) * Vector3.up) * distance;
            bool air = ev.Outcome == Outcome.HomeRun || ev.Outcome == Outcome.FlyOut || ev.Outcome == Outcome.Double || ev.Outcome == Outcome.Triple;
            var ball = Dot(_diamond, HomeSpot, 12, Color.white);
            float seconds = air ? 0.75f : 0.4f;
            float nextTrail = 0;
            for (float t = 0; t < seconds; t += Time.deltaTime * Speed)
            {
                float k = t / seconds;
                ball.anchoredPosition = Vector2.Lerp(HomeSpot, end, k);
                ball.localScale = Vector3.one * (air ? 1 + Mathf.Sin(k * Mathf.PI) * 0.9f : 1);
                if (t >= nextTrail)
                {
                    nextTrail = t + 0.04f;
                    var crumb = Dot(_diamond, ball.anchoredPosition, 6, new Color(1, 1, 1, 0.6f));
                    StartCoroutine(FadeAway(crumb, 0.5f));
                }
                yield return null;
            }
            StartCoroutine(FadeAway(ball, 0.4f));
        }

        IEnumerator FadeAway(RectTransform dot, float seconds)
        {
            var img = dot.GetComponent<Image>();
            var start = img.color;
            for (float t = 0; t < seconds && dot != null; t += Time.deltaTime * Speed)
            {
                img.color = new Color(start.r, start.g, start.b, start.a * (1 - t / seconds));
                yield return null;
            }
            if (dot != null) Destroy(dot.gameObject);
        }
    }
}
