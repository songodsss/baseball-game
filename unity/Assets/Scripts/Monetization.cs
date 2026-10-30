using System;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>Rewarded video ads. A real ad SDK (AdMob, Unity Ads) slots in behind this later.</summary>
    public interface IRewardedAds
    {
        bool IsReady { get; }
        /// <summary>Plays an ad; the callback says whether it was watched to the end.</summary>
        void Show(Action<bool> onDone);
    }

    /// <summary>In-app purchases. Unity IAP or a store SDK slots in behind this later.</summary>
    public interface IStore
    {
        /// <summary>Display price, e.g. "₩1,200".</summary>
        string PriceOf(string productId);
        void Buy(string productId, Action<bool> onDone);
    }

    /// <summary>
    /// Where the game finds its ad and store services. Until the real SDKs are added, test
    /// versions grant everything at once, so every flow can be played through.
    /// </summary>
    public static class Monetization
    {
        public static IRewardedAds Ads = new TestAds();
        public static IStore Store = new TestStore();

        /// <summary>Rewarded ads a player may watch per real-world day.</summary>
        public const int AdsPerDay = 3;
        /// <summary>Fatigue removed by one rewarded ad.</summary>
        public const int AdFatigueRecovery = 30;

        const string AdDateKey = "ads_date";
        const string AdCountKey = "ads_count";

        public static int AdsLeftToday
        {
            get
            {
                if (PlayerPrefs.GetString(AdDateKey, "") != Today) return AdsPerDay;
                return Mathf.Max(0, AdsPerDay - PlayerPrefs.GetInt(AdCountKey, 0));
            }
        }

        public static void CountAdWatched()
        {
            int used = AdsPerDay - AdsLeftToday + 1;
            PlayerPrefs.SetString(AdDateKey, Today);
            PlayerPrefs.SetInt(AdCountKey, used);
            PlayerPrefs.Save();
        }

        static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        class TestAds : IRewardedAds
        {
            public bool IsReady => true;

            public void Show(Action<bool> onDone)
            {
                Debug.Log("[테스트 광고] 실제 광고 SDK를 붙이기 전이라 바로 보상을 줍니다.");
                onDone(true);
            }
        }

        class TestStore : IStore
        {
            public string PriceOf(string productId) => "₩1,200";

            public void Buy(string productId, Action<bool> onDone)
            {
                Debug.Log($"[테스트 결제] {productId}: 실제 결제 없이 지급합니다.");
                onDone(true);
            }
        }
    }
}
