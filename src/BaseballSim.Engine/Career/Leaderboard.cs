using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>One line of a league leaderboard.</summary>
    public class LeaderRow
    {
        public int Rank;
        public string Name;
        public string Team;
        public string Value;
        public bool Mine;
    }

    /// <summary>A leaderboard: the top ten, plus my line underneath when I'm not in them.</summary>
    public class LeaderBoard
    {
        public string Title;
        public List<LeaderRow> Top = new List<LeaderRow>();
        /// <summary>My line when I rank below the top ten; null when I'm in the top ten or not on the board.</summary>
        public LeaderRow Me;
    }

    /// <summary>League leaders this season.</summary>
    public static class Leaderboard
    {
        public const int Size = 10;

        public const string Average = "타율";
        public const string HomeRuns = "홈런";
        public const string RunsBattedIn = "타점";
        public const string Strikeouts = "탈삼진";
        public const string Wins = "다승";
        public const string Saves = "세이브";
        public const string Era = "평균자책";
        public const string Runs = "득점";
        public const string Hits = "안타";
        public const string Steals = "도루";
        public const string Obp = "출루율";
        public const string Slg = "장타율";
        public const string Ops = "OPS";
        public const string Holds = "홀드";
        public const string Whip = "WHIP";

        public static readonly string[] BattingBoards = { Average, HomeRuns, RunsBattedIn, Runs, Hits, Steals, Obp, Slg, Ops };
        public static readonly string[] PitchingBoards = { Wins, Era, Strikeouts, Saves, Holds, Whip };
        public static readonly string[] Boards = BattingBoards.Concat(PitchingBoards).ToArray();

        static bool IsRate(string board) => board == Average || board == Era || board == Obp || board == Slg || board == Ops || board == Whip;

        public static List<LeaderBoard> All(League league, string myId) => Boards.Select(b => For(league, b, myId)).ToList();

        public static LeaderBoard For(League league, string board, string myId)
        {
            var bats = league.Batting.Values.Where(b => b.Line.PlateAppearances > 0);
            var arms = league.Pitching.Values.Where(p => p.Line.Outs > 0);
            switch (board)
            {
                case Average:
                    return Build(board, bats.Where(b => b.Line.PlateAppearances >= league.QualifyingPa)
                        .Select(b => (b.Id, b.Name, b.Team.Name, Key: Stats.Avg(b.Line), Text: Stats.Rate(Stats.Avg(b.Line)))), myId);
                case HomeRuns:
                    return Build(board, bats.Select(b => (b.Id, b.Name, b.Team.Name, Key: (double)b.Line.HomeRuns, Text: b.Line.HomeRuns.ToString())), myId);
                case RunsBattedIn:
                    return Build(board, bats.Select(b => (b.Id, b.Name, b.Team.Name, Key: (double)b.Line.RunsBattedIn, Text: b.Line.RunsBattedIn.ToString())), myId);
                case Strikeouts:
                    return Build(board, arms.Select(p => (p.Id, p.Name, p.Team.Name, Key: (double)p.Line.Strikeouts, Text: p.Line.Strikeouts.ToString())), myId);
                case Wins:
                    return Build(board, arms.Select(p => (p.Id, p.Name, p.Team.Name, Key: (double)p.Line.Wins, Text: $"{p.Line.Wins}승 {p.Line.Losses}패")), myId);
                case Saves:
                    return Build(board, arms.Select(p => (p.Id, p.Name, p.Team.Name, Key: (double)p.Line.Saves, Text: p.Line.Saves.ToString())), myId);
                case Era:
                    return Build(board, arms.Where(p => p.Line.Outs >= league.QualifyingOuts)
                        .Select(p => (p.Id, p.Name, p.Team.Name, Key: -p.Line.Era, Text: p.Line.EraText)), myId);
                case Runs:
                    return Build(board, bats.Select(b => (b.Id, b.Name, b.Team.Name, Key: (double)b.Line.Runs, Text: b.Line.Runs.ToString())), myId);
                case Hits:
                    return Build(board, bats.Select(b => (b.Id, b.Name, b.Team.Name, Key: (double)b.Line.Hits, Text: b.Line.Hits.ToString())), myId);
                case Steals:
                    return Build(board, bats.Select(b => (b.Id, b.Name, b.Team.Name, Key: (double)b.Line.StolenBases, Text: b.Line.StolenBases.ToString())), myId);
                case Obp:
                case Slg:
                case Ops:
                    Func<BattingLine, double> rate = board == Obp ? (Func<BattingLine, double>)Stats.Obp : board == Slg ? Stats.Slg : Stats.Ops;
                    return Build(board, bats.Where(b => b.Line.PlateAppearances >= league.QualifyingPa)
                        .Select(b => (b.Id, b.Name, b.Team.Name, Key: rate(b.Line), Text: Stats.Rate(rate(b.Line)))), myId);
                case Holds:
                    return Build(board, arms.Select(p => (p.Id, p.Name, p.Team.Name, Key: (double)p.Line.Holds, Text: p.Line.Holds.ToString())), myId);
                case Whip:
                    return Build(board, arms.Where(p => p.Line.Outs >= league.QualifyingOuts)
                        .Select(p => (p.Id, p.Name, p.Team.Name, Key: -p.Line.Whip, Text: p.Line.Whip.ToString("0.00"))), myId);
                default:
                    throw new ArgumentException("Unknown board: " + board);
            }
        }

        // Equal values share a rank (1, 2, 2, 4). A counting board leaves out zeros; nobody leads with none.
        static LeaderBoard Build(string title, IEnumerable<(string Id, string Name, string Team, double Key, string Text)> pool, string myId)
        {
            var sorted = pool.Where(x => IsRate(title) || x.Key > 0).OrderByDescending(x => x.Key).ToList();
            var board = new LeaderBoard { Title = title };
            int rank = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0 || sorted[i].Key != sorted[i - 1].Key) rank = i + 1;
                var x = sorted[i];
                var row = new LeaderRow { Rank = rank, Name = x.Name, Team = x.Team, Value = x.Text, Mine = x.Id == myId };
                if (i < Size) board.Top.Add(row);
                else if (row.Mine) { board.Me = row; break; }
            }
            return board;
        }
    }
}
