using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class Standing
    {
        public Team Team;
        public int Wins, Losses, Ties;
        public double WinPct => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
    }

    /// <summary>Ten fictional teams, every team playing once a day. One season is one year.</summary>
    public class League
    {
        public const int FullSeason = 144;
        /// <summary>Season lengths a player can pick: a full season, a half, or a quarter.</summary>
        public static readonly int[] SeasonLengths = { 144, 72, 36 };

        /// <summary>Games per season; a multiple of 9 so every rival is met equally often.</summary>
        public int GamesPerSeason { get; private set; }

        public static readonly string[] DefaultTeamNames =
        {
            "서울 블루윙스", "부산 마린스타즈", "인천 시호크스", "대구 썬더볼츠", "광주 타이거릴리",
            "대전 이글아이즈", "수원 나이츠", "창원 다이노스톰", "고양 레드폭스", "울산 웨일스",
        };

        public List<Team> Teams = new List<Team>();
        public Dictionary<Team, Standing> Standings = new Dictionary<Team, Standing>();
        readonly Dictionary<Team, int> _nextStarter = new Dictionary<Team, int>();

        public League(RosterGenerator generator, int gamesPerSeason = 36)
        {
            SetSeasonLength(gamesPerSeason);
            foreach (var name in DefaultTeamNames) Teams.Add(generator.CreateTeam(name));
            ResetStandings();
        }

        /// <summary>Only between seasons; Career.ChangeSeasonLength checks that.</summary>
        public void SetSeasonLength(int games)
        {
            if (Array.IndexOf(SeasonLengths, games) < 0)
                throw new ArgumentException($"Season length must be one of {string.Join(", ", SeasonLengths)}.");
            GamesPerSeason = games;
        }

        public void ResetStandings()
        {
            Standings = Teams.ToDictionary(t => t, t => new Standing { Team = t });
            foreach (var t in Teams) _nextStarter[t] = 0;
        }

        /// <summary>
        /// The day's matchups (away, home) by the circle method: nine rounds cover every pair once,
        /// and home and away swap every other cycle.
        /// </summary>
        public List<(Team Away, Team Home)> Matchups(int day)
        {
            int n = Teams.Count;
            int round = day % (n - 1);
            bool swap = (day / (n - 1)) % 2 == 1;
            var rotating = Teams.Skip(1).ToList();
            var order = new List<Team> { Teams[0] };
            for (int i = 0; i < n - 1; i++) order.Add(rotating[(i + round) % (n - 1)]);

            var games = new List<(Team, Team)>();
            for (int i = 0; i < n / 2; i++)
            {
                var a = order[i];
                var b = order[n - 1 - i];
                games.Add(swap ? (b, a) : (a, b));
            }
            return games;
        }

        public Player NextStarter(Team team)
        {
            int i = _nextStarter[team]++;
            return team.Rotation[i % team.Rotation.Count];
        }

        public void Record(GameResult result)
        {
            var home = Standings[result.Home];
            var away = Standings[result.Away];
            if (result.IsTie) { home.Ties++; away.Ties++; }
            else if (result.Winner == result.Home) { home.Wins++; away.Losses++; }
            else { away.Wins++; home.Losses++; }
        }

        public List<Standing> Ranked() => Standings.Values.OrderByDescending(s => s.WinPct).ToList();
    }
}
