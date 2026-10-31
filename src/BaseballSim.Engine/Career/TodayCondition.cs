using System;

namespace BaseballSim.Career
{
    public partial class Career
    {
        /// <summary>Stars shown for a day's condition, worst to best.</summary>
        public const int MaxConditionStars = 5;

        /// <summary>
        /// Today's form, -4 to +4. It comes from the date, not the shared random stream, so the screen can show it
        /// before the player picks a training and the game that day uses the same value.
        /// </summary>
        public int TodayForm
        {
            get
            {
                unchecked
                {
                    uint h = 2166136261;
                    foreach (char ch in Me.Name ?? "") h = (h ^ ch) * 16777619;
                    h = (h ^ (uint)Season) * 16777619;
                    h = (h ^ (uint)Day) * 16777619;
                    h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                    return (int)(h % 9) - 4;
                }
            }
        }

        /// <summary>Game condition today: form, minus tiredness, plus half the morale. Same formula as ForGame.</summary>
        public int TodayCondition => TodayForm - Math.Max(0, (Me.Fatigue - 50) / 6) + Me.Morale / 2;

        /// <summary>Today's condition as 1–5 stars.</summary>
        public int ConditionStars
        {
            get
            {
                int c = TodayCondition;
                return c <= -4 ? 1 : c <= -2 ? 2 : c <= 1 ? 3 : c <= 3 ? 4 : 5;
            }
        }

        /// <summary>Training pays off more on a good day: +10% at 4 stars, +25% at 5.</summary>
        public static double ConditionTraining(int stars) => stars >= 5 ? 1.25 : stars == 4 ? 1.1 : 1.0;

        /// <summary>A short label for the stars, e.g. "최상".</summary>
        public static string ConditionLabel(int stars) =>
            stars >= 5 ? "최상" : stars == 4 ? "좋음" : stars == 3 ? "보통" : stars == 2 ? "나쁨" : "최악";
    }
}
