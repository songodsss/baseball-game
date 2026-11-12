using System.Collections.Generic;

namespace BaseballSim.Career
{
    public enum EquipmentSlot
    {
        Bat,           // 배트
        BattingGloves, // 배팅 장갑
        Glove,         // 글러브
        Spikes,        // 스파이크
        Helmet,        // 헬멧
    }

    public enum Stat
    {
        Contact,
        Power,
        Eye,
        Speed,
        Defense,
        // Pitchers'
        Velocity,
        Control,
        Breaking,
        Stamina,
    }

    /// <summary>
    /// Gear bought with in-game salary only. Never sold for real money, so stats can't be bought.
    /// </summary>
    public class Equipment
    {
        public string Id;
        public string Name;
        public EquipmentSlot Slot;
        /// <summary>Price in 만원 of in-game salary.</summary>
        public int Price;
        public Dictionary<Stat, int> Bonus = new Dictionary<Stat, int>();
    }

    public static class EquipmentCatalog
    {
        public static readonly List<Equipment> All = Build();

        public static Equipment Find(string id) => All.Find(e => e.Id == id);

        static List<Equipment> Build()
        {
            var list = new List<Equipment>();
            // Four tiers per slot. Prices roughly track what a player earns: tier 2 after a
            // few weeks, tier 3 after a good season, tier 4 as a star.
            int[] prices = { 200, 800, 2500, 6000 };
            int[] bonuses = { 1, 2, 4, 6 };
            string[] tierNames = { "연습용", "프로용", "고급", "명품" };

            for (int tier = 0; tier < 4; tier++)
            {
                list.Add(Make($"bat{tier + 1}", $"{tierNames[tier]} 배트", EquipmentSlot.Bat, prices[tier],
                    (Stat.Power, bonuses[tier])));
                list.Add(Make($"bgloves{tier + 1}", $"{tierNames[tier]} 배팅 장갑", EquipmentSlot.BattingGloves, prices[tier],
                    (Stat.Contact, bonuses[tier])));
                list.Add(Make($"glove{tier + 1}", $"{tierNames[tier]} 글러브", EquipmentSlot.Glove, prices[tier],
                    (Stat.Defense, bonuses[tier])));
                list.Add(Make($"spikes{tier + 1}", $"{tierNames[tier]} 스파이크", EquipmentSlot.Spikes, prices[tier],
                    (Stat.Speed, bonuses[tier])));
                list.Add(Make($"helmet{tier + 1}", $"{tierNames[tier]} 헬멧", EquipmentSlot.Helmet, prices[tier],
                    (Stat.Eye, bonuses[tier])));
            }
            return list;
        }

        static Equipment Make(string id, string name, EquipmentSlot slot, int price, params (Stat Stat, int Amount)[] bonus)
        {
            var e = new Equipment { Id = id, Name = name, Slot = slot, Price = price };
            foreach (var b in bonus) e.Bonus[b.Stat] = b.Amount;
            return e;
        }
    }
}
