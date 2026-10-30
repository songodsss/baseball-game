using System.Collections.Generic;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>A paid product candidate. None of them touch ratings, growth or money.</summary>
    public class PremiumProduct
    {
        public string Id;
        public string Name;
        public string Description;
    }

    /// <summary>
    /// Paid product candidates, bought through Monetization.Store. Until a real store SDK keeps
    /// receipts, what's owned is remembered on the device, and the test screen can reset it.
    /// </summary>
    public static class Premium
    {
        public const string RemoveAds = "remove_ads";
        public const string ExtraSlots = "extra_slots";
        public const string FastSkip = "fast_skip";

        public static readonly List<PremiumProduct> All = new List<PremiumProduct>
        {
            new PremiumProduct
            {
                Id = RemoveAds, Name = "광고 제거",
                Description = "피로 회복 보상을 광고 없이 바로 받아요. 하루 횟수는 그대로예요.",
            },
            new PremiumProduct
            {
                Id = ExtraSlots, Name = "저장 슬롯 +2",
                Description = $"선수를 동시에 {SaveStore.BaseSlots + ExtraSlotCount}명까지 키울 수 있어요. 내 팀은 모든 슬롯이 함께 써요.",
            },
            new PremiumProduct
            {
                Id = FastSkip, Name = "빠른 진행",
                Description = $"같은 훈련을 {FastSkipDays}일 연속으로 한 번에 진행해요. 결과는 하루씩 누른 것과 똑같아요.",
            },
        };

        public const int ExtraSlotCount = 2;
        /// <summary>Days one fast-skip press plays, stopping early at anything that needs a choice.</summary>
        public const int FastSkipDays = 7;

        static string Key(string id) => "premium_" + id;

        public static bool Owns(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

        public static void Buy(string id, System.Action<bool> onDone)
        {
            Monetization.Store.Buy(id, bought =>
            {
                if (bought)
                {
                    PlayerPrefs.SetInt(Key(id), 1);
                    PlayerPrefs.Save();
                }
                onDone(bought);
            });
        }

        /// <summary>Test only: forget every purchase, to try the flows again.</summary>
        public static void ResetPurchases()
        {
            foreach (var p in All) PlayerPrefs.DeleteKey(Key(p.Id));
            PlayerPrefs.Save();
        }
    }
}
