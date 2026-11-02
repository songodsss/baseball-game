using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class LeagueStanding
    {
        public string Team;
        public int Wins, Losses, Ties;
        public double Pct => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
    }

    /// <summary>One game of my team in the my-team league, for the result card.</summary>
    public class LeagueGameResult
    {
        public GameResult Game;
        public Team Mine;
        public Team Opponent;
        public bool Home;
        public bool Won => Game.Winner == Mine;
        public bool Tie => Game.IsTie;
        /// <summary>Set when this game ended the season.</summary>
        public LeagueSeasonEnd SeasonEnd;
    }

    public class LeagueSeasonEnd
    {
        public int Division;
        public int Rank;
        public int Coins;
        public bool Champion;
        /// <summary>-1 relegated, 0 stays, +1 promoted.</summary>
        public int Move;
    }

    /// <summary>
    /// The my-team league: my retired players (at their peak) play a short season against five
    /// AI clubs. Four divisions, from 3부 up to the legends league; the top two go up, the last goes
    /// down. Seasons pay club coins by rank and a trophy for a title. A few games a day keep it a
    /// daily habit. AI rosters are rebuilt from a saved seed, so only standings need saving.
    /// </summary>
    public class MyTeamLeague
    {
        public const int Teams = 6;
        /// <summary>Each club plays every other twice: 10 rounds.</summary>
        public const int Rounds = (Teams - 1) * 2;
        public const int GamesPerDay = 10;
        public const int Divisions = 4;

        static readonly string[] DivisionNames = { "3부 리그", "2부 리그", "1부 리그", "레전드 리그" };
        /// <summary>How far above an ordinary league player each division's AI clubs are.</summary>
        static readonly int[] DivisionStrength = { 0, 6, 12, 20 };
        static readonly int[] ChampionCoins = { 300, 500, 800, 1200 };
        static readonly string[] ClubNames =
        {
            "해안 갈매기", "산골 반달곰", "강변 수달", "평원 늑대", "항구 고래", "고원 독수리",
            "숲속 부엉이", "사막 여우", "설원 표범", "호수 백조", "협곡 매", "초원 들소",
        };

        public int Division;
        /// <summary>Seasons started, counting the one in play; 0 before the first.</summary>
        public int Season;
        public int Round;
        public int Seed;
        public List<string> Opponents = new List<string>();
        public List<LeagueStanding> Standings = new List<LeagueStanding>();
        /// <summary>Titles won in each division.</summary>
        public int[] Trophies = new int[Divisions];
        public string Date = "";
        public int GamesToday;

        public static string DivisionName(int division) => DivisionNames[Math.Max(0, Math.Min(Divisions - 1, division))];
        public bool Started => Season > 0;
        public bool SeasonOver => Started && Round >= Rounds;
        public int GamesLeftToday(string today) => Date == today ? Math.Max(0, GamesPerDay - GamesToday) : GamesPerDay;
        public int TotalTrophies => Trophies.Sum();

        /// <summary>Starts a new season in the current division with five fresh AI clubs.</summary>
        public void StartSeason(IRandom random)
        {
            Season++;
            Round = 0;
            Seed = (int)(random.NextDouble() * int.MaxValue);
            var pool = ClubNames.OrderBy(_ => random.NextDouble()).Take(Teams - 1).ToList();
            Opponents = pool;
            Standings = new[] { DreamTeam.Name }.Concat(pool).Select(n => new LeagueStanding { Team = n }).ToList();
        }

        public List<LeagueStanding> Table() =>
            Standings.OrderByDescending(s => s.Pct).ThenByDescending(s => s.Wins).ThenBy(s => s.Team == DreamTeam.Name ? 0 : 1).ToList();

        public int MyRank => Table().FindIndex(s => s.Team == DreamTeam.Name) + 1;

        /// <summary>Round-robin pairings for a round (circle method, home and away swapped in the second half).</summary>
        public static List<(int Home, int Away)> Pairings(int round)
        {
            int r = round % (Teams - 1);
            var order = new List<int> { 0 };
            for (int k = 0; k < Teams - 1; k++) order.Add(1 + (k + r) % (Teams - 1));
            var games = new List<(int, int)>();
            for (int k = 0; k < Teams / 2; k++)
            {
                int a = order[k], b = order[Teams - 1 - k];
                bool flip = (k + r) % 2 == 1;
                if (round >= Teams - 1) flip = !flip;
                games.Add(flip ? (b, a) : (a, b));
            }
            return games;
        }

        /// <summary>The AI club at a standings index, rebuilt the same way every time from the season seed.</summary>
        public Team AiTeam(int index)
        {
            var random = new SeededRandom(unchecked(Seed + index * 7919));
            var team = new RosterGenerator(random).CreateTeam(Standings[index].Team);
            int strength = DivisionStrength[Division];
            int Shift(int rating) => Math.Max(15, Math.Min(95, rating + strength));
            foreach (var p in team.Lineup.Concat(team.Rotation).Concat(team.Bullpen))
            {
                p.Id = $"mtl{index}-" + p.Id;
                var b = p.Batting;
                b.Contact = Shift(b.Contact); b.Power = Shift(b.Power); b.Eye = Shift(b.Eye); b.Speed = Shift(b.Speed); b.Defense = Shift(b.Defense);
                if (p.Pitching != null)
                {
                    var q = p.Pitching;
                    q.Velocity = Shift(q.Velocity); q.Control = Shift(q.Control); q.Breaking = Shift(q.Breaking); q.Stamina = Shift(q.Stamina);
                }
            }
            return team;
        }

        /// <summary>
        /// Plays the next round: my game, simulated in full, and the other two games of the round.
        /// Null when the season is over, nobody has retired yet, or today's games are used up.
        /// </summary>
        public LeagueGameResult PlayRound(Club club, string today, IRandom random)
        {
            if (club.Roster.Count == 0) return null;
            if (!Started || SeasonOver) return null;
            if (GamesLeftToday(today) == 0) return null;
            if (Date != today) { Date = today; GamesToday = 0; }
            GamesToday++;

            var sim = new GameSimulator(random);
            LeagueGameResult mine = null;
            foreach (var (home, away) in Pairings(Round))
            {
                bool involvesMe = home == 0 || away == 0;
                var homeTeam = home == 0 ? DreamTeam.Build(club, random) : AiTeam(home);
                var awayTeam = away == 0 ? DreamTeam.Build(club, random) : AiTeam(away);
                int starter = Round % 5;
                var game = sim.Play(awayTeam, homeTeam, awayTeam.Rotation[starter % awayTeam.Rotation.Count], homeTeam.Rotation[starter % homeTeam.Rotation.Count]);
                Record(home, away, game, homeTeam);
                if (involvesMe)
                    mine = new LeagueGameResult { Game = game, Home = home == 0, Mine = home == 0 ? homeTeam : awayTeam, Opponent = home == 0 ? awayTeam : homeTeam };
            }
            Round++;
            if (SeasonOver) mine.SeasonEnd = EndSeason(club);
            return mine;
        }

        void Record(int home, int away, GameResult game, Team homeTeam)
        {
            var h = Standings[home];
            var a = Standings[away];
            if (game.IsTie) { h.Ties++; a.Ties++; }
            else if (game.Winner == homeTeam) { h.Wins++; a.Losses++; }
            else { a.Wins++; h.Losses++; }
        }

        /// <summary>Coins by rank, a trophy for first, then promotion (top two) or relegation (last).</summary>
        LeagueSeasonEnd EndSeason(Club club)
        {
            int rank = MyRank;
            int top = ChampionCoins[Division];
            int coins = rank == 1 ? top : rank == 2 ? top * 6 / 10 : rank == 3 ? top * 4 / 10 : top / 5;
            var end = new LeagueSeasonEnd { Division = Division, Rank = rank, Coins = coins, Champion = rank == 1 };
            club.Coins += coins;
            if (end.Champion) Trophies[Division]++;
            if (rank <= 2 && Division < Divisions - 1) { Division++; end.Move = 1; }
            else if (rank == Teams && Division > 0) { Division--; end.Move = -1; }
            return end;
        }
    }
}
