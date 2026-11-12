using System;
using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>
    /// Training tools from the shop, bought with the player's salary only (never real money). Each stat
    /// has three tiers; the best one owned makes that stat's training give more exp. Kept well under
    /// the training cards so the two stack without running away.
    /// </summary>
    public static class TrainingTools
    {
        public const int Tiers = 3;
        static readonly int[] Percent = { 10, 20, 30 };
        static readonly int[] Prices = { 300, 1500, 5000 };

        static readonly Dictionary<Stat, string[]> Names = new Dictionary<Stat, string[]>
        {
            { Stat.Contact, new[] { "배팅 티", "배팅 머신", "스윙 분석 센서" } },
            { Stat.Power, new[] { "덤벨 세트", "파워랙", "개인 웨이트룸" } },
            { Stat.Eye, new[] { "투구 영상 구독", "선구안 훈련 앱", "VR 타석 시뮬레이터" } },
            { Stat.Speed, new[] { "사다리·콘 세트", "저항 썰매", "스프린트 트레이너" } },
            { Stat.Defense, new[] { "펑고 배트", "바운드 볼 머신", "수비 반응 장비" } },
            { Stat.Velocity, new[] { "웨이티드 볼", "투구 레이더", "투구 동작 분석" } },
            { Stat.Control, new[] { "투구 타깃", "전담 불펜 포수", "제구 추적 장비" } },
            { Stat.Breaking, new[] { "그립 연습구", "회전수 측정기", "피치 디자인 랩" } },
            { Stat.Stamina, new[] { "줄넘기", "러닝머신", "고지대 훈련 마스크" } },
        };

        /// <summary>Name of a tier (1 to 3) for a stat.</summary>
        public static string Name(Stat stat, int tier) => Names[stat][Math.Max(1, Math.Min(Tiers, tier)) - 1];

        /// <summary>Extra training exp in percent at a tier (0 = none).</summary>
        public static int Bonus(int tier) => tier <= 0 ? 0 : Percent[Math.Min(Tiers, tier) - 1];

        /// <summary>Price in 만원 of a tier (1 to 3).</summary>
        public static int Price(int tier) => Prices[Math.Max(1, Math.Min(Tiers, tier)) - 1];
    }

    /// <summary>A training boost for a while, from a story event: one stat trains faster for some days.</summary>
    public class TrainingBoost
    {
        public Stat Stat;
        public int Percent;
        public int DaysLeft;
        /// <summary>Where it came from, shown on the growth screen.</summary>
        public string Source = "";
    }
}
