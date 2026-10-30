using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>League standings, my season and career numbers, and the retired players on my team.</summary>
    public class RecordsScreen : MonoBehaviour
    {
        enum Tab { Standings, MyRecord, Achievements, MyTeam, News }

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Highlight = new Color(0.25f, 0.55f, 0.95f, 0.25f);
        static readonly Color Stripe = new Color(1, 1, 1, 0.04f);

        CareerState _career;
        Club _club;
        RectTransform _content;
        GameObject _canvas;
        ChallengeResult _lastChallenge;
        readonly Dictionary<Tab, Button> _tabs = new Dictionary<Tab, Button>();

        public static void Open(CareerState career, Club club)
        {
            var screen = new GameObject("RecordsScreen").AddComponent<RecordsScreen>();
            screen._career = career;
            screen._club = club;
            screen.Build();
            screen.Show(Tab.Standings);
        }

        void Build()
        {
            var canvasGo = new GameObject("RecordsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo;
            UiAnim.FadeIn(canvasGo.transform, 0.2f);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var root = UiKit.SafeRoot(canvasGo.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);
            UiKit.Backdrop(root, ArtLibrary.Background("records"));

            UiKit.Height(UiKit.Label(root, "기록", 48), 80);

            var tabs = UiKit.Empty(root, "Tabs");
            UiKit.Height(tabs, 100);
            UiKit.Row(tabs, 16);
            _tabs[Tab.Standings] = UiKit.Button(tabs, "리그 순위", () => Show(Tab.Standings), UiKit.Card);
            _tabs[Tab.MyRecord] = UiKit.Button(tabs, "내 기록", () => Show(Tab.MyRecord), UiKit.Card);
            _tabs[Tab.Achievements] = UiKit.Button(tabs, "업적", () => Show(Tab.Achievements), UiKit.Card);
            _tabs[Tab.MyTeam] = UiKit.Button(tabs, "내 팀", () => Show(Tab.MyTeam), UiKit.Card);
            _tabs[Tab.News] = UiKit.Button(tabs, "뉴스", () => Show(Tab.News), UiKit.Card);
            foreach (var tab in _tabs.Values) tab.GetComponentInChildren<Text>().fontSize = 28;

            var card = UiKit.Panel(root, "Card", UiKit.Card);
            UiKit.Height(card, 0, 1);
            UiKit.Column(card, 20, 0);
            var scroll = UiKit.ScrollList(card, 4);
            UiKit.Height(scroll.parent, 0, 1);
            _content = scroll;

            UiKit.Height(UiKit.Button(root, "닫기", () => Destroy(gameObject), UiKit.Muted), 110);
        }

        void Show(Tab tab)
        {
            foreach (var pair in _tabs) pair.Value.GetComponent<Image>().color = pair.Key == tab ? UiKit.Accent : UiKit.Background;
            foreach (Transform child in _content) Destroy(child.gameObject);
            switch (tab)
            {
                case Tab.Standings: ShowStandings(); break;
                case Tab.MyRecord: ShowMyRecord(); break;
                case Tab.Achievements: ShowAchievements(); break;
                case Tab.News: ShowNews(); break;
                default: ShowMyTeam(); break;
            }
        }

        // ---------- Tabs ----------

        void ShowStandings()
        {
            var league = _career.League;
            Section($"{_career.Year} 시즌 {_career.Day}/{league.GamesPerSeason}경기");
            float[] widths = { 0, 100, 100, 100, 150, 130 };
            Header(widths, "팀", "승", "패", "무", "승률", "승차");
            var ranked = league.Ranked();
            for (int i = 0; i < ranked.Count; i++)
            {
                var s = ranked[i];
                double gb = league.GamesBehind(s);
                bool mine = s.Team == _career.MyTeam;
                Row(widths, i, mine, $"{i + 1}  {s.Team.Name}", s.Wins.ToString(), s.Losses.ToString(), s.Ties.ToString(),
                    Stats.Rate(s.WinPct), gb == 0 ? "-" : gb.ToString("0.#"));
            }
        }

        void ShowMyRecord()
        {
            if (_career.Me.IsPitcher) ShowMyPitching();
            else ShowMyBatting();
            ShowGrowth();
            ShowMoves();
        }

        static readonly Color[] StatColors =
        {
            new Color(0.35f, 0.65f, 1f), new Color(1f, 0.45f, 0.4f), new Color(0.45f, 0.85f, 0.5f),
            new Color(1f, 0.8f, 0.3f), new Color(0.75f, 0.55f, 1f),
        };

        /// <summary>Ratings and results season by season, ending with where the player stands now.</summary>
        void ShowGrowth()
        {
            var me = _career.Me;
            Section("성장 그래프");
            if (me.History.Count == 0)
            {
                Note("첫 시즌이 끝나면 시즌별 능력치 변화가 그래프로 나와요.");
                return;
            }
            // Show at most the last 12 seasons so the points stay readable.
            var history = me.History.Skip(Mathf.Max(0, me.History.Count - 12)).ToList();
            var labels = history.Select(h => $"{h.Age}세").Concat(new[] { "지금" }).ToList();

            var stats = me.MyStats.ToList();
            var series = new List<LineChart.Series>();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                var line = new LineChart.Series { Name = Stats.Name(stat), Color = StatColors[i % StatColors.Length] };
                foreach (var h in history) line.Values.Add(h.Ratings != null && h.Ratings.TryGetValue(stat, out int v) ? v : me.Ratings[stat]);
                line.Values.Add(me.Ratings[stat]);
                series.Add(line);
            }
            float low = Mathf.Floor(series.Min(l => l.Values.Min()) / 10f) * 10;
            LineChart.Create(_content, 460, Mathf.Min(low, 60), 100, labels, series.ToArray());

            if (me.IsPitcher)
            {
                var era = new LineChart.Series { Name = "평균자책", Color = new Color(1f, 0.55f, 0.3f) };
                foreach (var h in history) era.Values.Add((float)(h.Pitching?.Era ?? 0));
                era.Values.Add((float)_career.SeasonPitching.Era);
                Note("평균자책은 낮을수록 좋아요.", TextAnchor.MiddleLeft, 50);
                LineChart.Create(_content, 340, 0, Mathf.Max(6, Mathf.Ceil(era.Values.Max())), labels, era);
            }
            else
            {
                var ops = new LineChart.Series { Name = "OPS", Color = Gold };
                foreach (var h in history) ops.Values.Add((float)CareerState.Ops(h.Line));
                ops.Values.Add((float)CareerState.Ops(_career.SeasonLine));
                LineChart.Create(_content, 340, 0.4f, Mathf.Max(1.2f, ops.Values.Max() + 0.05f), labels, ops);
            }
        }

        static readonly Dictionary<NewsKind, string> NewsColors = new Dictionary<NewsKind, string>
        {
            { NewsKind.Me, "#FFD24D" }, { NewsKind.Rival, "#F28C40" }, { NewsKind.League, "#FFFFFF" },
        };

        void ShowNews()
        {
            Section(CareerState.PaperName);
            if (_career.News.Count == 0)
            {
                Note("경기를 치르면 하루하루 기사가 실려요.");
                return;
            }
            float[] widths = { 220, 0 };
            for (int i = 0; i < _career.News.Count; i++)
            {
                var n = _career.News[i];
                string when = $"{CareerState.YearOf(n.Season)} · {n.Day}일차";
                Row(widths, i, false, $"<color=#9EABC2>{when}</color>", $"<color={NewsColors[n.Kind]}>{n.Headline}</color>");
            }
        }

        void ShowMyPitching()
        {
            var me = _career.Me;
            var p = _career.SeasonPitching;
            bool relief = me.Role != PitcherRole.Starter;
            Section($"{me.Name}  {_career.Year} 시즌 · {Positions.RoleName(me.Role)} 투수");
            float[] statWidths = { 0, 0, 0, 0, 0, 0 };
            if (relief)
            {
                Header(statWidths, "경기", "세이브", "홀드", "블론", "평균자책", "WHIP");
                Row(statWidths, 0, false, $"{p.Games}", $"{p.Saves}", $"{p.Holds}", $"{p.BlownSaves}", p.EraText, p.Outs == 0 ? "-" : p.Whip.ToString("0.00"));
                Header(statWidths, "이닝", "피안타", "피홈런", "볼넷", "실점", "탈삼진");
                Row(statWidths, 0, false, p.Innings, $"{p.Hits}", $"{p.HomeRuns}", $"{p.Walks}", $"{p.Runs}", $"{p.Strikeouts}");
            }
            else
            {
                Header(statWidths, "승", "패", "평균자책", "이닝", "탈삼진", "WHIP");
                Row(statWidths, 0, false, $"{p.Wins}", $"{p.Losses}", p.EraText, p.Innings, $"{p.Strikeouts}", p.Outs == 0 ? "-" : p.Whip.ToString("0.00"));
                Header(statWidths, "선발", "피안타", "피홈런", "볼넷", "실점", "완봉");
                Row(statWidths, 0, false, $"{p.Starts}", $"{p.Hits}", $"{p.HomeRuns}", $"{p.Walks}", $"{p.Runs}", $"{p.Shutouts}");
            }
            var f = _career.FarmPitching;
            if (f.Outs > 0)
                Note($"2군 {f.Starts}경기  {f.Wins}승 {f.Losses}패  평균자책 {f.EraText}" + (_career.InFarm ? "  · 지금 2군" : ""), TextAnchor.MiddleLeft, 60);
            Note(relief ? (_career.CanPitchToday ? "불펜 대기 중" : "피로가 쌓여 쉬는 중") : $"다음 선발까지 {_career.DaysToNextStart}일", TextAnchor.MiddleLeft, 60);

            Section("시즌별 기록");
            float[] widths = { 0, 100, 110, 140, 160, 130, 170 };
            Header(widths, "연도", "나이", relief ? "세이브-홀드" : "승-패", "평균자책", "이닝", "탈삼진", "연봉");
            if (me.History.Count == 0) Note("첫 시즌이 끝나면 여기에 기록이 쌓여요.");
            for (int i = 0; i < me.History.Count; i++)
            {
                var h = me.History[i];
                var hp = h.Pitching ?? new PitcherLine();
                Row(widths, i, false, $"{CareerState.YearOf(h.Season)}", $"{h.Age}", relief ? $"{hp.Saves}-{hp.Holds}" : $"{hp.Wins}-{hp.Losses}", hp.EraText, hp.Innings, $"{hp.Strikeouts}", $"{h.Salary:N0}");
            }

            var t = Stats.CareerPitching(_career);
            Section("통산 (이번 시즌 포함)");
            if (relief)
            {
                Header(statWidths, "경기", "세이브", "홀드", "평균자책", "이닝", "탈삼진");
                Row(statWidths, 0, true, $"{t.Games}", $"{t.Saves}", $"{t.Holds}", t.EraText, t.Innings, $"{t.Strikeouts}");
            }
            else
            {
                Header(statWidths, "승", "패", "평균자책", "이닝", "탈삼진", "완봉");
                Row(statWidths, 0, true, $"{t.Wins}", $"{t.Losses}", t.EraText, t.Innings, $"{t.Strikeouts}", $"{t.Shutouts}");
            }
        }

        void ShowMyBatting()
        {
            var me = _career.Me;
            var l = _career.SeasonLine;
            Section($"{me.Name}  {_career.Year} 시즌");
            float[] statWidths = { 0, 0, 0, 0, 0, 0 };
            Header(statWidths, "타율", "출루율", "장타율", "OPS", "홈런", "타점");
            Row(statWidths, 0, false, Stats.Rate(Stats.Avg(l)), Stats.Rate(Stats.Obp(l)), Stats.Rate(Stats.Slg(l)),
                Stats.Rate(Stats.Ops(l)), l.HomeRuns.ToString(), l.RunsBattedIn.ToString());
            Header(statWidths, "타석", "안타", "2루타", "3루타", "볼넷", "삼진");
            Row(statWidths, 0, false, l.PlateAppearances.ToString(), l.Hits.ToString(), l.Doubles.ToString(),
                l.Triples.ToString(), l.Walks.ToString(), l.Strikeouts.ToString());
            var f = _career.FarmLine;
            if (f.PlateAppearances > 0)
                Note($"2군 {_career.FarmDays}경기  타율 {Stats.Rate(Stats.Avg(f))}  {f.HomeRuns}홈런  OPS {Stats.Rate(Stats.Ops(f))}" + (_career.InFarm ? "  · 지금 2군" : ""), TextAnchor.MiddleLeft, 60);

            Section("시즌별 기록");
            float[] widths = { 0, 100, 110, 110, 100, 100, 130, 170 };
            Header(widths, "연도", "나이", "경기", "타율", "홈런", "타점", "OPS", "연봉");
            if (me.History.Count == 0) Note("첫 시즌이 끝나면 여기에 기록이 쌓여요.");
            for (int i = 0; i < me.History.Count; i++)
            {
                var h = me.History[i];
                Row(widths, i, false, $"{CareerState.YearOf(h.Season)}", $"{h.Age}", $"{h.Games}", Stats.Rate(Stats.Avg(h.Line)),
                    $"{h.Line.HomeRuns}", $"{h.Line.RunsBattedIn}", Stats.Rate(Stats.Ops(h.Line)), $"{h.Salary:N0}");
            }

            var t = Stats.CareerTotal(_career);
            Section("통산 (이번 시즌 포함)");
            Header(statWidths, "타율", "안타", "홈런", "타점", "OPS", "볼넷");
            Row(statWidths, 0, true, Stats.Rate(Stats.Avg(t)), t.Hits.ToString(), t.HomeRuns.ToString(),
                t.RunsBattedIn.ToString(), Stats.Rate(Stats.Ops(t)), t.Walks.ToString());
        }

        /// <summary>Titles, moves, clubs and the rival: the same for hitters and pitchers.</summary>
        void ShowMoves()
        {
            var me = _career.Me;

            var titled = me.History.Where(h => h.Titles.Count > 0).ToList();
            if (titled.Count > 0)
            {
                Section("수상");
                foreach (var h in titled) Note($"{CareerState.YearOf(h.Season)}  {string.Join(", ", h.Titles)}", TextAnchor.MiddleLeft, 60);
            }

            var moved = me.History.Where(h => h.Notes.Count > 0 || h.FarmDays > 0).ToList();
            if (moved.Count > 0)
            {
                Section("강등·이적 기록");
                foreach (var h in moved)
                {
                    string farm = h.FarmDays > 0 ? $"2군 {h.FarmDays}일" : "";
                    Note($"{CareerState.YearOf(h.Season)}  {string.Join(", ", h.Notes.Concat(new[] { farm }).Where(x => x.Length > 0))}", TextAnchor.MiddleLeft, 60);
                }
            }

            // Consecutive seasons at one club, oldest first; seasons saved before free agency count as the current club.
            var stints = new List<(string Club, int From, int To)>();
            foreach (var h in me.History)
            {
                string club = h.Club ?? _career.MyTeam.Name;
                if (stints.Count > 0 && stints[stints.Count - 1].Club == club)
                    stints[stints.Count - 1] = (club, stints[stints.Count - 1].From, h.Season);
                else stints.Add((club, h.Season, h.Season));
            }
            if (stints.Count == 0 || stints[stints.Count - 1].Club != _career.MyTeam.Name)
                stints.Add((_career.MyTeam.Name, _career.Season, _career.Season));
            else stints[stints.Count - 1] = (_career.MyTeam.Name, stints[stints.Count - 1].From, _career.Season);
            if (stints.Count > 1 || _career.ContractSeasonsLeft > 0 || _career.Me.Clubs.Count > 0)
            {
                Section("소속 구단");
                foreach (var st in stints)
                    Note($"{CareerState.YearOf(st.From)}–{CareerState.YearOf(st.To)}  {st.Club}", TextAnchor.MiddleLeft, 60);
                if (_career.ContractSeasonsLeft > 0)
                    Note($"현재 {_career.MyTeam.Name} 계약 {_career.ContractSeasonsLeft}시즌 남음 · 연 {me.Salary:N0}만원", TextAnchor.MiddleLeft, 60);
            }

            if (_career.Rival != null)
            {
                var rivalYears = me.History.Where(h => h.RivalLine != null).ToList();
                double Mine(SeasonRecord h) => h.Pitching != null ? CareerState.PitchingForm(h.Pitching) : Stats.Ops(h.Line);
                int wins = rivalYears.Count(h => Mine(h) >= Stats.Ops(h.RivalLine));
                var rl = _career.RivalLine;
                Section($"라이벌 {_career.Rival.Name} ({_career.RivalTeam?.Name})");
                Note($"이번 시즌 타율 {Stats.Rate(Stats.Avg(rl))}  {rl.HomeRuns}홈런  OPS {Stats.Rate(Stats.Ops(rl))}", TextAnchor.MiddleLeft, 60);
                if (rivalYears.Count > 0)
                    Note($"시즌 대결 {wins}승 {rivalYears.Count - wins}패", TextAnchor.MiddleLeft, 60);
            }
        }

        void ShowAchievements()
        {
            var collection = _club.Collection;
            var mine = new HashSet<string>(_career.Me.Achievements);
            int collected = Achievements.All.Count(a => mine.Contains(a.Id) || collection.Contains(a.Id));
            Section($"이번 선수 {mine.Count}개  ·  내 팀 전체 {collected}/{Achievements.All.Count}개");
            float[] widths = { 0, 260 };
            Header(widths, "업적", "달성");
            for (int i = 0; i < Achievements.All.Count; i++)
            {
                var a = Achievements.All[i];
                string by = mine.Contains(a.Id) ? _career.Me.Name
                    : collection.Contains(a.Id) ? _club.Roster.First(p => p.Achievements.Contains(a.Id)).Name
                    : "-";
                bool reached = by != "-";
                string name = reached ? $"<b>{a.Name}</b>  <size=24><color=#9EABC2>{a.Description}</color></size>" : $"<color=#5C667A>{a.Name}  <size=24>{a.Description}</size></color>";
                Row(widths, i, mine.Contains(a.Id), name, by);
            }
        }

        void ShowMyTeam()
        {
            Section($"명예 포인트 {_club.LegacyPoints:N0}  ·  다음 선수 잠재력 +{_club.PotentialBonus}");
            if (_club.Roster.Count == 0)
            {
                Note("은퇴한 선수가 내 팀에 들어와요. 내 팀으로 도전 경기를 치를 수 있어요.");
                return;
            }

            Section($"도전 경기  ·  통산 {_club.ChallengeWins}승");
            Note("은퇴 선수들이 전성기 능력치로 뛰어요. 빈자리는 기본 선수가 채워요. 단계마다 첫 승리에 명예 포인트!", TextAnchor.MiddleLeft, 80);
            foreach (ChallengeTier tier in System.Enum.GetValues(typeof(ChallengeTier)))
            {
                var t = tier;
                bool won = _club.ChallengesWon.Contains(t);
                string label = $"{DreamTeam.TierName(t)} 도전" + (won ? "  ✓ 승리" : $"  (첫 승 +{DreamTeam.Reward(t)})");
                UiKit.Height(UiKit.Button(_content, label, () => Challenge(t), won ? UiKit.Card : UiKit.Accent), 100);
            }
            if (_lastChallenge != null)
            {
                var g = _lastChallenge.Game;
                string result = _lastChallenge.Won ? "<color=#5FA8FF><b>승리!</b></color>" : "<color=#F28C40>패배</color>";
                string reward = _lastChallenge.LegacyPoints > 0 ? $"  명예 포인트 +{_lastChallenge.LegacyPoints}" : "";
                Note($"{g.Away.Name} {g.AwayScore} : {g.HomeScore} {g.Home.Name}  {result}{reward}", TextAnchor.MiddleLeft, 70);
                UiKit.Height(UiKit.Button(_content, "도전 경기 보기", () => Watch(g), UiKit.Muted), 90);
            }

            Section("선수단");
            float[] widths = { 0, 110, 130, 100, 300, 110 };
            Header(widths, "선수", "번호", "포지션", "시즌", "통산", "종합");
            var roster = _club.Roster.OrderByDescending(p => p.Overall).ToList();
            for (int i = 0; i < roster.Count; i++)
            {
                var p = roster[i];
                var arm = p.CareerPitching;
                string totals = !p.IsPitcher || arm == null ? $"{p.CareerLine.Hits}안타 {p.CareerLine.HomeRuns}홈런"
                    : p.Role == PitcherRole.Closer ? $"{arm.Saves}세이브 {arm.Strikeouts}K"
                    : p.Role == PitcherRole.Setup ? $"{arm.Holds}홀드 {arm.Strikeouts}K"
                    : $"{arm.Wins}승 {arm.Strikeouts}K";
                Row(widths, i, p.HallOfFame, (p.HallOfFame ? "★ " : "") + p.Name, p.ShownNumber > 0 ? $"{p.ShownNumber}" : "-",
                    p.IsPitcher ? Positions.RoleName(p.Role) : Positions.Name(p.Position), $"{p.Seasons}", totals, $"{p.Overall}");
            }

            // Numbers on my own team are mine to set; a number retired in a career stays a career story.
            Section("내 팀 등번호");
            foreach (var p in roster)
            {
                var player = p;
                var row = UiKit.Empty(_content, "Number");
                UiKit.Height(row, 80);
                UiKit.Row(row, 10).childForceExpandWidth = false;
                var label = UiKit.Label(row, "", 28, TextAnchor.MiddleLeft);
                UiKit.Width(label, 0, 1);
                void ShowNumber() => label.text = $"{player.Name}  <b>{(player.ShownNumber > 0 ? player.ShownNumber + "번" : "번호 없음")}</b>" +
                    (player.TeamNumber > 0 && player.Number > 0 ? $" <size=22><color=#9EABC2>(현역 {player.Number}번)</color></size>" : "");
                foreach (int step in new[] { -10, -1, 1, 10 })
                {
                    int d = step;
                    var b = UiKit.Button(row, d > 0 ? $"+{d}" : $"{d}", () =>
                    {
                        int from = player.ShownNumber > 0 ? player.ShownNumber : 1;
                        player.TeamNumber = (from - 1 + d + 99) % 99 + 1;
                        ShowNumber();
                        Save();
                    }, UiKit.Background);
                    b.GetComponentInChildren<Text>().fontSize = 26;
                    UiKit.Width(b, 100);
                }
                ShowNumber();
            }

            var famers = roster.Where(p => p.HallOfFame).ToList();
            if (famers.Count > 0)
            {
                Section($"명예의 전당 ({famers.Count}명)");
                foreach (var p in famers)
                    Note($"★ {p.Name}  점수 {p.HallOfFameScore}  올스타 {p.AllStars}회" + (p.NumberRetiredBy != null ? $"  · {p.NumberRetiredBy} 영구결번" : ""), TextAnchor.MiddleLeft, 60);
            }
        }

        void Challenge(ChallengeTier tier)
        {
            _lastChallenge = DreamTeam.Play(_club, tier, new SeededRandom(System.Environment.TickCount));
            Save();
            Show(Tab.MyTeam);
        }

        void Watch(GameResult game)
        {
            _canvas.SetActive(false);
            MatchScreen.Open(game, null, () => { if (this != null) _canvas.SetActive(true); });
        }

        /// <summary>My team lives in the club, which the growth screen saves too; save here so a result can't be lost.</summary>
        void Save()
        {
            if (_career != null && _career.IsMidGame) return;
            SaveStore.Save(_club, _career);
        }

        // ---------- Pieces ----------

        void Section(string title)
        {
            var label = UiKit.Label(_content, title, 34, TextAnchor.LowerLeft, Gold);
            UiKit.Height(label, 80);
        }

        void Header(float[] widths, params string[] cells) => UiKit.TableRow(_content, widths, cells, 26, UiKit.Muted);

        void Row(float[] widths, int index, bool highlight, params string[] cells) =>
            UiKit.TableRow(_content, widths, cells, 30, Color.white, highlight ? Highlight : index % 2 == 1 ? Stripe : (Color?)null);

        void Note(string text, TextAnchor anchor = TextAnchor.MiddleCenter, float height = 100)
        {
            var label = UiKit.Label(_content, text, 28, anchor, UiKit.Muted);
            UiKit.Height(label, height);
        }
    }
}
