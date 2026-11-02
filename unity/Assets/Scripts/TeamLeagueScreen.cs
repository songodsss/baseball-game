using System;
using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// The main stadium: my team league. My retired players play a ten-round season against five AI
    /// clubs, a few games a day, for club coins, trophies and promotion up to the legends league.
    /// </summary>
    public class TeamLeagueScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Win = new Color(0.37f, 0.66f, 1f);
        static readonly Color Loss = new Color(0.95f, 0.55f, 0.25f);
        static readonly Color Highlight = new Color(0.25f, 0.55f, 0.95f, 0.35f);

        Club _club;
        Action _onClose;
        IRandom _random;
        GameObject _canvas;
        RectTransform _card;
        readonly List<string> _log = new List<string>();
        LeagueGameResult _last;
        LeagueSeasonEnd _seasonEnd;

        static string Today => DailyMissions.DateOf(DateTime.Now);

        public static void Open(Club club, Action onClose = null)
        {
            var screen = new GameObject("TeamLeagueScreen").AddComponent<TeamLeagueScreen>();
            screen._club = club;
            screen._onClose = onClose;
            screen._random = new SeededRandom(Environment.TickCount);
            screen.Build();
        }

        void Build()
        {
            _canvas = UiKit.Canvas(transform, "TeamLeagueCanvas", 15);
            var dim = UiKit.Dim(_canvas.transform);
            _card = UiKit.PopupCard(dim, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            UiKit.Column(_card, 32, 14);
            Fill();
        }

        void Save() => SaveStore.SaveClub(_club);

        void Fill()
        {
            foreach (Transform child in _card)
            {
                var layout = child.GetComponent<LayoutElement>();
                if (layout == null || !layout.ignoreLayout) Destroy(child.gameObject);
            }
            var league = _club.TeamLeague;

            var heading = UiKit.Label(_card, "메인 스타디움 · 내 팀 리그", 44, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 76);

            string stadium = Cosmetics.Find(_club.WornId(CosmeticKind.StadiumTheme) ?? "")?.Name ?? "기본 구장";
            string trophies = league.TotalTrophies > 0
                ? string.Join(" ", Enumerable.Range(0, MyTeamLeague.Divisions).Where(d => league.Trophies[d] > 0).Select(d => $"{MyTeamLeague.DivisionName(d)} {league.Trophies[d]}"))
                : "아직 없음";
            UiKit.Height(UiKit.Label(_card,
                $"<b>{MyTeamLeague.DivisionName(league.Division)}</b>" + (league.Started ? $"  ·  시즌 {league.Season}  ·  {league.Round}/{MyTeamLeague.Rounds}경기" : "") +
                $"\n<size=24><color=#BAC4D8>우승 트로피: {trophies}   ·   홈 구장: {stadium}   ·   오늘 남은 경기 {league.GamesLeftToday(Today)}</color></size>",
                30, TextAnchor.MiddleCenter), 96);

            var body = UiKit.ScrollList(_card, 8);
            UiKit.Height(body.parent, 0, 1);

            if (_club.Roster.Count == 0)
            {
                UiKit.Height(UiKit.Label(body, "선수를 한 명 은퇴시켜 내 팀에 영입하면 리그에 나갈 수 있어요.\n빈 자리는 기본 선수가 채워요.", 30, TextAnchor.MiddleCenter, UiKit.Muted), 300);
            }
            else
            {
                if (_seasonEnd != null) SeasonEndBox(body, _seasonEnd);
                if (league.Started) StandingsTable(body, league);
                else UiKit.Height(UiKit.Label(body,
                    "내 팀 은퇴 선수들이 AI 구단 5팀과 10경기 시즌을 치러요.\n상위 2팀은 위 리그로 올라가고, 꼴찌는 내려가요.\n순위에 따라 구단 코인, 1위는 우승 트로피.",
                    28, TextAnchor.MiddleCenter, UiKit.Muted), 220);

                if (_log.Count > 0)
                {
                    UiKit.Height(UiKit.Label(body, "최근 경기", 30, TextAnchor.LowerLeft, Gold), 60);
                    foreach (var line in _log.AsEnumerable().Reverse().Take(10))
                        UiKit.Height(UiKit.Label(body, line, 28), 46);
                }
            }

            var buttons = UiKit.Empty(_card, "Buttons");
            UiKit.Height(buttons, 104);
            UiKit.Row(buttons, 12);
            bool canStart = _club.Roster.Count > 0 && (!league.Started || league.SeasonOver);
            bool canPlay = _club.Roster.Count > 0 && league.Started && !league.SeasonOver && league.GamesLeftToday(Today) > 0;
            if (canStart)
                UiKit.Button(buttons, league.Started ? "다음 시즌 시작" : "첫 시즌 시작", StartSeason, UiKit.Accent);
            else
            {
                var next = UiKit.Button(buttons, "다음 경기", () => PlayRounds(1), UiKit.Accent);
                next.interactable = canPlay;
                var all = UiKit.Button(buttons, "오늘 경기 몰아서", () => PlayRounds(MyTeamLeague.GamesPerDay), UiKit.Card);
                all.interactable = canPlay;
            }
            if (_last != null)
            {
                var game = _last.Game;
                UiKit.Button(buttons, "방금 경기 보기", () => Watch(game), UiKit.Muted);
            }

            UiKit.Height(UiKit.Button(_card, "닫기", Close, UiKit.Muted), 96);
        }

        void StandingsTable(RectTransform body, MyTeamLeague league)
        {
            float[] widths = { 90, 0, 110, 110, 110 };
            UiKit.TableRow(body, widths, new[] { "순위", "구단", "승", "패", "무" }, 24, UiKit.Muted);
            var table = league.Table();
            for (int k = 0; k < table.Count; k++)
            {
                var s = table[k];
                bool mine = s.Team == DreamTeam.Name;
                string mark = k < 2 && league.Division < MyTeamLeague.Divisions - 1 ? " ▲" : k == table.Count - 1 && league.Division > 0 ? " ▼" : "";
                UiKit.TableRow(body, widths, new[] { $"{k + 1}{mark}", mine ? $"<b>{s.Team}</b>" : s.Team, s.Wins.ToString(), s.Losses.ToString(), s.Ties.ToString() },
                    30, Color.white, mine ? Highlight : (Color?)null);
            }
        }

        void SeasonEndBox(RectTransform body, LeagueSeasonEnd end)
        {
            string move = end.Move > 0 ? $"<color=#5FA8FF>{MyTeamLeague.DivisionName(end.Division + 1)} 승격!</color>"
                : end.Move < 0 ? $"<color=#F28C40>{MyTeamLeague.DivisionName(end.Division - 1)} 강등</color>" : "리그 잔류";
            string title = end.Champion ? $"<color=#FFD24D><b>{MyTeamLeague.DivisionName(end.Division)} 우승!</b></color>" : $"<b>{MyTeamLeague.DivisionName(end.Division)} {end.Rank}위</b>";
            var box = UiKit.Panel(body, "SeasonEnd", UiKit.Shade(UiKit.Card, 1.5f));
            UiKit.Height(box, 170);
            var label = UiKit.Label(box, $"{title}\n구단 코인 +{end.Coins:N0}  ·  {move}", 32, TextAnchor.MiddleCenter);
            UiKit.Stretch(label.rectTransform);
            UiAnim.PopIn(box);
        }

        void StartSeason()
        {
            _club.TeamLeague.StartSeason(_random);
            _seasonEnd = null;
            _last = null;
            _log.Clear();
            Save();
            Fill();
        }

        void PlayRounds(int count)
        {
            var league = _club.TeamLeague;
            for (int k = 0; k < count; k++)
            {
                var result = league.PlayRound(_club, Today, _random);
                if (result == null) break;
                _last = result;
                var g = result.Game;
                int mine = result.Home ? g.HomeScore : g.AwayScore, theirs = result.Home ? g.AwayScore : g.HomeScore;
                string verdict = result.Tie ? "<color=#BAC4D8>무</color>" : result.Won ? "<color=#5FA8FF>승</color>" : "<color=#F28C40>패</color>";
                _log.Add($"{league.Round}. {verdict}  {mine} : {theirs}  vs {result.Opponent.Name} ({(result.Home ? "홈" : "원정")})");
                if (result.SeasonEnd != null) { _seasonEnd = result.SeasonEnd; break; }
            }
            Save();
            Fill();
        }

        void Watch(GameResult game)
        {
            _canvas.SetActive(false);
            MatchScreen.Open(game, null, () => { if (this != null) _canvas.SetActive(true); });
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
