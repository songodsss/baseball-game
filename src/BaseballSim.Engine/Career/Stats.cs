namespace BaseballSim.Career
{
    /// <summary>Rate stats for record screens.</summary>
    public static class Stats
    {
        public static double Avg(BattingLine l) => l.AtBats == 0 ? 0 : (double)l.Hits / l.AtBats;

        public static double Obp(BattingLine l) => l.PlateAppearances == 0 ? 0 : (double)(l.Hits + l.Walks) / l.PlateAppearances;

        public static double Slg(BattingLine l) =>
            l.AtBats == 0 ? 0 : (double)(l.Hits + l.Doubles + 2 * l.Triples + 3 * l.HomeRuns) / l.AtBats;

        public static double Ops(BattingLine l) => Career.Ops(l);

        /// <summary>Baseball style: .312, or 1.024 for an OPS over one.</summary>
        public static string Rate(double value) => value >= 1 ? value.ToString("0.000") : value.ToString(".000");

        /// <summary>Every finished season plus the one in progress.</summary>
        public static BattingLine CareerTotal(Career career)
        {
            var total = new BattingLine();
            foreach (var season in career.Me.History) Career.Accumulate(total, season.Line);
            Career.Accumulate(total, career.SeasonLine);
            return total;
        }
    }
}
