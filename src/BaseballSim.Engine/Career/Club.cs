using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>A retired player kept on the user's own team for later team content.</summary>
    public class RetiredPlayer
    {
        public string Name;
        public int Seasons;
        public BattingLine CareerLine;
        /// <summary>Best rating reached in each stat, so a long career isn't punished for its decline years.</summary>
        public Dictionary<Stat, int> PeakRatings;
        public int LegacyPoints;

        public int Overall => (int)Math.Round(PeakRatings.Values.Average());
    }

    /// <summary>
    /// What outlasts a single career: the retired players the user has raised, and legacy points
    /// that give the next player a head start, so even a disappointing career still pays off.
    /// </summary>
    public class Club
    {
        public List<RetiredPlayer> Roster = new List<RetiredPlayer>();
        public int LegacyPoints;

        /// <summary>Added to every potential of the next new player, at most +8.</summary>
        public int PotentialBonus => Math.Min(8, LegacyPoints / 200);

        public RetiredPlayer Retire(Career career)
        {
            var me = career.Me;
            var line = new BattingLine();
            foreach (var season in me.History) Career.Accumulate(line, season.Line);

            var retired = new RetiredPlayer
            {
                Name = me.Name,
                Seasons = me.History.Count,
                CareerLine = line,
                PeakRatings = new Dictionary<Stat, int>(me.PeakRatings),
                LegacyPoints = line.Hits / 3 + line.HomeRuns * 2 + me.History.Count * 5,
            };
            Roster.Add(retired);
            LegacyPoints += retired.LegacyPoints;
            return retired;
        }
    }
}
