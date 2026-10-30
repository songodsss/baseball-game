using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class EventChoice
    {
        public string Label;
        /// <summary>What happens, told after the choice is made.</summary>
        public string Result;
        public Action<EventEffects> Apply;
    }

    /// <summary>
    /// A small story beat between days: a coach's offer, a slump, a fan letter. The player picks one
    /// of the choices. Effects are small on purpose: they colour a career, they don't decide it.
    /// </summary>
    public class StoryEvent
    {
        public string Id;
        public string Title;
        /// <summary>{name} and {team} are filled in.</summary>
        public string Text;
        /// <summary>Relative odds among the events that can happen today; 0 = not today.</summary>
        public Func<Career, DayResult, double> Weight;
        public List<EventChoice> Choices = new List<EventChoice>();

        public string TextFor(Career career) => Text.Replace("{name}", career.Me.Name).Replace("{team}", career.MyTeam.Name);
    }

    public class EventOutcome
    {
        public string Text;
        /// <summary>Changes in words, e.g. "컨택 +1", "피로 +15".</summary>
        public List<string> Changes = new List<string>();
        public Dictionary<Stat, int> PointsGained = new Dictionary<Stat, int>();
    }

    /// <summary>What an event choice can do to the player; every change is noted for the result card.</summary>
    public class EventEffects
    {
        readonly Career _career;
        readonly IRandom _random;
        public readonly EventOutcome Outcome;

        internal EventEffects(Career career, IRandom random, EventOutcome outcome)
        {
            _career = career;
            _random = random;
            Outcome = outcome;
        }

        CareerPlayer Me => _career.Me;

        public void Exp(Stat stat, int amount)
        {
            int points = Me.GainExp(stat, amount);
            if (points == 0) return;
            Outcome.PointsGained.TryGetValue(stat, out var p);
            Outcome.PointsGained[stat] = p + points;
            Outcome.Changes.Add($"{Stats.Name(stat)} +{points}");
        }

        public void Fatigue(int delta)
        {
            int before = Me.Fatigue;
            Me.Fatigue = Math.Max(0, Math.Min(100, Me.Fatigue + delta));
            if (Me.Fatigue != before) Outcome.Changes.Add($"피로 {Signed(Me.Fatigue - before)}");
        }

        public void Money(int delta)
        {
            Me.Money = Math.Max(0, Me.Money + delta);
            Outcome.Changes.Add($"보유금 {Signed(delta)}만원");
        }

        public void Morale(int delta)
        {
            int before = Me.Morale;
            Me.Morale = Math.Max(-CareerPlayer.MaxMorale, Math.Min(CareerPlayer.MaxMorale, Me.Morale + delta));
            if (Me.Morale != before) Outcome.Changes.Add($"사기 {Signed(Me.Morale - before)}");
        }

        public void ShortenInjury(int days)
        {
            if (!Me.IsInjured) return;
            int before = Me.InjuryDaysLeft;
            Me.InjuryDaysLeft = Math.Max(0, Me.InjuryDaysLeft - days);
            Outcome.Changes.Add($"부상 {before}일 → {Me.InjuryDaysLeft}일");
        }

        /// <summary>Rolls for a small injury; returns true if it happened.</summary>
        public bool MaybeInjure(double chance, int days)
        {
            if (Me.IsInjured || _random.NextDouble() >= chance) return false;
            Me.InjuryDaysLeft = days;
            Outcome.Changes.Add($"부상 {days}일");
            return true;
        }

        public void Say(string text) => Outcome.Text += "\n" + text;

        static string Signed(int v) => v >= 0 ? $"+{v:N0}" : $"{v:N0}";
    }

    public static class StoryEvents
    {
        public static readonly List<StoryEvent> All = Build();

        public static StoryEvent Find(string id) => All.Find(e => e.Id == id);

        /// <summary>Weighted pick among events possible today; null if none.</summary>
        public static StoryEvent Pick(Career career, DayResult day, IRandom random)
        {
            var options = All.Select(e => (Event: e, Weight: e.Weight(career, day))).Where(o => o.Weight > 0).ToList();
            double total = options.Sum(o => o.Weight);
            if (total <= 0) return null;
            double roll = random.NextDouble() * total;
            foreach (var o in options)
            {
                roll -= o.Weight;
                if (roll < 0) return o.Event;
            }
            return options.Last().Event;
        }

        static bool PlayedWell(DayResult d) => d.MyLine != null && (d.MyLine.HomeRuns > 0 || d.MyLine.Hits >= 3);

        static List<StoryEvent> Build()
        {
            var list = new List<StoryEvent>();

            list.Add(new StoryEvent
            {
                Id = "coach_extra",
                Title = "코치의 특별 지도",
                Text = "타격 코치가 {name}에게 다가왔다.\n\"오늘 끝나고 30분만 더 하자. 스윙이 조금 늦어.\"",
                Weight = (c, d) => c.Me.IsInjured ? 0 : 1,
                Choices =
                {
                    new EventChoice { Label = "남아서 배운다", Result = "땀범벅이 됐지만 감이 잡혔다.", Apply = e => { e.Exp(Stat.Contact, 60); e.Fatigue(15); } },
                    new EventChoice { Label = "오늘은 쉬겠다고 한다", Result = "\"그래, 몸 관리도 실력이지.\"", Apply = e => e.Fatigue(-10) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_advice",
                Title = "선배의 조언",
                Text = "베테랑 선배가 라커룸에서 말을 걸었다.\n\"신인 때는 하나만 확실히 해. 뭘 배우고 싶어?\"",
                Weight = (c, d) => c.Me.Age <= 23 ? 1 : 0,
                Choices =
                {
                    new EventChoice { Label = "힘 쓰는 법", Result = "하체를 쓰는 요령을 배웠다.", Apply = e => e.Exp(Stat.Power, 50) },
                    new EventChoice { Label = "공 보는 법", Result = "투수의 버릇을 읽는 법을 배웠다.", Apply = e => e.Exp(Stat.Eye, 50) },
                    new EventChoice { Label = "베이스 도는 법", Result = "스타트 타이밍을 배웠다.", Apply = e => e.Exp(Stat.Speed, 50) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "slump",
                Title = "슬럼프",
                Text = "{name}, 최근 경기에서 안타가 없다.\n배트가 무겁게 느껴진다.",
                Weight = (c, d) => c.HitlessStreak >= 3 ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "영상을 돌려본다", Result = "타이밍이 반 박자 늦었던 걸 찾았다.", Apply = e => { e.Exp(Stat.Eye, 30); e.Morale(4); } },
                    new EventChoice { Label = "하루 푹 쉰다", Result = "머리를 비우니 몸이 가벼워졌다.", Apply = e => { e.Fatigue(-25); e.Morale(5); } },
                    new EventChoice { Label = "특타로 정면 돌파", Result = "손바닥이 다 까졌다. 그래도 뭔가 남았다.", Apply = e => { e.Exp(Stat.Contact, 50); e.Fatigue(20); e.Morale(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "fan_letter",
                Title = "팬레터",
                Text = "오늘 경기를 본 어린이 팬에게서 편지가 왔다.\n\"{name} 선수처럼 되고 싶어요!\"",
                Weight = (c, d) => PlayedWell(d) ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "답장을 쓴다", Result = "편지를 쓰며 처음 야구를 시작한 날이 떠올랐다.", Apply = e => e.Morale(6) },
                    new EventChoice { Label = "사인 공을 보낸다", Result = "구단 SNS에 소식이 올라왔다.", Apply = e => { e.Morale(4); e.Money(-20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "commercial",
                Title = "광고 제의",
                Text = "스포츠 음료 회사에서 광고 촬영 제의가 들어왔다.\n촬영은 하루 종일 걸린다고 한다.",
                Weight = (c, d) => c.SeasonLine.PlateAppearances >= 30 && Career.Ops(c.SeasonLine) >= 0.8 ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "찍는다", Result = "어색한 미소였지만 출연료는 두둑했다.", Apply = e => { e.Money(400); e.Fatigue(10); } },
                    new EventChoice { Label = "야구에 집중한다", Result = "감독이 그 이야기를 듣고 고개를 끄덕였다.", Apply = e => e.Morale(3) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "night_out",
                Title = "원정 숙소의 밤",
                Text = "원정 숙소. 동기들이 맛집에 가자고 문을 두드린다.\n내일도 경기가 있다.",
                Weight = (c, d) => c.Me.IsInjured ? 0 : 1,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "따라 나간다", Result = "오랜만에 크게 웃었다.",
                        Apply = e =>
                        {
                            e.Morale(5);
                            e.Fatigue(15);
                            if (e.MaybeInjure(0.1, 2)) e.Say("돌아오는 길에 계단에서 발목을 삐끗했다...");
                        },
                    },
                    new EventChoice { Label = "방에서 쉰다", Result = "일찍 잠들었다.", Apply = e => e.Fatigue(-10) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "rehab",
                Title = "재활",
                Text = "트레이너가 재활 프로그램을 제안했다.\n개인 비용이 들지만 복귀가 빨라진다고 한다.",
                Weight = (c, d) => c.Me.InjuryDaysLeft >= 2 ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "받는다 (300만원)", Result = "꼼꼼한 관리 덕에 회복이 빨라졌다.", Apply = e => { e.Money(-300); e.ShortenInjury(2); } },
                    new EventChoice { Label = "천천히 회복한다", Result = "조급해하지 않기로 했다.", Apply = e => e.Morale(1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "family_visit",
                Title = "가족의 응원",
                Text = "부모님이 {team} 홈구장을 찾았다.\n관중석에서 손을 흔드는 모습이 보인다.",
                Weight = (c, d) => 0.7,
                Choices =
                {
                    new EventChoice { Label = "손을 흔들어 답한다", Result = "어깨에 힘이 들어간다.", Apply = e => e.Morale(7) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "rival",
                Title = "라이벌",
                Text = "같은 포지션에 거포 신인이 들어왔다.\n기자들이 둘을 비교하기 시작했다.",
                Weight = (c, d) => c.Me.Age >= 24 && c.Me.Age <= 32 ? 0.7 : 0,
                Choices =
                {
                    new EventChoice { Label = "자극제로 삼는다", Result = "새벽 훈련을 시작했다.", Apply = e => { e.Exp(Stat.Power, 30); e.Exp(Stat.Contact, 30); e.Fatigue(15); } },
                    new EventChoice { Label = "내 야구를 한다", Result = "흔들리지 않기로 했다.", Apply = e => e.Morale(3) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "gear_sponsor",
                Title = "용품 협찬",
                Text = "용품 회사 담당자가 찾아왔다.\n\"저희 장갑 한번 써 보시겠어요? 사용료도 드립니다.\"",
                Weight = (c, d) => d.MyLine != null && d.MyLine.Hits >= 2 ? 1 : 0.2,
                Choices =
                {
                    new EventChoice { Label = "계약한다", Result = "손에 착 감긴다.", Apply = e => e.Money(250) },
                    new EventChoice { Label = "쓰던 걸 쓴다", Result = "익숙한 게 최고다.", Apply = e => e.Morale(2) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "defense_drill",
                Title = "펑고 100개",
                Text = "수비 코치가 펑고 배트를 들었다.\n\"오늘 실책 하나 했지? 100개 받고 가.\"",
                Weight = (c, d) => c.Me.IsInjured ? 0 : 0.8,
                Choices =
                {
                    new EventChoice { Label = "다 받는다", Result = "유니폼이 흙투성이가 됐다.", Apply = e => { e.Exp(Stat.Defense, 60); e.Fatigue(15); } },
                    new EventChoice { Label = "50개만 부탁한다", Result = "\"다음엔 100개다.\"", Apply = e => { e.Exp(Stat.Defense, 25); e.Fatigue(5); } },
                },
            });

            return list;
        }
    }
}
