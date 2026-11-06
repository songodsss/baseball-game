using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// More story beats that come up on their own while raising a player, so each season feels a little
    /// different without adding anything to do each day:
    /// follow-ups (a choice comes back weeks later), game-day surprises, the calendar (opening day,
    /// rainy season, the last home game), the people around the player (manager, juniors, family),
    /// record chases, interviews and the all-star home run derby.
    /// </summary>
    public static partial class StoryEvents
    {
        static bool Played(DayResult d) => d.MyLine != null && d.MyLine.PlateAppearances > 0 || d.MyPitching != null;

        /// <summary>Played at home today.</summary>
        static bool HomeGame(Career c, DayResult d) => d.MyGame != null && d.MyGame.Home == c.MyTeam;

        static bool Active(Career c) => !c.Me.IsInjured && !c.InMilitary;

        /// <summary>A few weeks of games: how long before a follow-up comes back.</summary>
        static int Gap(Career c) => Math.Max(3, c.League.GamesPerSeason / 8);

        static double Progress(Career c) => (double)c.Day / c.League.GamesPerSeason;

        static bool InWindow(Career c, double from, double to) => Progress(c) >= from && Progress(c) < to;

        /// <summary>A follow-up is ready: its mark is there and old enough.</summary>
        static bool Due(Career c, string mark, int gap) => c.GamesSince(mark) >= gap;

        static bool MadeAllStar(Career c) => c.SeasonNotes.Any(n => n.Contains("올스타"));

        static void AddLifeEvents(List<StoryEvent> list)
        {
            // ---------- Follow-ups: a choice now, its answer weeks later ----------

            list.Add(new StoryEvent
            {
                Id = "junior_sneak",
                Title = "몰래 나간 후배",
                Text = "통금 시간. 막내 후배 {junior}가 숙소 담을 넘다가 {name}과 눈이 마주쳤다.\n\"선배님... 한 번만 봐 주세요.\"",
                Weight = (c, d) => c.Me.Age >= 21 && !c.Seen("junior_sneak") ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "못 본 걸로 한다", Result = "후배가 몇 번이나 고개를 숙였다.", Apply = e => { e.Mark("junior_owes"); e.Morale(1); e.Close("junior", 2); } },
                    new EventChoice { Label = "따끔하게 타이른다", Result = "\"내일 경기 생각해.\" 후배가 조용히 방으로 돌아갔다.", Apply = e => { e.Morale(2); e.Close("junior", 1); } },
                    new EventChoice { Label = "코치에게 알린다", Result = "규칙은 규칙이다. 코치가 고개를 끄덕였다.", Apply = e => { e.Trust(2); e.Morale(-1); e.Close("junior", -1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "junior_repay",
                Title = "후배의 보답",
                Text = "그때 봐줬던 {junior}가 전력분석 노트를 들고 왔다.\n\"선배님, 다음 상대 버릇이요. 제가 밤새 찾았어요.\"",
                Weight = (c, d) => Due(c, "junior_owes", Gap(c)) && Active(c) ? 5 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "고맙다고 밥을 산다", Result = "노트는 생각보다 훨씬 꼼꼼했다. 후배가 환하게 웃었다.",
                        Apply = e => { e.Unmark("junior_owes"); e.Exp(Stat.Eye, 50); e.Boost(Stat.Eye, 20, 7, "후배의 분석 노트"); e.Money(-50); e.Morale(4); e.Close("junior", 1); },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "camp_coach",
                Title = "특별 인스트럭터",
                Text = "구단이 시즌 초반 2주 동안 외부 인스트럭터를 불렀다.\n\"한 가지만 제대로 잡아 줄게. 뭘 고치고 싶어?\"",
                Weight = (c, d) => InWindow(c, 0.03, 0.15) && Active(c) && !c.SeenThisSeason("camp_coach") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "기술 (컨택·제구)", Result = "동작이 한결 간결해졌다. 2주만 더 붙잡자.", Apply = e => e.Boost(Stat.Contact, 25, 14, "특별 인스트럭터") },
                    new EventChoice { Label = "힘 (파워·구속)", Result = "하체를 쓰는 법을 처음부터 다시 배웠다.", Apply = e => e.Boost(Stat.Power, 25, 14, "특별 인스트럭터") },
                    new EventChoice { Label = "눈 (선구안·변화구)", Result = "공이 한결 오래 보이기 시작했다.", Apply = e => e.Boost(Stat.Eye, 25, 14, "특별 인스트럭터") },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "orphanage",
                Title = "보육원 방문",
                Text = "구단 봉사활동으로 보육원을 찾았다.\n한 아이가 {name}의 손을 잡고 물었다. \"다음 경기에서 이겨 줄 거예요?\"",
                Weight = (c, d) => !c.HasMark("promise") && !c.SeenThisSeason("orphanage") ? 0.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"꼭 해낼게\" 약속한다", Result = "새끼손가락을 걸었다. 이제 지켜야 한다.", Apply = e => { e.Mark("promise"); e.Morale(3); } },
                    new EventChoice { Label = "사인과 사진만 찍어 준다", Result = "아이들이 사인볼을 꼭 끌어안았다.", Apply = e => { e.Fans(20); e.Morale(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "promise_kept",
                Title = "약속을 지킨 날",
                Text = "오늘 경기가 끝나자 보육원에서 영상 편지가 왔다.\n아이들이 TV 앞에서 방방 뛰고 있다.",
                Weight = (c, d) => c.HasMark("promise") && PlayedWell(d) ? 8 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "답장 영상을 찍는다", Result = "구단 SNS에 올라간 영상이 하루 만에 퍼졌다.",
                        Apply = e => { e.Unmark("promise"); e.Fans(120); e.Morale(8); },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "promise_late",
                Title = "아직 못 지킨 약속",
                Text = "보육원에서 편지가 왔다.\n\"괜찮아요. 우리는 계속 응원할 거예요!\"",
                Weight = (c, d) => Due(c, "promise", Gap(c) * 2) ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "다시 찾아간다", Result = "아이들과 캐치볼을 했다. 약속은 다음에 꼭.", Apply = e => { e.Unmark("promise"); e.Fans(30); e.Morale(3); e.Fatigue(5); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "investment",
                Title = "솔깃한 제안",
                Text = "선배의 지인이 카페를 차린다며 투자를 권했다.\n\"1,000만원이면 돼. 잘되면 두 배는 돌려줄게.\"",
                Weight = (c, d) => c.Me.Age >= 23 && c.Me.Money >= 2000 && !c.Seen("investment") ? 0.4 : 0,
                Choices =
                {
                    new EventChoice { Label = "투자한다 (1,000만원)", Result = "계약서에 사인했다. 잘되겠지?", Apply = e => { e.Money(-1000); e.Mark("invested"); } },
                    new EventChoice { Label = "정중히 거절한다", Result = "\"야구만 하겠습니다.\"", Apply = e => e.Morale(1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "investment_result",
                Title = "카페 소식",
                Text = "투자했던 카페에서 연락이 왔다.",
                Weight = (c, d) => Due(c, "invested", Gap(c) * 2) ? 5 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "소식을 듣는다", Result = "",
                        Apply = e =>
                        {
                            e.Unmark("invested");
                            if (e.Chance(0.5))
                            {
                                e.Say("동네 명소가 됐다고 한다! 약속대로 두 배를 돌려받았다.");
                                e.Money(2000);
                                e.Morale(4);
                            }
                            else
                            {
                                e.Say("가게가 문을 닫았다... 비싼 수업료였다.");
                                e.Morale(-3);
                            }
                        },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "routine",
                Title = "나만의 루틴",
                Text = "잘 풀린 날 아침에 먹은 게 김치찌개였다.\n동료가 웃으며 말한다. \"그거 루틴으로 하자.\"",
                Weight = (c, d) => PlayedWell(d) && !c.HasMark("routine") && !c.Seen("routine") ? 1 : 0,
                Choices =
                {
                    new EventChoice { Label = "경기 날엔 김치찌개", Result = "식당 아주머니가 {name} 자리를 비워 두기 시작했다.", Apply = e => { e.Mark("routine"); e.Morale(3); } },
                    new EventChoice { Label = "징크스는 안 믿는다", Result = "\"실력이지, 찌개가 아니야.\"", Apply = e => e.Morale(1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "routine_break",
                Title = "루틴의 배신",
                Text = "김치찌개를 꼬박꼬박 먹었는데 요즘 영 안 풀린다.\n동료들이 눈치를 본다.",
                Weight = (c, d) => c.HasMark("routine") && c.HitlessStreak >= 3 ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "루틴을 버린다", Result = "된장찌개를 시켰다. 마음이 이상하게 가벼워졌다.", Apply = e => { e.Unmark("routine"); e.Morale(4); } },
                    new EventChoice { Label = "끝까지 믿는다", Result = "오늘도 김치찌개. 국물까지 다 마셨다.", Apply = e => { e.Exp(Stat.Eye, 25); e.Morale(-1); } },
                },
            });

            // ---------- Game-day surprises ----------

            list.Add(new StoryEvent
            {
                Id = "bench_clear",
                Title = "벤치 클리어링",
                Text = "몸에 맞는 공이 두 번 연속 나오자 양 팀 선수들이 그라운드로 뛰쳐나왔다.",
                Weight = (c, d) => Played(d) && !c.InFarm ? 0.6 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "앞장서서 나간다", Result = "동료들 앞을 막아섰다. 관중석이 들끓었다.",
                        Apply = e =>
                        {
                            e.Morale(4);
                            e.Fans(30);
                            e.Trust(-1);
                            if (e.MaybeInjure(0.08, 2, InjuryKind.Wrist)) e.Say("몸싸움 중에 손목을 접질렸다...");
                        },
                    },
                    new EventChoice { Label = "동료들을 말린다", Result = "흥분한 동료를 붙잡았다. 감독이 고개를 끄덕였다.", Apply = e => { e.Trust(1); e.Morale(2); } },
                    new EventChoice { Label = "더그아웃을 지킨다", Result = "멀리서 지켜봤다. 괜히 다칠 필요는 없다.", Apply = e => e.Fatigue(-5) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "rain_delay",
                Title = "우천 중단",
                Text = "갑자기 쏟아진 비로 경기가 한 시간 넘게 멈췄다.\n방수포 위로 빗소리만 들린다.",
                Weight = (c, d) => Played(d) && !c.Me.IsPitcher ? 0.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "실내 연습장에서 스윙", Result = "땀이 식기 전에 감을 붙잡아 뒀다.", Apply = e => { e.Exp(Stat.Contact, 40); e.Fatigue(10); } },
                    new EventChoice { Label = "라커룸에서 쉰다", Result = "따뜻한 차 한 잔. 다리가 풀렸다.", Apply = e => e.Fatigue(-15) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "rain_delay_pitcher",
                Title = "비 그친 마운드",
                Text = "우천 중단 한 시간. 비가 그치자 투수 코치가 물었다.\n\"어깨 식었을 텐데, 다시 올라갈 수 있겠어?\"",
                Weight = (c, d) => d.MyPitching != null && d.MyPitching.Starts > 0 ? 0.8 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "다시 올라간다", Result = "젖은 마운드에서 끝까지 버텼다.",
                        Apply = e =>
                        {
                            e.Exp(Stat.Stamina, 50);
                            e.Fatigue(20);
                            e.Trust(1);
                            if (e.MaybeInjure(0.1, 3, InjuryKind.Shoulder)) e.Say("경기 뒤 어깨가 뻐근하다...");
                        },
                    },
                    new EventChoice { Label = "공을 넘긴다", Result = "\"무리할 때가 아니지.\" 불펜이 이어받았다.", Apply = e => e.Fatigue(-10) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "bunt_sign",
                Title = "번트 사인",
                Text = "8회 무사 1루. 3루 코치가 번트 사인을 냈다.\n그런데 오늘 {name}의 방망이는 뜨겁다.",
                Weight = (c, d) => PlayedWell(d) && !c.Me.IsPitcher && !c.InFarm ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "사인대로 번트", Result = "주자를 2루에 보냈다. 감독이 박수를 쳤다.", Apply = e => { e.Trust(2); e.Morale(-1); } },
                    new EventChoice
                    {
                        Label = "강공으로 간다", Result = "",
                        Apply = e =>
                        {
                            double p = 0.25 + e.Rating(Stat.Power) / 300.0;
                            if (e.Chance(p))
                            {
                                e.Say("초구를 받아쳐 우중간을 갈랐다! 결승 2루타!");
                                e.Fans(50);
                                e.Morale(5);
                                e.Trust(-1);
                            }
                            else
                            {
                                e.Say("힘없는 땅볼, 병살타... 감독이 고개를 돌렸다.");
                                e.Trust(-3);
                                e.Morale(-3);
                            }
                        },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "beanball",
                Title = "머리 쪽 공",
                Text = "시속 150km 공이 헬멧을 스치고 지나갔다.\n상대 투수는 모자도 안 벗는다.",
                Weight = (c, d) => Played(d) && !c.Me.IsPitcher ? 0.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "참고 1루로 걸어간다", Result = "다음 타석에서 갚아 주면 된다.", Apply = e => { e.Trust(1); e.Morale(1); } },
                    new EventChoice { Label = "마운드를 노려본다", Result = "관중석에서 함성이 터졌다. 심판이 둘을 떼어 놓았다.", Apply = e => { e.Fans(25); e.Morale(3); e.Trust(-1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foul_ball_kid",
                Title = "놓친 파울볼",
                Text = "관중석 맨 앞줄 아이가 파울볼을 놓치고 울먹인다.\n{name}의 손에는 방금 그 공이 있다.",
                Weight = (c, d) => Played(d) ? 0.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "이닝 끝나고 건넨다", Result = "아이가 공을 들고 펄쩍 뛰었다. 중계 카메라가 그걸 잡았다.", Apply = e => { e.Fans(40); e.Morale(3); } },
                    new EventChoice { Label = "사인까지 해서 준다", Result = "공에 이름을 적어 건넸다. 아이 아빠가 더 좋아한다.", Apply = e => { e.Fans(55); e.Morale(3); e.Fatigue(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "mound_visit",
                Title = "감독의 마운드 방문",
                Text = "7회 2사 만루. 감독이 직접 마운드에 올라왔다.\n\"{name}, 더 던질 수 있겠나?\"",
                Weight = (c, d) => d.MyPitching != null && d.MyPitching.Starts > 0 ? 0.8 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "\"끝까지 던지겠습니다\"", Result = "",
                        Apply = e =>
                        {
                            e.Fatigue(15);
                            e.Exp(Stat.Stamina, 30);
                            if (e.Chance(0.35 + e.Rating(Stat.Stamina) / 250.0))
                            {
                                e.Say("마지막 공, 헛스윙 삼진! 포효가 터져 나왔다.");
                                e.Trust(2);
                                e.Morale(5);
                            }
                            else
                            {
                                e.Say("밀어내기 볼넷... 감독이 말없이 공을 받아 갔다.");
                                e.Trust(-1);
                                e.Morale(-2);
                            }
                        },
                    },
                    new EventChoice { Label = "공을 건넨다", Result = "불펜이 불을 껐다. 다음엔 내가 끝내고 싶다.", Apply = e => e.Fatigue(-5) },
                },
            });

            // ---------- The manager ----------

            list.Add(new StoryEvent
            {
                Id = "trust_high",
                Title = "감독실 호출",
                Text = "감독이 {name}을 감독실로 불렀다.\n\"요즘 네가 하는 걸 다 보고 있다. 중요한 경기는 너한테 맡기겠다.\"",
                Weight = (c, d) => c.Me.ManagerTrust >= 4 && !c.SeenThisSeason("trust_high") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"믿음에 보답하겠습니다\"", Result = "문을 나서는데 가슴이 뜨거웠다.", Apply = e => { e.Morale(8); e.Fans(30); } },
                    new EventChoice { Label = "\"하나만 가르쳐 주십시오\"", Result = "감독이 직접 옛날 노트를 꺼내 보여 줬다.", Apply = e => { e.Exp(Stat.Eye, 60); e.Boost(Stat.Eye, 25, 10, "감독의 노트"); e.Morale(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "trust_low",
                Title = "감독 면담",
                Text = "감독이 팔짱을 끼고 말했다.\n\"요즘 네 마음대로더라. 팀이 먼저야, 알겠나?\"",
                Weight = (c, d) => c.Me.ManagerTrust <= -4 && !c.SeenThisSeason("trust_low") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "고개 숙여 사과한다", Result = "\"알면 됐다.\" 분위기가 조금 풀렸다.", Apply = e => { e.Trust(4); e.Morale(-2); } },
                    new EventChoice { Label = "실력으로 보여 드린다", Result = "그날 밤 혼자 남아 훈련했다.", Apply = e => { e.Exp(Stat.Contact, 50); e.Fatigue(15); e.Trust(1); } },
                },
            });

            // ---------- The calendar ----------

            list.Add(new StoryEvent
            {
                Id = "childrens_day",
                Title = "어린이날",
                Text = "어린이날 홈경기. 경기 전 그라운드가 꼬마 팬들로 가득하다.",
                Weight = (c, d) => InWindow(c, 0.12, 0.3) && !c.InFarm && !c.SeenThisSeason("childrens_day") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "캐치볼을 해 준다", Result = "공을 받을 때마다 아이들이 소리를 질렀다.", Apply = e => { e.Fans(50); e.Morale(3); e.Fatigue(5); } },
                    new EventChoice { Label = "사인볼을 나눠 준다 (100만원)", Result = "사인볼 상자가 순식간에 비었다.", Apply = e => { e.Money(-100); e.Fans(70); e.Morale(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "rainy_season",
                Title = "장마",
                Text = "일주일째 비. 경기는 줄줄이 취소되고 훈련은 실내에서만 한다.",
                Weight = (c, d) => InWindow(c, 0.45, 0.6) && !c.SeenThisSeason("rainy_season") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "웨이트장에서 산다", Result = "비 오는 동안 몸이 한 뼘 커졌다.", Apply = e => { e.Exp(Stat.Power, 30); e.Boost(Stat.Power, 30, 7, "장마 웨이트"); e.Fatigue(10); } },
                    new EventChoice { Label = "영상실에서 공부한다", Result = "리그 투수들의 공 배합을 외웠다.", Apply = e => { e.Exp(Stat.Eye, 25); e.Boost(Stat.Eye, 30, 7, "장마 영상 공부"); } },
                    new EventChoice { Label = "푹 쉬며 몸을 만든다", Result = "쌓인 피로가 빗물에 씻겨 나갔다.", Apply = e => { e.Fatigue(-25); e.Morale(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "heat_wave",
                Title = "폭염",
                Text = "낮 기온 37도. 그라운드에서 열기가 올라온다.\n선수들이 하나둘 지쳐 간다.",
                Weight = (c, d) => InWindow(c, 0.6, 0.78) && !c.SeenThisSeason("heat_wave") && Active(c) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "얼음 욕조에 들어간다", Result = "온몸이 얼얼하지만 다리가 살아났다.", Apply = e => e.Fatigue(-20) },
                    new EventChoice
                    {
                        Label = "그래도 특타", Result = "땀이 비 오듯 했다.",
                        Apply = e =>
                        {
                            e.Exp(Stat.Contact, 60);
                            e.Fatigue(25);
                            if (e.MaybeInjure(0.06, 2, InjuryKind.Hamstring)) e.Say("더위에 허벅지가 올라왔다...");
                        },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "chuseok",
                Title = "추석 연휴",
                Text = "추석인데 원정 경기다. 고향 집 단톡방에 송편 사진이 올라왔다.",
                Weight = (c, d) => InWindow(c, 0.8, 0.93) && !c.SeenThisSeason("chuseok") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "영상 통화로 인사한다", Result = "할머니가 화면 너머로 \"밥 잘 챙겨 먹어라\" 하신다.", Apply = e => e.Morale(5) },
                    new EventChoice { Label = "선수단과 송편을 빚는다", Result = "모양은 엉망이었지만 다들 크게 웃었다.", Apply = e => { e.Morale(3); e.Trust(1); } },
                },
            });

            // ---------- People ----------

            list.Add(new StoryEvent
            {
                Id = "veteran_return",
                Title = "돌아온 베테랑",
                Text = "같은 자리를 지키던 베테랑 {veteran} 선배가 부상에서 돌아왔다.\n\"자리 잘 지키고 있었네. 이제 다시 경쟁이다.\"",
                Weight = (c, d) => c.Me.Age <= 25 && !c.SeenThisSeason("veteran_return") && Active(c) ? 0.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "옆에서 배운다", Result = "베테랑의 준비 과정은 남달랐다.", Apply = e => { e.Exp(Stat.Defense, 40); e.Exp(Stat.Eye, 30); e.Close("veteran", 2); } },
                    new EventChoice { Label = "정면으로 경쟁한다", Result = "새벽 6시, 훈련장에 불을 제일 먼저 켰다.", Apply = e => { e.Exp(Stat.Contact, 45); e.Fatigue(15); e.Morale(3); e.Close("veteran", 1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "mentee",
                Title = "신인의 질문",
                Text = "후배 {junior}가 쭈뼛거리며 다가왔다.\n\"선배님처럼 하려면 뭐부터 해야 해요?\"",
                // Only while {junior} is still a rookie: once he's a regular (or the captain) this would make no sense.
                Weight = (c, d) => c.Me.Age >= 26 && !c.HasMark("mentee") && !c.SeenThisSeason("mentee") && !c.Seen("junior_rise") && !c.Seen("mentee_first") ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "직접 가르친다", Result = "가르치다 보니 내 기본기도 다시 보였다.", Apply = e => { e.Mark("mentee"); e.Exp(Stat.Defense, 30); e.Fatigue(10); e.Morale(2); e.Close("junior", 2); } },
                    new EventChoice { Label = "\"꾸준함이 전부야\"", Result = "짧은 한마디에 {junior}가 눈을 반짝였다.", Apply = e => e.Morale(1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "mentee_first",
                Title = "후배의 첫 홈런 공",
                Text = "가르쳤던 {junior}가 데뷔 첫 홈런을 쳤다.\n경기가 끝나자 그 공을 들고 {name}에게 왔다.\n\"선배님이 가지세요.\"",
                Weight = (c, d) => Due(c, "mentee", Gap(c)) && !c.Seen("junior_rise") && !c.Seen("mentee_first") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"네가 간직해\"", Result = "후배가 공을 꼭 쥐고 고개를 숙였다.", Apply = e => { e.Unmark("mentee"); e.Morale(6); e.Trust(1); e.Close("junior", 1); } },
                    new EventChoice { Label = "사물함에 모셔 둔다", Result = "내 첫 홈런 공 옆에 나란히 놓았다.", Apply = e => { e.Unmark("mentee"); e.Morale(8); e.Close("junior", 1); } },
                },
            });

            AddTeammateEvents(list);

            list.Add(new StoryEvent
            {
                Id = "hometown",
                Title = "고향 원정",
                Text = "고향 근처 원정 경기. 외야석에 '{name} 우리 동네 자랑' 플래카드가 걸렸다.",
                Weight = (c, d) => !c.SeenThisSeason("hometown") && !c.InFarm ? 0.4 : 0,
                Choices =
                {
                    new EventChoice { Label = "손을 크게 흔든다", Result = "동창들이 목이 터져라 이름을 불렀다.", Apply = e => e.Morale(6) },
                    new EventChoice { Label = "경기 뒤 밥을 산다 (150만원)", Result = "옛 친구들과 밤늦게까지 웃었다.", Apply = e => { e.Money(-150); e.Morale(8); e.Fans(20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "proposal",
                Title = "결혼 이야기",
                Text = "오래 만난 연인이 조심스럽게 말을 꺼냈다.\n\"우리 이제 결혼할까?\"",
                // Comes sooner after the blind-date romance; without it, from 29.
                Weight = (c, d) => c.Me.Age <= 34 && !c.HasMark("married") && !c.SeenThisSeason("proposal") && Romance.Lover(c) == null
                    && (c.HasMark("dating") ? c.Me.Age >= 25 && c.Seen("dating_news") : c.Me.Age >= 29) ? (c.HasMark("dating") ? 1.5 : 0.5) : 0,
                Choices =
                {
                    new EventChoice { Label = "시즌 중에 식을 올린다", Result = "동료들이 모두 와서 축가를 불렀다.", Apply = e => { e.Mark("married"); e.Unmark("dating"); e.Money(-500); e.Morale(8); e.Fatigue(10); } },
                    new EventChoice { Label = "시즌 끝나고 하자", Result = "\"좋아, 그때까지 다치지 마.\"", Apply = e => { e.Mark("married"); e.Unmark("dating"); e.Morale(5); } },
                    new EventChoice { Label = "조금만 더 생각하자", Result = "아직은 야구만 보고 싶다.", Apply = e => e.Morale(-2) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "first_child",
                Title = "아빠가 된 날",
                Text = "경기 전 전화가 울렸다. \"건강한 아기예요!\"\n{name}, 아빠가 됐다.",
                Weight = (c, d) => Due(c, "married", c.League.GamesPerSeason) && !c.Seen("first_child") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "하루 쉬고 병원으로", Result = "작은 손이 손가락을 꼭 쥐었다.", Apply = e => { e.Fatigue(-30); e.Morale(10); } },
                    new EventChoice { Label = "경기 뛰고 달려간다", Result = "그날 밤 '요람 세리머니'가 화제가 됐다.", Apply = e => { e.Morale(8); e.Fans(60); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "kid_at_park",
                Title = "아이의 첫 야구장",
                Text = "아이가 처음으로 야구장에 왔다.\n작은 유니폼에 {name}의 등번호가 박혀 있다.",
                Weight = (c, d) => c.Seen("first_child") && c.GamesSince("seen:first_child") >= c.League.GamesPerSeason && !c.Seen("kid_at_park") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "관중석에 손 하트", Result = "아이가 엄마 품에서 손을 흔들었다.", Apply = e => { e.Morale(8); e.Fans(30); } },
                },
            });

            // ---------- Record chases ----------

            list.Add(new StoryEvent
            {
                Id = "hit_streak",
                Title = "연속 안타 행진",
                Text = "{name}, 연속 경기 안타 행진 중이다.\n기자들이 매일 숫자를 묻는다.",
                Weight = (c, d) => c.HitStreak >= 7 && !c.SeenThisSeason("hit_streak") ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "기록은 잊는다", Result = "휴대폰을 껐다. 내일 공만 생각한다.", Apply = e => e.Morale(4) },
                    new EventChoice { Label = "한 타석씩 집중한다", Result = "타석마다 투수의 첫 공을 끝까지 봤다.", Apply = e => { e.Exp(Stat.Contact, 45); e.Fatigue(5); } },
                    new EventChoice { Label = "인터뷰에 응한다", Result = "기록 행진이 저녁 뉴스에 나왔다.", Apply = e => { e.Fans(60); e.Morale(-1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "avg_chase",
                Title = "꿈의 4할",
                Text = "시즌 후반인데 {name}의 타율이 아직 4할 근처다.\n'꿈의 기록' 기사가 쏟아진다.",
                Weight = (c, d) => !c.Me.IsPitcher && Progress(c) >= 0.6 && c.SeasonLine.PlateAppearances >= c.League.QualifyingPa * 0.6
                    && Stats.Avg(c.SeasonLine) >= 0.37 && !c.SeenThisSeason("avg_chase") ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "기록에 도전한다", Result = "남은 경기가 하나하나 결승전 같다.", Apply = e => { e.Exp(Stat.Contact, 50); e.Fatigue(10); e.Fans(50); } },
                    new EventChoice { Label = "팀 승리가 먼저다", Result = "\"제 기록보다 가을야구요.\" 감독이 미소 지었다.", Apply = e => { e.Trust(2); e.Morale(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "title_race",
                Title = "타이틀 경쟁",
                Text = "시즌 막바지. {name}이 개인 타이틀 경쟁의 맨 앞에 있다.\n2위와의 차이는 한 끗이다.",
                Weight = (c, d) => Progress(c) >= 0.75 && !c.SeenThisSeason("title_race") && LeadsABoard(c) ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "끝까지 경쟁한다", Result = "매일 순위표를 확인하게 됐다.", Apply = e => { e.Exp(Stat.Power, 40); e.Fatigue(10); e.Morale(3); } },
                    new EventChoice { Label = "평소처럼 한다", Result = "숫자는 따라오는 거다.", Apply = e => { e.Morale(4); e.Fatigue(-10); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "gem",
                Title = "완벽투",
                Text = "오늘 {name}은 상대 타선을 무실점으로 꽁꽁 묶었다.\n포수가 경기 공을 내밀었다.",
                Weight = (c, d) => d.MyPitching != null && d.MyPitching.Outs >= 18 && d.MyPitching.Runs == 0 && !c.SeenThisSeason("gem") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "공을 기념으로 간직한다", Result = "공에 날짜를 적어 진열장에 올렸다.", Apply = e => { e.Morale(6); e.Fans(40); } },
                    new EventChoice { Label = "바로 다음 경기를 준비한다", Result = "오늘 던진 공 배합을 노트에 적었다.", Apply = e => { e.Exp(Stat.Control, 40); e.Morale(3); } },
                },
            });

            // ---------- Interviews ----------

            list.Add(new StoryEvent
            {
                Id = "interview",
                Title = "수훈 선수 인터뷰",
                Text = "오늘의 수훈 선수는 {name}! 마이크가 앞에 놓였다.\n\"오늘 활약, 한마디 해 주시죠.\"",
                Weight = (c, d) => PlayedWell(d) && !c.HasMark("provoked") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"동료들 덕분이에요\"", Result = "겸손한 한마디에 더그아웃이 웃었다.", Apply = e => { e.Morale(2); e.Trust(1); e.Fans(20); } },
                    new EventChoice { Label = "\"이제 시작입니다\"", Result = "자신감 넘치는 말에 팬들이 열광했다.", Apply = e => { e.Morale(4); e.Fans(40); } },
                    new EventChoice { Label = "\"다음 상대도 자신 있어요\"", Result = "도발 섞인 한마디가 하루 종일 기사로 돌았다.", Apply = e => { e.Mark("provoked"); e.Fans(70); e.Morale(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "provoke_good",
                Title = "말한 대로",
                Text = "큰소리쳤던 그대로 해냈다.\n'{name}, 말과 실력 모두 진짜' 기사가 올라왔다.",
                Weight = (c, d) => c.GamesSince("provoked") >= 1 && PlayedWell(d) ? 8 : 0,
                Choices =
                {
                    new EventChoice { Label = "세리머니로 답한다", Result = "관중석이 같은 동작을 따라 했다.", Apply = e => { e.Unmark("provoked"); e.Fans(100); e.Morale(5); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "provoke_bad",
                Title = "부메랑",
                Text = "큰소리친 뒤로 영 안 풀린다.\n댓글창이 시끄럽다.",
                Weight = (c, d) => c.HasMark("provoked") && (c.HitlessStreak >= 2 || Due(c, "provoked", Gap(c))) ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "공개 사과한다", Result = "\"말이 앞섰습니다.\" 여론이 조금 가라앉았다.", Apply = e => { e.Unmark("provoked"); e.Morale(-1); e.Trust(1); } },
                    new EventChoice { Label = "실력으로 갚는다", Result = "이를 악물고 배트를 잡았다.", Apply = e => { e.Unmark("provoked"); e.Exp(Stat.Contact, 40); e.Morale(-3); e.Fatigue(10); } },
                },
            });
        }

        static bool LeadsABoard(Career c)
        {
            var boards = c.Me.IsPitcher ? new[] { Leaderboard.Wins, Leaderboard.Strikeouts, Leaderboard.Saves } : new[] { Leaderboard.HomeRuns, Leaderboard.RunsBattedIn, Leaderboard.Average };
            return boards.Any(b => Leaderboard.For(c.League, b, c.Me.Id).Top.Any(r => r.Mine && r.Rank == 1));
        }

        // ---------- Scheduled: these come at their moment, without the daily roll ----------

        /// <summary>An event whose day has come (opening day, the derby after the all-star game, the last home game); null otherwise.</summary>
        /// <summary>Opening day, told before the first pitch (after any prologue scene).</summary>
        public static StoryEvent AtSeasonStart(Career c) =>
            Offseason.Next(c) ?? (c.InMilitary || c.InFarm || c.SeenThisSeason("opening") ? null : Opening(c));

        /// <summary>Scenes that can't wait a day for the main story: the trade rumor before the deadline.</summary>
        public static StoryEvent Urgent(Career c, DayResult day) => c.InMilitary ? null : RumorMoment(c);

        public static StoryEvent Scheduled(Career c, DayResult day)
        {
            if (c.InMilitary) return null;
            if (c.Day <= 1 && !c.InFarm && !c.SeenThisSeason("opening")) return Opening(c); // in case the season start was skipped
            // Right after my club's last home game of the year (it comes first: it can't wait for another day).
            if (!c.InFarm && !c.SeenThisSeason("last_home") && c.Day - 1 >= LastHomeDay(c) && c.Day - 1 <= LastHomeDay(c) + 2) return LastHome;
            // Hitters get the real home run derby at the break; pitchers have their own fun.
            if (c.Day > c.AllStarDay && c.Day <= c.AllStarDay + 3 && MadeAllStar(c) && !c.Me.IsInjured && c.Me.IsPitcher && !c.SeenThisSeason(RadarDerby.Id))
                return RadarDerby;
            var moved = ClubChangeMoment(c, day);
            if (moved != null) return moved;
            var bond = BondMoment(c);
            if (bond != null) return bond;
            var serial = SerialMoment(c, day);
            if (serial != null) return serial;
            return null;
        }

        /// <summary>The day of my club's last home game this season.</summary>
        public static int LastHomeDay(Career c)
        {
            for (int d = c.League.GamesPerSeason - 1; d >= 0; d--)
                if (c.League.Matchups(d).Any(m => m.Home == c.MyTeam)) return d;
            return -1;
        }

        static StoryEvent Opening(Career c) => new StoryEvent
        {
            Id = "opening",
            Title = c.Season == 1 ? "프로 첫 개막전" : "개막전",
            Text = c.Season == 1
                ? "스프링캠프의 땀이 마르기도 전에 개막전이다.\n가득 찬 관중석. {name}의 프로 첫 시즌이 시작된다."
                : $"{Career.YearOf(c.Season)}년 개막전. 겨우내 준비한 것을 보여 줄 시간이다.\n전광판에 {{name}}의 이름이 떴다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "초구부터 자신 있게", Result = "심장이 쿵쾅거렸지만 몸은 가벼웠다.", Apply = e => { e.Morale(5); e.Fans(20); } },
                new EventChoice { Label = "캠프에서 배운 걸 되새긴다", Result = "캠프 노트를 한 번 더 넘겨 봤다.", Apply = e => { e.Exp(Stat.Eye, 30); e.Morale(2); } },
            },
        };

        static readonly StoryEvent LastHome = new StoryEvent
        {
            Id = "last_home",
            Title = "시즌 마지막 홈경기",
            Text = "올 시즌 마지막 홈경기가 끝났다.\n선수단이 그라운드에 한 줄로 섰다. 팬들이 자리를 떠나지 않는다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "모자를 벗고 인사한다", Result = "\"내년에 또 봐요!\" 함성이 오래 이어졌다.", Apply = e => { e.Fans(40); e.Morale(4); } },
                new EventChoice { Label = "사인볼을 던져 준다 (50만원)", Result = "외야석까지 공이 날아가자 환호성이 터졌다.", Apply = e => { e.Money(-50); e.Fans(60); e.Morale(3); } },
            },
        };

        // ---------- The all-star home run derby ----------


        static readonly StoryEvent RadarDerby = new StoryEvent
        {
            Id = "radar_derby",
            Title = "강속구 대결",
            Text = "올스타 휴식기 이벤트, 투수들의 스피드건 대결에 초대받았다!\n전광판에 숫자가 뜰 때마다 함성이 터진다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "어깨가 빠져라 전력 투구", Result = "", Apply = e => Radar(e, all: true) },
                new EventChoice { Label = "부드럽게 힘을 실어서", Result = "", Apply = e => Radar(e, all: false) },
            },
        };

        static void Radar(EventEffects e, bool all)
        {
            var me = e.Career.Me;
            int speed = 135 + (int)Math.Round(e.Rating(Stat.Velocity) * 0.22 + e.Roll() * (all ? 6 : 3)) + (all ? 2 : 0);
            int best = 149 + (int)(e.Roll() * 9); // the hardest other thrower: 149 to 157
            e.Say($"전광판에 {speed}km! (다른 투수 최고 {best}km)");
            if (speed > best)
            {
                e.Say("강속구 대결 우승! 관중석이 들썩였다.");
                e.Money(500);
                e.Fans(150);
                e.Morale(6);
                e.Career.SeasonNotes.Add($"{e.Career.MonthText} 강속구 대결 우승");
            }
            else
            {
                e.Fans(40);
                e.Morale(2);
            }
            if (speed > me.DerbyBest)
            {
                if (me.DerbyBest > 0) e.Say($"개인 최고 구속 경신! ({me.DerbyBest}km → {speed}km)");
                me.DerbyBest = speed;
            }
            if (all && e.MaybeInjure(0.08, 3, InjuryKind.Shoulder)) e.Say("던지고 나니 어깨가 찌릿하다...");
        }
    }
}
