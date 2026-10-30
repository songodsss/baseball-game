using System;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Popularity, 0-100: hits, homers and wins win fans, honours win more, and almost a third of them drift
    /// away each winter. It can carry a player into the all-star game by fan vote and brings in a little
    /// sponsor money; it never touches ratings.
    /// </summary>
    public partial class Career
    {
        public const int FanPointsPerPopularity = 30;
        const int MaxFanPoints = 3600;
        /// <summary>Popularity that wins an all-star spot by fan vote even without the numbers.</summary>
        public const int FanVotePopularity = 70;
        /// <summary>Sponsor money per popularity point at season end: kept small on purpose.</summary>
        const int SponsorPerPopularity = 8;

        internal void AddFansFromEvent(int points) => AddFans(points);

        void AddFans(int points) => Me.FanPoints = Math.Max(0, Math.Min(MaxFanPoints, Me.FanPoints + points));

        /// <summary>Fans won by a game of mine, at full-season pace.</summary>
        void FansForGame(BattingLine bat, PitcherLine pitch)
        {
            int points = bat != null ? 2 * bat.Hits + 6 * bat.HomeRuns : 0;
            if (pitch != null) points += 10 * pitch.Wins + pitch.Strikeouts + 30 * pitch.Shutouts;
            AddFans(points * DayWeight);
        }

        /// <summary>At season end: honours win fans, almost a third drift away, and sponsors pay on what's left.</summary>
        void SettleFans(SeasonSummary summary)
        {
            int titles = summary.Record.Titles.Count(t => t != Awards.Mvp);
            AddFans(150 * titles + (summary.Record.Titles.Contains(Awards.Mvp) ? 300 : 0));
            summary.Popularity = Me.Popularity;
            summary.Sponsor = Me.Popularity * SponsorPerPopularity;
            Me.Money += summary.Sponsor;
            Me.FanPoints = Me.FanPoints * 7 / 10;
        }
    }
}
