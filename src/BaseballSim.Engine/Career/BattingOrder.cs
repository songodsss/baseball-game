using System;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Where my player bats: rookies start at the bottom, and the manager moves him up as he hits.
    /// The top of the order gets more plate appearances, so a promotion means more chances.
    /// </summary>
    public partial class Career
    {
        // Rank among the nine hitters (0 = best) to lineup index: best hits 4th, then 3rd, 5th, 2nd...
        static readonly int[] SlotForRank = { 3, 2, 4, 1, 5, 6, 6, 7, 8 };

        public static string SlotName(int slot) => $"{slot + 1}번";

        /// <summary>How big a role a slot is, 0 = cleanup: 4th, 3rd, 5th, leadoff, 2nd, then down the order.</summary>
        public static int SlotStanding(int slot) => Array.IndexOf(new[] { 3, 2, 4, 0, 1, 5, 6, 7, 8 }, slot);

        /// <summary>OPS a hitter with these ratings would put up on average (calibrated from the plate-appearance model).</summary>
        static double ExpectedOps(int contact, int power, int eye) => 0.734 + ((contact + power + eye) / 3.0 - 50) * 0.0092;

        /// <summary>This season's OPS once there's enough of it, else what the ratings promise.</summary>
        double Rating(string id, BattingRatings ratings)
        {
            if (League.Batting.TryGetValue(id, out var season) && season.Line.PlateAppearances >= League.GamesPerSeason * 3 / 2)
                return Ops(season.Line);
            return ExpectedOps(ratings.Contact, ratings.Power, ratings.Eye);
        }

        /// <summary>Re-sets the batting order; returns the old slot if it changed.</summary>
        int? UpdateBattingOrder()
        {
            if (Me.IsPitcher) return null;
            var me = Me.ForGame(new SeededRandom(0));
            double mine = Rating(Me.Id, me.Batting);
            var others = MyTeam.Lineup.Where((p, i) => i != LineupSlot).Select(p => Rating(p.Id, p.Batting));
            int rank = others.Count(o => o > mine);

            // A cleanup hitter keeps the job through a small dip, so the order doesn't flap.
            if (LineupSlot == 3 && rank <= 1) return null;
            int slot = SlotForRank[Math.Min(rank, SlotForRank.Length - 1)];
            // A quick hitter near the top leads off instead.
            if (rank <= 3 && me.Batting.Speed >= 65 && me.Batting.Speed > me.Batting.Power) slot = 0;
            if (slot == LineupSlot) return null;
            int old = LineupSlot;
            LineupSlot = slot;
            if (slot == 3 && !SeasonNotes.Any(n => n.EndsWith("4번 타자"))) SeasonNotes.Add($"{MonthText} 4번 타자");
            return old;
        }
    }
}
