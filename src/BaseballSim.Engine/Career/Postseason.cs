using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class PlayoffSeries
    {
        public string Name;
        public Team High; // better seed
        public Team Low;
        public int HighWins, LowWins;
        public List<GameResult> Games = new List<GameResult>();
        public Team Winner => HighWins > LowWins ? High : Low;
        public Team Loser => Winner == High ? Low : High;
    }

    public class PostseasonResult
    {
        public List<PlayoffSeries> Series = new List<PlayoffSeries>();
        public Team Champion;
        public bool MyTeamIn;
        public bool MyTeamWon;
        /// <summary>My player's games in the postseason (not counted in season stats).</summary>
        public BattingLine MyLine = new BattingLine();
        /// <summary>My pitcher's postseason starts.</summary>
        public PitcherLine MyPitching = new PitcherLine();
        /// <summary>Postseason games my club has played, for a pitcher's turn: he starts every fourth.</summary>
        public int MyTeamGames;
        public int Bonus;
    }

    /// <summary>
    /// Autumn baseball: the top four play semifinals (1 v 4, 2 v 3, best of three), then a best-of-five final.
    /// Played at once when the regular season ends; the match screen can replay any game.
    /// </summary>
    public static class Postseason
    {
        /// <summary>Lines for the records: my team's result first, then each series as "준플레이오프  A 2 : 1 B".</summary>
        public static List<string> Describe(PostseasonResult post, Team myTeam)
        {
            var lines = new List<string>();
            var lost = post.Series.FirstOrDefault(s => s.Loser == myTeam);
            lines.Add(post.MyTeamWon ? "우승!"
                : lost != null ? $"{lost.Name} 탈락 · {post.Champion?.Name} 우승"
                : $"진출 실패 · {post.Champion?.Name} 우승");
            foreach (var s in post.Series) lines.Add($"{s.Name}  {s.High.Name} {s.HighWins} : {s.LowWins} {s.Low.Name}");
            return lines;
        }

        public const int Teams = 4;
        public const string ChampionTitle = "챔피언 결정전 우승";
        const int MaxGamesPerSeries = 9; // ties replay, but never forever

        public static PostseasonResult Play(Career career, GameSimulator sim, IRandom random)
        {
            var seeds = career.League.Ranked().Take(Teams).Select(s => s.Team).ToList();
            var result = new PostseasonResult { MyTeamIn = seeds.Contains(career.MyTeam) };

            var semi1 = Series(career, sim, random, "준결승", seeds[0], seeds[3], 2, result);
            var semi2 = Series(career, sim, random, "준결승", seeds[1], seeds[2], 2, result);
            // The better regular-season team hosts the final.
            var finalists = new[] { semi1.Winner, semi2.Winner }.OrderBy(t => seeds.IndexOf(t)).ToList();
            var final = Series(career, sim, random, "챔피언 결정전", finalists[0], finalists[1], 3, result);

            result.Champion = final.Winner;
            result.MyTeamWon = result.Champion == career.MyTeam;
            if (result.MyTeamIn)
            {
                // Postseason shares, as a slice of salary: more the further the team went.
                double share = result.MyTeamWon ? 0.15 : final.Loser == career.MyTeam ? 0.08 : 0.04;
                result.Bonus = (int)(career.Me.Salary * share);
                career.Me.Money += result.Bonus;
            }
            return result;
        }

        static PlayoffSeries Series(Career career, GameSimulator sim, IRandom random, string name, Team high, Team low, int winsNeeded, PostseasonResult result)
        {
            var series = new PlayoffSeries { Name = name, High = high, Low = low };
            var league = career.League;
            while (series.HighWins < winsNeeded && series.LowWins < winsNeeded && series.Games.Count < MaxGamesPerSeries)
            {
                // The better seed is at home in odd games.
                bool highHome = series.Games.Count % 2 == 0;
                var home = highHome ? high : low;
                var away = highHome ? low : high;
                var game = PlayGame(career, sim, random, away, home, league.NextStarter(away), league.NextStarter(home), result);
                series.Games.Add(game);
                if (game.Winner == high) series.HighWins++;
                else if (game.Winner == low) series.LowWins++;
            }
            // Still level after the cap: the better seed goes through.
            if (series.HighWins == series.LowWins) series.HighWins++;
            result.Series.Add(series);
            return series;
        }

        static GameResult PlayGame(Career career, GameSimulator sim, IRandom random, Team away, Team home, Player awayStarter, Player homeStarter, PostseasonResult result)
        {
            bool mine = (away == career.MyTeam || home == career.MyTeam) && !career.Me.IsInjured;
            if (!mine) return sim.Play(away, home, awayStarter, homeStarter);

            var gamePlayer = career.Me.ForGame(random);
            if (gamePlayer.IsPitcher && career.Me.Role != PitcherRole.Starter)
            {
                // A reliever is in the bullpen every game, in his own job.
                var pen = career.MyTeam.Bullpen;
                int at = pen.Count - (career.Me.Role == PitcherRole.Closer ? 1 : 2);
                var regularArm = pen[at];
                pen[at] = gamePlayer;
                try
                {
                    var game = sim.Play(away, home, awayStarter, homeStarter);
                    if (game.Pitching.TryGetValue(gamePlayer, out var arm)) result.MyPitching.Add(arm, false, false, false);
                    return game;
                }
                finally
                {
                    pen[at] = regularArm;
                }
            }
            if (gamePlayer.IsPitcher)
            {
                if (result.MyTeamGames++ % 4 != 0) return sim.Play(away, home, awayStarter, homeStarter);
                bool atHome = home == career.MyTeam;
                var replaced = atHome ? homeStarter : awayStarter;
                int slot = career.MyTeam.Rotation.IndexOf(replaced);
                career.MyTeam.Rotation[slot] = gamePlayer;
                try
                {
                    var game = sim.Play(away, home, atHome ? awayStarter : gamePlayer, atHome ? gamePlayer : homeStarter);
                    if (game.Pitching.TryGetValue(gamePlayer, out var arm))
                    {
                        int ours = atHome ? game.HomeScore : game.AwayScore, theirs = atHome ? game.AwayScore : game.HomeScore;
                        var (win, loss) = PitcherLine.Decision(arm, ours, theirs);
                        result.MyPitching.Add(arm, true, win, loss);
                    }
                    return game;
                }
                finally
                {
                    career.MyTeam.Rotation[slot] = replaced;
                }
            }
            var regular = career.MyTeam.Lineup[career.LineupSlot];
            career.MyTeam.Lineup[career.LineupSlot] = gamePlayer;
            try
            {
                var game = sim.Play(away, home, awayStarter, homeStarter);
                if (game.Batting.TryGetValue(gamePlayer, out var line)) Career.Accumulate(result.MyLine, line);
                return game;
            }
            finally
            {
                career.MyTeam.Lineup[career.LineupSlot] = regular;
            }
        }
    }
}
