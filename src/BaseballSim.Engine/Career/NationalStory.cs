using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Scenes around a national-team call-up: the news, the squad, a teammate from another club, the
    /// final's eve and the medal. They come between Korea's games, each once a tournament.
    /// </summary>
    public static class NationalStory
    {
        const string Call = "nt:call", Camp = "nt:camp", Room = "nt:room", Eve = "nt:eve", Semi = "nt:semi", End = "nt:end";

        public static StoryEvent Next(Career c, NationalTeam.Run run)
        {
            var t = run.Result;
            if (!t.Selected) return null;
            if (run.GamesPlayed == 0 && !Done(c, Call)) return CallUp(c);
            if (run.GamesPlayed == 0 && !Done(c, Camp)) return Squad(c, t);
            if (run.GamesPlayed == 1 && !run.Done && !Done(c, Room)) return Roommate(c, t);
            if (run.NextStage == "준결승" && !Done(c, Semi)) return SemiEve(c);
            if (run.NextStage == "결승" && !Done(c, Eve)) return FinalEve(c, run);
            if (run.Done && !Done(c, End)) return Medal(c, t);
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

        /// <summary>A teammate from another club in the squad, for the scenes.</summary>
        static string Mate(Career c, TournamentResult t, int which)
        {
            var names = t.Korea.Lineup.Concat(t.Korea.Rotation)
                .Where(p => p.Id != c.Me.Id && p.Id != RivalState.PlayerId && !c.MyTeam.Lineup.Concat(c.MyTeam.Rotation).Any(m => m.Id == p.Id))
                .Select(p => p.Name).Distinct().ToList();
            return names.Count == 0 ? "대표팀 선배" : names[which % names.Count];
        }

        static bool RivalIn(TournamentResult t) =>
            t.Korea.Lineup.Concat(t.Korea.Rotation).Concat(t.Korea.Bullpen).Any(p => p.Id == RivalState.PlayerId);

        static StoryEvent CallUp(Career c)
        {
            bool first = c.Me.GoldMedals == 0 && !c.Me.History.Any(h => h.Notes.Any(n => n.Contains(NationalTeam.Name)));
            bool needsService = !c.MilitaryDone && !c.InMilitary;
            return Make(Call, new StoryEvent
            {
                Id = "nt_call",
                Title = first ? "생애 첫 국가대표" : "다시 태극마크",
                Text = (first
                    ? $"대표팀 명단 발표 날. 휴대폰이 쉴 새 없이 울렸다.\n'{NationalTeam.Name} 최종 명단, {{name}} 발탁!'\n가슴에 태극마크를 다는 날이 왔다."
                    : $"{NationalTeam.Name} 명단에 또 {{name}}의 이름이 올랐다.\n이번엔 대표팀의 중심으로서다.")
                    + "\n대회 기간 동안 리그는 잠시 쉬어 간다."
                    + (needsService ? "\n금메달을 따면 병역 특례도 걸려 있다." : ""),
                Choices =
                {
                    new EventChoice { Label = "\"나라를 대표한다는 건 영광입니다\"", Result = "인터뷰 영상이 하루 종일 뉴스에 나왔다.", Apply = e => { e.Fans(80); e.Morale(4); } },
                    new EventChoice { Label = "부모님께 제일 먼저 전화한다", Result = "어머니가 수화기 너머로 한참을 우셨다.", Apply = e => { e.Morale(6); } },
                    new EventChoice { Label = "들뜨지 않고 바로 개인 훈련", Result = "대표팀 합류 전까지 하루도 쉬지 않았다.", Apply = e => { e.Exp(Main(c), 50); e.Fatigue(10); } },
                },
            });
        }

        static StoryEvent Squad(Career c, TournamentResult t)
        {
            string mate = Mate(c, t, 0);
            bool rival = RivalIn(t) && c.Rival != null;
            return Make(Camp, new StoryEvent
            {
                Id = "nt_camp",
                Title = "대표팀 첫 소집",
                Text = rival
                    ? $"대표팀 라커룸. 리그 최고의 선수들이 한자리에 모였다.\n그리고 그 사이에 라이벌 {c.Rival.Name}이(가) 있었다.\n\"이번엔 같은 편이네.\""
                    : $"대표팀 라커룸. 리그에서 상대로만 만나던 선수들이 같은 유니폼을 입었다.\n리그 최정상급 선수 {mate}이(가) 먼저 손을 내밀었다.",
                Choices = rival
                    ? new List<EventChoice>
                    {
                        new EventChoice { Label = "\"이번만 손잡자\"", Result = "어색한 악수. 하지만 둘 다 웃고 있었다.", Apply = e => { e.Morale(5); e.Fans(60); } },
                        new EventChoice { Label = "\"대표팀에서도 내가 더 잘할 거야\"", Result = "라이벌이 피식 웃었다. 묘하게 힘이 난다.", Apply = e => { e.Morale(4); e.Exp(Main(c), 40); } },
                    }
                    : new List<EventChoice>
                    {
                        new EventChoice { Label = "비결을 물어본다", Result = $"{mate}이(가) 자기 루틴을 아낌없이 알려 줬다.", Apply = e => { e.Exp(Main(c), 60); } },
                        new EventChoice { Label = "막내답게 분위기를 띄운다", Result = "어색하던 라커룸에 웃음이 터졌다.", Apply = e => { e.Morale(5); e.Trust(1); } },
                    },
            });
        }

        static StoryEvent Roommate(Career c, TournamentResult t)
        {
            string mate = Mate(c, t, 1);
            bool won = t.Games.Count > 0 && t.Games[0].Won;
            return Make(Room, new StoryEvent
            {
                Id = "nt_room",
                Title = "대표팀 룸메이트",
                Text = (won ? "첫 경기를 이기고 숙소로 돌아왔다." : "첫 경기를 내주고 숙소로 돌아왔다. 분위기가 무겁다.")
                    + $"\n룸메이트는 다른 팀의 {mate}. 리그에선 서로 견제하던 사이다.\n불 끄기 전, {mate}이(가) 먼저 말을 걸었다.",
                Choices =
                {
                    new EventChoice { Label = "밤새 야구 이야기를 한다", Result = "상대 투수 공략법까지 나눴다. 리그로 돌아가면 다시 적이지만.", Apply = e => { e.Exp(c.Me.IsPitcher ? Stat.Control : Stat.Eye, 50); e.Morale(3); } },
                    new EventChoice { Label = "\"내일은 꼭 이기자\"", Result = "주먹을 맞부딪쳤다. 그날 밤은 푹 잤다.", Apply = e => { e.Morale(5); e.Fatigue(-10); } },
                },
            });
        }

        static StoryEvent SemiEve(Career c) => Make(Semi, new StoryEvent
        {
            Id = "nt_semi",
            Title = "준결승 전날",
            Text = "조별리그를 통과했다. 내일 지면 끝, 이기면 결승이다.\n감독이 선수단을 모아 짧게 말했다. \"오늘 밤은 각자 할 일을 해라.\"",
            Choices =
            {
                new EventChoice { Label = "상대 영상을 한 번 더 본다", Result = "상대 에이스의 버릇 하나를 찾아냈다.", Apply = e => { e.Exp(c.Me.IsPitcher ? Stat.Control : Stat.Eye, 40); } },
                new EventChoice { Label = "일찍 잔다", Result = "몸이 가볍다. 준비는 끝났다.", Apply = e => { e.Fatigue(-15); e.Morale(2); } },
            },
        });

        static StoryEvent FinalEve(Career c, NationalTeam.Run run)
        {
            bool needsService = !c.MilitaryDone && !c.InMilitary;
            string opponent = run.NextOpponent?.Name ?? "상대";
            return Make(Eve, new StoryEvent
            {
                Id = "nt_final_eve",
                Title = "결승 전야",
                Text = $"내일은 {opponent}와(과)의 결승.\n숙소 창밖으로 경기장 조명이 보인다. 잠이 오지 않는다."
                    + (needsService ? "\n이기면 금메달, 그리고 병역 특례. 모두가 그 이야기를 꺼내지 않으려 애썼다." : ""),
                Choices =
                {
                    new EventChoice { Label = "주장으로서 선수단을 모은다", Result = "\"내일은 우리 인생에서 제일 긴 하루가 될 거다. 즐기자.\" 모두가 고개를 끄덕였다.", Apply = e => { e.Morale(6); e.Trust(1); } },
                    new EventChoice { Label = "혼자 섀도 스윙 백 번", Result = "땀이 식을 무렵 마음도 가라앉았다.", Apply = e => { e.Exp(Main(c), 40); e.Morale(3); } },
                    new EventChoice { Label = "가족에게 영상 통화", Result = "\"아들, 그냥 하던 대로 해.\" 그 한마디면 충분했다.", Apply = e => { e.Morale(5); } },
                },
            });
        }

        static StoryEvent Medal(Career c, TournamentResult t)
        {
            bool needsService = !c.MilitaryDone && !c.InMilitary;
            switch (t.Medal)
            {
                case NationalTeam.Gold:
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_gold",
                        Title = "금메달!",
                        Text = "마지막 아웃카운트가 올라갔다. 더그아웃이 텅 비었다. 모두 마운드로 달려 나갔다.\n시상대 가장 높은 곳, 애국가가 울린다."
                            + (needsService ? "\n병역 특례도 확정됐다. 이제 야구에만 집중할 수 있다." : ""),
                        Choices =
                        {
                            new EventChoice { Label = "태극기를 들고 그라운드를 돈다", Result = "관중석의 함성이 끝없이 이어졌다.", Apply = e => { e.Fans(150); e.Morale(5); } },
                            new EventChoice { Label = "동료들과 끌어안고 운다", Result = "리그에선 적이었던 얼굴들이 이 순간만큼은 가족이었다.", Apply = e => { e.Morale(7); e.Trust(1); } },
                        },
                    });
                case "은메달":
                    return Make(End, new StoryEvent
                    {
                        Id = "nt_silver",
                        Title = "아쉬운 은메달",
                        Text = "결승에서 졌다. 목에 걸린 은메달이 유난히 무겁다.\n" + (needsService ? "병역 특례는 다음 기회로 미뤄졌다." : "그래도 여기까지 온 것도 대단하다고, 모두가 서로를 다독였다."),
                        Choices =
                        {
                            new EventChoice { Label = "4년 뒤를 다짐한다", Result = "메달을 서랍 깊이 넣었다. 다음엔 금색으로.", Apply = e => { e.Morale(2); e.Exp(Main(c), 50); } },
                            new EventChoice { Label = "후배들을 먼저 위로한다", Result = "\"고개 들어. 잘했어.\" 후배가 눈물을 닦았다.", Apply = e => { e.Trust(1); e.Fans(50); } },
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
                            new EventChoice { Label = "\"다음엔 더 높은 곳에\"", Result = "동메달을 손에 꼭 쥐었다.", Apply = e => { e.Morale(2); } },
                            new EventChoice { Label = "리그 복귀 준비를 서두른다", Result = "소속팀 경기가 기다리고 있다.", Apply = e => { e.Exp(Main(c), 40); } },
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
