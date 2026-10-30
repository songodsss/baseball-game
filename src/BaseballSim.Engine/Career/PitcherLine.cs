using System;

namespace BaseballSim.Career
{
    /// <summary>A pitcher's numbers over a stretch: a game, a season, a career. All runs count as earned.</summary>
    public class PitcherLine
    {
        public int Games, Starts, Wins, Losses, Outs, Hits, Runs, Walks, Strikeouts, HomeRuns, Shutouts;

        public double Era => Outs == 0 ? 0 : Runs * 27.0 / Outs;
        public double Whip => Outs == 0 ? 0 : (Hits + Walks) * 3.0 / Outs;
        /// <summary>"123.1" style: whole innings and leftover outs.</summary>
        public string Innings => $"{Outs / 3}.{Outs % 3}";
        public string EraText => Outs == 0 ? "-.--" : Era.ToString("0.00");

        /// <summary>A starter's call: the win if he went five and left his side ahead of what he gave up, the loss if he gave up as many as his side scored in a defeat.</summary>
        public static (bool Win, bool Loss) Decision(PitchingLine line, int ourRuns, int theirRuns)
        {
            bool win = ourRuns > theirRuns && line.OutsRecorded >= 15 && line.Runs < ourRuns;
            bool loss = ourRuns < theirRuns && line.Runs > 0 && line.Runs >= ourRuns;
            return (win, loss);
        }

        public void Add(PitchingLine line, bool start, bool win, bool loss)
        {
            Games++;
            if (start) Starts++;
            if (win) Wins++;
            if (loss) Losses++;
            Outs += line.OutsRecorded; Hits += line.Hits; Runs += line.Runs;
            Walks += line.Walks; Strikeouts += line.Strikeouts; HomeRuns += line.HomeRuns;
            if (start && line.OutsRecorded >= 27 && line.Runs == 0) Shutouts++;
        }

        public void Add(PitcherLine l)
        {
            Games += l.Games; Starts += l.Starts; Wins += l.Wins; Losses += l.Losses; Outs += l.Outs; Hits += l.Hits;
            Runs += l.Runs; Walks += l.Walks; Strikeouts += l.Strikeouts; HomeRuns += l.HomeRuns; Shutouts += l.Shutouts;
        }

        public override string ToString() =>
            $"{Wins}승 {Losses}패 · 평균자책 {EraText} · {Innings}이닝 · 탈삼진 {Strikeouts}";
    }

    /// <summary>One pitcher's numbers for the season so far, for leaderboards and awards.</summary>
    public class PitcherSeason
    {
        public string Id;
        public string Name;
        public Team Team;
        public PitcherLine Line = new PitcherLine();
    }
}
