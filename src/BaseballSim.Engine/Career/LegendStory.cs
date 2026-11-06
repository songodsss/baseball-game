using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public partial class Career
    {
        /// <summary>How far each legend's story has gone in this career, by legend id.</summary>
        public Dictionary<string, LegendStage> LegendStages = new Dictionary<string, LegendStage>();
        /// <summary>When the last legend came by; -1 = not yet.</summary>
        public int LegendLastSeason = -1;
        public int LegendLastDay;
        /// <summary>Set once 장태산 and 오세찬 have come together.</summary>
        public bool LegendDuoSeen;

        public LegendStage StageOf(string legendId) => LegendStages.TryGetValue(legendId, out var s) ? s : LegendStage.None;
        public IEnumerable<string> SurpassedLegends => LegendStages.Where(kv => kv.Value == LegendStage.Surpassed).Select(kv => kv.Key);
    }

    /// <summary>
    /// The legends' visits. Each legend comes three times in a career, at moments set by the player's
    /// growth: when the player's rating in their speciality reaches 50, when the player is halfway to
    /// their record, and the day the record falls (right after that game; an MVP ceremony before the
    /// next season). Visits come on days without a main story scene, at most one every quarter season,
    /// and never as a chore: just a card with a choice.
    /// </summary>
    public static class LegendStory
    {
        public const string IdPrefix = "legend_";
        public const int MeetRating = 50;
        const string DuoId = "legend_jang_oh";

        public static StoryEvent Next(Career c, DayResult day)
        {
            var me = c.Me;
            if (c.InMilitary) return null;
            if (c.LegendLastSeason >= 0 && DaysSinceLast(c) < Math.Max(1, c.League.GamesPerSeason / 4)) return null;
            var mine = Legends.All.Where(l => l.Visits(me)).ToList();

            if (!c.LegendDuoSeen && c.StageOf("jang") == LegendStage.Surpassed && c.StageOf("oh") == LegendStage.Surpassed)
                return Duo(c);
            foreach (var l in mine)
                if (!l.AtSeasonStart && c.StageOf(l.Id) == LegendStage.Met && l.Chases(c) && l.Halfway(c) && SeasonsSinceLast(c) >= 1)
                    return Return(c, l);
            foreach (var l in mine)
                if (c.StageOf(l.Id) == LegendStage.None && me.Ratings.TryGetValue(l.Speciality, out var r) && r >= MeetRating && !me.IsInjured)
                    return Meet(c, l);
            return null;
        }

        /// <summary>
        /// The day a record falls: right after that game, before any other scene and whatever the gap
        /// since the last visit. Ceremony-type legends wait for the season start instead.
        /// </summary>
        public static StoryEvent SurpassToday(Career c, DayResult day)
        {
            if (c.InMilitary || day == null) return null;
            foreach (var l in Legends.All.Where(l => l.Visits(c.Me) && !l.AtSeasonStart))
                if (Surpassable(c, l) && (l.SurpassWhen?.Invoke(c, day) ?? Played(day)))
                    return Surpass(c, l);
            return null;
        }

        /// <summary>Before a season's first game: a ceremony-type legend's visit (오세찬 after a ring).</summary>
        public static StoryEvent AtSeasonStart(Career c)
        {
            if (c.InMilitary || c.Day != 0) return null;
            foreach (var l in Legends.All.Where(l => l.Visits(c.Me) && l.AtSeasonStart))
            {
                if (Surpassable(c, l)) return Surpass(c, l);
                if (c.StageOf(l.Id) == LegendStage.Met && l.Chases(c) && l.Halfway(c)) return Return(c, l);
            }
            return null;
        }

        static bool Surpassable(Career c, Legend l) =>
            c.StageOf(l.Id) >= LegendStage.Met && c.StageOf(l.Id) < LegendStage.Surpassed && l.Chases(c) && l.Progress(c) >= 1;

        static bool Played(DayResult d) => d.MyLine != null && d.MyLine.PlateAppearances > 0 || d.MyPitching != null;

        static int DaysSinceLast(Career c) => (c.Season - c.LegendLastSeason) * c.League.GamesPerSeason + c.Day - c.LegendLastDay;
        static int SeasonsSinceLast(Career c) => c.LegendLastSeason < 0 ? 99 : c.Season - c.LegendLastSeason;

        static void Mark(Career c, Legend l, LegendStage stage)
        {
            if (c.StageOf(l.Id) < stage) c.LegendStages[l.Id] = stage;
            if (!c.Me.MetLegends.Contains(l.Id)) c.Me.MetLegends.Add(l.Id);
            c.LegendLastSeason = c.Season;
            c.LegendLastDay = c.Day;
        }

        static string Goal(Career c, Legend l) => l.Chases(c) ? $"\n\n기록 도전 · {l.Goal(c)}" : "";

        static StoryEvent Card(Career c, string id, string title, string text, params (string Label, string Result, Action<EventEffects> Apply)[] choices)
        {
            var ev = new StoryEvent { Id = id, Title = title, Text = MainStory.Fill(text, c), Weight = (_, __) => 0 };
            foreach (var (label, result, apply) in choices)
                ev.Choices.Add(new EventChoice { Label = MainStory.Fill(label, c), Result = MainStory.Fill(result, c), Apply = apply });
            return ev;
        }

        static StoryEvent Meet(Career c, Legend l) => Card(c, IdPrefix + l.Id, "레전드의 방문",
            $"'{l.Nickname}' {l.Name}({l.Years}).\n{l.Record}의 전설이 오늘 훈련장을 찾았다.\n\"{l.Line}\"\n\n" +
            $"{{name}}가 굳은 채로 인사하자 {l.Name}{Josa.After(l.Name, "이")} 손사래를 쳤다.\n\"{l.Record}? 그거 다 옛날 얘기다. 기록은 깨라고 있는 거야.\"\n그러더니 내 눈을 똑바로 봤다. \"너라면 할 수 있을 것 같아서 왔다.\"" + Goal(c, l),
            ($"{Stats.Name(l.Speciality)} 비법을 배운다", l.Lesson, e => { Mark(c, l, LegendStage.Met); e.Exp(l.Speciality, 80); e.Boost(l.Speciality, 30, 10, "레전드 " + l.Name); e.Fatigue(10); }),
            ("옛날 이야기를 듣는다", l.Story + "\n\n이야기 사이사이에 몸에 밴 습관 몇 가지가 들어 있었다. 잊기 전에 노트에 적었다.", e => { Mark(c, l, LegendStage.Met); e.Exp(l.Speciality, 60); e.Boost(l.Speciality, 20, 15, "레전드 " + l.Name + "의 이야기"); e.Morale(4); }));

        static StoryEvent Return(Career c, Legend l) => Card(c, IdPrefix + l.Id + "_2", "레전드의 재방문",
            $"'{l.Nickname}' {l.Name}\n\n" + l.ReturnText + Goal(c, l),
            (l.ReturnLabelA, l.ReturnResultA, e => { Mark(c, l, LegendStage.Returned); e.Exp(l.Speciality, 120); e.Boost(l.Speciality, 40, 15, "레전드 " + l.Name); e.Fatigue(10); }),
            (l.ReturnLabelB, l.ReturnResultB, e => { Mark(c, l, LegendStage.Returned); e.Exp(l.Speciality, 80); e.Boost(l.Speciality, 25, 20, "레전드와의 약속"); e.Fans(20); }));

        static StoryEvent Surpass(Career c, Legend l) => Card(c, IdPrefix + l.Id + "_3", "레전드를 넘어서",
            $"'{l.Nickname}' {l.Name} · {l.Record}\n\n" + l.SurpassText,
            (l.SurpassLabel, l.SurpassResult, e =>
            {
                // The biggest moment of a career pays like one: the legend's own touch stays for good.
                Mark(c, l, LegendStage.Surpassed);
                e.Raise(l.Speciality, SurpassPoints);
                e.Boost(l.Speciality, 50, 30, "레전드 계승");
                e.Money(SurpassBonus);
                e.Fans(150);
                e.Morale(10);
            }));

        /// <summary>Breaking a record: the speciality goes up for good, plus a club bonus (만원).</summary>
        public const int SurpassPoints = 5, SurpassBonus = 5000;

        static StoryEvent Duo(Career c) => Card(c, DuoId, "두 전설",
            "장태산과 오세찬이 함께 훈련장에 나타났다. 둘이 같이 온 건 처음이다.\n\n" +
            "\"이 덩치 때문에 MVP 2위만 세 번 했다.\" 오세찬이 투덜거렸다.\n\"그래서 미안하다고 밥 산다잖아. 20년째.\" 장태산이 껄껄 웃었다.\n\n" +
            "둘이 동시에 나를 봤다. \"근데 우리 둘 기록을 다 넘은 놈이 나올 줄은 몰랐지.\"",
            ("셋이서 밥을 먹는다", "두 사람은 밤늦도록 1990년대 이야기로 티격태격했다. 계산은 결국 장태산이 했다.",
                e => { c.LegendDuoSeen = true; c.LegendLastSeason = c.Season; c.LegendLastDay = c.Day; e.Morale(10); e.Fans(50); }),
            ("둘에게 동시에 배운다", "장태산은 스윙을, 오세찬은 스텝을 가르쳤다. 서로 자기 말이 맞다고 우겼다.",
                e => { c.LegendDuoSeen = true; c.LegendLastSeason = c.Season; c.LegendLastDay = c.Day; e.Exp(Stat.Power, 80); e.Exp(Stat.Defense, 80); }));

        /// <summary>Lines for the retirement article: the legends whose record fell come; the ones the player got close to send a word.</summary>
        public static List<string> EndingLines(Career c)
        {
            var lines = new List<string>();
            foreach (var l in Legends.All)
            {
                var stage = c.StageOf(l.Id);
                if (stage == LegendStage.Surpassed) lines.Add(l.Ceremony);
                else if (stage == LegendStage.Returned) lines.Add(l.NearMiss);
            }
            return lines;
        }
    }
}
