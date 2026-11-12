using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>The four people who come back through a whole story; who they are depends on the story.</summary>
    public enum Bond { Mentor, Friend, Family, Voice }

    /// <summary>Saved as a number: new endings go at the end.</summary>
    public enum StoryEnding { None, Legend, OneClub, BaseballSchool, Broadcaster, Hometown, Scout, Partner }

    /// <summary>The main stories. Each new player gets one the club hasn't seen much of yet.</summary>
    public enum StoryRoute { LastPick, Prodigy, Walkon, Legacy }

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
        /// <summary>Picks which version of each scene this career gets; 0 until first needed.</summary>
        public int Seed;

        public int BondOf(Bond b) => Bonds.TryGetValue(b, out var v) ? v : StartBond;
        public bool Has(string flag) => Flags.Contains(flag);
    }

    public partial class Career
    {
        public MainStoryState Story = new MainStoryState();

        const string FinalPrefix = "final:";

        /// <summary>The season the player said would be the last, or -1 (kept as a story flag).</summary>
        public int DeclaredLastSeason
        {
            get
            {
                var flag = Story.Flags.FirstOrDefault(f => f.StartsWith(FinalPrefix));
                return flag != null && int.TryParse(flag.Substring(FinalPrefix.Length), out var s) ? s : -1;
            }
        }

        /// <summary>Between seasons from 30 on: the coming season can be named the last one, so the story's last chapter is told in it.</summary>
        public bool CanDeclareLastSeason => Day == 0 && Me.Age >= 30 && DeclaredLastSeason < 0 && !MustRetire && !InMilitary;

        /// <summary>Makes the coming season the last: the final chapter plays out in it and the career ends after it.</summary>
        public bool DeclareLastSeason()
        {
            if (!CanDeclareLastSeason) return false;
            Story.Flags.Add(FinalPrefix + Season);
            return true;
        }

        /// <summary>This season is the career's last: the one named so, or the one before the retirement age.</summary>
        public bool IsFinalSeason => Me.Age >= RetirementAge - 1 || DeclaredLastSeason == Season;

        bool DeclaredSeasonPlayed => DeclaredLastSeason >= 0 && Season > DeclaredLastSeason;
    }

    /// <summary>One scene of a story: what it says and the choices. When it comes is set by its slot.</summary>
    internal class StoryScene
    {
        public string Slot;
        public string Title;
        public Func<Career, string> Text;
        public Func<Career, List<StoryOption>> Options;
        /// <summary>Played before the season's first game (a camp, a ceremony), never in the middle of it.</summary>
        public bool AtSeasonStart;
        /// <summary>A moment this version needs today on top of its slot's (a home game, a bad night...).</summary>
        public Func<Career, DayResult, bool> When;
        /// <summary>This story's own telling of the slot: the shared versions are left out (a story's own climax).</summary>
        public bool Only;
        /// <summary>Told only when this holds (a bond gone cold or very close); when it does, it comes before the other versions.</summary>
        public Func<Career, bool> Needs;
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
        /// <summary>How the story's own thread ended, from the choices made along the way (may be empty).</summary>
        public Func<Career, List<string>> Epilogue;
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
        public static readonly StoryRoute[] AllRoutes = { StoryRoute.LastPick, StoryRoute.Prodigy, StoryRoute.Walkon, StoryRoute.Legacy };

        class Slot
        {
            public string Id;
            public int Chapter;
            /// <summary>Its moment, checked after each day; false for scenes told only before a season.</summary>
            public Func<Career, DayResult, bool> Ready = (c, d) => false;
            /// <summary>Told before the season's first game when this is true (null = never then).</summary>
            public Func<Career, bool> Pre;
            /// <summary>Its moment has gone by for good: the scene is skipped so the story never tells it late.</summary>
            public Func<Career, DayResult, bool> Expired = (c, d) => false;
            /// <summary>Comes on its day even right after another scene.</summary>
            public bool NoGap;
        }

        /// <summary>The moments every story is told through, in order.</summary>
        static readonly List<Slot> Slots = new List<Slot>
        {
            // Prologue: before the first game of the first season.
            new Slot { Id = "draft", Chapter = 0, Pre = c => c.Season == 1, Expired = (c, d) => !(c.Season == 1 && c.Day == 0) },
            new Slot { Id = "roommate", Chapter = 0, Pre = c => c.Season == 1, Expired = (c, d) => !(c.Season == 1 && c.Day == 0) },
            new Slot { Id = "camp", Chapter = 0, Pre = c => c.Season == 1, Expired = (c, d) => !(c.Season == 1 && c.Day == 0) },
            // 1. Debut: the first season.
            new Slot
            {
                Id = "first", Chapter = 1, NoGap = true,
                // The day of the first hit (win, save, hold); or, with none by mid-season, the wait itself.
                Ready = (c, d) => FirstRecordToday(c, d) || !HasFirstRecord(c) && (c.Season > 1 || SeasonPart(c) >= 0.4),
                Expired = (c, d) => HasFirstRecord(c) && !FirstRecordToday(c, d),
            },
            new Slot
            {
                Id = "first_pay", Chapter = 1, NoGap = true,
                Ready = (c, d) => c.Season == 1 && d?.Payday != null && c.PaidMonths == 1,
                Expired = (c, d) => c.Season > 1 || c.PaidMonths > 1 || c.PaidMonths == 1 && d?.Payday == null,
            },
            new Slot { Id = "letter", Chapter = 1, Ready = (c, d) => c.Season > 1 || SeasonPart(c) >= 0.6 },
            new Slot { Id = "rival", Chapter = 1, Ready = (c, d) => c.Season == 1 && SeasonPart(c) >= 0.8, Expired = (c, d) => c.Season > 1 },
            new Slot { Id = "first_winter", Chapter = 1, Pre = c => c.Season == 2, Expired = (c, d) => c.Season > 2 || c.Season == 2 && c.Day > 0 },
            // 2. The first wall: years two to four.
            new Slot { Id = "sophomore", Chapter = 2, Ready = (c, d) => c.Season == 2 && SeasonPart(c) >= 0.1, Expired = (c, d) => c.Season > 2 },
            new Slot
            {
                Id = "wall", Chapter = 2,
                // Sent down today, or a run of bad games.
                Ready = (c, d) => DemotedToday(d) || !c.InFarm && (InASlump(c) || BadGame(c, d) && (c.Season >= 3 || c.Season >= 2 && SeasonPart(c) >= 0.5)),
            },
            new Slot { Id = "rooftop", Chapter = 2, Ready = (c, d) => true },
            // The story's own thread: a hint of what the mentor has kept quiet.
            new Slot { Id = "arc_hint", Chapter = 2, Ready = (c, d) => c.Season >= 2 && DaysSinceLast(c) >= Games(c) / 6 },
            new Slot { Id = "mentor_past", Chapter = 2, Ready = (c, d) => c.Season >= 3 },
            new Slot { Id = "breakthrough", Chapter = 2, Ready = (c, d) => !c.InFarm && PlayedWell(d) },
            // 3. The spot that calls your name: about 22 to 26.
            new Slot { Id = "pressure", Chapter = 3, Ready = (c, d) => c.Me.Age >= 22 || c.Season >= 4 && IsCore(c) },
            new Slot { Id = "spot", Chapter = 3, Pre = c => c.Season >= 2 && IsCore(c) || c.Me.Age >= 24 },
            // The thread turns: the secret comes out.
            new Slot { Id = "arc_reveal", Chapter = 3, Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "injury", Chapter = 3, Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "contract", Chapter = 3, Ready = (c, d) => c.Me.Age >= 25 },
            new Slot { Id = "kin_visit", Chapter = 3, Ready = (c, d) => c.Me.Age >= 25 && DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "race", Chapter = 3, Ready = (c, d) => SeasonPart(c) >= 0.8 && DaysSinceLast(c) >= Games(c) / 6 },
            // 4. The summit: about 27 to 30.
            new Slot
            {
                Id = "summit", Chapter = 4,
                // The awards come in the winter: told before the next season. A gold medal, the day it's won.
                Pre = c => WonTitleLastSeason(c),
                Ready = (c, d) => d?.Tournament?.Medal == NationalTeam.Gold && d.Tournament.Selected
                    || c.Me.Age >= 28 && (!HasSummit(c) || DaysSinceLast(c) >= Games(c) / 3),
            },
            // The thread's crisis, at the top of the career.
            new Slot { Id = "arc_crisis", Chapter = 4, Ready = (c, d) => c.Me.Age >= 27 && SeasonPart(c) >= 0.3 && DaysSinceLast(c) >= Games(c) / 6 },
            new Slot { Id = "bigstage", Chapter = 4, Ready = (c, d) => SeasonPart(c) >= 0.8 && Played(d) },
            new Slot { Id = "family", Chapter = 4, Ready = (c, d) => c.Me.Age >= 27 && DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "voice_feature", Chapter = 4, Ready = (c, d) => c.Me.Age >= 29 },
            new Slot { Id = "rival_peak", Chapter = 4, Ready = (c, d) => c.Me.Age >= 30 && SeasonPart(c) >= 0.4 },
            // 5. Veteran: about 31 to 36.
            new Slot { Id = "body", Chapter = 5, Ready = (c, d) => c.Me.Age >= 32 },
            new Slot { Id = "role_change", Chapter = 5, Ready = (c, d) => c.Me.Age >= 33 },
            // The thread's last big choice, handing something on.
            new Slot { Id = "arc_choice", Chapter = 5, Ready = (c, d) => c.Me.Age >= 33 && DaysSinceLast(c) >= Games(c) / 4 },
            new Slot { Id = "friend", Chapter = 5, Ready = (c, d) => DaysSinceLast(c) >= Games(c) / 4 && Played(d) },
            new Slot { Id = "mentor_old", Chapter = 5, Ready = (c, d) => c.Me.Age >= 34 },
            new Slot { Id = "milestone", Chapter = 5, Ready = (c, d) => c.Me.Age >= 35 && DaysSinceLast(c) >= Games(c) / 4 && Played(d) },
            new Slot { Id = "rival_end", Chapter = 5, Ready = (c, d) => c.Me.Age >= 36 },
            new Slot { Id = "family_game", Chapter = 5, Ready = (c, d) => c.Me.Age >= 36 && SeasonPart(c) >= 0.5 && DaysSinceLast(c) >= Games(c) / 6 && Played(d) },
            // 6. The last season.
            new Slot { Id = "last", Chapter = 6, Pre = c => c.IsFinalSeason },
            new Slot { Id = "farewell_tour", Chapter = 6, Ready = (c, d) => SeasonPart(c) >= 0.4 && Played(d) },
            new Slot { Id = "last_letter", Chapter = 6, Ready = (c, d) => SeasonPart(c) >= 0.7 },
            new Slot { Id = "last_game", Chapter = 6, Ready = (c, d) => SeasonPart(c) >= 0.95 },
        };

        static readonly List<StoryRouteDef> Routes = new List<StoryRouteDef> { LastPickStory.Build(), ProdigyStory.Build(), WalkonStory.Build(), LegacyStory.Build() };

        internal static StoryRouteDef RouteOf(Career c) => Routes.First(r => r.Id == c.Story.Route);

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
        /// <summary>When no scene is due, a legend's visit may come instead.</summary>
        public static StoryEvent Next(Career c, DayResult day)
        {
            if (c.InMilitary) return null;
            // A legend's record falling is today's news before anything else.
            var surpass = LegendStory.SurpassToday(c, day);
            if (surpass != null) return surpass;
            var slot = DueSlot(c, day);
            if (slot != null)
            {
                var scene = SceneFor(c, slot.Id);
                bool gapOk = slot.NoGap || c.Story.LastSeason < 0 || DaysSinceLast(c) >= MinGap(c);
                if (gapOk && !scene.AtSeasonStart && slot.Ready(c, day) && (scene.When?.Invoke(c, day) ?? true)) return ToEvent(c, slot.Id, scene);
            }
            return LegendStory.Next(c, day);
        }

        /// <summary>
        /// A scene told before the season's first game (the draft, spring camp, a winter ceremony), or null.
        /// Called again after each one, so the prologue plays out in a row.
        /// </summary>
        public static StoryEvent AtSeasonStart(Career c)
        {
            if (c.InMilitary || c.Day != 0) return null;
            var slot = DueSlot(c, null);
            if (slot != null)
            {
                var scene = SceneFor(c, slot.Id);
                if (scene.AtSeasonStart || slot.Pre != null && slot.Pre(c)) return ToEvent(c, slot.Id, scene);
            }
            return LegendStory.AtSeasonStart(c);
        }

        /// <summary>The next slot, after quietly passing over any whose moment has gone by.</summary>
        static Slot DueSlot(Career c, DayResult day)
        {
            var slot = NextSlot(c);
            // The last season has come: what's left of the earlier chapters stays untold.
            while (slot != null && (slot.Expired(c, day) || c.IsFinalSeason && slot.Chapter < 6))
            {
                c.Story.Seen.Add(slot.Id);
                slot = NextSlot(c);
            }
            if (slot != null && Stale(c, slot))
            {
                // One slot at a time, and the clock starts again so the next one gets its chance.
                c.Story.Seen.Add(slot.Id);
                c.Story.LastSeason = c.Season;
                c.Story.LastDay = c.Day - MinGap(c);
                slot = NextSlot(c);
            }
            return slot;
        }

        /// <summary>
        /// Two whole seasons without a scene: a slot whose moment never comes is passed over so the
        /// story can't stall. A slot told before a season (the last season...) waits for its moment.
        /// </summary>
        static bool Stale(Career c, Slot slot) =>
            slot.Pre == null && c.Story.LastSeason >= 0 && DaysSinceLast(c) >= 2 * Games(c);

        /// <summary>
        /// The first slot after the last one answered. Saves from before a slot was added skip the
        /// new ones that come earlier in the story, so an old career never jumps back a chapter.
        /// </summary>
        static Slot NextSlot(Career c)
        {
            int after = -1;
            for (int i = 0; i < Slots.Count; i++)
                if (c.Story.Seen.Contains(Slots[i].Id)) after = i;
            return after + 1 < Slots.Count ? Slots[after + 1] : null;
        }

        /// <summary>True once the last slot has been answered.</summary>
        public static bool IsFinished(Career c) => NextSlot(c) == null;

        /// <summary>The card for a slot of this career's story, whatever the day; for tests and previews.</summary>
        public static StoryEvent Preview(Career c, string slot) => ToEvent(c, slot);

        /// <summary>How many versions a slot has for this career's story.</summary>
        public static int VariantCount(Career c, string slot) => Options(c, slot).Count;

        /// <summary>One version of a slot's card, for tests and previews.</summary>
        public static StoryEvent Preview(Career c, string slot, int variant) => ToEvent(c, slot, Options(c, slot)[variant]);

        static StoryEvent ToEvent(Career c, string slotId, StoryScene scene = null)
        {
            var slot = Slots.First(s => s.Id == slotId);
            scene = scene ?? SceneFor(c, slotId);
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
                        var promise = StoryPromises.OnScene(c, slotId);
                        if (promise != null) e.Say($"[약속] {promise.Text}");
                        if (!c.Story.Seen.Contains(slotId)) c.Story.Seen.Add(slotId);
                        c.Story.Log.Add($"{title} — {label}");
                        c.Story.LastSeason = c.Season;
                        c.Story.LastDay = c.Day;
                    },
                });
            }
            return ev;
        }

        /// <summary>
        /// The version of a slot's scene this career gets: one of the story's own and the shared ones,
        /// picked at random once per career (the same pick every time it's asked for).
        /// </summary>
        static StoryScene SceneFor(Career c, string slotId)
        {
            var options = Options(c, slotId);
            if (options.Count == 1) return options[0];
            if (c.Story.Seed == 0) c.Story.Seed = SeedFor(c.Me);
            uint h = (uint)c.Story.Seed;
            foreach (char ch in slotId) h = h * 31 + ch;
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            return options[(int)(h % (uint)options.Count)];
        }

        static List<StoryScene> Options(Career c, string slotId)
        {
            var own = RouteOf(c).Scenes.Where(s => s.Slot == slotId).ToList();
            var all = own.Any(s => s.Only) ? own : own.Concat(SharedScenes.All.Concat(BondScenes.All).Where(s => s.Slot == slotId)).ToList();
            // A version for how close the player is to someone right now comes first; others wait for their moment.
            var fitting = all.Where(s => s.Needs != null && s.Needs(c)).ToList();
            return fitting.Count > 0 ? fitting : all.Where(s => s.Needs == null).ToList();
        }

        /// <summary>A stable number from who the player is, for saves made before the seed existed.</summary>
        internal static int SeedFor(CareerPlayer me)
        {
            uint h = 2166136261u;
            foreach (char ch in me.Name + me.Number + (int)me.Position) h = (h ^ ch) * 16777619u;
            // Starting ratings, not potential: potential is the same cap for everyone since limits were dropped.
            foreach (var stat in me.MyStats) h = (h ^ (uint)me.Ratings[stat]) * 16777619u;
            return (int)(h | 1);
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

        /// <summary>The friend's setback mid-career turns around: stayed by their side, or simply close enough.</summary>
        internal static bool FriendCameBack(Career c) => c.Story.Has("visited") || c.Story.BondOf(Bond.Friend) >= 75;

        internal static bool HasSummit(Career c) => c.Me.GoldMedals > 0 || c.Me.History.Any(h => h.Titles.Count > 0);

        static bool InASlump(Career c) => c.HitlessStreak >= 4;

        static bool DemotedToday(DayResult d) => d?.Move != null && d.Move.Kind == RosterMoveKind.Demoted;

        static bool Played(DayResult d) => d != null && (d.MyLine != null && d.MyLine.PlateAppearances > 0 || d.MyPitching != null);

        /// <summary>A bad night: no hit in three or more at bats, or three runs given up.</summary>
        internal static bool BadGame(Career c, DayResult d) =>
            d != null && (d.MyLine != null && d.MyLine.AtBats >= 3 && d.MyLine.Hits == 0 || d.MyPitching != null && d.MyPitching.Runs >= 3);

        static bool WonTitleLastSeason(Career c)
        {
            var last = c.Me.History.LastOrDefault();
            return last != null && last.Titles.Count > 0;
        }

        /// <summary>Today's game brought the first hit (or win, save, hold) of the career.</summary>
        internal static bool FirstRecordToday(Career c, DayResult d)
        {
            if (d == null || d.InFarm) return false;
            var me = c.Me;
            if (!me.IsPitcher)
            {
                int today = d.MyLine?.Hits ?? 0;
                return today > 0 && c.SeasonLine.Hits + me.History.Sum(h => h.Line?.Hits ?? 0) == today;
            }
            int Count(PitcherLine l) => l == null ? 0 : me.Role == PitcherRole.Closer ? l.Saves : me.Role == PitcherRole.Setup ? l.Holds : l.Wins;
            int now = Count(d.MyPitching);
            return now > 0 && Count(c.SeasonPitching) + me.History.Sum(h => Count(h.Pitching)) == now;
        }

        static bool PlayedWell(DayResult d) =>
            d != null && (d.MyLine != null && (d.MyLine.HomeRuns > 0 || d.MyLine.Hits >= 2)
            || d.MyPitching != null && !d.InFarm && (d.MyPitching.Wins + d.MyPitching.Saves + d.MyPitching.Holds > 0));

        static bool IsCore(Career c)
        {
            var me = c.Me;
            double average = me.MyStats.Average(s => me.Ratings[s]);
            return !c.InFarm && (me.IsPitcher ? average >= 65 : c.LineupSlot == 3 || average >= 68);
        }

        // ---------- Text ----------

        /// <summary>
        /// Fills {name} {team} {rival} {rivalTeam} {mentor} {first} {spot} {mound}, and the four people of
        /// the story by name: {coach} {pal} {kin} {voice}. A name may carry its particle, e.g. {pal:이}
        /// or {name:을}, which becomes 이/가, 을/를 and so on to suit the name.
        /// </summary>
        public static string Fill(string text, Career c)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var me = c.Me;
            var route = RouteOf(c);
            string NameOf(string key)
            {
                switch (key)
                {
                    case "name": return me.Name;
                    case "team": return c.MyTeam.Name;
                    case "rivalTeam": return c.RivalTeam?.Name ?? "다른 팀";
                    case "rival": return c.Rival?.Name ?? "그 녀석";
                    case "mentor": return c.Story.Has("manager") ? route.MentorAfter : route.MentorBefore;
                    case "coach": return route.BondNames[(int)Bond.Mentor];
                    case "pal": return route.BondNames[(int)Bond.Friend];
                    case "kin": return route.BondNames[(int)Bond.Family];
                    case "voice": return route.BondNames[(int)Bond.Voice];
                    case "voiceRole": return route.BondRoles[(int)Bond.Voice];
                    case "first": return FirstRecordName(me);
                    case "spot": return SpotName(me);
                    case "mound": return me.IsPitcher ? "마운드" : "타석";
                    default: return null;
                }
            }
            return Placeholder.Replace(text, m =>
            {
                string word = NameOf(m.Groups[1].Value);
                if (word == null) return m.Value;
                return m.Groups[2].Success ? word + Josa.After(word, m.Groups[2].Value) : word;
            });
        }

        static readonly System.Text.RegularExpressions.Regex Placeholder =
            new System.Text.RegularExpressions.Regex(@"\{([a-zA-Z]+)(?::([가-힣]+))?\}");

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
            // Otherwise the person the player ended up closest to decides where life goes next.
            var closest = AllBonds.OrderByDescending(b => c.Story.BondOf(b)).ThenBy(b => (int)b).First();
            if (c.Story.BondOf(closest) >= BondEndingAt)
                switch (closest)
                {
                    case Bond.Voice: return StoryEnding.Broadcaster;
                    case Bond.Family: return StoryEnding.Hometown;
                    case Bond.Mentor: return StoryEnding.Scout;
                    default: return StoryEnding.Partner;
                }
            return StoryEnding.BaseballSchool;
        }

        /// <summary>How close someone must end up for the ending to follow them.</summary>
        public const int BondEndingAt = 80;

        public static string EndingTitle(StoryEnding ending)
        {
            switch (ending)
            {
                case StoryEnding.Legend: return "엔딩 · 전설";
                case StoryEnding.OneClub: return "엔딩 · 원클럽맨";
                case StoryEnding.BaseballSchool: return "엔딩 · 작은 야구 교실";
                case StoryEnding.Broadcaster: return "엔딩 · 마이크 앞에서";
                case StoryEnding.Hometown: return "엔딩 · 고향";
                case StoryEnding.Scout: return "엔딩 · 다음 이야기를 찾아서";
                case StoryEnding.Partner: return "엔딩 · 다시 한 팀";
                default: return "";
            }
        }

        /// <summary>The ending told as an article, with a line for each person depending on how close they ended up.</summary>
        public static List<string> EndingLines(Career c, RetiredPlayer retired)
        {
            var ending = retired.Ending != StoryEnding.None ? retired.Ending : EndingFor(c, retired);
            var route = RouteOf(c);
            var lines = new List<string>(StoryArcs.Opening(c, retired, ending) ?? route.Opening(c, retired, ending));
            foreach (var b in AllBonds) lines.Add(route.BondLine(c, b, c.Story.BondOf(b)));
            if (c.Rival != null)
                lines.Add(c.Story.Has("rival_respect")
                    ? $"{c.Rival.Name} 선수가 보낸 화환도 있었다. '덕분에 나도 여기까지 왔다.'"
                    : $"{c.Rival.Name} 선수의 이름이 적힌 화환도 있었다. 끝까지 지기 싫은 상대였다.");
            if (route.Epilogue != null) lines.AddRange(route.Epilogue(c));
            if (StoryPromises.WasKept(c, "last")) lines.Add("마지막 시즌, 약속한 숫자까지 채우고 떠났다. 기록지는 지금도 액자에 걸려 있다.");
            lines.AddRange(LegendStory.EndingLines(c));
            lines.AddRange(Romance.EndingLines(c));
            lines.Add(StoryArcs.Closing(c, retired, ending) ?? route.Closing(c, retired, ending));
            return lines.Select(l => Fill(l, c)).ToList();
        }
    }
}
