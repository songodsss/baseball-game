using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class DerbyEntrant
    {
        public string Name;
        public string Team;
        public bool Mine;
        public int SeasonHomeRuns;
        /// <summary>Home runs in each round reached: quarterfinal, semifinal, final.</summary>
        public List<int> Rounds = new List<int>();
    }

    public class DerbyResult
    {
        /// <summary>Eight sluggers, seeded by first-half home runs.</summary>
        public List<DerbyEntrant> Entrants = new List<DerbyEntrant>();
        public DerbyEntrant Winner;
        public bool MyIn => Entrants.Any(e => e.Mine);
        public bool MyWon => Winner != null && Winner.Mine;
        public int Bonus;
    }

    /// <summary>
    /// All-Star Saturday: the home run derby. The eight first-half home run leaders (my hitter too, if
    /// he's an All-Star with some pop) swing until ten outs; the best four go on, then the best two.
    /// </summary>
    public static class HomeRunDerby
    {
        public const int Outs = 10;
        public const int WinnerBonus = 1000, EntryBonus = 200;

        public static DerbyResult Play(Career career, Player mine, IRandom random)
        {
            var league = career.League;
            var hitters = league.Teams.SelectMany(t => t.Lineup.Select(p => (Player: p, Team: t)))
                .Where(x => x.Player.Id != career.Me.Id)
                .Select(x => (x.Player, x.Team, Hr: league.Batting.TryGetValue(x.Player.Id, out var b) ? b.Line.HomeRuns : 0))
                .OrderByDescending(x => x.Hr).ThenByDescending(x => x.Player.Batting.Power)
                .ToList();

            var field = new List<(Player Player, DerbyEntrant Entry)>();
            if (mine != null && !mine.IsPitcher)
            {
                int myHr = career.SeasonLine.HomeRuns;
                // In if he'd make the top eight, or the fans want to see him swing.
                bool slugger = hitters.Count < 8 || myHr >= hitters[7].Hr || career.Me.Popularity >= Career.FanVotePopularity;
                if (slugger) field.Add((mine, new DerbyEntrant { Name = career.Me.Name, Team = career.MyTeam.Name, Mine = true, SeasonHomeRuns = myHr }));
            }
            foreach (var h in hitters)
            {
                if (field.Count >= 8) break;
                field.Add((h.Player, new DerbyEntrant { Name = h.Player.Name, Team = h.Team.Name, SeasonHomeRuns = h.Hr }));
            }

            var result = new DerbyResult { Entrants = field.Select(f => f.Entry).ToList() };
            var alive = field;
            foreach (int keep in new[] { 4, 2, 1 })
            {
                foreach (var f in alive) f.Entry.Rounds.Add(Swing(f.Player, random));
                int round = alive[0].Entry.Rounds.Count - 1;
                // Ties go to the one with more home runs in the season.
                alive = alive.OrderByDescending(f => f.Entry.Rounds[round]).ThenByDescending(f => f.Entry.SeasonHomeRuns).Take(keep).ToList();
            }
            result.Winner = alive[0].Entry;
            if (result.MyIn)
            {
                result.Bonus = EntryBonus + (result.MyWon ? WinnerBonus : 0);
                career.Me.Money += result.Bonus;
            }
            return result;
        }

        /// <summary>Home runs before ten outs: power is nearly everything, a good eye helps a little.</summary>
        static int Swing(Player p, IRandom random)
        {
            double chance = Math.Max(0.06, Math.Min(0.62, 0.18 + (p.Batting.Power - 50) * 0.0085 + (p.Batting.Contact - 50) * 0.002));
            int hr = 0, outs = 0;
            while (outs < Outs)
            {
                if (random.NextDouble() < chance) hr++;
                else outs++;
            }
            return hr;
        }
    }
}
