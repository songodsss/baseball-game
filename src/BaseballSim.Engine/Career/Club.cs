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
        public List<string> Achievements = new List<string>();
        public List<string> Titles = new List<string>();

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

        /// <summary>Premium uniforms bought; free ones are always owned. Kept across careers.</summary>
        public List<string> OwnedUniforms = new List<string>();
        public string SelectedUniform = Wardrobe.DefaultId;

        public bool OwnsUniform(Uniform u) => !u.Premium || OwnedUniforms.Contains(u.Id);

        /// <summary>Records a store purchase. The store itself (real payments) lives in the Unity layer.</summary>
        public void GrantUniform(string id)
        {
            if (!OwnedUniforms.Contains(id)) OwnedUniforms.Add(id);
        }

        /// <summary>False if the uniform isn't owned.</summary>
        public bool SelectUniform(string id)
        {
            var u = Wardrobe.All.Find(x => x.Id == id);
            if (u == null || !OwnsUniform(u)) return false;
            SelectedUniform = id;
            return true;
        }

        /// <summary>Achievement ids any retired player of mine ever reached.</summary>
        public HashSet<string> Collection => new HashSet<string>(Roster.SelectMany(p => p.Achievements));

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
                Achievements = new List<string>(me.Achievements),
                Titles = me.History.SelectMany(h => h.Titles).ToList(),
                // Each season scaled to a full 144 games, so a short-season career earns the same.
                LegacyPoints = (int)me.History.Sum(r => (r.Line.Hits / 10.0 + r.Line.HomeRuns / 2.0) * League.FullSeason / r.Games + 5),
            };
            Roster.Add(retired);
            LegacyPoints += retired.LegacyPoints;
            return retired;
        }
    }
}
