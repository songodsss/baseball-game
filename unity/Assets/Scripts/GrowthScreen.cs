using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// Prototype growth screen: status, ratings, the six daily actions, today's results,
    /// the equipment shop and the season-end summary. Everything is built in code.
    /// </summary>
    public class GrowthScreen : MonoBehaviour
    {
        // Press Play in any scene and the prototype starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GrowthScreen>() == null)
                new GameObject("GrowthScreen").AddComponent<GrowthScreen>();
        }

        static readonly Dictionary<Stat, string> StatNames = new Dictionary<Stat, string>
        {
            { Stat.Contact, "컨택" }, { Stat.Power, "파워" }, { Stat.Eye, "선구안" },
            { Stat.Speed, "주루" }, { Stat.Defense, "수비" },
        };

        static readonly (DailyAction Action, string Label)[] Actions =
        {
            (DailyAction.TrainContact, "타격 훈련"), (DailyAction.TrainPower, "웨이트"),
            (DailyAction.TrainEye, "선구안 훈련"), (DailyAction.TrainSpeed, "주루 훈련"),
            (DailyAction.TrainDefense, "수비 훈련"), (DailyAction.Rest, "휴식"),
        };

        IRandom _random;
        CareerState _career;
        Club _club = new Club();

        Text _header, _status, _log, _seasonLengthLabel;
        RectTransform _fatigueFill;
        readonly Dictionary<Stat, Text> _ratingTexts = new Dictionary<Stat, Text>();
        readonly Dictionary<Stat, RectTransform> _expFills = new Dictionary<Stat, RectTransform>();
        readonly List<Button> _actionButtons = new List<Button>();
        Button _seasonLengthButton, _watchButton;
        GameObject _canvas;
        DayResult _lastDay;
        bool _retired;
        RectTransform _overlay;
        readonly List<string> _logLines = new List<string>();

        void Start()
        {
            _random = new SeededRandom(System.Environment.TickCount);
            bool loaded = SaveStore.TryLoad(_random, out var club, out var career);
            if (loaded)
            {
                _club = club;
                _career = career;
            }
            BuildUi();
            Refresh();

            if (_career == null) ShowNewPlayer();
            else if (_career.Day == 0 && _career.MustRetire) Retire(); // closed the app on the final season-end card
            else Log($"{_career.Me.Name} 선수, {_career.Season}시즌 {_career.Day + 1}일차부터 이어서 합니다.");
        }

        // Phones kill backgrounded apps without warning, so save whenever the app is left too.
        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();

        /// <summary>Between retiring and starting the next player there's no career to save.</summary>
        void Save() => SaveStore.Save(_club, _retired ? null : _career);

        void StartNewCareer(string name, int games)
        {
            var league = new League(new RosterGenerator(_random), games);
            var me = CareerState.NewPlayer(name, _random, _club.PotentialBonus);
            _career = new CareerState(me, league, league.Teams[0], _random);
            _lastDay = null;
            _retired = false;
            Save();
        }

        // ---------- UI construction ----------

        void BuildUi()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var root = UiKit.Panel(canvasGo.transform, "Root", UiKit.Background);
            UiKit.Stretch(root);
            UiKit.Column(root, 40, 20);

            _header = UiKit.Label(root, "", 44);
            UiKit.Height(_header, 120);

            BuildStatusCard(root);
            BuildRatingsCard(root);

            var actions = UiKit.Empty(root, "Actions");
            UiKit.Height(actions, 260);
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(320, 115);
            grid.spacing = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            foreach (var (action, label) in Actions)
            {
                var a = action;
                _actionButtons.Add(UiKit.Button(actions, label, () => PlayDay(a),
                    a == DailyAction.Rest ? new Color(0.2f, 0.6f, 0.45f) : UiKit.Accent));
            }

            var logCard = UiKit.Panel(root, "Log", UiKit.Card);
            UiKit.Height(logCard, 400, 1);
            _log = UiKit.Label(logCard, "", 30, TextAnchor.UpperLeft);
            UiKit.Stretch(_log.rectTransform);
            _log.rectTransform.offsetMin = new Vector2(24, 24);
            _log.rectTransform.offsetMax = new Vector2(-24, -24);

            var bottom = UiKit.Empty(root, "Bottom");
            UiKit.Height(bottom, 110);
            UiKit.Row(bottom, 20);
            _watchButton = UiKit.Button(bottom, "오늘 경기 보기", WatchGame);
            UiKit.Button(bottom, "장비 상점", ShowShop, UiKit.Card);
            UiKit.Button(bottom, "기록", () => RecordsScreen.Open(_career, _club), UiKit.Card);
            _seasonLengthButton = UiKit.Button(bottom, "", CycleSeasonLength, UiKit.Card);
            _seasonLengthLabel = _seasonLengthButton.GetComponentInChildren<Text>();
            foreach (var label in bottom.GetComponentsInChildren<Text>()) label.fontSize = 30;
        }

        void BuildStatusCard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Status", UiKit.Card);
            UiKit.Height(card, 150);
            UiKit.Column(card, 24, 12);
            _status = UiKit.Label(card, "", 32);
            UiKit.Height(_status, 50);
            var fatigueRow = UiKit.Empty(card, "Fatigue");
            UiKit.Height(fatigueRow, 30);
            UiKit.Row(fatigueRow, 16);
            UiKit.Width(UiKit.Label(fatigueRow, "피로", 28, TextAnchor.MiddleLeft, UiKit.Muted), 90);
            var bar = UiKit.Bar(fatigueRow, UiKit.Warning);
            UiKit.Width(bar.parent, 0, 1);
            _fatigueFill = bar;
        }

        void BuildRatingsCard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Ratings", UiKit.Card);
            UiKit.Height(card, 380);
            UiKit.Column(card, 24, 10);
            foreach (var stat in CareerPlayer.AllStats)
            {
                var row = UiKit.Empty(card, StatNames[stat]);
                UiKit.Height(row, 60);
                UiKit.Row(row, 16);
                UiKit.Width(UiKit.Label(row, StatNames[stat], 32, TextAnchor.MiddleLeft, UiKit.Muted), 140);
                var value = UiKit.Label(row, "", 32);
                UiKit.Width(value, 300);
                _ratingTexts[stat] = value;
                var bar = UiKit.Bar(row, UiKit.Accent);
                UiKit.Width(bar.parent, 0, 1);
                _expFills[stat] = bar;
            }
        }

        // ---------- Actions ----------

        void PlayDay(DailyAction action)
        {
            if (_overlay != null) return;
            var result = _career.PlayDay(action);
            _lastDay = result;
            DescribeDay(result);
            if (_career.IsSeasonOver) ShowSeasonEnd();
            Save();
            Refresh();
        }

        void DescribeDay(DayResult r)
        {
            string label = Actions.First(a => a.Action == r.Action).Label;
            var lines = new List<string> { $"<b>{r.Day + 1}일차</b> {label}" };
            if (r.GotInjured) lines.Add("<color=#F28C40>무리하다 부상! 며칠 쉬어야 합니다.</color>");
            if (r.PointsGained.Count > 0)
                lines.Add("능력치 상승: " + string.Join(", ", r.PointsGained.Select(p => $"{StatNames[p.Key]} +{p.Value}")));

            if (r.SatOut)
            {
                lines.Add("부상으로 결장.");
            }
            else if (r.MyGame != null)
            {
                var g = r.MyGame;
                lines.Add($"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}");
                var l = r.MyLine;
                lines.Add($"오늘 성적 {l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런 {l.RunsBattedIn}타점 {l.Walks}볼넷  (+{r.MoneyEarned}만원)");
                foreach (var ev in g.Events.Where(e => e.Batter == r.MyGamePlayer && IsHighlight(e)))
                    lines.Add($"  {Commentary.HalfInning(ev.Inning, ev.IsTopHalf)} {Commentary.Describe(ev)}");
            }
            Log(string.Join("\n", lines));
        }

        static bool IsHighlight(GameEvent e) => MatchReplay.IsHighlight(e);

        /// <summary>Replays the last day's game in 3D; the growth screen hides until it closes.</summary>
        void WatchGame()
        {
            if (_lastDay?.MyGame == null) return;
            _canvas.SetActive(false);
            MatchScreen.Open(_lastDay.MyGame, _lastDay.MyGamePlayer, () => _canvas.SetActive(true));
        }

        void CycleSeasonLength()
        {
            if (_career.Day != 0) return;
            var lengths = League.SeasonLengths;
            int next = lengths[(System.Array.IndexOf(lengths, _career.League.GamesPerSeason) + 1) % lengths.Length];
            _career.ChangeSeasonLength(next);
            Save();
            Refresh();
        }

        // ---------- Overlays ----------

        RectTransform OpenOverlay(string title)
        {
            CloseOverlay();
            _overlay = UiKit.Panel(GetComponentInChildren<Canvas>().transform, "Overlay", new Color(0, 0, 0, 0.75f));
            UiKit.Stretch(_overlay);
            var card = UiKit.Panel(_overlay, "Card", UiKit.Card);
            card.anchorMin = new Vector2(0.05f, 0.1f);
            card.anchorMax = new Vector2(0.95f, 0.9f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            UiKit.Column(card, 40, 20);
            UiKit.Height(UiKit.Label(card, title, 48, TextAnchor.MiddleCenter), 90);
            return card;
        }

        void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }

        void ShowShop()
        {
            var card = OpenOverlay($"장비 상점  (보유 {_career.Me.Money:N0}만원)");
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                _career.Me.Equipped.TryGetValue(slot, out var current);
                var next = EquipmentCatalog.All
                    .Where(e => e.Slot == slot && (current == null || e.Price > current.Price))
                    .OrderBy(e => e.Price)
                    .FirstOrDefault();

                var row = UiKit.Empty(card, slot.ToString());
                UiKit.Height(row, 110);
                UiKit.Row(row, 20);
                string have = current == null ? "없음" : current.Name;
                UiKit.Width(UiKit.Label(row, $"{have}", 30, TextAnchor.MiddleLeft, UiKit.Muted), 0, 1);
                if (next == null)
                {
                    UiKit.Width(UiKit.Label(row, "최고 등급", 30, TextAnchor.MiddleCenter), 420);
                    continue;
                }
                string bonus = string.Join(" ", next.Bonus.Select(b => $"{StatNames[b.Key]}+{b.Value}"));
                var item = next;
                var buy = UiKit.Button(row, $"{item.Name}\n{bonus} · {item.Price:N0}만원", () =>
                {
                    if (_career.Buy(item.Id))
                    {
                        Log($"{item.Name} 구입!");
                        Save();
                    }
                    ShowShop();
                    Refresh();
                }, _career.Me.Money >= item.Price ? UiKit.Accent : new Color(0.3f, 0.3f, 0.35f));
                buy.GetComponentInChildren<Text>().fontSize = 28;
                UiKit.Width(buy, 420);
            }
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 110);
        }

        void ShowSeasonEnd()
        {
            var line = _career.SeasonLine;
            var summary = _career.EndSeason();
            var card = OpenOverlay($"{summary.Record.Season}시즌 종료");

            double avg = line.AtBats == 0 ? 0 : (double)line.Hits / line.AtBats;
            var text = new List<string>
            {
                $"팀 순위 {summary.Rank}위",
                $"타율 {avg:0.000}  {line.HomeRuns}홈런  {line.RunsBattedIn}타점  OPS {CareerState.Ops(line):0.000}",
                $"연봉 {summary.OldSalary:N0} → <b>{summary.NewSalary:N0}</b>만원",
                $"나이 {_career.Me.Age}세가 되었습니다.",
            };
            if (summary.Decline.Count > 0)
                text.Add("<color=#F28C40>노쇠화: " + string.Join(", ", summary.Decline.Select(d => $"{StatNames[d.Key]} -{d.Value}")) + "</color>");
            UiKit.Height(UiKit.Label(card, string.Join("\n\n", text), 34, TextAnchor.UpperCenter), 0, 1);

            if (_lastDay?.MyGame != null)
                UiKit.Height(UiKit.Button(card, "마지막 경기 보기", WatchGame, UiKit.Muted), 110);
            if (_career.Me.Age >= 30)
                UiKit.Height(UiKit.Button(card, "은퇴하고 내 팀에 영입", Retire, UiKit.Warning), 110);
            if (!_career.MustRetire)
                UiKit.Height(UiKit.Button(card, "다음 시즌", CloseOverlay), 110);
        }

        void Retire()
        {
            var retired = _club.Retire(_career);
            _retired = true;
            Save();
            var card = OpenOverlay("은퇴");
            var t = retired.CareerLine;
            string text =
                $"{retired.Name}, {retired.Seasons}시즌 통산 {t.Hits}안타 {t.HomeRuns}홈런\n\n" +
                $"전성기 능력치 종합 {retired.Overall}로 내 팀에 합류했습니다.\n" +
                $"명예 포인트 +{retired.LegacyPoints} (다음 선수 잠재력 +{_club.PotentialBonus})\n\n" +
                $"내 팀 선수 {_club.Roster.Count}명";
            UiKit.Height(UiKit.Label(card, text, 34, TextAnchor.UpperCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "새 선수 육성 시작", ShowNewPlayer), 110);
        }

        /// <summary>Name the next player and pick a season length. Shown on first launch and after each retirement.</summary>
        void ShowNewPlayer()
        {
            bool first = _club.Roster.Count == 0;
            var card = OpenOverlay(first ? "프로야구 선수 키우기" : "새 선수 등록");

            string intro = first
                ? "18세 신인을 프로 1군에 입단시켜 은퇴할 때까지 키워 보세요.\n은퇴한 선수는 내 팀에 남습니다."
                : $"내 팀 선수 {_club.Roster.Count}명 · 명예 포인트 덕분에 새 선수 잠재력 +{_club.PotentialBonus}";
            UiKit.Height(UiKit.Label(card, intro, 32, TextAnchor.MiddleCenter, UiKit.Muted), 160);

            UiKit.Height(UiKit.Label(card, "선수 이름", 30, TextAnchor.LowerLeft), 60);
            var box = UiKit.Panel(card, "Name", UiKit.Background);
            UiKit.Height(box, 120);
            var text = UiKit.Label(box, "", 44, TextAnchor.MiddleCenter);
            text.supportRichText = false;
            UiKit.Stretch(text.rectTransform);
            var placeholder = UiKit.Label(box, "이름을 입력하세요", 44, TextAnchor.MiddleCenter, UiKit.Muted);
            UiKit.Stretch(placeholder.rectTransform);
            var input = box.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 8;
            input.text = first ? "강하늘" : "";

            UiKit.Height(UiKit.Label(card, "시즌 길이 (짧을수록 하루 성장이 커요. 시즌 사이에 바꿀 수 있어요)", 30, TextAnchor.LowerLeft), 80);
            var lengths = UiKit.Empty(card, "Lengths");
            UiKit.Height(lengths, 110);
            UiKit.Row(lengths, 16);
            int games = _career != null ? _career.League.GamesPerSeason : 36;
            var buttons = new List<(int Games, Button Button)>();
            void Highlight()
            {
                foreach (var (g, b) in buttons) b.GetComponent<Image>().color = g == games ? UiKit.Accent : UiKit.Background;
            }
            foreach (int length in League.SeasonLengths)
            {
                int l = length;
                buttons.Add((l, UiKit.Button(lengths, $"{l}경기", () => { games = l; Highlight(); })));
            }
            Highlight();

            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "육성 시작", () =>
            {
                string name = input.text.Trim();
                if (name.Length == 0)
                {
                    placeholder.color = UiKit.Warning;
                    return;
                }
                StartNewCareer(name, games);
                CloseOverlay();
                _logLines.Clear();
                Log($"{_career.Me.Name} 선수, {_career.MyTeam.Name} 입단! 훈련을 골라 하루를 시작하세요.");
                Refresh();
            }), 120);
        }

        // ---------- Refresh ----------

        void Log(string entry)
        {
            _logLines.Insert(0, entry);
            if (_logLines.Count > 4) _logLines.RemoveAt(_logLines.Count - 1);
            _log.text = string.Join("\n\n", _logLines);
        }

        void Refresh()
        {
            if (_career == null) return; // first launch, still on the new-player card
            var me = _career.Me;
            var rank = _career.League.Ranked().FindIndex(s => s.Team == _career.MyTeam) + 1;
            _header.text = $"<b>{me.Name}</b>  {me.Age}세 · {_career.MyTeam.Name}\n" +
                $"<size=32><color=#9EABC2>{_career.Season}시즌 {_career.Day + 1}/{_career.League.GamesPerSeason}일 · 팀 {rank}위</color></size>";

            var l = _career.SeasonLine;
            string avg = l.AtBats == 0 ? ".000" : ((double)l.Hits / l.AtBats).ToString(".000");
            string injury = me.IsInjured ? $"  <color=#F28C40>부상 {me.InjuryDaysLeft}일</color>" : "";
            _status.text = $"타율 {avg}  {l.HomeRuns}홈런  ·  보유 {me.Money:N0}만원{injury}";
            UiKit.SetBar(_fatigueFill, me.Fatigue / 100f);

            foreach (var stat in CareerPlayer.AllStats)
            {
                int bonus = me.EquipmentBonus(stat);
                string gear = bonus > 0 ? $"<color=#5FA8FF>+{bonus}</color>" : "";
                _ratingTexts[stat].text = $"<b>{me.Ratings[stat]}</b>{gear} <size=24><color=#9EABC2>/ {me.Potential[stat]}</color></size>";
                bool maxed = me.Ratings[stat] >= me.Potential[stat];
                UiKit.SetBar(_expFills[stat], maxed ? 1f : (float)me.Exp[stat] / CareerPlayer.ExpToNext(me.Ratings[stat]));
            }

            foreach (var b in _actionButtons) b.interactable = !me.IsInjured || b == _actionButtons.Last();
            _seasonLengthLabel.text = _career.Day == 0
                ? $"{_career.League.GamesPerSeason}경기 (변경)"
                : $"{_career.League.GamesPerSeason}경기 시즌";
            _seasonLengthButton.interactable = _career.Day == 0;
            _watchButton.interactable = _lastDay?.MyGame != null;
        }
    }
}
