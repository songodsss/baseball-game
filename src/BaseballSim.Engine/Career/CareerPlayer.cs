using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class SeasonRecord
    {
        public int Season;
        public int Age;
        /// <summary>Season length the player picked for this year.</summary>
        public int Games;
        public BattingLine Line;
        /// <summary>A pitcher's season; null for hitters.</summary>
        public PitcherLine Pitching;
        public Dictionary<Stat, int> Ratings;
        public int Salary;
        /// <summary>My titles this season, e.g. "홈런왕", and the championship if won.</summary>
        public List<string> Titles = new List<string>();
        public bool InjuryFree;
        /// <summary>Club played for this season; null in saves from before free agency.</summary>
        public string Club;
        /// <summary>Moves this season: "5월 2군 강등", trades, a release.</summary>
        public List<string> Notes = new List<string>();
        /// <summary>Days spent in 2군.</summary>
        public int FarmDays;
        /// <summary>A season of military service.</summary>
        public bool Military;
        /// <summary>The rival's batting that season; null before rivals existed.</summary>
        public BattingLine RivalLine;
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

        public const int MaxMorale = 10;
        /// <summary>-10..+10, from story events. Adds to game form and fades by one a day.</summary>
        public int Morale;

        /// <summary>Spendable in-game money, 만원.</summary>
        public int Money;
        /// <summary>Season salary, 만원. Paid out game by game.</summary>
        public int Salary = 3000;

        public List<Equipment> Owned = new List<Equipment>();
        public Dictionary<EquipmentSlot, Equipment> Equipped = new Dictionary<EquipmentSlot, Equipment>();
        public List<SeasonRecord> History = new List<SeasonRecord>();
        /// <summary>Level 0-3 of each facility; missing means 0.</summary>
        public Dictionary<Facility, int> Facilities = new Dictionary<Facility, int>();
        /// <summary>League legends this player has met in story events, so each visits once.</summary>
        public List<string> MetLegends = new List<string>();
        /// <summary>Achievement ids unlocked in this career, in the order reached.</summary>
        public List<string> Achievements = new List<string>();
        /// <summary>Clubs moved to by trade, free agency or tryout, in order. Empty means a one-club career.</summary>
        public List<string> Clubs = new List<string>();
        public int AllStarSelections;
        /// <summary>Steals over the whole career (season lines in history don't keep them).</summary>
        public int CareerStolenBases;
        public int AllStarMvps;
        /// <summary>Big at bats where the pitch I sat on came.</summary>
        public int ReadsRight;
        public int GoldMedals;
        public Position Position = Position.Outfield;
        /// <summary>Jersey number, 1-99; 0 = none picked.</summary>
        public int Number;
        /// <summary>Trait ids picked, in order.</summary>
        public List<string> Traits = new List<string>();
        /// <summary>Exp put into a stat already at its potential; enough of it raises the potential.</summary>
        public Dictionary<Stat, int> BreakthroughExp = new Dictionary<Stat, int>();
        public Dictionary<Stat, int> Breakthroughs = new Dictionary<Stat, int>();
        /// <summary>Stats whose potential went up since the last day result took them.</summary>
        public List<Stat> NewBreakthroughs = new List<Stat>();

        public const int BreakthroughGain = 3;
        public const int MaxBreakthroughs = 4;
        public int BreakthroughCount(Stat stat) => Breakthroughs.TryGetValue(stat, out var n) ? n : 0;
        /// <summary>Exp to break through a stat's limit: grows with each breakthrough.</summary>
        public int BreakthroughNeed(Stat stat) => 600 + 300 * BreakthroughCount(stat);
        public int BreakthroughProgress(Stat stat) => BreakthroughExp.TryGetValue(stat, out var e) ? e : 0;

        public int FacilityLevel(Facility f) => Facilities.TryGetValue(f, out var level) ? level : 0;

        public bool IsInjured => InjuryDaysLeft > 0;

        public bool IsPitcher => Position == Position.Pitcher;
        /// <summary>A pitcher's job, picked at registration: starter, setup man or closer.</summary>
        public PitcherRole Role = PitcherRole.Starter;

        /// <summary>A hitter's five stats.</summary>
        public static IEnumerable<Stat> AllStats => HitterStats;
        public static readonly Stat[] HitterStats = { Stat.Contact, Stat.Power, Stat.Eye, Stat.Speed, Stat.Defense };
        public static readonly Stat[] PitcherStats = { Stat.Velocity, Stat.Control, Stat.Breaking, Stat.Stamina };
        /// <summary>The stats this player trains: a hitter's five or a pitcher's four.</summary>
        public IEnumerable<Stat> MyStats => IsPitcher ? PitcherStats : HitterStats;

        /// <summary>
        /// The pitcher's counterpart of a hitter stat, for things written for hitters (story events,
        /// gear): contact→control, power→velocity, eye→breaking ball, speed→stamina, glove→control,
        /// and the other way round for a hitter.
        /// </summary>
        public Stat RoleStat(Stat stat)
        {
            if (!IsPitcher)
            {
                // And back the other way for a hitter.
                switch (stat)
                {
                    case Stat.Control: return Stat.Contact;
                    case Stat.Velocity: return Stat.Power;
                    case Stat.Breaking: return Stat.Eye;
                    case Stat.Stamina: return Stat.Speed;
                    default: return stat;
                }
            }
            switch (stat)
            {
                case Stat.Contact: case Stat.Defense: return Stat.Control;
                case Stat.Power: return Stat.Velocity;
                case Stat.Eye: return Stat.Breaking;
                case Stat.Speed: return Stat.Stamina;
                default: return stat;
            }
        }

        public int EquipmentBonus(Stat stat) => Equipped.Values.Sum(e => e.Bonus.Where(b => RoleStat(b.Key) == stat).Sum(b => b.Value));

        /// <summary>Game-day rating: effective rating less what a recent injury still takes away.</summary>
        public int GameRating(Stat stat) => Math.Max(1, EffectiveRating(stat) - InjuryPenalty(stat));

        /// <summary>The last injury; it lingers for a while after the player is back.</summary>
        public InjuryKind Injury;
        /// <summary>Days after coming back that the last injury still costs a little.</summary>
        public int LingerDays;
        public int InjuryPenalty(Stat stat) =>
            !IsInjured && LingerDays > 0 && RoleStat(Injuries.StatOf(Injury)) == stat ? Injuries.Penalty(Injury) : 0;

        /// <summary>Fan points; popularity is a 0-100 view of them.</summary>
        public int FanPoints;
        public int Popularity => Math.Min(100, FanPoints / Career.FanPointsPerPopularity);

        public int EffectiveRating(Stat stat) => Math.Min(100, Ratings[stat] + EquipmentBonus(stat) + BaseballSim.Career.Traits.BoostFor(this, stat));

        /// <summary>Exp needed for the next point; higher ratings cost more.</summary>
        public static int ExpToNext(int rating) => 80 + (int)(rating * 1.6) + rating * rating * 3 / 100;

        /// <summary>Adds exp and converts it into rating points. Returns points gained.</summary>
        public int GainExp(Stat stat, int amount)
        {
            if (Ratings[stat] >= Potential[stat])
            {
                BreakThrough(stat, amount);
                return 0;
            }
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

        /// <summary>
        /// Training past the limit isn't wasted: it fills a gauge, and a full gauge lifts the potential
        /// by 3, up to four times a stat. Effort can make up for an unlucky roll.
        /// </summary>
        void BreakThrough(Stat stat, int amount)
        {
            if (BreakthroughCount(stat) >= MaxBreakthroughs || Potential[stat] >= 99 || amount <= 0) return;
            BreakthroughExp[stat] = BreakthroughProgress(stat) + amount;
            if (BreakthroughExp[stat] < BreakthroughNeed(stat)) return;
            BreakthroughExp[stat] = 0;
            Breakthroughs[stat] = BreakthroughCount(stat) + 1;
            Potential[stat] = Math.Min(99, Potential[stat] + BreakthroughGain);
            NewBreakthroughs.Add(stat);
        }

        /// <summary>Builds the match-engine player for one game: ratings plus gear, form from fatigue.</summary>
        public Player ForGame(IRandom random)
        {
            var p = new Player(Id, Name);
            int form = (int)Math.Round(random.NextDouble() * 8 - 4);
            int tiredness = Math.Max(0, (Fatigue - 50) / 6);
            p.Condition = form - tiredness + Morale / 2;
            p.Traits = new HashSet<string>(Traits);
            if (IsPitcher)
            {
                p.Pitching = new PitchingRatings
                {
                    Velocity = GameRating(Stat.Velocity), Control = GameRating(Stat.Control),
                    Breaking = GameRating(Stat.Breaking), Stamina = GameRating(Stat.Stamina),
                };
                p.Batting.Contact = p.Batting.Power = p.Batting.Eye = p.Batting.Speed = 25;
                p.Batting.Defense = 50;
                return p;
            }
            p.Batting.Contact = GameRating(Stat.Contact);
            p.Batting.Power = GameRating(Stat.Power);
            p.Batting.Eye = GameRating(Stat.Eye);
            p.Batting.Speed = GameRating(Stat.Speed);
            // A designated hitter doesn't field: his glove neither helps nor hurts the team.
            p.Batting.Defense = Position == Position.DesignatedHitter ? 50 : GameRating(Stat.Defense);
            return p;
        }

        public Dictionary<Stat, int> Snapshot() => new Dictionary<Stat, int>(Ratings);
    }
}
