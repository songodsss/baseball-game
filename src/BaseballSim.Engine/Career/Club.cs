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
        public int AllStars;
        /// <summary>Career score for the hall of fame: hits and homers scaled to 144-game seasons, plus honours.</summary>
        public int HallOfFameScore;
        public bool HallOfFame;
        /// <summary>Club that retired this player's number: a hall-of-famer's club of ten seasons or more. Null if none.</summary>
        public string NumberRetiredBy;
        /// <summary>Money left at retirement, given to youth baseball for legacy points.</summary>
        public int Donation;
        public int DonationPoints;

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

        public const int HallOfFameLine = 400;
        const int HallOfFamePoints = 100;
        /// <summary>만원 of leftover money per legacy point when donated at retirement.</summary>
        public const int DonationPerPoint = 10000;

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

            double Scaled(Func<BattingLine, int> stat) => me.History.Sum(r => stat(r.Line) * (double)League.FullSeason / r.Games);
            int titles = retired.Titles.Count(t => t != Postseason.ChampionTitle);
            int rings = retired.Titles.Count(t => t == Postseason.ChampionTitle);
            retired.AllStars = me.AllStarSelections;
            retired.HallOfFameScore = (int)(Scaled(l => l.Hits) / 10 + Scaled(l => l.HomeRuns) / 2 + 20 * titles + 10 * rings + 10 * retired.AllStars);
            retired.HallOfFame = retired.HallOfFameScore >= HallOfFameLine;
            if (retired.HallOfFame)
            {
                retired.LegacyPoints += HallOfFamePoints;
                var longest = me.History.GroupBy(h => h.Club ?? career.MyTeam.Name).OrderByDescending(g => g.Count()).FirstOrDefault();
                if (longest != null && longest.Count() >= 10) retired.NumberRetiredBy = longest.Key;
            }

            // Money can't follow the player into retirement; it goes to youth baseball instead.
            retired.Donation = Math.Max(0, me.Money);
            retired.DonationPoints = retired.Donation / DonationPerPoint;
            me.Money = 0;

            Roster.Add(retired);
            LegacyPoints += retired.LegacyPoints + retired.DonationPoints;
            return retired;
        }
    }
}
