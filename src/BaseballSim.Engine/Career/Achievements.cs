using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class Achievement
    {
        public string Id;
        public string Name;
        public string Description;
        public Func<Career, bool> Reached;
    }

    /// <summary>
    /// Career milestones to collect. Each career unlocks its own; retired players keep theirs,
    /// so the club's collection shows which milestones any of my players ever reached.
    /// </summary>
    public static class Achievements
    {
        public static readonly List<Achievement> All = Build();

        public static Achievement Find(string id) => All.Find(a => a.Id == id);

        /// <summary>Unlocks whatever has newly been reached; returns the new ones.</summary>
        public static List<Achievement> Check(Career career)
        {
            var unlocked = new List<Achievement>();
            foreach (var a in All)
            {
                if (career.Me.Achievements.Contains(a.Id) || !a.Reached(career)) continue;
                career.Me.Achievements.Add(a.Id);
                unlocked.Add(a);
            }
            return unlocked;
        }

        static bool HasTitle(Career c, string title) => c.Me.History.Any(h => h.Titles.Contains(title));

        static List<Achievement> Build()
        {
            var list = new List<Achievement>();
            void Add(string id, string name, string description, Func<Career, bool> reached) =>
                list.Add(new Achievement { Id = id, Name = name, Description = description, Reached = reached });
            BattingLine Total(Career c) => Stats.CareerTotal(c);

            Add("first_hit", "프로 첫 안타", "프로 무대에서 첫 안타", c => Total(c).Hits >= 1);
            Add("first_hr", "프로 첫 홈런", "프로 무대에서 첫 홈런", c => Total(c).HomeRuns >= 1);
            Add("hits_100", "100안타", "통산 100안타", c => Total(c).Hits >= 100);
            Add("hits_500", "500안타", "통산 500안타", c => Total(c).Hits >= 500);
            Add("hits_1000", "1000안타", "통산 1000안타", c => Total(c).Hits >= 1000);
            Add("hr_50", "50홈런", "통산 50홈런", c => Total(c).HomeRuns >= 50);
            Add("hr_100", "100홈런", "통산 100홈런", c => Total(c).HomeRuns >= 100);
            Add("hr_200", "200홈런", "통산 200홈런", c => Total(c).HomeRuns >= 200);
            Add("three_hit_season", "3할 타자", "규정 타석을 채우고 시즌 타율 .300 이상",
                c => c.Me.History.Any(h => h.Line.PlateAppearances >= Math.Ceiling(3.1 * h.Games) && Stats.Avg(h.Line) >= 0.3));
            Add("first_title", "첫 타이틀", "개인 타이틀 수상", c => c.Me.History.Any(h => h.Titles.Any(t => t != Postseason.ChampionTitle)));
            Add("rookie", "신인왕", "신인왕 수상", c => HasTitle(c, Awards.Rookie));
            Add("mvp", "MVP", "시즌 MVP 수상", c => HasTitle(c, Awards.Mvp));
            Add("champion", "우승 반지", "챔피언 결정전 우승", c => HasTitle(c, Postseason.ChampionTitle));
            Add("salary_1", "억대 연봉", "연봉 1억 원 돌파", c => c.Me.Salary >= 10000);
            Add("salary_10", "10억 연봉", "연봉 10억 원 돌파", c => c.Me.Salary >= 100000);
            Add("full_gear", "명품 장비", "모든 장비를 최고 등급으로", c => c.Me.Equipped.Count == 5 && c.Me.Equipped.Values.All(e => e.Price == EquipmentCatalog.All.Max(x => x.Price)));
            Add("facilities", "나만의 훈련 센터", "개인 시설을 모두 3단계 이상으로", c => Facilities.All.All(f => c.Me.FacilityLevel(f) >= 3));
            Add("all_star", "올스타", "올스타전 출전", c => c.Me.AllStarSelections >= 1);
            Add("all_star_mvp", "별 중의 별", "올스타전 MVP", c => c.Me.AllStarMvps >= 1);
            Add("reads", "수싸움의 달인", "찬스 타석 노림수 30번 적중", c => c.Me.ReadsRight >= 30);
            Add("gold", "금메달", "아시아 야구 대회 금메달", c => c.Me.GoldMedals >= 1);
            Add("golden_glove", "골든글러브", "골든글러브 수상", c => HasTitle(c, Awards.GoldenGlove));
            PitcherLine Arm(Career c) => Stats.CareerPitching(c);
            Add("first_win", "프로 첫 승", "프로 무대에서 첫 승리", c => Arm(c).Wins >= 1);
            Add("first_k", "프로 첫 탈삼진", "프로 무대에서 첫 삼진", c => Arm(c).Strikeouts >= 1);
            Add("wins_50", "50승", "통산 50승", c => Arm(c).Wins >= 50);
            Add("wins_100", "100승", "통산 100승", c => Arm(c).Wins >= 100);
            Add("k_500", "500탈삼진", "통산 500탈삼진", c => Arm(c).Strikeouts >= 500);
            Add("k_1000", "1000탈삼진", "통산 1000탈삼진", c => Arm(c).Strikeouts >= 1000);
            Add("shutout", "완봉승", "9이닝 무실점 완봉", c => Arm(c).Shutouts >= 1);
            Add("ace_season", "2점대 에이스", "규정 이닝을 채우고 평균자책 2점대 이하",
                c => c.Me.History.Any(h => h.Pitching != null && h.Pitching.Outs >= 3 * h.Games && h.Pitching.Era < 3.0));
            Add("first_save", "프로 첫 세이브", "프로 무대에서 첫 세이브", c => Arm(c).Saves >= 1);
            Add("saves_100", "100세이브", "통산 100세이브", c => Arm(c).Saves >= 100);
            Add("saves_300", "300세이브", "통산 300세이브", c => Arm(c).Saves >= 300);
            Add("first_hold", "프로 첫 홀드", "프로 무대에서 첫 홀드", c => Arm(c).Holds >= 1);
            Add("holds_100", "100홀드", "통산 100홀드", c => Arm(c).Holds >= 100);
            Add("fan_favorite", "국민 스타", "인기도 100 달성", c => c.Me.Popularity >= 100);
            Add("iron_man", "철인", "부상 없이 시즌 완주", c => c.Me.History.Any(h => h.InjuryFree));
            return list;
        }
    }
}
