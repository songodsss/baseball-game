using System;

namespace BaseballSim
{
    public enum Pitch
    {
        Fastball,
        Breaking,
    }

    /// <summary>What my player looks for in a big at bat.</summary>
    public enum Approach
    {
        /// <summary>No read: plain odds.</summary>
        Normal,
        SitFastball,
        SitBreaking,
        /// <summary>Take pitches: more walks, less damage.</summary>
        Patient,
    }

    /// <summary>
    /// A big at bat for my player, paused for the user to pick an approach. The pitch the
    /// pitcher will go with is already decided; the user sees his tendency and a hunch.
    /// </summary>
    public class AtBatSituation
    {
        public int Inning;
        public bool IsTopHalf;
        public int Outs;
        /// <summary>Runners on first, second, third.</summary>
        public bool[] Bases;
        public int MyRuns, TheirRuns;
        public Player Pitcher;
        /// <summary>Share of fastballs this pitcher throws in big spots, from his ratings.</summary>
        public double FastballShare;
        /// <summary>My player's hunch for this pitch; right more often with a better eye.</summary>
        public Pitch Hunch;
        public double HunchAccuracy;
        internal Pitch Coming;
    }

    public static class AtBatChoice
    {
        // Career traits that work in big at bats.
        public const string Reader = "reader";
        public const string Clutch = "clutch";
        public const string Gambler = "gambler";

        /// <summary>Scoring position, or a close game late.</summary>
        public static bool IsBig(int inning, bool[] bases, int scoreGap) =>
            bases[1] || bases[2] || inning >= 7 && Math.Abs(scoreGap) <= 1;

        /// <summary>Power pitchers lean on the fastball, finesse pitchers on breaking stuff: 35% to 75%.</summary>
        public static double FastballShare(Player pitcher)
        {
            int lean = pitcher.Pitching.Velocity - pitcher.Pitching.Breaking;
            return Math.Max(0.35, Math.Min(0.75, 0.55 + lean / 100.0));
        }

        /// <summary>Hunches are right 55% of the time at eye 20, up to 90% at eye 100.</summary>
        public static double HunchAccuracy(int eye) => 0.55 + 0.35 * Math.Max(0, Math.Min(80, eye - 20)) / 80.0;

        public static AtBatSituation Read(Player batter, Player pitcher, IRandom random)
        {
            double share = FastballShare(pitcher);
            var coming = random.NextDouble() < share ? Pitch.Fastball : Pitch.Breaking;
            double accuracy = Math.Min(0.95, HunchAccuracy(batter.Batting.Eye) + (batter.Traits.Contains(Reader) ? 0.15 : 0));
            var other = coming == Pitch.Fastball ? Pitch.Breaking : Pitch.Fastball;
            return new AtBatSituation
            {
                Pitcher = pitcher,
                FastballShare = share,
                Coming = coming,
                HunchAccuracy = accuracy,
                Hunch = random.NextDouble() < accuracy ? coming : other,
            };
        }

        public static bool GuessedRight(Approach approach, Pitch coming) =>
            approach == Approach.SitFastball && coming == Pitch.Fastball || approach == Approach.SitBreaking && coming == Pitch.Breaking;

        /// <summary>
        /// A right guess turns the at bat into a hitter's count; a wrong one leaves the batter fooled.
        /// Sitting on the likelier pitch pays on average, and a good eye pays more.
        /// </summary>
        public static void Apply(OutcomeOdds odds, Approach approach, Pitch coming, Player batter = null)
        {
            if (approach == Approach.Normal) return;
            bool clutch = batter != null && batter.Traits.Contains(Clutch);
            bool gambler = batter != null && batter.Traits.Contains(Gambler);
            if (approach == Approach.Patient)
                Scale(odds, strikeout: 1.15, walk: 1.7, homeRun: 0.7, single: 0.9, extra: 0.85);
            else if (GuessedRight(approach, coming))
                Scale(odds, strikeout: 0.55, walk: 0.8, homeRun: clutch ? 2.4 : 1.9, single: 1.25, extra: clutch ? 1.8 : 1.5);
            else if (gambler)
                Scale(odds, strikeout: 1.3, walk: 1.0, homeRun: 0.75, single: 0.9, extra: 0.85);
            else
                Scale(odds, strikeout: 1.6, walk: 1.0, homeRun: 0.5, single: 0.8, extra: 0.7);
        }

        public static string Note(Approach approach, Pitch coming)
        {
            switch (approach)
            {
                case Approach.Patient: return "신중하게";
                case Approach.SitFastball: return coming == Pitch.Fastball ? "직구 노림 적중!" : "직구 노렸는데 변화구";
                case Approach.SitBreaking: return coming == Pitch.Breaking ? "변화구 노림 적중!" : "변화구 노렸는데 직구";
                default: return null;
            }
        }

        static void Scale(OutcomeOdds o, double strikeout, double walk, double homeRun, double single, double extra)
        {
            o.Strikeout *= strikeout;
            o.Walk *= walk;
            o.HomeRun *= homeRun;
            o.Single *= single;
            o.Double *= extra;
            o.Triple *= extra;
            double total = o.Strikeout + o.Walk + o.HitByPitch + o.HomeRun + o.Triple + o.Double + o.Single + o.GroundOut + o.FlyOut + o.LineOut;
            o.Strikeout /= total; o.Walk /= total; o.HitByPitch /= total; o.HomeRun /= total; o.Triple /= total;
            o.Double /= total; o.Single /= total; o.GroundOut /= total; o.FlyOut /= total; o.LineOut /= total;
        }
    }
}
