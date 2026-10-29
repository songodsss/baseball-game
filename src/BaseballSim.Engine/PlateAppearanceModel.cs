using System;

namespace BaseballSim
{
    public enum Outcome
    {
        Strikeout,
        Walk,
        HitByPitch,
        Single,
        Double,
        Triple,
        HomeRun,
        GroundOut,
        FlyOut,
        LineOut,
    }

    /// <summary>Chances of each outcome for one batter facing one pitcher.</summary>
    public class OutcomeOdds
    {
        public double Strikeout, Walk, HitByPitch, HomeRun, Triple, Double, Single, GroundOut, FlyOut, LineOut;

        public Outcome Pick(IRandom random)
        {
            double roll = random.NextDouble();
            if ((roll -= Strikeout) < 0) return Outcome.Strikeout;
            if ((roll -= Walk) < 0) return Outcome.Walk;
            if ((roll -= HitByPitch) < 0) return Outcome.HitByPitch;
            if ((roll -= HomeRun) < 0) return Outcome.HomeRun;
            if ((roll -= Triple) < 0) return Outcome.Triple;
            if ((roll -= Double) < 0) return Outcome.Double;
            if ((roll -= Single) < 0) return Outcome.Single;
            if ((roll -= GroundOut) < 0) return Outcome.GroundOut;
            if ((roll -= FlyOut) < 0) return Outcome.FlyOut;
            return Outcome.LineOut;
        }
    }

    /// <summary>
    /// Turns ratings into outcome odds. Every rating is centred so 50 means league average,
    /// and each one scales a base rate up or down exponentially, which keeps odds positive
    /// and makes a 90 feel clearly different from a 50 without breaking the game.
    /// </summary>
    public static class PlateAppearanceModel
    {
        // League-average rates per plate appearance when every rating is 50.
        const double BaseStrikeout = 0.19;
        const double BaseWalk = 0.078;
        const double BaseHitByPitch = 0.01;
        const double BaseHomeRun = 0.023;
        const double BaseBabip = 0.315; // hits on balls in play, excluding home runs
        const double BaseDoubleShare = 0.21;
        const double BaseTripleShare = 0.02;

        /// <param name="fieldingDefense">Average defense rating of the fielding team.</param>
        public static OutcomeOdds Compute(Player batter, Player pitcher, double fieldingDefense, double fatigue)
        {
            var b = batter.Batting;
            var p = pitcher.Pitching;
            double contact = Centre(b.Contact, batter.Condition);
            double power = Centre(b.Power, batter.Condition);
            double eye = Centre(b.Eye, batter.Condition);
            double speed = Centre(b.Speed, batter.Condition);
            // A tired pitcher loses stuff and command.
            double velocity = Centre(p.Velocity, pitcher.Condition) - fatigue;
            double control = Centre(p.Control, pitcher.Condition) - fatigue;
            double breaking = Centre(p.Breaking, pitcher.Condition) - fatigue;
            double defense = (fieldingDefense - 50) / 50;

            var odds = new OutcomeOdds
            {
                Strikeout = BaseStrikeout * Math.Exp(0.6 * velocity + 0.25 * breaking - 0.6 * contact),
                Walk = BaseWalk * Math.Exp(0.6 * eye - 0.7 * control),
                HitByPitch = BaseHitByPitch * Math.Exp(-0.3 * control),
                HomeRun = BaseHomeRun * Math.Exp(1.0 * power - 0.4 * breaking - 0.2 * velocity),
            };

            double inPlay = Math.Max(0.2, 1 - odds.Strikeout - odds.Walk - odds.HitByPitch - odds.HomeRun);
            double babip = Clamp(BaseBabip * Math.Exp(0.25 * contact + 0.1 * speed - 0.2 * breaking - 0.15 * defense), 0.18, 0.42);
            double hits = inPlay * babip;
            double outs = inPlay - hits;

            double doubleShare = BaseDoubleShare * Math.Exp(0.4 * power);
            double tripleShare = BaseTripleShare * Math.Exp(0.8 * speed);
            odds.Double = hits * doubleShare;
            odds.Triple = hits * tripleShare;
            odds.Single = hits - odds.Double - odds.Triple;

            // Power hitters lift the ball; others hit more grounders.
            double flyShare = Clamp(0.36 + 0.08 * power, 0.2, 0.55);
            odds.FlyOut = outs * flyShare;
            odds.LineOut = outs * 0.12;
            odds.GroundOut = outs - odds.FlyOut - odds.LineOut;

            Normalise(odds);
            return odds;
        }

        static double Centre(int rating, int condition) => (Clamp(rating + condition, 1, 100) - 50) / 50.0;

        static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        static void Normalise(OutcomeOdds o)
        {
            double total = o.Strikeout + o.Walk + o.HitByPitch + o.HomeRun + o.Triple + o.Double + o.Single
                + o.GroundOut + o.FlyOut + o.LineOut;
            o.Strikeout /= total; o.Walk /= total; o.HitByPitch /= total; o.HomeRun /= total; o.Triple /= total;
            o.Double /= total; o.Single /= total; o.GroundOut /= total; o.FlyOut /= total; o.LineOut /= total;
        }
    }
}
