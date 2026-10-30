using System;
using System.Collections;
using System.Collections.Generic;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Replays one finished game over the 3D field: scoreboard, outs and runners, play-by-play,
    /// and controls to pause, speed up, jump to my next at-bat or straight to the result.
    /// </summary>
    public class MatchScreen : MonoBehaviour
    {
        static readonly float[] Speeds = { 1f, 2f, 4f };
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        const int InningColumns = 9;

        MatchReplay _replay;
        Action _onClose;
        MatchField _field;

        int _speedIndex;
        bool _paused;
        /// <summary>Steps before this index are applied without animation.</summary>
        int _skipUntil = -1;

        RectTransform _canvas;
        Text[,] _board;
        Text _halfText, _matchupText, _log, _pop, _speedLabel, _pauseLabel;
        Image[] _baseLights = new Image[3];
        Image[] _outLights = new Image[2];
        readonly List<string> _logLines = new List<string>();
        Coroutine _popRoutine;

        /// <summary>Opens the screen; <paramref name="onClose"/> runs when the player leaves it.</summary>
        public const string SpeedPref = "match_speed";

        /// <summary>Opens the screen; <paramref name="onClose"/> runs when the player leaves it.</summary>
        public static MatchScreen Open(GameResult game, Player mine, Action onClose)
        {
            var screen = new GameObject("MatchScreen").AddComponent<MatchScreen>();
            screen._replay = new MatchReplay(game, mine);
            screen._onClose = onClose;
            screen._speedIndex = Mathf.Clamp(PlayerPrefs.GetInt(SpeedPref, 0), 0, Speeds.Length - 1);
            return screen;
        }

        public static string SpeedName(int index) => $"x{Speeds[index]:0}";
        public static int SpeedCount => Speeds.Length;

        float Speed => Speeds[_speedIndex];
        bool Skipping => _replay.Position < _skipUntil;
        GameResult Game => _replay.Game;

        void Start()
        {
            _field = MatchField.Build(transform);
            _field.Mine = _replay.Mine;
            _field.Skipping = () => Skipping;
            BuildUi();
            StartCoroutine(Run());
        }

        // ---------- Playback ----------

        IEnumerator Run()
        {
            RefreshBoard(-1);
            Log($"{Game.Away.Name} vs {Game.Home.Name}, 경기 시작!");
            yield return Wait(0.8f);

            while (!_replay.IsFinished)
            {
                while (_paused && !Skipping) yield return null;

                var step = _replay.Advance();
                var ev = step.Event;
                bool animate = !Skipping;

                var batting = ev.IsTopHalf ? Game.Away : Game.Home;
                var fielding = ev.IsTopHalf ? Game.Home : Game.Away;
                _field.SetTeamColors(TeamColor(batting), TeamColor(fielding));

                if (animate)
                {
                    _field.Speed = Speed;
                    _field.Snap(ev.BasesBefore, ev.Kind == EventKind.PlateAppearance ? ev.Batter : null);
                    ShowSituation(ev, ev.OutsBefore, ev.BasesBefore);
                    if (step.StartsHalf)
                    {
                        Pop(step.HalfLabel, Color.white);
                        yield return Wait(0.9f);
                    }
                    if (step.IsMine)
                    {
                        Pop("내 타석!", Gold);
                        yield return Wait(0.8f);
                    }

                    if (ev.Kind == EventKind.PitchingChange)
                    {
                        yield return Wait(0.6f);
                    }
                    else
                    {
                        yield return _field.Pitch();
                        if (!Skipping) Pop(OutcomeWord(ev), ResultColor(step));
                        yield return _field.Result(step);
                    }
                }

                // Runners left on at the third out walk off the field.
                _field.Snap(ev.OutsAfter >= 3 ? new Player[3] : ev.BasesAfter, null);
                ShowSituation(ev, ev.OutsAfter, ev.BasesAfter);
                RefreshBoard(step.Index);
                string text = $"<color=#9EABC2>{step.HalfLabel}</color> {step.Text}";
                Log(step.IsMine ? $"<color=#FFD24D>{text}</color>" : text);

                if (animate) yield return Wait(0.6f);
            }

            yield return Wait(0.8f);
            ShowFinal();
        }

        /// <summary>Waits game-time seconds; ends early while skipping.</summary>
        IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds && !Skipping; t += Time.deltaTime * Speed)
                yield return null;
        }

        void TogglePause()
        {
            _paused = !_paused;
            _pauseLabel.text = _paused ? "재생" : "일시정지";
        }

        void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            _field.Speed = Speed;
            _speedLabel.text = $"속도 x{Speed:0}";
        }

        void SkipToMyTurn()
        {
            int next = _replay.NextMineIndex(_replay.Position + 1);
            if (next < 0)
            {
                Pop(_replay.Mine == null ? "오늘은 결장" : "남은 타석이 없어요", Color.white);
                return;
            }
            _skipUntil = next;
        }

        void SkipToEnd() => _skipUntil = int.MaxValue;

        // ---------- UI ----------

        void BuildUi()
        {
            var canvasGo = new GameObject("MatchCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _canvas = (RectTransform)canvasGo.transform;

            var root = UiKit.SafeRoot(_canvas);
            UiKit.Column(root, 30, 16);

            BuildScoreboard(root);
            BuildSituation(root);

            // The 3D field shows through this gap.
            UiKit.Height(UiKit.Empty(root, "Field View"), 0, 1);

            var logCard = UiKit.Panel(root, "Log", new Color(0.07f, 0.09f, 0.14f, 0.85f));
            UiKit.Height(logCard, 300);
            _log = UiKit.Label(logCard, "", 30, TextAnchor.LowerLeft);
            UiKit.Stretch(_log.rectTransform);
            _log.rectTransform.offsetMin = new Vector2(24, 20);
            _log.rectTransform.offsetMax = new Vector2(-24, -20);

            var controls = UiKit.Empty(root, "Controls");
            UiKit.Height(controls, 120);
            UiKit.Row(controls, 16);
            _pauseLabel = UiKit.Button(controls, "일시정지", TogglePause, UiKit.Card).GetComponentInChildren<Text>();
            _speedLabel = UiKit.Button(controls, $"속도 {SpeedName(_speedIndex)}", CycleSpeed, UiKit.Card).GetComponentInChildren<Text>();
            UiKit.Button(controls, "내 타석으로", SkipToMyTurn, new Color(0.75f, 0.58f, 0.15f));
            UiKit.Button(controls, "결과 보기", SkipToEnd, UiKit.Accent);

            _pop = UiKit.Label(_canvas, "", 110, TextAnchor.MiddleCenter);
            _pop.rectTransform.anchorMin = new Vector2(0, 0.45f);
            _pop.rectTransform.anchorMax = new Vector2(1, 0.6f);
            _pop.rectTransform.offsetMin = _pop.rectTransform.offsetMax = Vector2.zero;
            _pop.fontStyle = FontStyle.Bold;
            _pop.raycastTarget = false;
            var outline = _pop.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(4, -4);
        }

        void BuildScoreboard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Scoreboard", new Color(0.07f, 0.09f, 0.14f, 0.9f));
            UiKit.Height(card, 230);
            UiKit.Column(card, 16, 6);

            int columns = InningColumns + 3; // team, innings, R, H
            _board = new Text[3, columns];
            for (int row = 0; row < 3; row++)
            {
                var line = UiKit.Empty(card, "Row " + row);
                UiKit.Height(line, 0, 1);
                UiKit.Row(line, 4);
                for (int col = 0; col < columns; col++)
                {
                    bool total = col >= columns - 2;
                    var cell = UiKit.Label(line, "", row == 0 ? 26 : 34,
                        col == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter,
                        row == 0 ? UiKit.Muted : total ? Gold : Color.white);
                    UiKit.Width(cell, col == 0 ? 250 : 0, col == 0 ? 0 : 1);
                    _board[row, col] = cell;
                }
            }
            _board[1, 0].text = Game.Away.Name;
            _board[2, 0].text = Game.Home.Name;
            _board[0, columns - 2].text = "R";
            _board[0, columns - 1].text = "H";
        }

        void BuildSituation(RectTransform root)
        {
            var card = UiKit.Panel(root, "Situation", new Color(0.07f, 0.09f, 0.14f, 0.85f));
            UiKit.Height(card, 130);
            UiKit.Row(card, 20);
            card.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(24, 24, 12, 12);

            var diamond = UiKit.Empty(card, "Bases");
            UiKit.Width(diamond, 110);
            // First base on the right, second on top, third on the left.
            var spots = new[] { new Vector2(30, 0), new Vector2(0, 30), new Vector2(-30, 0) };
            for (int b = 0; b < 3; b++)
            {
                var light = UiKit.Panel(diamond, "Base " + (b + 1), UiKit.Card, rounded: false);
                light.sizeDelta = new Vector2(34, 34);
                light.anchorMin = light.anchorMax = new Vector2(0.5f, 0.5f);
                light.anchoredPosition = spots[b];
                light.localRotation = Quaternion.Euler(0, 0, 45);
                _baseLights[b] = light.GetComponent<Image>();
            }

            var outs = UiKit.Empty(card, "Outs");
            UiKit.Width(outs, 90);
            UiKit.Row(outs, 14).padding = new RectOffset(0, 0, 38, 38);
            for (int i = 0; i < 2; i++)
            {
                _outLights[i] = UiKit.Panel(outs, "Out " + (i + 1), UiKit.Card, rounded: false).GetComponent<Image>();
                _outLights[i].sprite = UiKit.CircleSprite;
                _outLights[i].preserveAspect = true;
            }

            var texts = UiKit.Empty(card, "Texts");
            UiKit.Width(texts, 0, 1);
            UiKit.Column(texts, 0, 4);
            _halfText = UiKit.Label(texts, "", 34);
            _matchupText = UiKit.Label(texts, "", 28, TextAnchor.MiddleLeft, UiKit.Muted);
        }

        void ShowSituation(GameEvent ev, int outs, Player[] bases)
        {
            for (int b = 0; b < 3; b++)
                _baseLights[b].color = bases[b] == null ? UiKit.Card : bases[b] == _replay.Mine ? Gold : UiKit.Warning;
            for (int i = 0; i < 2; i++)
                _outLights[i].color = outs > i ? new Color(0.9f, 0.3f, 0.3f) : UiKit.Card;

            _halfText.text = $"{Commentary.HalfInning(ev.Inning, ev.IsTopHalf)}  {outs}아웃";
            _matchupText.text = ev.Kind == EventKind.PlateAppearance
                ? $"타자 {ev.Batter.Name}  vs  투수 {ev.Pitcher.Name}"
                : $"투수 교체 {ev.Pitcher.Name}";
        }

        void RefreshBoard(int index)
        {
            var score = _replay.ScoreAfter(index);
            int first = Mathf.Max(1, score.Innings - InningColumns + 1);
            for (int i = 0; i < InningColumns; i++)
            {
                int inning = first + i;
                _board[0, i + 1].text = inning.ToString();
                _board[1, i + 1].text = Cell(score.Away, inning);
                _board[2, i + 1].text = Cell(score.Home, inning);
            }
            int r = InningColumns + 1, h = InningColumns + 2;
            _board[1, r].text = score.AwayRuns.ToString();
            _board[2, r].text = score.HomeRuns.ToString();
            _board[1, h].text = score.AwayHits.ToString();
            _board[2, h].text = score.HomeHits.ToString();
        }

        string Cell(List<int?> runs, int inning)
        {
            if (inning > runs.Count) return "";
            var value = runs[inning - 1];
            if (value.HasValue) return value.Value.ToString();
            // Home didn't need to bat in the final inning.
            return _replay.IsFinished && inning == runs.Count ? "X" : "";
        }

        void Log(string line)
        {
            _logLines.Add(line);
            if (_logLines.Count > 5) _logLines.RemoveAt(0);
            _log.text = string.Join("\n", _logLines);
        }

        void Pop(string text, Color color)
        {
            if (_popRoutine != null) StopCoroutine(_popRoutine);
            _popRoutine = StartCoroutine(PopRoutine(text, color));
        }

        IEnumerator PopRoutine(string text, Color color)
        {
            _pop.text = text;
            for (float t = 0; t < 1; t += Time.deltaTime / 1.2f)
            {
                _pop.color = new Color(color.r, color.g, color.b, t < 0.7f ? 1 : (1 - t) / 0.3f);
                _pop.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, Mathf.Min(1, t * 6));
                yield return null;
            }
            _pop.text = "";
        }

        static string OutcomeWord(GameEvent ev)
        {
            switch (ev.Outcome)
            {
                case Outcome.Strikeout: return "삼진!";
                case Outcome.Walk: return "볼넷";
                case Outcome.HitByPitch: return "몸에 맞는 공";
                case Outcome.Single: return "안타!";
                case Outcome.Double: return "2루타!";
                case Outcome.Triple: return "3루타!";
                case Outcome.HomeRun: return ev.RunnersScored.Count == 4 ? "만루 홈런!!" : "홈런!!";
                case Outcome.GroundOut: return ev.DoublePlay ? "병살" : "땅볼 아웃";
                case Outcome.FlyOut: return ev.SacrificeFly ? "희생 플라이" : "뜬공 아웃";
                default: return "직선타 아웃";
            }
        }

        Color ResultColor(ReplayStep step)
        {
            bool good = MatchReplay.IsHit(step.Event.Outcome) || step.Event.Outcome == Outcome.Walk || step.Event.Outcome == Outcome.HitByPitch;
            if (step.IsMine) return good ? Gold : Color.white;
            return step.IsHighlight ? UiKit.Warning : Color.white;
        }

        /// <summary>The club's own uniform colour.</summary>
        static Color TeamColor(Team team) => ColorUtility.TryParseHtmlString(League.ColorOf(team), out var c) ? c : Color.gray;

        // ---------- Result ----------

        void ShowFinal()
        {
            var overlay = UiKit.Dim(_canvas, "Final");
            var card = UiKit.PopupCard(overlay, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.75f));
            UiKit.Column(card, 40, 20);

            string title = Game.IsTie ? "무승부" : $"{Game.Winner.Name} 승리";
            UiKit.Height(UiKit.Label(card, title, 52, TextAnchor.MiddleCenter), 90);

            var lines = new List<string>
            {
                $"{Game.Away.Name} {Game.AwayScore} : {Game.HomeScore} {Game.Home.Name}",
            };
            if (Game.Innings > 9) lines.Add($"연장 {Game.Innings}회");
            if (_replay.Mine != null && Game.Batting.TryGetValue(_replay.Mine, out var l))
                lines.Add($"<color=#FFD24D>{_replay.Mine.Name}  {l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런 {l.RunsBattedIn}타점 {l.Walks}볼넷</color>");
            UiKit.Height(UiKit.Label(card, string.Join("\n\n", lines), 36, TextAnchor.UpperCenter), 0, 1);

            UiKit.Height(UiKit.Button(card, "육성 화면으로", Close), 120);
        }

        void Close()
        {
            Destroy(gameObject);
            _onClose?.Invoke();
        }
    }
}
