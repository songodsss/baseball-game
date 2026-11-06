using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Career milestones reached in a game. Only the big ones get a ceremony, as in the real league
    /// (1,000 hits, 200 homers, 100 wins...): the crowd stands, the club hands over a bonus. Smaller
    /// round numbers are a line in the log and the season record, nothing more.
    /// </summary>
    public partial class Career
    {
        const int MilestoneBonus = 1000, MinorMilestoneBonus = 100;

        static IEnumerable<int> Every(int from, int step) => Enumerable.Range(0, 20).Select(i => from + i * step);
        // (small marks: log only; big marks: the ceremony)
        static (int[] Minor, IEnumerable<int> Big) HitMarks() => (new[] { 500 }, Every(1000, 500));
        static (int[] Minor, IEnumerable<int> Big) HomeRunMarks() => (new[] { 100, 150 }, Every(200, 50));
        static (int[] Minor, IEnumerable<int> Big) WinMarks() => (new[] { 50 }, Every(100, 50));
        static (int[] Minor, IEnumerable<int> Big) ReliefMarks() => (new[] { 50, 100 }, Every(150, 50));
        static (int[] Minor, IEnumerable<int> Big) StrikeoutMarks() => (new[] { 500 }, Every(1000, 500));

        /// <summary>Big milestones today's 1군 game crossed, as ceremony lines; small ones go to <see cref="DayResult.MinorMilestones"/>.</summary>
        List<string> CheckMilestones(DayResult result)
        {
            var reached = new List<string>();
            void Cross(int after, int today, (int[] Minor, IEnumerable<int> Big) marks, string unit)
            {
                int before = after - today;
                foreach (int mark in marks.Minor.Where(m => m > before && m <= after))
                {
                    result.MinorMilestones.Add($"통산 {mark:N0}{unit}");
                    SeasonNotes.Add($"{MonthText} 통산 {mark:N0}{unit}");
                    Me.Money += MinorMilestoneBonus;
                    AddFans(20);
                }
                foreach (int mark in marks.Big.Where(m => m > before && m <= after))
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
                Cross(total.Saves, result.MyPitching.Saves, ReliefMarks(), "세이브");
                Cross(total.Holds, result.MyPitching.Holds, ReliefMarks(), "홀드");
            }
            foreach (var _ in reached)
            {
                Me.Money += MilestoneBonus;
                AddFans(100);
                Me.Morale = Math.Min(CareerPlayer.MaxMorale, Me.Morale + 3);
            }
            return reached;
        }

        /// <summary>What the stadium does for a big milestone.</summary>
        public static string MilestoneCeremony(string milestone, string name) =>
            $"{milestone} 달성! 리그에서도 손꼽히는 기록이다. 전광판에 {name}의 이름이 떠오르고 관중이 모두 일어나 박수를 보냈다. 구단이 기념 상금 {MilestoneBonus:N0}만원을 전달했다.";

        /// <summary>A log line for a smaller round number.</summary>
        public static string MinorMilestoneLine(string milestone) => $"{milestone}. 전광판에 짧게 축하 문구가 떴다. (+{MinorMilestoneBonus}만원)";
    }
}
