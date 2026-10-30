using System.Collections.Generic;

namespace BaseballSim
{
    /// <summary>One runner's trip during a play. Bases are 0-2 (To == From means the runner held); see the constants for the rest.</summary>
    public class RunnerMove
    {
        public const int BattersBox = -1;
        public const int Scored = 3;
        public const int Out = -2;

        public Player Runner;
        public int From;
        public int To;
    }

    /// <summary>One event of a game, ready for the match screen to show.</summary>
    public class ReplayStep
    {
        public int Index;
        public GameEvent Event;
        public string Text;
        public string HalfLabel;
        public bool StartsHalf;
        /// <summary>My player is the batter.</summary>
        public bool IsMine;
        public bool IsHighlight;
        /// <summary>Lead runner first, batter last. Empty for a pitching change.</summary>
        public List<RunnerMove> Moves = new List<RunnerMove>();
    }

    /// <summary>Runs per inning and totals, as a scoreboard shows them at some point in the game.</summary>
    public class LineScore
    {
        /// <summary>Index 0 is the 1st inning. Null means that half hasn't been played (yet, or at all).</summary>
        public List<int?> Away = new List<int?>();
        public List<int?> Home = new List<int?>();
        public int AwayRuns, HomeRuns, AwayHits, HomeHits;
        public int Innings => Away.Count;
    }

    /// <summary>
    /// Steps through a finished GameResult one event at a time, so the match screen can replay it
    /// at any speed or jump ahead. All the bookkeeping lives here; the screen only animates.
    /// </summary>
    public class MatchReplay
    {
        public readonly GameResult Game;
        public readonly Player Mine;
        public readonly List<ReplayStep> Steps = new List<ReplayStep>();

        /// <summary>Index of the step shown last; -1 before the first pitch.</summary>
        public int Position { get; private set; } = -1;
        public bool IsFinished => Position >= Steps.Count - 1;
        public ReplayStep Current => Position < 0 ? null : Steps[Position];

        public MatchReplay(GameResult game, Player mine = null)
        {
            Game = game;
            Mine = mine;
            for (int i = 0; i < game.Events.Count; i++)
            {
                var ev = game.Events[i];
                var prev = i == 0 ? null : game.Events[i - 1];
                Steps.Add(new ReplayStep
                {
                    Index = i,
                    Event = ev,
                    Text = Commentary.Describe(ev) + (ev.Note != null ? $" ({ev.Note})" : ""),
                    HalfLabel = Commentary.HalfInning(ev.Inning, ev.IsTopHalf),
                    StartsHalf = prev == null || prev.Inning != ev.Inning || prev.IsTopHalf != ev.IsTopHalf,
                    IsMine = mine != null && ev.Kind == EventKind.PlateAppearance && ev.Batter == mine,
                    IsHighlight = IsHighlight(ev),
                    Moves = MovesFor(ev),
                });
            }
        }

        public ReplayStep Advance()
        {
            if (IsFinished) return null;
            Position++;
            return Steps[Position];
        }

        /// <summary>Index of my next plate appearance at or after <paramref name="from"/>, or -1.</summary>
        public int NextMineIndex(int from)
        {
            for (int i = System.Math.Max(0, from); i < Steps.Count; i++)
                if (Steps[i].IsMine) return i;
            return -1;
        }

        public static bool IsHit(Outcome o) =>
            o == Outcome.Single || o == Outcome.Double || o == Outcome.Triple || o == Outcome.HomeRun;

        public static bool IsHighlight(GameEvent e) =>
            e.Kind == EventKind.PlateAppearance &&
            (e.Outcome == Outcome.HomeRun || e.Outcome == Outcome.Double || e.Outcome == Outcome.Triple || e.RunnersScored.Count > 0);

        /// <summary>The scoreboard after step <paramref name="index"/> (-1 = before the game).</summary>
        public LineScore ScoreAfter(int index)
        {
            var score = new LineScore();
            for (int i = 0; i <= index && i < Steps.Count; i++)
            {
                var ev = Steps[i].Event;
                while (score.Away.Count < ev.Inning)
                {
                    score.Away.Add(null);
                    score.Home.Add(null);
                }
                var half = ev.IsTopHalf ? score.Away : score.Home;
                int runs = ev.RunnersScored.Count;
                half[ev.Inning - 1] = (half[ev.Inning - 1] ?? 0) + runs;
                bool hit = ev.Kind == EventKind.PlateAppearance && IsHit(ev.Outcome);
                if (ev.IsTopHalf) { score.AwayRuns += runs; if (hit) score.AwayHits++; }
                else { score.HomeRuns += runs; if (hit) score.HomeHits++; }
            }
            return score;
        }

        static List<RunnerMove> MovesFor(GameEvent ev)
        {
            var moves = new List<RunnerMove>();
            if (ev.Kind != EventKind.PlateAppearance) return moves;

            for (int b = 2; b >= 0; b--)
                if (ev.BasesBefore[b] != null) moves.Add(new RunnerMove { Runner = ev.BasesBefore[b], From = b, To = Destination(ev, ev.BasesBefore[b]) });
            moves.Add(new RunnerMove { Runner = ev.Batter, From = RunnerMove.BattersBox, To = Destination(ev, ev.Batter) });
            return moves;
        }

        static int Destination(GameEvent ev, Player runner)
        {
            int after = System.Array.IndexOf(ev.BasesAfter, runner);
            if (after >= 0) return after;
            return ev.RunnersScored.Contains(runner) ? RunnerMove.Scored : RunnerMove.Out;
        }
    }
}
