using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum GoalTier { Easy, Normal, Hard }

    public enum GoalKind { Avg, HomeRuns, RunsBattedIn, Hits, Ops, Wins, Strikeouts, Era }

    /// <summary>
    /// A target the player commits to before a season. Harder targets pay more, so picking one is a
    /// bet on how the season will go. Targets are set from what the player's ratings promise.
    /// </summary>
    public class SeasonGoal
    {
        public GoalKind Kind;
        public GoalTier Tier;
        public double Target;

        public static readonly string[] TierNames = { "쉬움", "보통", "어려움" };

        bool IsRate => Kind == GoalKind.Avg || Kind == GoalKind.Ops || Kind == GoalKind.Era;
        public static readonly GoalKind[] HitterKinds = { GoalKind.Avg, GoalKind.HomeRuns, GoalKind.RunsBattedIn, GoalKind.Hits, GoalKind.Ops };
        public static readonly GoalKind[] PitcherKinds = { GoalKind.Wins, GoalKind.Strikeouts, GoalKind.Era };

        public string Text
        {
            get
            {
                switch (Kind)
                {
                    case GoalKind.Avg: return $"타율 {Stats.Rate(Target)} 이상 (규정 타석)";
                    case GoalKind.Ops: return $"OPS {Stats.Rate(Target)} 이상 (규정 타석)";
                    case GoalKind.HomeRuns: return $"홈런 {Target:0}개";
                    case GoalKind.RunsBattedIn: return $"타점 {Target:0}개";
                    case GoalKind.Wins: return $"{Target:0}승";
                    case GoalKind.Strikeouts: return $"탈삼진 {Target:0}개";
                    case GoalKind.Era: return $"평균자책 {Target:0.00} 이하 (규정 이닝)";
                    default: return $"안타 {Target:0}개";
                }
            }
        }

        public double Current(Career career) => Current(career.SeasonLine, career.SeasonPitching);

        public double Current(BattingLine l, PitcherLine p)
        {
            switch (Kind)
            {
                case GoalKind.Wins: return p.Wins;
                case GoalKind.Strikeouts: return p.Strikeouts;
                case GoalKind.Era: return p.Era;
                case GoalKind.Avg: return Stats.Avg(l);
                case GoalKind.Ops: return Career.Ops(l);
                case GoalKind.HomeRuns: return l.HomeRuns;
                case GoalKind.RunsBattedIn: return l.RunsBattedIn;
                default: return l.Hits;
            }
        }

        public string Progress(Career career) => Progress(career.SeasonLine, career.SeasonPitching);

        /// <summary>Where a season stands against the goal, e.g. "12/20" or ".312"; also for a finished season's record.</summary>
        public string Progress(BattingLine l, PitcherLine p)
        {
            p = p ?? new PitcherLine();
            return Kind == GoalKind.Era ? p.EraText : IsRate ? Stats.Rate(Current(l, p)) : $"{Current(l, p):0}/{Target:0}";
        }

        public bool Met(Career career)
        {
            if (Kind == GoalKind.Era)
                return career.SeasonPitching.Outs >= career.League.QualifyingOuts && Current(career) <= Target;
            return Current(career) >= Target && (!IsRate || career.SeasonLine.PlateAppearances >= career.League.QualifyingPa);
        }

        /// <summary>Money as a share of salary plus a flat amount.</summary>
        public int Money(int salary) => Tier == GoalTier.Hard ? salary / 5 + 3000 : Tier == GoalTier.Normal ? salary / 10 + 1500 : salary / 20 + 500;

        /// <summary>Exp added to every stat.</summary>
        public int Exp => Tier == GoalTier.Hard ? 400 : Tier == GoalTier.Normal ? 200 : 100;
    }

    public partial class Career
    {
        /// <summary>This season's goal, picked before the first game; null if none was picked.</summary>
        public SeasonGoal Goal;
        List<SeasonGoal> _goalOffer;

        /// <summary>One goal per tier, of different kinds, offered before a season's first game. Null once the season is under way or a goal is set.</summary>
        public List<SeasonGoal> GoalOffer
        {
            get
            {
                if (Day != 0 || Goal != null || InFarm) return null;
                return _goalOffer ?? (_goalOffer = MakeGoalOffer());
            }
        }

        public void ChooseGoal(SeasonGoal goal)
        {
            if (GoalOffer == null || !GoalOffer.Contains(goal)) throw new InvalidOperationException("Not on offer.");
            Goal = goal;
            _goalOffer = null;
        }

        List<SeasonGoal> MakeGoalOffer() => Me.IsPitcher ? MakePitcherGoalOffer() : MakeHitterGoalOffer();

        /// <summary>A pitcher's three goals, one of each kind, from what his stuff promises over a season of starts.</summary>
        List<SeasonGoal> MakePitcherGoalOffer()
        {
            var me = Me.ForGame(new SeededRandom(Season)).Pitching;
            double starts = League.GamesPerSeason / (double)StartEvery;
            double era = ExpectedEra(me.Velocity, me.Control, me.Breaking);
            double wins = starts * Math.Max(0.15, 0.40 + (LeagueEra - era) * 0.08);
            double strikeouts = starts * Math.Max(1, 4.35 + 0.039 * (me.Velocity - 46.5));
            double TargetFor(GoalKind kind, GoalTier tier)
            {
                int t = (int)tier;
                if (kind == GoalKind.Era) return Math.Round(Math.Max(1.0, era + new[] { 0.60, 0.0, -0.60 }[t]), 2);
                return Math.Max(1, Math.Round((kind == GoalKind.Wins ? wins : strikeouts) * new[] { 0.8, 1.0, 1.2 }[t]));
            }
            var kinds = SeasonGoal.PitcherKinds.OrderBy(_ => _random.NextDouble()).ToList();
            return Enum.GetValues(typeof(GoalTier)).Cast<GoalTier>()
                .Select((tier, i) => new SeasonGoal { Kind = kinds[i], Tier = tier, Target = TargetFor(kinds[i], tier) })
                .ToList();
        }

        List<SeasonGoal> MakeHitterGoalOffer()
        {
            // What a season looks like for these ratings against average pitching, from the at-bat model.
            var me = Me.ForGame(new SeededRandom(Season));
            me.Condition = 0;
            var odds = PlateAppearanceModel.Compute(me, new Player("avg", "avg") { Pitching = new PitchingRatings() }, 50, 0);
            double pa = League.GamesPerSeason * (4.6 - 0.1 * LineupSlot);
            double ab = pa * (1 - odds.Walk - odds.HitByPitch);
            double hitRate = odds.Single + odds.Double + odds.Triple + odds.HomeRun;
            double avg = hitRate * pa / ab;
            double obp = hitRate + odds.Walk + odds.HitByPitch;
            double slg = (odds.Single + 2 * odds.Double + 3 * odds.Triple + 4 * odds.HomeRun) * pa / ab;
            double homeRuns = odds.HomeRun * pa, hits = hitRate * pa;
            double rbi = homeRuns * 1.6 + (hits - homeRuns) * 0.3;

            double Expected(GoalKind kind)
            {
                switch (kind)
                {
                    case GoalKind.Avg: return avg;
                    case GoalKind.Ops: return obp + slg;
                    case GoalKind.HomeRuns: return homeRuns;
                    case GoalKind.RunsBattedIn: return rbi;
                    default: return hits;
                }
            }
            double TargetFor(GoalKind kind, GoalTier tier)
            {
                double e = Expected(kind);
                int t = (int)tier;
                if (kind == GoalKind.Avg) return Math.Round(e + new[] { -0.025, 0.005, 0.030 }[t], 3);
                if (kind == GoalKind.Ops) return Math.Round(e + new[] { -0.060, 0.015, 0.080 }[t], 3);
                return Math.Max(1, Math.Round(e * new[] { 0.8, 1.0, 1.2 }[t]));
            }

            var kinds = SeasonGoal.HitterKinds.OrderBy(_ => _random.NextDouble()).Take(3).ToList();
            return Enum.GetValues(typeof(GoalTier)).Cast<GoalTier>()
                .Select((tier, i) => new SeasonGoal { Kind = kinds[i], Tier = tier, Target = TargetFor(kinds[i], tier) })
                .ToList();
        }

        /// <summary>At season end: pays out a met goal. Returns whether it was met (false with no goal).</summary>
        bool SettleGoal(SeasonSummary summary)
        {
            var goal = Goal;
            Goal = null;
            _goalOffer = null;
            summary.Goal = goal;
            if (goal == null || !goal.Met(this)) return false;
            summary.GoalMoney = goal.Money(Me.Salary);
            Me.Money += summary.GoalMoney;
            foreach (var stat in Me.MyStats) Me.GainExp(stat, goal.Exp);
            summary.Record.Notes.Add($"시즌 목표 달성 ({SeasonGoal.TierNames[(int)goal.Tier]}): {goal.Text}");
            return true;
        }
    }
}
