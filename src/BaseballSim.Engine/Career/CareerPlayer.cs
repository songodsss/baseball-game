using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class SeasonRecord
    {
        public int Season;
        public int Age;
        public BattingLine Line;
        public Dictionary<Stat, int> Ratings;
        public int Salary;
    }

    /// <summary>The one player the user is raising, with everything that persists between days.</summary>
    public class CareerPlayer
    {
        public string Id;
        public string Name;
        public int Age;

        /// <summary>Trained ratings, never above potential. Equipment adds on top during games.</summary>
        public Dictionary<Stat, int> Ratings = new Dictionary<Stat, int>();
        public Dictionary<Stat, int> Potential = new Dictionary<Stat, int>();
        public Dictionary<Stat, int> Exp = new Dictionary<Stat, int>();
        public Dictionary<Stat, int> PeakRatings = new Dictionary<Stat, int>();

        /// <summary>0-100. High fatigue hurts game form and training, and risks injury.</summary>
        public int Fatigue;
        public int InjuryDaysLeft;

        /// <summary>Spendable in-game money, 만원.</summary>
        public int Money;
        /// <summary>Season salary, 만원. Paid out game by game.</summary>
        public int Salary = 3000;

        public List<Equipment> Owned = new List<Equipment>();
        public Dictionary<EquipmentSlot, Equipment> Equipped = new Dictionary<EquipmentSlot, Equipment>();
        public List<SeasonRecord> History = new List<SeasonRecord>();

        public bool IsInjured => InjuryDaysLeft > 0;

        public static IEnumerable<Stat> AllStats => (Stat[])Enum.GetValues(typeof(Stat));

        public int EquipmentBonus(Stat stat) => Equipped.Values.Sum(e => e.Bonus.TryGetValue(stat, out var b) ? b : 0);

        public int EffectiveRating(Stat stat) => Math.Min(100, Ratings[stat] + EquipmentBonus(stat));

        /// <summary>Exp needed for the next point; higher ratings cost more.</summary>
        public static int ExpToNext(int rating) => 80 + (int)(rating * 1.6);

        /// <summary>Adds exp and converts it into rating points. Returns points gained.</summary>
        public int GainExp(Stat stat, int amount)
        {
            if (Ratings[stat] >= Potential[stat]) return 0;
            Exp[stat] += amount;
            int gained = 0;
            while (Ratings[stat] < Potential[stat] && Exp[stat] >= ExpToNext(Ratings[stat]))
            {
                Exp[stat] -= ExpToNext(Ratings[stat]);
                Ratings[stat]++;
                gained++;
            }
            if (Ratings[stat] >= Potential[stat]) Exp[stat] = 0;
            if (Ratings[stat] > PeakRatings[stat]) PeakRatings[stat] = Ratings[stat];
            return gained;
        }

        /// <summary>Builds the match-engine player for one game: ratings plus gear, form from fatigue.</summary>
        public Player ForGame(IRandom random)
        {
            var p = new Player(Id, Name);
            p.Batting.Contact = EffectiveRating(Stat.Contact);
            p.Batting.Power = EffectiveRating(Stat.Power);
            p.Batting.Eye = EffectiveRating(Stat.Eye);
            p.Batting.Speed = EffectiveRating(Stat.Speed);
            p.Batting.Defense = EffectiveRating(Stat.Defense);
            int form = (int)Math.Round(random.NextDouble() * 8 - 4);
            int tiredness = Math.Max(0, (Fatigue - 50) / 6);
            p.Condition = form - tiredness;
            return p;
        }

        public Dictionary<Stat, int> Snapshot() => new Dictionary<Stat, int>(Ratings);
    }
}
