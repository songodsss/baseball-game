using System.Collections.Generic;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;

namespace BaseballGame
{
    public enum ProductKind
    {
        /// <summary>Bought once, kept for good (remembered on the device).</summary>
        Permanent,
        /// <summary>Bought again and again; goes into the club save.</summary>
        Consumable,
        /// <summary>A thank-you for supporting the game.</summary>
        Support,
    }

    /// <summary>A paid product. None of them touch ratings, money, gear or results.</summary>
    public class PremiumProduct
    {
        public string Id;
        public string Name;
        public string Description;
        /// <summary>Price in won.</summary>
        public int Price;
        public ProductKind Kind;
        /// <summary>Where in the game it is used, shown in the store.</summary>
        public string Where;
    }

    /// <summary>
    /// Paid products, bought through Monetization.Store. Until a real store SDK keeps receipts,
    /// permanent purchases are remembered on the device and the test screen can reset them;
    /// consumables (tickets) and cosmetics go into the club save.
    /// Cosmetics bought with money use the product id "cos_" + the cosmetic id.
    /// </summary>
    public static class Premium
    {
        public const string RemoveAds = "remove_ads";
        public const string ExtraSlots = "extra_slots";
        public const string FastSkip = "fast_skip";
        public const string StarterPack = "starter_pack";
        public const string Tickets5 = "tickets_5";
        public const string Rewind = "season_rewind";
        public const string SupportSmall = "support_small";
        public const string SupportBig = "support_big";
        public const string CosmeticPrefix = "cos_";

        public const int ExtraSlotCount = 2;
        /// <summary>Days one fast-skip press plays, stopping early at anything that needs a choice.</summary>
        public const int FastSkipDays = 7;

        public static readonly List<PremiumProduct> All = new List<PremiumProduct>
        {
            new PremiumProduct
            {
                Id = StarterPack, Name = "시작 패키지", Price = 9900, Kind = ProductKind.Permanent, Where = "구단 스토어",
                Description = "광고 제거 + 저장 슬롯 +2 + 빠른 진행을 한 번에 (따로 사면 14,700원). 한 번만 살 수 있어요.",
            },
            new PremiumProduct
            {
                Id = RemoveAds, Name = "광고 제거", Price = 5900, Kind = ProductKind.Permanent, Where = "트레이닝 센터",
                Description = "피로 회복 보상을 광고 없이 바로 받아요. 하루 횟수는 그대로예요.",
            },
            new PremiumProduct
            {
                Id = ExtraSlots, Name = "저장 슬롯 +2", Price = 4900, Kind = ProductKind.Permanent, Where = "선수 고르기",
                Description = $"선수를 동시에 {SaveStore.BaseSlots + ExtraSlotCount}명까지 키워서 내 팀을 더 빨리 채워요.",
            },
            new PremiumProduct
            {
                Id = FastSkip, Name = "빠른 진행", Price = 3900, Kind = ProductKind.Permanent, Where = "트레이닝 센터",
                Description = $"같은 훈련을 {FastSkipDays}일 연속으로 한 번에 진행해요. 결과는 하루씩 누른 것과 똑같아요.",
            },
            new PremiumProduct
            {
                Id = Tickets5, Name = "피로 회복권 5장", Price = 1900, Kind = ProductKind.Consumable, Where = "트레이닝 센터",
                Description = $"피로를 {DailyMissions.TicketRecovery}씩 풀어 훈련을 더 해요. 하루 {DailyMissions.MaxTicketsPerDay}장까지 쓸 수 있어요.",
            },
            new PremiumProduct
            {
                Id = Rewind, Name = "시즌 되감기권", Price = 2900, Kind = ProductKind.Consumable, Where = "시즌 결과 화면",
                Description = $"부상이나 부진으로 망한 시즌을 처음부터 다시 해요. 선수 한 명당 {BaseballSim.Career.Career.MaxRewinds}번까지.",
            },
            new PremiumProduct
            {
                Id = SupportSmall, Name = "응원 팩", Price = 1200, Kind = ProductKind.Support, Where = "야구 박물관",
                Description = "개발 응원! 후원자 카드 테두리를 드려요.",
            },
            new PremiumProduct
            {
                Id = SupportBig, Name = "큰 응원 팩", Price = 12000, Kind = ProductKind.Support, Where = "야구 박물관",
                Description = "크게 응원! 후원자 카드 테두리를 드려요.",
            },
        };

        public static PremiumProduct Find(string id) => All.FirstOrDefault(p => p.Id == id);

        /// <summary>Won price of a product or a money-sold cosmetic; 0 if unknown.</summary>
        public static int PriceOf(string id)
        {
            if (id.StartsWith(CosmeticPrefix)) return Cosmetics.Find(id.Substring(CosmeticPrefix.Length))?.Won ?? 0;
            return Find(id)?.Price ?? 0;
        }

        public static string PriceText(int won) => $"₩{won:N0}";

        static string Key(string id) => "premium_" + id;

        public static bool Owns(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

        /// <summary>The starter pack is gone once bought, or once its three parts are all owned.</summary>
        public static bool StarterPackAvailable =>
            !Owns(StarterPack) && !(Owns(RemoveAds) && Owns(ExtraSlots) && Owns(FastSkip));

        public static bool CanBuy(string id)
        {
            var p = Find(id);
            if (p == null) return false;
            if (id == StarterPack) return StarterPackAvailable;
            return p.Kind != ProductKind.Permanent || !Owns(id);
        }

        /// <summary>
        /// Buys a product and hands it over: permanent ones on the device, the rest into the club.
        /// The caller saves the club afterwards.
        /// </summary>
        public static void Buy(string id, Club club, System.Action<bool> onDone)
        {
            if (!CanBuy(id)) { onDone(false); return; }
            Monetization.Store.Buy(id, bought =>
            {
                if (bought) Grant(id, club);
                onDone(bought);
            });
        }

        /// <summary>Buys a cosmetic with money.</summary>
        public static void BuyCosmetic(string cosmeticId, Club club, System.Action<bool> onDone)
        {
            var c = Cosmetics.Find(cosmeticId);
            if (c == null || c.Won <= 0 || club.Owns(cosmeticId)) { onDone(false); return; }
            Monetization.Store.Buy(CosmeticPrefix + cosmeticId, bought =>
            {
                if (bought) club.GrantCosmetic(cosmeticId);
                onDone(bought);
            });
        }

        static void Grant(string id, Club club)
        {
            switch (id)
            {
                case StarterPack:
                    Remember(StarterPack);
                    Remember(RemoveAds);
                    Remember(ExtraSlots);
                    Remember(FastSkip);
                    break;
                case Tickets5: club.Daily.RecoveryTickets += 5; break;
                case Rewind: club.RewindTickets++; break;
                case SupportSmall:
                case SupportBig: club.GrantCosmetic(Cosmetics.SupporterFrame); break;
                default: Remember(id); break;
            }
        }

        static void Remember(string id)
        {
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
        }

        /// <summary>Test only: forget every permanent purchase, to try the flows again.</summary>
        public static void ResetPurchases()
        {
            foreach (var p in All) PlayerPrefs.DeleteKey(Key(p.Id));
            PlayerPrefs.Save();
        }
    }
}
