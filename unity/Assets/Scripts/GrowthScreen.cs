using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
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
        /// <summary>Plays a save slot; the title screen calls this after a slot is picked.</summary>
        public static void Open(int slot)
        {
            SaveStore.CurrentSlot = slot;
            new GameObject("GrowthScreen").AddComponent<GrowthScreen>();
        }

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        static readonly Dictionary<Stat, string> StatNames =
            ((Stat[])System.Enum.GetValues(typeof(Stat))).ToDictionary(s => s, Stats.Name);

        // A hitter sees the first five, a pitcher the next four; rest is last for both.
        static readonly (DailyAction Action, string Label)[] Actions =
        {
            (DailyAction.TrainContact, "타격 훈련"), (DailyAction.TrainPower, "웨이트"),
            (DailyAction.TrainEye, "선구안 훈련"), (DailyAction.TrainSpeed, "주루 훈련"),
            (DailyAction.TrainDefense, "수비 훈련"),
            (DailyAction.TrainVelocity, "구속 훈련"), (DailyAction.TrainControl, "제구 훈련"),
            (DailyAction.TrainBreaking, "변화구 연마"), (DailyAction.TrainStamina, "체력 훈련"),
            (DailyAction.Rest, "휴식"),
        };

        IRandom _random;
        CareerState _career;
        Club _club = new Club();

        Image _headerBand;
        RectTransform _emblem;
        string _emblemTeam;
        readonly Dictionary<Stat, int> _shownRatings = new Dictionary<Stat, int>();
        Text _header, _status, _money, _log, _seasonLengthLabel, _todayText;
        RectTransform _fatigueFill;
        readonly Dictionary<Stat, Text> _ratingTexts = new Dictionary<Stat, Text>();
        readonly Dictionary<Stat, RectTransform> _expFills = new Dictionary<Stat, RectTransform>();
        Text _condition;
        readonly List<Button> _actionButtons = new List<Button>();
        readonly Dictionary<Stat, GameObject> _ratingRows = new Dictionary<Stat, GameObject>();
        Button _seasonLengthButton, _watchButton, _adButton, _ticketButton, _missionButton, _fastButton;
        bool _fast;
        GameObject _canvas;
        DayResult _lastDay;
        int _goalSkippedSeason;
        bool _retired;
        RectTransform _overlay;
        readonly List<string> _logLines = new List<string>();

        void Start()
        {
            _random = new SeededRandom(System.Environment.TickCount);
            _club = SaveStore.LoadClub(_random);
            SaveStore.TryLoadCareer(SaveStore.CurrentSlot, _club, _random, out _career);
            BuildUi();
            Refresh();

            if (_career == null) ShowNewPlayer();
            else if (_career.Day == 0 && _career.MustRetire) Retire(); // closed the app on the final season-end card
            else if (_career.Day == 0 && _career.PendingOffers.Count > 0) ShowOffers(); // closed the app mid-negotiation
            else if (_career.Day == 0 && _career.PendingSalary != null) ShowSalaryTalk();
            else
            {
                Log($"{_career.Me.Name} 선수, {_career.Year}년 {_career.Day + 1}일차부터 이어서 합니다.");
                CheckIn();
            }
        }

        // Phones kill backgrounded apps without warning, so save whenever the app is left too.
        void OnApplicationPause(bool paused)
        {
            if (paused) Save();
            else if (_overlay == null) CheckIn(); // back the next morning: a new day's missions and check-in
        }

        void OnApplicationQuit() => Save();

        /// <summary>
        /// Hands the phone reminders what they need (they live in their own assembly and read PlayerPrefs). Also
        /// called on every save and refresh, since which object hears about the app pausing first isn't fixed.
        /// </summary>
        void TellNotifications()
        {
            PlayerPrefs.SetString(NotifyPlayerKey, _career != null && !_retired ? _career.Me.Name : "");
            string today = DailyMissions.DateOf(System.DateTime.Now);
            if (_club.Daily.CheckedInToday(today)) PlayerPrefs.SetString(NotifyCheckInKey, today);
        }

        // Same keys as PhoneNotifications (Notifications folder), which this assembly doesn't reference.
        const string NotifyEnabledKey = "notify_enabled", NotifyPlayerKey = "notify_player", NotifyCheckInKey = "notify_checkin";

        /// <summary>Between retiring and starting the next player there's no career to save.</summary>
        void Save()
        {
            // Mid-game the day is half played; the last save (before the day) stands, and the day replays on restart.
            TellNotifications();
            if (_career != null && _career.IsMidGame) return;
            SaveStore.Save(_club, _retired ? null : _career);
        }

        void StartNewCareer(string name, int games, int team, Position position, int number, PitcherRole role)
        {
            var league = new League(new RosterGenerator(_random), games);
            var me = CareerState.NewPlayer(name, _random, _club.PotentialBonus, position, number, role);
            _career = new CareerState(me, league, league.Teams[team], _random) { DecideAtBats = true };
            _shownRatings.Clear();
            _lastDay = null;
            _retired = false;
            Save();
        }

        // ---------- UI construction ----------

        void BuildUi()
        {
            UiKit.EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo;
            UiAnim.FadeIn(canvasGo.transform, 0.25f);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.gameObject.AddComponent<FitScaler>();

            var root = UiKit.SafeRoot(canvasGo.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);

            UiKit.Backdrop(root, ArtLibrary.Background("growth"));

            // The header sits on a band in the club colour, with the club badge at the left.
            var top = UiKit.Panel(root, "Top", UiKit.Card);
            _headerBand = top.GetComponent<Image>();
            UiKit.Height(top, 140);
            Keep(top);
            var topRow = UiKit.Row(top, 16);
            topRow.childForceExpandWidth = false;
            topRow.childControlHeight = false;
            topRow.childAlignment = TextAnchor.MiddleLeft;
            topRow.padding = new RectOffset(16, 16, 10, 10);
            _emblem = ArtLibrary.Emblem(top, "", 104);
            _emblem.sizeDelta = new Vector2(104, 104);
            _header = UiKit.Label(top, "", 44);
            Tappable(_header, () => ProfileScreen.Open(_career));
            _header.rectTransform.sizeDelta = new Vector2(0, 120);
            UiKit.Width(_header, 0, 1);
            var change = HeaderButton(top, "선수\n변경", ConfirmSwitchPlayer);
            change.GetComponentInChildren<Text>().fontSize = 24;
            _missionButton = HeaderButton(top, "미션", ShowMissions);
            HeaderButton(top, "도감", () => CollectionScreen.Open(_career, _club));
            HeaderButton(top, "설정", ShowSettings);

            BuildStatusCard(root);
            BuildRatingsCard(root);

            var todayRow = UiKit.Empty(root, "Today");
            UiKit.Height(todayRow, 64);
            UiKit.Row(todayRow, 16).childForceExpandWidth = false;
            _todayText = UiKit.Label(todayRow, "", 30, TextAnchor.MiddleCenter, UiKit.Muted);
            Tappable(_todayText, () => ScheduleScreen.Open(_career));
            UiKit.Width(_todayText, 0, 1);
            _fastButton = UiKit.Button(todayRow, "", () => { _fast = !_fast; Refresh(); }, UiKit.Card);
            _fastButton.GetComponentInChildren<Text>().fontSize = 26;
            UiKit.Width(_fastButton, 260);

            var actions = UiKit.Empty(root, "Actions");
            UiKit.Height(actions, 260);
            Keep(actions);
            var grid = actions.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(320, 115);
            grid.spacing = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            foreach (var (action, label) in Actions)
            {
                var a = action;
                var button = UiKit.Button(actions, $"{label}\n<size=24><color=#FFE9A8>{ActionEffect(a)}</color></size>", () => PlayDay(a),
                    a == DailyAction.Rest ? new Color(0.2f, 0.6f, 0.45f) : UiKit.Accent);
                button.GetComponentInChildren<Text>().fontSize = 32;
                UiKit.ButtonIcon(button, ArtLibrary.TrainingIcon(a));
                _actionButtons.Add(button);
            }

            var logCard = UiKit.Panel(root, "Log", UiKit.Card);
            UiKit.Height(logCard, 400, 1).minHeight = 120; // the log gives way first on a short screen
            _log = UiKit.Label(logCard, "", 30, TextAnchor.UpperLeft);
            UiKit.Stretch(_log.rectTransform);
            _log.rectTransform.offsetMin = new Vector2(24, 24);
            _log.rectTransform.offsetMax = new Vector2(-24, -24);

            var bottom = UiKit.Empty(root, "Bottom");
            UiKit.Height(bottom, 110);
            Keep(bottom);
            UiKit.Row(bottom, 20);
            _watchButton = UiKit.Button(bottom, "오늘 경기 보기", WatchGame);
            UiKit.ButtonIcon(_watchButton, ArtLibrary.Get("Icons/UI/watch"), 50);
            UiKit.ButtonIcon(UiKit.Button(bottom, "상점", ShowShop, UiKit.Card), ArtLibrary.Get("Icons/UI/shop"), 50);
            UiKit.ButtonIcon(UiKit.Button(bottom, "기록", () => RecordsScreen.Open(_career, _club), UiKit.Card), ArtLibrary.Get("Icons/UI/records"), 50);
            _seasonLengthButton = UiKit.Button(bottom, "", CycleSeasonLength, UiKit.Card);
            _seasonLengthLabel = _seasonLengthButton.GetComponentInChildren<Text>();
            foreach (var label in bottom.GetComponentsInChildren<Text>()) label.fontSize = 30;
        }

        /// <summary>What a training raises: the main stat a lot, a side stat a little.</summary>
        static string ActionEffect(DailyAction action)
        {
            if (action == DailyAction.Rest) return "피로 회복";
            var (main, side) = CareerState.TrainingTargets(action);
            return $"{Stats.Name(main)}▲▲ {Stats.Name(side)}▲";
        }

        void BuildStatusCard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Status", UiKit.Card);
            UiKit.Height(card, 240);
            UiKit.Column(card, 24, 12);
            var moneyRow = UiKit.Empty(card, "Money");
            UiKit.Height(moneyRow, 50);
            UiKit.Row(moneyRow, 12).childForceExpandWidth = false;
            var coin = UiKit.Panel(moneyRow, "Coin", Gold);
            coin.GetComponent<Image>().sprite = UiKit.CircleSprite;
            coin.GetComponent<Image>().type = Image.Type.Simple;
            UiKit.Width(coin, 40);
            UiKit.Height(coin, 40);
            _money = UiKit.Label(moneyRow, "", 36, TextAnchor.MiddleLeft, Gold);
            _money.fontStyle = FontStyle.Bold;
            UiKit.Width(_money, 0, 1);
            _status = UiKit.Label(card, "", 32);
            UiKit.Height(_status, 90);
            _condition = UiKit.Label(card, "", 28, TextAnchor.MiddleLeft);
            UiKit.Height(_condition, 38);
            card.GetComponent<LayoutElement>().preferredHeight += 50; // room for the condition line
            Keep(card);
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
            _ticketButton = UiKit.Button(fatigueRow, "", UseRecoveryTicket, new Color(0.55f, 0.4f, 0.8f));
            _ticketButton.GetComponentInChildren<Text>().fontSize = 26;
            UiKit.Width(_ticketButton, 200);
        }

        /// <summary>Makes a label open something when tapped, with no button look.</summary>
        void Tappable(Text label, UnityEngine.Events.UnityAction onClick)
        {
            var button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            button.onClick.AddListener(UiSound.Tap);
            button.onClick.AddListener(() => { if (_overlay == null && _career != null) onClick(); });
        }

        /// <summary>Never squash this below its height; the log shrinks instead.</summary>
        static void Keep(Component c)
        {
            var layout = c.GetComponent<LayoutElement>();
            if (layout != null) layout.minHeight = layout.preferredHeight;
        }

        Button HeaderButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var button = UiKit.Button(parent, label, onClick, UiKit.Card);
            button.GetComponentInChildren<Text>().fontSize = 28;
            UiKit.Width(button, 130);
            ((RectTransform)button.transform).sizeDelta = new Vector2(130, 100);
            return button;
        }

        void BuildRatingsCard(RectTransform root)
        {
            var card = UiKit.Panel(root, "Ratings", UiKit.Card);
            UiKit.Height(card, 440);
            Keep(card);
            UiKit.Column(card, 24, 8);
            UiKit.Height(UiKit.Label(card, "<b>능력치</b>  <size=24><color=#9EABC2>지금 / 최대(잠재력) · 막대: 다음 +1까지 경험치</color></size>", 32, TextAnchor.MiddleLeft, Gold), 48);
            foreach (var stat in StatNames.Keys)
            {
                var row = UiKit.Empty(card, StatNames[stat]);
                _ratingRows[stat] = row.gameObject;
                UiKit.Height(row, 60);
                UiKit.Row(row, 16);
                var name = UiKit.Label(row, StatNames[stat], 34, TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                UiKit.Width(name, 150);
                var value = UiKit.Label(row, "", 36);
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
            if (_career.GoalOffer != null && _goalSkippedSeason != _career.Season)
            {
                ShowGoalOffer(action);
                return;
            }
            TakeSnapshot();
            if (_fast && Premium.Owns(Premium.FastSkip)) { FastForward(action); return; }
            var result = _career.PlayDay(action);
            if (result.AtBat != null) ShowAtBat(result);
            else AfterDay(result);
        }

        /// <summary>
        /// Fast skip (a paid convenience): the same action several days running, big moments played
        /// the usual way. It stops at the first day that needs a choice or a look, and when tired.
        /// </summary>
        void FastForward(DailyAction action)
        {
            for (int i = 0; i < Premium.FastSkipDays; i++)
            {
                var result = _career.PlayDay(action);
                if (result.AtBat != null) _club.Daily.Count(MissionKind.BigMoment);
                while (result.AtBat != null) result = _career.ChooseApproach(Approach.Normal);
                bool stop = i == Premium.FastSkipDays - 1 || _career.IsSeasonOver || result.Move != null || result.Trade != null ||
                    result.AllStar != null || result.Tournament != null || result.Milestones.Count > 0 || result.TraitOffer != null ||
                    result.Event != null || result.GotInjured || _career.GoalOffer != null ||
                    (action != DailyAction.Rest && _career.Me.Fatigue >= 80) || !_career.TrainingChoices.Contains(action);
                if (stop) { AfterDay(result); return; }
                _lastDay = result;
                _club.Daily.Record(result);
                DescribeDay(result);
            }
        }

        void AfterDay(DayResult result)
        {
            _lastDay = result;
            _club.Daily.Record(result);
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
            else if (result.AllStar != null)
            {
                ShowAllStar(result.AllStar);
            }
            else if (result.Tournament != null)
            {
                ShowTournament(result.Tournament);
            }
            else if (result.Milestones.Count > 0)
            {
                ShowMilestones(result.Milestones);
            }
            else if (result.TraitOffer != null)
            {
                ShowTraitOffer(result.TraitOffer);
            }
            else if (result.Event != null)
            {
                ShowEvent(result.Event);
                Tip("event", "가끔 이벤트가 생겨요. 고르는 답에 따라 경험치, 피로, 사기가 달라져요. 사기가 높으면 경기 컨디션이 좋아져요.");
            }
            Save();
            Refresh();
            ShowGains(result);
            ShowTips();
        }

        // ---------- Growth feedback ----------

        readonly Dictionary<Stat, (int Rating, int Exp)> _before = new Dictionary<Stat, (int Rating, int Exp)>();
        int _moneyBefore;

        void TakeSnapshot()
        {
            _before.Clear();
            foreach (var stat in _career.Me.MyStats) _before[stat] = (_career.Me.Ratings[stat], _career.Me.Exp[stat]);
            _moneyBefore = _career.Me.Money;
        }

        /// <summary>Numbers rise off each rating that grew today, and off the money.</summary>
        void ShowGains(DayResult result)
        {
            var me = _career.Me;
            int shown = 0;
            foreach (var stat in me.MyStats)
            {
                if (!_before.TryGetValue(stat, out var was) || !_ratingTexts.TryGetValue(stat, out var label)) continue;
                int up = me.Ratings[stat] - was.Rating;
                int exp = me.Exp[stat] - was.Exp;
                string text = up > 0 ? $"▲{up}" : exp > 0 ? $"+{exp}" : null;
                if (text == null) continue;
                UiAnim.FloatUp(label.rectTransform, text, up > 0 ? Gold : new Color(0.55f, 0.8f, 1f), up > 0 ? 44 : 32, 0.08f * shown++);
            }
            int earned = me.Money - _moneyBefore;
            if (earned > 0) UiAnim.FloatUp(_money.rectTransform, $"+{earned:N0}만원", Gold, 32);
            _before.Clear();
        }

        // ---------- Big at bats ----------


        /// <summary>The game is paused at my big at bat: read the pitcher and pick an approach.</summary>
        void ShowAtBat(DayResult result)
        {
            _club.Daily.Count(MissionKind.BigMoment);
            if (result.AtBat.Pitching) { ShowPitchChoice(result); return; }
            var s = result.AtBat;
            if (s.Stealing) { ShowSteal(result); return; }
            var card = OpenOverlay("찬스 타석!");
            SituationBoard(card, s, result.AtBatLog.Count > 0 ? "직전 찬스: " + result.AtBatLog.Last() : null);
            PitchMix(card, $"투수 {s.Pitcher.Name}의 승부구", s.FastballShare);
            Hunch(card, $"느낌: 이번엔 <b>{(s.Hunch == Pitch.Fastball ? "직구" : "변화구")}</b>가 올 것 같다", $"선구안 감 적중률 {s.HunchAccuracy:P0}");
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            ChoiceRow(card, ("직구 노리기", Approach.SitFastball, UiKit.Accent), ("변화구 노리기", Approach.SitBreaking, UiKit.Accent));
            ChoiceRow(card, ("신중하게 (볼넷 노림)", Approach.Patient, UiKit.Muted), ("평소대로", Approach.Normal, UiKit.Background));
            // Tactics only when the bases allow them.
            if (s.CanBunt && s.CanHitAndRun)
                ChoiceRow(card, ($"번트 (성공 {s.BuntOdds:P0})", Approach.Bunt, Tactic), ("히트앤런", Approach.HitAndRun, Tactic));
            else if (s.CanBunt)
                ChoiceRow(card, ($"번트로 주자 보내기 (성공 {s.BuntOdds:P0} · 번트 안타 {s.BuntHitOdds:P0})", Approach.Bunt, Tactic));
            Tip("atbat", "찬스 타석에선 노림수를 골라요. 노린 공이 오면 장타 확률이 크게 오르고, 틀리면 삼진이 늘어요. 선구안이 높을수록 '느낌'이 정확해요.");
        }

        /// <summary>My pitcher in a jam: read what the batter sits on and pick the pitch.</summary>
        void ShowPitchChoice(DayResult result)
        {
            var s = result.AtBat;
            var card = OpenOverlay("위기 상황! 승부구 선택");
            SituationBoard(card, s, result.AtBatLog.Count > 0 ? "직전 승부: " + result.AtBatLog.Last() : null);
            PitchMix(card, $"타자 {s.Batter.Name}이(가) 노리는 공", s.SitsFastball);
            Hunch(card, $"포수의 느낌: <b>{(s.Hunch == Pitch.Fastball ? "직구" : "변화구")}</b>를 노리고 있다", $"제구 감 적중률 {s.HunchAccuracy:P0}");
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            ChoiceRow(card, ("직구로 승부", Approach.ThrowFastball, UiKit.Accent), ("변화구로 승부", Approach.ThrowBreaking, UiKit.Accent));
            ChoiceRow(card, ("어렵게 승부 (볼넷 각오)", Approach.PitchAround, UiKit.Muted), ("평소대로", Approach.Normal, UiKit.Background));
            Tip("pitch", "위기에선 승부구를 골라요. 타자가 노리지 않는 공을 던지면 삼진이 늘고, 노리는 공을 던지면 장타를 맞기 쉬워요. 제구가 높을수록 포수의 느낌이 정확해요.");
        }

        static readonly Color Tactic = new Color(0.45f, 0.35f, 0.8f);

        /// <summary>My player on first with second open: run or stay.</summary>
        void ShowSteal(DayResult result)
        {
            var s = result.AtBat;
            var card = OpenOverlay("도루 찬스!");
            SituationBoard(card, s, result.AtBatLog.Count > 0 ? "직전 작전: " + result.AtBatLog.Last() : null);
            UiKit.Height(UiKit.Label(card, $"투수 {s.Pitcher.Name}의 견제를 뚫고 2루로?", 30, TextAnchor.LowerLeft, UiKit.Muted), 48);
            var bar = UiKit.Bar(card, s.StealOdds >= 0.7 ? new Color(0.3f, 0.75f, 0.45f) : UiKit.Warning);
            UiKit.Height(bar.parent, 64);
            UiKit.SetBar(bar, (float)s.StealOdds);
            var odds = UiKit.Label(bar.parent, $"성공 확률 {s.StealOdds:P0}  (주력 {_career.Me.EffectiveRating(Stat.Speed)})", 30, TextAnchor.MiddleCenter);
            odds.fontStyle = FontStyle.Bold;
            UiKit.Stretch(odds.rectTransform);
            UiKit.Height(UiKit.Label(card, "성공하면 득점권, 실패하면 아웃이에요. 빠를수록 성공률이 올라가요.", 26, TextAnchor.MiddleCenter, UiKit.Muted), 60);
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            ChoiceRow(card, ("뛴다!", Approach.Steal, Gold), ("멈춘다", Approach.Normal, UiKit.Background));
            Tip("steal", "1루에 나가면 도루를 시도할 수 있어요. 주루 훈련으로 주력을 올리면 성공률이 높아져요.");
        }

        static readonly Color BaseOff = new Color(1, 1, 1, 0.15f);
        static readonly Color FastballColor = new Color(0.3f, 0.6f, 1f);
        static readonly Color BreakingColor = new Color(0.95f, 0.55f, 0.25f);

        /// <summary>
        /// A broadcast-style board for a paused game: inning, score with club badges, runners on a
        /// diamond, outs as lights, and who is facing whom.
        /// </summary>
        void SituationBoard(RectTransform card, AtBatSituation s, string previous)
        {
            var board = UiKit.Panel(card, "Situation", UiKit.Background);
            UiKit.Height(board, 300);
            var row = UiKit.Row(board, 24);
            row.padding = new RectOffset(28, 28, 20, 20);
            row.childForceExpandWidth = false;

            // Diamond and outs.
            var left = UiKit.Empty(board, "Field");
            UiKit.Width(left, 230);
            var diamond = UiKit.Empty(left, "Bases");
            diamond.anchorMin = diamond.anchorMax = new Vector2(0.5f, 0.62f);
            diamond.sizeDelta = new Vector2(200, 200);
            var spots = new[] { new Vector2(68, 0), new Vector2(0, 68), new Vector2(-68, 0), new Vector2(0, -68) };
            for (int b = 0; b < 4; b++)
            {
                bool home = b == 3;
                var bag = UiKit.Panel(diamond, home ? "Home" : "Base " + (b + 1), home ? Color.white : s.Bases[b] ? Gold : BaseOff, rounded: false);
                bag.sizeDelta = home ? new Vector2(30, 30) : new Vector2(54, 54);
                bag.anchorMin = bag.anchorMax = new Vector2(0.5f, 0.5f);
                bag.anchoredPosition = spots[b];
                bag.localRotation = Quaternion.Euler(0, 0, 45);
                if (!home && s.Bases[b]) UiAnim.Pulse(bag, 1.15f, 0.6f);
            }
            var outs = UiKit.Empty(left, "Outs");
            outs.anchorMin = new Vector2(0, 0);
            outs.anchorMax = new Vector2(1, 0);
            outs.pivot = new Vector2(0.5f, 0);
            outs.sizeDelta = new Vector2(0, 44);
            var outRow = UiKit.Row(outs, 12);
            outRow.childForceExpandWidth = false;
            outRow.childAlignment = TextAnchor.MiddleCenter;
            UiKit.Width(UiKit.Label(outs, "OUT", 26, TextAnchor.MiddleCenter, UiKit.Muted), 70);
            for (int i = 0; i < 2; i++)
            {
                var light = UiKit.Panel(outs, "Out", i < s.Outs ? new Color(1f, 0.35f, 0.3f) : BaseOff, rounded: false);
                light.GetComponent<Image>().sprite = UiKit.CircleSprite;
                UiKit.Width(light, 34);
            }

            // Inning, score, matchup.
            var right = UiKit.Empty(board, "Score");
            UiKit.Width(right, 0, 1);
            UiKit.Column(right, 0, 8).childAlignment = TextAnchor.MiddleCenter;
            bool late = s.Inning >= 8;
            var inning = UiKit.Label(right, Commentary.HalfInning(s.Inning, s.IsTopHalf), 46, TextAnchor.MiddleCenter, late ? Gold : Color.white);
            inning.fontStyle = FontStyle.Bold;
            UiKit.Height(inning, 64);
            string opponent = _career.MidGameOpponent?.Name ?? "상대";
            ScoreLine(right, _career.MyTeam.Name, s.MyRuns, s.MyRuns >= s.TheirRuns);
            ScoreLine(right, opponent, s.TheirRuns, s.TheirRuns >= s.MyRuns);
            string matchup = s.Stealing ? $"1루 주자 <b>{_career.Me.Name}</b>  ·  타자 {s.Batter.Name}"
                : s.Pitching ? $"투수 <b>{_career.Me.Name}</b>  vs  타자 {s.Batter.Name}" : $"타자 <b>{_career.Me.Name}</b>  vs  투수 {s.Pitcher.Name}";
            UiKit.Height(UiKit.Label(right, matchup, 28, TextAnchor.MiddleCenter, UiKit.Muted), 44);

            if (s.Score != null) LineScoreTable(card, s, opponent);
            if (previous != null)
                UiKit.Height(UiKit.Label(card, previous, 26, TextAnchor.MiddleCenter, UiKit.Muted), 44);
        }

        /// <summary>The inning-by-inning scoreboard; the half under way is lit in gold.</summary>
        void LineScoreTable(RectTransform card, AtBatSituation s, string opponent)
        {
            var line = s.Score;
            int first = Mathf.Max(1, line.Innings - 8);
            int columns = Mathf.Max(9, line.Innings - first + 1);
            var widths = new float[columns + 3];
            widths[0] = 210;
            for (int i = 1; i <= columns; i++) widths[i] = 0;
            widths[columns + 1] = widths[columns + 2] = 70;

            var table = UiKit.Panel(card, "Line Score", UiKit.Background);
            UiKit.Height(table, 176);
            UiKit.Column(table, 8, 0);
            var header = new string[columns + 3];
            header[0] = "";
            for (int i = 0; i < columns; i++) header[i + 1] = (first + i).ToString();
            header[columns + 1] = "R";
            header[columns + 2] = "H";
            UiKit.TableRow(table, widths, header, 24, UiKit.Muted);

            string away = s.MineIsAway ? _career.MyTeam.Name : opponent;
            string home = s.MineIsAway ? opponent : _career.MyTeam.Name;
            ScoreRow(table, widths, away, line.Away, line.AwayRuns, line.AwayHits, first, columns, s.MineIsAway, s.IsTopHalf ? s.Inning : 0);
            ScoreRow(table, widths, home, line.Home, line.HomeRuns, line.HomeHits, first, columns, !s.MineIsAway, s.IsTopHalf ? 0 : s.Inning);
        }

        static void ScoreRow(Transform table, float[] widths, string team, List<int?> innings, int runs, int hits,
            int first, int columns, bool mine, int liveInning)
        {
            var cells = new string[columns + 3];
            // Club names are long: the nickname alone fits the column ("서울 블루윙스" → "블루윙스").
            int space = team.IndexOf(' ');
            string shortName = space >= 0 ? team.Substring(space + 1) : team;
            cells[0] = mine ? $"<b>{shortName}</b>" : shortName;
            for (int i = 0; i < columns; i++)
            {
                int inning = first + i;
                int? r = inning <= innings.Count ? innings[inning - 1] : null;
                string text = r?.ToString() ?? "";
                cells[i + 1] = inning == liveInning ? $"<color=#FFD24D><b>{text}</b></color>" : text;
            }
            cells[columns + 1] = $"<color=#FFD24D><b>{runs}</b></color>";
            cells[columns + 2] = hits.ToString();
            UiKit.TableRow(table, widths, cells, 32, mine ? Color.white : new Color(0.8f, 0.84f, 0.9f));
        }

        void ScoreLine(RectTransform parent, string team, int runs, bool leading)
        {
            var line = UiKit.Empty(parent, team);
            UiKit.Height(line, 64);
            var row = UiKit.Row(line, 14);
            row.childForceExpandWidth = false;
            row.childControlHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            var emblem = ArtLibrary.Emblem(line, team, 52);
            emblem.sizeDelta = new Vector2(52, 52);
            var name = UiKit.Label(line, team, 32);
            name.rectTransform.sizeDelta = new Vector2(0, 60);
            UiKit.Width(name, 0, 1);
            var score = UiKit.Label(line, runs.ToString(), 48, TextAnchor.MiddleRight, leading ? Color.white : UiKit.Muted);
            score.fontStyle = FontStyle.Bold;
            score.rectTransform.sizeDelta = new Vector2(80, 60);
            UiKit.Width(score, 80);
        }

        /// <summary>A two-colour bar: fastball share against breaking balls.</summary>
        void PitchMix(RectTransform card, string title, double fastball)
        {
            UiKit.Height(UiKit.Label(card, title, 28, TextAnchor.LowerLeft, UiKit.Muted), 44);
            var bar = UiKit.Panel(card, "Mix", BreakingColor);
            UiKit.Height(bar, 64);
            var fill = UiKit.Panel(bar, "Fastball", FastballColor);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2((float)fastball, 1);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var left = UiKit.Label(bar, $"직구 {fastball:P0}", 30, TextAnchor.MiddleLeft);
            UiKit.Stretch(left.rectTransform);
            left.rectTransform.offsetMin = new Vector2(20, 0);
            left.fontStyle = FontStyle.Bold;
            var right = UiKit.Label(bar, $"변화구 {1 - fastball:P0}", 30, TextAnchor.MiddleRight);
            UiKit.Stretch(right.rectTransform);
            right.rectTransform.offsetMax = new Vector2(-20, 0);
            right.fontStyle = FontStyle.Bold;
        }

        /// <summary>The hunch, called out in gold so it reads before anything else.</summary>
        void Hunch(RectTransform card, string text, string accuracy)
        {
            var box = UiKit.Panel(card, "Hunch", new Color(1f, 0.82f, 0.3f, 0.16f));
            UiKit.Height(box, 104);
            UiKit.Column(box, 10, 0).childAlignment = TextAnchor.MiddleCenter;
            UiKit.Height(UiKit.Label(box, text, 36, TextAnchor.MiddleCenter, Gold), 56);
            UiKit.Height(UiKit.Label(box, accuracy, 26, TextAnchor.MiddleCenter, UiKit.Muted), 36);
        }

        void ChoiceRow(RectTransform card, params (string Label, Approach Approach, Color Color)[] choices)
        {
            var row = UiKit.Empty(card, "Choices");
            UiKit.Height(row, 115);
            UiKit.Row(row, 20);
            foreach (var c in choices)
            {
                var approach = c.Approach;
                UiKit.Button(row, c.Label, () => Answer(approach), c.Color).GetComponentInChildren<Text>().fontSize = 32;
            }
        }

        void Answer(Approach approach)
        {
            CloseOverlay();
            var result = _career.ChooseApproach(approach);
            if (result.AtBat != null) ShowAtBat(result);
            else AfterDay(result);
        }

        // ---------- Season goals ----------

        /// <summary>Before a season's first game: commit to a goal. Harder ones pay more.</summary>
        void ShowGoalOffer(DailyAction then)
        {
            var card = OpenOverlay($"{_career.Year} 시즌 목표");
            UiKit.Height(UiKit.Label(card, "이번 시즌 목표를 하나 골라요.\n어려울수록 보상이 커요. 시즌이 끝나면 정산해요.", 32, TextAnchor.MiddleCenter, UiKit.Muted), 140);
            foreach (var goal in _career.GoalOffer)
            {
                var g = goal;
                string label = $"[{SeasonGoal.TierNames[(int)g.Tier]}] {g.Text}\n<size=26>보상 {g.Money(_career.Me.Salary):N0}만원 + 모든 능력치 경험치 {g.Exp}</size>";
                var color = g.Tier == GoalTier.Hard ? UiKit.Warning : g.Tier == GoalTier.Normal ? UiKit.Accent : UiKit.Muted;
                UiKit.Height(UiKit.Button(card, label, () =>
                {
                    _career.ChooseGoal(g);
                    Log($"<color=#FFD24D>시즌 목표</color> {g.Text} ({SeasonGoal.TierNames[(int)g.Tier]})");
                    Save();
                    CloseOverlay();
                    Refresh();
                    PlayDay(then);
                }, color), 150);
            }
            UiKit.Height(UiKit.Button(card, "목표 없이 시작", () =>
            {
                _goalSkippedSeason = _career.Season;
                CloseOverlay();
                PlayDay(then);
            }, UiKit.Background), 100);
        }

        // ---------- Traits ----------

        void ShowTraitOffer(List<Trait> offer)
        {
            var card = OpenOverlay("특성 선택");
            UiKit.Height(UiKit.Label(card, $"능력치 총합 {Traits.RatingTotal(_career.Me)} 달성! 특성 하나를 골라요.\n훈련한 방향에 따라 후보가 달라져요.", 32, TextAnchor.MiddleCenter, UiKit.Muted), 140);
            foreach (var trait in offer)
            {
                var t = trait;
                UiKit.Height(UiKit.Button(card, $"{t.Name}\n<size=26>{t.Description}</size>", () =>
                {
                    _career.ChooseTrait(t.Id);
                    Log($"<color=#FFD24D>특성 획득: {t.Name}</color> ({t.Description})");
                    CloseOverlay();
                    Save();
                    Refresh();
                }), 150);
            }
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            Tip("trait", "능력치 총합이 늘 때마다 특성을 하나씩 골라요. 한계에 닿은 능력치를 계속 훈련하면 잠재력이 올라가요(한계 돌파).");
        }

        // ---------- Story events ----------

        void ShowEvent(StoryEvent ev)
        {
            var card = OpenOverlay(ev.Title);
            ArtLibrary.Banner(card, ArtLibrary.LegendPortrait(ev.Id) ?? ArtLibrary.EventArt(ev.Id), 380);
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

        void ShowTournament(TournamentResult t)
        {
            var card = OpenOverlay($"{t.Year} {NationalTeam.Name}");
            string medal = t.Medal == null ? "메달 없이 조별리그 탈락" : $"<color=#FFD24D><b>대한민국 {t.Medal}!</b></color>";
            string myGames = _career.Me.IsPitcher
                ? (t.MyPitching.Outs > 0 ? $"{t.MyPitching.Innings}이닝 {t.MyPitching.Runs}실점 {t.MyPitching.Strikeouts}탈삼진" : "등판 기회가 없었어요")
                : $"{t.MyLine.AtBats}타수 {t.MyLine.Hits}안타 {t.MyLine.HomeRuns}홈런 {t.MyLine.RunsBattedIn}타점";
            string me = t.Selected
                ? $"국가대표로 출전! {myGames}\n<color=#5FA8FF>보너스 +{t.Bonus:N0}만원</color>"
                : _career.InFarm ? "2군에 있어서 대표팀에 뽑히지 못했어요."
                : _career.Me.IsPitcher ? "이번엔 대표팀에 뽑히지 못했어요. 평균자책이 리그 최상위면 뽑혀요." : "이번엔 대표팀에 뽑히지 못했어요. 시즌 OPS가 리그 최상위면 뽑혀요.";
            UiKit.Height(UiKit.Label(card, $"{medal}\n\n{me}", 34, TextAnchor.MiddleCenter), 0, 1);
            foreach (var game in t.Games)
            {
                var g = game.Game;
                string label = $"{game.Stage}  {g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}  {(game.Won ? "승" : "패")}";
                UiKit.Height(UiKit.Button(card, label, () => Watch(g), game.Won ? UiKit.Accent : UiKit.Muted), 90);
            }
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay), 110);
        }

        void ShowMilestones(List<string> milestones)
        {
            var card = OpenOverlay(milestones[0] + "!");
            string text = string.Join("\n\n", milestones.Select(m => CareerState.MilestoneCeremony(m, _career.Me.Name)));
            UiKit.Height(UiKit.Label(card, text, 34, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay), 110);
        }

        static string PitchingText(PitcherLine p) =>
            $"{p.Innings}이닝 {p.Runs}실점 {p.Strikeouts}탈삼진 {p.Walks}볼넷" +
            (p.Wins > 0 ? " <color=#5FA8FF>승</color>" : p.Losses > 0 ? " <color=#F28C40>패</color>" : "") +
            (p.Saves > 0 ? " <color=#5FA8FF>세이브</color>" : p.Holds > 0 ? " <color=#5FA8FF>홀드</color>" : p.BlownSaves > 0 ? " <color=#F28C40>블론세이브</color>" : "");

        void ShowAllStar(AllStarResult allStar)
        {
            var g = allStar.Game;
            var card = OpenOverlay(allStar.MyMvp ? "올스타전 MVP!" : "올스타전");
            string text = $"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}\nMVP {allStar.Mvp}\n\n";
            if (allStar.Selected)
            {
                var l = allStar.MyLine;
                string mine = allStar.MyPitching != null ? (_career.Me.Role == PitcherRole.Starter ? "선발 " : "구원 ") + PitchingText(allStar.MyPitching)
                    : _career.Me.IsPitcher ? "등판 기회가 없었어요" : $"{l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런 {l.RunsBattedIn}타점";
                text += (allStar.FanVote ? $"<color=#FFD24D>팬 투표로 뽑힌 올스타!</color> (인기 {_career.Me.Popularity})" : "팬과 감독이 뽑은 올스타!") + $"\n내 성적 {mine}\n" +
                        $"<color=#5FA8FF>보너스 +{allStar.Bonus:N0}만원 · 사기 +2</color>";
            }
            else
            {
                text += _career.InFarm ? "2군에 있어서 올스타에 뽑히지 못했어요."
                    : $"이번엔 올스타에 뽑히지 못했어요. 전반기 {(_career.Me.IsPitcher ? "평균자책" : "OPS")}이 좋거나, 인기가 {CareerState.FanVotePopularity} 이상이면 팬 투표로 뽑혀요.";
            }
            UiKit.Height(UiKit.Label(card, text, 34, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "올스타전 보기", () => Watch(g), UiKit.Muted), 110);
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay), 110);
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
            if (_career.ConditionStars >= 4)
                Tip("condition", "컨디션이 좋은 날(★4 이상)엔 훈련 효과가 올라가요. 컨디션은 매일 바뀌고, 피로가 쌓이면 떨어지고 사기가 높으면 올라가요.");
            if (_career.Day >= 5)
                Tip("records", "'기록'에서 리그 순위와 내 성적을 볼 수 있어요.");
        }

        /// <summary>"오늘 컨디션 ★★★★☆ 좋음 · 훈련 효과 +10%"</summary>
        static string ConditionText(int stars)
        {
            string color = stars >= 5 ? "#FFD24D" : stars == 4 ? "#5FA8FF" : stars <= 2 ? "#F28C40" : "#C8D2E6";
            string filled = new string('★', stars) + "<color=#4A5468>" + new string('☆', CareerState.MaxConditionStars - stars) + "</color>";
            double bonus = CareerState.ConditionTraining(stars) - 1;
            string extra = bonus > 0 ? $" · 훈련 효과 +{Mathf.RoundToInt((float)bonus * 100)}%" : stars <= 2 ? " · 경기력이 떨어질 수 있어요" : "";
            return $"<color=#9EABC2>오늘 컨디션</color>  <color={color}>{filled}  {CareerState.ConditionLabel(stars)}{extra}</color>";
        }

        void DescribeDay(DayResult r)
        {
            string label = Actions.First(a => a.Action == r.Action).Label;
            var lines = new List<string> { $"<b>{r.Day + 1}일차</b> {label}" };
            if (r.GotInjured) lines.Add($"<color=#F28C40>무리하다 부상! {Injuries.Name(r.Injury)}로 {_career.Me.InjuryDaysLeft}일 쉬어야 합니다. 복귀 후에도 한동안 {StatNames[_career.Me.RoleStat(Injuries.StatOf(r.Injury))]}이(가) 조금 떨어져요.</color>");
            foreach (var a in r.NewAchievements) lines.Add($"<color=#5FA8FF>업적 달성: {a.Name}</color>");
            if (r.ConditionStars >= 4 && !r.GotInjured)
                lines.Add($"<color={(r.ConditionStars >= 5 ? "#FFD24D" : "#5FA8FF")}>컨디션 {CareerState.ConditionLabel(r.ConditionStars)}! 훈련 효과 +{Mathf.RoundToInt((float)(CareerState.ConditionTraining(r.ConditionStars) - 1) * 100)}%</color>");
            if (r.PointsGained.Count > 0)
                lines.Add("능력치 상승: " + string.Join(", ", r.PointsGained.Select(p => $"{StatNames[p.Key]} +{p.Value}")));

            if (r.SatOut)
            {
                lines.Add(r.InFarm ? "부상으로 2군 경기 결장." : "부상으로 결장.");
            }
            else if (r.FarmPitching != null)
            {
                lines.Add($"<color=#9EABC2>{(_career.InMilitary ? CareerState.MilitaryClub : "2군")} {(_career.Me.Role == PitcherRole.Starter ? "선발" : "구원")}</color> {PitchingText(r.FarmPitching)}");
            }
            else if (r.InFarm && r.BetweenStarts)
            {
                lines.Add($"<color=#9EABC2>{(_career.InMilitary ? CareerState.MilitaryClub : "2군")}</color> 등판 없는 날" + (_career.Me.Role == PitcherRole.Starter ? $" · 다음 등판까지 {_career.DaysToNextStart + 1}일" : "") + $"");
            }
            else if (r.MyGame != null && r.MyPitching == null && _career.Me.IsPitcher)
            {
                var g = r.MyGame;
                lines.Add($"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}");
                lines.Add((_career.Me.Role == PitcherRole.Starter ? $"등판 없는 날 · 다음 선발까지 {_career.DaysToNextStart + 1}일"
                    : _career.Me.Fatigue >= CareerState.ReliefFatigueLimit ? "피로가 쌓여 휴식 (불펜 대기 제외)" : "불펜 대기, 등판 기회 없음") + $"");
            }
            else if (r.MyGame != null && r.MyPitching != null)
            {
                var g = r.MyGame;
                lines.Add($"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}");
                lines.Add($"<b>{(_career.Me.Role == PitcherRole.Starter ? "선발 등판" : "구원 등판")}</b> {PitchingText(r.MyPitching)}");
                if (r.RivalLine != null)
                    lines.Add($"<color=#E0475B>라이벌과 투타 대결</color> {_career.Rival.Name} {r.RivalLine.AtBats}타수 {r.RivalLine.Hits}안타 {r.RivalLine.HomeRuns}홈런");
            }
            else if (r.FarmLine != null)
            {
                var f = r.FarmLine;
                lines.Add($"<color=#9EABC2>{(_career.InMilitary ? CareerState.MilitaryClub : "2군")} 경기</color> {f.AtBats}타수 {f.Hits}안타 {f.HomeRuns}홈런 {f.Walks}볼넷");
            }
            else if (r.MyGame != null)
            {
                var g = r.MyGame;
                lines.Add($"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}");
                var l = r.MyLine;
                lines.Add($"오늘 성적 {l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런 {l.RunsBattedIn}타점 {l.Walks}볼넷");
                if (r.RivalLine != null)
                    lines.Add($"<color=#E0475B>라이벌 맞대결</color> {_career.Rival.Name} {r.RivalLine.AtBats}타수 {r.RivalLine.Hits}안타 {r.RivalLine.HomeRuns}홈런");
                foreach (var ev in g.Events.Where(e => e.Batter == r.MyGamePlayer && IsHighlight(e)))
                    lines.Add($"  {Commentary.HalfInning(ev.Inning, ev.IsTopHalf)} {Commentary.Describe(ev)}");
            }
            foreach (var atBat in r.AtBatLog) lines.Add($"  {(_career.Me.IsPitcher ? "승부구" : "찬스")}: {atBat}");
            if (r.OldSlot != null)
            {
                bool up = CareerState.SlotStanding(_career.LineupSlot) < CareerState.SlotStanding(r.OldSlot.Value);
                lines.Add($"<color={(up ? "#5FA8FF" : "#F28C40")}>타순 변경: {CareerState.SlotName(r.OldSlot.Value)} → {CareerState.SlotName(_career.LineupSlot)}</color>");
            }
            foreach (var m in r.Milestones) lines.Add($"<color=#FFD24D>{m} 달성!</color>");
            foreach (var stat in r.Breakthroughs)
                lines.Add($"<color=#F28C40>한계 돌파! {StatNames[stat]} 잠재력 +{CareerPlayer.BreakthroughGain} → {_career.Me.Potential[stat]}</color>");
            if (r.Move != null) lines.Add($"<color=#FFD24D>{r.Move.Text}</color>");
            if (r.Tournament != null)
                lines.Add($"<color=#FFD24D>{NationalTeam.Name}</color> 대한민국 {r.Tournament.Medal ?? "조별리그 탈락"}" + (r.Tournament.Selected ? " (국가대표 출전)" : "") +
                    (r.Tournament.Selected && r.Tournament.Medal == NationalTeam.Gold ? "  <color=#5FA8FF>병역 면제!</color>" : ""));
            if (r.AllStar != null)
            {
                var g = r.AllStar.Game;
                lines.Add($"<color=#FFD24D>올스타전</color> {g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}  MVP {r.AllStar.Mvp}");
            }
            if (r.Payday != null)
            {
                var pay = r.Payday;
                string label2 = _career.InMilitary ? "군 복무 수당" : "월급";
                lines.Add($"<color=#FFD24D>{pay.Month} {label2} {pay.Salary:N0}만원" + (pay.Bonus > 0 ? $" + 성적 보너스 {pay.Bonus:N0}만원" : "") + " 입금!</color>");
            }
            Log(string.Join("\n", lines));
        }

        static bool IsHighlight(GameEvent e) => MatchReplay.IsHighlight(e);

        /// <summary>Replays the last day's game in 3D; the growth screen hides until it closes.</summary>
        void WatchGame()
        {
            if (_lastDay?.MyGame == null) return;
            _club.Daily.Count(MissionKind.WatchGame);
            Save();
            Refresh();
            _canvas.SetActive(false);
            MatchScreen.Open(_lastDay.MyGame, _lastDay.MyGamePlayer, () => _canvas.SetActive(true));
        }

        /// <summary>Replays any game; my player is found by id among its batters.</summary>
        void Watch(GameResult game)
        {
            var mine = game.Batting.Keys.Concat(game.Pitching.Keys).FirstOrDefault(p => p.Id == _career.Me.Id);
            _canvas.SetActive(false);
            MatchScreen.Open(game, mine, () => _canvas.SetActive(true));
        }

        // ---------- Monetization hooks ----------

        /// <summary>A rewarded ad takes fatigue down, a few times a day. Never touches ratings.</summary>
        void WatchAdForRecovery()
        {
            if (_overlay != null || Monetization.AdsLeftToday <= 0) return;
            void Reward(bool watched)
            {
                if (!watched) return;
                Monetization.CountAdWatched();
                int before = _career.Me.Fatigue;
                _career.RecoverFatigue(Monetization.AdFatigueRecovery);
                Log($"{(Premium.Owns(Premium.RemoveAds) ? "회복 보상" : "광고 보상")}: 피로 {before} → {_career.Me.Fatigue}");
                Save();
                Refresh();
            }
            // With ads removed the reward comes straight away; the daily limit stays.
            if (Premium.Owns(Premium.RemoveAds)) Reward(true);
            else if (Monetization.Ads.IsReady) Monetization.Ads.Show(Reward);
        }

        // ---------- Daily missions and check-in ----------

        static string Today => DailyMissions.DateOf(System.DateTime.Now);

        /// <summary>A new date brings new missions; the first visit of the day gets the check-in reward.</summary>
        void CheckIn()
        {
            // Not before the screen is up (Unity also reports an unpause at startup), nor over the new-player card.
            if (_club == null || _career == null || _canvas == null) return;
            var daily = _club.Daily;
            bool newDay = daily.Refresh(Today);
            var reward = daily.CheckIn(Today, _club, _career);
            if (reward == null)
            {
                if (newDay) { Save(); Refresh(); }
                return;
            }
            Save();
            Refresh();
            var card = OpenOverlay("출석 체크");
            var week = UiKit.Empty(card, "Week");
            UiKit.Height(week, 170);
            UiKit.Row(week, 10);
            int dayOfWeek = (daily.Streak - 1) % 7;
            for (int i = 0; i < 7; i++)
            {
                var r = DailyMissions.CheckInReward(i + 1);
                var cell = UiKit.Panel(week, "Day", i < dayOfWeek ? UiKit.Background : i == dayOfWeek ? UiKit.Accent : UiKit.Shade(UiKit.Card, 1.3f));
                var label = UiKit.Label(cell, $"{i + 1}일\n<size=20>{DailyMissions.DescribeReward(r.Kind, r.Amount)}</size>", 24, TextAnchor.MiddleCenter,
                    i < dayOfWeek ? UiKit.Muted : Color.white);
                UiKit.Stretch(label.rectTransform);
                if (i == dayOfWeek) UiAnim.PopIn(cell);
            }
            string text = $"<color=#FFD24D><b>{daily.Streak}일 연속 출석!</b></color>\n\n오늘의 보상: <b>{DailyMissions.DescribeReward(reward.Value.Kind, reward.Value.Amount)}</b>\n\n" +
                $"<size=26><color=#9EABC2>하루라도 빠지면 1일차부터 다시 시작해요. 총 출석 {daily.TotalCheckIns}일</color></size>";
            UiKit.Height(UiKit.Label(card, text, 34, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "오늘의 미션 보기", ShowMissions), 110);
            UiKit.Height(UiKit.Button(card, "확인", CloseOverlay, UiKit.Muted), 110);
        }

        void ShowMissions()
        {
            var daily = _club.Daily;
            daily.Refresh(Today);
            var card = OpenOverlay("오늘의 미션");
            UiKit.Height(UiKit.Label(card, "매일 자정에 새 미션 3개가 나와요. 다 하면 보상을 받으세요.\n모든 슬롯이 함께 써요.", 28, TextAnchor.MiddleCenter, UiKit.Muted), 100);
            for (int i = 0; i < daily.Today.Count; i++)
            {
                int index = i;
                var m = daily.Today[i];
                var row = UiKit.Panel(card, "Mission", UiKit.Shade(UiKit.Card, 1.3f));
                UiKit.Height(row, 170);
                var layout = UiKit.Row(row, 16);
                layout.padding = new RectOffset(24, 24, 16, 16);
                layout.childForceExpandWidth = false;
                var info = UiKit.Empty(row, "Info");
                UiKit.Width(info, 0, 1);
                UiKit.Column(info, 0, 6);
                UiKit.Height(UiKit.Label(info, $"<b>{m.Text}</b>", 30), 50);
                UiKit.Height(UiKit.Label(info, $"보상 {m.RewardText}  ·  {m.Progress}/{m.Goal}", 26, TextAnchor.MiddleLeft, UiKit.Muted), 40);
                var bar = UiKit.Bar(info, m.Done ? new Color(0.2f, 0.75f, 0.45f) : UiKit.Accent);
                UiKit.Height(bar.parent, 22);
                UiKit.SetBar(bar, (float)m.Progress / m.Goal);
                var claim = UiKit.Button(row, m.Claimed ? "받음" : m.Done ? "받기" : "진행 중", () =>
                {
                    if (!daily.Claim(index, _club, _career)) return;
                    Log($"<color=#5FA8FF>미션 보상</color> {m.RewardText}");
                    Save();
                    Refresh();
                    ShowMissions();
                }, m.Done && !m.Claimed ? new Color(0.2f, 0.75f, 0.45f) : UiKit.Background);
                claim.interactable = m.Done && !m.Claimed;
                UiKit.Width(claim, 200);
            }
            UiKit.Height(UiKit.Label(card, $"출석 {daily.Streak}일 연속 · 피로 회복권 {daily.RecoveryTickets}장", 28, TextAnchor.MiddleCenter, UiKit.Muted), 70);
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 110);
        }

        void UseRecoveryTicket()
        {
            if (_overlay != null) return;
            int before = _career.Me.Fatigue;
            if (!_club.Daily.UseTicket(_career)) return;
            Log($"피로 회복권 사용: 피로 {before} → {_career.Me.Fatigue}");
            Save();
            Refresh();
        }

        static Color TeamColor(string teamName) => UiKit.TeamColor(teamName);

        // ---------- Settings ----------

        static readonly string[] TipKeys = { "start", "watch", "fatigue", "shop", "records", "event", "season_end", "move", "atbat", "trait", "pitch", "steal" };

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

            if (_career != null)
            {
                UiKit.Height(UiKit.Label(card, "찬스 타석", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
                UiKit.Height(UiKit.Button(card, _career.DecideAtBats ? "노림수 직접 고르기: 켜짐" : "노림수 직접 고르기: 꺼짐 (자동)", () =>
                {
                    _career.DecideAtBats = !_career.DecideAtBats;
                    Save();
                    ShowSettings();
                }, _career.DecideAtBats ? UiKit.Accent : UiKit.Background), 110);
            }

            UiKit.Height(UiKit.Label(card, "소리", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            UiKit.Height(UiKit.Button(card, UiSound.Muted ? "효과음: 꺼짐" : "효과음: 켜짐", () =>
            {
                UiSound.Muted = !UiSound.Muted;
                ShowSettings();
            }, UiSound.Muted ? UiKit.Background : UiKit.Accent), 110);

            UiKit.Height(UiKit.Label(card, "도움말", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            UiKit.Height(UiKit.Button(card, "도움말 처음부터 다시 보기", () =>
            {
                foreach (var key in TipKeys) PlayerPrefs.DeleteKey("tip_" + key);
                PlayerPrefs.Save();
                CloseOverlay();
                ShowTips();
            }, UiKit.Background), 110);

            UiKit.Height(UiKit.Label(card, "폰 알림", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            bool notify = PlayerPrefs.GetInt(NotifyEnabledKey, 1) == 1;
            UiKit.Height(UiKit.Button(card, notify ? "출석·훈련 알림: 켜짐 (안드로이드)" : "출석·훈련 알림: 꺼짐", () =>
            {
                PlayerPrefs.SetInt(NotifyEnabledKey, notify ? 0 : 1);
                PlayerPrefs.Save();
                ShowSettings();
            }, notify ? UiKit.Accent : UiKit.Background), 110);

            UiKit.Height(UiKit.Label(card, "데이터", 30, TextAnchor.LowerLeft, UiKit.Muted), 60);
            var data = UiKit.Empty(card, "Data");
            UiKit.Height(data, 110);
            UiKit.Row(data, 16);
            UiKit.Button(data, "타이틀로", BackToTitle, UiKit.Accent);
            UiKit.Button(data, "유료 상품 (테스트)", () => { CloseOverlay(); PremiumScreen.Open(Refresh); }, new Color(0.55f, 0.4f, 0.8f));
            UiKit.Height(UiKit.Button(card, "모든 데이터 지우고 처음부터", ConfirmReset, UiKit.Warning), 110);

            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Label(card, "프로토타입 0.1 · 결제와 광고는 아직 테스트용이라 실제로 청구되지 않아요.", 24, TextAnchor.MiddleCenter, UiKit.Muted), 60);
            UiKit.Height(UiKit.Label(card, "아이콘: game-icons.net (Delapouite, Lorc, Skoll · CC BY 3.0) · 글꼴: Jua (SIL OFL)\n" +
                "선수 모델: Quaternius · 잔디·흙·하늘: Poly Haven · 버튼·효과음: Kenney (모두 CC0)", 22, TextAnchor.MiddleCenter, UiKit.Muted), 70);
            UiKit.Height(UiKit.Button(card, "닫기", CloseOverlay, UiKit.Muted), 110);
        }

        void ConfirmReset()
        {
            var card = OpenOverlay("정말 지울까요?");
            UiKit.Height(UiKit.Label(card, "모든 슬롯의 선수, 내 팀 은퇴 선수, 업적, 도감이 모두 사라져요.\n되돌릴 수 없어요.", 32, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "지우기", () =>
            {
                SaveStore.DeleteAll();
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                // Nothing of the old game may be saved on the way out.
                _retired = true;
                _club = new Club();
                TitleScreen.Show();
                Destroy(gameObject);
            }, UiKit.Warning), 110);
            UiKit.Height(UiKit.Button(card, "취소", ShowSettings, UiKit.Muted), 110);
        }

        /// <summary>Back to the slot screen to play another player; this one is saved where it stands.</summary>
        void ConfirmSwitchPlayer()
        {
            // Not over a decision card (season end, offers, a big at bat); the new-player card may leave.
            if (_career == null) { BackToTitle(); return; }
            if (_overlay != null) return;
            var card = OpenOverlay("다른 선수 키우기");
            UiKit.Height(UiKit.Label(card, $"{_career.Me.Name} 선수는 지금 상태 그대로 슬롯 {SaveStore.CurrentSlot}에 저장돼요.\n" +
                "슬롯 화면에서 다른 슬롯을 고르거나 새 선수를 만들 수 있어요.\n언제든 다시 돌아와서 이어 할 수 있어요.", 32, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "슬롯 화면으로", BackToTitle, UiKit.Accent), 110);
            UiKit.Height(UiKit.Button(card, "취소", CloseOverlay, UiKit.Muted), 110);
        }

        void BackToTitle()
        {
            Save();
            // Nothing more may be saved from this screen once the title is up.
            _retired = true;
            _career = null;
            TitleScreen.Show();
            Destroy(gameObject);
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
            // Swapping one popup for the next (or redrawing it) shouldn't replay the entrance.
            bool animate = _overlay == null;
            CloseOverlay();
            _overlay = UiKit.Dim(GetComponentInChildren<Canvas>().transform, animate: animate);
            var card = UiKit.PopupCard(_overlay, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), animate);
            UiKit.Column(card, 40, 20);
            var heading = UiKit.Label(card, title, 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);
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
                string bonus = string.Join(" ", next.Bonus.Select(b => $"{StatNames[_career.Me.RoleStat(b.Key)]}+{b.Value}"));
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
            var pitching = _career.SeasonPitching;
            bool pitcher = _career.Me.IsPitcher;
            var summary = _career.EndSeason();
            var card = OpenOverlay($"{CareerState.YearOf(summary.Record.Season)} 시즌 종료");
            var body = UiKit.ScrollList(card, 6);
            UiKit.Height(body.parent, 0, 1);
            void Section(string title) => UiKit.Height(UiKit.Label(body, title, 32, TextAnchor.LowerLeft, Gold), 70);
            void Line(string text, int height = 50) => UiKit.Height(UiKit.Label(body, text, 30, TextAnchor.MiddleLeft), height);

            Section("정규시즌");
            Line($"팀 순위 {summary.Rank}위");
            Line(pitcher
                ? pitching.ToString().Replace(" · ", "  ") + (pitching.BlownSaves > 0 ? $"  블론 {pitching.BlownSaves}" : "")
                : $"타율 {Stats.Rate(Stats.Avg(line))}  {line.HomeRuns}홈런  {line.RunsBattedIn}타점  OPS {Stats.Rate(Stats.Ops(line))}");
            Line($"인기 {summary.Popularity}  ·  광고 수입 {summary.Sponsor:N0}만원");

            var post = summary.Postseason;
            Section("가을야구");
            foreach (var series in post.Series)
                Line($"{series.Name}  {series.High.Name} {series.HighWins} : {series.LowWins} {series.Low.Name}");
            if (post.MyTeamWon) Line("<color=#FFD24D><b>우승!</b></color>  " + MyPostLine(post), 60);
            else if (post.MyTeamIn) Line($"{post.Champion.Name} 우승  ·  " + MyPostLine(post));
            else Line($"{post.Champion.Name} 우승  ·  {_career.MyTeam.Name}은 진출하지 못했습니다.");

            if (summary.RivalLine != null)
            {
                var rl = summary.RivalLine;
                Section($"라이벌 {_career.Rival.Name} ({summary.RivalTeam?.Name})");
                if (!pitcher) Line($"나     타율 {Stats.Rate(Stats.Avg(line))}  {line.HomeRuns}홈런  OPS {Stats.Rate(Stats.Ops(line))}");
                Line($"라이벌 타율 {Stats.Rate(Stats.Avg(rl))}  {rl.HomeRuns}홈런  OPS {Stats.Rate(Stats.Ops(rl))}" + (summary.RivalTitles.Count > 0 ? $"  ·  <color=#E0475B>{string.Join(", ", summary.RivalTitles)}</color>" : ""));
                double mine = pitcher ? CareerState.PitchingForm(pitching) : Stats.Ops(line);
                Line(mine >= Stats.Ops(rl) ? "<color=#5FA8FF>이번 시즌은 내가 이겼어요!</color>" : "<color=#F28C40>이번 시즌은 라이벌이 앞섰어요.</color>");
            }

            Section("시즌 타이틀");
            foreach (var a in summary.Awards.Where(a => a.Title != Awards.GoldenGlove))
            {
                string text = $"{a.Title}  {a.Winner} ({a.Team})  {a.Value}";
                Line(a.Mine ? $"<color=#FFD24D><b>{text}</b></color>" : text);
            }
            var gloves = summary.Awards.Where(a => a.Title == Awards.GoldenGlove).ToList();
            if (gloves.Count > 0)
            {
                Line($"{Awards.GoldenGlove}", 44);
                foreach (var group in gloves.GroupBy(g => g.Position))
                    Line($"  {Positions.Name(group.Key)}  " + string.Join(", ", group.Select(g => g.Mine ? $"<color=#FFD24D><b>{g.Winner}</b></color>" : $"{g.Winner} ({g.Team})")), 44);
            }

            if (summary.Goal != null)
            {
                Section("시즌 목표");
                Line(summary.GoalMet
                    ? $"<color=#FFD24D>달성!</color> {summary.Goal.Text}  ·  +{summary.GoalMoney:N0}만원, 모든 능력치 경험치 +{summary.Goal.Exp}"
                    : $"<color=#F28C40>실패</color> {summary.Goal.Text}  (기록 {summary.Goal.Progress(line, pitching)})");
            }

            if (summary.Enlisted)
            {
                Section("병역");
                Line($"{CareerState.EnlistByAge}세가 되어 입대해요. 다음 시즌은 {CareerState.MilitaryClub}에서 보내요.");
                Line("<color=#9EABC2>연봉 없이 2군 수준 경기를 뛰고, 훈련과 성장은 계속돼요. 계약과 FA는 제대 후로 미뤄져요.</color>", 70);
            }
            else if (summary.CanEnlist)
            {
                int chances = Enumerable.Range(_career.Year, CareerState.EnlistByAge - _career.Me.Age).Count(NationalTeam.IsTournamentYear);
                Section("병역");
                Line($"{CareerState.EnlistByAge}세 전에 군 복무를 해야 해요. 남은 국제대회 {chances}번 (금메달이면 면제)");
                UiKit.Height(UiKit.Button(body, $"다음 시즌 {CareerState.MilitaryClub} 입대", () =>
                {
                    _career.Enlist();
                    Log($"<color=#FFD24D>입대 결정</color> 다음 시즌은 {CareerState.MilitaryClub}에서 뛰어요.");
                    Save();
                    ShowSeasonEndAfterEnlist();
                }, UiKit.Muted), 100);
            }

            Section("계약");
            if (summary.SalaryTalk != null)
                Line($"구단 제시 연봉 {summary.OldSalary:N0} → <b>{summary.SalaryTalk.Offer:N0}</b>만원  (성적 시세 {summary.SalaryTalk.Worth:N0}만원)");
            else
                Line($"연봉 {summary.OldSalary:N0} → <b>{summary.NewSalary:N0}</b>만원" + (_career.ContractSeasonsLeft > 0 ? "  (계약 기간)" : ""));
            if (post.Bonus > 0) Line($"가을야구 보너스 {post.Bonus:N0}만원");
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
            var record = summary.Record;
            UiKit.Height(UiKit.Button(card, "이번 시즌 선수 카드", () => CareerCard.Show(_canvas.transform, SeasonCard(record)), UiKit.Muted), 100);
            if (_career.Me.Age >= 30 || _career.MustRetire)
                UiKit.Height(UiKit.Button(card, "은퇴하고 내 팀에 영입", Retire, UiKit.Warning), 100);
            if (!_career.MustRetire && !mustPick && _career.PendingSalary != null)
                UiKit.Height(UiKit.Button(card, "연봉 협상하기", ShowSalaryTalk), 100);
            else if (!_career.MustRetire && !mustPick)
                UiKit.Height(UiKit.Button(card, "다음 시즌", CloseOverlay), 100);
        }

        /// <summary>Accept the club's number or push for more; the odds depend on how the season compares to the offer.</summary>
        void ShowSalaryTalk()
        {
            var talk = _career.PendingSalary;
            if (talk == null) { CloseOverlay(); return; }
            var card = OpenOverlay("연봉 협상");
            string leverage = talk.Worth > talk.Offer ? "<color=#5FA8FF>성적이 제시액보다 좋아요. 협상력이 있어요.</color>"
                : "<color=#F28C40>성적이 제시액만 못해요. 무리하면 마음만 상할 수 있어요.</color>";
            UiKit.Height(UiKit.Label(card,
                $"작년 연봉 {talk.OldSalary:N0}만원\n구단 제시 <b>{talk.Offer:N0}</b>만원\n성적 시세 {talk.Worth:N0}만원" + (talk.Titles > 0 ? $" · 타이틀 {talk.Titles}개" : "") + $"\n\n{leverage}",
                34, TextAnchor.MiddleCenter), 0, 1);
            void Choice(string label, SalaryAsk ask, Color? color = null) =>
                UiKit.Height(UiKit.Button(card, ask == SalaryAsk.Accept ? label : $"{label} {talk.Ask(ask):N0}만원  (성공 {talk.Chance(ask):P0})", () =>
                {
                    var result = _career.Negotiate(ask);
                    Log($"<b>연봉 협상</b> {result.Text}");
                    Save();
                    Refresh();
                    var done = OpenOverlay(result.Granted ? "협상 완료" : "협상 결렬");
                    UiKit.Height(UiKit.Label(done, result.Text + (result.MoraleChange < 0 ? $"\n\n<color=#F28C40>사기 {result.MoraleChange}</color>" : ""), 36, TextAnchor.MiddleCenter), 0, 1);
                    UiKit.Height(UiKit.Button(done, "다음 시즌", CloseOverlay), 110);
                }, color), 110);
            Choice("제시액 수락", SalaryAsk.Accept);
            Choice("소폭 인상 요구", SalaryAsk.AskSome, UiKit.Muted);
            Choice("대폭 인상 요구", SalaryAsk.AskBig, UiKit.Warning);
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

        void ShowSeasonEndAfterEnlist()
        {
            var card = OpenOverlay("입대");
            UiKit.Height(UiKit.Label(card, $"다음 시즌은 {CareerState.MilitaryClub}에서 보내요.\n연봉은 없지만 훈련과 성장은 계속돼요.\n제대하면 원래 팀으로 돌아와요.", 36, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "다음 시즌", CloseOverlay), 110);
        }

        string MyPostLine(PostseasonResult post)
        {
            var l = post.MyLine;
            if (_career.Me.IsPitcher)
            {
                var p = post.MyPitching;
                if (p.Games == 0) return "등판하지 못했어요";
                return _career.Me.Role == PitcherRole.Starter ? $"내 성적 {p.Starts}경기 {p.Wins}승 {p.Losses}패 평균자책 {p.EraText}"
                    : $"내 성적 {p.Games}경기 {p.Saves}세이브 {p.Holds}홀드 평균자책 {p.EraText}";
            }
            return l.PlateAppearances == 0 ? "출전하지 못했어요" : $"내 성적 {l.AtBats}타수 {l.Hits}안타 {l.HomeRuns}홈런";
        }

        void Retire()
        {
            var retired = _club.Retire(_career);
            _retired = true;
            Save();
            var card = OpenOverlay(retired.HallOfFame ? "은퇴 · 명예의 전당 헌액" : "은퇴");
            var t = retired.CareerLine;
            string honours = retired.HallOfFame
                ? $"<color=#FFD24D>★ 명예의 전당 헌액 (점수 {retired.HallOfFameScore})</color>\n" +
                  (retired.NumberRetiredBy != null ? $"<color=#FFD24D>{retired.NumberRetiredBy}가 등번호를 영구결번했습니다.</color>\n" : "")
                : $"<color=#9EABC2>명예의 전당 점수 {retired.HallOfFameScore} / {Club.HallOfFameLine}</color>\n";
            string donation = retired.Donation > 0
                ? $"남은 돈 {retired.Donation:N0}만원을 유소년 야구에 기부 → 명예 포인트 +{retired.DonationPoints}\n"
                : "";
            var arm = retired.CareerPitching;
            string totals = retired.IsPitcher
                ? (retired.Role == PitcherRole.Starter ? $"통산 {arm.Wins}승 {arm.Losses}패" : $"통산 {arm.Games}경기 {arm.Saves}세이브 {arm.Holds}홀드") + $" 평균자책 {arm.EraText} {arm.Strikeouts}탈삼진"
                : $"통산 {t.Hits}안타 {t.HomeRuns}홈런";
            string text =
                $"<i>{string.Join(" ", retired.Ceremony)}</i>\n\n" +
                $"{retired.Name}, {retired.Seasons}시즌 {totals} · 올스타 {retired.AllStars}회\n\n" +
                honours + "\n" +
                $"전성기 능력치 종합 {retired.Overall}로 내 팀에 합류했습니다.\n" +
                donation +
                $"명예 포인트 +{retired.LegacyPoints + retired.DonationPoints} (다음 선수 잠재력 +{_club.PotentialBonus})\n\n" +
                $"내 팀 선수 {_club.Roster.Count}명";
            UiKit.Height(UiKit.Label(card, text, 30, TextAnchor.UpperCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "커리어 카드 보기", () => CareerCard.Show(_canvas.transform, RetiredCard(retired)), UiKit.Muted), 100);
            UiKit.Height(UiKit.Button(card, "새 선수 육성 시작", ShowNewPlayer), 110);
        }

        // ---------- Player cards ----------

        CareerCard.Data SeasonCard(SeasonRecord record)
        {
            var me = _career.Me;
            var data = new CareerCard.Data
            {
                Name = me.Name,
                Number = me.Number,
                Position = me.IsPitcher ? $"{Positions.RoleName(me.Role)} 투수" : Positions.Name(me.Position),
                Team = record.Club ?? _career.MyTeam.Name,
                Subtitle = $"{CareerState.YearOf(record.Season)} 시즌 · {record.Age}세",
                Titles = new List<string>(record.Titles),
                FileName = $"{me.Name}_{CareerState.YearOf(record.Season)}",
            };
            foreach (var stat in me.MyStats) data.Ratings[stat] = record.Ratings != null && record.Ratings.TryGetValue(stat, out int v) ? v : me.Ratings[stat];
            if (record.Pitching != null)
            {
                var p = record.Pitching;
                data.Lines.Add(me.Role == PitcherRole.Starter ? $"{p.Wins}승 {p.Losses}패  평균자책 {p.EraText}" : $"{p.Saves}세이브 {p.Holds}홀드  평균자책 {p.EraText}");
                data.Lines.Add($"{p.Innings}이닝  탈삼진 {p.Strikeouts}  WHIP {(p.Outs == 0 ? "-" : p.Whip.ToString("0.00"))}");
            }
            else
            {
                var l = record.Line;
                string avg = l.AtBats == 0 ? ".000" : ((double)l.Hits / l.AtBats).ToString(".000");
                data.Lines.Add($"타율 {avg}  {l.HomeRuns}홈런  {l.RunsBattedIn}타점");
                data.Lines.Add($"{l.Hits}안타  OPS {CareerState.Ops(l):0.000}  연봉 {record.Salary:N0}만원");
            }
            return data;
        }

        static CareerCard.Data RetiredCard(RetiredPlayer retired)
        {
            var data = new CareerCard.Data
            {
                Name = retired.Name,
                Number = retired.ShownNumber,
                Position = retired.IsPitcher ? $"{Positions.RoleName(retired.Role)} 투수" : Positions.Name(retired.Position),
                Team = retired.MainClub ?? "",
                Subtitle = $"통산 {retired.Seasons}시즌 · 올스타 {retired.AllStars}회",
                Banner = retired.HallOfFame ? "★ 명예의 전당 ★" : null,
                FileName = $"{retired.Name}_커리어",
            };
            foreach (var pair in retired.PeakRatings) data.Ratings[pair.Key] = pair.Value;
            if (retired.IsPitcher)
            {
                var a = retired.CareerPitching;
                data.Lines.Add($"통산 {a.Wins}승 {a.Losses}패 {a.Saves}세이브 {a.Holds}홀드");
                data.Lines.Add($"평균자책 {a.EraText}  탈삼진 {a.Strikeouts}");
            }
            else
            {
                var t = retired.CareerLine;
                string avg = t.AtBats == 0 ? ".000" : ((double)t.Hits / t.AtBats).ToString(".000");
                data.Lines.Add($"통산 타율 {avg}  {t.Hits}안타  {t.HomeRuns}홈런");
                data.Lines.Add($"{t.RunsBattedIn}타점  OPS {CareerState.Ops(t):0.000}");
            }
            // Titles repeat across seasons: "홈런왕 ×3".
            foreach (var g in retired.Titles.GroupBy(x => x).OrderByDescending(g => g.Count()))
                data.Titles.Add(g.Count() > 1 ? $"{g.Key} ×{g.Count()}" : g.Key);
            return data;
        }

        /// <summary>Name the next player and pick a season length. Shown on first launch and after each retirement.</summary>
        void ShowNewPlayer()
        {
            bool first = _club.Roster.Count == 0;
            var card = OpenOverlay(first ? "프로야구 선수 키우기" : "새 선수 등록");

            string intro = first
                ? "18세 신인을 프로 1군에 입단시켜 은퇴할 때까지 키워 보세요.\n은퇴한 선수는 내 팀에 남습니다."
                : $"내 팀 선수 {_club.Roster.Count}명 · 명예 포인트 덕분에 새 선수 잠재력 +{_club.PotentialBonus}";
            UiKit.Height(UiKit.Label(card, intro, 30, TextAnchor.MiddleCenter, UiKit.Muted), 110);

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
            UiKit.Height(teams, 180);
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
                var emblem = ArtLibrary.TeamEmblem(League.DefaultTeamNames[i]);
                if (emblem != null)
                {
                    UiKit.ButtonIcon(b, emblem);
                    UiKit.SetButtonLabel(b, "");
                }
                teamButtons.Add(b);
            }
            ShowTeam();

            // Position, once and for good: a pitcher is a different career from a hitter.
            var position = Position.Outfield;
            var positionLabel = UiKit.Label(card, "", 30, TextAnchor.LowerLeft);
            UiKit.Height(positionLabel, 60);
            var positions = UiKit.Empty(card, "Positions");
            UiKit.Height(positions, 180);
            var posGrid = positions.gameObject.AddComponent<GridLayoutGroup>();
            posGrid.cellSize = new Vector2(210, 80);
            posGrid.spacing = new Vector2(10, 10);
            posGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            posGrid.constraintCount = 4;
            posGrid.childAlignment = TextAnchor.MiddleCenter;
            var positionButtons = new List<(Position Position, Button Button)>();
            var role = PitcherRole.Starter;
            var roleRow = UiKit.Empty(card, "Roles");
            UiKit.Height(roleRow, 90);
            UiKit.Row(roleRow, 12);
            var roleButtons = new List<(PitcherRole Role, Button Button)>();
            void ShowPosition()
            {
                roleRow.gameObject.SetActive(position == Position.Pitcher);
                foreach (var (r, b) in roleButtons) b.GetComponent<Image>().color = r == role ? UiKit.Accent : UiKit.Background;
                string what = position == Position.Pitcher
                    ? role == PitcherRole.Starter ? "5경기마다 선발 등판, 구속·제구·변화구·체력을 키워요"
                    : role == PitcherRole.Closer ? "9회 세이브 상황에 나와 경기를 끝내요"
                    : "8회 접전에 나와 리드를 지키고 홀드를 쌓아요"
                    : position == Position.Catcher ? "수비 부담이 커서 더 지치지만 골든글러브에서 수비가 크게 쳐줘요"
                    : position == Position.DesignatedHitter ? "수비를 안 해서 덜 지치지만 수비 능력은 쓰이지 않아요"
                    : "타격·주루·수비 다섯 능력치를 키워요";
                positionLabel.text = $"포지션: <b>{Positions.Name(position)}</b> <size=24><color=#9EABC2>{what} (바꿀 수 없어요)</color></size>";
                foreach (var (pos, b) in positionButtons) b.GetComponent<Image>().color = pos == position ? UiKit.Accent : UiKit.Background;
            }
            foreach (var pos in new[] { Position.Pitcher }.Concat(Positions.Fielders))
            {
                var p = pos;
                var b = UiKit.Button(positions, Positions.Name(p), () => { position = p; ShowPosition(); });
                b.GetComponentInChildren<Text>().fontSize = 28;
                positionButtons.Add((p, b));
            }
            // The pitcher's job, under the positions; the row is shown for a pitcher only.
            roleRow.SetSiblingIndex(positions.GetSiblingIndex() + 1);
            foreach (var r in new[] { PitcherRole.Starter, PitcherRole.Setup, PitcherRole.Closer })
            {
                var pr = r;
                var b = UiKit.Button(roleRow, $"{Positions.RoleName(pr)} 투수", () => { role = pr; ShowPosition(); });
                b.GetComponentInChildren<Text>().fontSize = 28;
                roleButtons.Add((pr, b));
            }
            ShowPosition();

            int number = 1 + (int)(_random.NextDouble() * 99);
            var numberRow = UiKit.Empty(card, "Number");
            UiKit.Height(numberRow, 90);
            UiKit.Row(numberRow, 12).childForceExpandWidth = false;
            var numberLabel = UiKit.Label(numberRow, "", 30, TextAnchor.MiddleLeft);
            UiKit.Width(numberLabel, 0, 1);
            void ShowNumber() => numberLabel.text = $"등번호 <b>{number}</b>번";
            foreach (int step in new[] { -10, -1, 1, 10 })
            {
                int d = step;
                var b = UiKit.Button(numberRow, d > 0 ? $"+{d}" : $"{d}", () => { number = (number - 1 + d + 99) % 99 + 1; ShowNumber(); }, UiKit.Background);
                b.GetComponentInChildren<Text>().fontSize = 28;
                UiKit.Width(b, 120);
            }
            ShowNumber();

            UiKit.Height(UiKit.Label(card, "시즌 길이 (짧을수록 하루 성장이 커요. 시즌 사이에 바꿀 수 있어요)", 30, TextAnchor.LowerLeft), 70);
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
                StartNewCareer(name, games, team, position, number, role);
                CloseOverlay();
                _logLines.Clear();
                Log($"{CareerState.StartYear}년, {_career.Me.Name} 선수 {_career.MyTeam.Name} 입단! 훈련을 골라 하루를 시작하세요.");
                Tip("start", "훈련을 하나 고르면 하루가 지나고 팀 경기가 열려요. 경험치 막대가 다 차면 능력치가 1 올라요. 잠재력(/ 뒤 숫자)까지 키울 수 있어요.");
                Refresh();
                CheckIn();
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
            TellNotifications();
            var me = _career.Me;
            var rank = _career.League.Ranked().FindIndex(s => s.Team == _career.MyTeam) + 1;
            string role = me.IsPitcher && me.Role != PitcherRole.Starter
                ? (_career.CanPitchToday ? " · 불펜 대기" : " · <color=#F28C40>휴식 (피로)</color>")
                : me.IsPitcher
                ? (_career.IsStartDay ? " · <color=#FFD24D>오늘 선발</color>" : $" · 선발 D-{_career.DaysToNextStart}")
                : $" · {CareerState.SlotName(_career.LineupSlot)} 타자";
            string number = me.Number > 0 ? $"#{me.Number} " : "";
            string position = me.IsPitcher ? $"{Positions.RoleName(me.Role)} 투수" : Positions.Name(me.Position);
            if (_emblemTeam != _career.MyTeam.Name)
            {
                _emblemTeam = _career.MyTeam.Name;
                ArtLibrary.SetEmblem(_emblem, _emblemTeam);
                _headerBand.color = UiKit.Shade(TeamColor(_emblemTeam), 0.55f) + new Color(0.05f, 0.05f, 0.08f, 0);
            }
            _header.text = $"<b>{me.Name}</b> <size=26><color=#5FA8FF>프로필 ›</color></size>  {number}{position} · {me.Age}세 · {_career.MyTeam.Name}\n" +
                $"<size=32><color=#9EABC2>{_career.Year} 시즌 {_career.Day + 1}/{_career.League.GamesPerSeason}일 · 팀 {rank}위" + (_career.InMilitary ? " · <color=#9EABC2>군 복무 중</color>" : _career.InFarm ? " · <color=#F28C40>2군</color>" : role) +
                (_career.ContractSeasonsLeft > 0 ? $" · 계약 {_career.ContractSeasonsLeft}년" : "") + "</color></size>";

            var l = _career.SeasonLine;
            string avg = l.AtBats == 0 ? ".000" : ((double)l.Hits / l.AtBats).ToString(".000");
            string injury = me.IsInjured ? $"  <color=#F28C40>{Injuries.Name(me.Injury)} {me.InjuryDaysLeft}일</color>"
                : me.LingerDays > 0 ? $"  <color=#F28C40>후유증 {me.LingerDays}일</color>" : "";
            string morale = me.Morale > 0 ? $"  <color=#5FA8FF>사기 +{me.Morale}</color>" : me.Morale < 0 ? $"  <color=#F28C40>사기 {me.Morale}</color>" : "";
            var next = Traits.NextMilestone(me);
            string traits = me.Traits.Count == 0 ? "특성 없음" : "특성 " + string.Join(" · ", me.Traits.Select(id => Traits.Find(id)?.Name));
            if (_career.Goal != null) traits = $"목표 {_career.Goal.Text} ({_career.Goal.Progress(_career)})  ·  " + traits;
            var p = _career.SeasonPitching;
            string season = !me.IsPitcher ? $"타율 {avg}  {l.HomeRuns}홈런" + (l.StolenBases > 0 ? $"  {l.StolenBases}도루" : "")
                : me.Role == PitcherRole.Closer ? $"{p.Saves}세이브 평균자책 {p.EraText}"
                : me.Role == PitcherRole.Setup ? $"{p.Holds}홀드 평균자책 {p.EraText}"
                : $"{p.Wins}승 {p.Losses}패 평균자책 {p.EraText}";
            _money.text = $"{me.Money:N0}만원  <size=26><color=#9EABC2>연봉 {me.Salary:N0}만원</color></size>";
            _status.text = $"{season}  ·  인기 {me.Popularity}{injury}{morale}\n" +
                $"<size=24><color=#9EABC2>{traits}" + (next != null ? $"  (다음 특성: 능력치 총합 {Traits.RatingTotal(me)}/{next})" : "") + "</color></size>";
            UiKit.SetBar(_fatigueFill, me.Fatigue / 100f);
            _condition.text = ConditionText(_career.ConditionStars);

            foreach (var row in _ratingRows) row.Value.SetActive(me.MyStats.Contains(row.Key));
            foreach (var stat in me.MyStats)
            {
                int bonus = me.EffectiveRating(stat) - me.Ratings[stat];
                string gear = bonus > 0 ? $"<color=#5FA8FF>+{bonus}</color>" : "";
                bool maxed = me.Ratings[stat] >= me.Potential[stat];
                bool canBreak = maxed && me.BreakthroughCount(stat) < CareerPlayer.MaxBreakthroughs && me.Potential[stat] < 99;
                string limit = canBreak ? $" <color=#F28C40>돌파 {me.BreakthroughCount(stat)}/{CareerPlayer.MaxBreakthroughs}</color>" : "";
                // A rating that went up since the last refresh swells for a moment.
                if (_shownRatings.TryGetValue(stat, out int shown) && me.Ratings[stat] > shown) UiAnim.Pulse(_ratingTexts[stat]);
                _shownRatings[stat] = me.Ratings[stat];
                _ratingTexts[stat].text = $"<b>{me.Ratings[stat]}</b>{gear} <size=24><color=#9EABC2>/ {me.Potential[stat]}</color>{limit}</size>";
                // At the limit the bar shows progress toward the next breakthrough instead.
                UiKit.SetBar(_expFills[stat], canBreak
                    ? (float)me.BreakthroughProgress(stat) / me.BreakthroughNeed(stat)
                    : maxed ? 1f : (float)me.Exp[stat] / CareerPlayer.ExpToNext(me.Ratings[stat]));
            }

            var choices = _career.TrainingChoices;
            for (int i = 0; i < _actionButtons.Count; i++)
            {
                var b = _actionButtons[i];
                b.gameObject.SetActive(choices.Contains(Actions[i].Action));
                b.interactable = !me.IsInjured || Actions[i].Action == DailyAction.Rest;
            }
            _seasonLengthLabel.text = _career.Day == 0
                ? $"{_career.League.GamesPerSeason}경기 (변경)"
                : $"{_career.League.GamesPerSeason}경기 시즌";
            _seasonLengthButton.interactable = _career.Day == 0;
            var today = _career.TodaysGame();
            _todayText.text = (today == null ? "" : $"오늘 경기  vs {today.Value.Opponent.Name} ({(today.Value.Home ? "홈" : "원정")})  ") + "<color=#5FA8FF>일정 ›</color>";
            int adsLeft = Monetization.AdsLeftToday;
            UiKit.SetButtonLabel(_adButton, $"{(Premium.Owns(Premium.RemoveAds) ? "바로 회복" : "광고 보고 회복")} ({adsLeft})");
            _adButton.interactable = adsLeft > 0 && me.Fatigue > 0;
            int tickets = _club.Daily.RecoveryTickets;
            UiKit.SetButtonLabel(_ticketButton, $"회복권 {tickets}장");
            _ticketButton.interactable = tickets > 0 && me.Fatigue > 0;
            int unclaimed = _club.Daily.Unclaimed;
            UiKit.SetButtonLabel(_missionButton, unclaimed > 0 ? $"미션 <color=#FFD24D>●{unclaimed}</color>" : "미션");
            bool canFast = Premium.Owns(Premium.FastSkip);
            _fastButton.gameObject.SetActive(canFast);
            if (!canFast) _fast = false;
            UiKit.SetButtonLabel(_fastButton, _fast ? $"빠른 진행 {Premium.FastSkipDays}일: 켜짐" : "빠른 진행: 꺼짐");
            _fastButton.GetComponent<Image>().color = _fast ? UiKit.Warning : UiKit.Card;
            _watchButton.interactable = _lastDay?.MyGame != null;
        }
    }
}
