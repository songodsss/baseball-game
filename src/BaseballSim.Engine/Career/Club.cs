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
        public Position Position = Position.Outfield;
        /// <summary>Number worn in the career; 0 if none was picked.</summary>
        public int Number;
        /// <summary>Number on my own team, set freely there; 0 = the career number.</summary>
        public int TeamNumber;
        public int ShownNumber => TeamNumber > 0 ? TeamNumber : Number;
        /// <summary>The club he played the most seasons for.</summary>
        public string MainClub;
        /// <summary>A pitcher's career line; null for hitters.</summary>
        public PitcherLine CareerPitching;

        public PitcherRole Role = PitcherRole.Starter;
        public bool IsPitcher => Position == Position.Pitcher;
        public int Overall => (int)Math.Round(PeakRatings.Values.Average());

        /// <summary>The farewell at his last game, longer for a bigger career.</summary>
        public List<string> Ceremony
        {
            get
            {
                var lines = new List<string>();
                string number = Number > 0 ? $"등번호 {Number}번 " : "";
                lines.Add($"{MainClub ?? "구단"} 홈구장. {number}{Name}의 은퇴식이 열렸다.");
                var arm = CareerPitching ?? new PitcherLine();
                string pitched = Role == PitcherRole.Closer ? $"통산 {arm.Saves}세이브" : Role == PitcherRole.Setup ? $"통산 {arm.Holds}홀드" : $"통산 {arm.Wins}승";
                lines.Add(IsPitcher
                    ? $"{Seasons}시즌, {pitched} {arm.Strikeouts}탈삼진. 마지막으로 마운드에 올라 시구를 했다."
                    : $"{Seasons}시즌, 통산 {CareerLine.Hits}안타 {CareerLine.HomeRuns}홈런. 마지막 타석에 들어서자 관중이 이름을 연호했다.");
                if (Titles.Count > 0) lines.Add($"타이틀 {Titles.Count}개의 기억이 전광판을 채웠다.");
                if (NumberRetiredBy != null) lines.Add($"{NumberRetiredBy}이(가) 그의 {(Number > 0 ? Number + "번" : "등번호")}을 영구결번으로 지정했다. 유니폼이 외야 펜스 위에 걸렸다.");
                if (HallOfFame) lines.Add("그리고 명예의 전당 헌액 소식이 전해졌다.");
                lines.Add(Donation > 0 ? $"그는 남은 {Donation:N0}만원을 유소년 야구에 기부하며 그라운드를 떠났다." : "그는 모자를 벗어 인사하고 그라운드를 떠났다.");
                return lines;
            }
        }
    }

    /// <summary>
    /// What outlasts a single career: the retired players the user has raised, and legacy points
    /// that give the next player a head start, so even a disappointing career still pays off.
    /// </summary>
    public class Club
    {
        public List<RetiredPlayer> Roster = new List<RetiredPlayer>();
        public int LegacyPoints;
        /// <summary>Levels my team has beaten at least once, and wins overall.</summary>
        public HashSet<ChallengeTier> ChallengesWon = new HashSet<ChallengeTier>();
        public int ChallengeWins;
        /// <summary>Real-calendar missions and check-ins, shared by every player slot.</summary>
        public DailyMissions Daily = new DailyMissions();
        /// <summary>Legend ids and rival names met by any player of mine, for the album.</summary>
        public HashSet<string> LegendsMet = new HashSet<string>();
        public List<string> RivalsMet = new List<string>();

        /// <summary>Legends met so far, counting the career in progress.</summary>
        public HashSet<string> AllLegendsMet(Career career)
        {
            var all = new HashSet<string>(LegendsMet);
            if (career != null) all.UnionWith(career.Me.MetLegends);
            return all;
        }

        /// <summary>Added to every potential of the next new player, at most +8.</summary>
        public int PotentialBonus => Math.Min(8, LegacyPoints / 200);

        public const int HallOfFameLine = 650;
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
                Position = me.Position,
                Role = me.Role,
                Number = me.Number,
                MainClub = me.History.GroupBy(h => h.Club ?? career.MyTeam.Name).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? career.MyTeam.Name,
            };
            if (me.IsPitcher)
            {
                retired.CareerPitching = new PitcherLine();
                foreach (var season in me.History) if (season.Pitching != null) retired.CareerPitching.Add(season.Pitching);
            }

            // A season's worth: hits and homers, or wins and strikeouts, scaled to a full 144 games so a
            // short-season career earns the same.
            double Worth(SeasonRecord r) => (r.Pitching != null
                ? r.Pitching.Wins + r.Pitching.Saves * 0.6 + r.Pitching.Holds * 0.4 + r.Pitching.Strikeouts / 10.0
                : r.Line.Hits / 10.0 + r.Line.HomeRuns / 2.0) * League.FullSeason / r.Games;
            retired.LegacyPoints = (int)me.History.Sum(r => Worth(r) + 5);
            int titles = retired.Titles.Count(t => t != Postseason.ChampionTitle);
            int rings = retired.Titles.Count(t => t == Postseason.ChampionTitle);
            retired.AllStars = me.AllStarSelections;
            retired.HallOfFameScore = (int)(me.History.Sum(Worth) + 20 * titles + 10 * rings + 10 * retired.AllStars);
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

            LegendsMet.UnionWith(me.MetLegends);
            if (career.Rival != null && !RivalsMet.Contains(career.Rival.Name)) RivalsMet.Add(career.Rival.Name);

            Roster.Add(retired);
            LegacyPoints += retired.LegacyPoints + retired.DonationPoints;
            return retired;
        }
    }
}
