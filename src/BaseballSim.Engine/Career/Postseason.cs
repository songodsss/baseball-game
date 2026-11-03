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
    /// My club's games are played one at a time, with my big at bats asked about like in the season.
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

        /// <summary>
        /// Where a postseason replay stopped: before my club's next game, at a big at bat in it, or at the end.
        /// </summary>
        public class Run
        {
            public PostseasonResult Result;
            /// <summary>My club's latest game: finished, or paused at <see cref="DayResult.AtBat"/>.</summary>
            public DayResult Current;
            /// <summary>The series my club plays next, and the rival in it; null when my club is done.</summary>
            public PlayoffSeries NextSeries;
            public Team NextOpponent;
            /// <summary>The series and rival of <see cref="Current"/>.</summary>
            public PlayoffSeries CurrentSeries;
            public Team CurrentOpponent;
            public bool Done;
        }

        /// <summary>
        /// Plays the whole postseason from <paramref name="seed"/>. My club's games are played up to
        /// <paramref name="myGames"/> of them, my big at bats answered from <paramref name="decisions"/> in order;
        /// it stops at the first one without an answer, or before my club's next game. With
        /// <paramref name="auto"/> it plays to the end, unanswered spots played the usual way.
        /// Replaying with the same seed and answers always gives the same postseason, so the
        /// autumn can be played a game at a time and survives a save.
        /// </summary>
        public static Run Play(Career career, int seed, IList<Approach> decisions, int myGames, bool auto)
        {
            var master = new SeededRandom(seed);
            var seeds = career.League.Ranked().Take(Teams).Select(s => s.Team).ToList();
            var run = new Run { Result = new PostseasonResult { MyTeamIn = seeds.Contains(career.MyTeam) } };
            var ctx = new Ctx { Career = career, Master = master, Decisions = decisions, MyGames = myGames, Auto = auto, Run = run };

            var semi1 = Series(ctx, "준결승", seeds[0], seeds[3], 2);
            if (semi1 == null) return run;
            var semi2 = Series(ctx, "준결승", seeds[1], seeds[2], 2);
            if (semi2 == null) return run;
            // The better regular-season team hosts the final.
            var finalists = new[] { semi1.Winner, semi2.Winner }.OrderBy(t => seeds.IndexOf(t)).ToList();
            var final = Series(ctx, "챔피언 결정전", finalists[0], finalists[1], 3);
            if (final == null) return run;

            var result = run.Result;
            result.Champion = final.Winner;
            result.MyTeamWon = result.Champion == career.MyTeam;
            if (result.MyTeamIn)
            {
                // Postseason shares, as a slice of salary: more the further the team went.
                double share = result.MyTeamWon ? 0.15 : final.Loser == career.MyTeam ? 0.08 : 0.04;
                result.Bonus = (int)(career.Me.Salary * share);
            }
            run.Done = true;
            return run;
        }

        class Ctx
        {
            public Career Career;
            public IRandom Master;
            public IList<Approach> Decisions;
            public int Asked;
            public int MyGames, Played;
            public bool Auto;
            public Run Run;
            public readonly Dictionary<Team, int> Starter = new Dictionary<Team, int>();

            // Aces first: each club's rotation from the top, kept apart from the league's regular-season turn.
            public Player NextStarter(Team team)
            {
                Starter.TryGetValue(team, out int i);
                Starter[team] = i + 1;
                return team.Rotation[i % team.Rotation.Count];
            }
        }

        /// <summary>Plays a series; null if the replay stopped in it (the series so far is still listed).</summary>
        static PlayoffSeries Series(Ctx ctx, string name, Team high, Team low, int winsNeeded)
        {
            var series = new PlayoffSeries { Name = name, High = high, Low = low };
            ctx.Run.Result.Series.Add(series);
            var me = ctx.Career.MyTeam;
            while (series.HighWins < winsNeeded && series.LowWins < winsNeeded && series.Games.Count < MaxGamesPerSeries)
            {
                // The better seed is at home in odd games.
                bool highHome = series.Games.Count % 2 == 0;
                var home = highHome ? high : low;
                var away = highHome ? low : high;
                bool mine = home == me || away == me;
                if (mine && !ctx.Auto && ctx.Played >= ctx.MyGames)
                {
                    ctx.Run.NextSeries = series;
                    ctx.Run.NextOpponent = home == me ? away : home;
                    return null;
                }
                if (mine)
                {
                    ctx.Run.CurrentSeries = series;
                    ctx.Run.CurrentOpponent = home == me ? away : home;
                }
                var game = PlayGame(ctx, away, home, ctx.NextStarter(away), ctx.NextStarter(home));
                if (game == null) return null; // paused at my big at bat
                series.Games.Add(game);
                if (game.Winner == high) series.HighWins++;
                else if (game.Winner == low) series.LowWins++;
            }
            // Still level after the cap: the better seed goes through.
            if (series.HighWins == series.LowWins) series.HighWins++;
            return series;
        }

        static GameResult PlayGame(Ctx ctx, Team away, Team home, Player awayStarter, Player homeStarter)
        {
            var career = ctx.Career;
            int gameSeed = (int)(ctx.Master.NextDouble() * int.MaxValue);
            bool mine = away == career.MyTeam || home == career.MyTeam;
            if (!mine) return new GameSimulator(new SeededRandom(gameSeed)).Play(away, home, awayStarter, homeStarter);

            ctx.Played++;
            var result = ctx.Run.Result;
            var day = new DayResult { Season = career.Season, Day = career.Day, Action = DailyAction.Rest };
            ctx.Run.Current = day;
            if (career.Me.IsInjured)
            {
                day.SatOut = true;
                day.MyGame = new GameSimulator(new SeededRandom(gameSeed)).Play(away, home, awayStarter, homeStarter);
                return day.MyGame;
            }

            var gamePlayer = career.Me.ForGame(ctx.Master);
            var sim = new GameSimulator(new SeededRandom(gameSeed))
            {
                DecideFor = gamePlayer,
                Decide = s => ctx.Asked < ctx.Decisions.Count ? ctx.Decisions[ctx.Asked++] : ctx.Auto ? Approach.Normal : (Approach?)null,
            };
            bool atHome = home == career.MyTeam;
            List<Player> staff = null;
            int slot = -1;
            if (gamePlayer.IsPitcher && career.Me.Role != PitcherRole.Starter)
            {
                // A reliever is in the bullpen every game, in his own job.
                staff = career.MyTeam.Bullpen;
                slot = staff.Count - (career.Me.Role == PitcherRole.Closer ? 1 : 2);
            }
            else if (gamePlayer.IsPitcher)
            {
                // A starter takes every fourth game.
                if (result.MyTeamGames++ % 4 != 0)
                {
                    day.BetweenStarts = true;
                    day.MyGame = new GameSimulator(new SeededRandom(gameSeed)).Play(away, home, awayStarter, homeStarter);
                    return day.MyGame;
                }
                staff = career.MyTeam.Rotation;
                slot = staff.IndexOf(atHome ? homeStarter : awayStarter);
                if (atHome) homeStarter = gamePlayer; else awayStarter = gamePlayer;
            }
            else
            {
                staff = career.MyTeam.Lineup;
                slot = career.LineupSlot;
            }

            var replaced = staff[slot];
            staff[slot] = gamePlayer;
            GameResult game;
            try
            {
                game = sim.Play(away, home, awayStarter, homeStarter);
            }
            finally
            {
                staff[slot] = replaced;
            }

            var events = game?.Events ?? sim.Partial.Events;
            day.AtBatLog = events
                .Where(e => (e.Batter == gamePlayer || e.Pitcher == gamePlayer) && e.Note != null)
                .Select(e => $"{Commentary.HalfInning(e.Inning, e.IsTopHalf)} {Commentary.Describe(e)} ({e.Note})")
                .ToList();
            if (game == null)
            {
                day.AtBat = sim.Suspended;
                return null;
            }

            day.MyGame = game;
            day.MyGamePlayer = gamePlayer;
            if (gamePlayer.IsPitcher)
            {
                if (game.Pitching.TryGetValue(gamePlayer, out var arm))
                {
                    var line = new PitcherLine();
                    if (career.Me.Role == PitcherRole.Starter)
                    {
                        int ours = atHome ? game.HomeScore : game.AwayScore, theirs = atHome ? game.AwayScore : game.HomeScore;
                        var (win, loss) = PitcherLine.Decision(arm, ours, theirs);
                        line.Add(arm, true, win, loss);
                        result.MyPitching.Add(arm, true, win, loss);
                    }
                    else
                    {
                        line.Add(arm, false, false, false);
                        result.MyPitching.Add(arm, false, false, false);
                    }
                    day.MyPitching = line;
                }
                else day.BetweenStarts = true;
            }
            else
            {
                day.MyLine = game.Batting.TryGetValue(gamePlayer, out var line) ? line : new BattingLine();
                Career.Accumulate(result.MyLine, day.MyLine);
            }
            return game;
        }
    }
}
