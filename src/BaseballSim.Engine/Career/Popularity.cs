using System;
using System.Collections.Generic;
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

        /// <summary>From here a home crowd keeps the player's good mood from fading that day.</summary>
        public const int HomeCheerPopularity = 50;

        /// <summary>Salary worth goes up a quarter of a percent per popularity point (+25% at 100).</summary>
        public static double PopularityPay(int popularity) => 1 + popularity / 400.0;

        static readonly (int From, string Name)[] Tiers =
        {
            (100, "국민 스타"), (80, "슈퍼스타"), (60, "스타"), (40, "인기 선수"), (20, "주목받는 선수"), (0, "무명"),
        };

        public static string PopularityTier(int popularity) => Tiers.First(t => popularity >= t.From).Name;

        /// <summary>What popularity does, for the profile: each line with whether it's on yet.</summary>
        public static List<(bool On, string Text)> PopularityPerks(int popularity) => new List<(bool, string)>
        {
            (popularity >= 20, "인기 20+ · 홈 관중 응원: 홈경기 컨디션 +1"),
            (popularity >= 40, $"인기 40+ · 월급날 팬 선물: 주특기 훈련 효율 +{FanGiftPercent(Math.Max(40, popularity))}% (20일)"),
            (popularity >= HomeCheerPopularity, $"인기 {HomeCheerPopularity}+ · 홈경기에서 뛴 날은 좋은 사기가 떨어지지 않아요"),
            (popularity >= 60, "인기 60+ · 홈 찬스에서 관중 함성: 노림수 '느낌' 적중률 +5%"),
            (popularity >= FanVotePopularity, $"인기 {FanVotePopularity}+ · 성적이 모자라도 팬 투표로 올스타·홈런 더비 출전"),
            (popularity >= 80, "인기 80+ · 어디서나 응원: 원정에서도 컨디션 +1 (홈은 +2)"),
            (popularity >= 100, "인기 100 · 국민 스타: 모든 훈련 효율 +10%"),
            (popularity > 0, $"연봉 시세 +{popularity / 4}% · 시즌 끝 광고 수입 {Krw.Format(popularity * SponsorPerPopularity)}"),
        };

        /// <summary>The fans' gift on payday: 10% at 40, 15% at 60, 20% at 80 and up.</summary>
        public static int FanGiftPercent(int popularity) => popularity >= 80 ? 20 : popularity >= 60 ? 15 : popularity >= 40 ? 10 : 0;

        /// <summary>Game condition from the crowd: +1 at home from 20, and +1 anywhere from 80.</summary>
        int CrowdForm(bool home) => (home && Me.Popularity >= 20 ? 1 : 0) + (Me.Popularity >= 80 ? 1 : 0);

        double PopularityTraining => Me.Popularity >= 100 ? 1.1 : 1.0;

        /// <summary>On payday, from 40 popularity the fans send something: the main stat trains faster for a while.</summary>
        string FanGift()
        {
            int pct = FanGiftPercent(Me.Popularity);
            if (pct == 0 || InMilitary) return null;
            var main = Me.MyStats.OrderByDescending(s => Me.Ratings[s]).First();
            AddBoost(main, pct, 20, "팬 선물");
            return $"팬 선물 도착! {Stats.Name(main)} 훈련 효율 +{pct}% (20일)";
        }

        bool CheeredAtHome(DayResult day) =>
            Me.Popularity >= HomeCheerPopularity && day?.MyGame != null && day.MyGame.Home == MyTeam
            && (day.MyLine != null && day.MyLine.PlateAppearances > 0 || day.MyPitching != null);

        internal void AddFansFromEvent(int points) => AddFans(points);

        void AddFans(int points) => Me.FanPoints = Math.Max(0, Math.Min(MaxFanPoints, Me.FanPoints + points));

        /// <summary>Fans won by a game of mine, at full-season pace.</summary>
        void FansForGame(BattingLine bat, PitcherLine pitch)
        {
            int points = bat != null ? 2 * bat.Hits + 6 * bat.HomeRuns : 0;
            if (pitch != null) points += 10 * pitch.Wins + 8 * pitch.Saves + 5 * pitch.Holds + pitch.Strikeouts + 30 * pitch.Shutouts;
            AddFans((int)Math.Round(points * DayWeight * StatusFans));
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
