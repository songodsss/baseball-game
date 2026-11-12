using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum Rarity { N, R, SR, SSR }

    /// <summary>The two draw boxes: training cards for the player I'm raising, team gear for my team.</summary>
    public enum GachaBox { Cards, Gear }

    /// <summary>
    /// A training card: worn by the player I'm raising, it makes training of one stat give more exp.
    /// It never adds rating points by itself, so a card speeds growth up but the player still has to train.
    /// One card per stat and rarity; drawing it again levels it up.
    /// </summary>
    public class TrainingCard
    {
        public string Id;
        public string Name;
        public Stat Stat;
        public Rarity Rarity;
    }

    /// <summary>A piece of team gear: my retired players all play one stat higher with it on.</summary>
    public class TeamGear
    {
        public string Id;
        public string Name;
        public Stat Stat;
        public Rarity Rarity;
    }

    /// <summary>One thing that came out of a draw.</summary>
    public class DrawResult
    {
        public GachaBox Box;
        public string Id;
        public string Name;
        public Rarity Rarity;
        /// <summary>First copy.</summary>
        public bool New;
        /// <summary>Level after this copy.</summary>
        public int Level;
        /// <summary>This copy raised the level (cards: every copy does; gear: once enough copies are in).</summary>
        public bool LevelUp;
        /// <summary>Coins given back for a copy of something already at the top level.</summary>
        public int Refund;
        /// <summary>The SSR came from the pity counter.</summary>
        public bool FromPity;
        /// <summary>A pick ticket for an SSR of the player's choice (Id is null until it is used).</summary>
        public bool Pick;
    }

    /// <summary>
    /// Draw boxes. Published odds (shown in the game, as Korean law asks), a pity counter that guarantees
    /// an SSR of the player's choice within <see cref="Pity"/> draws, and at least one SR or better in every ten-draw.
    /// Draws are paid in club coins, which every player earns by playing, or in draw tickets that can
    /// also be bought; money only makes it faster.
    /// </summary>
    public static class Gacha
    {
        public const int SingleCoins = 150, TenCoins = 1350;
        public const int Pity = 80;
        public const int MaxLevel = 5;
        public const int CardSlots = 3;

        /// <summary>Odds in percent, N to SSR; they add up to 100.</summary>
        public static readonly double[] Odds = { 60, 30, 8.5, 1.5 };
        static readonly int[] CardPercent = { 20, 40, 70, 100 };
        const int CardPercentPerLevel = 15;
        static readonly int[] GearPoints = { 1, 2, 3, 5 };
        static readonly int[] RefundCoins = { 10, 30, 100, 300 };

        public static string RarityName(Rarity r) => r.ToString();

        public static string IdOf(GachaBox box, Stat stat, Rarity rarity) =>
            box == GachaBox.Cards ? Cards.First(c => c.Stat == stat && c.Rarity == rarity).Id : Gear.First(g => g.Stat == stat && g.Rarity == rarity).Id;

        static readonly Stat[] AllStats = (Stat[])Enum.GetValues(typeof(Stat));

        static string CardName(Stat s, Rarity r)
        {
            string stat = Stats.Name(s);
            switch (r)
            {
                case Rarity.N: return $"{stat} 연습 노트";
                case Rarity.R: return $"{stat} 훈련 영상";
                case Rarity.SR: return $"{stat} 전담 코치";
                default: return $"레전드의 {stat} 비법서";
            }
        }

        static readonly Dictionary<Stat, string> GearItem = new Dictionary<Stat, string>
        {
            { Stat.Contact, "배팅 장갑" }, { Stat.Power, "배트" }, { Stat.Eye, "고글" }, { Stat.Speed, "스파이크" },
            { Stat.Defense, "글러브" }, { Stat.Velocity, "투구 슬리브" }, { Stat.Control, "로진백" },
            { Stat.Breaking, "공인구 연습구" }, { Stat.Stamina, "컨디셔닝 밴드" },
        };

        static string GearName(Stat s, Rarity r)
        {
            string item = GearItem[s];
            switch (r)
            {
                case Rarity.N: return $"보급형 {item}";
                case Rarity.R: return $"선수용 {item}";
                case Rarity.SR: return $"프로 {item}";
                default: return $"명품 {item}";
            }
        }

        public static readonly List<TrainingCard> Cards =
            AllStats.SelectMany(s => ((Rarity[])Enum.GetValues(typeof(Rarity))).Select(r =>
                new TrainingCard { Id = $"card_{s}_{r}".ToLowerInvariant(), Name = CardName(s, r), Stat = s, Rarity = r })).ToList();

        public static readonly List<TeamGear> Gear =
            AllStats.SelectMany(s => ((Rarity[])Enum.GetValues(typeof(Rarity))).Select(r =>
                new TeamGear { Id = $"gear_{s}_{r}".ToLowerInvariant(), Name = GearName(s, r), Stat = s, Rarity = r })).ToList();

        public static TrainingCard Card(string id) => Cards.FirstOrDefault(c => c.Id == id);
        public static TeamGear GearPiece(string id) => Gear.FirstOrDefault(g => g.Id == id);

        /// <summary>
        /// Extra training exp of a card at a level, in percent: N 20, R 40, SR 70, SSR 100, +15 per level above 1
        /// (SSR Lv.5 = 160%). Growth gets steep near the top, so this is what it takes to show: over a 14-season
        /// career one SSR Lv.5 is worth about +7 in its stat, two on the same stat about +12.
        /// </summary>
        public static int CardBonus(Rarity r, int level) => CardPercent[(int)r] + CardPercentPerLevel * Math.Max(0, Math.Min(MaxLevel, level) - 1);

        /// <summary>Rating points a gear piece adds to my team at a level: base by rarity, +1 per level above 1.</summary>
        public static int GearBonus(Rarity r, int level) => GearPoints[(int)r] + Math.Max(0, Math.Min(GearMaxLevel(r), level) - 1);

        /// <summary>Top level of a gear piece: every level is +1, up to +4 (N), +6 (R), +8 (SR), +10 (SSR).</summary>
        public static int GearMaxLevel(Rarity r) => GearTop[(int)r] - GearPoints[(int)r] + 1;
        static readonly int[] GearTop = { 4, 6, 8, 10 };

        public static int MaxLevelOf(GachaBox box, Rarity r) => box == GachaBox.Cards ? MaxLevel : GearMaxLevel(r);

        /// <summary>
        /// Copies of one gear piece needed for a level: each level asks one more copy than the last
        /// (Lv.2 one more, Lv.3 two more, Lv.4 three more...), so 1, 2, 4, 7, 11, 16 in all.
        /// </summary>
        public static int GearCopiesFor(int level) => level <= 0 ? 0 : 1 + level * (level - 1) / 2;

        /// <summary>Level a gear piece has with this many copies.</summary>
        public static int GearLevelFrom(Rarity r, int copies)
        {
            int level = 0;
            while (level < GearMaxLevel(r) && copies >= GearCopiesFor(level + 1)) level++;
            return level;
        }

        public static int Refund(Rarity r) => RefundCoins[(int)r];

        public static int Cost(int count) => count >= 10 ? TenCoins : SingleCoins * count;

        /// <summary>Lines for the odds table, from SSR down: rarity, percent, how many items share it.</summary>
        public static IEnumerable<(Rarity Rarity, double Percent, int Items)> OddsTable(GachaBox box)
        {
            for (int k = Odds.Length - 1; k >= 0; k--)
                yield return ((Rarity)k, Odds[k], AllStats.Length);
        }

        static Rarity Roll(IRandom random, Rarity atLeast)
        {
            double total = 0;
            for (int k = (int)atLeast; k < Odds.Length; k++) total += Odds[k];
            double roll = random.NextDouble() * total;
            for (int k = (int)atLeast; k < Odds.Length; k++)
            {
                roll -= Odds[k];
                if (roll < 0) return (Rarity)k;
            }
            return Rarity.SSR;
        }

        /// <summary>
        /// Draws 1 or 10. Uses draw tickets first when <paramref name="useTickets"/> (one per draw),
        /// otherwise coins. Returns null if there is not enough to pay.
        /// </summary>
        public static List<DrawResult> Draw(Club club, GachaBox box, int count, IRandom random, bool useTickets = false)
        {
            count = count >= 10 ? 10 : 1;
            if (useTickets)
            {
                if (club.DrawTickets < count) return null;
                club.DrawTickets -= count;
            }
            else
            {
                if (club.Coins < Cost(count)) return null;
                club.Coins -= Cost(count);
            }

            var results = new List<DrawResult>();
            for (int k = 0; k < count; k++)
            {
                int sincePity = club.PityCount(box) + 1;
                bool pity = sincePity >= Pity;
                bool lastOfTen = count == 10 && k == count - 1 && results.All(x => x.Rarity < Rarity.SR);
                var rarity = pity ? Rarity.SSR : Roll(random, lastOfTen ? Rarity.SR : Rarity.N);
                club.SetPityCount(box, rarity == Rarity.SSR ? 0 : sincePity);
                if (pity)
                {
                    // The guaranteed SSR is the player's pick, so draws always add up to something wanted.
                    club.SsrPicks[(int)box]++;
                    results.Add(new DrawResult { Box = box, Rarity = Rarity.SSR, Name = "SSR 선택권", FromPity = true, Pick = true });
                    continue;
                }
                var stat = AllStats[Math.Min(AllStats.Length - 1, (int)(random.NextDouble() * AllStats.Length))];
                string id = box == GachaBox.Cards ? Cards.First(c => c.Stat == stat && c.Rarity == rarity).Id
                                                  : Gear.First(g => g.Stat == stat && g.Rarity == rarity).Id;
                results.Add(club.AddCopy(box, id, rarity, pity));
            }
            club.Draws += count;
            return results;
        }
    }
}
