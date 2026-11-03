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
    /// The romance: every story has two heroines, met by chance and kept meeting through things that
    /// would have happened anyway (an injury, a rainout, a deadline). No dating two at once: twice
    /// the same day pulls the player two ways (a crossroads) and where he goes fills one heart and
    /// cools the other. When one heart is clearly ahead, the other woman moves on with her own life
    /// (a new job, another city) and a 썸 begins: two small fluttering scenes, then the confession.
    /// Only when both are close does the player have to decide one night. A date, the proposal and
    /// a line in the ending follow. The generic blind-date romance only comes when no heroine came.
    /// Kept in story marks ("heart:key", "some:key", "drift:key", "lover:key"), so no save change.
    /// </summary>
    public static class Romance
    {
        public const int StartHeart = 20, ConfessAt = 40;
        /// <summary>One heart this far ahead of the other starts the 썸 without a hard choice.</summary>
        public const int ClearLead = 10;
        const string HeartPrefix = "heart:", LoverPrefix = "lover:", SomePrefix = "some:", DriftPrefix = "drift:";
        const string ChoiceId = "her_choice", ProposeId = "her_propose";

        class Def : Heroine
        {
            public StoryEvent[] Steps;
            /// <summary>Two small scenes of the 썸, after the other heroine moved on.</summary>
            public StoryEvent[] Some;
            /// <summary>How she moves on when the player's heart went to the other one.</summary>
            public string DriftTitle, Drift;
            /// <summary>The confession, said by the player / by her.</summary>
            public string Confess, HerFirst;
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

        /// <summary>The heroine of the 썸 (before the confession), or null.</summary>
        public static Heroine Some(Career c) => Defs.FirstOrDefault(d => c.HasMark(SomePrefix + d.Key));

        /// <summary>She moved on with her own life.</summary>
        public static bool Drifted(Career c, string key) => c.HasMark(DriftPrefix + key);

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

            var x = Crossroads.Where(r => r.Route == c.Story.Route).ToList();
            string prev = null;
            foreach (var ev in new[] { hs[0].Steps[0], hs[1].Steps[0], hs[0].Steps[1], hs[1].Steps[1], x[0].Event, hs[0].Steps[2], hs[1].Steps[2], x[1].Event })
            {
                if (!c.Seen(ev.Id))
                {
                    bool ready = prev == null ? c.Season >= 2 || c.Day >= season * 3 / 10 : Due("seen:" + prev, gap);
                    // Meeting them happens by chance; letting one of them matter waits until the player has made it.
                    if (ev.Id.StartsWith("her_x_") && !Established(c)) ready = false;
                    return ready ? ev : null;
                }
                prev = ev.Id;
            }

            var lover = (Def)Lover(c);
            if (lover == null)
            {
                var some = (Def)Some(c);
                if (some == null)
                {
                    if (c.HasMark("romance_none") || c.Me.Age > 34) return null;
                    var open = hs.Where(h => Heart(c, h.Key) >= ConfessAt).OrderByDescending(h => Heart(c, h.Key)).ToList();
                    if (open.Count == 0) return !c.Seen(ChoiceId) && Due("seen:" + prev, gap) ? Apart(hs) : null;
                    if (open.Count == 1 || Heart(c, open[0].Key) - Heart(c, open[1].Key) >= ClearLead)
                        return Due("seen:" + prev, gap) ? DriftScene(hs.First(h => h != open[0]), open[0]) : null;
                    if (!c.Seen(ChoiceId)) return Due("seen:" + prev, gap) ? Choice(c, open) : null;
                    return c.HasMark("romance_wait") && !c.SeenThisSeason(ChoiceId) && c.Day >= season * 3 / 10 ? Choice(c, open) : null;
                }
                var other = hs.First(h => h != some);
                if (!Drifted(c, other.Key)) return Due(SomePrefix + some.Key, gap / 2) ? DriftScene(other, some) : null;
                string before = "seen:her_" + other.Key + "_drift";
                foreach (var ev in some.Some)
                {
                    if (!c.Seen(ev.Id)) return Due(before, gap) ? ev : null;
                    before = "seen:" + ev.Id;
                }
                return Due(before, gap) ? Confession(some) : null;
            }
            if (!c.Seen(lover.Date.Id)) return Due(LoverPrefix + lover.Key, season / 2) ? lover.Date : null;
            if (!c.HasMark("married") && ReadyToMarry(c) && Due(LoverPrefix + lover.Key, season) && Due("seen:" + lover.Date.Id, season / 2) && !c.SeenThisSeason(ProposeId))
                return Propose(lover);
            return null;
        }

        /// <summary>The player has a place on the team (the story's "이름이 불리는 자리" chapter has come): only then does he let someone in.</summary>
        public static bool Established(Career c) => c.Story.Seen.Contains("spot");

        /// <summary>The proposal waits for a settled career: 27 or older, and at the top (the summit chapter) or past 29.</summary>
        public static bool ReadyToMarry(Career c) => c.Me.Age >= 27 && (c.Story.Seen.Contains("summit") || c.Me.Age >= 29);

        /// <summary>Neither heart grew: both drift away and the romance closes.</summary>
        static StoryEvent Apart(List<Def> hs) => new StoryEvent
        {
            Id = ChoiceId, Title = "엇갈린 계절",
            Text = $"{hs[0].Name}도, {hs[1].Name}도 요즘은 연락이 뜸하다.\n바쁘다는 핑계로 답장을 미룬 건 나였다.\n\n빈 휴대폰 화면을 한참 보다가, 배트를 들었다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "지금은 야구만 생각한다", Result = "스윙 소리만 밤늦게까지 이어졌다.", Apply = e => { e.Exp(Stat.Power, 40); e.Mark("romance_none"); } },
            },
        };

        /// <summary>Both hearts are close: one night the player has to know which way his own leans.</summary>
        static StoryEvent Choice(Career c, List<Def> open)
        {
            var ev = new StoryEvent
            {
                Id = ChoiceId, Title = "마음이 기우는 쪽",
                Text = c.HasMark("romance_wait")
                    ? $"한 시즌이 또 지나갔다.\n{open[0].Name}도, {open[1].Name}도 아직 그 자리에 있다. 둘 다 기다려 주지는 않을 것이다."
                    : $"원정 숙소, 잠이 오지 않는 밤.\n오늘 {open[0].Name}에게서도, {open[1].Name}에게서도 '경기 잘 봤어요' 문자가 왔다.\n\n둘 다 좋은 사람이다. 그래서 더 이대로 둘 수 없다.\n휴대폰 화면을 켰다 껐다 하다가, 한 사람의 이름 앞에서 손가락이 멈췄다.",
                Weight = (_, __) => 0,
            };
            foreach (var h in open)
            {
                var her = h;
                ev.Choices.Add(new EventChoice
                {
                    Label = $"{her.Name}에게 답장을 쓴다",
                    Result = $"'다음 주에 시간 돼요?' 보내기 버튼을 누르고 한참 숨을 참았다.\n답장은 1분 만에 왔다. '네.' 그 한 글자를 오래 봤다.",
                    Apply = e => { e.Mark(SomePrefix + her.Key); e.Unmark("romance_wait"); AddHeart(e, her.Key, 5); e.Morale(5); },
                });
            }
            ev.Choices.Add(new EventChoice
            {
                Label = "아직은 모르겠다", Result = "휴대폰을 엎어 두었다. 시즌이 끝나면 알게 되겠지.",
                Apply = e => { e.Mark("romance_wait"); e.Exp(Stat.Eye, 30); },
            });
            return ev;
        }

        /// <summary>The other heroine moves on with her own life; the 썸 with the one the heart went to begins.</summary>
        static StoryEvent DriftScene(Def her, Def chosen) => new StoryEvent
        {
            Id = "her_" + her.Key + "_drift", Title = her.DriftTitle, Text = her.Drift,
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "진심으로 응원한다", Result = $"\"잘 지내요. 진짜로.\" 웃으며 인사했다.\n돌아오는 길, 휴대폰에 {chosen.Name}의 이름이 떠 있었다.",
                    Apply = e => { e.Mark(SomePrefix + chosen.Key); e.Mark(DriftPrefix + her.Key); e.Morale(3); } },
                new EventChoice { Label = "짧게 인사만 한다", Result = $"인사는 짧았다. 그래도 그게 맞는 것 같았다.\n그날 밤, {chosen.Name}에게서 온 문자를 몇 번이고 다시 읽었다.",
                    Apply = e => { e.Mark(SomePrefix + chosen.Key); e.Mark(DriftPrefix + her.Key); } },
            },
        };

        static StoryEvent Confession(Def her) => new StoryEvent
        {
            Id = "her_" + her.Key + "_confess", Title = "고백",
            Text = $"요즘 {her.Name}{Particle(her.Name, "을")} 만나고 돌아오는 길이 점점 길어진다.\n오늘은 헤어지기 직전, 둘 다 한참 말이 없었다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "내가 먼저 말한다", Result = her.Confess, Apply = e => Together(e, her) },
                new EventChoice { Label = "그녀가 말을 꺼내길 기다린다", Result = her.HerFirst, Apply = e => Together(e, her) },
            },
        };

        static void Together(EventEffects e, Def her)
        {
            e.Mark(LoverPrefix + her.Key);
            e.Unmark(SomePrefix + her.Key);
            e.Mark("dating");
            AddHeart(e, her.Key, 10);
            e.Morale(8);
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

        /// <summary>A crossroads answer: going to one of them fills her heart and cools the other's a little.</summary>
        static EventChoice X(string to, string away, string label, string result, Action<EventEffects> more = null) => new EventChoice
        {
            Label = label, Result = result,
            Apply = e => { AddHeart(e, to, 15); AddHeart(e, away, -5); more?.Invoke(e); },
        };

        class Crossroad
        {
            public StoryRoute Route;
            public StoryEvent Event;
        }

        static StoryEvent S(string id, string title, string text, params EventChoice[] choices)
        {
            var ev = new StoryEvent { Id = id, Title = title, Text = text, Weight = (_, __) => 0 };
            ev.Choices.AddRange(choices);
            return ev;
        }

        /// <summary>Two days a story's two heroines need the player at the same time, through no plan of anyone's.</summary>
        static readonly List<Crossroad> Crossroads = new List<Crossroad>
        {
            // ----- 마지막 순번 -----
            new Crossroad { Route = StoryRoute.LastPick, Event = S("her_x_lastpick_1", "같은 날, 두 개의 부탁",
                "1군 콜업 통보를 받은 날 오후. 휴대폰이 연달아 울렸다.\n\n'마감 한 시간 전이에요. 콜업 소감 한 줄만 주시면 제 첫 단독이 돼요.' 한소라였다.\n'어깨 검사 오늘 꼭 받고 올라가세요. 재활실에서 기다릴게요.' 강유진이었다.\n\n짐도 아직 다 못 쌌다. 갈 수 있는 곳은 한 군데뿐이다.",
                X("han_sora", "kang_yujin", "기자실로 간다", "한소라가 녹음기를 쥔 손을 떨며 받아 적었다. 다음 날 아침, 그녀의 첫 단독 기사가 포털 맨 위에 올라 있었다.\n강유진에게서는 '검사는 1군 트레이너한테 꼭 받으세요' 문자 한 줄만 와 있었다.", e => e.Fans(20)),
                X("kang_yujin", "han_sora", "재활실로 간다", "강유진이 어깨를 한참 눌러 보더니 고개를 끄덕였다. \"이제 올라가도 돼요.\" 문 앞까지 배웅하고는 금방 돌아섰다.\n한소라의 기사에는 내 소감 대신 '연락이 닿지 않았다'가 적혀 있었다.", e => e.Fatigue(-10)),
                new EventChoice { Label = "둘 다 양해를 구하고 혼자 짐을 싼다", Result = "둘에게 미안하다는 문자를 보냈다. 빈 방에서 짐을 싸는데 이상하게 마음이 무거웠다.", Apply = e => e.Exp(Stat.Contact, 30) }) },
            new Crossroad { Route = StoryRoute.LastPick, Event = S("her_x_lastpick_2", "비 오는 밤의 두 문자",
                "4타수 무안타에 실책까지. 최악의 경기가 끝났다. 밖에는 비가 쏟아지고 있었다.\n\n'데스크랑 크게 싸웠어요. 그쪽 기사 때문에. ...잠깐 나올 수 있어요?' 한소라.\n'아까 넘어지는 거 봤어요. 재활실 불 켜 둘게요. 오든 말든.' 강유진.\n\n우산은 하나다.",
                X("han_sora", "kang_yujin", "신문사 앞으로 간다", "한소라가 처마 밑에서 젖은 채 웃었다. \"이길 줄 알았는데 졌어요. 그쪽처럼.\" 둘이 편의점 앞에서 컵라면을 먹었다.\n그날 재활실 불은 새벽까지 켜져 있었다고, 나중에 들었다.", e => e.Morale(5)),
                X("kang_yujin", "han_sora", "재활실로 간다", "강유진이 말없이 무릎에 얼음을 대 줬다. \"오늘 같은 날은 혼자 있으면 안 돼요.\" 그 말이 오래 남았다.\n한소라에게서는 '괜찮아요, 다음에 봐요' 한 줄이 와 있었다.", e => e.Fatigue(-10)),
                new EventChoice { Label = "숙소에서 혼자 스윙 영상을 본다", Result = "영상을 열 번쯤 돌려 보다가 잠들었다. 두 문자에는 아침에야 답장했다.", Apply = e => e.Exp(Stat.Eye, 30) }) },

            // ----- 천재의 무게 -----
            new Crossroad { Route = StoryRoute.Prodigy, Event = S("her_x_prodigy_1", "두 개의 출구",
                "홈경기가 끝났다. 1루 쪽 출구에는 서하린네 반 아이들이 견학을 와서 기다리고 있다.\n3루 쪽 출구에서는 소이가 팬 미팅 라이브를 켜 놓고 기다리고 있다.\n\n구단 버스는 30분 뒤에 떠난다. 둘 다 들르기엔 시간이 없다.",
                X("soi", "seo_harin", "3루 쪽, 소이의 라이브로 간다", "라이브 화면에 내가 나타나자 채팅창이 폭발했다. 소이가 카메라를 잡은 손을 덜덜 떨었다. \"여러분 이거 실화예요!\"\n버스 창밖으로, 1루 쪽 출구를 떠나는 아이들의 노란 버스가 보였다.", e => e.Fans(40)),
                X("seo_harin", "soi", "1루 쪽, 아이들에게 간다", "아이들이 소리를 지르며 달려왔다. 서하린이 뒤에서 조용히 고개를 숙였다. \"와 줄 줄 알았어.\"\n그날 소이의 라이브는 평소보다 일찍 끝났다.", e => { e.Fans(20); e.Morale(4); }),
                new EventChoice { Label = "버스로 곧장 간다", Result = "버스 맨 뒷자리에 앉아 창밖을 봤다. 양쪽 출구 모두 한참 동안 사람들이 서 있었다.", Apply = e => e.Fatigue(-5) }) },
            new Crossroad { Route = StoryRoute.Prodigy, Event = S("her_x_prodigy_2", "몰락 기사 다음 날",
                "'천재의 몰락' 기사가 1면에 실렸다.\n\n소이가 연락해 왔다. \"반박 인터뷰 찍어요! 제 채널에서 다 말해요. 사람들이 진짜 그쪽을 알아야 해요.\"\n서하린도 연락해 왔다. \"내일 우리 학교 운동회야. 아무도 기사 같은 거 안 읽는 데야. 와서 그냥 줄다리기나 해.\"",
                X("soi", "seo_harin", "소이와 인터뷰를 찍는다", "카메라 앞에서 처음으로 슬럼프 이야기를 했다. 소이가 질문하다가 울컥했다. 영상은 하루 만에 100만 회를 넘겼다.\n서하린이 보낸 운동회 사진에는 빈 의자 하나가 찍혀 있었다.", e => { e.Fans(40); e.Morale(4); }),
                X("seo_harin", "soi", "운동회에 간다", "청팀 줄다리기에서 아이들이랑 엉덩방아를 찧었다. 서하린이 배를 잡고 웃었다. 오랜만에 야구 생각을 하나도 안 했다.\n소이에게서는 '푹 쉬어요' 문자가 왔다. 끝에 이모티콘이 없었다.", e => { e.Morale(8); e.Fatigue(-10); }),
                new EventChoice { Label = "아무 데도 안 가고 훈련한다", Result = "기사에 대한 대답은 배트로 하기로 했다. 둘에게는 짧게 고맙다고만 했다.", Apply = e => e.Exp(Stat.Power, 30) }) },

            // ----- 연습생 신화 -----
            new Crossroad { Route = StoryRoute.Walkon, Event = S("her_x_walkon_1", "태풍이 오던 밤",
                "태풍 때문에 경기가 취소된 밤. 휴대폰이 두 번 울렸다.\n\n'셔터가 바람에 안 내려가요. 할머니는 허리 아프고... 혹시 근처예요?' 정가을.\n'도서관 지하 서고에 물이 새요. 옛날 신문이 다 젖게 생겼어요. 사람 손이 모자라요.' 윤세아.\n\n둘 다 구장에서 걸어서 10분. 방향은 반대다.",
                X("jung_gaeul", "yoon_sea", "국밥집으로 뛰어간다", "온몸으로 셔터를 붙잡고 내렸다. 정가을이 젖은 머리로 웃었다. 할머니가 새벽 두 시에 국밥을 끓여 줬다.\n다음 날 들은 이야기로는, 도서관 신문 몇 묶음은 결국 젖었다고 했다.", e => { e.Fatigue(10); e.Morale(5); }),
                X("yoon_sea", "jung_gaeul", "도서관으로 뛰어간다", "윤세아와 둘이서 새벽까지 신문 묶음을 위층으로 날랐다. 마지막 묶음에 내 첫 기사가 실린 2군 소식지가 있었다. 윤세아가 그걸 꼭 끌어안았다.\n국밥집 셔터는 옆집 아저씨가 내려 줬다고 했다.", e => { e.Fatigue(10); e.Morale(5); }),
                new EventChoice { Label = "숙소 창문을 테이프로 막는다", Result = "밤새 창문이 덜컹거렸다. 아침에 둘 다 무사하다는 문자를 받고서야 안심했다.", Apply = e => e.Fatigue(-5) }) },
            new Crossroad { Route = StoryRoute.Walkon, Event = S("her_x_walkon_2", "제일 먼저 전화할 사람",
                "\"내일부터 1군이다.\" 도봉구 감독의 한마디에 다리가 풀렸다.\n주차장에 혼자 앉아 휴대폰을 꺼냈다.\n\n연습생 시절 매일 밤 국밥을 내주던 가게. 비 오는 날마다 불을 켜 두던 도서관.\n이 소식을 제일 먼저 듣는 사람은 한 명이다.",
                X("jung_gaeul", "yoon_sea", "정가을에게 전화한다", "정가을이 비명을 질렀다. 수화기 너머로 할머니가 국자를 떨어뜨리는 소리가 들렸다.\n윤세아는 다음 날 뉴스로 소식을 들었다고 했다. '축하해요.' 문자는 짧고 단정했다.", e => e.Morale(6)),
                X("yoon_sea", "jung_gaeul", "윤세아에게 전화한다", "한참 조용하다가, 윤세아가 아주 작게 말했다. \"...그 책, 반납 안 해도 돼요. 1군 가서 읽어요.\"\n정가을은 할머니한테 듣고 알았다고 했다. '왜 나한테는 늦게 말해!' 웃는 이모티콘이 붙어 있었다.", e => e.Morale(6)),
                new EventChoice { Label = "도봉구 감독님께 먼저 감사 인사를 한다", Result = "\"나한테 왜 전화해. 가서 잘해.\" 무뚝뚝한 목소리가 조금 떨렸다.", Apply = e => e.Morale(4) }) },

            // ----- 아버지의 등번호 -----
            new Crossroad { Route = StoryRoute.Legacy, Event = S("her_x_legacy_1", "같은 시각, 다른 동네",
                "쉬는 날 오후 두 시.\n민채은이 아버지 야구 교실에서 아이들 원포인트 레슨을 같이 해 달라고 했다.\n한예린은 같은 시간, 라디오 특집 녹음에 꼭 내 목소리가 필요하다고 했다.\n\n야구 교실과 방송국은 도시 끝과 끝이다.",
                X("min_chaeeun", "han_yerin", "야구 교실로 간다", "아이들이 '10번 아들이다!' 하고 몰려들었다. 민채은이 웃으며 정정했다. \"아니야, 그냥 이 형 이름으로 불러.\" 해 질 때까지 공을 던졌다.\n특집 방송은 다른 선수의 목소리로 나갔다.", e => { e.Morale(5); e.Fans(20); }),
                X("han_yerin", "min_chaeeun", "방송국으로 간다", "녹음 부스에서 한예린이 질문지를 내밀었다. 아버지 이야기는 한 줄도 없었다. 처음으로 내 야구 이야기만 30분을 했다.\n민채은에게서 '애들이 기다렸는데' 문자가 왔다. 그래도 끝에 '다음엔 꼭'이 붙어 있었다.", e => e.Morale(5)),
                new EventChoice { Label = "아버지와 캐치볼을 한다", Result = "둘 다에게 미안하다고 하고 아버지와 공을 주고받았다. 아버지가 말했다. \"요즘 표정이 복잡하다, 너.\"", Apply = e => e.Morale(3) }) },
            new Crossroad { Route = StoryRoute.Legacy, Event = S("her_x_legacy_2", "10번의 날",
                "아버지의 영구결번 기념일. 매년 이날은 마음이 이상하다.\n\n민채은이 문자를 보냈다. '저녁에 그 운동장 가자. 우리 어릴 때 이름 새긴 펜스, 아직 있어.'\n한예린도 문자를 보냈다. '오늘 밤 생방송, 아드님 말고 당신 이름으로 5분 드릴게요. 하고 싶은 말 해요.'",
                X("min_chaeeun", "han_yerin", "어릴 적 운동장으로 간다", "펜스에 새긴 두 이름이 아직 그대로였다. 민채은이 손가락으로 글자를 따라 그었다. \"너 그때 10번 될 거라고 울었잖아.\" \"지금은 안 울어.\" \"알아.\"\n그날 밤 생방송 5분은 신청곡으로 채워졌다.", e => e.Morale(6)),
                X("han_yerin", "min_chaeeun", "생방송 스튜디오로 간다", "마이크 앞에서 처음으로 말했다. '저는 10번이 아니어도 괜찮습니다.' 부스 너머 한예린이 고개를 끄덕였다.\n민채은이 라디오를 들었다고 했다. '잘했네.' 한 줄이었다.", e => { e.Morale(6); e.Fans(30); }),
                new EventChoice { Label = "아버지 집에 간다", Result = "아버지와 말없이 옛날 경기 테이프를 봤다. 휴대폰은 꺼 두었다.", Apply = e => e.Morale(4) }) },
        };

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
                        C("han_sora", "\"이 수첩, 다 채우면 저도 보여 줘요\"", "\"...그럼 오래 걸릴 거예요. 은퇴할 때까지 쓸 거라서.\" 한소라의 귀가 빨개졌다.", 15),
                        C("han_sora", "\"고마워요. 계속 써 주세요\"", "한소라가 수첩을 가방에 넣으며 말했다. \"그럼 계속 잘하세요. 빈칸 생기면 섭섭할 거예요.\"", 5, e => e.Fans(30))),
                },
                Confess = "구장 뒤 주차장, 마감을 끝낸 한소라를 기다렸다.\n말을 꺼내자 한소라가 한참 아무 말도 하지 않았다.\n\n한소라가 말했다. \"기자가 선수랑 이러면 안 되는데... 그럼 당신 기사는 이제 다른 사람이 쓰라고 할게요.\"",
                Some = new[]
                {
                    S("her_han_sora_some1", "새벽 두 시의 메시지",
                        "새벽 두 시. 잠이 안 와서 휴대폰을 봤는데 한소라에게서 문자가 와 있었다.\n'오늘 타구 속도 구단 신기록이었던 거 알아요? 기사엔 못 썼어요. 그냥 말해 주고 싶어서.'\n1분 뒤에 한 줄이 더 왔다. '자요?'",
                        C("han_sora", "'아니요, 안 자요'라고 답한다", "새벽 네 시까지 문자를 주고받았다. 야구 이야기는 처음 10분뿐이었다.", 10),
                        C("han_sora", "'소라 씨도 얼른 자요'라고 답한다", "'네 ㅎㅎ' 그 두 글자에 한참 웃었다.", 5)),
                    S("her_han_sora_some2", "들켜 버린 자리",
                        "더그아웃에서 윤도현이 옆구리를 찔렀다.\n도현이 말했다. \"야, 1루 뒤 기자석 맨 앞자리. 저 기자님 요즘 우리 경기만 오시더라? 근데 너만 쳐다보시던데.\"",
                        C("han_sora", "\"일하시는 거잖아\" 하고 모른 척한다", "\"그래? 그럼 너 홈런 칠 때마다 저분이 왜 일어나서 박수 치냐?\" 도현이 낄낄댔다. 귀가 뜨거워졌다.", 10),
                        C("han_sora", "기자석을 향해 슬쩍 모자를 들어 본다", "한소라가 노트북 뒤로 얼굴을 숨겼다. 도현이 더그아웃 바닥에 드러누워 웃었다.", 10)),
                },
                DriftTitle = "1군 담당 기자",
                Drift = "한소라가 본사 야구부로 발령이 났다. 이제 리그 전체를 맡는다고 했다.\n한소라가 말했다. \"이제부터는 선수랑 기자로만 봐요. 그게 맞는 것 같아요.\" 그리고 늘 그렇듯 수첩을 덮으며 웃었다. \"그래도 기록은 계속 쓸 거예요.\"",
                HerFirst = "한소라가 먼저 걸음을 멈췄다.\n한소라가 말했다. \"수첩 마지막 장에 기사 아닌 걸 하나 썼어요.\" 수첩을 펼쳐 내밀었다. '이 선수가 좋다.'\n\"기자가 선수랑 이러면 안 되는데... 그래서 기사는 이제 다른 사람이 쓰라고 할게요.\"",
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
                        C("kang_yujin", "\"그럼 앞으로도 유진 씨한테만 받을게요\"", "강유진이 차트를 들여다보는 척했다. \"...그건 스케줄 봐서요.\" 귀 끝이 빨갰다.", 15),
                        C("kang_yujin", "\"믿고 뛸게요\"", "그날 경기 내내 발목이 가벼웠다. 더그아웃 뒤에서 강유진이 몰래 보고 있었다.", 5, e => e.Morale(3))),
                },
                Confess = "재활실, 마지막 환자가 나간 뒤였다.\n말을 다 듣고도 강유진은 차트에서 눈을 떼지 않았다.\n\n강유진이 말했다. \"...저 원래 선수랑 사적으로 안 만나요.\"\n그리고 차트를 내려놓았다. \"근데 예외로 할게요.\"",
                Some = new[]
                {
                    S("her_kang_yujin_some1", "재활실의 노래",
                        "재활실 문을 열었는데 낯익은 노래가 흘러나왔다. 내 타석 등장곡이었다.\n강유진이 말했다. \"...환자들이 신나는 노래 틀어 달래서요. 별 뜻 없어요.\"",
                        C("kang_yujin", "\"이 노래 좋죠?\" 하고 웃는다", "\"...보통이에요.\" 강유진이 볼륨을 한 칸 올렸다.", 10),
                        C("kang_yujin", "모른 척 스트레칭을 시작한다", "노래가 끝나자 같은 노래가 다시 나왔다. 반복 재생이었다.", 5)),
                    S("her_kang_yujin_some2", "기울어진 우산",
                        "퇴근길에 비가 쏟아졌다. 강유진이 말없이 우산을 펴서 같이 썼다.\n주차장까지 가는 동안 우산이 이상하게 내 쪽으로만 기울어 있었다. 그녀의 한쪽 어깨가 다 젖었다.",
                        C("kang_yujin", "우산을 그녀 쪽으로 다시 민다", "\"선수 어깨가 더 비싸요.\" 강유진이 우산을 다시 밀었다. 결국 둘 다 반씩 젖었다.", 10),
                        C("kang_yujin", "\"고마워요\"라고만 한다", "강유진이 아무렇지 않은 척 젖은 어깨를 털었다. \"감기 걸리면 제 탓이니까요.\"", 5)),
                },
                DriftTitle = "새로운 자리",
                Drift = "강유진이 국가대표팀 의무 트레이너로 뽑혔다. 다음 달부터 대표팀 훈련장으로 간다고 했다.\n강유진이 말했다. \"재활 일지는 후임한테 잘 넘겨 뒀어요. 아프면 아프다고 하는 거, 잊지 마요.\"",
                HerFirst = "강유진이 먼저 걸음을 멈췄다. 차트도, 테이프도 없는 빈손이었다.\n강유진이 말했다. \"저 원래 선수랑 사적으로 안 만나요. ...근데 요즘 자꾸 규칙을 어기게 돼요.\" 한숨 같은 웃음이 나왔다. \"책임지세요.\"",
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
                Some = new[]
                {
                    S("her_soi_some1", "혼자 보려고",
                        "소이에게서 영상 링크 하나가 왔다. 비공개 영상이었다.\n제목은 '혼자 보려고'. 내 경기 장면만 모은 3분짜리 편집본이었다. 마지막 장면은 퇴근길에 카메라를 보고 웃는 내 얼굴이었다.",
                        C("soi", "'이거 나도 혼자 볼게요'라고 답한다", "'헐 ㅠㅠㅠ' 그 뒤로 이모티콘이 스무 개 왔다.", 10),
                        C("soi", "'잘 만들었네요'라고 답한다", "'그쵸? 편집 사흘 걸렸어요.' 사흘이라는 말이 오래 남았다.", 5)),
                    S("her_soi_some2", "실시간 채팅",
                        "소이의 라이브를 보고 있었다. 채팅창에 질문이 올라왔다. '소이님 남친 있어요?'\n소이가 웃다가, 잠깐 화면 밖의 휴대폰을 봤다. 내가 보고 있다는 걸 아는 것처럼.\n소이가 말했다. \"음... 비밀이에요!\"",
                        C("soi", "채팅창에 'ㅋㅋ'를 친다", "소이가 내 닉네임을 보고 그대로 굳었다. 방송이 3초 멈췄다.", 10),
                        C("soi", "조용히 라이브를 끈다", "다음 날 소이가 물었다. \"어제 라이브 봤어요? ...아니 그냥요.\"", 5)),
                },
                DriftTitle = "소이의 여행",
                Drift = "소이가 해외 야구 투어 채널을 시작한다고 했다. 1년 동안 미국과 일본 구장을 돌아다닌다고.\n소이가 말했다. \"이제 제 채널은 제 힘으로 키워 볼게요. 맨날 그쪽 덕만 봤잖아요.\" 씩 웃는 얼굴이 조금 어른 같았다.",
                HerFirst = "소이가 먼저 카메라를 가방에 넣었다.\n소이가 말했다. \"카메라 껐어요. 이건 방송 아니에요.\" 숨을 크게 들이쉬었다. \"좋아해요. 구독자로서 말고요.\" 얼굴이 머리색만큼 빨개졌다.",
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
                Some = new[]
                {
                    S("her_seo_harin_some1", "선생님 남자친구예요?",
                        "학교에 들렀다가 서하린네 반 아이들에게 둘러싸였다.\n맨 앞의 꼬마가 큰 소리로 물었다. \"선생님 남자친구예요?\"\n서하린이 출석부를 든 채로 굳었다.",
                        C("seo_harin", "\"아직은 아니야\"라고 대답한다", "교실이 뒤집어졌다. '아직은'이래! 서하린이 출석부로 얼굴을 가렸다.", 10),
                        C("seo_harin", "\"선생님 친구야\"라고 대답한다", "\"에이~\" 아이들이 실망했다. 서하린이 아주 작게 웃었다.", 5)),
                    S("her_seo_harin_some2", "서랍 속 사진",
                        "서하린이 교무실 서랍을 정리하다 사진 한 장을 찾았다며 보냈다.\n열일곱 살의 우리 둘. 야구부 단체 사진 구석에서, 둘 다 서로를 보고 있었다.",
                        C("seo_harin", "'그때도 너만 보고 있었네'라고 답한다", "한참 답이 없다가, '...나도.' 한 줄이 왔다.", 10),
                        C("seo_harin", "'우리 진짜 어렸다'라고 답한다", "'응. 근데 지금도 별로 안 변했어.' 답장이 왔다.", 5)),
                },
                DriftTitle = "섬마을 학교",
                Drift = "서하린이 고향 섬의 작은 분교로 발령을 신청했다고 했다. 전교생이 열두 명이라고.\n서하린이 말했다. \"거기 애들한테도 야구 가르쳐 줄 거야. 네 경기 같이 볼게. 거기서도 다 보여.\"",
                HerFirst = "서하린이 먼저 스탠드 끝에 걸터앉았다.\n서하린이 말했다. \"고3 때 하려던 말이 있었는데, 결국 못 했어. 지금은 해도 돼?\" 대답도 하기 전에 이어서 말했다. \"좋아해. 그때부터 계속.\"",
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
                Some = new[]
                {
                    S("her_jung_gaeul_some1", "메뉴판의 새 이름",
                        "국밥집 계산대 옆에 정가을의 빵이 진열되기 시작했다.\n새로 생긴 빵 이름표에 '중견수 단팥빵'이라고 적혀 있었다.\n정가을이 말했다. \"아, 그거? 그냥 생각나서 붙인 거야. 별 뜻 없어.\"",
                        C("jung_gaeul", "그 자리에서 다섯 개를 산다", "\"이거 다 먹으면 배탈 나!\" 정가을이 말렸지만 다 먹었다. 다음 날부터 그 빵이 매일 한 개씩 따로 남겨져 있었다.", 10),
                        C("jung_gaeul", "\"이름 좋네\" 하고 웃는다", "정가을이 이름표를 괜히 반듯하게 고쳐 놓았다.", 5)),
                    S("her_jung_gaeul_some2", "할머니의 눈치",
                        "가게 문 닫을 시간. 할머니가 갑자기 허리를 두드렸다.\n할머니가 말했다. \"아이고 허리야. 나는 먼저 들어간다. 설거지는 둘이 해라.\" 그리고 너무 빠른 걸음으로 사라졌다.",
                        C("jung_gaeul", "둘이 나란히 설거지를 한다", "그릇 부딪히는 소리만 났다. 정가을이 내 팔꿈치를 툭 쳤다. \"할머니 허리 하나도 안 아파.\" \"알아.\"", 10),
                        C("jung_gaeul", "\"할머니 괜찮으신가?\" 하고 걱정한다", "정가을이 한숨을 쉬었다. \"...너 진짜 눈치 없다.\" 그래도 웃고 있었다.", 5)),
                },
                DriftTitle = "가을의 유학",
                Drift = "정가을이 일본 제과 학교에 합격했다. 2년 과정이라고 했다.\n정가을이 말했다. \"할머니 국밥 잘 부탁해. 연습생 때처럼 매일 와서 먹어 줘.\" 공항 가는 버스에서 손을 크게 흔들었다.",
                HerFirst = "셔터를 반쯤 내리다가 정가을이 손을 멈췄다.\n정가을이 말했다. \"할머니가 그러는데, 너 나 좋아한대.\" 대답을 기다리지도 않고 이어서 말했다. \"...근데 나도 그래. 할머니가 그건 몰라.\"\n셔터 너머에서 할머니가 크게 헛기침을 했다.",
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
                Some = new[]
                {
                    S("her_yoon_sea_some1", "책갈피",
                        "반납하려던 책 사이에 책갈피가 끼워져 있었다. 내가 넣은 게 아니었다.\n단정한 손글씨. '오늘도 잘 버텼어요. 이 페이지가 제일 좋아요.'\n그 페이지에는 '기다리는 사람이 있다는 건' 이라는 문장에 밑줄이 그어져 있었다.",
                        C("yoon_sea", "책갈피에 답을 적어 다시 끼워 둔다", "다음 날 반납대에서 윤세아가 책을 펼쳐 보고는 고개를 숙였다. 귀가 새빨갰다.", 10),
                        C("yoon_sea", "책갈피를 지갑에 넣어 둔다", "그 책갈피는 원정 내내 지갑 속에 있었다.", 5)),
                    S("her_yoon_sea_some2", "폐관 10분 전",
                        "원정에서 돌아와 늦게 도서관에 들렀다. 폐관 시간이 이미 지났는데 불이 켜져 있었다.\n윤세아가 열쇠를 든 채로 말했다. \"...오늘 오실 것 같아서요. 10분만 기다려 봤어요.\"",
                        C("yoon_sea", "\"매일 10분씩 기다려 줄 수 있어요?\"", "\"...10분이면 돼요?\" 윤세아가 아주 작게 웃었다. \"30분도 돼요.\"", 10),
                        C("yoon_sea", "\"늦어서 미안해요\"", "\"아니에요. 책은 기다려 줘요. ...저도요.\" 마지막 말은 거의 안 들렸다.", 5)),
                },
                DriftTitle = "새 도서관",
                Drift = "윤세아가 다른 도시의 큰 중앙도서관으로 옮긴다고 했다. 오래 꿈꾸던 자리라고.\n윤세아가 말했다. \"마지막으로 빌려 가실 책이에요. 반납은... 안 하셔도 돼요.\" 단정하게 웃었다.",
                HerFirst = "폐관 음악이 흐르는 도서관. 윤세아가 대출 카드 한 장을 내밀었다.\n카드 뒷면에 단정한 글씨로 적혀 있었다. '좋아해요. 반납 기한 없이.'\n윤세아가 말했다. \"...말로 하기엔 너무 떨려서요.\"",
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
                Some = new[]
                {
                    S("her_min_chaeeun_some1", "낡은 글러브",
                        "민채은의 글러브 손목 끈에 낯익은 매듭이 있었다. 열두 살 때 내가 묶어 준 매듭.\n민채은이 말했다. \"뭘 봐. 길들인 글러브 바꾸면 손이 어색하단 말이야.\"",
                        C("min_chaeeun", "\"그거 아직도 쓰는구나\" 하고 웃는다", "\"...국가대표 경기도 이걸로 뛰었어.\" 민채은이 글러브로 얼굴을 반쯤 가렸다.", 10),
                        C("min_chaeeun", "못 본 척 캐치볼을 시작한다", "공이 평소보다 세게 날아왔다.", 5)),
                    S("her_min_chaeeun_some2", "방송 인터뷰",
                        "TV에서 민채은의 인터뷰가 나왔다. 기자가 물었다. \"가장 친한 야구 선수는 누구예요?\"\n민채은이 말했다. \"아... 그건 비밀이에요.\" 화면 속 민채은의 얼굴이 빨개졌다. 휴대폰이 울렸다. '보지 마.'",
                        C("min_chaeeun", "'다 봤는데'라고 답한다", "'죽는다 진짜.' 그리고 1분 뒤, '...어땠어?'", 10),
                        C("min_chaeeun", "'못 봤어'라고 답한다", "'거짓말.' 그 뒤로 답장이 없다가, 다음 날 아침 '잘 잤냐' 한 줄이 왔다.", 5)),
                },
                DriftTitle = "해외 리그",
                Drift = "민채은이 해외 프로 소프트볼 리그와 계약했다. 다음 시즌부터 미국에서 뛴다고 했다.\n민채은이 말했다. \"너도 거기서 버티잖아. 나도 거기서 버텨 볼게.\" 주먹을 내밀었다. 어릴 때처럼 주먹을 맞댔다.",
                HerFirst = "민채은이 먼저 펜스에 기대어 섰다.\n민채은이 말했다. \"너는 맨날 등번호 얘기만 했잖아. 그래서 나도 맨날 그 얘기만 들어 줬고.\" 글러브를 벗었다. \"이제 내 얘기 해도 돼? 좋아해, 바보야. 아주 오래전부터.\"",
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
                Some = new[]
                {
                    S("her_han_yerin_some1", "마지막 곡",
                        "한예린의 심야 방송은 매일 다른 노래로 끝난다.\n그런데 이번 주 마지막 곡은 전부 내 타석 등장곡의 다른 버전이었다. 피아노, 기타, 어쿠스틱.",
                        C("han_yerin", "'이번 주 선곡 좋네요'라고 문자한다", "'...눈치챘어요?' 그다음 날 마지막 곡은 아카펠라 버전이었다.", 10),
                        C("han_yerin", "모른 척 다음 날 방송도 듣는다", "다음 날도, 그다음 날도 같은 노래였다.", 5)),
                    S("her_han_yerin_some2", "옥상 커피",
                        "방송국 옥상. 한예린이 커피 두 잔을 들고 왔다.\n말없이 헤드폰 한쪽을 내 귀에 걸어 줬다. 아직 방송 안 된 새 노래가 흘러나왔다.\n한예린이 말했다. \"제일 먼저 들려주고 싶어서요.\"",
                        C("han_yerin", "끝까지 같이 듣는다", "노래가 끝나도 둘 다 헤드폰을 빼지 않았다.", 10),
                        C("han_yerin", "\"좋은 노래네요\" 하고 커피를 마신다", "\"...그쵸.\" 한예린이 헤드폰을 조금 더 꼭 눌러 줬다.", 5)),
                },
                DriftTitle = "부산의 아침",
                Drift = "한예린이 부산 방송국 아침 프로그램 메인 PD가 됐다. 처음으로 자기 이름을 건 방송이라고.\n한예린이 말했다. \"오지영 선배가 그러더라고요. 등번호 말고 사람 이야기 하라고. 저도 이제 제 이야기 해 볼게요.\"",
                HerFirst = "한예린이 먼저 이어폰 한쪽을 내 귀에 꽂아 줬다.\n한예린이 말했다. \"오늘 마지막 곡, 제 신청곡이에요.\" 노래가 시작되기 전에 덧붙였다. \"제목이 제 대답이에요. ...아니, 그냥 말할게요. 좋아해요.\"",
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
