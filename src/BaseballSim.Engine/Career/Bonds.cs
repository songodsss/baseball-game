using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Farewells and after-stories for the people the player grew close to (closeness of Career.CloseBond or
    /// more): the veteran, the coach, the junior, and main-story characters recorded with Career.Befriend.
    /// Nobody can look after every senior; these are the ones the choices made close. The roommate senior and
    /// the draft-class mate have their own arcs in Teammates.cs.
    /// </summary>
    public static partial class StoryEvents
    {
        /// <summary>Seasons a close senior plays on after the player meets him: 3 to 6, fixed by his name.</summary>
        static int SeasonsTogether(Career c, string who)
        {
            string name = c.BondName(who);
            int h = 0;
            foreach (char ch in name) h = h * 31 + ch;
            return 3 + (h & 0x7fffffff) % 4;
        }

        static int SeasonsSinceMet(Career c, string who) => c.Season - c.MetStamp(who) / 1000;

        /// <summary>A farewell or after-story whose time has come for a close bond; null otherwise.</summary>
        static StoryEvent BondMoment(Career c)
        {
            foreach (var who in c.Bonds.OrderByDescending(c.Closeness))
            {
                if (!c.IsClose(who)) continue;
                bool leaves = who == "veteran" || who == "coach" || who.StartsWith("story:");
                if (leaves && !c.HasMark("gone:" + who) && !c.HasMark("gone:ex" + who)
                    && SeasonsSinceMet(c, who) >= SeasonsTogether(c, who) && Progress(c) >= 0.6 && Progress(c) < 0.95 && !c.InFarm)
                    return who == "coach" ? CoachFarewell(c) : SeniorFarewell(who);

                bool gone = who == "exveteran" || who == "excoach" || who.StartsWith("story:");
                if (gone && c.HasMark("gone:" + who) && SeasonsSince(c, "gone:" + who) >= 2 && !c.Seen("after:" + who))
                    return who == "excoach" ? CoachAfter() : SeniorAfter(c, who);

                if (who == "junior" && SeasonsSinceMet(c, who) >= 3 && !c.Seen("junior_rise") && Progress(c) >= 0.4)
                    return JuniorRise;
                if (who == "junior" && c.Seen("junior_rise") && c.Me.Age >= 34 && !c.Seen("junior_captain"))
                    return JuniorCaptain;
            }
            return null;
        }

        static StoryEvent SeniorFarewell(string who)
        {
            string x = "{" + who + "}";
            string after = who == "veteran" ? "{exveteran}" : x; // results are filled after PartWith renames him
            return new StoryEvent
            {
                Id = "farewell:" + who,
                Title = "함께한 선배의 은퇴",
                Text = $"{x} 선배가 이번 경기를 끝으로 유니폼을 벗는다.\n함께 땀 흘린 시간이 떠오른다. 경기가 끝나고 선배가 {{name}}을 찾았다.",
                Weight = (_, __) => 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "말없이 안아 드린다", Result = $"{after} 선배가 등을 두 번 두드렸다. \"고마웠다.\"",
                        Apply = e => { e.Career.PartWith(who); e.Morale(6); },
                    },
                    new EventChoice
                    {
                        Label = "유니폼을 바꿔 입는다", Result = $"서로의 등번호에 사인을 했다. {after} 선배의 유니폼은 사물함 맨 위에 걸렸다.",
                        Apply = e => { e.Career.PartWith(who); e.Morale(5); e.Fans(20); },
                    },
                    new EventChoice
                    {
                        Label = "마지막으로 한 수 청한다", Result = "\"잘될 때 더 조심해. 무너지는 건 늘 그때더라.\"",
                        Apply = e => { e.Career.PartWith(who); e.Exp(Stat.Eye, 40); e.Morale(3); },
                    },
                },
            };
        }

        static StoryEvent SeniorAfter(Career c, string who)
        {
            string x = "{" + who + "}";
            string name = c.BondName(who);
            int pick = 0;
            foreach (char ch in name) pick += ch;
            switch (pick % 3)
            {
                case 0:
                    return new StoryEvent
                    {
                        Id = "after:" + who,
                        Title = "선배의 야구교실",
                        Text = $"은퇴한 {x} 선배가 동네에 유소년 야구교실을 열었다.\n\"애들이 네 팬이래. 한 번 와 줄래?\"",
                        Weight = (_, __) => 0,
                        Choices =
                        {
                            new EventChoice { Label = "쉬는 날 찾아간다", Result = "꼬마들이 다리에 매달렸다. 선배가 뒤에서 흐뭇하게 웃었다.", Apply = e => { e.Morale(6); e.Fans(30); e.Fatigue(5); } },
                            new EventChoice { Label = "사인볼 한 상자를 보낸다 (100만원)", Result = $"\"역시 내 후배다.\" {x} 선배에게서 사진이 왔다.", Apply = e => { e.Money(-100); e.Morale(4); e.Fans(40); } },
                        },
                    };
                case 1:
                    return new StoryEvent
                    {
                        Id = "after:" + who,
                        Title = "상대 팀 코치가 된 선배",
                        Text = $"오늘 상대 팀 1루 코치 박스에 낯익은 얼굴이 서 있었다.\n{x} 선배가 코치가 되어 돌아왔다.",
                        Weight = (_, __) => 0,
                        Choices =
                        {
                            new EventChoice { Label = "경기 전에 모자를 벗고 인사한다", Result = "\"오늘은 봐주는 거 없다.\" 선배가 웃으며 손을 흔들었다.", Apply = e => e.Morale(5) },
                            new EventChoice { Label = "\"오늘은 제가 이깁니다\"", Result = "1루를 밟을 때마다 선배가 혀를 찼다. 둘 다 즐거웠다.", Apply = e => { e.Morale(3); e.Fans(25); } },
                        },
                    };
                default:
                    return new StoryEvent
                    {
                        Id = "after:" + who,
                        Title = "선배의 편지",
                        Text = $"해외 연수 중인 {x} 선배에게서 편지가 왔다.\n'네 경기는 여기서도 다 챙겨 본다. 요즘 타이밍이 반 박자 빠르더라.'",
                        Weight = (_, __) => 0,
                        Choices =
                        {
                            new EventChoice { Label = "영상을 다시 돌려 본다", Result = "정말이었다. 선배 눈은 여전하다.", Apply = e => { e.Exp(Stat.Eye, 40); e.Morale(3); } },
                            new EventChoice { Label = "긴 답장을 쓴다", Result = "쓰다 보니 신인 시절 이야기로 세 장이 넘었다.", Apply = e => e.Morale(6) },
                        },
                    };
            }
        }

        static StoryEvent CoachFarewell(Career c) => new StoryEvent
        {
            Id = "farewell:coach",
            Title = "코치와의 이별",
            Text = "{coach} 코치가 계약이 끝나 팀을 떠난다.\n짐을 챙기던 코치가 {name}을 불렀다. \"너 처음 왔을 때 스윙, 아직도 기억난다.\"",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice
                {
                    Label = "고개 숙여 인사한다", Result = "\"감사했습니다.\" 그 한마디에 코치 눈가가 붉어졌다.",
                    Apply = e => { e.Career.PartWith("coach"); e.Morale(5); },
                },
                new EventChoice
                {
                    Label = "마지막 숙제를 받는다", Result = "\"하체. 하체만 잊지 마라.\" 노트 첫 장에 크게 적었다.",
                    Apply = e => { e.Career.PartWith("coach"); e.Exp(Stat.Power, 40); e.Morale(3); },
                },
            },
        };

        static StoryEvent CoachAfter() => new StoryEvent
        {
            Id = "after:excoach",
            Title = "적장이 된 스승",
            Text = "상대 팀 새 감독은 {excoach}, 예전에 나를 가르치던 그 코치다.\n경기 전, 감독이 먼저 손을 내밀었다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "두 손으로 악수한다", Result = "\"많이 컸다.\" 짧은 말에 많은 게 담겨 있었다.", Apply = e => { e.Morale(6); e.Trust(1); } },
                new EventChoice { Label = "\"오늘 한 수 보여 드릴게요\"", Result = "감독이 껄껄 웃었다. \"그래, 그 말 기다렸다.\"", Apply = e => { e.Morale(4); e.Fans(30); } },
            },
        };

        static readonly StoryEvent JuniorRise = new StoryEvent
        {
            Id = "junior_rise",
            Title = "후배의 성장",
            Text = "{junior}가 어느새 팀 주전이 됐다.\n수훈 선수 인터뷰에서 {junior}가 말했다. \"{name} 선배 덕분입니다.\"",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "모른 척 쑥스러워한다", Result = "라커룸에서 {junior}의 머리를 한 번 헝클어뜨렸다.", Apply = e => e.Morale(6) },
                new EventChoice { Label = "축하 회식을 쏜다 (100만원)", Result = "\"선배님이 사 주는 고기는 다르네요!\" 그 말이 어디서 들어 본 것 같다.", Apply = e => { e.Money(-100); e.Morale(7); e.Trust(1); } },
            },
        };

        static readonly StoryEvent JuniorCaptain = new StoryEvent
        {
            Id = "junior_captain",
            Title = "주장 완장",
            Text = "새 주장으로 {junior}가 뽑혔다.\n완장을 받으러 가던 {junior}가 {name} 앞에서 멈췄다. \"선배님한테 배운 대로 할게요.\"",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "어깨를 두드려 준다", Result = "이제 팀은 걱정 없겠다. 그런 생각이 들었다.", Apply = e => { e.Morale(8); e.Close("junior", 1); } },
            },
        };
    }
}
