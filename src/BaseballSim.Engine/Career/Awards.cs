using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class SeasonAward
    {
        public string Title;
        public string Winner;
        public string Team;
        /// <summary>The winning number, formatted: ".342", "18".</summary>
        public string Value;
        public bool Mine;
        /// <summary>For golden gloves.</summary>
        public Position Position;
    }

    /// <summary>End-of-season titles from the league's batting lines.</summary>
    public static class Awards
    {
        public const string Mvp = "MVP";
        public const string Rookie = "신인왕";
        public const string GoldenGlove = "골든글러브";
        public const string Wins = "다승왕";
        public const string EraTitle = "평균자책점 1위";
        public const string StrikeoutTitle = "탈삼진왕";

        /// <summary>
        /// Rate titles need a qualifying number of plate appearances; counting titles don't.
        /// Ties go to whoever got there with fewer plate appearances.
        /// </summary>
        /// <param name="rookieIds">Players in their first season: my player and the rival, the only rookies in the league.</param>
        /// <param name="fieldingOf">Position and defense rating by player id, for the golden gloves.</param>
        public static List<SeasonAward> For(League league, string myId, IEnumerable<string> rookieIds, Func<string, (Position Position, int Defense)> fieldingOf = null)
        {
            var all = league.Batting.Values.Where(b => b.Line.PlateAppearances > 0).ToList();
            var qualified = all.Where(b => b.Line.PlateAppearances >= league.QualifyingPa).ToList();
            var awards = new List<SeasonAward>();

            void Add(string title, IEnumerable<BatterSeason> pool, Func<BattingLine, double> key, Func<double, string> format)
            {
                var best = pool.OrderByDescending(b => key(b.Line)).ThenBy(b => b.Line.PlateAppearances).FirstOrDefault();
                if (best == null || key(best.Line) <= 0) return;
                awards.Add(new SeasonAward
                {
                    Title = title, Winner = best.Name, Team = best.Team.Name, Value = format(key(best.Line)), Mine = best.Id == myId,
                });
            }

            string Count(double v) => ((int)v).ToString();
            Add("타격왕", qualified, Stats.Avg, Stats.Rate);
            Add("홈런왕", all, l => l.HomeRuns, Count);
            Add("타점왕", all, l => l.RunsBattedIn, Count);
            Add("최다안타", all, l => l.Hits, Count);
            Add("출루율 1위", qualified, Stats.Obp, Stats.Rate);

            // Pitching titles: wins and strikeouts for anyone, ERA for qualified innings only.
            var arms = league.Pitching.Values.Where(p => p.Line.Outs > 0).ToList();
            var qualifiedArms = arms.Where(p => p.Line.Outs >= league.QualifyingOuts).ToList();
            void AddArm(string title, IEnumerable<PitcherSeason> pool, Func<PitcherLine, double> key, Func<PitcherLine, string> format)
            {
                var best = pool.OrderByDescending(p => key(p.Line)).ThenByDescending(p => p.Line.Outs).FirstOrDefault();
                if (best == null) return;
                awards.Add(new SeasonAward { Title = title, Winner = best.Name, Team = best.Team.Name, Value = format(best.Line), Mine = best.Id == myId });
            }
            AddArm(Wins, arms.Where(p => p.Line.Wins > 0), l => l.Wins, l => $"{l.Wins}승");
            AddArm(EraTitle, qualifiedArms, l => -l.Era, l => l.EraText);
            AddArm(StrikeoutTitle, arms.Where(p => p.Line.Strikeouts > 0), l => l.Strikeouts, l => l.Strikeouts.ToString());

            // MVP: best OPS among qualified hitters, a little extra for playing on a winning team. A
            // qualified pitcher counts on the same scale from his ERA, and needs a little more to win it.
            double WinBonus(Team t) => 0.1 * league.Standings[t].WinPct;
            var mvp = qualified.Select(b => (b.Id, b.Name, b.Team, Score: Stats.Ops(b.Line) + WinBonus(b.Team), Value: "OPS " + Stats.Rate(Stats.Ops(b.Line))))
                .Concat(qualifiedArms.Select(p => (p.Id, p.Name, p.Team, Score: Career.PitchingForm(p.Line) - 0.05 + WinBonus(p.Team), Value: $"{p.Line.Wins}승 · 평균자책 {p.Line.EraText}")))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (mvp.Name != null)
                awards.Add(new SeasonAward { Title = Mvp, Winner = mvp.Name, Team = mvp.Team.Name, Value = mvp.Value, Mine = mvp.Id == myId });

            // The best qualified rookie with decent numbers; the rest of the league is made of established players.
            var rookies = rookieIds.ToList();
            var rookie = rookies
                .Select(id => league.Batting.TryGetValue(id, out var b) ? b : null)
                .Where(b => b != null && b.Line.PlateAppearances >= league.QualifyingPa && Stats.Ops(b.Line) >= 0.700)
                .Select(b => (b.Id, b.Name, b.Team, Score: Stats.Ops(b.Line), Value: "OPS " + Stats.Rate(Stats.Ops(b.Line))))
                .Concat(rookies
                    .Select(id => league.Pitching.TryGetValue(id, out var p) ? p : null)
                    .Where(p => p != null && p.Line.Outs >= league.QualifyingOuts && Career.PitchingForm(p.Line) >= 0.700)
                    .Select(p => (p.Id, p.Name, p.Team, Score: Career.PitchingForm(p.Line), Value: $"{p.Line.Wins}승 · 평균자책 {p.Line.EraText}")))
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();
            if (rookie.Name != null)
                awards.Add(new SeasonAward { Title = Rookie, Winner = rookie.Name, Team = rookie.Team.Name, Value = rookie.Value, Mine = rookie.Id == myId });

            // Golden gloves by position: hitting first, the glove counting more where fielding is harder.
            if (fieldingOf != null)
            {
                var pitcher = qualifiedArms.OrderByDescending(p => Career.PitchingForm(p.Line)).FirstOrDefault();
                if (pitcher != null)
                    awards.Add(new SeasonAward
                    {
                        Title = GoldenGlove, Winner = pitcher.Name, Team = pitcher.Team.Name, Position = Position.Pitcher,
                        Value = $"투수 · {pitcher.Line.Wins}승 · 평균자책 {pitcher.Line.EraText}", Mine = pitcher.Id == myId,
                    });
                foreach (var position in Positions.Fielders)
                {
                    var winners = qualified
                        .Select(b => (Batter: b, Fielding: fieldingOf(b.Id)))
                        .Where(x => x.Fielding.Position == position)
                        .OrderByDescending(x => Stats.Ops(x.Batter.Line) + (x.Fielding.Defense - 50) * Positions.DefenseWeight(position))
                        .Take(Positions.GoldenGloves(position));
                    foreach (var (b, f) in winners)
                        awards.Add(new SeasonAward
                        {
                            Title = GoldenGlove, Winner = b.Name, Team = b.Team.Name, Position = position,
                            Value = $"{Positions.Name(position)} · OPS {Stats.Rate(Stats.Ops(b.Line))}" + (position == Position.DesignatedHitter ? "" : $" · 수비 {f.Defense}"),
                            Mine = b.Id == myId,
                        });
                }
            }

            return awards;
        }
    }
}
