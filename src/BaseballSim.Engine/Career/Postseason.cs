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
        /// <summary>Wins the better seed starts with (the wild card's 4th seed has one).</summary>
        public int HeadStart;
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
        /// <summary>The Korean Series MVP: the champion's best player of the series. Empty if no series was played.</summary>
        public string KsMvp = "";
        public string KsMvpLine = "";
        public bool KsMvpMine;
    }

    /// <summary>
    /// Autumn baseball, as in the KBO: the top five go in a step ladder. 4th v 5th in the wild card
    /// (4th starts a win up, every game at 4th's park), then the semi playoff (3rd, best of five), the
    /// playoff (2nd, best of five) and the Korean Series (1st, best of seven). No ties: extra innings
    /// go on until somebody wins. My club's games are played one at a time, my big at bats asked about.
    /// </summary>
    public static class Postseason
    {
        /// <summary>Lines for the records: my team's result first, then each series as "준플레이오프  A 2 : 1 B".</summary>
        public static List<string> Describe(PostseasonResult post, Team myTeam)
        {
            var lines = new List<string>();
            var lost = post.Series.FirstOrDefault(s => s.Loser == myTeam);
            lines.Add(post.MyTeamWon ? "우승!"
                : lost != null && lost.Name == KoreanSeries ? $"준우승 · {post.Champion?.Name} 우승"
                : lost != null ? $"{lost.Name} 탈락 · {post.Champion?.Name} 우승"
                : $"진출 실패 · {post.Champion?.Name} 우승");
            foreach (var s in post.Series) lines.Add($"{s.Name}  {s.High.Name} {s.HighWins} : {s.LowWins} {s.Low.Name}");
            if (!string.IsNullOrEmpty(post.KsMvp)) lines.Add($"{KsMvpTitle}  {post.KsMvp}");
            return lines;
        }

        public const int Teams = 5;
        public const string ChampionTitle = "한국시리즈 우승";
        /// <summary>The old name of the title, still in saves from before the KBO format.</summary>
        public const string OldChampionTitle = "챔피언 결정전 우승";
        public const string KsMvpTitle = "한국시리즈 MVP";
        public const string WildCard = "와일드카드 결정전", SemiPlayoff = "준플레이오프", Playoff = "플레이오프", KoreanSeries = "한국시리즈";
        const int MaxGamesPerSeries = 9; // a tie (only past the innings cap) replays, but never forever
        /// <summary>Postseason games have no tie: this is only a safety cap.</summary>
        public const int PostseasonMaxInnings = 30;

        /// <summary>Wins a club needs to take the series, counting the 4th seed's head start in the wild card.</summary>
        public static int WinsNeeded(string series) => series == KoreanSeries ? 4 : series == WildCard ? 2 : 3;

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

            var wild = Series(ctx, WildCard, seeds[3], seeds[4], headStart: 1);
            if (wild == null) return run;
            var semi = Series(ctx, SemiPlayoff, seeds[2], wild.Winner);
            if (semi == null) return run;
            var po = Series(ctx, Playoff, seeds[1], semi.Winner);
            if (po == null) return run;
            var final = Series(ctx, KoreanSeries, seeds[0], po.Winner);
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
            PickKsMvp(final, career, result);
            run.Done = true;
            return run;
        }

        /// <summary>The champion's best of the Korean Series: big hits and runs for a batter, outs and few runs for an arm.</summary>
        static void PickKsMvp(PlayoffSeries final, Career career, PostseasonResult result)
        {
            var champ = final.Winner;
            var scores = new Dictionary<string, (string Name, double Score, string Line)>();
            var bats = new Dictionary<string, BattingLine>();
            var arms = new Dictionary<string, PitchingLine>();
            foreach (var g in final.Games)
            {
                bool Ours(string id) => champ.Lineup.Any(p => p.Id == id) || id == career.Me.Id && career.MyTeam == champ;
                foreach (var b in g.Batting.Where(b => Ours(b.Key.Id)))
                {
                    if (!bats.TryGetValue(b.Key.Id, out var line)) bats[b.Key.Id] = line = new BattingLine();
                    Career.Accumulate(line, b.Value);
                    scores[b.Key.Id] = (b.Key.Name, 0, "");
                }
                var staff = champ.Rotation.Concat(champ.Bullpen).Select(p => p.Id).ToList();
                foreach (var a in g.Pitching.Where(a => staff.Contains(a.Key.Id) || a.Key.Id == career.Me.Id && career.MyTeam == champ))
                {
                    if (!arms.TryGetValue(a.Key.Id, out var line)) arms[a.Key.Id] = line = new PitchingLine();
                    line.OutsRecorded += a.Value.OutsRecorded;
                    line.Runs += a.Value.Runs;
                    line.Strikeouts += a.Value.Strikeouts;
                    line.Saves += a.Value.Saves;
                    scores[a.Key.Id] = (a.Key.Name, 0, "");
                }
            }
            string best = null;
            double top = double.MinValue;
            foreach (var id in scores.Keys.ToList())
            {
                double score = 0;
                string text = "";
                if (bats.TryGetValue(id, out var b) && b.PlateAppearances > 0)
                {
                    int singles = b.Hits - b.Doubles - b.Triples - b.HomeRuns;
                    score += singles + 2 * b.Doubles + 3 * b.Triples + 4.5 * b.HomeRuns + 0.6 * b.Walks + 1.2 * b.RunsBattedIn + 0.8 * b.Runs - 0.25 * (b.AtBats - b.Hits);
                    text = $"타율 {(b.AtBats == 0 ? 0 : (double)b.Hits / b.AtBats):.000} {b.HomeRuns}홈런 {b.RunsBattedIn}타점";
                }
                if (arms.TryGetValue(id, out var a) && a.OutsRecorded > 0)
                {
                    score += a.OutsRecorded * 0.55 - a.Runs * 2.2 + a.Strikeouts * 0.3 + a.Saves * 2;
                    string arm = $"{a.OutsRecorded / 3}.{a.OutsRecorded % 3}이닝 {a.Runs}실점 {a.Strikeouts}탈삼진" + (a.Saves > 0 ? $" {a.Saves}세이브" : "");
                    text = text == "" ? arm : text + " / " + arm;
                }
                scores[id] = (scores[id].Name, score, text);
                if (score > top) { top = score; best = id; }
            }
            if (best == null) return;
            result.KsMvp = scores[best].Name;
            result.KsMvpLine = scores[best].Line;
            result.KsMvpMine = best == career.Me.Id;
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
        static PlayoffSeries Series(Ctx ctx, string name, Team high, Team low, int headStart = 0)
        {
            int winsNeeded = WinsNeeded(name);
            var series = new PlayoffSeries { Name = name, High = high, Low = low, HighWins = headStart, HeadStart = headStart };
            ctx.Run.Result.Series.Add(series);
            var me = ctx.Career.MyTeam;
            while (series.HighWins < winsNeeded && series.LowWins < winsNeeded && series.Games.Count < MaxGamesPerSeries)
            {
                // The better seed hosts: every wild card game, games 1, 2 and 5 of a best of five, 1, 2, 6 and 7 of the Korean Series.
                int n = series.Games.Count + 1;
                bool highHome = name == WildCard || n <= 2 || (name == KoreanSeries ? n >= 6 : n == 5);
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
            // Still level after the cap (or a wild card tie): the better seed goes through.
            if (series.HighWins <= series.LowWins && series.HighWins < winsNeeded && series.LowWins < winsNeeded) series.HighWins = series.LowWins + 1;
            return series;
        }

        static GameResult PlayGame(Ctx ctx, Team away, Team home, Player awayStarter, Player homeStarter)
        {
            var career = ctx.Career;
            int gameSeed = (int)(ctx.Master.NextDouble() * int.MaxValue);
            bool mine = away == career.MyTeam || home == career.MyTeam;
            if (!mine) return new GameSimulator(new SeededRandom(gameSeed)) { MaxInnings = PostseasonMaxInnings }.Play(away, home, awayStarter, homeStarter);

            ctx.Played++;
            var result = ctx.Run.Result;
            var day = new DayResult { Season = career.Season, Day = career.Day, Action = DailyAction.Rest };
            ctx.Run.Current = day;
            if (career.Me.IsInjured)
            {
                day.SatOut = true;
                day.MyGame = new GameSimulator(new SeededRandom(gameSeed)) { MaxInnings = PostseasonMaxInnings }.Play(away, home, awayStarter, homeStarter);
                return day.MyGame;
            }

            var gamePlayer = career.Me.ForGame(ctx.Master);
            var sim = new GameSimulator(new SeededRandom(gameSeed))
            {
                MaxInnings = PostseasonMaxInnings,
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
                    day.MyGame = new GameSimulator(new SeededRandom(gameSeed)) { MaxInnings = PostseasonMaxInnings }.Play(away, home, awayStarter, homeStarter);
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
