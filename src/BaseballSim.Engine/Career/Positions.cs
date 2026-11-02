using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>Outfield is the old catch-all outfielder, kept so old saves read; saves turn it into CenterField.</summary>
    public enum Position { Catcher, FirstBase, SecondBase, ThirdBase, Shortstop, Outfield, DesignatedHitter, Pitcher, LeftField, CenterField, RightField }

    public enum PitcherRole { Starter, Setup, Closer }

    /// <summary>
    /// Positions, picked once at registration. Harder positions make the glove count for more in the
    /// golden glove vote and a catcher tires faster; a designated hitter doesn't field at all. A pitcher
    /// is a different career: he trains pitching and starts every fifth game.
    /// </summary>
    public static class Positions
    {
        public static readonly Position[] All = (Position[])Enum.GetValues(typeof(Position));
        /// <summary>Fielding positions in lineup order: C, 1B, 2B, 3B, SS, LF, CF, RF, DH.</summary>
        public static readonly Position[] Fielders =
        {
            Position.Catcher, Position.FirstBase, Position.SecondBase, Position.ThirdBase, Position.Shortstop,
            Position.LeftField, Position.CenterField, Position.RightField, Position.DesignatedHitter,
        };

        static readonly string[] KoreanNames = { "포수", "1루수", "2루수", "3루수", "유격수", "외야수", "지명타자", "투수", "좌익수", "중견수", "우익수" };
        static readonly string[] Shorts = { "C", "1B", "2B", "3B", "SS", "OF", "DH", "P", "LF", "CF", "RF" };
        /// <summary>What the game shows: scorecard letters, e.g. "CF".</summary>
        public static string Name(Position p) => Shorts[(int)p];
        public static string Short(Position p) => Shorts[(int)p];
        /// <summary>The Korean name, for explaining a position at registration, e.g. "중견수".</summary>
        public static string KoreanName(Position p) => KoreanNames[(int)p];

        public static bool IsOutfield(Position p) =>
            p == Position.Outfield || p == Position.LeftField || p == Position.CenterField || p == Position.RightField;

        /// <summary>Old saves' plain outfielder becomes a center fielder.</summary>
        public static Position Upgrade(Position p) => p == Position.Outfield ? Position.CenterField : p;

        /// <summary>How much the glove matters, per defense point above 50, in OPS terms.</summary>
        public static double DefenseWeight(Position p)
        {
            switch (p)
            {
                case Position.Catcher:
                case Position.Shortstop: return 0.006;
                case Position.SecondBase:
                case Position.CenterField: return 0.005;
                case Position.ThirdBase:
                case Position.Outfield:
                case Position.LeftField:
                case Position.RightField: return 0.004;
                case Position.FirstBase: return 0.002;
                default: return 0;
            }
        }

        static readonly string[] RoleNames = { "SP", "RP", "CP" };
        static readonly string[] RoleKoreanNames = { "선발", "중계", "마무리" };
        /// <summary>What the game shows for a pitcher: SP (starter), RP (setup), CP (closer).</summary>
        public static string RoleName(PitcherRole r) => RoleNames[(int)r];
        public static string RoleKoreanName(PitcherRole r) => RoleKoreanNames[(int)r];

        public static int ExtraGameFatigue(Position p) => p == Position.Catcher ? 3 : p == Position.DesignatedHitter ? -2 : 0;

        /// <summary>League lineups are built catcher, 1B, 2B, 3B, SS, LF, CF, RF, DH.</summary>
        public static Position OfLineupIndex(int index) => index >= 0 && index < Fielders.Length ? Fielders[index] : Position.DesignatedHitter;

        /// <summary>
        /// Golden glove groups: the three outfield spots vote together for three gloves, as in the real awards;
        /// every other position gets one.
        /// </summary>
        public static readonly Position[] GloveGroups =
        {
            Position.Catcher, Position.FirstBase, Position.SecondBase, Position.ThirdBase, Position.Shortstop,
            Position.Outfield, Position.DesignatedHitter,
        };

        public static Position GloveGroup(Position p) => IsOutfield(p) ? Position.Outfield : p;

        public static int GoldenGloves(Position group) => group == Position.Outfield ? 3 : 1;
    }

    public partial class Career
    {
        /// <summary>Position and defense of any hitter in the league, for the golden gloves.</summary>
        (Position, int) FieldingOf(string id)
        {
            if (id == Me.Id) return (Me.Position, Me.EffectiveRating(Stat.Defense));
            foreach (var team in League.Teams)
            {
                int i = team.Lineup.FindIndex(p => p.Id == id);
                if (i >= 0) return (Positions.OfLineupIndex(i), team.Lineup[i].Batting.Defense);
            }
            return (Position.DesignatedHitter, 50);
        }
    }
}
