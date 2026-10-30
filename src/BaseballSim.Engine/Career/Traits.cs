using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class Trait
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Shown when the trait isn't offered yet, e.g. "파워 55 이상".</summary>
        public string Requirement;
        public Func<CareerPlayer, bool> Eligible;
        /// <summary>Game-day rating bonus, for the build traits; can go past potential.</summary>
        public Stat? BoostStat;
    }

    /// <summary>
    /// Picked by the user as the player grows: a slot opens at each total-rating milestone and three
    /// traits are offered, mostly shaped by what the player trained. Turns training into a build.
    /// </summary>
    public static class Traits
    {
        public const string Slugger = "slugger";
        public const string Contact = "contact";
        public const string Eye = "eye";
        public const string Speed = "speed";
        public const string Glove = "glove";
        public const string Grinder = "grinder";
        public const string Iron = "iron";
        public const string Recovery = "recovery";
        public const string LateBloomer = "late_bloomer";
        public const string Fireballer = "fireballer";
        public const string Pinpoint = "pinpoint";
        public const string Junkballer = "junkballer";
        public const string Workhorse = "workhorse";

        public const int Boost = 6;
        /// <summary>Total of the five ratings at which each slot opens. A new player starts near 190 and peaks around 350.</summary>
        public static readonly int[] Milestones = { 210, 250, 290, 330, 370 };

        public static readonly List<Trait> All = Build();

        public static Trait Find(string id) => All.Find(t => t.Id == id);

        /// <summary>A pitcher's four ratings count five-fourths, so both reach the milestones at the same pace.</summary>
        public static int RatingTotal(CareerPlayer me) => me.MyStats.Sum(s => me.Ratings[s]) * 5 / me.MyStats.Count();

        public static int SlotsEarned(CareerPlayer me) => Milestones.Count(m => RatingTotal(me) >= m);

        public static int? NextMilestone(CareerPlayer me) => Milestones.Where(m => RatingTotal(me) < m).Cast<int?>().FirstOrDefault();

        /// <summary>Three traits the player qualifies for and doesn't have, at random.</summary>
        public static List<Trait> Offer(CareerPlayer me, IRandom random) =>
            All.Where(t => !me.Traits.Contains(t.Id) && t.Eligible(me))
                .OrderBy(_ => random.NextDouble())
                .Take(3)
                .ToList();

        public static int BoostFor(CareerPlayer me, Stat stat) =>
            me.Traits.Select(Find).Count(t => t != null && t.BoostStat == stat) * Boost;

        static List<Trait> Build()
        {
            var list = new List<Trait>();
            void Add(string id, string name, string description, string requirement, Func<CareerPlayer, bool> eligible, Stat? boost = null) =>
                list.Add(new Trait { Id = id, Name = name, Description = description, Requirement = requirement, Eligible = eligible, BoostStat = boost });
            Func<CareerPlayer, bool> AtLeast(Stat stat, int rating) => me => me.Ratings.TryGetValue(stat, out var r) && r >= rating;

            Add(Slugger, "거포 본능", $"경기에서 파워 +{Boost}", "파워 55 이상", AtLeast(Stat.Power, 55), Stat.Power);
            Add(Contact, "정교한 타격", $"경기에서 컨택 +{Boost}", "컨택 55 이상", AtLeast(Stat.Contact, 55), Stat.Contact);
            Add(Eye, "선구안의 달인", $"경기에서 선구안 +{Boost}", "선구안 55 이상", AtLeast(Stat.Eye, 55), Stat.Eye);
            Add(Speed, "준족", $"경기에서 주루 +{Boost}", "주루 55 이상", AtLeast(Stat.Speed, 55), Stat.Speed);
            Add(Glove, "철벽 수비", $"경기에서 수비 +{Boost}", "수비 55 이상", AtLeast(Stat.Defense, 55), Stat.Defense);
            Add(Fireballer, "파이어볼러", $"경기에서 구속 +{Boost}", "구속 55 이상", AtLeast(Stat.Velocity, 55), Stat.Velocity);
            Add(Pinpoint, "핀포인트 제구", $"경기에서 제구 +{Boost}", "제구 55 이상", AtLeast(Stat.Control, 55), Stat.Control);
            Add(Junkballer, "마구", $"경기에서 변화구 +{Boost}", "변화구 55 이상", AtLeast(Stat.Breaking, 55), Stat.Breaking);
            Add(Workhorse, "이닝 이터", $"경기에서 체력 +{Boost}", "체력 55 이상", AtLeast(Stat.Stamina, 55), Stat.Stamina);
            Add(AtBatChoice.Reader, "노림수 장인", "찬스 타석 '느낌' 적중률 +15%", "선구안 50 이상", AtLeast(Stat.Eye, 50));
            Add(AtBatChoice.Clutch, "찬스에 강함", "노림수가 맞으면 장타가 더 잘 나옴", "파워 50 이상", AtLeast(Stat.Power, 50));
            Add(AtBatChoice.Gambler, "승부사", "노림수가 빗나가도 손해가 절반", "컨택 50 이상", AtLeast(Stat.Contact, 50));
            Add(Grinder, "연습벌레", "훈련 경험치 +25%", "누구나", _ => true);
            Add(Iron, "강철 체력", "훈련·경기 피로 -30%", "누구나", _ => true);
            Add(Recovery, "빠른 회복", "부상 기간 -40%", "누구나", _ => true);
            Add(LateBloomer, "대기만성", "28세부터 성장 속도 1.6배", "누구나", _ => true);
            return list;
        }
    }
}
