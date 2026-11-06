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
            Add("champion", "우승 반지", "한국시리즈 우승", c => HasTitle(c, Postseason.ChampionTitle));
            Add("salary_1", "억대 연봉", "연봉 1억 원 돌파", c => c.Me.Salary >= 10000);
            Add("salary_10", "10억 연봉", "연봉 10억 원 돌파", c => c.Me.Salary >= 100000);
            Add("full_gear", "명품 장비", "모든 장비를 최고 등급으로", c => c.Me.Equipped.Count == 5 && c.Me.Equipped.Values.All(e => e.Price == EquipmentCatalog.All.Max(x => x.Price)));
            Add("facilities", "나만의 훈련 센터", "개인 시설을 모두 3단계 이상으로", c => Facilities.All.All(f => c.Me.FacilityLevel(f) >= 3));
            Add("all_star", "올스타", "올스타전 출전", c => c.Me.AllStarSelections >= 1);
            Add("all_star_mvp", "별 중의 별", "올스타전 MVP", c => c.Me.AllStarMvps >= 1);
            Add("derby_king", "홈런 더비 킹", "홈런 더비 우승", c => c.Me.HomeRunDerbyWins >= 1);
            Add("reads", "수싸움의 달인", "찬스 타석 노림수 30번 적중", c => c.Me.ReadsRight >= 30);
            Add("gold", "금메달", "아시아 야구 대회 금메달", c => c.Me.GoldMedals >= 1);
            Add("wbc", "세계 정상", "WBC 우승", c => c.Me.WbcTitles >= 1);
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
            Add("full_season", "풀시즌", "144경기 시즌 완주", c => c.Me.History.Any(h => h.Games >= League.FullSeason));
            Add("full_season_10", "144경기의 사나이", "144경기 시즌 10번 완주", c => c.Me.History.Count(h => h.Games >= League.FullSeason) >= 10);

            // Seasons to remember and the rest of the game: steals, power-speed seasons, autumn heroics, life.
            int TitleCount(Career c, string title) => c.Me.History.Count(h => h.Titles.Contains(title));
            bool Season(Career c, Func<SeasonRecord, bool> f) => c.Me.History.Any(f);
            Add("first_sb", "프로 첫 도루", "프로 무대에서 첫 도루", c => Total(c).StolenBases >= 1);
            Add("sb_30_season", "발야구", "한 시즌 30도루", c => Season(c, h => h.Line.StolenBases >= 30));
            Add("sb_100", "대도", "통산 100도루", c => Total(c).StolenBases >= 100);
            Add("sb_300", "베이스 위의 지배자", "통산 300도루", c => Total(c).StolenBases >= 300);
            Add("twenty_twenty", "20-20 클럽", "한 시즌 20홈런-20도루", c => Season(c, h => h.Line.HomeRuns >= 20 && h.Line.StolenBases >= 20));
            Add("thirty_thirty", "30-30 클럽", "한 시즌 30홈런-30도루", c => Season(c, h => h.Line.HomeRuns >= 30 && h.Line.StolenBases >= 30));
            Add("hr_30_season", "거포의 탄생", "한 시즌 30홈런", c => Season(c, h => h.Line.HomeRuns >= 30));
            Add("hr_50_season", "50홈런 시즌", "한 시즌 50홈런", c => Season(c, h => h.Line.HomeRuns >= 50));
            Add("hr_300", "300홈런", "통산 300홈런", c => Total(c).HomeRuns >= 300);
            Add("hr_400", "400홈런", "통산 400홈런", c => Total(c).HomeRuns >= 400);
            Add("rbi_100_season", "100타점", "한 시즌 100타점", c => Season(c, h => h.Line.RunsBattedIn >= 100));
            Add("runs_100_season", "100득점", "한 시즌 100득점", c => Season(c, h => h.Line.Runs >= 100));
            Add("hits_200_season", "200안타", "한 시즌 200안타", c => Season(c, h => h.Line.Hits >= 200));
            Add("hits_2000", "2000안타", "통산 2000안타", c => Total(c).Hits >= 2000);
            Add("walks_100_season", "선구안의 정석", "한 시즌 볼넷 100개", c => Season(c, h => h.Line.Walks >= 100));
            Add("wins_15_season", "15승 투수", "한 시즌 15승", c => Season(c, h => h.Pitching != null && h.Pitching.Wins >= 15));
            Add("k_200_season", "닥터 K", "한 시즌 200탈삼진", c => Season(c, h => h.Pitching != null && h.Pitching.Strikeouts >= 200));
            Add("saves_40_season", "철벽 마무리", "한 시즌 40세이브", c => Season(c, h => h.Pitching != null && h.Pitching.Saves >= 40));
            Add("holds_30_season", "셋업의 품격", "한 시즌 30홀드", c => Season(c, h => h.Pitching != null && h.Pitching.Holds >= 30));
            Add("k_2000", "2000탈삼진", "통산 2000탈삼진", c => Arm(c).Strikeouts >= 2000);
            Add("multi_crown", "다관왕", "한 시즌 개인 타이틀 3개 이상", c => Season(c, h => h.Titles.Count(Awards.IsStatTitle) >= 3));
            Add("mvp_3", "시대의 얼굴", "시즌 MVP 3회", c => TitleCount(c, Awards.Mvp) >= 3);
            Add("ks_mvp", "가을의 영웅", "한국시리즈 MVP", c => HasTitle(c, Postseason.KsMvpTitle));
            Add("rings_3", "왕조", "한국시리즈 우승 3회", c => TitleCount(c, Postseason.ChampionTitle) >= 3);
            Add("golden_glove_5", "황금 장갑 수집가", "골든글러브 5회", c => TitleCount(c, Awards.GoldenGlove) >= 5);
            Add("all_star_5", "단골 올스타", "올스타전 5회 출전", c => c.Me.AllStarSelections >= 5);
            Add("legend_surpass", "전설을 넘어", "레전드의 기록을 넘기", c => Legends.All.Any(l => c.StageOf(l.Id) == LegendStage.Surpassed));
            Add("legend_surpass_3", "새로운 전설", "레전드 3명의 기록을 넘기", c => Legends.All.Count(l => c.StageOf(l.Id) == LegendStage.Surpassed) >= 3);
            Add("moved", "새 유니폼", "다른 구단으로 이적", c => c.Me.Clubs.Count > 0);
            Add("married", "평생의 짝", "결혼", c => c.HasMark("married"));
            Add("one_club", "원클럽맨", "한 구단에서만 10시즌", c => c.Me.Clubs.Count == 0 && c.Me.History.Count >= 10);
            return list;
        }
    }
}
