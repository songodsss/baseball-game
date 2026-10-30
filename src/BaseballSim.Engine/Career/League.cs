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

    /// <summary>One batter's numbers for the season so far, for leaderboards and awards.</summary>
    public class BatterSeason
    {
        public string Id;
        public string Name;
        public Team Team;
        public BattingLine Line = new BattingLine();
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
        /// <summary>Every batter's season line, by player id. Reset with the standings.</summary>
        public Dictionary<string, BatterSeason> Batting = new Dictionary<string, BatterSeason>();
        readonly Dictionary<Team, int> _nextStarter = new Dictionary<Team, int>();

        public League(RosterGenerator generator, int gamesPerSeason = 36)
        {
            SetSeasonLength(gamesPerSeason);
            foreach (var name in DefaultTeamNames) Teams.Add(generator.CreateTeam(name));
            ResetStandings();
        }

        /// <summary>Rebuilds a saved league; SaveGame restores standings and rotations afterwards.</summary>
        internal League(List<Team> teams, int gamesPerSeason)
        {
            SetSeasonLength(gamesPerSeason);
            Teams = teams;
            ResetStandings();
        }

        /// <summary>Starts handed out so far this season; SaveGame keeps rotations where they were.</summary>
        internal int StartsMade(Team team) => _nextStarter[team];
        internal void SetStartsMade(Team team, int starts) => _nextStarter[team] = starts;

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
            Batting = new Dictionary<string, BatterSeason>();
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

            // Call while the lineups are as they were for the game, so every batter is found on a team.
            foreach (var pair in result.Batting)
            {
                var team = result.Away.Lineup.Contains(pair.Key) ? result.Away : result.Home;
                if (!Batting.TryGetValue(pair.Key.Id, out var season))
                    Batting[pair.Key.Id] = season = new BatterSeason { Id = pair.Key.Id, Name = pair.Key.Name, Team = team };
                Career.Accumulate(season.Line, pair.Value);
            }
        }

        /// <summary>Plate appearances needed to qualify for rate titles: 3.1 per team game, as in pro leagues.</summary>
        public int QualifyingPa => (int)Math.Ceiling(3.1 * GamesPerSeason);

        public List<Standing> Ranked() => Standings.Values.OrderByDescending(s => s.WinPct).ToList();

        /// <summary>Games behind the leader: half the difference in wins plus half the difference in losses.</summary>
        public double GamesBehind(Standing standing)
        {
            var leader = Ranked()[0];
            return ((leader.Wins - standing.Wins) + (standing.Losses - leader.Losses)) / 2.0;
        }
    }
}
