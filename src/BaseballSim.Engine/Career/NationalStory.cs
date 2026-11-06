using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Scenes around a national-team call-up: the news, the squad, a teammate from another club, the
    /// knockout eves and the finish. They come between Korea's games, each once a tournament. The
    /// wording follows who the player is by then (the squad's youngest, a regular, a veteran who may
    /// wear the armband) and which tournament it is, and most scenes have a few versions so a second
    /// or third call-up doesn't read the same. Military service is a footnote, not the point.
    /// </summary>
    public static class NationalStory
    {
        const string Call = "nt:call", Camp = "nt:camp", Room = "nt:room", Scout = "nt:scout", Eve = "nt:eve", Semi = "nt:semi", End = "nt:end";

        public static StoryEvent Next(Career c, NationalTeam.Run run)
        {
            var t = run.Result;
            if (!t.Selected) return null;
            if (run.GamesPlayed == 0 && !Done(c, Call)) return CallUp(c, t);
            if (run.GamesPlayed == 0 && !Done(c, Camp)) return Squad(c, t);
            if (run.GamesPlayed == 1 && !run.Done && !Done(c, Room)) return Roommate(c, t);
            if (t.Wbc && run.NextStage == "8강" && !Done(c, Scout)) return Scouts(c, run);
            if (run.NextStage == "준결승" && !Done(c, Semi)) return SemiEve(c, t);
            if (run.NextStage == "결승" && !Done(c, Eve)) return FinalEve(c, run);
            if (run.Done && !Done(c, End)) return Finish(c, t);
            return null;
        }

        static bool Done(Career c, string key) => c.Me.StoryMarks.TryGetValue(key, out int stamp) && stamp / 1000 == c.Season;

        static StoryEvent Make(string key, StoryEvent ev)
        {
            foreach (var choice in ev.Choices)
            {
                var apply = choice.Apply;
                choice.Apply = e => { e.Mark(key); apply?.Invoke(e); };
            }
            ev.Weight = (_, __) => 0;
            return ev;
        }

        static Stat Main(Career c) => c.Me.MyStats.OrderByDescending(s => c.Me.Ratings[s]).First();

        /// <summary>Earlier tournaments the player was picked for.</summary>
        static int Caps(Career c) => c.Me.History.Count(h => h.Notes.Any(n => n.Contains(NationalTeam.Name) || n.Contains(NationalTeam.WbcName)));

        enum Place { Youngest, Regular, Veteran }

        /// <summary>Where the player stands in the squad: the kid on a first call-up, a regular, or a veteran.</summary>
        static Place PlaceOf(Career c)
        {
            int caps = Caps(c);
            if (c.Me.Age >= 30 || caps >= 2) return Place.Veteran;
            if (c.Me.Age <= 23 && caps == 0) return Place.Youngest;
            return Place.Regular;
        }

        /// <summary>Which version of a scene this tournament gets: changes from one tournament to the next.</summary>
        static int Pick(Career c, int versions) => (c.Season * 7 + Caps(c) * 3) % versions;

        /// <summary>A teammate from another club in the squad, for the scenes.</summary>
        static string Mate(Career c, TournamentResult t, int which)
        {
            var names = t.Korea.Lineup.Concat(t.Korea.Rotation)
                .Where(p => p.Id != c.Me.Id && p.Id != RivalState.PlayerId && !c.MyTeam.Lineup.Concat(c.MyTeam.Rotation).Any(m => m.Id == p.Id))
                .Select(p => p.Name).Distinct().ToList();
            return names.Count == 0 ? "대표팀 선배" : names[(which + c.Season) % names.Count];
        }

        static bool RivalIn(TournamentResult t) =>
            t.Korea.Lineup.Concat(t.Korea.Rotation).Concat(t.Korea.Bullpen).Any(p => p.Id == RivalState.PlayerId);

        static StoryEvent CallUp(Career c, TournamentResult t)
        {
            bool first = Caps(c) == 0 && c.Me.GoldMedals == 0;
            var place = PlaceOf(c);
            string text;
            if (t.Wbc)
                text = first
                    ? "WBC 최종 명단 발표. 메이저리거들 이름 사이에 {name}의 이름이 있었다.\n세계 최고의 선수들과 붙는 무대. 손이 조금 떨렸다."
                    : place == Place.Veteran
                        ? "WBC 명단에 또 {name}의 이름이 올랐다.\n이번엔 대표팀이 기대는 이름으로서다. 기자들의 질문도 달라졌다. \"이번엔 어디까지 갑니까?\""
                        : "WBC 명단 발표. 다시 한번 태극마크다.\n이번 상대는 일본, 미국, 도미니카. 텔레비전으로만 보던 선수들이다.";
            else
            {
                string[] firstVersions =
                {
                    $"대표팀 명단 발표 날. 휴대폰이 쉴 새 없이 울렸다.\n'{NationalTeam.Name} 최종 명단, {{name}} 발탁!'\n가슴에 태극마크를 다는 날이 왔다.",
                    $"훈련을 마치고 라커룸에 들어서자 동료들이 박수를 쳤다.\n\"봤어? 너 {NationalTeam.Name} 뽑혔어!\"\n어릴 때 텔레비전 앞에서 따라 부르던 애국가가 떠올랐다.",
                };
                string[] againVersions =
                {
                    $"{NationalTeam.Name} 명단에 또 {{name}}의 이름이 올랐다.\n이번엔 대표팀의 중심으로서다.",
                    $"몇 번째 태극마크인지 이제 헷갈린다. {NationalTeam.Name} 명단에 또 {{name}}.\n그래도 유니폼을 받는 순간은 매번 새롭다.",
                };
                text = first ? firstVersions[Pick(c, firstVersions.Length)] : againVersions[Pick(c, againVersions.Length)];
            }
            text += "\n대회 기간 동안 리그는 잠시 쉬어 간다.";

            return Make(Call, new StoryEvent
            {
                Id = "nt_call",
                Title = t.Wbc ? (first ? "세계 무대로" : "다시 WBC") : first ? "생애 첫 국가대표" : "다시 태극마크",
                Text = text,
                Choices =
                {
                    new EventChoice { Label = "\"나라를 대표한다는 건 영광입니다\"", Result = "인터뷰 영상이 하루 종일 뉴스에 나왔다.", Apply = e => { e.Fans(80); e.Morale(4); } },
                    new EventChoice { Label = "가족에게 제일 먼저 전화한다", Result = first ? "수화기 너머로 한참 동안 말이 없었다. 우는 소리였다." : "\"또 뽑혔어? 이제 놀랍지도 않네.\" 말과 달리 목소리가 들떠 있었다.", Apply = e => { e.Morale(5); e.Fans(20); } },
                    new EventChoice { Label = "들뜨지 않고 바로 개인 훈련", Result = "대표팀 합류 전까지 하루도 쉬지 않았다.", Apply = e => { e.Exp(Main(c), 60); e.Fatigue(10); } },
                },
            });
        }

        static StoryEvent Squad(Career c, TournamentResult t)
        {
            string mate = Mate(c, t, 0);
            var place = PlaceOf(c);
            if (RivalIn(t) && c.Rival != null && Pick(c, 2) == 0)
                return Make(Camp, new StoryEvent
                {
                    Id = "nt_camp",
                    Title = "대표팀 첫 소집",
                    Text = $"대표팀 라커룸. 리그 최고의 선수들이 한자리에 모였다.\n그리고 그 사이에 라이벌 {c.Rival.Name}이(가) 있었다.\n\"이번엔 같은 편이네.\"",
                    Choices =
                    {
                        new EventChoice { Label = "\"이번만 손잡자\"", Result = "어색한 악수. 하지만 둘 다 웃고 있었다.", Apply = e => { e.Morale(5); e.Fans(60); } },
                        new EventChoice { Label = "\"대표팀에서도 내가 더 잘할 거야\"", Result = "라이벌이 피식 웃었다. 묘하게 힘이 난다.", Apply = e => { e.Morale(3); e.Exp(Main(c), 50); } },
                    },
                });

            if (place == Place.Youngest)
                return Make(Camp, new StoryEvent
                {
                    Id = "nt_camp",
                    Title = "대표팀 막내",
                    Text = $"대표팀 라커룸. 리그에서 상대로만 만나던 선배들이 같은 유니폼을 입었다.\n최연소 선수는 {{name}}. 장비 가방 나르는 건 당연히 막내 몫이다.\n{mate} 선배가 가방 하나를 뺏어 들었다. \"반은 내가 든다.\"",
                    Choices =
                    {
                        new EventChoice { Label = "선배들 루틴을 옆에서 배운다", Result = $"{mate} 선배의 경기 전 준비는 30분이 넘었다. 그대로 따라 해 봤다.", Apply = e => { e.Exp(Main(c), 60); e.Boost(Main(c), 15, 10, "대표팀 선배의 루틴"); } },
                        new EventChoice { Label = "막내답게 분위기를 띄운다", Result = "어색하던 라커룸에 웃음이 터졌다. 선배들이 이름을 외워 줬다.", Apply = e => { e.Morale(5); e.Fans(40); } },
                    },
                });

            if (place == Place.Veteran)
                return Make(Camp, new StoryEvent
                {
                    Id = "nt_camp",
                    Title = "고참의 무게",
                    Text = (Pick(c, 2) == 0
                        ? "대표팀 라커룸. 처음 보는 어린 얼굴들이 쭈뼛거리며 인사하러 왔다.\n어느새 {name}가 이 방에서 손에 꼽는 고참이다."
                        : "소집 첫날 저녁, 감독이 {name}를 따로 불렀다.\n\"이번엔 네가 중심을 잡아 줘야겠다. 애들이 너를 보고 움직인다.\"")
                        + (c.Me.Age >= 30 ? "\n선수단 투표로 주장 완장이 {name}에게 돌아왔다." : ""),
                    Choices =
                    {
                        new EventChoice { Label = "후배들을 모아 밥을 산다", Result = "고기 굽는 연기 사이로 처음 만난 후배들이 하나둘 말문을 열었다.", Apply = e => { e.Money(-100); e.Morale(4); e.Trust(1); e.Fans(40); } },
                        new EventChoice { Label = "말 대신 훈련으로 보여 준다", Result = "제일 먼저 나와 제일 늦게 들어갔다. 다음 날부터 후배들이 더 일찍 나왔다.", Apply = e => { e.Exp(Main(c), 50); e.Trust(1); e.Fatigue(10); } },
                    },
                });

            return Make(Camp, new StoryEvent
            {
                Id = "nt_camp",
                Title = "대표팀 첫 소집",
                Text = t.Wbc
                    ? $"대표팀 소집. 메이저리그에서 뛰는 교포 선수 둘이 합류했다.\n타격 훈련을 지켜보던 {mate}이(가) 중얼거렸다. \"공 소리부터 다르네.\""
                    : $"대표팀 라커룸. 리그에서 상대로만 만나던 선수들이 같은 유니폼을 입었다.\n리그 최정상급 선수 {mate}이(가) 먼저 손을 내밀었다.",
                Choices =
                {
                    new EventChoice { Label = "비결을 물어본다", Result = $"{(t.Wbc ? "메이저리거 선배" : mate)}이(가) 자기 루틴을 아낌없이 알려 줬다.", Apply = e => { e.Exp(Main(c), 60); e.Boost(Main(c), 15, 10, "대표팀 동료의 비결"); } },
                    new EventChoice { Label = "같이 특타·특훈을 자청한다", Result = "둘이서 해가 질 때까지 공을 쳤다. 손바닥이 얼얼했다.", Apply = e => { e.Exp(Main(c), 80); e.Fatigue(15); e.Morale(2); } },
                },
            });
        }

        static StoryEvent Roommate(Career c, TournamentResult t)
        {
            string mate = Mate(c, t, 1);
            bool won = t.Games.Count > 0 && t.Games[0].Won;
            string start = won ? "첫 경기를 이기고 숙소로 돌아왔다." : "첫 경기를 내주고 숙소로 돌아왔다. 분위기가 무겁다.";
            string[] versions =
            {
                $"\n룸메이트는 다른 팀의 {mate}. 리그에선 서로 견제하던 사이다.\n불 끄기 전, {mate}이(가) 먼저 말을 걸었다.",
                $"\n룸메이트 {mate}은(는) 밤마다 상대 투수 영상을 본다. 오늘도 노트북 불빛이 새어 나온다.\n\"같이 볼래?\"",
                $"\n룸메이트 {mate}이(가) 편의점 봉지를 내밀었다. \"야식 먹고 자자. 내일 일은 내일 걱정하고.\"",
            };
            int v = Pick(c, versions.Length);
            var stat = c.Me.IsPitcher ? Stat.Control : Stat.Eye;
            return Make(Room, new StoryEvent
            {
                Id = "nt_room",
                Title = "대표팀 룸메이트",
                Text = start + versions[v],
                Choices =
                {
                    new EventChoice { Label = v == 1 ? "밤늦게까지 영상을 같이 본다" : "밤새 야구 이야기를 한다", Result = "상대 투수 공략법까지 나눴다. 리그로 돌아가면 다시 적이지만.", Apply = e => { e.Exp(stat, 60); e.Boost(stat, 15, 10, "룸메이트의 분석"); } },
                    new EventChoice { Label = "\"내일은 꼭 이기자\" 하고 일찍 잔다", Result = "주먹을 맞부딪쳤다. 그날 밤은 푹 잤다.", Apply = e => { e.Morale(4); e.Fatigue(-15); } },
                },
            });
        }

        /// <summary>The WBC's knockout round: big-league scouts in the stands.</summary>
        static StoryEvent Scouts(Career c, NationalTeam.Run run) => Make(Scout, new StoryEvent
        {
            Id = "nt_scout",
            Title = "관중석의 스카우트",
            Text = $"8강 상대는 {run.NextOpponent?.Name ?? "강호"}. 몸을 푸는데 포수가 귀띔했다.\n\"백네트 뒤 봤어? 메이저리그 스카우트들이 줄지어 앉아 있어. 스피드건까지 들고.\"",
            Choices =
            {
                new EventChoice { Label = "의식하지 않고 내 야구를 한다", Result = "스카우트는 잊었다. 눈앞의 공만 보기로 했다.", Apply = e => { e.Morale(4); e.Exp(Main(c), 40); } },
                new EventChoice { Label = "보여 줄 기회다", Result = "심장이 뛰었다. 오늘은 세계에 이름을 알리는 날이다.", Apply = e => { e.Morale(6); e.Fans(80); } },
            },
        });

        static StoryEvent SemiEve(Career c, TournamentResult t)
        {
            string[] versions =
            {
                (t.Wbc ? "8강을 넘었다. 이제 4강, 세계 네 팀 안이다." : "조별리그를 통과했다.") + " 내일 지면 끝, 이기면 결승이다.\n감독이 선수단을 모아 짧게 말했다. \"오늘 밤은 각자 할 일을 해라.\"",
                "준결승 전날 밤. 숙소 식당에 아무도 말이 없다.\n누군가 틀어 놓은 텔레비전에서 내일 경기 예상이 흘러나왔다. 대부분 상대 팀이 이긴다고 한다.",
            };
            var stat = c.Me.IsPitcher ? Stat.Control : Stat.Eye;
            return Make(Semi, new StoryEvent
            {
                Id = "nt_semi",
                Title = "준결승 전날",
                Text = versions[Pick(c, versions.Length)],
                Choices =
                {
                    new EventChoice { Label = "상대 영상을 한 번 더 본다", Result = "상대 에이스의 버릇 하나를 찾아냈다.", Apply = e => { e.Exp(stat, 50); e.Morale(2); } },
                    new EventChoice { Label = "일찍 잔다", Result = "몸이 가볍다. 준비는 끝났다.", Apply = e => { e.Fatigue(-15); e.Morale(3); } },
                },
            });
        }

        static StoryEvent FinalEve(Career c, NationalTeam.Run run)
        {
            string opponent = run.NextOpponent?.Name ?? "상대";
            var place = PlaceOf(c);
            var choices = new List<EventChoice>();
            if (place == Place.Veteran)
                choices.Add(new EventChoice { Label = "고참으로서 선수단을 모은다", Result = "\"내일은 우리 인생에서 제일 긴 하루가 될 거다. 즐기자.\" 모두가 고개를 끄덕였다.", Apply = e => { e.Morale(6); e.Trust(1); } });
            else if (place == Place.Youngest)
                choices.Add(new EventChoice { Label = "선배들 방을 돌며 인사한다", Result = "\"막내가 긴장했구나?\" 선배들이 등을 두드려 줬다. 오히려 마음이 놓였다.", Apply = e => { e.Morale(6); e.Fans(20); } });
            else
                choices.Add(new EventChoice { Label = "룸메이트와 내일을 그려 본다", Result = "\"9회에 네가 치고, 내가 막고.\" 둘이서 웃다가 잠들었다.", Apply = e => { e.Morale(6); } });
            choices.Add(new EventChoice { Label = c.Me.IsPitcher ? "혼자 섀도 피칭 백 번" : "혼자 섀도 스윙 백 번", Result = "땀이 식을 무렵 마음도 가라앉았다.", Apply = e => { e.Exp(Main(c), 50); e.Morale(3); } });
            choices.Add(new EventChoice { Label = "가족에게 영상 통화", Result = "\"그냥 하던 대로 해.\" 그 한마디면 충분했다.", Apply = e => { e.Morale(5); e.Fatigue(-5); } });

            string[] versions =
            {
                $"내일은 {opponent}와(과)의 결승.\n숙소 창밖으로 경기장 조명이 보인다. 잠이 오지 않는다.",
                $"결승 상대는 {opponent}. 저녁 미팅이 끝나고 다들 말없이 각자 방으로 흩어졌다.\n복도 끝 창문 너머로 내일 경기장이 보인다.",
            };
            return Make(Eve, new StoryEvent
            {
                Id = "nt_final_eve",
                Title = "결승 전야",
                Text = versions[Pick(c, versions.Length)] + (run.Result.Wbc ? "\n이기면 세계 정상이다." : ""),
                Choices = choices,
            });
        }

        static StoryEvent Finish(Career c, TournamentResult t)
        {
            bool needsService = !c.MilitaryDone && !c.InMilitary;
            var place = PlaceOf(c);
            string juniors = place == Place.Youngest ? "선배들" : "후배들";
            if (t.Wbc)
            {
                switch (t.Medal)
                {
                    case NationalTeam.WbcChampion:
                        return Make(End, new StoryEvent
                        {
                            Id = "nt_wbc_champ",
                            Title = "세계 정상!",
                            Text = "마지막 공이 미트에 꽂혔다. 대한민국, WBC 우승.\n메이저리거들을 꺾고 세계 맨 꼭대기에 섰다. 전 세계 중계 화면에 태극기가 펄럭인다.",
                            Choices =
                            {
                                new EventChoice { Label = "태극기를 마운드에 꽂는다", Result = "그 사진이 다음 날 모든 신문 1면을 장식했다.", Apply = e => { e.Fans(250); e.Morale(8); } },
                                new EventChoice { Label = "동료들과 끌어안고 운다", Result = "리그에선 적이었던 얼굴들이 이 순간만큼은 가족이었다.", Apply = e => { e.Morale(8); e.Trust(1); e.Exp(Main(c), 80); } },
                            },
                        });
                    case "준우승":
                        return Make(End, new StoryEvent
                        {
                            Id = "nt_wbc_second",
                            Title = "세계 2위",
                            Text = "결승에서 졌다. 그래도 세계 2위다.\n상대 팀 메이저리거가 다가와 모자를 건넸다. \"Great game.\"",
                            Choices =
                            {
                                new EventChoice { Label = "모자를 받고 내 모자를 건넨다", Result = "그 모자는 지금도 라커에 걸려 있다.", Apply = e => { e.Fans(120); e.Morale(4); } },
                                new EventChoice { Label = "다음 WBC를 다짐한다", Result = "준우승 메달을 꼭 쥐었다. 4년 뒤엔 한 계단 더.", Apply = e => { e.Exp(Main(c), 70); e.Morale(3); } },
                            },
                        });
                    case "4강":
                    case "8강":
                        return Make(End, new StoryEvent
                        {
                            Id = "nt_wbc_out",
                            Title = t.Medal == "4강" ? "세계 4강" : "8강에서 멈추다",
                            Text = (t.Medal == "4강" ? "준결승에서 졌다. 세계 네 팀 안에 든 것만으로도 기사가 쏟아졌다." : "8강에서 강호에게 막혔다. 1라운드는 통과했지만 아쉬움이 크다.")
                                + "\n메이저리그 투수들의 공은 확실히 달랐다. 그 공을 쳐 본 경험은 남는다.",
                            Choices =
                            {
                                new EventChoice { Label = "그 공을 잊지 않고 훈련한다", Result = "리그로 돌아오니 공이 느려 보였다.", Apply = e => { e.Exp(Main(c), 80); e.Boost(Main(c), 20, 15, "세계 무대의 경험"); } },
                                new EventChoice { Label = $"{juniors}과(와) 서로 다독인다", Result = "\"우리, 생각보다 잘했다.\" 공항 가는 버스가 조금 덜 조용했다.", Apply = e => { e.Morale(4); e.Trust(1); } },
                            },
                        });
                    default:
                        return Make(End, new StoryEvent
                        {
                            Id = "nt_wbc_r1",
                            Title = "1라운드 탈락",
                            Text = "1라운드에서 짐을 쌌다. 세계의 벽은 높았다.\n기사 제목들이 차갑다.",
                            Choices =
                            {
                                new EventChoice { Label = "고개 숙여 사과한다", Result = "\"응원에 보답하지 못해 죄송합니다.\"", Apply = e => { e.Morale(-1); e.Fans(30); } },
                                new EventChoice { Label = "말없이 소속팀으로 돌아간다", Result = "분한 마음은 리그에서 풀기로 했다.", Apply = e => { e.Exp(Main(c), 60); e.Boost(Main(c), 15, 10, "설욕의 다짐"); } },
                            },
                        });
                }
            }

            switch (t.Medal)
            {
                case NationalTeam.Gold:
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_gold",
                        Title = "금메달!",
                        Text = "마지막 아웃카운트가 올라갔다. 더그아웃이 텅 비었다. 모두 마운드로 달려 나갔다.\n시상대 가장 높은 곳, 애국가가 울린다."
                            + (needsService ? "\n(이 금메달로 병역 특례도 받게 됐다.)" : ""),
                        Choices =
                        {
                            new EventChoice { Label = "태극기를 들고 그라운드를 돈다", Result = "관중석의 함성이 끝없이 이어졌다.", Apply = e => { e.Fans(150); e.Morale(5); } },
                            new EventChoice { Label = "동료들과 끌어안고 운다", Result = "리그에선 적이었던 얼굴들이 이 순간만큼은 가족이었다.", Apply = e => { e.Morale(7); e.Trust(1); e.Exp(Main(c), 50); } },
                        },
                    });
                case "은메달":
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_silver",
                        Title = "아쉬운 은메달",
                        Text = "결승에서 졌다. 목에 걸린 은메달이 유난히 무겁다.\n그래도 여기까지 온 것도 대단하다고, 모두가 서로를 다독였다.",
                        Choices =
                        {
                            new EventChoice { Label = "4년 뒤를 다짐한다", Result = "메달을 서랍 깊이 넣었다. 다음엔 금색으로.", Apply = e => { e.Morale(2); e.Exp(Main(c), 60); } },
                            new EventChoice { Label = $"{juniors}을(를) 먼저 위로한다", Result = place == Place.Youngest ? "\"막내가 우리를 위로하네.\" 선배들이 웃으며 머리를 헝클었다." : "\"고개 들어. 잘했어.\" 후배가 눈물을 닦았다.", Apply = e => { e.Trust(1); e.Fans(50); } },
                        },
                    });
                case "동메달":
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_bronze",
                        Title = "동메달",
                        Text = "준결승에서 막혔다. 시상대 맨 끝자리.\n그래도 빈손으로 돌아가진 않는다.",
                        Choices =
                        {
                            new EventChoice { Label = "\"다음엔 더 높은 곳에\"", Result = "동메달을 손에 꼭 쥐었다.", Apply = e => { e.Morale(2); e.Exp(Main(c), 40); } },
                            new EventChoice { Label = "리그 복귀 준비를 서두른다", Result = "소속팀 경기가 기다리고 있다.", Apply = e => { e.Exp(Main(c), 50); e.Fatigue(-10); } },
                        },
                    });
                default:
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_out",
                        Title = "조별리그 탈락",
                        Text = "조별리그에서 짐을 쌌다. 공항 입국장이 조용하다.\n기사 제목들이 차갑다.",
                        Choices =
                        {
                            new EventChoice { Label = "고개 숙여 사과한다", Result = "\"응원에 보답하지 못해 죄송합니다.\"", Apply = e => { e.Morale(-2); e.Fans(30); } },
                            new EventChoice { Label = "말없이 소속팀으로 돌아간다", Result = "분한 마음은 리그에서 풀기로 했다.", Apply = e => { e.Exp(Main(c), 60); e.Morale(-1); } },
                        },
                    });
            }
        }
    }
}
