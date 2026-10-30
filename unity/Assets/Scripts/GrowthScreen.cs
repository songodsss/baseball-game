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

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

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

        Text _header, _status, _log, _seasonLengthLabel, _todayText;
        RectTransform _fatigueFill;
        readonly Dictionary<Stat, Text> _ratingTexts = new Dictionary<Stat, Text>();
        readonly Dictionary<Stat, RectTransform> _expFills = new Dictionary<Stat, RectTransform>();
        readonly List<Button> _actionButtons = new List<Button>();
        Button _seasonLengthButton, _watchButton, _adButton;
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
            else if (_career.Day == 0 && _career.PendingOffers.Count > 0) ShowOffers(); // closed the app mid-negotiation
            else Log($"{_career.Me.Name} 선수, {_career.Year}년 {_career.Day + 1}일차부터 이어서 합니다.");
        }

        // Phones kill backgrounded apps without warning, so save whenever the app is left too.
        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        void OnApplicationQuit() => Save();

        /// <summary>Between retiring and starting the next player there's no career to save.</summary>
        void Save() => SaveStore.Save(_club, _retired ? null : _career);

        void StartNewCareer(string name, int games, int team)
        {
            var league = new League(new RosterGenerator(_random), games);
            var me = CareerState.NewPlayer(name, _random, _club.PotentialBonus);
            _career = new CareerState(me, league, league.Teams[team], _random);
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

            var root = UiKit.SafeRoot(canvasGo.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);

            var top = UiKit.Empty(root, "Top");
            UiKit.Height(top, 120);
            var topRow = UiKit.Row(top, 16);
            topRow.childForceExpandWidth = false;
            _header = UiKit.Label(top, "", 44);
            UiKit.Width(_header, 0, 1);
            var settings = UiKit.Button(top, "설정", ShowSettings, UiKit.Card);
            settings.GetComponentInChildren<Text>().fontSize = 28;
            UiKit.Width(settings, 150);

            BuildStatusCard(root);
            BuildRatingsCard(root);

            _todayText = UiKit.Label(root, "", 30, TextAnchor.MiddleCenter, UiKit.Muted);
            UiKit.Height(_todayText, 44);

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
            UiKit.Button(bottom, "상점", ShowShop, UiKit.Card);
            UiKit.Button(bottom, "기록", () => RecordsScreen.Open(_career, _club), UiKit.Card);
            _seasonLengthButton = UiKit.Button(bottom, "", CycleSeasonLength, UiKit.Card);
            _seasonLengthLabel = _seasonLengthButton.GetComponentInChildren<Text>();
            foreach (var label in bottom.GetComponentsInChildren<Text>()) label.fontSize = 30;
        }

        void BuildStatusCard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Status", UiKit.Card);
            UiKit.Height(card, 180);
            UiKit.Column(card, 24, 12);
            _status = UiKit.Label(card, "", 32);
            UiKit.Height(_status, 50);
            var fatigueRow = UiKit.Empty(card, "Fatigue");
            UiKit.Height(fatigueRow, 66);
            UiKit.Row(fatigueRow, 16).childForceExpandWidth = false;
            UiKit.Width(UiKit.Label(fatigueRow, "피로", 28, TextAnchor.MiddleLeft, UiKit.Muted), 90);
            var bar = UiKit.Bar(fatigueRow, UiKit.Warning);
            UiKit.Width(bar.parent, 0, 1);
            _fatigueFill = bar;
            _adButton = UiKit.Button(fatigueRow, "", WatchAdForRecovery, new Color(0.2f, 0.6f, 0.45f));
            _adButton.GetComponentInChildren<Text>().fontSize = 26;
            UiKit.Width(_adButton, 280);
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
            if (_career.IsSeasonOver)
            {
                ShowSeasonEnd();
                Tip("season_end", "시즌이 끝나면 성적(OPS)에 따라 연봉이 재계약돼요. 8시즌을 뛰면 FA가 되어 다른 구단으로 옮길 수 있어요. 30세부터는 은퇴해서 내 팀에 영입할 수 있어요.");
            }
            else if (result.Move != null)
            {
                ShowMove(result.Move);
                Tip("move", "성적이 나쁘면 2군에 내려가고, 2군에서 잘 치면 다시 올라와요. 트레이드 마감 즈음엔 이적할 수도 있어요.");
            }
            else if (result.Trade != null)
            {
                ShowTrade(result.Trade);
                Tip("move", "성적이 나쁘면 2군에 내려가고, 2군에서 잘 치면 다시 올라와요. 트레이드 마감 즈음엔 이적할 수도 있어요.");
            }
            else if (result.Event != null)
            {
                ShowEvent(result.Event);
                Tip("event", "가끔 이벤트가 생겨요. 고르는 답에 따라 경험치, 피로, 사기가 달라져요. 사기가 높으면 경기 컨디션이 좋아져요.");
            }
            Save();
            Refresh();
            ShowTips();
        }

        // ---------- Story events ----------

        void ShowEvent(StoryEvent ev)
        {
            var card = OpenOverlay(ev.Title);
            UiKit.Height(UiKit.Label(card, ev.TextFor(_career), 36, TextAnchor.MiddleCenter), 0, 1);
            for (int i = 0; i < ev.Choices.Count; i++)
            {
                int choice = i;
                UiKit.Height(UiKit.Button(card, ev.Choices[i].Label, () => ResolveEvent(ev, choice)), 120);
            }
        }

        void ResolveEvent(StoryEvent ev, int choice)
        {
            var outcome = _career.ResolveEvent(choice);
            string changes = outcome.Changes.Count == 0 ? "" : string.Join("  ", outcome.Changes);
            Log($"<b>{ev.Title}</b> {ev.Choices[choice].Label}" + (changes.Length > 0 ? $"\n{changes}" : ""));

            var card = OpenOverlay(ev.Title);
            string text = outcome.Text + (changes.Length > 0 ? $"\n\n<color=#5FA8FF>{changes}</color>" : "");
            UiKit.Height(UiKit.Label(card, text, 36, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay), 120);
            Save();
            Refresh();
        }

        // ---------- Roster moves ----------

        void ShowMove(RosterMove move)
        {
            string title = move.Kind == RosterMoveKind.Demoted ? "2군 강등" : move.Kind == RosterMoveKind.Promoted ? "1군 복귀" : "트레이드";
            var card = OpenOverlay(title);
            UiKit.Height(UiKit.Label(card, move.Text, 36, TextAnchor.MiddleCenter), 0, 1);
            if (move.Kind == RosterMoveKind.Traded)
                UiKit.Height(UiKit.Label(card, $"{move.From.Name} → <b>{move.To.Name}</b>", 40, TextAnchor.MiddleCenter, TeamColor(move.To.Name)), 100);
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay), 120);
        }

        void ShowTrade(TradeOffer offer)
        {
            var card = OpenOverlay("트레이드 제안");
            UiKit.Height(UiKit.Label(card, offer.Text, 36, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, $"{offer.Team.Name}로 이적", () =>
            {
                var move = _career.AcceptTrade();
                Log($"<color=#FFD24D>{move.Text}</color>");
                Save();
                Refresh();
                ShowMove(move);
            }, TeamColor(offer.Team.Name)), 120);
            UiKit.Height(UiKit.Button(card, $"{_career.MyTeam.Name}에 남기", () =>
            {
                _career.DeclineTrade();
                Log($"{offer.Team.Name}의 트레이드 제안을 거절했어요.");
                CloseOverlay();
            }, UiKit.Muted), 120);
        }

        // ---------- Tutorial ----------

        /// <summary>One-time hints for a new player, remembered across launches.</summary>
        void Tip(string key, string text)
        {
            string pref = "tip_" + key;
            if (PlayerPrefs.GetInt(pref, 0) == 1) return;
            PlayerPrefs.SetInt(pref, 1);
            PlayerPrefs.Save();
            Log($"<color=#5FA8FF><b>도움말</b> {text}</color>");
        }

        void ShowTips()
        {
            if (_career == null) return;
            if (_career.Day >= 1)
                Tip("watch", "'오늘 경기 보기'를 누르면 방금 끝난 경기를 3D로 다시 볼 수 있어요.");
            if (_career.Me.Fatigue >= 60)
                Tip("fatigue", "피로가 80을 넘으면 훈련 효과가 반으로 줄고 부상 위험이 생겨요. 휴식으로 회복하세요.");
            if (_career.Me.Money >= 200)
                Tip("shop", "연봉이 쌓였어요. 상점에서 장비를 사면 능력치가 올라가요. 장비를 다 모으면 개인 시설에 투자할 수 있어요.");
            if (_career.Day >= 5)
                Tip("records", "'기록'에서 리그 순위와 내 성적을 볼 수 있어요.");
        }

        void DescribeDay(DayResult r)
        {
            string label = Actions.First(a => a.Action == r.Action).Label;
            var lines = new List<string> { $"<b>{r.Day + 1}일차</b> {label}" };
            if (r.GotInjured) lines.Add("<color=#F28C40>무리하다 부상! 며칠 쉬어야 합니다.</color>");
            foreach (var a in r.NewAchievements) lines.Add($"<color=#5FA8FF>업적 달성: {a.Name}</color>");
            if (r.PointsGained.Count > 0)
                lines.Add("능력치 상승: " + string.Join(", ", r.PointsGained.Select(p => $"{StatNames[p.Key]} +{p.Value}")));

            if (r.SatOut)
            {
                lines.Add(r.InFarm ? "부상으로 2군 경기 결장." : "부상으로 결장.");
            }
            else if (r.FarmLine != null)
            {
                var f = r.FarmLine;
                lines.Add($"<color=#9EABC2>2군 경기</color> {f.AtBats}타수 {f.Hits}안타 {f.HomeRuns}홈런 {f.Walks}볼넷  (+{r.MoneyEarned}만원)");
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
            if (r.Move != null) lines.Add($"<color=#FFD24D>{r.Move.Text}</color>");
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

        /// <summary>Replays any game; my player is found by id among its batters.</summary>
        void Watch(GameResult game)
        {
            var mine = game.Batting.Keys.FirstOrDefault(p => p.Id == _career.Me.Id);
            _canvas.SetActive(false);
            MatchScreen.Open(game, mine, () => _canvas.SetActive(true));
        }

        // ---------- Monetization hooks ----------

        /// <summary>A rewarded ad takes fatigue down, a few times a day. Never touches ratings.</summary>
        void WatchAdForRecovery()
        {
            if (_overlay != null || Monetization.AdsLeftToday <= 0 || !Monetization.Ads.IsReady) return;
            Monetization.Ads.Show(watched =>
            {
                if (!watched) return;
                Monetization.CountAdWatched();
                int before = _career.Me.Fatigue;
                _career.RecoverFatigue(Monetization.AdFatigueRecovery);
                Log($"광고 보상: 피로 {before} → {_career.Me.Fatigue}");
                Save();
                Refresh();
            });
        }

        static Color TeamColor(string teamName) =>
            ColorUtility.TryParseHtmlString(League.TeamColors.TryGetValue(teamName, out var hex) ? hex : "#888888", out var c) ? c : Color.gray;

        // ---------- Settings ----------

        static readonly string[] TipKeys = { "start", "watch", "fatigue", "shop", "records", "event", "season_end", "move" };

        void ShowSettings()
        {
            var card = OpenOverlay("설정");

            UiKit.Height(UiKit.Label(card, "경기 보기 기본 속도", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            var speeds = UiKit.Empty(card, "Speeds");
            UiKit.Height(speeds, 110);
            UiKit.Row(speeds, 16);
            int current = PlayerPrefs.GetInt(MatchScreen.SpeedPref, 0);
            for (int i = 0; i < MatchScreen.SpeedCount; i++)
            {
                int index = i;
                UiKit.Button(speeds, MatchScreen.SpeedName(i), () =>
                {
                    PlayerPrefs.SetInt(MatchScreen.SpeedPref, index);
                    PlayerPrefs.Save();
                    ShowSettings();
                }, i == current ? UiKit.Accent : UiKit.Background);
            }

            UiKit.Height(UiKit.Label(card, "도움말", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            UiKit.Height(UiKit.Button(card, "도움말 처음부터 다시 보기", () =>
            {
                foreach (var key in TipKeys) PlayerPrefs.DeleteKey("tip_" + key);
                PlayerPrefs.Save();
                CloseOverlay();
                ShowTips();
            }, UiKit.Background), 110);

            UiKit.Height(UiKit.Label(card, "데이터", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            UiKit.Height(UiKit.Button(card, "모든 데이터 지우고 처음부터", ConfirmReset, UiKit.Warning), 110);

            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Label(card, "프로토타입 0.1 · 결제와 광고는 아직 테스트용이라 실제로 청구되지 않아요.", 24, TextAnchor.MiddleCenter, UiKit.Muted), 60);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 110);
        }

        void ConfirmReset()
        {
            var card = OpenOverlay("정말 지울까요?");
            UiKit.Height(UiKit.Label(card, "진행 중인 선수, 내 팀 은퇴 선수, 업적이 모두 사라져요.\n되돌릴 수 없어요.", 32, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "지우기", () =>
            {
                SaveStore.Delete();
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                // Nothing of the old game may be saved on the way out.
                _retired = true;
                _club = new Club();
                var fresh = new GameObject("GrowthScreen").AddComponent<GrowthScreen>();
                fresh.name = "GrowthScreen";
                Destroy(gameObject);
            }, UiKit.Warning), 110);
            UiKit.Height(UiKit.Button(card, "취소", ShowSettings, UiKit.Muted), 110);
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
            var card = OpenOverlay($"상점  (보유 {_career.Me.Money:N0}만원)");
            UiKit.Height(UiKit.Label(card, "장비", 30, TextAnchor.LowerLeft, UiKit.Muted), 40);
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                _career.Me.Equipped.TryGetValue(slot, out var current);
                var next = EquipmentCatalog.All
                    .Where(e => e.Slot == slot && (current == null || e.Price > current.Price))
                    .OrderBy(e => e.Price)
                    .FirstOrDefault();

                var row = UiKit.Empty(card, slot.ToString());
                UiKit.Height(row, 100);
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

            // Facilities: a place for salary once gear is maxed. They never raise a rating directly.
            UiKit.Height(UiKit.Label(card, "개인 시설", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            foreach (var facility in Facilities.All)
            {
                int level = _career.Me.FacilityLevel(facility);
                var row = UiKit.Empty(card, facility.ToString());
                UiKit.Height(row, 100);
                UiKit.Row(row, 20);
                string now = level == 0 ? "없음" : Facilities.Describe(facility, level);
                UiKit.Width(UiKit.Label(row, $"{Facilities.Name(facility)} Lv.{level}\n<size=24>{now}</size>", 30, TextAnchor.MiddleLeft, UiKit.Muted), 0, 1);
                if (level >= Facilities.MaxLevel)
                {
                    UiKit.Width(UiKit.Label(row, "최고 단계", 30, TextAnchor.MiddleCenter), 420);
                    continue;
                }
                int price = Facilities.Price(level + 1);
                var f = facility;
                var upgrade = UiKit.Button(row, $"Lv.{level + 1}: {Facilities.Describe(f, level + 1)}\n{price:N0}만원", () =>
                {
                    if (_career.UpgradeFacility(f))
                    {
                        Log($"{Facilities.Name(f)} Lv.{_career.Me.FacilityLevel(f)} 완공!");
                        Save();
                    }
                    ShowShop();
                    Refresh();
                }, _career.Me.Money >= price ? UiKit.Accent : new Color(0.3f, 0.3f, 0.35f));
                upgrade.GetComponentInChildren<Text>().fontSize = 26;
                UiKit.Width(upgrade, 420);
            }
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 110);
        }

        void ShowSeasonEnd()
        {
            var line = _career.SeasonLine;
            var summary = _career.EndSeason();
            var card = OpenOverlay($"{CareerState.YearOf(summary.Record.Season)} 시즌 종료");
            var body = UiKit.ScrollList(card, 6);
            UiKit.Height(body.parent, 0, 1);
            void Section(string title) => UiKit.Height(UiKit.Label(body, title, 32, TextAnchor.LowerLeft, Gold), 70);
            void Line(string text, int height = 50) => UiKit.Height(UiKit.Label(body, text, 30, TextAnchor.MiddleLeft), height);

            Section("정규시즌");
            Line($"팀 순위 {summary.Rank}위");
            Line($"타율 {Stats.Rate(Stats.Avg(line))}  {line.HomeRuns}홈런  {line.RunsBattedIn}타점  OPS {Stats.Rate(Stats.Ops(line))}");

            var post = summary.Postseason;
            Section("가을야구");
            foreach (var series in post.Series)
                Line($"{series.Name}  {series.High.Name} {series.HighWins} : {series.LowWins} {series.Low.Name}");
            if (post.MyTeamWon) Line("<color=#FFD24D><b>우승!</b></color>  " + MyPostLine(post), 60);
            else if (post.MyTeamIn) Line($"{post.Champion.Name} 우승  ·  " + MyPostLine(post));
            else Line($"{post.Champion.Name} 우승  ·  {_career.MyTeam.Name}은 진출하지 못했습니다.");

            Section("시즌 타이틀");
            foreach (var a in summary.Awards)
            {
                string text = $"{a.Title}  {a.Winner} ({a.Team})  {a.Value}";
                Line(a.Mine ? $"<color=#FFD24D><b>{text}</b></color>" : text);
            }

            Section("계약");
            Line($"연봉 {summary.OldSalary:N0} → <b>{summary.NewSalary:N0}</b>만원" + (post.Bonus > 0 ? $"  ·  가을야구 보너스 {post.Bonus:N0}만원" : ""));
            Line($"나이 {_career.Me.Age}세가 되었습니다.");
            if (summary.Decline.Count > 0)
                Line("<color=#F28C40>노쇠화: " + string.Join(", ", summary.Decline.Select(d => $"{StatNames[d.Key]} -{d.Value}")) + "</color>");

            if (summary.NewAchievements.Count > 0)
            {
                Section("새 업적");
                foreach (var a in summary.NewAchievements) Line($"<color=#5FA8FF>{a.Name}</color>  {a.Description}");
            }

            if (summary.Record.Notes.Count > 0)
            {
                Section("이번 시즌 이동");
                foreach (var note in summary.Record.Notes) Line(note);
            }

            // Free agency or tryouts: pick one offer instead of "다음 시즌".
            bool mustPick = summary.Offers.Count > 0;
            if (summary.Released)
            {
                Section("방출");
                Line($"<color=#F28C40>{_career.MyTeam.Name}에서 방출됐어요.</color>", 50);
                Line(mustPick ? "입단 테스트를 제안한 구단이 있어요. 2군에서 다시 시작해요." : "제안한 구단이 없어요. 유니폼을 벗을 때가 됐어요.", 50);
            }
            else if (mustPick)
            {
                Section("FA 협상");
                Line("자유계약 선수가 되었어요. 계약 기간 동안 연봉이 고정돼요.", 50);
            }
            AddOfferButtons(body, summary.Offers);

            var finalGame = post.Series.Last().Games.LastOrDefault();
            if (finalGame != null)
                UiKit.Height(UiKit.Button(card, "결정전 마지막 경기 보기", () => Watch(finalGame), UiKit.Muted), 100);
            if (_career.Me.Age >= 30 || _career.MustRetire)
                UiKit.Height(UiKit.Button(card, "은퇴하고 내 팀에 영입", Retire, UiKit.Warning), 100);
            if (!_career.MustRetire && !mustPick)
                UiKit.Height(UiKit.Button(card, "다음 시즌", CloseOverlay), 100);
        }

        void AddOfferButtons(Transform parent, List<ContractOffer> offers)
        {
            foreach (var offer in offers)
            {
                var o = offer;
                string label = o.Tryout
                    ? $"{o.Team.Name}  입단 테스트  연 {o.Salary:N0}만원"
                    : $"{(o.Team == _career.MyTeam ? "잔류  " : "")}{o.Team.Name}  {o.Years}년  연 {o.Salary:N0}만원";
                UiKit.Height(UiKit.Button(parent, label, () => SignOffer(o), TeamColor(o.Team.Name)), 100);
            }
        }

        /// <summary>Offers still unanswered after a restart.</summary>
        void ShowOffers()
        {
            var card = OpenOverlay(_career.Released ? "입단 테스트 제안" : "FA 협상");
            UiKit.Height(UiKit.Label(card, _career.Released ? "방출된 뒤 입단 테스트를 제안한 구단들이에요." : "계약 기간 동안 연봉이 고정돼요.", 32, TextAnchor.MiddleCenter, UiKit.Muted), 100);
            AddOfferButtons(card, _career.PendingOffers.ToList());
            if (_career.Me.Age >= 30)
                UiKit.Height(UiKit.Button(card, "은퇴하고 내 팀에 영입", Retire, UiKit.Warning), 100);
        }

        void SignOffer(ContractOffer offer)
        {
            bool moved = offer.Team != _career.MyTeam;
            _career.Sign(offer);
            Log(offer.Tryout
                ? $"{offer.Team.Name} 입단 테스트 합격! 2군에서 다시 시작합니다."
                : moved
                    ? $"FA 이적! {offer.Team.Name}와 {offer.Years}년 연 {offer.Salary:N0}만원 계약"
                    : $"{offer.Team.Name} 잔류, {offer.Years}년 연 {offer.Salary:N0}만원 계약");
            Save();
            CloseOverlay();
            Refresh();
        }

        string MyPostLine(PostseasonResult post)
        {
            var l = post.MyLine;
            return l.PlateAppearances == 0 ? "출전하지 못했어요" : $"내 성적 {l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런";
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

            // Each club has its own uniform; picking the club picks the uniform.
            int team = (int)(_random.NextDouble() * League.DefaultTeamNames.Length);
            var teamLabel = UiKit.Label(card, "", 30, TextAnchor.LowerLeft);
            UiKit.Height(teamLabel, 60);
            var teams = UiKit.Empty(card, "Teams");
            UiKit.Height(teams, 190);
            var grid = teams.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(166, 85);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var teamButtons = new List<Button>();
            void ShowTeam()
            {
                teamLabel.text = $"입단 팀: <b>{League.DefaultTeamNames[team]}</b>";
                for (int i = 0; i < teamButtons.Count; i++)
                {
                    var c = TeamColor(League.DefaultTeamNames[i]);
                    teamButtons[i].GetComponent<Image>().color = i == team ? c : new Color(c.r, c.g, c.b, 0.3f);
                }
            }
            for (int i = 0; i < League.DefaultTeamNames.Length; i++)
            {
                int index = i;
                var b = UiKit.Button(teams, League.DefaultTeamNames[i].Replace(" ", "\n"), () => { team = index; ShowTeam(); });
                b.GetComponentInChildren<Text>().fontSize = 24;
                teamButtons.Add(b);
            }
            ShowTeam();

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
                StartNewCareer(name, games, team);
                CloseOverlay();
                _logLines.Clear();
                Log($"{CareerState.StartYear}년, {_career.Me.Name} 선수 {_career.MyTeam.Name} 입단! 훈련을 골라 하루를 시작하세요.");
                Tip("start", "훈련을 하나 고르면 하루가 지나고 팀 경기가 열려요. 경험치 막대가 다 차면 능력치가 1 올라요. 잠재력(/ 뒤 숫자)까지 키울 수 있어요.");
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
                $"<size=32><color=#9EABC2>{_career.Year} 시즌 {_career.Day + 1}/{_career.League.GamesPerSeason}일 · 팀 {rank}위" + (_career.InFarm ? " · <color=#F28C40>2군</color>" : "") +
                (_career.ContractSeasonsLeft > 0 ? $" · 계약 {_career.ContractSeasonsLeft}년" : "") + "</color></size>";

            var l = _career.SeasonLine;
            string avg = l.AtBats == 0 ? ".000" : ((double)l.Hits / l.AtBats).ToString(".000");
            string injury = me.IsInjured ? $"  <color=#F28C40>부상 {me.InjuryDaysLeft}일</color>" : "";
            string morale = me.Morale > 0 ? $"  <color=#5FA8FF>사기 +{me.Morale}</color>" : me.Morale < 0 ? $"  <color=#F28C40>사기 {me.Morale}</color>" : "";
            _status.text = $"타율 {avg}  {l.HomeRuns}홈런  ·  보유 {me.Money:N0}만원{injury}{morale}";
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
            var today = _career.TodaysGame();
            _todayText.text = today == null ? "" : $"오늘 경기  vs {today.Value.Opponent.Name} ({(today.Value.Home ? "홈" : "원정")})";
            int adsLeft = Monetization.AdsLeftToday;
            UiKit.SetButtonLabel(_adButton, $"광고 보고 회복 ({adsLeft})");
            _adButton.interactable = adsLeft > 0 && me.Fatigue > 0;
            _watchButton.interactable = _lastDay?.MyGame != null;
        }
    }
}
