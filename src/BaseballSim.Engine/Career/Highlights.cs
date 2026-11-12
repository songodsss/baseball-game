using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>A stat crossed into a new grade today (by its career best, so an injury dip never repeats it).</summary>
    public class GradeUp
    {
        public Stat Stat;
        public string Grade;
        public int BoostPercent, BoostDays;
    }

    /// <summary>The day's big moment in my game, for a cut-in: "끝내기 홈런!", "삼진쇼!".</summary>
    public class Highlight
    {
        public string Title;
        public string Sub;
        /// <summary>2 for the rarest (walk-off, grand slam, shutout), 1 otherwise.</summary>
        public int Level;
    }

    /// <summary>
    /// Grades for ratings (E below 40, then D, C, B, A, S, SS, SSS every 10) and the day's big moments.
    /// </summary>
    public static class Highlights
    {
        static readonly string[] Grades = { "E", "D", "C", "B", "A", "S", "SS", "SSS" };

        public static int GradeIndex(int rating) => System.Math.Max(0, System.Math.Min(Grades.Length - 1, (rating - 30) / 10));

        public static string Grade(int rating) => Grades[GradeIndex(rating)];

        /// <summary>The best moment of my game today, or null.</summary>
        public static Highlight Of(DayResult r)
        {
            var g = r.MyGame;
            if (g == null || r.InFarm) return null;
            var me = r.MyGamePlayer;
            if (r.MyPitching != null)
            {
                var p = r.MyPitching;
                if (p.Shutouts > 0) return new Highlight { Title = "완봉승!", Sub = $"9이닝 무실점 {p.Strikeouts}탈삼진", Level = 2 };
                if (p.Strikeouts >= 10) return new Highlight { Title = "삼진쇼!", Sub = $"{p.Strikeouts}탈삼진", Level = 1 };
                if (p.Wins > 0 && p.Outs >= 21 && p.Runs <= 1) return new Highlight { Title = "압도적 호투!", Sub = $"{p.Innings}이닝 {p.Runs}실점 승리", Level = 1 };
                return null;
            }
            if (me == null || r.MyLine == null) return null;
            var mine = g.Events.Where(e => e.Kind == EventKind.PlateAppearance && e.Batter == me).ToList();
            var last = g.Events.LastOrDefault(e => e.Kind == EventKind.PlateAppearance);
            bool walkOff = last != null && last.Batter == me && !last.IsTopHalf && last.RunnersScored.Count > 0 && g.HomeScore > g.AwayScore && last.Inning >= 9;
            if (walkOff) return new Highlight { Title = last.Outcome == Outcome.HomeRun ? "끝내기 홈런!" : "끝내기!", Sub = $"{last.Inning}회말 {g.AwayScore}:{g.HomeScore}", Level = 2 };
            if (mine.Any(e => e.Outcome == Outcome.HomeRun && e.RunnersScored.Count == 4)) return new Highlight { Title = "만루 홈런!", Sub = "그랜드슬램", Level = 2 };
            int hr = r.MyLine.HomeRuns;
            if (hr >= 2) return new Highlight { Title = "멀티 홈런!", Sub = $"오늘 {hr}홈런 {r.MyLine.RunsBattedIn}타점", Level = 1 };
            if (r.MyLine.Hits >= 4) return new Highlight { Title = "맹타!", Sub = $"{r.MyLine.AtBats}타수 {r.MyLine.Hits}안타", Level = 1 };
            if (hr == 1) return new Highlight { Title = "홈런!", Sub = r.MyLine.RunsBattedIn > 1 ? $"{r.MyLine.RunsBattedIn}타점" : "담장을 넘겼다", Level = 1 };
            return null;
        }
    }

    public partial class Career
    {
        const string GradeMark = "grade:";
        /// <summary>A new grade lifts that stat's training for a while: 10% + 5% per grade, 10 days.</summary>
        public const int GradeBoostDays = 10;

        /// <summary>Remembers each stat's grade as already reached (a new career, or an old save on first look).</summary>
        int Peak(Stat s) => Math.Max(Me.PeakRatings.TryGetValue(s, out var p) ? p : 0, Me.Ratings.TryGetValue(s, out var r) ? r : 0);

        void NoteGrades()
        {
            foreach (var s in Me.MyStats)
                if (!Me.StoryMarks.ContainsKey(GradeMark + s)) Me.StoryMarks[GradeMark + s] = Highlights.GradeIndex(Peak(s));
        }

        List<GradeUp> CheckGrades()
        {
            var ups = new List<GradeUp>();
            foreach (var s in Me.MyStats)
            {
                int now = Highlights.GradeIndex(Peak(s));
                if (!Me.StoryMarks.TryGetValue(GradeMark + s, out int was)) { Me.StoryMarks[GradeMark + s] = now; continue; }
                if (now <= was) continue;
                Me.StoryMarks[GradeMark + s] = now;
                int pct = 10 + 5 * now;
                AddBoost(s, pct, GradeBoostDays, "등급 상승");
                AddFans(30 * now);
                ups.Add(new GradeUp { Stat = s, Grade = Highlights.Grade(Peak(s)), BoostPercent = pct, BoostDays = GradeBoostDays });
            }
            return ups;
        }
    }
}
