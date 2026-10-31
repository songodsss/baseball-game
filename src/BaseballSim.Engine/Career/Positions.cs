using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum Position { Catcher, FirstBase, SecondBase, ThirdBase, Shortstop, Outfield, DesignatedHitter, Pitcher }

    public enum PitcherRole { Starter, Setup, Closer }

    /// <summary>
    /// Positions, picked once at registration. Harder positions make the glove count for more in the
    /// golden glove vote and a catcher tires faster; a designated hitter doesn't field at all. A pitcher
    /// is a different career: he trains pitching and starts every fifth game.
    /// </summary>
    public static class Positions
    {
        public static readonly Position[] All = (Position[])Enum.GetValues(typeof(Position));
        public static readonly Position[] Fielders = All.Where(p => p != Position.Pitcher).ToArray();

        static readonly string[] Names = { "포수", "1루수", "2루수", "3루수", "유격수", "외야수", "지명타자", "투수" };
        public static string Name(Position p) => Names[(int)p];

        /// <summary>How much the glove matters, per defense point above 50, in OPS terms.</summary>
        public static double DefenseWeight(Position p)
        {
            switch (p)
            {
                case Position.Catcher:
                case Position.Shortstop: return 0.006;
                case Position.SecondBase: return 0.005;
                case Position.ThirdBase:
                case Position.Outfield: return 0.004;
                case Position.FirstBase: return 0.002;
                default: return 0;
            }
        }

        static readonly string[] RoleNames = { "선발", "중계", "마무리" };
        public static string RoleName(PitcherRole r) => RoleNames[(int)r];

        public static int ExtraGameFatigue(Position p) => p == Position.Catcher ? 3 : p == Position.DesignatedHitter ? -2 : 0;

        /// <summary>League lineups are built catcher, 1B, 2B, 3B, SS, three outfielders, DH.</summary>
        public static Position OfLineupIndex(int index) =>
            index < 5 ? (Position)index : index < 8 ? Position.Outfield : Position.DesignatedHitter;

        /// <summary>Golden gloves per position: three outfielders, one of everything else.</summary>
        public static int GoldenGloves(Position p) => p == Position.Outfield ? 3 : 1;
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
