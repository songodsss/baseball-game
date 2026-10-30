using System.Collections.Generic;

namespace BaseballSim.Career
{
    public enum Facility
    {
        TrainerRoom,  // 트레이너실: faster fatigue recovery
        RehabCenter,  // 재활 센터: fewer and shorter injuries
        Conditioning, // 컨디셔닝 센터: slower decline after 32
    }

    /// <summary>
    /// The player's own facilities, bought with in-game salary once gear is maxed. They make a career
    /// easier to manage and last longer, but never raise a rating directly.
    /// </summary>
    public static class Facilities
    {
        public const int MaxLevel = 3;

        /// <summary>Price in 만원 of each level, 1 to 3.</summary>
        static readonly int[] Prices = { 8000, 25000, 70000 };

        public static IEnumerable<Facility> All => (Facility[])System.Enum.GetValues(typeof(Facility));

        public static int Price(int level) => Prices[level - 1];

        public static string Name(Facility f)
        {
            switch (f)
            {
                case Facility.TrainerRoom: return "트레이너실";
                case Facility.RehabCenter: return "재활 센터";
                default: return "컨디셔닝 센터";
            }
        }

        /// <summary>What the given level does, for the shop.</summary>
        public static string Describe(Facility f, int level)
        {
            switch (f)
            {
                case Facility.TrainerRoom: return $"매일 피로 회복 +{ExtraRecovery(level)}";
                case Facility.RehabCenter: return $"부상 확률·기간 -{(int)(InjuryReduction(level) * 100)}%";
                default: return $"노쇠화 -{(int)(DeclineReduction(level) * 100)}%";
            }
        }

        public static int ExtraRecovery(int level) => 2 * level;
        public static double InjuryReduction(int level) => 0.25 * level;
        public static double DeclineReduction(int level) => 0.2 * level;
    }
}
