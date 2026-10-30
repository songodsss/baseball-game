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
    }

    /// <summary>End-of-season titles from the league's batting lines.</summary>
    public static class Awards
    {
        public const string Mvp = "MVP";
        public const string Rookie = "신인왕";

        /// <summary>
        /// Rate titles need a qualifying number of plate appearances; counting titles don't.
        /// Ties go to whoever got there with fewer plate appearances.
        /// </summary>
        public static List<SeasonAward> For(League league, string myId, bool myRookieSeason)
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
            // MVP: best OPS among qualified hitters, a little extra for playing on a winning team.
            var mvp = qualified.OrderByDescending(b => Stats.Ops(b.Line) + 0.1 * league.Standings[b.Team].WinPct).FirstOrDefault();
            if (mvp != null)
                awards.Add(new SeasonAward
                {
                    Title = Mvp, Winner = mvp.Name, Team = mvp.Team.Name, Value = "OPS " + Stats.Rate(Stats.Ops(mvp.Line)), Mine = mvp.Id == myId,
                });

            // Only my player is ever a rookie: the rest of the league is made of established players.
            if (myRookieSeason && league.Batting.TryGetValue(myId, out var me)
                && me.Line.PlateAppearances >= league.QualifyingPa && Stats.Ops(me.Line) >= 0.700)
                awards.Add(new SeasonAward { Title = Rookie, Winner = me.Name, Team = me.Team.Name, Value = "OPS " + Stats.Rate(Stats.Ops(me.Line)), Mine = true });

            return awards;
        }
    }
}
