using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>The four people who come back through a whole story; who they are depends on the story.</summary>
    public enum Bond { Mentor, Friend, Family, Voice }

    public enum StoryEnding { None, Legend, OneClub, BaseballSchool }

    /// <summary>The main stories. Each new player gets one the club hasn't seen much of yet.</summary>
    public enum StoryRoute { LastPick, Prodigy, Walkon }

    /// <summary>How far the main story has gone in this career, and how close the player is to each person in it.</summary>
    public class MainStoryState
    {
        public const int StartBond = 50;
        public StoryRoute Route;
        /// <summary>Scene slots answered, in order.</summary>
        public List<string> Seen = new List<string>();
        public Dictionary<Bond, int> Bonds = new Dictionary<Bond, int>();
        /// <summary>Choices that later scenes and the ending remember, e.g. "visited".</summary>
        public List<string> Flags = new List<string>();
        /// <summary>"1장 · 데뷔 · 첫 기록 — 시작일 뿐입니다", one line per scene answered.</summary>
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

    /// <summary>One scene of a story: what it says and the choices. When it comes is set by its slot.</summary>
    internal class StoryScene
    {
        public string Slot;
        public string Title;
        public Func<Career, string> Text;
        public Func<Career, List<StoryOption>> Options;
    }

    internal class StoryOption
    {
        public string Label;
        public string Result;
        public Action<EventEffects, Career> Apply;
    }

    /// <summary>A whole story: its people, a scene for every slot, and how it tells each ending.</summary>
    internal class StoryRouteDef
    {
        public StoryRoute Id;
        public string Name;
        public string Blurb;
        public string[] BondNames;
        public string[] BondRoles;
        /// <summary>What {mentor} reads as before and after the "manager" flag (set in chapter 3).</summary>
        public string MentorBefore, MentorAfter;
        public List<StoryScene> Scenes;
        /// <summary>The headline and first line of the ending article.</summary>
        public Func<Career, RetiredPlayer, StoryEnding, string[]> Opening;
        /// <summary>A line for one person, by how close they ended up (0-100).</summary>
        public Func<Career, Bond, int, string> BondLine;
        public Func<Career, RetiredPlayer, StoryEnding, string> Closing;
    }

    /// <summary>
    /// The main story: a growth story from the first day to retirement, told in fixed scenes that each
    /// come once a career, in order, on the day their moment arrives. Every story has the same slots
    /// (first hit or win, the first wall, the big spot, the summit, decline, the last season); only
    /// the people and the words differ. A scene uses the story event card and takes the place of the
    /// day's random event. Every chapter also has an age or season by which it starts anyway.
    /// </summary>
    public static class MainStory
    {
        public const string IdPrefix = "main_";

        public static readonly string[] ChapterNames =
        {
            "프롤로그", "1장 · 데뷔", "2장 · 첫 번째 벽", "3장 · 이름이 불리는 자리", "4장 · 정점", "5장 · 베테랑", "6장 · 마지막 시즌",
        };

        public static readonly Bond[] AllBonds = { Bond.Mentor, Bond.Friend, Bond.Family, Bond.Voice };
        public static readonly StoryRoute[] AllRoutes = { StoryRoute.LastPick, StoryRoute.Prodigy, StoryRoute.Walkon };

        class Slot
        {
            public string Id;
            public int Chapter;
            public Func<Career, DayResult, bool> Ready;
        }

        /// <summary>The moments every story is told through, in order.</summary>
        static readonly List<Slot> Slots = new List<Slot>
        {
            new Slot { Id = "draft", Chapter = 0, Ready = (c, d) => true },
            new Slot { Id = "roommate", Chapter = 0, Ready = (c, d) => true },
            new Slot { Id = "first", Chapter = 1, Ready = (c, d) => HasFirstRecord(c) || c.Season > 1 || SeasonPart(c) >= 0.4 },
            new Slot { Id = "letter", Chapter = 1, Ready = (c, d) => c.Season > 1 || SeasonPart(c) >= 0.6 },
            new Slot { Id = "rival", Chapter = 1, Ready = (c, d) => c.Season > 1 || SeasonPart(c) >= 0.8 },
            new Slot { Id = "wall", Chapter = 2, Ready = (c, d) => InASlump(c) || c.Season >= 2 && SeasonPart(c) >= 0.5 || c.Season >= 3 },
            new Slot { Id = "rooftop", Chapter = 2, Ready = (c, d) => true },
            new Slot { Id = "breakthrough", Chapter = 2, Ready = (c, d) => !c.InFarm && (PlayedWell(d) || DaysSinceLast(c) >= Games(c) / 3) },
            new Slot { Id = "spot", Chapter = 3, Ready = (c, d) => c.Season >= 2 && IsCore(c) || c.Me.Age >= 24 },
            new Slot { Id = "injury", Chapter = 3, Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "race", Chapter = 3, Ready = (c, d) => SeasonPart(c) >= 0.5 && DaysSinceLast(c) >= Games(c) / 6 },
            new Slot { Id = "summit", Chapter = 4, Ready = (c, d) => HasSummit(c) || c.Me.Age >= 28 },
            new Slot { Id = "family", Chapter = 4, Ready = (c, d) => c.Me.Age >= 27 && DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "body", Chapter = 5, Ready = (c, d) => c.Me.Age >= 32 },
            new Slot { Id = "friend", Chapter = 5, Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "family_game", Chapter = 5, Ready = (c, d) => SeasonPart(c) >= 0.5 && DaysSinceLast(c) >= Games(c) / 6 },
            new Slot { Id = "last", Chapter = 6, Ready = (c, d) => c.Me.Age >= 37 },
        };

        static readonly List<StoryRouteDef> Routes = new List<StoryRouteDef> { LastPickStory.Build(), ProdigyStory.Build(), WalkonStory.Build() };

        static StoryRouteDef RouteOf(Career c) => Routes.First(r => r.Id == c.Story.Route);

        public static int SceneCount => Slots.Count;
        public static IEnumerable<string> SceneIds => Slots.Select(s => s.Id);

        public static string RouteName(StoryRoute route) => Routes.First(r => r.Id == route).Name;
        public static string RouteBlurb(StoryRoute route) => Routes.First(r => r.Id == route).Blurb;
        public static string BondName(Career c, Bond b) => RouteOf(c).BondNames[(int)b];
        public static string BondRole(Career c, Bond b) => RouteOf(c).BondRoles[(int)b];

        /// <summary>The story for a new player: the one this club has played least, earliest first on a tie.</summary>
        public static StoryRoute NextRoute(Club club) =>
            AllRoutes.OrderBy(r => club.Roster.Count(p => p.Route == r && p.Ending != StoryEnding.None)).First();

        /// <summary>The chapter the career is in: that of the last scene answered (0 before any).</summary>
        public static int Chapter(Career c)
        {
            var last = c.Story.Seen.LastOrDefault();
            return last == null ? 0 : Slots.First(s => s.Id == last).Chapter;
        }

        public static string ChapterName(Career c) => ChapterNames[Chapter(c)];

        public static bool IsMain(StoryEvent ev) => ev != null && ev.Id.StartsWith(IdPrefix);

        /// <summary>The next scene if its moment has come today, as an event card; null otherwise.</summary>
        public static StoryEvent Next(Career c, DayResult day)
        {
            if (c.InMilitary || c.Story.Seen.Count >= Slots.Count) return null;
            var slot = Slots[c.Story.Seen.Count];
            if (c.Story.LastSeason >= 0 && DaysSinceLast(c) < MinGap(c)) return null;
            if (!slot.Ready(c, day)) return null;
            return ToEvent(c, slot.Id);
        }

        /// <summary>The card for a slot of this career's story, whatever the day; for tests and previews.</summary>
        public static StoryEvent Preview(Career c, string slot) => ToEvent(c, slot);

        static StoryEvent ToEvent(Career c, string slotId)
        {
            var slot = Slots.First(s => s.Id == slotId);
            var scene = RouteOf(c).Scenes.First(s => s.Slot == slotId);
            string title = $"{ChapterNames[slot.Chapter]} · {Fill(scene.Title, c)}";
            var ev = new StoryEvent { Id = IdPrefix + slotId, Title = title, Text = Fill(scene.Text(c), c), Weight = (_, __) => 0 };
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
                        if (!c.Story.Seen.Contains(slotId)) c.Story.Seen.Add(slotId);
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

        // ---------- For the stories ----------

        internal static void ChangeBond(EventEffects e, Career c, Bond b, int delta)
        {
            int before = c.Story.BondOf(b);
            int after = Math.Max(0, Math.Min(100, before + delta));
            c.Story.Bonds[b] = after;
            if (after != before) e.Outcome.Changes.Add($"{BondName(c, b)} 유대 {(after > before ? "+" : "")}{after - before}");
        }

        internal static void Flag(Career c, string flag)
        {
            if (!c.Story.Has(flag)) c.Story.Flags.Add(flag);
        }

        /// <summary>Exp from someone's lesson counts for more the closer the player is to them: x0.75 to x1.25.</summary>
        internal static int Scaled(Career c, Bond b, int exp) => (int)Math.Round(exp * (0.75 + c.Story.BondOf(b) / 200.0));

        internal static StoryOption O(string label, string result, Action<EventEffects, Career> apply) => new StoryOption { Label = label, Result = result, Apply = apply };

        internal static bool HasFirstRecord(Career c)
        {
            var me = c.Me;
            if (!me.IsPitcher) return c.SeasonLine.Hits + me.History.Sum(h => h.Line?.Hits ?? 0) > 0;
            int Count(PitcherLine l) => l == null ? 0 : me.Role == PitcherRole.Closer ? l.Saves : me.Role == PitcherRole.Setup ? l.Holds : l.Wins;
            return Count(c.SeasonPitching) + me.History.Sum(h => Count(h.Pitching)) > 0;
        }

        internal static bool HasSummit(Career c) => c.Me.GoldMedals > 0 || c.Me.History.Any(h => h.Titles.Count > 0);

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

        // ---------- Text ----------

        /// <summary>Fills {name} {team} {rival} {rivalTeam} {mentor} {first} {spot} {mound}.</summary>
        public static string Fill(string text, Career c)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var me = c.Me;
            var route = RouteOf(c);
            return text
                .Replace("{name}", me.Name)
                .Replace("{team}", c.MyTeam.Name)
                .Replace("{rivalTeam}", c.RivalTeam?.Name ?? "다른 팀")
                .Replace("{rival}", c.Rival?.Name ?? "그 녀석")
                .Replace("{mentor}", c.Story.Has("manager") ? route.MentorAfter : route.MentorBefore)
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

        /// <summary>The ending told as an article, with a line for each person depending on how close they ended up.</summary>
        public static List<string> EndingLines(Career c, RetiredPlayer retired)
        {
            var ending = retired.Ending != StoryEnding.None ? retired.Ending : EndingFor(c, retired);
            var route = RouteOf(c);
            var lines = new List<string>(route.Opening(c, retired, ending));
            foreach (var b in AllBonds) lines.Add(route.BondLine(c, b, c.Story.BondOf(b)));
            if (c.Rival != null)
                lines.Add(c.Story.Has("rival_respect")
                    ? $"{c.Rival.Name} 선수가 보낸 화환도 있었다. '덕분에 나도 여기까지 왔다.'"
                    : $"{c.Rival.Name} 선수의 이름이 적힌 화환도 있었다. 끝까지 지기 싫은 상대였다.");
            lines.Add(route.Closing(c, retired, ending));
            return lines.Select(l => Fill(l, c)).ToList();
        }
    }
}
