using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Career milestones reached in a game: the 100th hit, every 50th homer, the 100th win. The crowd
    /// stands, the club hands over a commemorative bonus, and it goes on the season record.
    /// </summary>
    public partial class Career
    {
        const int MilestoneBonus = 300;

        static IEnumerable<int> HitMarks() => new[] { 100, 250 }.Concat(Enumerable.Range(2, 20).Select(i => i * 250));
        static IEnumerable<int> HomeRunMarks() => Enumerable.Range(1, 20).Select(i => i * 50);
        static IEnumerable<int> WinMarks() => new[] { 10 }.Concat(Enumerable.Range(1, 10).Select(i => i * 50));
        static IEnumerable<int> StrikeoutMarks() => new[] { 100 }.Concat(Enumerable.Range(1, 10).Select(i => i * 500));

        /// <summary>Milestones today's 1군 game crossed, as ceremony lines; each one pays and wins fans.</summary>
        List<string> CheckMilestones(DayResult result)
        {
            var reached = new List<string>();
            void Cross(int after, int today, IEnumerable<int> marks, string unit)
            {
                int before = after - today;
                foreach (int mark in marks.Where(m => m > before && m <= after))
                {
                    reached.Add($"통산 {mark:N0}{unit}");
                    SeasonNotes.Add($"{MonthText} 통산 {mark:N0}{unit}");
                }
            }
            if (result.MyLine != null && !result.InFarm)
            {
                var total = Stats.CareerTotal(this);
                Cross(total.Hits, result.MyLine.Hits, HitMarks(), "안타");
                Cross(total.HomeRuns, result.MyLine.HomeRuns, HomeRunMarks(), "홈런");
            }
            if (result.MyPitching != null && !result.InFarm)
            {
                var total = Stats.CareerPitching(this);
                Cross(total.Wins, result.MyPitching.Wins, WinMarks(), "승");
                Cross(total.Strikeouts, result.MyPitching.Strikeouts, StrikeoutMarks(), "탈삼진");
            }
            foreach (var _ in reached)
            {
                Me.Money += MilestoneBonus;
                AddFans(100);
                Me.Morale = Math.Min(CareerPlayer.MaxMorale, Me.Morale + 3);
            }
            return reached;
        }

        /// <summary>What the stadium does for a milestone.</summary>
        public static string MilestoneCeremony(string milestone, string name) =>
            $"{milestone} 달성! 전광판에 {name}의 이름이 떠오르고 관중이 모두 일어나 박수를 보냈다. 구단이 기념 상금 {MilestoneBonus}만원을 전달했다.";
    }
}
