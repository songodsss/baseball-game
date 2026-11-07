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
    /// Replays one finished game like a TV broadcast in 2D: scoreboard, a big picture cut for each play
    /// (Resources/Art/Broadcast, drawn cards when a picture is missing), name plates, a small diamond
    /// with the runners and the ball's flight, play-by-play, and controls to pause, speed up, jump to
    /// my next at-bat or straight to the result.
    /// </summary>
    public class MatchScreen : MonoBehaviour
    {
        static readonly float[] Speeds = { 1f, 2f, 4f };
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        const int InningColumns = 9;

        MatchReplay _replay;
        Action _onClose;
        BroadcastView _view;

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
        /// <summary>The song before the match, put back on leaving.</summary>
        string _songBefore;

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
            BuildUi();
            _songBefore = GameAudio.CurrentMusic;
            GameAudio.PlayMusic(GameAudio.MusicMatch);
            GameAudio.PlayAmbience(GameAudio.Crowd);
            StartCoroutine(Run());
        }

        // ---------- Playback ----------

        IEnumerator Run()
        {
            RefreshBoard(-1);
            Log($"{Game.Away.Name} vs {Game.Home.Name}, 경기 시작!");
            _view.Cut(BroadcastView.Start, $"{Game.Away.Name} vs {Game.Home.Name}", "플레이볼!", TeamColor(Game.Home));
            yield return Wait(1.4f);

            while (!_replay.IsFinished)
            {
                while (_paused && !Skipping) yield return null;

                var step = _replay.Advance();
                var ev = step.Event;
                bool animate = !Skipping;

                var batting = ev.IsTopHalf ? Game.Away : Game.Home;
                var fielding = ev.IsTopHalf ? Game.Home : Game.Away;

                if (animate)
                {
                    _view.Speed = Speed;
                    _view.SetRunners(ev.BasesBefore, _replay.Mine);
                    ShowSituation(ev, ev.OutsBefore, ev.BasesBefore);
                    if (step.StartsHalf)
                    {
                        _view.Cut(BroadcastView.HalfStart, step.HalfLabel, $"{batting.Name} 공격", TeamColor(batting));
                        Pop(step.HalfLabel, Color.white);
                        yield return Wait(1.0f);
                    }

                    if (ev.Kind != EventKind.PlateAppearance)
                    {
                        if (ev.Kind == EventKind.StolenBase)
                        {
                            bool caught = ev.OutsAfter > ev.OutsBefore;
                            _view.Cut(BroadcastView.Steal, caught ? "도루 실패" : "도루 성공!", ev.Batter.Name, TeamColor(batting));
                            if (!caught) GameAudio.Sfx(GameAudio.CheerSmall, batting == Game.Home ? 0.6f : 0.3f);
                            Pop(caught ? "도루 실패" : "도루 성공!", caught ? Color.gray : Gold);
                        }
                        else
                            _view.Cut(BroadcastView.PitchingChange, "투수 교체", ev.Pitcher.Name, TeamColor(fielding));
                        yield return Wait(1.0f);
                    }
                    else
                    {
                        // The at-bat: name plates and the pitcher's windup, then the result's picture.
                        _view.Plates(ev.Batter.Name, TeamColor(batting), ev.Pitcher.Name, TeamColor(fielding), step.IsMine);
                        if (step.IsMine)
                        {
                            _view.Cut(_replay.Mine == ev.Pitcher ? BroadcastView.MyPitch : BroadcastView.MyAtBat,
                                _replay.Mine == ev.Pitcher ? "내 등판" : "내 타석!", _replay.Mine.Name, TeamColor(_replay.Mine == ev.Pitcher ? fielding : batting));
                            Pop(_replay.Mine == ev.Pitcher ? "승부!" : "내 타석!", Gold);
                            yield return Wait(1.0f);
                        }
                        else if (!step.StartsHalf) _view.Cut(BroadcastView.Pitch, null, null, TeamColor(fielding));
                        yield return Wait(0.55f);
                        if (!Skipping)
                        {
                            bool pitcherMine = _replay.Mine != null && ev.Pitcher == _replay.Mine;
                            _view.Cut(BroadcastView.CutFor(ev, pitcherMine), OutcomeWord(ev), ev.Batter.Name, TeamColor(MatchReplay.IsHit(ev.Outcome) || ev.Outcome == Outcome.Walk || ev.Outcome == Outcome.HitByPitch ? batting : fielding), big: ev.Outcome == Outcome.HomeRun);
                            Pop(OutcomeWord(ev), ResultColor(step));
                            OutcomeSound(step, batting == Game.Home);
                            yield return _view.Flight(ev);
                            yield return _view.MoveRunners(step.Moves, ev.BasesBefore, _replay.Mine);
                        }
                    }
                }

                // Runners left on at the third out walk off the field.
                if (_view != null) _view.SetRunners(ev.OutsAfter >= 3 ? new Player[3] : ev.BasesAfter, _replay.Mine);
                ShowSituation(ev, ev.OutsAfter, ev.BasesAfter);
                RefreshBoard(step.Index);
                string text = $"<color=#BAC4D8>{step.HalfLabel}</color> {step.Text}";
                Log(step.IsMine ? $"<color=#FFD24D>{text}</color>" : text);

                if (animate) yield return Wait(0.6f);
            }

            yield return Wait(0.8f);
            var me = MyTeam();
            bool walkOff = !Game.IsTie && Game.Winner == Game.Home && Game.Events.Count > 0 && !Game.Events[Game.Events.Count - 1].IsTopHalf && Game.Events[Game.Events.Count - 1].RunnersScored.Count > 0;
            if (walkOff) _view.Cut(BroadcastView.WalkOff, "끝내기!", Game.Home.Name, TeamColor(Game.Home), big: true);
            else if (me != null && !Game.IsTie) _view.Cut(Game.Winner == me ? BroadcastView.Win : BroadcastView.Lose, Game.Winner == me ? "승리!" : "패배", me.Name, TeamColor(me), big: Game.Winner == me);
            if (walkOff || me != null) yield return Wait(1.6f);
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
            _view.Speed = Speed;
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
            scaler.gameObject.AddComponent<FitScaler>();
            _canvas = (RectTransform)canvasGo.transform;

            var backdrop = UiKit.Panel(_canvas, "Backdrop", new Color(0.04f, 0.06f, 0.1f), rounded: false);
            UiKit.Stretch(backdrop);
            var stadium = ArtLibrary.Get("Broadcast/" + BroadcastView.Stadium);
            if (stadium != null)
            {
                var bg = UiKit.Panel(backdrop, "Stadium", new Color(0.45f, 0.45f, 0.5f), rounded: false);
                UiKit.Stretch(bg);
                var img = bg.GetComponent<Image>();
                img.sprite = stadium;
                img.preserveAspect = false;
            }

            var root = UiKit.SafeRoot(_canvas);
            UiKit.Column(root, 30, 16);

            BuildScoreboard(root);
            BuildSituation(root);

            var screen = UiKit.Panel(root, "Broadcast", Color.black);
            UiKit.Height(screen, 0, 1);
            _view = BroadcastView.Build(screen, Speed);

            var logCard = UiKit.Panel(root, "Log", new Color(0.07f, 0.09f, 0.14f, 0.85f));
            UiKit.Height(logCard, 230);
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
            if (_logLines.Count > 4) _logLines.RemoveAt(0);
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

        /// <summary>The crack of the bat or the pop of the mitt, and the crowd: louder for the home side and for my at-bats.</summary>
        static void OutcomeSound(ReplayStep step, bool homeBatting)
        {
            float crowd = (homeBatting ? 0.9f : 0.45f) * (step.IsMine || step.IsHighlight ? 1f : 0.75f);
            switch (step.Event.Outcome)
            {
                case Outcome.Strikeout:
                    GameAudio.Sfx(GameAudio.Mitt, 1f, 0.05f);
                    break;
                case Outcome.Walk:
                case Outcome.HitByPitch:
                    break;
                case Outcome.HomeRun:
                    GameAudio.Sfx(GameAudio.BatHomeRun);
                    GameAudio.Sfx(GameAudio.CheerBig, Mathf.Max(crowd, 0.6f));
                    break;
                case Outcome.Single:
                case Outcome.Double:
                case Outcome.Triple:
                    GameAudio.Sfx(GameAudio.BatHit, 1f, 0.06f);
                    GameAudio.Sfx(GameAudio.CheerSmall, crowd);
                    break;
                default: // an out on a ball in play
                    GameAudio.Sfx(GameAudio.BatHit, 0.6f, 0.08f);
                    break;
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

            var myTeam = MyTeam();
            if (myTeam == null || Game.IsTie) GameAudio.Sfx(GameAudio.CheerSmall, 0.6f);
            else GameAudio.Sfx(Game.Winner == myTeam ? GameAudio.Win : GameAudio.Lose);
        }

        /// <summary>The side my player is on, or null when watching someone else's game.</summary>
        Team MyTeam()
        {
            var mine = _replay.Mine;
            if (mine == null) return null;
            foreach (var team in new[] { Game.Away, Game.Home })
                if (team.Lineup.Contains(mine) || team.Rotation.Contains(mine) || team.Bullpen.Contains(mine)) return team;
            return null;
        }

        void Close()
        {
            GameAudio.PlayAmbience(null);
            GameAudio.PlayMusic(_songBefore);
            Destroy(gameObject);
            _onClose?.Invoke();
        }
    }
}
