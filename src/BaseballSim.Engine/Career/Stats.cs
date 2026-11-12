namespace BaseballSim.Career
{
    /// <summary>Rate stats for record screens.</summary>
    public static class Stats
    {
        public static string Name(Stat stat)
        {
            switch (stat)
            {
                case Stat.Contact: return "컨택";
                case Stat.Power: return "파워";
                case Stat.Eye: return "선구안";
                case Stat.Speed: return "주루";
                case Stat.Defense: return "수비";
                case Stat.Velocity: return "구속";
                case Stat.Control: return "제구";
                case Stat.Breaking: return "변화구";
                default: return "체력";
            }
        }

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

        /// <summary>A pitcher's finished seasons plus the one in progress (1군 only).</summary>
        public static PitcherLine CareerPitching(Career career)
        {
            var total = new PitcherLine();
            foreach (var season in career.Me.History) if (season.Pitching != null) total.Add(season.Pitching);
            total.Add(career.SeasonPitching);
            return total;
        }
    }
}
