using System;

namespace BaseballSim.Career
{
    public enum InjuryKind { None, Ankle, Hamstring, Wrist, Back, Shoulder }

    /// <summary>
    /// Kinds of injury: each keeps the player out for its own stretch, then costs a few points of one
    /// stat for as long again after he's back, so coming back isn't the end of it.
    /// </summary>
    public static class Injuries
    {
        static readonly (InjuryKind Kind, string Name, int MinDays, int MaxDays, Stat Stat, int Penalty, double Odds)[] Table =
        {
            (InjuryKind.Ankle, "발목 염좌", 3, 7, Stat.Speed, 3, 3),
            (InjuryKind.Hamstring, "햄스트링", 6, 12, Stat.Speed, 6, 3),
            (InjuryKind.Wrist, "손목 통증", 5, 11, Stat.Contact, 5, 2),
            (InjuryKind.Back, "허리 통증", 7, 14, Stat.Power, 5, 2),
            (InjuryKind.Shoulder, "어깨 염증", 10, 20, Stat.Defense, 6, 1),
        };

        static int Index(InjuryKind kind) => Array.FindIndex(Table, t => t.Kind == kind);

        public static string Name(InjuryKind kind) => kind == InjuryKind.None ? "" : Table[Index(kind)].Name;

        /// <summary>The hitter stat it hurts; a pitcher feels it in the matching pitching stat.</summary>
        public static Stat StatOf(InjuryKind kind) => kind == InjuryKind.None ? Stat.Contact : Table[Index(kind)].Stat;

        public static int Penalty(InjuryKind kind) => kind == InjuryKind.None ? 0 : Table[Index(kind)].Penalty;

        /// <summary>A random kind, weighted: ankles and hamstrings are common, shoulders rare.</summary>
        public static InjuryKind Roll(IRandom random)
        {
            double total = 0;
            foreach (var t in Table) total += t.Odds;
            double roll = random.NextDouble() * total;
            foreach (var t in Table)
            {
                roll -= t.Odds;
                if (roll < 0) return t.Kind;
            }
            return InjuryKind.Ankle;
        }

        /// <summary>Days out at full-season pace, before rehab and traits.</summary>
        public static double Days(InjuryKind kind, IRandom random)
        {
            var t = Table[Index(kind)];
            return t.MinDays + random.NextDouble() * (t.MaxDays - t.MinDays);
        }
    }
}
