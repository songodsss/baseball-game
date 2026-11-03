using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>How the fans took last season.</summary>
    public enum FanMood { Bad, Mixed, Good, Great, Hurt }

    /// <summary>
    /// The winter between seasons, so no two feel the same: what the fans say about last season, a
    /// winter plan picked from a few offers that change every year, and one winter happening. They
    /// come before opening day, one after another, after the main story's season-start scenes.
    /// </summary>
    public static class Offseason
    {
        // ---------- How last season went ----------

        public static FanMood Mood(SeasonRecord r)
        {
            int games = Math.Max(1, r.Games);
            double form = r.Pitching != null ? Career.PitchingForm(r.Pitching) : Career.Ops(r.Line);
            bool champion = r.Titles.Contains(Postseason.ChampionTitle);
            bool titled = r.Titles.Any(t => t != Postseason.ChampionTitle);
            if (r.InjuryDays > games / 4) return FanMood.Hurt;
            if (champion || titled || form >= 0.900) return FanMood.Great;
            if (r.FarmDays > games / 3 || form < 0.640) return FanMood.Bad;
            if (form >= 0.780) return FanMood.Good;
            return FanMood.Mixed;
        }

        static readonly Dictionary<FanMood, string[]> Comments = new Dictionary<FanMood, string[]>
        {
            [FanMood.Great] = new[]
            {
                "이 선수 보려고 시즌권 끊었습니다 ㅠㅠ", "{name} 없었으면 올해 어쩔 뻔했냐", "역대급 시즌이다 진짜",
                "유니폼 마킹 {name}으로 바꿨어요!", "이 정도면 국대 확정 아님?", "올해의 선수는 그냥 정해져 있었네",
                "직관 갈 때마다 해 주더라. 고맙다!", "우리 팀 얼굴 = {name}", "내년엔 더 잘할 거라는 게 무섭다 ㅋㅋ",
            },
            [FanMood.Good] = new[]
            {
                "올해 진짜 많이 늘었다", "꾸준한 게 제일 무섭지", "{name} 나오면 뭔가 해 줄 것 같음",
                "내년엔 타이틀 하나 가자!", "이제 주전 자리 굳혔네", "기대 이상이었다 ㅎㅎ",
                "중요할 때 한 방이 있음", "연봉 좀 올려 줘라 구단아",
            },
            [FanMood.Mixed] = new[]
            {
                "나쁘진 않았는데 뭔가 아쉽다", "기복만 줄이면 될 듯", "잘할 땐 잘하는데...",
                "내년이 진짜 승부처", "가능성은 보여 줬다", "좀 더 해 줄 수 있잖아",
                "평범했다. 그 이상은 아님", "찬스 때 좀 더 집중했으면",
            },
            [FanMood.Bad] = new[]
            {
                "이게 프로냐...", "{name} 2군 가서 다시 배워 와라", "연봉 아깝다는 말 나와도 할 말 없음",
                "찬스마다 맥이 끊김", "작년이랑 뭐가 달라진 건지 모르겠다", "그래도 아직 어리니까... 믿어 본다",
                "트레이드 카드로 쓰자는 말까지 나옴", "응원하는 마음으로 한 마디만. 정신 차려!",
                "경기장에서 한숨 나오게 하지 말자", "이번 겨울에 진짜 바뀌어서 와라",
            },
            [FanMood.Hurt] = new[]
            {
                "다친 게 너무 아까웠다", "건강하게만 돌아와 줘", "부상만 아니었으면...",
                "몸 관리부터 다시 하자", "아프지 말고 내년에 보자!", "재활 잘해서 꼭 돌아와라",
            },
        };

        /// <summary>A few fan comments about the season just finished, for the season-end card.</summary>
        public static List<string> FanComments(Career c, SeasonRecord r, int count = 3)
        {
            var pool = Comments[Mood(r)];
            int seed = Seed(c, 11);
            var picked = new List<string>();
            for (int k = 0; picked.Count < Math.Min(count, pool.Length); k++)
            {
                var line = pool[(seed + k * 7) % pool.Length];
                if (!picked.Contains(line)) picked.Add(line);
                if (k > 50) break;
            }
            return picked.Select(t => StoryEvent.Fill(t, c)).ToList();
        }

        // ---------- The season-start sequence ----------

        const string FansDone = "winter:fans", PlanDone = "winter:plan", HappeningDone = "winter:happening";

        /// <summary>The next winter scene before opening day, or null when they are all done (or it's the first season).</summary>
        public static StoryEvent Next(Career c)
        {
            if (c.Day != 0 || c.Me.History.Count == 0) return null;
            var last = c.Me.History[c.Me.History.Count - 1];
            if (!DoneThisSeason(c, FansDone) && !last.Military) return Fans(c, last);
            if (!DoneThisSeason(c, PlanDone)) return Plan(c);
            if (!DoneThisSeason(c, HappeningDone) && !c.InMilitary) return Happening(c);
            return null;
        }

        static bool DoneThisSeason(Career c, string key) => c.Me.StoryMarks.TryGetValue(key, out int stamp) && stamp / 1000 == c.Season;

        /// <summary>A number fixed for this player and winter, so the same winter always offers the same things.</summary>
        static int Seed(Career c, int salt)
        {
            unchecked
            {
                int h = 17;
                foreach (char ch in c.Me.Name ?? "") h = h * 31 + ch;
                h = h * 31 + c.Season * 101 + salt * 7919;
                return h & 0x7fffffff;
            }
        }

        /// <summary>Every choice also ticks off its step, so the sequence moves on.</summary>
        static StoryEvent Wrap(StoryEvent ev, string doneKey)
        {
            foreach (var choice in ev.Choices)
            {
                var apply = choice.Apply;
                choice.Apply = e => { e.Mark(doneKey); apply?.Invoke(e); };
            }
            ev.Weight = (_, __) => 0;
            return ev;
        }

        static T Pick<T>(IList<T> list, int seed) => list[seed % list.Count];

        static Stat MainStat(Career c, int seed) => Pick(c.Me.MyStats.ToList(), seed);

        // ---------- 1. The fans ----------

        static StoryEvent Fans(Career c, SeasonRecord r)
        {
            var mood = Mood(r);
            int seed = Seed(c, 1);
            string year = $"{Career.YearOf(r.Season)}";
            string line = r.Pitching != null
                ? (c.Me.Role == PitcherRole.Starter ? $"{r.Pitching.Wins}승 {r.Pitching.Losses}패 평균자책 {r.Pitching.EraText}" : $"{r.Pitching.Saves}세이브 {r.Pitching.Holds}홀드 평균자책 {r.Pitching.EraText}")
                : $"타율 {Stats.Rate(Stats.Avg(r.Line))} {r.Line.HomeRuns}홈런 OPS {Stats.Rate(Stats.Ops(r.Line))}";
            var stat = MainStat(c, seed);
            var options = new List<StoryEvent>();
            switch (mood)
            {
                case FanMood.Great:
                    options.Add(new StoryEvent
                    {
                        Id = "fans_great_parade", Title = "팬들의 환호",
                        Text = $"{year} 시즌 {line}.\n구단 SNS에 올라간 {{name}}의 하이라이트 영상이 하루 만에 100만 회를 넘었다.\n댓글창이 온통 하트다.",
                        Choices =
                        {
                            new EventChoice { Label = "팬들에게 감사 영상을 올린다", Result = "\"여러분 덕분입니다.\" 진심이 담긴 영상에 팬들이 또 한 번 울었다.", Apply = e => { e.Fans(80); e.Morale(5); } },
                            new EventChoice { Label = "들뜨지 않고 바로 훈련한다", Result = "축하는 하루면 충분하다. 다음 날 아침 6시, 훈련장 불이 켜졌다.", Apply = e => { e.Exp(stat, 60); e.Morale(2); } },
                            new EventChoice { Label = "팬미팅을 연다 (200만원)", Result = "3시간 동안 사인을 했다. 손목은 아팠지만 마음은 꽉 찼다.", Apply = e => { e.Money(-200); e.Fans(150); e.Morale(4); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_great_cover", Title = "잡지 표지 모델",
                        Text = $"{year} 시즌 {line}.\n스포츠 잡지가 {{name}}을(를) 신년호 표지로 골랐다.\n'올해의 얼굴' 인터뷰 요청도 함께 왔다.",
                        Choices =
                        {
                            new EventChoice { Label = "화보 촬영까지 한다 (+300만원)", Result = "어색한 포즈였지만 사진은 근사하게 나왔다.", Apply = e => { e.Money(300); e.Fans(100); } },
                            new EventChoice { Label = "인터뷰만 짧게", Result = "\"아직 보여 드릴 게 많습니다.\" 한 줄이 제목이 됐다.", Apply = e => { e.Fans(50); e.Morale(3); } },
                            new EventChoice { Label = "정중히 사양한다", Result = "그 시간에 영상 분석을 한 번 더 했다.", Apply = e => { e.Exp(stat, 50); e.Trust(1); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_great_kids", Title = "꼬마 팬의 편지",
                        Text = $"{year} 시즌 {line}.\n구단으로 삐뚤빼뚤한 글씨의 편지가 왔다.\n'저도 {{name}} 선수처럼 될 거예요. 내년에도 꼭 이겨 주세요!'",
                        Choices =
                        {
                            new EventChoice { Label = "답장과 사인 배트를 보낸다", Result = "며칠 뒤, 배트를 안고 웃는 아이 사진이 SNS에 올라왔다.", Apply = e => { e.Fans(70); e.Morale(5); } },
                            new EventChoice { Label = "편지를 사물함에 붙여 둔다", Result = "힘들 때마다 볼 수 있게. 그것만으로도 든든했다.", Apply = e => { e.Morale(6); } },
                        },
                    });
                    break;
                case FanMood.Good:
                    options.Add(new StoryEvent
                    {
                        Id = "fans_good_expect", Title = "커지는 기대",
                        Text = $"{year} 시즌 {line}.\n팬 커뮤니티 인기 글: '{{name}}, 내년엔 진짜 터진다'.\n기대가 커진 만큼 부담도 조금 생겼다.",
                        Choices =
                        {
                            new EventChoice { Label = "\"기대에 꼭 보답하겠습니다\"", Result = "말해 놓고 나니 오히려 마음이 단단해졌다.", Apply = e => { e.Morale(4); e.Fans(40); } },
                            new EventChoice { Label = "기대는 잊고 내 할 일만", Result = "댓글창을 닫고 훈련 일지를 펼쳤다.", Apply = e => { e.Exp(stat, 50); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_good_nickname", Title = "별명이 생겼다",
                        Text = $"{year} 시즌 {line}.\n팬들이 {{name}}에게 별명을 붙였다. 중계 캐스터까지 따라 부른다.\n마음에 드는 별명은?",
                        Choices =
                        {
                            new EventChoice { Label = "'승부사'", Result = "중요한 순간마다 그 별명이 들리기 시작했다.", Apply = e => { e.Morale(4); e.Fans(50); } },
                            new EventChoice { Label = "'성실맨'", Result = "코치들도 웃으며 그렇게 불렀다.", Apply = e => { e.Trust(2); e.Fans(30); } },
                            new EventChoice { Label = "'괴물'", Result = "조금 민망하지만, 들을수록 힘이 났다.", Apply = e => { e.Morale(3); e.Fans(70); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_good_jersey", Title = "유니폼 판매 순위",
                        Text = $"{year} 시즌 {line}.\n구단 스토어 유니폼 판매 순위에 {{name}}의 이름이 처음으로 상위권에 올랐다.",
                        Choices =
                        {
                            new EventChoice { Label = "스토어에 깜짝 방문한다", Result = "줄이 가게 밖까지 늘어섰다.", Apply = e => { e.Fans(80); e.Morale(3); } },
                            new EventChoice { Label = "\"1위까지 가 보겠습니다\"", Result = "기사 제목이 됐다. 이제 진짜 해야 한다.", Apply = e => { e.Morale(5); e.Fans(30); } },
                        },
                    });
                    break;
                case FanMood.Mixed:
                    options.Add(new StoryEvent
                    {
                        Id = "fans_mixed_debate", Title = "엇갈린 평가",
                        Text = $"{year} 시즌 {line}.\n'성장 중' vs '정체기'. 팬 커뮤니티에서 {{name}}을(를) 두고 토론이 붙었다.",
                        Choices =
                        {
                            new EventChoice { Label = "약점을 콕 집어 보완한다", Result = "영상으로 지난 시즌을 처음부터 다시 봤다.", Apply = e => { e.Exp(stat, 70); e.Morale(-1); } },
                            new EventChoice { Label = "신경 쓰지 않는다", Result = "평가는 내년 성적이 해 줄 것이다.", Apply = e => { e.Morale(2); } },
                            new EventChoice { Label = "직접 댓글로 답한다", Result = "\"내년엔 보여 드릴게요.\" 의외의 소통에 여론이 확 좋아졌다.", Apply = e => { e.Fans(60); e.Morale(1); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_mixed_column", Title = "칼럼 한 편",
                        Text = $"{year} 시즌 {line}.\n한 야구 기자가 칼럼을 썼다. '{{name}}, 재능은 충분하다. 문제는 기복이다.'",
                        Choices =
                        {
                            new EventChoice { Label = "기사를 출력해서 붙여 둔다", Result = "매일 아침 그 문장을 보며 이를 악물었다.", Apply = e => { e.Morale(3); e.Exp(stat, 40); } },
                            new EventChoice { Label = "기자에게 인터뷰를 청한다", Result = "솔직한 대화 끝에, 다음 칼럼은 응원 글이었다.", Apply = e => { e.Fans(50); e.Trust(1); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_mixed_fanclub", Title = "팬클럽 회장의 메시지",
                        Text = $"{year} 시즌 {line}.\n팬클럽 회장이 긴 메시지를 보냈다.\n'올해 아쉬웠던 거 알아요. 그래도 우리는 끝까지 {{name}} 편이에요.'",
                        Choices =
                        {
                            new EventChoice { Label = "고맙다고 전화한다", Result = "목소리가 떨렸다. 내년엔 꼭 웃게 해 주고 싶다.", Apply = e => { e.Morale(5); e.Fans(30); } },
                            new EventChoice { Label = "팬클럽에 단체 식사를 산다 (150만원)", Result = "밤늦게까지 야구 얘기로 웃었다.", Apply = e => { e.Money(-150); e.Fans(90); e.Morale(4); } },
                        },
                    });
                    break;
                case FanMood.Bad:
                    options.Add(new StoryEvent
                    {
                        Id = "fans_bad_comments", Title = "싸늘한 여론",
                        Text = $"{year} 시즌 {line}.\n'{{name}}, 이대로면 자리 없다.' 기사 댓글창이 싸늘하다.\n휴대폰을 쥔 손에 힘이 들어갔다.",
                        Choices =
                        {
                            new EventChoice { Label = "댓글을 끝까지 다 읽는다", Result = "아팠다. 하지만 하나도 잊지 않기로 했다.", Apply = e => { e.Morale(-4); e.Exp(stat, 90); } },
                            new EventChoice { Label = "휴대폰을 끄고 훈련장으로", Result = "말 대신 땀으로 답하기로 했다.", Apply = e => { e.Exp(stat, 50); e.Fatigue(10); } },
                            new EventChoice { Label = "공개 사과문을 올린다", Result = "\"실망시켜 드려 죄송합니다.\" 진심이 전해졌는지 여론이 조금 누그러졌다.", Apply = e => { e.Fans(30); e.Morale(-1); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_bad_banner", Title = "구단 앞 현수막",
                        Text = $"{year} 시즌 {line}.\n구단 사무실 앞에 현수막이 걸렸다. '응원도 지친다. 달라진 모습 보여 달라.'\n이름은 없지만 누굴 말하는지 안다.",
                        Choices =
                        {
                            new EventChoice { Label = "현수막 앞에서 고개 숙인다", Result = "지나가던 팬 몇 명이 말없이 어깨를 두드렸다.", Apply = e => { e.Fans(40); e.Morale(-2); e.Trust(1); } },
                            new EventChoice { Label = "사진을 찍어 배경화면으로", Result = "매일 보며 독기를 품었다.", Apply = e => { e.Morale(2); e.Exp(stat, 60); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_bad_radio", Title = "야구 라디오",
                        Text = $"{year} 시즌 {line}.\n택시 라디오에서 야구 프로그램이 나온다.\n\"{{name}}은(는) 솔직히 올해 기대 이하였죠. 내년이 마지막 기회일 수도 있어요.\"",
                        Choices =
                        {
                            new EventChoice { Label = "기사님께 \"제가 그 선수예요\"", Result = "기사님이 껄껄 웃었다. \"내년엔 잘해요! 나 팬 할게.\"", Apply = e => { e.Morale(4); e.Fans(20); } },
                            new EventChoice { Label = "조용히 이어폰을 꽂는다", Result = "들은 말을 곱씹으며 창밖을 봤다.", Apply = e => { e.Morale(-2); e.Exp(stat, 70); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_bad_oldfan", Title = "오래된 팬",
                        Text = $"{year} 시즌 {line}.\n악플 사이에 눈에 띄는 댓글 하나. '데뷔 때부터 봤어요. 이번 겨울만 잘 버텨요. 다들 금방 다시 좋아할 거예요.'",
                        Choices =
                        {
                            new EventChoice { Label = "그 댓글에 '좋아요'를 누른다", Result = "작은 하트 하나가 오래 마음에 남았다.", Apply = e => { e.Morale(5); } },
                            new EventChoice { Label = "그 팬을 개막전에 초대한다", Result = "\"정말요?\" 답장 끝에 울음 이모티콘이 잔뜩 붙어 있었다.", Apply = e => { e.Morale(4); e.Fans(40); } },
                        },
                    });
                    break;
                default: // hurt
                    options.Add(new StoryEvent
                    {
                        Id = "fans_hurt_wait", Title = "기다리는 사람들",
                        Text = $"{year} 시즌은 부상으로 {r.InjuryDays}일을 쉬었다.\n재활 센터 앞에 팬들이 보낸 꽃바구니가 쌓였다. '천천히, 건강하게.'",
                        Choices =
                        {
                            new EventChoice { Label = "재활 일지를 SNS에 공개한다", Result = "하루하루 나아지는 모습에 응원 댓글이 이어졌다.", Apply = e => { e.Fans(60); e.Morale(3); } },
                            new EventChoice { Label = "몸 만들기에만 집중한다", Result = "재활 트레이너가 놀랄 만큼 성실했다.", Apply = e => { e.Fatigue(-30); e.Exp(stat, 40); } },
                        },
                    });
                    options.Add(new StoryEvent
                    {
                        Id = "fans_hurt_doubt", Title = "'유리몸' 꼬리표",
                        Text = $"{year} 시즌은 부상으로 {r.InjuryDays}일을 쉬었다.\n'또 다쳤냐', '유리몸'... 아픈 몸보다 그 말들이 더 아팠다.",
                        Choices =
                        {
                            new EventChoice { Label = "체질 개선 프로그램 (400만원)", Result = "식단부터 수면까지 싹 바꿨다. 몸이 가벼워졌다.", Apply = e => { e.Money(-400); e.Fatigue(-40); e.Morale(3); } },
                            new EventChoice { Label = "실력으로 꼬리표를 뗀다", Result = "올해는 한 경기도 빠지지 않겠다고 다짐했다.", Apply = e => { e.Morale(4); } },
                        },
                    });
                    break;
            }
            return Wrap(Pick(options, seed / 3), FansDone);
        }

        // ---------- 2. The winter plan ----------

        class PlanOffer
        {
            public string Label;
            public string Result;
            public Func<Career, bool> Fits = _ => true;
            public Action<EventEffects, Career> Apply;
        }

        static readonly List<PlanOffer> Plans = new List<PlanOffer>
        {
            new PlanOffer { Label = "미국 개인 훈련 캠프 (800만원)", Result = "애리조나의 햇살 아래 메이저리그 트레이너와 한 달을 보냈다.",
                Fits = c => c.Me.Money >= 800, Apply = (e, c) => { e.Money(-800); var s = Best(c); e.Boost(s, 35, 20, "미국 개인 훈련"); e.Exp(s, 40); } },
            new PlanOffer { Label = "윈터리그 참가", Result = "따뜻한 남쪽 리그에서 겨울 내내 실전을 뛰었다. 몸은 지쳤지만 감은 살아 있다.",
                Apply = (e, c) => { foreach (var s in c.Me.MyStats) e.Exp(s, 35); e.Fatigue(25); } },
            new PlanOffer { Label = "푹 쉬며 가족과 보내기", Result = "고향 집 밥을 먹고 늦잠을 잤다. 마음이 말랑해졌다.",
                Apply = (e, c) => { e.Morale(8); e.Fatigue(-50); } },
            new PlanOffer { Label = "웨이트 트레이닝 집중", Result = "겨울 동안 몸무게가 5kg 늘었다. 전부 근육이다.",
                Fits = c => !c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Power, 30, 20, "겨울 웨이트"); e.Exp(Stat.Power, 50); } },
            new PlanOffer { Label = "선구안 특훈 (영상 분석실)", Result = "투수 영상 2000개를 봤다. 공이 느리게 보이기 시작했다.",
                Fits = c => !c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Eye, 30, 20, "영상 분석"); e.Exp(Stat.Eye, 50); } },
            new PlanOffer { Label = "육상 코치와 스프린트 훈련", Result = "출발 자세부터 다시 배웠다. 첫발이 확 빨라졌다.",
                Fits = c => !c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Speed, 30, 20, "스프린트 훈련"); e.Exp(Stat.Speed, 50); } },
            new PlanOffer { Label = "수비 펑고 1만 개", Result = "글러브 끈이 두 번 끊어졌다. 이제 어떤 타구도 무섭지 않다.",
                Fits = c => !c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Defense, 30, 20, "펑고 특훈"); e.Exp(Stat.Defense, 50); } },
            new PlanOffer { Label = "타격 폼 교정", Result = "익숙한 폼을 버리는 건 무서웠다. 하지만 타구 질이 달라졌다.",
                Fits = c => !c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Contact, 30, 20, "폼 교정"); e.Exp(Stat.Contact, 50); e.Morale(-2); } },
            new PlanOffer { Label = "구속 아카데미 (500만원)", Result = "하체 회전부터 다시 만들었다. 스피드건 숫자가 올라간다.",
                Fits = c => c.Me.IsPitcher && c.Me.Money >= 500, Apply = (e, c) => { e.Money(-500); e.Boost(Stat.Velocity, 35, 20, "구속 아카데미"); e.Exp(Stat.Velocity, 50); } },
            new PlanOffer { Label = "새 변화구 장착", Result = "그립을 수백 번 바꿔 쥔 끝에, 손끝에 감이 왔다.",
                Fits = c => c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Breaking, 30, 20, "새 변화구"); e.Exp(Stat.Breaking, 50); } },
            new PlanOffer { Label = "제구 집중 불펜 투구", Result = "포수 미트만 보며 하루 200개씩 던졌다.",
                Fits = c => c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Control, 30, 20, "제구 특훈"); e.Exp(Stat.Control, 50); } },
            new PlanOffer { Label = "산악 러닝으로 체력 만들기", Result = "매일 산을 올랐다. 숨이 덜 차기 시작했다.",
                Fits = c => c.Me.IsPitcher, Apply = (e, c) => { e.Boost(Stat.Stamina, 30, 20, "산악 러닝"); e.Exp(Stat.Stamina, 50); } },
            new PlanOffer { Label = "레전드 선배의 개인 레슨 (600만원)", Result = "\"공 하나에 생각 하나.\" 선배의 한 마디가 머릿속을 맴돈다.",
                Fits = c => c.Me.Money >= 600, Apply = (e, c) => { e.Money(-600); var s = Weakest(c); e.Boost(s, 40, 15, "레전드 레슨"); e.Exp(s, 60); } },
            new PlanOffer { Label = "멘탈 코치 상담", Result = "긴장될 때 숨 쉬는 법을 배웠다. 찬스가 기다려진다.",
                Apply = (e, c) => { e.Morale(6); e.Exp(c.Me.IsPitcher ? Stat.Control : Stat.Eye, 40); } },
            new PlanOffer { Label = "예능·광고 촬영 (+500만원)", Result = "카메라 앞은 어색했지만 통장은 두둑해졌다. 훈련은 조금 밀렸다.",
                Apply = (e, c) => { e.Money(500); e.Fans(100); e.Morale(2); } },
            new PlanOffer { Label = "유소년 야구 캠프 코치", Result = "아이들에게 가르치다 보니, 내가 잊고 있던 기본이 보였다.",
                Apply = (e, c) => { e.Fans(60); e.Morale(4); foreach (var s in c.Me.MyStats) e.Exp(s, 15); } },
            new PlanOffer { Label = "동료들과 자율 합동 훈련", Result = "같이 땀 흘리니 팀 분위기가 남달라졌다.",
                Apply = (e, c) => { e.Trust(2); e.Exp(Best(c), 40); e.Morale(3); } },
            new PlanOffer { Label = "일본 독립리그 연수", Result = "낯선 리그, 낯선 공. 세밀한 야구를 몸으로 배웠다.",
                Apply = (e, c) => { var s = MainStat(c, c.Season); e.Boost(s, 25, 25, "해외 연수"); e.Exp(s, 40); e.Fatigue(15); } },
        };

        static Stat Best(Career c) => c.Me.MyStats.OrderByDescending(s => c.Me.Ratings[s]).First();
        static Stat Weakest(Career c) => c.Me.MyStats.OrderBy(s => c.Me.Ratings[s]).First();

        /// <summary>Three or four of the winter plans, a different handful every year.</summary>
        static StoryEvent Plan(Career c)
        {
            var fits = Plans.Where(p => p.Fits(c)).ToList();
            int seed = Seed(c, 2);
            var picked = new List<PlanOffer>();
            for (int k = 0; picked.Count < Math.Min(4, fits.Count) && k < 200; k++)
            {
                var p = fits[(seed / 7 + k * 5) % fits.Count];
                if (!picked.Contains(p)) picked.Add(p);
            }
            var ev = new StoryEvent
            {
                Id = "winter_plan",
                Title = $"{Career.YearOf(c.Season)} 비시즌 계획",
                Text = c.InMilitary
                    ? $"{Career.MilitaryClub}에서 맞는 겨울. 그래도 쉬지 않는다.\n이번 겨울은 어떻게 보낼까?"
                    : "긴 겨울이 시작됐다. 스프링캠프까지 남은 시간은 두 달.\n이번 겨울을 어떻게 보내느냐가 내년을 결정한다.",
            };
            foreach (var p in picked)
            {
                var plan = p;
                ev.Choices.Add(new EventChoice { Label = plan.Label, Result = plan.Result, Apply = e => plan.Apply(e, c) });
            }
            return Wrap(ev, PlanDone);
        }

        // ---------- 3. A winter happening ----------

        static List<StoryEvent> Happenings(Career c)
        {
            var stat = MainStat(c, c.Season);
            return new List<StoryEvent>
            {
                new StoryEvent
                {
                    Id = "winter_school", Title = "모교 방문",
                    Text = "고등학교 야구부에서 연락이 왔다. 후배들 앞에서 한마디 해 줄 수 있냐고.\n낡은 그물망, 흙먼지 날리는 운동장. 그대로다.",
                    Choices =
                    {
                        new EventChoice { Label = "배팅볼을 던져 준다", Result = "후배들이 눈을 반짝이며 줄을 섰다.", Apply = e => { e.Fans(40); e.Morale(5); } },
                        new EventChoice { Label = "야구부에 장비를 기부한다 (300만원)", Result = "새 배트와 공 상자를 보고 감독님이 눈시울을 붉혔다.", Apply = e => { e.Money(-300); e.Fans(90); e.Morale(6); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_variety", Title = "예능 섭외",
                    Text = "인기 예능에서 출연 제의가 왔다. 운동선수 특집이란다.\n매니저가 기대에 찬 눈으로 쳐다본다.",
                    Choices =
                    {
                        new EventChoice { Label = "출연한다 (+200만원)", Result = "의외의 입담으로 실시간 검색어에 올랐다.", Apply = e => { e.Money(200); e.Fans(120); } },
                        new EventChoice { Label = "야구에 집중하겠다고 거절", Result = "\"야구로 화면에 나오겠습니다.\"", Apply = e => { e.Exp(stat, 40); e.Trust(1); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_wedding", Title = "동기의 결혼식",
                    Text = "입단 동기가 결혼한다. 축가를 불러 달라고 한다.\n노래는... 자신이 없다.",
                    Choices =
                    {
                        new EventChoice { Label = "용기 내서 부른다", Result = "음정은 엉망이었지만 하객들이 떼창으로 살려 줬다.", Apply = e => { e.Morale(6); e.Fans(30); } },
                        new EventChoice { Label = "축사로 대신한다", Result = "담백한 축사에 신부가 울었다.", Apply = e => { e.Morale(4); e.Trust(1); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_charity", Title = "자선 야구 대회",
                    Text = "연말 자선 야구 대회에 초대받았다. 연예인 팀과 한판이다.\n이기는 것보다 웃기는 게 중요하다는데...",
                    Choices =
                    {
                        new EventChoice { Label = "투수로 깜짝 등판한다", Result = "포지션 파괴에 관중석이 뒤집어졌다.", Apply = e => { e.Fans(100); e.Morale(4); } },
                        new EventChoice { Label = "진지하게 이기러 간다", Result = "혼자 진지해서 오히려 더 웃겼다.", Apply = e => { e.Fans(60); e.Exp(stat, 20); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_snow", Title = "폭설",
                    Text = "밤새 눈이 30cm 쌓였다. 실내 훈련장도 문을 닫았다.\n창밖이 하얗다.",
                    Choices =
                    {
                        new EventChoice { Label = "주차장에서라도 스윙한다", Result = "눈 위에 발자국이 원을 그렸다.", Apply = e => { e.Exp(stat, 40); e.Fatigue(10); } },
                        new EventChoice { Label = "하루쯤은 눈사람을 만든다", Result = "등번호를 단 눈사람 사진이 SNS에서 화제가 됐다.", Apply = e => { e.Morale(5); e.Fans(50); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_agent", Title = "에이전트의 제안",
                    Text = "에이전트가 서류 뭉치를 내밀었다. 스포츠 브랜드 후원 계약이다.\n대신 시즌 중에도 촬영이 몇 번 있다고.",
                    Choices =
                    {
                        new EventChoice { Label = "계약한다 (+600만원)", Result = "새 스파이크와 함께 두둑한 계약금이 들어왔다.", Apply = e => { e.Money(600); e.Morale(-1); e.Fans(40); } },
                        new EventChoice { Label = "작은 브랜드와 의리 계약 (+200만원)", Result = "무명 시절부터 장비를 대 주던 곳이다. 사장님이 크게 기뻐했다.", Apply = e => { e.Money(200); e.Morale(4); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_parents", Title = "부모님의 가게",
                    Text = "오랜만에 부모님 가게에 들렀다. 벽에 내 사진과 기사가 빼곡히 붙어 있다.\n\"우리 아들 왔다!\" 손님들이 박수를 친다.",
                    Choices =
                    {
                        new EventChoice { Label = "손님들께 사인을 해 드린다", Result = "그날 가게 매출이 최고 기록을 찍었다.", Apply = e => { e.Fans(30); e.Morale(5); } },
                        new EventChoice { Label = "가게 리모델링을 해 드린다 (500만원)", Result = "어머니가 말없이 손을 꼭 잡았다.", Apply = e => { e.Money(-500); e.Morale(8); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_rookie", Title = "신인과의 만남",
                    Text = "올해 입단한 신인이 찾아왔다. \"선배님 영상 보면서 야구했습니다!\"\n어디서 많이 본 눈빛이다.",
                    Choices =
                    {
                        new EventChoice { Label = "같이 새벽 훈련을 하자고 한다", Result = "후배를 끌어 주다 보니 나도 더 열심히 하게 됐다.", Apply = e => { e.Exp(stat, 40); e.Trust(1); } },
                        new EventChoice { Label = "밥을 사 주며 조언한다", Result = "\"부상만 조심해. 나머진 알아서 따라와.\"", Apply = e => { e.Morale(4); e.Trust(1); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_award_show", Title = "연말 시상식 초대",
                    Text = "스포츠 연말 시상식에 초대받았다. 정장을 입어야 한다.\n수상자는 아니지만 레드카펫은 걷는다.",
                    Choices =
                    {
                        new EventChoice { Label = "정장을 맞춘다 (100만원)", Result = "사진이 '베스트 드레서'로 뽑혔다.", Apply = e => { e.Money(-100); e.Fans(80); } },
                        new EventChoice { Label = "수상자들을 보며 다짐한다", Result = "내년엔 저 무대 위에 서겠다.", Apply = e => { e.Morale(6); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_injury_scare", Title = "빙판길",
                    Text = "새벽 러닝 중 빙판에 미끄러질 뻔했다. 간신히 버텼지만 식은땀이 났다.",
                    Choices =
                    {
                        new EventChoice { Label = "실내 러닝으로 바꾼다", Result = "조심해서 나쁠 건 없다.", Apply = e => { e.Fatigue(-10); e.Morale(1); } },
                        new EventChoice { Label = "아랑곳 않고 계속 뛴다", Result = "다행히 아무 일 없었다. 체력은 확실히 붙었다.", Apply = e => { if (!e.MaybeInjure(0.15, 5)) e.Exp(c.Me.IsPitcher ? Stat.Stamina : Stat.Speed, 60); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_book", Title = "야구 에세이 제안",
                    Text = "출판사에서 에세이를 써 보자는 연락이 왔다.\n제목 후보는 '{name}의 야구 노트'.",
                    Choices =
                    {
                        new EventChoice { Label = "쓴다 (+300만원)", Result = "글을 쓰며 내 야구를 돌아봤다. 생각이 정리됐다.", Apply = e => { e.Money(300); e.Fans(50); e.Exp(c.Me.IsPitcher ? Stat.Control : Stat.Eye, 30); } },
                        new EventChoice { Label = "은퇴하고 쓰겠다고 한다", Result = "\"그때 꼭 저희랑 해요.\" 편집자가 웃었다.", Apply = e => { e.Morale(2); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_volunteer", Title = "연탄 봉사",
                    Text = "구단이 연말 연탄 봉사를 한다. 자율 참가다.\n추운 골목, 끝없는 계단.",
                    Choices =
                    {
                        new EventChoice { Label = "참가한다", Result = "얼굴이 새까매졌지만 할머니의 귤 한 봉지가 따뜻했다.", Apply = e => { e.Fans(70); e.Morale(4); e.Fatigue(10); } },
                        new EventChoice { Label = "몰래 기부만 한다 (200만원)", Result = "이름 없이 보낸 돈이 나중에 기사로 알려졌다.", Apply = e => { e.Money(-200); e.Fans(90); e.Morale(3); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_game_night", Title = "선수단 게임 대회",
                    Text = "선수단 단톡방에 공지가 올라왔다. '겨울 야구 게임 대회. 꼴찌는 커피 쏘기.'\n자기 자신을 고를 수 있다는데...",
                    Choices =
                    {
                        new EventChoice { Label = "나를 골라 우승한다", Result = "게임 속 내 능력치가 너무 낮다는 사실에 모두 뒤집어졌다.", Apply = e => { e.Morale(5); e.Trust(1); } },
                        new EventChoice { Label = "일부러 져 주고 커피를 산다 (30만원)", Result = "커피 30잔. 분위기 메이커 등극.", Apply = e => { e.Money(-30); e.Trust(2); } },
                    },
                },
                new StoryEvent
                {
                    Id = "winter_scout_talk", Title = "해외 스카우트의 관심",
                    Text = "해외 구단 스카우트가 경기 영상을 요청했다는 소문이 돌았다.\n아직 먼 얘기지만 가슴이 뛴다.",
                    Choices =
                    {
                        new EventChoice { Label = "영어 공부를 시작한다", Result = "하루 30분. 언젠가를 위해.", Apply = e => { e.Morale(4); } },
                        new EventChoice { Label = "지금 팀에 집중한다", Result = "\"{team}에서 우승하는 게 먼저입니다.\" 팬들이 감동했다.", Apply = e => { e.Fans(80); e.Trust(1); } },
                    },
                },
            };
        }

        /// <summary>One winter happening, preferring ones this player hasn't had yet.</summary>
        static StoryEvent Happening(Career c)
        {
            var all = Happenings(c);
            var fresh = all.Where(h => !c.Me.StoryMarks.ContainsKey("seen:" + h.Id)).ToList();
            var pool = fresh.Count > 0 ? fresh : all;
            return Wrap(Pick(pool, Seed(c, 3) / 11), HappeningDone);
        }
    }
}
