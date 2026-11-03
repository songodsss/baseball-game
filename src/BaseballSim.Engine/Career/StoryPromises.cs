using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>A promise made in a story scene, settled at the end of that season.</summary>
    public class StoryPromise
    {
        public string Id;
        public int Season;
        /// <summary>A season count (hits, wins, saves, holds, home runs), or null for "the team reaches the autumn" (top four).</summary>
        public GoalKind? Kind;
        public double Target;
        /// <summary>"최강훈과의 약속 · 안타 120개", filled with the story's names.</summary>
        public string Text;

        public bool IsTeamPromise => Kind == null;

        /// <summary>Where the season stands: "85/120", or the team's place.</summary>
        public string Progress(Career c)
        {
            if (IsTeamPromise) return $"현재 {c.StandingRank()}위 / 4위 안";
            var goal = new SeasonGoal { Kind = Kind.Value, Target = Target };
            return goal.Progress(c);
        }
    }

    public class PromiseResult
    {
        public StoryPromise Promise;
        public bool Kept;
        public string Line;
    }

    /// <summary>
    /// Story promises: a few scenes end with something to do on the field this season (hold on after
    /// the first wall, live up to the big spot, take the team to the autumn in the crisis, a number
    /// for family, a last-season promise). Kept or not, it's settled at season end with a line from
    /// the person it was made to, and the story remembers it. Kept as story flags, so no save change.
    /// </summary>
    public static class StoryPromises
    {
        const string Prefix = "promise:";
        const string KeptPrefix = "kept:";
        const string BrokenPrefix = "broken:";

        class Def
        {
            public Bond Who;
            /// <summary>"{coach}와의 약속 · " is put in front of the target.</summary>
            public string Title;
            /// <summary>Share of the season's expected count; 0 = a team promise (top four).</summary>
            public double Share;
            public string Kept, Broken;
            public int KeptBond = 10, BrokenBond = -5;
        }

        static readonly Dictionary<string, Def> Defs = new Dictionary<string, Def>
        {
            ["wall"] = new Def
            {
                Who = Bond.Mentor, Title = "{coach:과}의 약속", Share = 0.75,
                Kept = "{coach:이} 시즌 마지막 날 짧게 말했다. \"버텼네. 그거면 됐다.\"",
                Broken = "{coach:이} 어깨를 한 번 두드렸다. \"올해는 여기까지. 벽은 내년에도 거기 있다.\"",
            },
            ["spot"] = new Def
            {
                Who = Bond.Voice, Title = "{spot}의 약속", Share = 1.0,
                Kept = "{voice}의 시즌 결산 제목. '이름값, 증명 완료.'",
                Broken = "{voice}의 시즌 결산은 조심스러웠다. '아직은 그 자리가 무겁다. 하지만 내년은 다를 것이다.'",
                BrokenBond = 0,
            },
            ["arc_crisis"] = new Def
            {
                Who = Bond.Mentor, Title = "흔들리는 팀을 가을야구로", Share = 0,
                Kept = "가을야구 확정. 라커룸에서 {coach:이} 처음으로 크게 웃었다.",
                Broken = "가을야구는 끝내 오지 않았다. {coach:이} 빈 그라운드를 오래 바라봤다.",
            },
            ["family"] = new Def
            {
                Who = Bond.Family, Title = "{kin:과}의 약속", Share = 0.95,
                Kept = "{kin:이} 기록지를 오려서 냉장고에 붙였다고 한다. \"약속 지켰네.\"",
                Broken = "{kin:이} 전화로 웃었다. \"숫자는 상관없어. 다치지만 마.\"",
                BrokenBond = 0,
            },
            ["last"] = new Def
            {
                Who = Bond.Mentor, Title = "마지막 시즌의 약속", Share = 0.85,
                Kept = "마지막 시즌, 약속한 숫자를 채웠다. {coach:이} 기록지에 사인을 해서 돌려줬다.",
                Broken = "마지막 시즌, 숫자는 조금 모자랐다. 그래도 {coach:이} 말했다. \"끝까지 뛴 게 약속이었다.\"",
                BrokenBond = 0,
            },
        };

        /// <summary>The promise a scene leaves behind, if its slot makes one; called when the scene is answered.</summary>
        public static StoryPromise OnScene(Career c, string slot)
        {
            if (!Defs.TryGetValue(slot, out var def) || c.InMilitary) return null;
            var kind = def.Share > 0 ? MainKind(c.Me) : (GoalKind?)null;
            double target = kind == null ? 4 : Math.Max(1, Math.Round(c.ExpectedSeason(kind.Value) * def.Share));
            c.Story.Flags.RemoveAll(f => f.StartsWith(Prefix + slot + ":"));
            c.Story.Flags.Add($"{Prefix}{slot}:{c.Season}:{(kind == null ? "team" : kind.Value.ToString())}:{target.ToString(CultureInfo.InvariantCulture)}");
            return Active(c).FirstOrDefault(p => p.Id == slot);
        }

        /// <summary>This season's open promises.</summary>
        public static List<StoryPromise> Active(Career c)
        {
            var list = new List<StoryPromise>();
            foreach (var flag in c.Story.Flags.Where(f => f.StartsWith(Prefix)))
            {
                var parts = flag.Substring(Prefix.Length).Split(':');
                if (parts.Length != 4 || !Defs.TryGetValue(parts[0], out var def)) continue;
                if (!int.TryParse(parts[1], out var season) || season != c.Season) continue;
                GoalKind? kind = parts[2] == "team" ? (GoalKind?)null : (GoalKind)Enum.Parse(typeof(GoalKind), parts[2]);
                double target = double.Parse(parts[3], CultureInfo.InvariantCulture);
                var p = new StoryPromise { Id = parts[0], Season = season, Kind = kind, Target = target };
                string what = kind == null ? "정규시즌 4위 안" : new SeasonGoal { Kind = kind.Value, Target = target }.Text;
                p.Text = Fill($"{def.Title} · {what}", c);
                list.Add(p);
            }
            return list;
        }

        public static bool WasKept(Career c, string id) => c.Story.Has(KeptPrefix + id);
        public static bool WasBroken(Career c, string id) => c.Story.Has(BrokenPrefix + id);

        /// <summary>At season end (before the season number moves on): each promise kept or not, with its line and bond change.</summary>
        internal static List<PromiseResult> Settle(Career c, SeasonSummary summary)
        {
            var results = new List<PromiseResult>();
            foreach (var p in Active(c))
            {
                var def = Defs[p.Id];
                bool kept = p.IsTeamPromise ? summary.Rank <= 4 : new SeasonGoal { Kind = p.Kind.Value, Target = p.Target }.Current(c) >= p.Target;
                c.Story.Flags.Add((kept ? KeptPrefix : BrokenPrefix) + p.Id);
                int delta = kept ? def.KeptBond : def.BrokenBond;
                int before = c.Story.BondOf(def.Who);
                c.Story.Bonds[def.Who] = Math.Max(0, Math.Min(100, before + delta));
                if (kept) c.Me.Morale = Math.Min(CareerPlayer.MaxMorale, c.Me.Morale + 3);
                string line = Fill(kept ? def.Kept : def.Broken, c);
                results.Add(new PromiseResult { Promise = p, Kept = kept, Line = line });
                summary.Record.Notes.Add($"스토리 약속 {(kept ? "지킴" : "못 지킴")}: {p.Text}");
            }
            c.Story.Flags.RemoveAll(f => f.StartsWith(Prefix));
            return results;
        }

        /// <summary>The count a promise is about: hits, or wins, saves or holds by role.</summary>
        static GoalKind MainKind(CareerPlayer me) =>
            !me.IsPitcher ? GoalKind.Hits : me.Role == PitcherRole.Closer ? GoalKind.Saves : me.Role == PitcherRole.Setup ? GoalKind.Holds : GoalKind.Wins;
    }

    public partial class Career
    {
        /// <summary>The team's place in the standings today (1 = first).</summary>
        public int StandingRank()
        {
            var ranked = League.Ranked();
            int i = ranked.FindIndex(s => s.Team == MyTeam);
            return i < 0 ? ranked.Count : i + 1;
        }

        /// <summary>What a whole season of this kind looks like for the player's ratings now (the "normal" goal, no randomness).</summary>
        internal double ExpectedSeason(GoalKind kind)
        {
            var me = Me.ForGame(new SeededRandom(Season));
            if (!Me.IsPitcher)
            {
                me.Condition = 0;
                var odds = PlateAppearanceModel.Compute(me, new Player("avg", "avg") { Pitching = new PitchingRatings() }, 50, 0);
                double pa = League.GamesPerSeason * (4.6 - 0.1 * LineupSlot);
                double hitRate = odds.Single + odds.Double + odds.Triple + odds.HomeRun;
                return kind == GoalKind.HomeRuns ? odds.HomeRun * pa : hitRate * pa;
            }
            var pitch = me.Pitching;
            double era = ExpectedEra(pitch.Velocity, pitch.Control, pitch.Breaking);
            double quality = Math.Max(0.5, 1 + (LeagueEra - era) * 0.1);
            switch (kind)
            {
                case GoalKind.Saves: return League.GamesPerSeason * SavesPerGame * quality;
                case GoalKind.Holds: return League.GamesPerSeason * HoldsPerGame * quality;
                default:
                    double starts = League.GamesPerSeason / (double)StartEvery;
                    return starts * Math.Max(0.15, 0.40 + (LeagueEra - era) * 0.08);
            }
        }
    }
}
