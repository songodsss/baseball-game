using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>A passing state of the body or the bat: not a trait, it comes from how the days went and wears off.</summary>
    public class StatusDef
    {
        public string Id, Name, Description;
        public bool Good;
    }

    public class ActiveStatus
    {
        public StatusDef Def;
        /// <summary>Days left; for the rookie blessing, seasons are shown instead (see Text).</summary>
        public int DaysLeft;
        public string Text;
    }

    /// <summary>
    /// The league gets a little stronger every season against my player, a rookie gets a head start for two
    /// seasons, and passing states (상태) come and go with the days: a hot bat, a slump, a fragile body after an
    /// injury, iron legs after a rested week, wear from days on end at high fatigue, a focused stretch after a
    /// training day that clicked.
    /// </summary>
    public static class Statuses
    {
        public const string Rookie = "rookie", Hot = "hot", Cold = "cold", Fragile = "fragile", Iron = "iron", Worn = "worn", Focus = "focus";

        public static readonly List<StatusDef> All = new List<StatusDef>
        {
            new StatusDef { Id = Rookie, Name = "새싹의 축복", Good = true, Description = "프로 첫 두 시즌: 훈련 경험치 +25%, 지쳐도 부상 확률 절반" },
            new StatusDef { Id = Hot, Name = "상승세", Good = true, Description = "몸이 가볍다: 경기 컨디션 +2" },
            new StatusDef { Id = Cold, Name = "침체", Good = false, Description = "감이 안 온다: 경기 컨디션 -2 (좋은 경기 하나면 풀려요)" },
            new StatusDef { Id = Fragile, Name = "유리몸", Good = false, Description = "부상에서 막 돌아왔다: 지친 날 부상 확률 2배" },
            new StatusDef { Id = Iron, Name = "강철 체력", Good = true, Description = "푹 쉰 몸: 훈련·경기 피로 30% 덜 쌓임" },
            new StatusDef { Id = Worn, Name = "피로 누적", Good = false, Description = "며칠째 무리했다: 훈련 효율 -20%, 컨디션 -1 (피로 50 아래로 내리면 풀려요)" },
            new StatusDef { Id = Focus, Name = "몰입", Good = true, Description = "훈련이 손에 붙는다: 대성공 확률 1.5배" },
        };

        public static StatusDef Find(string id) => All.FirstOrDefault(d => d.Id == id);
    }

    public partial class Career
    {
        const string StatusMark = "status:";
        /// <summary>Rating points the league plays above my player: 1.2 a season after the first, at most 15.</summary>
        public const double OppositionPerSeason = 1.2;
        public const int MaxOpposition = 15;
        public const int RookieSeasons = 2;
        public const double RookieTraining = 1.25;

        public int OppositionLevel => Math.Min(MaxOpposition, (int)Math.Round((Season - 1) * OppositionPerSeason));

        public bool RookieBlessed => Season <= RookieSeasons;

        public bool HasStatus(string id) =>
            id == Statuses.Rookie ? RookieBlessed : Me.StoryMarks.TryGetValue(StatusMark + id, out int until) && until > Stamp;

        /// <summary>The states on now, good ones first.</summary>
        public List<ActiveStatus> ActiveStatuses()
        {
            var list = new List<ActiveStatus>();
            foreach (var d in Statuses.All)
            {
                if (!HasStatus(d.Id)) continue;
                if (d.Id == Statuses.Rookie) { list.Add(new ActiveStatus { Def = d, DaysLeft = 0, Text = $"{RookieSeasons - Season + 1}시즌 남음" }); continue; }
                int left = Me.StoryMarks[StatusMark + d.Id] - Stamp;
                list.Add(new ActiveStatus { Def = d, DaysLeft = left, Text = left > 900 ? "풀릴 때까지" : $"{left}일" });
            }
            return list.OrderByDescending(a => a.Def.Good).ToList();
        }

        /// <summary>Puts a state on for some days (a full-season count, scaled to the season's length); returns true if it is new.</summary>
        bool SetStatus(string id, int days)
        {
            bool fresh = !HasStatus(id);
            int scaled = days >= 900 ? 999 : Math.Max(1, (int)Math.Round(days * League.GamesPerSeason / 144.0));
            Me.StoryMarks[StatusMark + id] = Stamp + scaled;
            return fresh;
        }

        void ClearStatus(string id) => Me.StoryMarks.Remove(StatusMark + id);

        /// <summary>Game condition from the states on today.</summary>
        int StatusForm => (HasStatus(Statuses.Hot) ? 2 : 0) - (HasStatus(Statuses.Cold) ? 2 : 0) - (HasStatus(Statuses.Worn) ? 1 : 0);

        /// <summary>Training from the states: the rookie blessing up, wear down.</summary>
        double StatusTraining => (RookieBlessed ? RookieTraining : 1.0) * (HasStatus(Statuses.Worn) ? 0.8 : 1.0);

        /// <summary>The chance of getting hurt when tired, scaled by the states.</summary>
        double StatusInjury => (RookieBlessed ? 0.5 : 1.0) * (HasStatus(Statuses.Fragile) ? 2.0 : 1.0);

        double StatusCrit => HasStatus(Statuses.Focus) ? 1.5 : 1.0;

        bool StatusIron => HasStatus(Statuses.Iron);

        /// <summary>At the end of each day: states come from how it went, and go.</summary>
        void UpdateStatuses(DayResult r, bool wasInjured)
        {
            void Gain(string id, int days) { if (SetStatus(id, days)) r.NewStatuses.Add(Statuses.Find(id)); }

            // The bat (or the arm).
            bool bigDay = Highlights.Of(r) != null;
            if (bigDay || HitStreak >= 5) { Gain(Statuses.Hot, 5); ClearStatus(Statuses.Cold); }
            else if (!Me.IsPitcher && HitlessStreak >= 4 || Me.IsPitcher && r.MyPitching != null && r.MyPitching.Runs >= 6) { Gain(Statuses.Cold, 4); ClearStatus(Statuses.Hot); }
            if (HasStatus(Statuses.Cold) && (r.MyLine != null && r.MyLine.Hits >= 2 || r.MyPitching != null && r.MyPitching.Wins > 0)) ClearStatus(Statuses.Cold);

            // The body.
            if (wasInjured && !Me.IsInjured) Gain(Statuses.Fragile, 10);
            Count("fresh_days", Me.Fatigue <= 30, out int fresh);
            if (fresh >= 7) { Gain(Statuses.Iron, 7); Me.StoryMarks["fresh_days"] = 0; }
            Count("tired_days", Me.Fatigue >= 80, out int tired);
            if (tired >= 3) Gain(Statuses.Worn, 999);
            if (HasStatus(Statuses.Worn) && Me.Fatigue < 50) ClearStatus(Statuses.Worn);

            if (r.TrainingCrit > 0) Gain(Statuses.Focus, 3);
        }

        void Count(string key, bool today, out int run)
        {
            run = today ? (Me.StoryMarks.TryGetValue(key, out int v) ? v : 0) + 1 : 0;
            Me.StoryMarks[key] = run;
        }
    }
}
