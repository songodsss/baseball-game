using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>The four people who come back through the whole career.</summary>
    public enum Bond { Coach, Roommate, Sibling, Reporter }

    public enum StoryEnding { None, Legend, OneClub, BaseballSchool }

    /// <summary>How far the main story has gone in this career, and how close the player is to each person in it.</summary>
    public class MainStoryState
    {
        public const int StartBond = 50;
        /// <summary>Scene ids answered, in order.</summary>
        public List<string> Seen = new List<string>();
        public Dictionary<Bond, int> Bonds = new Dictionary<Bond, int>();
        /// <summary>Choices that later scenes and the ending remember, e.g. "visited".</summary>
        public List<string> Flags = new List<string>();
        /// <summary>"1장 · 데뷔 — 첫 기록: 시작일 뿐입니다", one line per scene answered.</summary>
        public List<string> Log = new List<string>();
        /// <summary>When the last scene was answered; -1 = none yet.</summary>
        public int LastSeason = -1;
        public int LastDay;

        public int BondOf(Bond b) => Bonds.TryGetValue(b, out var v) ? v : StartBond;
        public bool Has(string flag) => Flags.Contains(flag);
    }

    public partial class Career
    {
        public MainStoryState Story = new MainStoryState();
    }

    /// <summary>
    /// The main story: a growth story from the last pick of the draft to retirement, told in fixed scenes
    /// that each come once a career, in order, on the day their moment arrives. A scene uses the story
    /// event card, so it shows like any other event; on its day it takes the place of the random one.
    /// Every chapter also has an age or season by which it starts anyway, so no career gets stuck.
    /// </summary>
    public static class MainStory
    {
        public const string IdPrefix = "main_";

        public static readonly string[] ChapterNames =
        {
            "프롤로그", "1장 · 데뷔", "2장 · 첫 번째 벽", "3장 · 이름이 불리는 자리", "4장 · 정점", "5장 · 베테랑", "6장 · 마지막 시즌",
        };

        public static string BondName(Bond b)
        {
            switch (b)
            {
                case Bond.Coach: return "최강훈";
                case Bond.Roommate: return "윤도현";
                case Bond.Sibling: return "하윤";
                default: return "한소라";
            }
        }

        public static string BondRole(Bond b)
        {
            switch (b)
            {
                case Bond.Coach: return "나를 뽑은 코치";
                case Bond.Roommate: return "동기 · 룸메이트";
                case Bond.Sibling: return "동생";
                default: return "스포츠 데일리 기자";
            }
        }

        public static readonly Bond[] AllBonds = { Bond.Coach, Bond.Roommate, Bond.Sibling, Bond.Reporter };

        class Scene
        {
            public string Id;
            public int Chapter;
            public string Title;
            public Func<Career, string> Text;
            public Func<Career, DayResult, bool> Ready;
            public Func<Career, List<Option>> Options;
        }

        class Option
        {
            public string Label;
            public string Result;
            public Action<EventEffects, Career> Apply;
        }

        static readonly List<Scene> Scenes = Build();

        public static int SceneCount => Scenes.Count;
        public static IEnumerable<string> SceneIds => Scenes.Select(s => s.Id);

        /// <summary>The chapter the career is in: that of the last scene answered (0 before any).</summary>
        public static int Chapter(Career c)
        {
            var last = c.Story.Seen.LastOrDefault();
            return last == null ? 0 : Scenes.First(s => s.Id == last).Chapter;
        }

        public static string ChapterName(Career c) => ChapterNames[Chapter(c)];

        public static bool IsMain(StoryEvent ev) => ev != null && ev.Id.StartsWith(IdPrefix);

        /// <summary>The next scene if its moment has come today, as an event card; null otherwise.</summary>
        public static StoryEvent Next(Career c, DayResult day)
        {
            if (c.InMilitary || c.Story.Seen.Count >= Scenes.Count) return null;
            var scene = Scenes[c.Story.Seen.Count];
            if (c.Story.LastSeason >= 0 && DaysSinceLast(c) < MinGap(c)) return null;
            if (!scene.Ready(c, day)) return null;
            return ToEvent(c, scene);
        }

        /// <summary>The card for a scene by id, whatever the day; for tests and previews.</summary>
        public static StoryEvent Preview(Career c, string id) => ToEvent(c, Scenes.First(s => s.Id == id));

        static StoryEvent ToEvent(Career c, Scene scene)
        {
            string title = $"{ChapterNames[scene.Chapter]} · {scene.Title}";
            var ev = new StoryEvent { Id = IdPrefix + scene.Id, Title = title, Text = Fill(scene.Text(c), c), Weight = (_, __) => 0 };
            foreach (var o in scene.Options(c))
            {
                var option = o;
                string label = Fill(option.Label, c);
                ev.Choices.Add(new EventChoice
                {
                    Label = label,
                    Result = Fill(option.Result, c),
                    Apply = e =>
                    {
                        option.Apply(e, c);
                        if (!c.Story.Seen.Contains(scene.Id)) c.Story.Seen.Add(scene.Id);
                        c.Story.Log.Add($"{title} — {label}");
                        c.Story.LastSeason = c.Season;
                        c.Story.LastDay = c.Day;
                    },
                });
            }
            return ev;
        }

        /// <summary>At least three days of a full season between scenes (one day in shorter seasons).</summary>
        static int MinGap(Career c) => Math.Max(1, 3 * c.League.GamesPerSeason / League.FullSeason);

        static int DaysSinceLast(Career c) =>
            c.Story.LastSeason < 0 ? int.MaxValue : (c.Season - c.Story.LastSeason) * c.League.GamesPerSeason + c.Day - c.Story.LastDay;

        static double SeasonPart(Career c) => (double)c.Day / c.League.GamesPerSeason;

        static int Games(Career c) => c.League.GamesPerSeason;

        // ---------- Effects ----------

        static void ChangeBond(EventEffects e, Career c, Bond b, int delta)
        {
            int before = c.Story.BondOf(b);
            int after = Math.Max(0, Math.Min(100, before + delta));
            c.Story.Bonds[b] = after;
            if (after != before) e.Outcome.Changes.Add($"{BondName(b)} 유대 {(after > before ? "+" : "")}{after - before}");
        }

        static void Flag(Career c, string flag)
        {
            if (!c.Story.Has(flag)) c.Story.Flags.Add(flag);
        }

        /// <summary>Exp from someone's lesson counts for more the closer the player is to them: x0.75 to x1.25.</summary>
        static int Scaled(Career c, Bond b, int exp) => (int)Math.Round(exp * (0.75 + c.Story.BondOf(b) / 200.0));

        static Option O(string label, string result, Action<EventEffects, Career> apply) => new Option { Label = label, Result = result, Apply = apply };

        // ---------- Text ----------

        /// <summary>Fills {name} {team} {rival} {rivalTeam} {coach} {first} {spot} {mound}.</summary>
        public static string Fill(string text, Career c)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var me = c.Me;
            return text
                .Replace("{name}", me.Name)
                .Replace("{team}", c.MyTeam.Name)
                .Replace("{rivalTeam}", c.RivalTeam?.Name ?? "다른 팀")
                .Replace("{rival}", c.Rival?.Name ?? "그 녀석")
                .Replace("{coach}", c.Story.Has("manager") ? "최강훈 감독" : "최강훈 코치")
                .Replace("{first}", FirstRecordName(me))
                .Replace("{spot}", SpotName(me))
                .Replace("{mound}", me.IsPitcher ? "마운드" : "타석");
        }

        static string FirstRecordName(CareerPlayer me)
        {
            if (!me.IsPitcher) return "첫 안타";
            switch (me.Role)
            {
                case PitcherRole.Closer: return "첫 세이브";
                case PitcherRole.Setup: return "첫 홀드";
                default: return "첫 승";
            }
        }

        static string SpotName(CareerPlayer me)
        {
            if (!me.IsPitcher) return "4번 타자";
            switch (me.Role)
            {
                case PitcherRole.Closer: return "마무리";
                case PitcherRole.Setup: return "필승조";
                default: return "에이스";
            }
        }

        // ---------- Triggers ----------

        static bool HasFirstRecord(Career c)
        {
            var me = c.Me;
            if (!me.IsPitcher) return c.SeasonLine.Hits + me.History.Sum(h => h.Line?.Hits ?? 0) > 0;
            int Count(PitcherLine l) => l == null ? 0 : me.Role == PitcherRole.Closer ? l.Saves : me.Role == PitcherRole.Setup ? l.Holds : l.Wins;
            return Count(c.SeasonPitching) + me.History.Sum(h => Count(h.Pitching)) > 0;
        }

        static bool InASlump(Career c) => c.InFarm || c.HitlessStreak >= 4;

        static bool PlayedWell(DayResult d) =>
            d.MyLine != null && (d.MyLine.HomeRuns > 0 || d.MyLine.Hits >= 2)
            || d.MyPitching != null && !d.InFarm && (d.MyPitching.Wins + d.MyPitching.Saves + d.MyPitching.Holds > 0);

        static bool IsCore(Career c)
        {
            var me = c.Me;
            double average = me.MyStats.Average(s => me.Ratings[s]);
            return !c.InFarm && (me.IsPitcher ? average >= 65 : c.LineupSlot == 3 || average >= 68);
        }

        static bool HasSummit(Career c) => c.Me.GoldMedals > 0 || c.Me.History.Any(h => h.Titles.Count > 0);

        // ---------- Scenes ----------

        static List<Scene> Build() => new List<Scene>
        {
            // ----- Prologue -----
            new Scene
            {
                Id = "draft", Chapter = 0, Title = "마지막 이름",
                Ready = (c, d) => true,
                Text = c => "드래프트 날, 마지막 순번.\n다들 자리를 뜨던 그때, {team}의 테이블에서 이름이 불렸다.\n\n\"{name}.\"\n\n" +
                            "구단 버스 앞에서 한 사내가 손을 내밀었다. 2군 코치 최강훈.\n\"너 뽑자고 우긴 사람이 나다. 후회하게 만들지 마라.\"",
                Options = c => new List<Option>
                {
                    O("\"반드시 증명하겠습니다\"", "\"눈빛은 합격이다.\" 최강훈이 처음으로 웃었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Coach, 10); }),
                    O("\"왜 저를 뽑으셨어요?\"", "\"고3 여름, 비 오는 날 혼자 남아서 배트 돌리던 놈. 그게 너였다.\"",
                        (e, k) => { e.Exp(Stat.Contact, 30); ChangeBond(e, k, Bond.Coach, 5); }),
                },
            },
            new Scene
            {
                Id = "roommate", Chapter = 0, Title = "룸메이트",
                Ready = (c, d) => true,
                Text = c => "숙소 첫날 밤. 짐을 푸는데 문이 벌컥 열렸다.\n\"네가 {name}? 나 윤도현. 1라운드.\"\n\n동갑내기 룸메이트가 씩 웃으며 손을 내밀었다.\n" +
                            "\"마지막 순번이랑 1라운드라. 누가 먼저 1군 붙박이 되나 내기할래?\"",
                Options = c => new List<Option>
                {
                    O("내기를 받는다", "\"진 사람이 1년 내내 빨래 담당이다.\" 둘은 새벽까지 떠들었다.",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Roommate, 5); Flag(k, "bet"); }),
                    O("\"같이 올라가자\"", "도현이 잠깐 멈칫하더니 웃었다. \"...그래. 둘 다 올라가면 되지.\"",
                        (e, k) => ChangeBond(e, k, Bond.Roommate, 10)),
                    O("피곤하다며 먼저 잔다", "도현은 머쓱하게 불을 껐다.",
                        (e, k) => { e.Fatigue(-10); ChangeBond(e, k, Bond.Roommate, -5); }),
                },
            },

            // ----- 1. Debut -----
            new Scene
            {
                Id = "first", Chapter = 1, Title = "첫 기록",
                Ready = (c, d) => HasFirstRecord(c) || c.Season > 1 || SeasonPart(c) >= 0.4,
                Text = c => (HasFirstRecord(c)
                                ? "마침내 {first}.\n더그아웃에 돌아오자 선배들이 헬멧을 마구 두드렸다.\n\n"
                                : "아직 기록이 없다. {first}까지가 이렇게 멀 줄이야.\n그래도 매일 경기장에 나간다.\n\n") +
                            "경기 후, 수첩을 든 기자가 다가왔다.\n\"스포츠 데일리 한소라입니다. 저도 이번이 첫 취재예요. 한마디만 부탁드려요!\"",
                Options = c => new List<Option>
                {
                    O("\"시작일 뿐입니다\"", "다음 날 신문 귀퉁이에 짧은 기사가 실렸다. '마지막 순번의 당찬 첫걸음'.",
                        (e, k) => { e.Morale(3); e.Fans(20); ChangeBond(e, k, Bond.Reporter, 10); }),
                    O("\"코치님 덕분입니다\"", "기사를 본 최강훈은 쓸데없는 소리라며 신문을 접었다. 그런데 그 신문이 코치실 벽에 붙었다.",
                        (e, k) => { ChangeBond(e, k, Bond.Coach, 10); ChangeBond(e, k, Bond.Reporter, 5); }),
                    O("얼어붙는다", "한소라가 웃었다. \"괜찮아요, 저도 떨려요. 다음엔 길게 해 주세요.\"",
                        (e, k) => { e.Morale(1); ChangeBond(e, k, Bond.Reporter, 5); }),
                },
            },
            new Scene
            {
                Id = "letter", Chapter = 1, Title = "동생의 그림",
                Ready = (c, d) => c.Season > 1 || SeasonPart(c) >= 0.6,
                Text = c => "집에서 소포가 왔다. 크레파스로 그린 그림 한 장.\n{team} 유니폼을 입은 사람과, 그 옆에 작은 글러브를 낀 아이.\n\n" +
                            "'나도 리틀야구 들어갔어! 나중에 같이 뛰자! — 하윤'\n\n아홉 살 동생 하윤의 글씨다.",
                Options = c => new List<Option>
                {
                    O("영상 통화를 건다", "화면 너머로 하윤이 새 글러브를 자랑했다. 통화를 끊고도 한참 웃음이 났다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Sibling, 10); }),
                    O("그림을 사물함에 붙인다", "경기 전마다 그림을 보는 게 습관이 됐다.",
                        (e, k) => { e.Morale(3); e.Exp(Stat.Eye, 20); ChangeBond(e, k, Bond.Sibling, 5); }),
                    O("답장 대신 훈련한다", "'지금은 보여 줄 게 없다.' 배트를 들었다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Sibling, -5); }),
                },
            },
            new Scene
            {
                Id = "rival", Chapter = 1, Title = "같은 해, 다른 유니폼",
                Ready = (c, d) => c.Season > 1 || SeasonPart(c) >= 0.8,
                Text = c => "스포츠 데일리 1면, '올해의 신인 경쟁'.\n사진 속에는 나와 {rivalTeam}의 {rival}.\n같은 해 프로에 온 동갑내기. 1라운드 1순위였다고 한다.\n\n" +
                            "한소라의 기사 마지막 줄.\n'두 사람의 이야기는 이제 막 시작됐다.'",
                Options = c => new List<Option>
                {
                    O("{rival}, 기억해 둔다", "그날 밤, 상대 기록을 처음으로 찾아봤다.",
                        (e, k) => { e.Exp(Stat.Power, 30); e.Morale(3); Flag(k, "rival_fire"); }),
                    O("신문을 접는다", "남과 비교하지 않기로 했다. 지금은 내 야구가 먼저다.",
                        (e, k) => e.Morale(4)),
                    O("도현에게 보여 준다", "\"야, 이 사람이 네 라이벌이래. 그럼 나는?\" 도현이 투덜거렸다.",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Roommate, 5); }),
                },
            },

            // ----- 2. The first wall -----
            new Scene
            {
                Id = "wall", Chapter = 2, Title = "첫 번째 벽",
                Ready = (c, d) => InASlump(c) || c.Season >= 2 && SeasonPart(c) >= 0.5 || c.Season >= 3,
                Text = c => c.InFarm
                    ? "2군 강등 통보.\n짐을 싸서 2군 구장에 도착하니, 낡은 펑고 배트를 든 사람이 기다리고 있었다.\n\n\"왔냐.\" 최강훈이었다.\n\"내일 새벽 5시. 늦으면 그냥 집에 가라.\""
                    : "몇 경기째 아무것도 되지 않는다.\n{mound}에 서는 게 무서워지기 시작했다.\n\n경기 후 주차장. 2군에 있어야 할 최강훈이 차에 기대 서 있었다.\n\"얼굴 보러 왔다. 내일 새벽 5시, 2군 구장. 올래?\"",
                Options = c => new List<Option>
                {
                    O("새벽 5시에 간다", "첫날은 공 하나 제대로 못 쳤다. 사흘째, 최강훈이 처음으로 고개를 끄덕였다.",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Coach, 70)); e.Fatigue(20); ChangeBond(e, k, Bond.Coach, 15); Flag(k, "dawn"); }),
                    O("혼자 해 보겠다고 한다", "\"...그래. 그것도 답이다.\" 최강훈은 말없이 돌아섰다.",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(3); ChangeBond(e, k, Bond.Coach, -5); }),
                },
            },
            new Scene
            {
                Id = "rooftop", Chapter = 2, Title = "옥상의 캔커피",
                Ready = (c, d) => true,
                Text = c => "숙소 옥상. 도현이 캔커피 두 개를 들고 올라왔다.\n\"너 요즘 얼굴 장난 아니다.\"\n\n한참 말이 없다가, 도현이 툭 던졌다.\n" +
                            "\"나도 1라운드라는 거 때문에 매일 무서워. 너만 그런 거 아니야.\"",
                Options = c => new List<Option>
                {
                    O("속마음을 털어놓는다", "밤새 이야기했다. 어깨가 조금 가벼워졌다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Roommate, 15); }),
                    O("\"괜찮아\"라고만 한다", "도현은 더 묻지 않았다. 캔커피가 미지근해졌다.",
                        (e, k) => e.Morale(2)),
                    O("\"같이 야간 훈련 가자\"", "둘이서 불 꺼진 실내 연습장에 불을 켰다.",
                        (e, k) => { e.Exp(Stat.Power, 40); e.Fatigue(15); ChangeBond(e, k, Bond.Roommate, 10); }),
                },
            },
            new Scene
            {
                Id = "breakthrough", Chapter = 2, Title = "벽을 넘는 날",
                Ready = (c, d) => !c.InFarm && (PlayedWell(d) || DaysSinceLast(c) >= Games(c) / 3),
                Text = c => "오늘은 뭔가 달랐다.\n{mound}에서 몸이 먼저 움직였다. 머리가 아니라 몸이 기억하고 있었다.\n\n" +
                            (c.Story.Has("dawn") ? "새벽 5시의 2군 구장이 떠올랐다.\n" : "") +
                            "관중석 맨 위, 모자를 눌러 쓴 최강훈이 보였다.\n그는 엄지를 한 번 들어 보이고 자리를 떴다.",
                Options = c => new List<Option>
                {
                    O("벽은 넘으라고 있는 거다", "어딘가에서 한소라가 이 장면을 수첩에 적고 있었다.",
                        (e, k) => { e.Morale(6); e.Fans(20); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Reporter, 5); }),
                    O("코치님께 전화한다", "\"한 경기 잘했다고 전화하냐.\" 목소리가 웃고 있었다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Coach, 10); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new Scene
            {
                Id = "spot", Chapter = 3, Title = "이름이 불리는 자리",
                Ready = (c, d) => c.Season >= 2 && IsCore(c) || c.Me.Age >= 24,
                Text = c => "구단 발표. 새 감독은 최강훈.\n취임 첫날, 감독실로 불려 갔다.\n\n\"올해부터 네가 우리 팀 {spot}다.\"\n" +
                            (c.Story.Has("dawn") ? "\"새벽 5시 기억하지? 주장 완장도 같이 받아라.\"\n" : "") +
                            "\"마지막 순번으로 들어온 놈이 그 자리에 서는 거. 그거 보려고 여기까지 왔다.\"",
                Options = c => new List<Option>
                {
                    O("\"맡겨 주십시오\"", "그날 밤, 사물함 이름표 위에 새 글씨가 붙었다.",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Coach, 10); }),
                    O("\"부담됩니다\"", "\"부담 없는 자리는 없다. 그러니까 너다.\"",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(2); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Coach, 5); }),
                },
            },
            new Scene
            {
                Id = "injury", Chapter = 3, Title = "도현의 부상",
                Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4,
                Text = c => (c.Me.IsPitcher ? "홈 플레이트에서 주자와 부딪힌 도현이 일어나지 못했다.\n" : "수비하던 도현이 그대로 쓰러졌다.\n") +
                            "들것이 들어오고, 경기장이 조용해졌다.\n\n진단은 무릎 인대 파열. 시즌 아웃, 어쩌면 그 이상.\n" +
                            "병실에서 도현이 천장만 보며 말했다.\n\"...야. 나 이제 끝난 걸까.\"",
                Options = c => new List<Option>
                {
                    O("매일 병문안을 간다", "훈련이 끝나면 매일 병원에 갔다. 도현이 조금씩 다시 웃기 시작했다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Roommate, 20); Flag(k, "visited"); }),
                    O("\"네 몫까지 뛴다\"", "그날 경기, 모자 안쪽에 도현의 등번호를 적었다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Roommate, 5); }),
                    O("아무 말도 못 한다", "말 대신 어깨를 짚었다. 도현이 눈을 감았다.",
                        (e, k) => ChangeBond(e, k, Bond.Roommate, 5)),
                },
            },
            new Scene
            {
                Id = "race", Chapter = 3, Title = "타이틀 레이스",
                Ready = (c, d) => SeasonPart(c) >= 0.5 && DaysSinceLast(c) >= Games(c) / 6,
                Text = c => "시즌 후반. 개인 타이틀 경쟁이 뜨겁다.\n그 한가운데에 나와 {rivalTeam}의 {rival}.\n\n" +
                            "경기 전 그라운드에서 {rival} 선수와 눈이 마주쳤다.\n\"이번엔 내가 이긴다.\" 입 모양만 봐도 알 수 있었다.",
                Options = c => new List<Option>
                {
                    O("\"덤벼\"", "그날 경기, 공 하나하나에 온 힘을 실었다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); e.Morale(4); }),
                    O("\"좋은 경기 하자\"", "{rival} 선수가 잠깐 놀라더니 모자를 살짝 들어 올렸다.",
                        (e, k) => { e.Morale(5); e.Fans(30); Flag(k, "rival_respect"); }),
                },
            },

            // ----- 4. The summit -----
            new Scene
            {
                Id = "summit", Chapter = 4, Title = "정상",
                Ready = (c, d) => HasSummit(c) || c.Me.Age >= 28,
                Text = c => (HasSummit(c)
                                ? "시상식. 무대 위에서 내려다본 객석에 아는 얼굴들이 보였다.\n최강훈, 지팡이를 짚은 도현, 그리고 부쩍 자란 하윤.\n\n"
                                : "스물여덟. 아직 트로피는 없다.\n그래도 이제 리그에서 내 이름을 모르는 사람은 없다.\n\n") +
                            "한소라가 이번엔 표지 인터뷰를 요청했다.\n\"마지막 순번에서 여기까지. 무슨 말을 하고 싶으세요?\"",
                Options = c => new List<Option>
                {
                    O("\"아직 멀었습니다\"", "표지 제목은 '아직 배고프다'였다.",
                        (e, k) => { e.Morale(5); e.Fans(60); ChangeBond(e, k, Bond.Reporter, 10); }),
                    O("고마운 사람들 이름을 부른다", "기사에는 네 사람의 이름이 실렸다.",
                        (e, k) =>
                        {
                            e.Fans(40);
                            ChangeBond(e, k, Bond.Coach, 5);
                            ChangeBond(e, k, Bond.Roommate, 5);
                            ChangeBond(e, k, Bond.Sibling, 5);
                            ChangeBond(e, k, Bond.Reporter, 10);
                        }),
                },
            },
            new Scene
            {
                Id = "sibling_draft", Chapter = 4, Title = "하윤의 지명",
                Ready = (c, d) => c.Me.Age >= 27 && DaysSinceLast(c) >= Games(c) / 4,
                Text = c => "올해 드래프트. 온 가족이 TV 앞에 모였다.\n그리고 3라운드에서, 그 이름이 불렸다.\n\n\"하윤.\"\n\n" +
                            "열여덟 살 동생이 화면 속에서 울고 있었다.\n곧 전화가 울렸다. \"나 이제 진짜 같은 리그야! 봐주는 거 없다?\"",
                Options = c => new List<Option>
                {
                    O("\"봐주는 거 없다\"", "그날 밤, 둘은 처음으로 선수 대 선수로 이야기했다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Sibling, 10); }),
                    O("글러브를 선물한다 (200만원)", "어릴 적 크레파스 그림 속 그 글러브처럼.",
                        (e, k) => { e.Money(-200); ChangeBond(e, k, Bond.Sibling, 15); }),
                    O("프로는 다르다고 충고한다", "하윤이 잠깐 조용해졌다. \"...알아. 그러니까 지켜봐.\"",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Sibling, -5); }),
                },
            },

            // ----- 5. Veteran -----
            new Scene
            {
                Id = "body", Chapter = 5, Title = "몸이 먼저 안다",
                Ready = (c, d) => c.Me.Age >= 32,
                Text = c => "아침에 일어나는 데 시간이 걸린다.\n같은 훈련인데 회복이 하루 더 걸린다.\n\n트레이너가 조심스럽게 말했다.\n\"이제는 덜 하는 게 더 하는 거예요.\"",
                Options = c => new List<Option>
                {
                    O("요령을 익힌다", "힘 대신 경험으로 싸우는 법을 배우기 시작했다.",
                        (e, k) => { e.Exp(Stat.Eye, 60); e.Fatigue(-20); }),
                    O("나이는 숫자다, 더 한다", "새벽 러닝을 늘렸다. 몸이 비명을 질렀지만 버텼다.",
                        (e, k) =>
                        {
                            e.Exp(Stat.Power, 60);
                            e.Fatigue(25);
                            if (e.MaybeInjure(0.15, 3, InjuryKind.Hamstring)) e.Say("...결국 허벅지가 탈이 났다.");
                        }),
                    O("{coach}을 찾아간다", "\"나도 서른둘에 그랬다. 그때부터가 진짜 야구다.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Coach, 10); }),
                },
            },
            new Scene
            {
                Id = "dohyeon", Chapter = 5, Title = "도현의 선택",
                Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4,
                Text = c => c.Story.Has("visited")
                    ? "도현에게서 문자가 왔다.\n'재활 끝. 다음 주 1군 복귀. 너 병원 매일 왔던 거, 그거 갚으러 간다.'\n\n복귀전, 도현이 그라운드에 들어서자 관중이 모두 일어섰다."
                    : "도현의 은퇴식. 끝내 무릎은 예전으로 돌아오지 않았다.\n\n마이크를 잡은 도현이 관중석이 아니라 나를 봤다.\n" +
                      "\"제 몫까지 뛰어 준 친구가 있어서, 후회 없습니다.\"\n\n도현은 구단 트레이너로 남는다고 한다.",
                Options = c => c.Story.Has("visited")
                    ? new List<Option>
                    {
                        O("달려가 끌어안는다", "둘 다 유니폼이 흙투성이가 됐다. 상관없었다.",
                            (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Roommate, 10); Flag(k, "dohyeon_back"); }),
                        O("\"늦었잖아\"", "\"주인공은 원래 늦게 나오는 거야.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Roommate, 5); Flag(k, "dohyeon_back"); }),
                    }
                    : new List<Option>
                    {
                        O("꽃다발을 건넨다", "도현이 꽃다발로 내 머리를 툭 쳤다. \"울지 마, 바보야.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Roommate, 10); }),
                        O("\"네 몫은 아직 안 끝났어\"", "다음 날부터 도현이 짠 훈련표를 받았다.",
                            (e, k) => { e.Morale(8); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Roommate, 5); }),
                    },
            },
            new Scene
            {
                Id = "sibling_match", Chapter = 5, Title = "하윤과의 맞대결",
                Ready = (c, d) => SeasonPart(c) >= 0.5 && DaysSinceLast(c) >= Games(c) / 6,
                Text = c => (c.Me.IsPitcher ? "9회, 타석에 하윤이 들어섰다.\n" : "오늘 상대 마운드에 하윤이 서 있었다.\n") +
                            "어릴 적 마당에서 공을 던져 주던 기억이 스쳤다.\n\n하윤이 모자를 고쳐 쓰며 고개를 끄덕였다. 진짜 승부를 하자는 뜻이다.",
                Options = c => new List<Option>
                {
                    O("전력으로 상대한다", "결과보다 하윤의 웃는 얼굴이 더 오래 기억에 남았다.",
                        (e, k) => { e.Morale(5); e.Fans(30); ChangeBond(e, k, Bond.Sibling, 10); }),
                    O("경기 후 같이 밥을 먹는다", "부모님께 보낼 사진을 찍었다. 둘 다 유니폼 차림으로.",
                        (e, k) => { e.Fatigue(-10); ChangeBond(e, k, Bond.Sibling, 10); }),
                },
            },

            // ----- 6. The last season -----
            new Scene
            {
                Id = "last", Chapter = 6, Title = "마지막 대화",
                Ready = (c, d) => c.Me.Age >= 37,
                Text = c => "서른일곱의 봄, 스프링캠프 마지막 날.\n{coach}이 불렀다. 자기도 올해가 마지막 시즌이라고 했다.\n\n" +
                            "\"마지막 순번으로 너 뽑던 날 말이다. 사람들이 다 웃었다.\"\n\"지금은 아무도 안 웃지.\"",
                Options = c => new List<Option>
                {
                    O("\"끝까지 달리겠습니다\"", "\"그래야 내가 뽑은 놈이지.\"",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Coach, 5); }),
                    O("고개 숙여 인사한다", "최강훈이 등을 돌렸다. 어깨가 조금 떨리고 있었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Coach, 15); }),
                },
            },
        };

        // ---------- Endings ----------

        /// <summary>The ending a career earns: the hall of fame, else a one-club career of ten seasons or more, else the baseball school.</summary>
        public static StoryEnding EndingFor(Career c, RetiredPlayer retired)
        {
            if (retired.HallOfFame) return StoryEnding.Legend;
            if (c.Me.Clubs.Count == 0 && retired.Seasons >= 10) return StoryEnding.OneClub;
            return StoryEnding.BaseballSchool;
        }

        public static string EndingTitle(StoryEnding ending)
        {
            switch (ending)
            {
                case StoryEnding.Legend: return "엔딩 · 전설";
                case StoryEnding.OneClub: return "엔딩 · 원클럽맨";
                case StoryEnding.BaseballSchool: return "엔딩 · 작은 야구 교실";
                default: return "";
            }
        }

        /// <summary>The ending told as 한소라's last article, with a line for each person depending on how close they ended up.</summary>
        public static List<string> EndingLines(Career c, RetiredPlayer retired)
        {
            var ending = retired.Ending != StoryEnding.None ? retired.Ending : EndingFor(c, retired);
            var s = c.Story;
            var lines = new List<string>();
            string club = retired.MainClub ?? c.MyTeam.Name;
            switch (ending)
            {
                case StoryEnding.Legend:
                    lines.Add("[스포츠 데일리 · 한소라] '마지막 순번, 명예의 전당에 서다'");
                    lines.Add($"헌액식 연단에 선 {retired.Name}. 준비한 원고는 접어 두고 한 사람씩 이름을 불렀다.");
                    break;
                case StoryEnding.OneClub:
                    lines.Add($"[스포츠 데일리 · 한소라] '{club}의 심장, 그라운드를 떠나다'");
                    lines.Add($"{retired.Seasons}시즌 동안 {club} 유니폼만 입었다. 마지막 경기, 외야석이 카드섹션으로 등번호를 그렸다.");
                    break;
                default:
                    lines.Add("[스포츠 데일리 · 한소라] '마지막 순번의 두 번째 출발'");
                    lines.Add("은퇴 1년 뒤, 고향 동네에 작은 유소년 야구 교실이 문을 열었다.");
                    break;
            }

            int coach = s.BondOf(Bond.Coach), mate = s.BondOf(Bond.Roommate), sib = s.BondOf(Bond.Sibling), rep = s.BondOf(Bond.Reporter);
            lines.Add(coach >= 70 ? "백발이 된 최강훈이 맨 앞자리에서 모자를 벗었다. \"내 눈은 틀리지 않았다.\""
                : coach <= 30 ? "최강훈은 오지 않았다. 다음 날 짧은 문자 한 통이 왔다. '잘했다.'"
                : "최강훈은 뒤쪽 자리에서 조용히 박수를 쳤다.");
            lines.Add(mate >= 70
                ? (s.Has("bet") ? "도현이 소리쳤다. \"빨래 내기는 내가 졌다! 인정!\"" : "도현이 누구보다 크게 박수를 쳤다. 눈가가 붉었다.")
                : mate <= 30 ? "도현에게서는 꽃다발만 도착했다. 카드에는 '축하한다, 친구'."
                : "도현이 손을 흔들었다. \"수고했다, 룸메이트.\"");
            lines.Add(sib >= 70 ? "프로 선수가 된 하윤이 유니폼 차림으로 꽃을 들고 올라왔다. 크레파스 그림 속 그 아이였다."
                : sib <= 30 ? "하윤은 경기 일정 때문에 영상 메시지를 보냈다. \"그래도 내 첫 번째 영웅이야.\""
                : "하윤이 객석에서 휴대폰으로 사진을 찍고 있었다.");
            if (c.Rival != null)
                lines.Add(s.Has("rival_respect")
                    ? $"{c.Rival.Name} 선수가 보낸 화환도 있었다. '덕분에 나도 여기까지 왔다.'"
                    : $"{c.Rival.Name} 선수의 이름이 적힌 화환도 있었다. 끝까지 지기 싫은 상대였다.");
            lines.Add(rep >= 70 ? "한소라는 첫 인터뷰 때의 수첩을 꺼내 보였다. 첫 장에 그날의 한마디가 적혀 있었다."
                : "한소라의 기사는 짧았지만 정확했다.");

            switch (ending)
            {
                case StoryEnding.Legend: lines.Add("마지막 순번으로 시작한 이야기는, 리그의 전설로 끝났다."); break;
                case StoryEnding.OneClub: lines.Add("한 팀에서 시작해 한 팀에서 끝난 이야기. 팬들은 오래도록 그 이름을 응원가로 불렀다."); break;
                default: lines.Add($"화려한 기록은 없어도, {retired.Name} 선수를 보고 야구를 시작한 아이들이 있다. 그거면 충분했다."); break;
            }
            return lines;
        }
    }
}
