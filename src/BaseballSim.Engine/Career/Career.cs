using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum DailyAction
    {
        TrainContact, // 타격 훈련
        TrainPower,   // 웨이트 트레이닝
        TrainEye,     // 선구안 훈련
        TrainSpeed,   // 주루 훈련
        TrainDefense, // 수비 훈련
        Rest,         // 휴식
    }

    public class DayResult
    {
        public int Season;
        public int Day;
        public DailyAction Action;
        /// <summary>Rating points gained today, from training and from the game.</summary>
        public Dictionary<Stat, int> PointsGained = new Dictionary<Stat, int>();
        public bool GotInjured;
        public bool SatOut;
        public GameResult MyGame;
        public Player MyGamePlayer;
        public BattingLine MyLine;
        public int MoneyEarned;
        /// <summary>A story event to answer before the next day, or null.</summary>
        public StoryEvent Event;
    }

    public class SeasonSummary
    {
        public SeasonRecord Record;
        public int Rank;
        public int OldSalary;
        public int NewSalary;
        public Dictionary<Stat, int> Decline = new Dictionary<Stat, int>();
    }

    /// <summary>
    /// One player's career, a game day at a time: pick a daily action, then the league plays
    /// its games. All tuning numbers for growth live in this class.
    /// </summary>
    public class Career
    {
        public const int MinSalary = 3000;
        public const int RetirementAge = 40;

        // Training and fatigue, per day of a full 144-game season.
        const int TrainMainExp = 10;
        const int TrainSideExp = 3;
        const int TrainFatigue = 15;
        const int GameFatigue = 6;
        const int RestRecovery = 35;
        const int DailyRecovery = 5;
        const int TiredThreshold = 80;
        const double InjuryChanceWhenTired = 0.08;
        /// <summary>Story events per day of a full season; about 8-9 a season whatever its length.</summary>
        const double EventChancePerDay = 0.06;

        public CareerPlayer Me;
        public League League;
        public Team MyTeam;
        public int Season = 1;
        public int Day;
        public BattingLine SeasonLine = new BattingLine();

        /// <summary>My player bats here; the regular in that spot sits for the game.</summary>
        public int LineupSlot = 5;

        /// <summary>A retired player from my club coaching this career; boosts training in their best stats.</summary>
        public RetiredPlayer Coach;

        /// <summary>Games in a row with at bats and no hit. Story events react to slumps.</summary>
        public int HitlessStreak;

        /// <summary>Set by PlayDay now and then; answer it with ResolveEvent. The next PlayDay drops it.</summary>
        public StoryEvent PendingEvent { get; private set; }

        readonly IRandom _random;
        readonly GameSimulator _sim;

        public Career(CareerPlayer me, League league, Team myTeam, IRandom random)
        {
            Me = me;
            League = league;
            MyTeam = myTeam;
            _random = random;
            _sim = new GameSimulator(random);
        }

        public bool IsSeasonOver => Day >= League.GamesPerSeason;

        /// <summary>
        /// In a shorter season each day's growth and pay count for more (x2 at 72 games, x4 at 36),
        /// so a player peaks around age 26 whichever length is picked.
        /// </summary>
        int DayWeight => League.FullSeason / League.GamesPerSeason;
        public bool MustRetire => Me.Age >= RetirementAge;

        /// <summary>Creates a fresh 18-year-old with random starting ratings and potential.</summary>
        public static CareerPlayer NewPlayer(string name, IRandom random, int potentialBonus = 0)
        {
            var me = new CareerPlayer { Id = "me", Name = name, Age = 18 };
            foreach (var stat in CareerPlayer.AllStats)
            {
                int start = 30 + (int)(random.NextDouble() * 16);
                int potential = (int)Math.Round(Normal(random, 68, 10)) + potentialBonus;
                potential = Math.Max(start + 10, Math.Min(99, potential));
                me.Ratings[stat] = start;
                me.PeakRatings[stat] = start;
                me.Potential[stat] = potential;
                me.Exp[stat] = 0;
            }
            return me;
        }

        public DayResult PlayDay(DailyAction action)
        {
            if (IsSeasonOver) throw new InvalidOperationException("Season is over; call EndSeason first.");

            var result = new DayResult { Season = Season, Day = Day, Action = action };
            if (Me.IsInjured) result.Action = action = DailyAction.Rest;
            PendingEvent = null;

            ApplyAction(action, result);

            foreach (var (away, home) in League.Matchups(Day))
            {
                if (away == MyTeam || home == MyTeam) PlayMyGame(away, home, result);
                else League.Record(_sim.Play(away, home, League.NextStarter(away), League.NextStarter(home)));
            }

            Me.Fatigue = Math.Max(0, Me.Fatigue - DailyRecovery);
            if (Me.InjuryDaysLeft > 0) Me.InjuryDaysLeft--;
            Me.Morale -= Math.Sign(Me.Morale);
            Day++;

            if (_random.NextDouble() < Math.Min(0.5, EventChancePerDay * DayWeight))
                PendingEvent = StoryEvents.Pick(this, result, _random);
            result.Event = PendingEvent;
            return result;
        }

        /// <summary>Applies the chosen answer to the pending story event.</summary>
        public EventOutcome ResolveEvent(int choice)
        {
            if (PendingEvent == null) throw new InvalidOperationException("No story event to resolve.");
            var picked = PendingEvent.Choices[choice];
            PendingEvent = null;
            var outcome = new EventOutcome { Text = picked.Result };
            picked.Apply(new EventEffects(this, _random, outcome));
            return outcome;
        }

        /// <summary>Extra training exp from the coach for this stat: up to +30% from a coach whose peak was in the 80s or better.</summary>
        public double CoachBonus(Stat stat)
        {
            if (Coach == null || !Coach.PeakRatings.TryGetValue(stat, out var peak)) return 0;
            return Math.Max(0, Math.Min(0.3, (peak - 50) / 100.0));
        }

        void ApplyAction(DailyAction action, DayResult result)
        {
            if (action == DailyAction.Rest)
            {
                Me.Fatigue = Math.Max(0, Me.Fatigue - RestRecovery);
                return;
            }

            bool tired = Me.Fatigue >= TiredThreshold;
            if (tired && _random.NextDouble() < InjuryChanceWhenTired)
            {
                Me.InjuryDaysLeft = (int)Math.Ceiling((5 + _random.NextDouble() * 10) / DayWeight);
                result.GotInjured = true;
                return;
            }

            var (main, side) = TrainingTargets(action);
            double mult = AgeGrowth(Me.Age) * (tired ? 0.5 : 1.0);
            AddPoints(result, main, Me.GainExp(main, (int)Math.Round(TrainMainExp * DayWeight * mult * (1 + CoachBonus(main)))));
            AddPoints(result, side, Me.GainExp(side, (int)Math.Round(TrainSideExp * DayWeight * mult)));
            Me.Fatigue = Math.Min(100, Me.Fatigue + TrainFatigue);
        }

        static (Stat Main, Stat Side) TrainingTargets(DailyAction action)
        {
            switch (action)
            {
                case DailyAction.TrainContact: return (Stat.Contact, Stat.Eye);
                case DailyAction.TrainPower: return (Stat.Power, Stat.Contact);
                case DailyAction.TrainEye: return (Stat.Eye, Stat.Contact);
                case DailyAction.TrainSpeed: return (Stat.Speed, Stat.Defense);
                default: return (Stat.Defense, Stat.Speed);
            }
        }

        static double AgeGrowth(int age) => age <= 22 ? 1.3 : age <= 27 ? 1.0 : age <= 31 ? 0.7 : 0.4;

        static void AddPoints(DayResult result, Stat stat, int points)
        {
            if (points == 0) return;
            result.PointsGained.TryGetValue(stat, out var p);
            result.PointsGained[stat] = p + points;
        }

        void PlayMyGame(Team away, Team home, DayResult result)
        {
            var awayStarter = League.NextStarter(away);
            var homeStarter = League.NextStarter(home);

            if (Me.IsInjured)
            {
                result.SatOut = true;
                result.MyGame = _sim.Play(away, home, awayStarter, homeStarter);
                League.Record(result.MyGame);
                return;
            }

            var gamePlayer = Me.ForGame(_random);
            var regular = MyTeam.Lineup[LineupSlot];
            MyTeam.Lineup[LineupSlot] = gamePlayer;
            try
            {
                result.MyGame = _sim.Play(away, home, awayStarter, homeStarter);
            }
            finally
            {
                MyTeam.Lineup[LineupSlot] = regular;
            }
            League.Record(result.MyGame);

            result.MyGamePlayer = gamePlayer;
            result.MyLine = result.MyGame.Batting.TryGetValue(gamePlayer, out var line) ? line : new BattingLine();
            if (result.MyLine.AtBats > 0) HitlessStreak = result.MyLine.Hits == 0 ? HitlessStreak + 1 : 0;
            Accumulate(SeasonLine, result.MyLine);
            GainGameExp(result);
            PayForGame(result);
            Me.Fatigue = Math.Min(100, Me.Fatigue + GameFatigue);
        }

        // Every game gives some exp, even an 0-for-4, so a bad day never feels wasted.
        void GainGameExp(DayResult result)
        {
            var l = result.MyLine;
            double mult = AgeGrowth(Me.Age) * DayWeight;
            int extraBase = l.Doubles + l.Triples;
            int Exp(double raw) => (int)Math.Round(raw * mult);

            AddPoints(result, Stat.Defense, Me.GainExp(Stat.Defense, Exp(2)));
            AddPoints(result, Stat.Contact, Me.GainExp(Stat.Contact, Exp(1 + 2 * l.Hits)));
            AddPoints(result, Stat.Power, Me.GainExp(Stat.Power, Exp(2 * extraBase + 4 * l.HomeRuns)));
            AddPoints(result, Stat.Eye, Me.GainExp(Stat.Eye, Exp(1 + 2 * l.Walks)));
            AddPoints(result, Stat.Speed, Me.GainExp(Stat.Speed, Exp(l.Runs + 2 * l.Triples)));
        }

        void PayForGame(DayResult result)
        {
            var l = result.MyLine;
            int pay = Me.Salary / League.GamesPerSeason + DayWeight * (5 * l.Hits + 30 * l.HomeRuns + 5 * l.RunsBattedIn);
            Me.Money += pay;
            result.MoneyEarned = pay;
        }

        /// <summary>Ages the player, renegotiates salary on this season's OPS, and starts a new season.</summary>
        public SeasonSummary EndSeason()
        {
            var ranked = League.Ranked();
            var summary = new SeasonSummary
            {
                Rank = ranked.FindIndex(s => s.Team == MyTeam) + 1,
                OldSalary = Me.Salary,
            };

            summary.Record = new SeasonRecord
            {
                Season = Season,
                Age = Me.Age,
                Games = League.GamesPerSeason,
                Line = SeasonLine,
                Ratings = Me.Snapshot(),
                Salary = Me.Salary,
            };
            Me.History.Add(summary.Record);

            // Salary moves halfway toward what this season's OPS is worth, so one fluke year
            // doesn't make or break a contract. OPS .800 is worth about 8,500, .950 about 24,000.
            double ops = Ops(SeasonLine);
            double worth = MinSalary * Math.Exp((ops - 0.650) * 7);
            int next = (int)((Me.Salary + worth) / 2 / 100) * 100;
            Me.Salary = Math.Max(MinSalary, Math.Min(250000, next));
            summary.NewSalary = Me.Salary;

            Me.Age++;
            if (Me.Age >= 32)
            {
                foreach (var stat in CareerPlayer.AllStats)
                {
                    int drop = (int)(_random.NextDouble() * (Me.Age - 30));
                    if (stat == Stat.Speed) drop += 1; // legs go first
                    int before = Me.Ratings[stat];
                    // The ceiling drops too, or training would simply win the points back.
                    Me.Potential[stat] = Math.Max(20, Me.Potential[stat] - drop);
                    Me.Ratings[stat] = Math.Max(20, Math.Min(Me.Potential[stat], before - drop));
                    if (before != Me.Ratings[stat]) summary.Decline[stat] = before - Me.Ratings[stat];
                }
            }

            Season++;
            Day = 0;
            SeasonLine = new BattingLine();
            League.ResetStandings();
            return summary;
        }

        /// <summary>The player can switch between 144, 72 and 36 games before any season starts.</summary>
        public bool ChangeSeasonLength(int games)
        {
            if (Day != 0 || Array.IndexOf(League.SeasonLengths, games) < 0) return false;
            League.SetSeasonLength(games);
            return true;
        }

        public bool Buy(string equipmentId)
        {
            var item = EquipmentCatalog.Find(equipmentId);
            if (item == null || Me.Owned.Contains(item) || Me.Money < item.Price) return false;
            Me.Money -= item.Price;
            Me.Owned.Add(item);
            Me.Equipped[item.Slot] = item;
            return true;
        }

        /// <summary>Hook for rewarded ads and time-skip purchases: recover fatigue without spending a day.</summary>
        public void RecoverFatigue(int amount) => Me.Fatigue = Math.Max(0, Me.Fatigue - amount);

        public static double Ops(BattingLine l)
        {
            if (l.PlateAppearances == 0 || l.AtBats == 0) return 0;
            double obp = (double)(l.Hits + l.Walks) / l.PlateAppearances;
            double slg = (double)(l.Hits + l.Doubles + 2 * l.Triples + 3 * l.HomeRuns) / l.AtBats;
            return obp + slg;
        }

        public static void Accumulate(BattingLine total, BattingLine add)
        {
            total.PlateAppearances += add.PlateAppearances; total.AtBats += add.AtBats; total.Hits += add.Hits;
            total.Doubles += add.Doubles; total.Triples += add.Triples; total.HomeRuns += add.HomeRuns;
            total.Walks += add.Walks; total.Strikeouts += add.Strikeouts; total.Runs += add.Runs;
            total.RunsBattedIn += add.RunsBattedIn;
        }

        static double Normal(IRandom random, double mean, double sd)
        {
            double u1 = 1 - random.NextDouble();
            double u2 = random.NextDouble();
            return mean + sd * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }
    }
}
