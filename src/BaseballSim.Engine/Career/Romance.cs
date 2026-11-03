using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>One of the two women a story's player can fall for.</summary>
    public class Heroine
    {
        public string Key;
        public string Name;
        /// <summary>"신입 기자"</summary>
        public string Role;
        public StoryRoute Route;
    }

    /// <summary>
    /// The romance: every story has two heroines. Each gets three small scenes, taking turns, a few
    /// weeks apart; how warmly the player answers fills her heart. Then one evening the player says
    /// who it is (or that it's baseball for now), a date follows, then the proposal, and the ending
    /// gets a line of its own. The generic blind-date romance only comes when no heroine was chosen.
    /// Kept in story marks ("heart:key" holds the number, "lover:key" the choice), so no save change.
    /// </summary>
    public static class Romance
    {
        public const int StartHeart = 20, ConfessAt = 40;
        const string HeartPrefix = "heart:", LoverPrefix = "lover:";
        const string ChoiceId = "her_choice", ProposeId = "her_propose";

        class Def : Heroine
        {
            public StoryEvent[] Steps;
            public string Confess;
            public StoryEvent Date;
            public string ProposeTitle, ProposeText, ProposeYes;
            public string Ending;
        }

        public static int Heart(Career c, string key) => c.Me.StoryMarks.TryGetValue(HeartPrefix + key, out var v) ? v : StartHeart;

        static void AddHeart(EventEffects e, string key, int delta)
        {
            var c = e.Career;
            c.Me.StoryMarks[HeartPrefix + key] = Math.Max(0, Math.Min(100, Heart(c, key) + delta));
        }

        /// <summary>The story's two heroines.</summary>
        public static IReadOnlyList<Heroine> Of(StoryRoute route) => Defs.Where(d => d.Route == route).Cast<Heroine>().ToList();

        /// <summary>Heroines the player has met so far in this career, with their hearts.</summary>
        public static List<Heroine> Met(Career c) => Defs.Where(d => d.Route == c.Story.Route && c.Seen(d.Steps[0].Id)).Cast<Heroine>().ToList();

        /// <summary>The heroine the player chose, or null.</summary>
        public static Heroine Lover(Career c) => Defs.FirstOrDefault(d => c.HasMark(LoverPrefix + d.Key));

        public static bool Married(Career c) => Lover(c) != null && c.HasMark("married");

        /// <summary>"연인" until a heroine is chosen; her name after.</summary>
        public static string LoverName(Career c) => Lover(c)?.Name ?? "연인";

        /// <summary>The route's romance has begun (its first heroine came): the generic blind date stays away.</summary>
        public static bool Started(Career c) => Defs.Any(d => d.Route == c.Story.Route && c.Seen(d.Steps[0].Id));

        /// <summary>The next romance scene whose moment has come, or null. Takes the place of a day's random event.</summary>
        internal static StoryEvent Moment(Career c, DayResult d)
        {
            if (c.InMilitary || c.Day < 5 || c.Day >= c.League.GamesPerSeason * 95 / 100) return null;
            var hs = Defs.Where(x => x.Route == c.Story.Route).ToList();
            if (hs.Count < 2) return null;
            int season = c.League.GamesPerSeason;
            int gap = Math.Max(4, season / 3);
            bool Due(string mark, int games) => c.GamesSince(mark) >= games;

            string prev = null;
            foreach (var ev in new[] { hs[0].Steps[0], hs[1].Steps[0], hs[0].Steps[1], hs[1].Steps[1], hs[0].Steps[2], hs[1].Steps[2] })
            {
                if (!c.Seen(ev.Id))
                {
                    bool ready = prev == null ? c.Season >= 2 || c.Day >= season * 3 / 10 : Due("seen:" + prev, gap);
                    return ready ? ev : null;
                }
                prev = ev.Id;
            }

            var lover = (Def)Lover(c);
            if (lover == null)
            {
                if (c.HasMark("romance_none") || c.Me.Age > 34) return null;
                if (!c.Seen(ChoiceId)) return Due("seen:" + prev, gap) ? Choice(c, hs) : null;
                return c.HasMark("romance_wait") && !c.SeenThisSeason(ChoiceId) && c.Day >= season * 3 / 10 ? Choice(c, hs) : null;
            }
            if (!c.Seen(lover.Date.Id)) return Due(LoverPrefix + lover.Key, season / 2) ? lover.Date : null;
            if (!c.HasMark("married") && c.Me.Age >= 25 && Due("seen:" + lover.Date.Id, season / 2) && !c.SeenThisSeason(ProposeId))
                return Propose(lover);
            return null;
        }

        static StoryEvent Choice(Career c, List<Def> hs)
        {
            var open = hs.Where(h => Heart(c, h.Key) >= ConfessAt).OrderByDescending(h => Heart(c, h.Key)).ToList();
            if (open.Count == 0)
                return new StoryEvent
                {
                    Id = ChoiceId, Title = "엇갈린 계절",
                    Text = $"{hs[0].Name}도, {hs[1].Name}도 요즘은 연락이 뜸하다.\n바쁘다는 핑계로 답장을 미룬 건 나였다.\n\n빈 휴대폰 화면을 한참 보다가, 배트를 들었다.",
                    Weight = (_, __) => 0,
                    Choices =
                    {
                        new EventChoice { Label = "지금은 야구만 생각한다", Result = "스윙 소리만 밤늦게까지 이어졌다.", Apply = e => { e.Exp(Stat.Power, 40); e.Mark("romance_none"); } },
                    },
                };
            var ev = new StoryEvent
            {
                Id = ChoiceId, Title = "마음이 기우는 쪽",
                Text = c.HasMark("romance_wait")
                    ? "한 시즌이 또 지나간다.\n그동안 미뤄 둔 말이 있다. 이번에는 정말 말해야 할 것 같다."
                    : open.Count == 2
                        ? $"휴대폰을 열 때마다 떠오르는 얼굴이 있다.\n{open[0].Name}, 그리고 {open[1].Name}.\n\n오늘 밤에는 한 사람에게 전화를 걸기로 했다."
                        : $"요즘 경기가 끝나면 제일 먼저 {open[0].Name}의 얼굴이 떠오른다.\n오늘은 그 말을 해야 할 것 같다.",
                Weight = (_, __) => 0,
            };
            foreach (var h in open)
            {
                var her = h;
                ev.Choices.Add(new EventChoice
                {
                    Label = $"{her.Name}에게 마음을 전한다",
                    Result = her.Confess,
                    Apply = e => { e.Mark(LoverPrefix + her.Key); e.Mark("dating"); e.Unmark("romance_wait"); AddHeart(e, her.Key, 10); e.Morale(8); },
                });
            }
            ev.Choices.Add(new EventChoice
            {
                Label = "지금은 야구에 집중한다", Result = "휴대폰을 내려놓았다. 언젠가 말할 날이 오겠지.",
                Apply = e => { e.Mark("romance_wait"); e.Exp(Stat.Eye, 30); },
            });
            return ev;
        }

        static StoryEvent Propose(Def her) => new StoryEvent
        {
            Id = ProposeId, Title = her.ProposeTitle, Text = her.ProposeText,
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "반지를 꺼낸다 (시즌 중에 식을 올린다)", Result = her.ProposeYes + "\n\n동료들이 모두 와서 축가를 불렀다.",
                    Apply = e => { e.Mark("married"); e.Unmark("dating"); e.Money(-500); e.Morale(10); e.Fatigue(10); AddHeart(e, her.Key, 10); } },
                new EventChoice { Label = "반지를 꺼낸다 (식은 시즌 끝나고)", Result = her.ProposeYes + $"\n\n\"식은 시즌 끝나고. 그때까지 다치지 마.\" {her.Name}{Particle(her.Name, "이")} 웃었다.",
                    Apply = e => { e.Mark("married"); e.Unmark("dating"); e.Morale(8); AddHeart(e, her.Key, 10); } },
                new EventChoice { Label = "아직은 반지를 주머니에 둔다", Result = "주머니 속 상자가 하루 종일 무거웠다. 다음 시즌에는 꼭.",
                    Apply = e => e.Morale(-1) },
            },
        };

        /// <summary>The ending's line about her, when the player married her.</summary>
        public static List<string> EndingLines(Career c)
        {
            var her = (Def)Lover(c);
            if (her == null) return new List<string>();
            if (!c.HasMark("married")) return new List<string> { $"{her.Name}{Particle(her.Name, "은")} 은퇴식 날 관중석 맨 앞에 앉아 있었다. 둘 사이의 이야기는 이제부터다." };
            return new List<string> { $"[결혼 엔딩 · {her.Name}] {her.Ending}" };
        }

        static string Particle(string word, string p)
        {
            char last = word[word.Length - 1];
            bool batchim = last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0;
            switch (p)
            {
                case "이": case "가": return batchim ? "이" : "가";
                case "은": case "는": return batchim ? "은" : "는";
                case "을": case "를": return batchim ? "을" : "를";
                case "과": case "와": return batchim ? "과" : "와";
                default: return p;
            }
        }

        // ---------- Building ----------

        static EventChoice C(string key, string label, string result, int heart, Action<EventEffects> more = null) => new EventChoice
        {
            Label = label, Result = result,
            Apply = e => { AddHeart(e, key, heart); more?.Invoke(e); },
        };

        static StoryEvent S(string id, string title, string text, params EventChoice[] choices)
        {
            var ev = new StoryEvent { Id = id, Title = title, Text = text, Weight = (_, __) => 0 };
            ev.Choices.AddRange(choices);
            return ev;
        }

        static readonly List<Def> Defs = new List<Def>
        {
            // ===== 마지막 순번 =====
            new Def
            {
                Key = "han_sora", Name = "한소라", Role = "스포츠 데일리 신입 기자", Route = StoryRoute.LastPick,
                Steps = new[]
                {
                    S("her_han_sora_1", "2군 담당 막내 기자",
                        "관중석이 텅 빈 2군 경기. 기자석에 노트북을 편 사람이 딱 한 명 있었다.\n경기가 끝나자 한소라가 녹음기를 들고 다가왔다.\n\n한소라가 말했다. \"저도 올해 들어온 막내라 2군 담당이에요. 우리 둘 다 마지막 순번이네요.\"",
                        C("han_sora", "인터뷰에 성실히 답한다", "질문이 끝나고도 한참 이야기를 했다. 한소라가 수첩을 덮으며 웃었다. \"오늘 기사, 잘 쓸게요.\"", 15, e => e.Morale(3)),
                        C("han_sora", "짧게 답하고 훈련하러 간다", "\"바쁘시구나.\" 한소라가 고개를 끄덕였다. 다음 날 기사에는 '말보다 스윙이 많은 선수'라고 적혀 있었다.", 5, e => e.Exp(Stat.Contact, 30))),
                    S("her_han_sora_2", "잘린 기사",
                        "우천 취소. 기자실 앞 처마 밑에서 한소라가 비를 보고 서 있었다.\n한소라가 말했다. \"'마지막 순번의 하루'라는 기사를 썼는데요, 데스크에서 잘렸어요. 아무도 안 궁금해한대요.\"",
                        C("han_sora", "\"그 기사, 제가 읽고 싶어요\"", "그날 밤 메일 한 통이 왔다. 원고 맨 끝 줄. '이 선수는 언젠가 1면에 나온다.' 한참 화면을 봤다.", 15, e => e.Morale(5)),
                        C("han_sora", "\"다음엔 실릴 거예요\"", "한소라가 피식 웃었다. \"그쪽이 잘해야 실리죠.\"", 5, e => e.Morale(2))),
                    S("her_han_sora_3", "손으로 쓴 기록",
                        "경기 후 주차장. 한소라가 낡은 수첩 한 권을 내밀었다.\n2군 첫 경기부터 오늘까지, 내 기록이 전부 손글씨로 적혀 있었다.\n\n한소라가 말했다. \"기사엔 안 쓰는 거예요. 그냥, 제가 보려고.\"",
                        C("han_sora", "\"밥 한번 살게요. 기사 말고요\"", "\"...기사 말고요?\" 한소라의 귀가 빨개졌다. \"그럼 국밥으로 해요. 비싼 거 사면 기사 써 버릴 거예요.\"", 15),
                        C("han_sora", "\"고마워요. 계속 써 주세요\"", "한소라가 수첩을 가방에 넣으며 말했다. \"그럼 계속 잘하세요. 빈칸 생기면 섭섭할 거예요.\"", 5, e => e.Fans(30))),
                },
                Confess = "구장 뒤 주차장, 마감을 끝낸 한소라를 기다렸다.\n말을 꺼내자 한소라가 한참 아무 말도 하지 않았다.\n\n한소라가 말했다. \"기자가 선수랑 이러면 안 되는데... 그럼 당신 기사는 이제 다른 사람이 쓰라고 할게요.\"",
                Date = S("her_han_sora_date", "마감 날의 야식",
                    "밤 11시. '마감 중, 오늘 못 봐요 ㅠ' 문자가 왔다.\n신문사 건물 불이 한 층만 켜져 있었다.",
                    C("han_sora", "국밥 두 그릇을 사 들고 간다", "로비로 내려온 한소라가 국밥 봉지를 보고 웃음을 터뜨렸다. \"첫 데이트도 국밥이었는데.\" 계단에 앉아 같이 먹었다.", 10, e => { e.Morale(6); e.Fatigue(5); }),
                    C("han_sora", "'기사 잘 써' 문자를 보낸다", "새벽 2시에 답장이 왔다. '덕분에 다 썼어. 내일 1면 봐.' 1면은 내 기사였다.", 5, e => e.Morale(4))),
                ProposeTitle = "1면",
                ProposeText = "한소라가 처음으로 1면 기사를 쓴 날. 그 기사의 주인공은 나였다.\n신문을 사 들고 그녀의 집 앞으로 갔다. 신문 사이에 작은 상자를 끼워서.",
                ProposeYes = "신문을 펼친 한소라가 그대로 굳었다.\n\"이거... 내가 쓴 기사 중에 제일 좋은 기사 됐네.\" 울면서 웃었다.",
                Ending = "한소라는 지금 스포츠 데일리 야구부장이다. 내 은퇴 기사는 끝내 후배에게 맡겼다. \"이건 내가 쓰면 객관적일 수가 없어서.\"",
            },
            new Def
            {
                Key = "kang_yujin", Name = "강유진", Role = "물리치료사", Route = StoryRoute.LastPick,
                Steps = new[]
                {
                    S("her_kang_yujin_1", "무뚝뚝한 치료사",
                        "슬라이딩하다 손목을 접질려 재활센터에 갔다.\n치료사가 차트를 보며 손목을 이리저리 눌렀다. 웃음기 하나 없는 얼굴.\n\n강유진이 말했다. \"아프면 아프다고 하세요. 참는 거 하나도 안 멋있어요.\"",
                        C("kang_yujin", "\"...사실 많이 아파요\"", "강유진이 처음으로 고개를 들었다. \"그렇게 말하는 선수가 제일 빨리 나아요.\" 테이핑 손길이 조금 부드러워졌다.", 15, e => e.Fatigue(-10)),
                        C("kang_yujin", "\"괜찮습니다\"", "강유진이 한숨을 쉬더니 손목을 꾹 눌렀다. 비명이 나왔다. \"괜찮다면서요.\"", 0, e => e.Fatigue(-5))),
                    S("her_kang_yujin_2", "새벽 6시의 재활실",
                        "새벽 개인 훈련을 하러 나왔는데 재활실 불이 켜져 있었다.\n강유진이 다른 선수들의 재활 계획서를 쓰고 있었다. 책상에는 커피 두 잔.\n\n강유진이 말했다. \"한 잔은 원래 제 거 두 잔이에요. ...하나 드실래요?\"",
                        C("kang_yujin", "스트레칭을 봐 달라고 한다", "\"골반이 굳어 있네요. 매일 아침 이거 하세요.\" 강유진이 종이에 그림까지 그려 줬다. 다음 날도, 그다음 날도 재활실 불이 켜져 있었다.", 15, e => { e.Fatigue(-10); e.Exp(Stat.Speed, 20); }),
                        C("kang_yujin", "커피만 받아서 나간다", "\"훈련 무리하지 마요.\" 등 뒤에서 짧은 말이 들렸다.", 5)),
                    S("her_kang_yujin_3", "제일 잘된 테이핑",
                        "중요한 경기 전, 발목 테이핑을 받았다.\n강유진이 마지막 한 바퀴를 감고 손바닥으로 발목을 두 번 두드렸다.\n\n강유진이 말했다. \"이거 제가 해 본 테이핑 중에 제일 잘됐어요. 그러니까 다치지 마요.\"",
                        C("kang_yujin", "\"경기 끝나고 저녁 같이 먹어요\"", "강유진이 차트를 들여다보는 척했다. \"...회복식으로만 시키세요. 튀김 먹으면 안 가요.\"", 15),
                        C("kang_yujin", "\"믿고 뛸게요\"", "그날 경기 내내 발목이 가벼웠다. 더그아웃 뒤에서 강유진이 몰래 보고 있었다.", 5, e => e.Morale(3))),
                },
                Confess = "재활실, 마지막 환자가 나간 뒤였다.\n말을 다 듣고도 강유진은 차트에서 눈을 떼지 않았다.\n\n강유진이 말했다. \"...저 원래 선수랑 사적으로 안 만나요.\"\n그리고 차트를 내려놓았다. \"근데 예외로 할게요.\"",
                Date = S("her_kang_yujin_date", "쉬는 날의 등산",
                    "월요일 휴식일. 강유진이 문자를 보냈다.\n'쉬는 날이라고 누워만 있으면 안 돼요. 가벼운 산책. 7시. 늦으면 스쿼트 50개.'",
                    C("kang_yujin", "6시 50분에 도착한다", "강유진이 시계를 보고 아주 조금 웃었다. 산 중턱에서 김밥을 꺼냈는데, 반찬이 전부 단백질이었다.", 10, e => { e.Morale(6); e.Fatigue(-5); }),
                    C("kang_yujin", "늦잠을 자서 7시 10분에 도착한다", "\"스쿼트 50개.\" 진짜 시켰다. 그래도 끝나고 손수건을 건네줬다.", 5, e => { e.Morale(3); e.Exp(Stat.Power, 20); })),
                ProposeTitle = "재활 일지 마지막 장",
                ProposeText = "강유진은 내가 처음 다친 날부터 재활 일지를 써 왔다. 그 일지가 벌써 세 권째다.\n오늘은 내가 그 일지의 마지막 장에 한 줄을 써서 돌려주기로 했다.",
                ProposeYes = "일지를 펼친 강유진이 한참 동안 그 한 줄을 읽었다.\n'평생 담당해 주세요.'\n\"...이건 장기 계약이네요.\" 처음 보는 얼굴로 웃었다.",
                Ending = "강유진은 은퇴한 다음 날에도 아침 스트레칭을 시킨다. \"선수 생활은 끝나도 몸은 평생 쓰는 거예요.\"",
            },

            // ===== 천재의 무게 =====
            new Def
            {
                Key = "soi", Name = "소이", Role = "팬 유튜버", Route = StoryRoute.Prodigy,
                Steps = new[]
                {
                    S("her_soi_1", "구독자 3천 명",
                        "퇴근길 출입구 앞, 분홍 머리의 팬이 카메라를 들고 서 있었다.\n소이가 말했다. \"'야구하는 소이'예요! 구독자 3천 명... 아니 2천9백 명이요. 인사 한 번만 해 주시면 안 돼요?\"",
                        C("soi", "카메라에 손을 흔들어 준다", "\"헐, 진짜 해 주셨어!\" 그날 밤 그 영상이 조회수 10만을 넘겼다. 감사 댓글이 길게 달렸다.", 15, e => e.Fans(20)),
                        C("soi", "가볍게 목례만 하고 지나간다", "\"그것도 감사합니다!\" 등 뒤에서 밝은 목소리가 들렸다.", 5)),
                    S("her_soi_2", "대신 맞은 악플",
                        "슬럼프 기사가 쏟아진 주. 소이의 영상 댓글창에도 내 욕이 가득했는데, 전부 지워져 있었다.\n대신 소이 채널이 같이 공격받고 있었다. '천재 빠순이' 같은 말로.",
                        C("soi", "소이 영상에 직접 댓글을 단다", "'좋은 영상 고맙습니다. 다음 경기 꼭 이길게요. - {name}' 그 댓글이 고정됐다. 다음 날 소이의 구독자가 두 배가 됐다.", 15, e => { e.Fans(30); e.Morale(4); }),
                        C("soi", "\"제 편 안 들어도 돼요\"라고 메시지를 보낸다", "답장은 짧았다. '제 채널이니까 제 맘이에요!' 이모티콘이 열 개 붙어 있었다.", 5)),
                    S("her_soi_3", "카메라를 끈 소이",
                        "경기 후 구장 밖 벤치. 카메라를 끄고 혼자 울고 있는 사람이 있었다. 소이였다.\n소이가 말했다. \"채널 접을까 봐요. 나 하나 좋아서 하는 건데, 다들 왜 이렇게 미워해요?\"",
                        C("soi", "옆에 앉아 아무 말 없이 있어 준다", "한참 뒤 소이가 코를 훌쩍이며 웃었다. \"...이거 찍었으면 조회수 대박인데.\"", 15),
                        C("soi", "\"그만두지 마요. 제가 첫 구독자예요\"", "소이가 눈을 동그랗게 떴다. \"거짓말. ...진짜요?\" 휴대폰을 보여 줬다. 구독 날짜가 첫 영상 날이었다.", 15, e => e.Morale(3)),
                        C("soi", "못 본 척 지나간다", "다음 날, 소이의 새 영상이 올라왔다. 평소보다 조금 조용한 목소리였다.", 0)),
                },
                Confess = "\"카메라 꺼 주세요.\" 이번에는 내가 말했다.\n소이가 고개를 갸웃하며 카메라를 내렸다. 말을 다 듣고 나서, 얼굴이 머리색만큼 빨개졌다.\n\n소이가 말했다. \"아... 이거 방송 못 하겠다. 진짜로.\"",
                Date = S("her_soi_date", "몰래 데이트",
                    "모자를 깊게 쓰고 소이와 놀이공원에 갔다.\n입구에서부터 누군가 '어? 저 사람...' 하고 수군거렸다.",
                    C("soi", "모자를 벗고 당당하게 논다", "결국 사진이 찍혔다. 소이는 걱정했지만, 댓글은 '둘이 너무 잘 어울린다'로 가득했다.", 10, e => { e.Morale(6); e.Fans(30); }),
                    C("soi", "소이 손을 잡고 뛰어서 도망친다", "회전목마 뒤에 숨어서 둘이 숨넘어가게 웃었다. 카메라는 끝까지 꺼져 있었다.", 10, e => e.Morale(7))),
                ProposeTitle = "구독자 100만 라이브",
                ProposeText = "'야구하는 소이' 구독자 100만 기념 라이브 날.\n방송이 끝나고 카메라 불이 꺼지는 순간을 기다렸다. 주머니 속 상자를 쥔 채로.",
                ProposeYes = "소이가 두 손으로 입을 막았다.\n\"아 진짜... 카메라 켜 둘걸!\" 그러고는 엉엉 울었다.",
                Ending = "'야구하는 소이' 채널의 지난 영상 제목은 '구독자 여러분, 저 결혼했어요 (남편은 비밀 아님)'. 조회수는 지금도 1위다.",
            },
            new Def
            {
                Key = "seo_harin", Name = "서하린", Role = "고교 동창·교사", Route = StoryRoute.Prodigy,
                Steps = new[]
                {
                    S("her_seo_harin_1", "고교 야구부 매니저",
                        "원정 숙소 앞에 초등학생 견학단이 줄지어 있었다. 인솔하는 선생님 얼굴이 낯익었다.\n고교 야구부 매니저였던 서하린이었다.\n\n서하린이 말했다. \"천재 소리 듣기 전부터 알았지. 너 그때 맨날 제일 늦게까지 공 줍던 애.\"",
                        C("seo_harin", "아이들에게 한 명씩 사인을 해 준다", "서하린이 아이들 뒤에서 팔짱을 끼고 웃었다. \"그때랑 똑같네. 시키지도 않은 걸 끝까지 해.\"", 15, e => { e.Fans(20); e.Fatigue(5); }),
                        C("seo_harin", "\"오랜만이다\" 하고 짧게 인사한다", "\"응, 오랜만.\" 서하린이 아이들을 데리고 돌아섰다. 뒷모습이 고3 때랑 똑같았다.", 5)),
                    S("her_seo_harin_2", "옛날 기록지",
                        "서하린에게서 사진 한 장이 왔다. 고교 시절 야구부 기록지였다.\n맨 아래 칸에 동글동글한 글씨로 적혀 있었다. '오늘도 마지막까지 남음.'",
                        C("seo_harin", "\"그거 나 줘\"라고 답장한다", "다음 원정 때 서하린이 코팅까지 한 기록지를 들고 왔다. \"잃어버리면 죽어.\"", 15, e => e.Morale(5)),
                        C("seo_harin", "\"그땐 그랬지\"라고 답장한다", "'지금도 그러잖아. 다 봤어.' 답장이 왔다.", 5, e => e.Morale(2))),
                    S("her_seo_harin_3", "천재 아니어도 돼요",
                        "'천재의 몰락' 기사가 쏟아진 날. 구단으로 편지 한 묶음이 왔다.\n서하린네 반 아이들이 쓴 것이었다. 삐뚤빼뚤한 글씨로 '천재 아니어도 돼요. 그냥 야구해 주세요.'",
                        C("seo_harin", "쉬는 날 학교로 찾아가 일일 코치를 한다", "운동장에서 아이들과 캐치볼을 했다. 서하린이 스탠드에서 그때처럼 기록지를 들고 있었다.", 15, e => { e.Fatigue(10); e.Fans(30); e.Morale(5); }),
                        C("seo_harin", "아이들 한 명 한 명에게 답장을 쓴다", "서른 장을 쓰는 데 새벽 세 시까지 걸렸다. 서하린이 사진을 보냈다. 교실 벽에 답장이 줄줄이 붙어 있었다.", 5, e => e.Morale(5))),
                },
                Confess = "고등학교 운동장 스탠드. 둘이 매일 앉던 자리.\n\"고3 때 하려던 말이 있는데, 이제 해도 돼?\"\n\n서하린이 웃었다. \"늦었어. 나 그때부터 기다렸거든.\"",
                Date = S("her_seo_harin_date", "방학 숙제",
                    "서하린이 방학 숙제 검사를 하는 날이라며 교무실로 불렀다.\n책상 위에 아이들 그림일기가 쌓여 있었다. 절반이 내 그림이었다.",
                    C("seo_harin", "옆에 앉아 같이 도장을 찍는다", "'참 잘했어요' 도장을 백 개쯤 찍었다. 서하린이 내 손등에도 하나 찍었다.", 10, e => e.Morale(6)),
                    C("seo_harin", "그림일기 하나하나에 사인을 남긴다", "개학 날, 반 아이들이 소리를 질렀다는 문자가 왔다. '오늘 수업 망했어. 고마워.'", 10, e => { e.Morale(5); e.Fans(20); })),
                ProposeTitle = "그때 그 스탠드",
                ProposeText = "고등학교 운동장, 그때 그 스탠드에 다시 앉았다.\n서하린이 코팅해 준 기록지 뒷면에 한 줄을 적어 왔다.",
                ProposeYes = "기록지를 뒤집은 서하린이 한참 말이 없었다.\n'평생 마지막까지 남을게.'\n\"...이건 내가 쓴 글씨보다 못생겼다.\" 그래도 손가락을 내밀었다.",
                Ending = "서하린은 지금도 초등학교에서 아이들을 가르친다. 방학이면 둘이 학교 운동장에 작은 야구 교실을 연다. 기록지는 여전히 서하린이 쓴다.",
            },

            // ===== 연습생 신화 =====
            new Def
            {
                Key = "jung_gaeul", Name = "정가을", Role = "국밥집 손녀", Route = StoryRoute.Walkon,
                Steps = new[]
                {
                    S("her_jung_gaeul_1", "국밥집 손녀",
                        "할머니 국밥집 카운터에 낯선 얼굴이 앉아 있었다. 밀가루가 묻은 앞치마.\n정가을이 말했다. \"할머니가 연습생 오면 한 그릇 더 주라던데... 그게 그쪽이구나.\"",
                        C("jung_gaeul", "\"잘 먹겠습니다!\" 두 그릇을 비운다", "정가을이 빈 그릇 두 개를 보고 웃음을 터뜨렸다. \"할머니가 왜 좋아하는지 알겠다.\"", 15, e => { e.Fatigue(-5); e.Morale(3); }),
                        C("jung_gaeul", "\"괜찮아요, 계산할게요\"", "\"할머니한테 혼나요, 그러면.\" 정가을이 돈을 도로 밀어냈다.", 5)),
                    S("her_jung_gaeul_2", "실패한 빵",
                        "가게 뒷문 앞에서 정가을이 빵 봉지를 들고 쪼그려 앉아 있었다.\n정가을이 말했다. \"제빵 시험 또 떨어졌어요. 버리긴 아까워서... 연습생이면 배고프죠?\"",
                        C("jung_gaeul", "그 자리에서 전부 먹는다", "조금 탔고, 조금 짰다. 그래도 다 먹었다. 정가을이 무릎에 얼굴을 묻고 웃었다. \"바보 같아.\"", 15, e => e.Morale(4)),
                        C("jung_gaeul", "\"다음엔 붙을 거예요\"", "\"연습생한테 그런 말 들으니까 이상하게 힘 나네.\" 정가을이 봉지를 건넸다.", 5, e => e.Morale(2))),
                    S("her_jung_gaeul_3", "셔터를 내리는 밤",
                        "할머니가 감기에 걸렸다. 국밥집에 정가을 혼자였다. 손님은 끝이 없고, 그릇은 산더미였다.",
                        C("jung_gaeul", "말없이 앞치마를 두른다", "문 닫을 때까지 설거지를 했다. 정가을이 셔터를 내리며 말했다. \"선수가 이런 거 해도 돼요?\" \"연습생이라 괜찮아요.\"", 15, e => e.Fatigue(10)),
                        C("jung_gaeul", "할머니 약을 사 온다 (10만원)", "정가을이 약봉지를 받아 들고 한참 서 있었다. \"...고마워요. 할머니가 알면 또 한 그릇 더 주라고 하겠다.\"", 5, e => e.Money(-10))),
                },
                Confess = "국밥집 셔터를 같이 내리던 밤.\n정가을이 먼저 말했다. \"할머니가 그러는데, 너 나 좋아한대.\"\n\"...할머니 말이 맞아.\"\n\n셔터 너머에서 할머니가 크게 헛기침을 했다.",
                Date = S("her_jung_gaeul_date", "첫 합격",
                    "'붙었어!!!!!!' 정가을의 문자. 제빵 자격증 합격 통지서 사진이 따라왔다.",
                    C("jung_gaeul", "꽃다발을 들고 국밥집으로 달려간다", "할머니가 꽃다발을 보고 가게 손님들한테 국밥을 한 그릇씩 돌렸다. 정가을은 부끄러워서 주방에 숨었다.", 10, e => { e.Morale(6); e.Money(-10); }),
                    C("jung_gaeul", "'첫 빵은 나한테 팔아' 답장한다", "다음 날, 처음으로 하나도 안 탄 빵이 왔다. 포장지에 '1호 손님'이라고 적혀 있었다.", 10, e => e.Morale(5))),
                ProposeTitle = "빵집 개업 날",
                ProposeText = "국밥집 옆 빈 가게에 정가을의 작은 빵집이 문을 열었다.\n첫 손님으로 줄을 섰다. 주머니 속 상자를 만지작거리며.",
                ProposeYes = "\"뭐 드릴까요, 1호 손님?\"\n상자를 계산대에 올려놓았다. 정가을이 앞치마로 얼굴을 가렸다.\n옆 가게에서 할머니가 국자를 든 채로 뛰어나왔다.",
                Ending = "국밥집 옆 빵집의 간판 이름은 '연습생'이다. 매일 아침 첫 손님은 할머니, 두 번째 손님은 나다.",
            },
            new Def
            {
                Key = "yoon_sea", Name = "윤세아", Role = "도서관 사서", Route = StoryRoute.Walkon,
                Steps = new[]
                {
                    S("her_yoon_sea_1", "비 오는 도서관",
                        "우천으로 훈련이 취소됐다. 비를 피해 들어간 곳은 2군 구장 옆 시립도서관.\n야구 책 코너에서 책을 빼는데, 사서가 다가왔다.\n\n윤세아가 말했다. \"그 책 대출 기록에 이름이 한 명밖에 없어요. 매번 같은 분. ...그쪽이죠?\"",
                        C("yoon_sea", "\"추천해 줄 책 있어요?\"", "윤세아가 서가 사이를 오가더니 책 세 권을 안겨 줬다. 그중 하나는 야구 책이 아니었다. '이것도 읽어 보세요. 마음이 편해져요.'", 15, e => e.Exp(Stat.Eye, 30)),
                        C("yoon_sea", "책만 빌려서 나간다", "대출 영수증 뒤에 연필 글씨가 있었다. '반납일 지켜 주세요.'", 5)),
                    S("her_yoon_sea_2", "연체 안내",
                        "원정 때문에 책을 2주나 연체했다. 도서관에서 문자가 왔다.\n'반납 예정일이 지났습니다.' 그 아래 한 줄이 더 있었다. '어제 경기 잘 봤어요.'",
                        C("yoon_sea", "책을 들고 직접 반납하러 간다", "윤세아가 연체료 대신 다음 책을 내밀었다. \"이건 반납일 넉넉하게 해 드릴게요.\"", 15, e => e.Morale(4)),
                        C("yoon_sea", "'죄송합니다, 곧 갈게요' 답장한다", "'천천히 오세요. 책은 기다려 줘요.' 답장이 왔다.", 5)),
                    S("her_yoon_sea_3", "토요일 낭독회",
                        "윤세아가 도서관 어린이 낭독회에 와 줄 수 있냐고 물었다.\n윤세아가 말했다. \"야구 그림책이 있는데, 진짜 선수가 읽어 주면 애들이 좋아할 것 같아서요.\"",
                        C("yoon_sea", "토요일 아침 도서관에 간다", "그림책 속 타자 흉내를 내자 아이들이 뒤집어졌다. 맨 뒷줄에서 윤세아가 소리 없이 웃고 있었다.", 15, e => { e.Fans(20); e.Fatigue(5); e.Morale(4); }),
                        C("yoon_sea", "사인한 그림책만 보낸다", "'아이들이 좋아했어요. 다음엔 직접 와 주세요.' 사진 속 아이들이 그림책을 들고 있었다.", 5, e => e.Fans(10))),
                },
                Confess = "도서관 폐관 시간. 마지막 책을 반납하면서 말을 꺼냈다.\n윤세아가 아무 말 없이 대출 카드 한 장을 내밀었다.\n\n카드 뒷면에 단정한 글씨로 적혀 있었다. '저도요.'",
                Date = S("her_yoon_sea_date", "서가 사이",
                    "폐관 후 도서관. 윤세아가 열쇠를 흔들어 보였다.\n\"오늘 정리 당번이에요. 같이 할래요?\"",
                    C("yoon_sea", "같이 책을 정리한다", "불 꺼진 서가 사이에서 손전등 하나로 책을 꽂았다. 야구 코너에 내 이름이 들어간 신문 스크랩북이 새로 생겨 있었다.", 10, e => e.Morale(6)),
                    C("yoon_sea", "좋아하는 책을 하나씩 골라 읽어 준다", "윤세아가 고른 책은 시집이었다. 내가 고른 책은 야구 규칙집이었다. 둘 다 끝까지 읽었다.", 10, e => { e.Morale(5); e.Exp(Stat.Eye, 20); })),
                ProposeTitle = "처음 추천받은 책",
                ProposeText = "비 오던 날 윤세아가 추천해 준 첫 책. 야구 책이 아니었던 그 책.\n같은 책을 사서, 마지막 장 사이에 작은 반지를 끼웠다.",
                ProposeYes = "책장을 넘기던 윤세아의 손이 멈췄다.\n\"...반납일은요?\"\n\"없어요. 평생 대출이에요.\"",
                Ending = "윤세아는 지금 시립도서관 관장이다. 야구 코너 맨 윗칸에는 내 자서전이 꽂혀 있다. 대출 1위다.",
            },

            // ===== 아버지의 등번호 =====
            new Def
            {
                Key = "min_chaeeun", Name = "민채은", Role = "소꿉친구", Route = StoryRoute.Legacy,
                Steps = new[]
                {
                    S("her_min_chaeeun_1", "소꿉친구",
                        "시구 행사에 소프트볼 국가대표가 왔다. 마운드에서 공을 받는데 얼굴이 낯익었다.\n아버지 야구 교실에서 같이 공을 던지던 민채은이었다.\n\n민채은이 말했다. \"야, 너 아직도 10번 달고 싶다고 울던 거 기억하냐?\"",
                        C("min_chaeeun", "행사 끝나고 같이 캐치볼을 한다", "어릴 적처럼 해 질 때까지 공을 주고받았다. 민채은의 공이 여전히 더 아팠다.", 15, e => e.Morale(5)),
                        C("min_chaeeun", "\"그 얘긴 하지 마\"", "민채은이 깔깔 웃었다. \"그럼 다른 얘기 많이 하자. 아직 많아.\"", 5)),
                    S("her_min_chaeeun_2", "배팅센터 내기",
                        "쉬는 날, 민채은이 배팅센터로 불러냈다.\n민채은이 말했다. \"홈런 표지판 많이 맞히는 사람이 저녁 사기. 봐주면 죽는다.\"",
                        C("min_chaeeun", "전력으로 붙는다", "한 개 차이로 이겼다. 민채은이 배트를 내던지며 소리를 질렀다. \"한 판 더!\" 결국 저녁은 둘이 반씩 냈다.", 15, e => e.Exp(Stat.Power, 30)),
                        C("min_chaeeun", "적당히 져 준다", "민채은이 표정을 굳혔다. \"봐주는 거 제일 싫어. 알잖아.\" 저녁 내내 말이 없었다.", 0)),
                    S("her_min_chaeeun_3", "결승 전날 밤",
                        "밤늦게 전화가 울렸다. 국제대회 결승을 앞둔 민채은이었다.\n민채은이 말했다. \"나 떨려. 웃기지? 국가대표가.\"",
                        C("min_chaeeun", "\"아버지가 그러셨잖아. 첫 공만 생각하라고\"", "수화기 너머가 조용해졌다. \"...그 말 잊고 있었다. 고마워.\" 다음 날, 민채은은 결승타를 쳤다.", 15, e => e.Morale(4)),
                        C("min_chaeeun", "다음 날 아침에 응원 문자를 보낸다", "'고마워!' 짧은 답장. 결승 중계 화면 속 민채은이 잠깐 하늘을 봤다.", 5)),
                },
                Confess = "어릴 적 그 운동장. 아버지 야구 교실의 낡은 마운드.\n민채은이 말했다. \"너는 맨날 등번호 얘기만 했잖아.\"\n\"이제 다른 얘기 하려고.\"\n\n민채은이 글러브로 내 머리를 툭 쳤다. \"늦어, 바보야.\"",
                Date = S("her_min_chaeeun_date", "둘만의 홈런 더비",
                    "민채은이 소프트볼 구장 야간 조명을 빌렸다며 불렀다.\n\"관중 없는 홈런 더비. 진 사람이 소원 들어주기.\"",
                    C("min_chaeeun", "진심으로 친다", "마지막 스윙까지 동점. 민채은이 배트를 내려놓고 말했다. \"그럼 소원 하나씩.\" 둘 다 같은 소원이었다.", 10, e => { e.Morale(6); e.Exp(Stat.Power, 20); }),
                    C("min_chaeeun", "조명 아래 그냥 공을 주고받는다", "말없이 공을 주고받는 소리만 났다. 어릴 적이랑 똑같은 소리였다.", 10, e => e.Morale(6))),
                ProposeTitle = "야구 교실 마운드",
                ProposeText = "아버지 야구 교실 마운드에 민채은을 불렀다.\n어릴 적 둘이 이름을 새겨 둔 펜스 앞. 주머니에는 공 하나와 작은 상자.",
                ProposeYes = "공 대신 상자를 던졌다. 민채은이 한 손으로 받았다.\n열어 보고는 글러브로 얼굴을 가렸다. \"...이건 반칙이잖아.\"\n펜스 너머에서 아버지가 박수를 쳤다.",
                Ending = "민채은은 은퇴 후 아버지의 야구 교실을 같이 맡았다. 아이들은 둘 중 누가 더 멀리 치는지 아직도 내기를 한다. 정답은 아무도 모른다.",
            },
            new Def
            {
                Key = "han_yerin", Name = "한예린", Role = "라디오 PD", Route = StoryRoute.Legacy,
                Steps = new[]
                {
                    S("her_han_yerin_1", "지워진 질문",
                        "오지영의 라디오 부스에 게스트로 갔다. 질문지 첫 줄은 역시 '아버님은 뭐라고 하셨나요?'.\n그 줄을 신입 PD가 펜으로 쓱 그었다.\n\n한예린이 말했다. \"아버님 얘기 말고요. 오늘 경기 얘기 해 주세요. 그게 더 궁금해요.\"",
                        C("han_yerin", "오늘 경기 이야기를 한다", "처음으로 아버지 이름이 한 번도 안 나온 인터뷰였다. 방송이 끝나고 한예린이 엄지를 들었다.", 15, e => e.Morale(5)),
                        C("han_yerin", "결국 아버지 이야기를 꺼낸다", "한예린이 조금 아쉬운 얼굴로 고개를 끄덕였다. \"다음엔 그쪽 얘기 해 줘요.\"", 5)),
                    S("her_han_yerin_2", "새벽 라디오",
                        "원정 버스, 새벽 1시. 라디오에서 한예린이 만드는 심야 프로가 흘러나왔다.\n'청취자 사연입니다. 저는 10번 아들 말고, 그 선수 자체를 응원합니다.'\n휴대폰이 울렸다. '오늘 신청곡은 제가 골랐어요.'",
                        C("han_yerin", "답장으로 신청곡을 보낸다", "다음 날 새벽, 그 노래가 나왔다. '어느 야구 선수의 신청곡입니다.' 버스 맨 뒷자리에서 혼자 웃었다.", 15, e => e.Morale(4)),
                        C("han_yerin", "'잘 들었어요'라고만 보낸다", "'다음엔 신청곡도 보내 줘요.' 답장이 왔다.", 5)),
                    S("her_han_yerin_3", "다시 녹음",
                        "아버지 특집 방송 녹음 날. 한예린이 녹음 버튼을 잘못 눌러 30분 분량이 날아갔다.\n아버지는 이미 돌아가셨다. 한예린의 눈이 빨개졌다.",
                        C("han_yerin", "\"제가 대신 다시 할게요\"", "아버지 이야기를 내 목소리로 다시 녹음했다. 처음으로 아버지 이야기가 하나도 무겁지 않았다. 한예린이 부스 밖에서 내내 고개를 숙이고 있었다.", 15, e => { e.Fatigue(5); e.Morale(4); }),
                        C("han_yerin", "괜찮다고 다독인다", "\"누구나 실수해요. 저도 오늘 삼진 세 개 당했어요.\" 한예린이 울다가 웃었다.", 5)),
                },
                Confess = "방송국 옥상. 한예린이 마지막 곡을 틀고 올라왔다.\n말을 다 듣고, 한예린이 이어폰 한쪽을 내 귀에 꽂아 줬다.\n\n한예린이 말했다. \"오늘 마지막 곡, 제 신청곡이에요. 제목이 대답이에요.\"",
                Date = S("her_han_yerin_date", "공개 방송",
                    "한예린이 맡은 프로그램이 처음으로 공개 방송을 한다. 객석 맨 뒷줄에 모자를 쓰고 앉았다.",
                    C("han_yerin", "끝까지 조용히 지켜본다", "방송이 끝나고 한예린이 객석으로 뛰어왔다. \"왔으면 말을 하지!\" 그래도 제일 먼저 나를 찾았다.", 10, e => e.Morale(6)),
                    C("han_yerin", "사연 쪽지를 몰래 넣는다", "'PD님 오늘 너무 멋있습니다. - 맨 뒷줄 야구 선수' 진행자가 그걸 읽어 버렸다. 한예린이 부스에서 얼굴을 감쌌다.", 10, e => { e.Morale(6); e.Fans(20); })),
                ProposeTitle = "생방송 사연",
                ProposeText = "한예린의 프로그램 개편 첫 생방송. 사연 게시판에 이름 없이 글을 하나 올렸다.\n'10번 아들 말고, 그냥 한 사람으로서 청혼합니다.'",
                ProposeYes = "진행자가 사연을 읽다가 멈췄다. 부스 안의 한예린이 창밖을 봤다. 반지 상자를 든 내가 서 있었다.\n방송 사고 3초. 그다음은 박수 소리였다.",
                Ending = "한예린의 라디오는 지금 오지영의 시간대에 나간다. 오프닝 멘트는 늘 같다. '오늘도 등번호 말고, 사람 이야기.'",
            },
        };
    }
}
