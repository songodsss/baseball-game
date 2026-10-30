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
        enum Tab { Standings, MyRecord, Achievements, MyTeam }

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Highlight = new Color(0.25f, 0.55f, 0.95f, 0.25f);
        static readonly Color Stripe = new Color(1, 1, 1, 0.04f);

        CareerState _career;
        Club _club;
        RectTransform _content;
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
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var root = UiKit.SafeRoot(canvasGo.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);

            UiKit.Height(UiKit.Label(root, "기록", 48), 80);

            var tabs = UiKit.Empty(root, "Tabs");
            UiKit.Height(tabs, 100);
            UiKit.Row(tabs, 16);
            _tabs[Tab.Standings] = UiKit.Button(tabs, "리그 순위", () => Show(Tab.Standings), UiKit.Card);
            _tabs[Tab.MyRecord] = UiKit.Button(tabs, "내 기록", () => Show(Tab.MyRecord), UiKit.Card);
            _tabs[Tab.Achievements] = UiKit.Button(tabs, "업적", () => Show(Tab.Achievements), UiKit.Card);
            _tabs[Tab.MyTeam] = UiKit.Button(tabs, "내 팀", () => Show(Tab.MyTeam), UiKit.Card);

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
                int wins = rivalYears.Count(h => Stats.Ops(h.Line) >= Stats.Ops(h.RivalLine));
                var rl = _career.RivalLine;
                Section($"라이벌 {_career.Rival.Name} ({_career.RivalTeam?.Name})");
                Note($"이번 시즌 타율 {Stats.Rate(Stats.Avg(rl))}  {rl.HomeRuns}홈런  OPS {Stats.Rate(Stats.Ops(rl))}", TextAnchor.MiddleLeft, 60);
                if (rivalYears.Count > 0)
                    Note($"시즌 OPS 대결 {wins}승 {rivalYears.Count - wins}패", TextAnchor.MiddleLeft, 60);
            }

            var t = Stats.CareerTotal(_career);
            Section("통산 (이번 시즌 포함)");
            Header(statWidths, "타율", "안타", "홈런", "타점", "OPS", "볼넷");
            Row(statWidths, 0, true, Stats.Rate(Stats.Avg(t)), t.Hits.ToString(), t.HomeRuns.ToString(),
                t.RunsBattedIn.ToString(), Stats.Rate(Stats.Ops(t)), t.Walks.ToString());
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
            float[] widths = { 0, 110, 130, 120, 130 };
            Header(widths, "선수", "시즌", "안타", "홈런", "종합");
            if (_club.Roster.Count == 0) Note("은퇴한 선수가 내 팀에 들어와요. 나중에 이 선수들로 팀 콘텐츠를 즐길 수 있어요.");
            var roster = _club.Roster.OrderByDescending(p => p.Overall).ToList();
            for (int i = 0; i < roster.Count; i++)
            {
                var p = roster[i];
                Row(widths, i, p.HallOfFame, (p.HallOfFame ? "★ " : "") + p.Name, $"{p.Seasons}", $"{p.CareerLine.Hits}", $"{p.CareerLine.HomeRuns}", $"{p.Overall}");
            }

            var famers = roster.Where(p => p.HallOfFame).ToList();
            if (famers.Count > 0)
            {
                Section($"명예의 전당 ({famers.Count}명)");
                foreach (var p in famers)
                    Note($"★ {p.Name}  점수 {p.HallOfFameScore}  올스타 {p.AllStars}회" + (p.NumberRetiredBy != null ? $"  · {p.NumberRetiredBy} 영구결번" : ""), TextAnchor.MiddleLeft, 60);
            }
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
