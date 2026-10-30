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
        public List<Achievement> NewAchievements = new List<Achievement>();
        /// <summary>Played in 2군 today; FarmLine is my batting there (null if hurt).</summary>
        public bool InFarm;
        public BattingLine FarmLine;
        /// <summary>Sent down, called up or traded today, or null.</summary>
        public RosterMove Move;
        /// <summary>A trade offer to answer before the next day, or null.</summary>
        public TradeOffer Trade;
        /// <summary>A big at bat waiting for <see cref="Career.ChooseApproach"/>; null once the day is done.</summary>
        public AtBatSituation AtBat;
        /// <summary>Stats whose potential went up today through training past the limit.</summary>
        public List<Stat> Breakthroughs = new List<Stat>();
        /// <summary>A trait slot opened: pick one with ChooseTrait. Stays on offer until picked.</summary>
        public List<Trait> TraitOffer;
        /// <summary>Set when the batting order changed today: the slot I batted in before. LineupSlot is the new one.</summary>
        public int? OldSlot;
        /// <summary>My big at bats so far today with the approach taken, e.g. "7회 ... 2루타 (직구 노림 적중!)".</summary>
        public List<string> AtBatLog = new List<string>();
        /// <summary>The all-star game, played on the midseason day; null on other days.</summary>
        public AllStarResult AllStar;
    }

    public class SeasonSummary
    {
        public SeasonRecord Record;
        public int Rank;
        public int OldSalary;
        public int NewSalary;
        public Dictionary<Stat, int> Decline = new Dictionary<Stat, int>();
        public List<SeasonAward> Awards = new List<SeasonAward>();
        public PostseasonResult Postseason;
        /// <summary>Free-agency offers, my club's first. Empty unless the player is a free agent this winter.</summary>
        public List<ContractOffer> Offers = new List<ContractOffer>();
        /// <summary>Let go by the club; Offers are tryouts, and none means the career is over.</summary>
        public bool Released;
        public List<Achievement> NewAchievements = new List<Achievement>();
    }

    /// <summary>A club's offer to a free agent: salary for each season of the deal.</summary>
    public class ContractOffer
    {
        public Team Team;
        public int Salary;
        public int Years;
        /// <summary>A 1-year chance after a release: starts in 2군, salary renegotiated as usual.</summary>
        public bool Tryout;
    }

    /// <summary>
    /// One player's career, a game day at a time: pick a daily action, then the league plays
    /// its games. All tuning numbers for growth live in this class.
    /// </summary>
    public partial class Career
    {
        public const int MinSalary = 3000;
        public const int MaxSalary = 250000;
        /// <summary>Seasons played before the player becomes a free agent.</summary>
        public const int FreeAgencySeasons = 8;
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

        /// <summary>Every career starts in 2026: each player gets their own timeline rather than following the last one's.</summary>
        public const int StartYear = 2026;
        public static int YearOf(int season) => StartYear + season - 1;
        public int Year => YearOf(Season);
        public int Day;
        public BattingLine SeasonLine = new BattingLine();

        /// <summary>My player bats here (0 = leadoff); the regular in that spot sits. Rookies start 9th; see BattingOrder.</summary>
        public int LineupSlot = 8;

        /// <summary>Games in a row with at bats and no hit. Story events react to slumps.</summary>
        public int HitlessStreak;

        /// <summary>Missed at least one game hurt this season.</summary>
        public bool InjuredThisSeason;

        /// <summary>Seasons left on a signed deal; salary is fixed until it runs out.</summary>
        public int ContractSeasonsLeft;

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
        public bool MustRetire => Me.Age >= RetirementAge || Released && PendingOffers.Count == 0;

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
            if (Released) throw new InvalidOperationException("Released; sign a tryout offer or retire.");
            if (IsMidGame) throw new InvalidOperationException("Answer the big at bat first.");
            PendingOffers.Clear(); // free-agency offers left unanswered: stays on a yearly deal
            PendingTrade = null;

            var result = new DayResult { Season = Season, Day = Day, Action = action };
            if (Me.IsInjured) result.Action = action = DailyAction.Rest;
            PendingEvent = null;

            ApplyAction(action, result);

            (Team Away, Team Home)? myGame = null;
            foreach (var (away, home) in League.Matchups(Day))
            {
                if ((away == MyTeam || home == MyTeam) && InFarm) PlayFarmGame(away, home, result);
                else if (away == MyTeam || home == MyTeam) myGame = (away, home);
                else League.Record(_sim.Play(away, home, League.NextStarter(away), League.NextStarter(home)));
            }
            // My game goes last, since it may pause for a big at bat.
            if (myGame != null && !StartMyGame(myGame.Value.Away, myGame.Value.Home, result)) return result;
            return FinishDay(result);
        }

        /// <summary>Answers the paused big at bat. Returns the day's result: paused again at the next big at bat, or finished.</summary>
        public DayResult ChooseApproach(Approach approach)
        {
            if (!IsMidGame) throw new InvalidOperationException("No at bat waiting.");
            _game.Decisions.Add(approach);
            var result = _game.Result;
            return RunMyGame(result) ? FinishDay(result) : result;
        }

        DayResult FinishDay(DayResult result)
        {
            result.AtBat = null;
            Me.Fatigue = Math.Max(0, Me.Fatigue - DailyRecovery - Facilities.ExtraRecovery(Me.FacilityLevel(Facility.TrainerRoom)));
            if (Me.InjuryDaysLeft > 0) Me.InjuryDaysLeft--;
            Me.Morale -= Math.Sign(Me.Morale);
            Day++;
            ReviewRoster(result);
            if (Day == AllStarDay) PlayAllStar(result);

            if (PendingTrade == null && result.AllStar == null && _random.NextDouble() < Math.Min(0.5, EventChancePerDay * DayWeight))
                PendingEvent = StoryEvents.Pick(this, result, _random);
            result.Event = PendingEvent;
            result.NewAchievements = Achievements.Check(this);
            result.Breakthroughs = new List<Stat>(Me.NewBreakthroughs);
            Me.NewBreakthroughs.Clear();
            if (PendingTraitOffer == null && Me.Traits.Count < Traits.SlotsEarned(Me))
            {
                var offer = Traits.Offer(Me, _random);
                if (offer.Count > 0) PendingTraitOffer = offer;
            }
            result.TraitOffer = PendingTraitOffer;
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

        void ApplyAction(DailyAction action, DayResult result)
        {
            if (action == DailyAction.Rest)
            {
                Me.Fatigue = Math.Max(0, Me.Fatigue - RestRecovery);
                return;
            }

            bool tired = Me.Fatigue >= TiredThreshold;
            double rehab = 1 - Facilities.InjuryReduction(Me.FacilityLevel(Facility.RehabCenter));
            if (tired && _random.NextDouble() < InjuryChanceWhenTired * rehab)
            {
                double quick = Me.Traits.Contains(Traits.Recovery) ? 0.6 : 1.0;
                Me.InjuryDaysLeft = (int)Math.Ceiling((5 + _random.NextDouble() * 10) * rehab * quick / DayWeight);
                result.GotInjured = true;
                return;
            }

            var (main, side) = TrainingTargets(action);
            double mult = AgeGrowth() * (tired ? 0.5 : 1.0) * (Me.Traits.Contains(Traits.Grinder) ? 1.25 : 1.0);
            AddPoints(result, main, Me.GainExp(main, (int)Math.Round(TrainMainExp * DayWeight * mult)));
            AddPoints(result, side, Me.GainExp(side, (int)Math.Round(TrainSideExp * DayWeight * mult)));
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(TrainFatigue));
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

        double AgeGrowth()
        {
            int age = Me.Age;
            double growth = age <= 22 ? 1.3 : age <= 27 ? 1.0 : age <= 31 ? 0.7 : 0.4;
            return age >= 28 && Me.Traits.Contains(Traits.LateBloomer) ? growth * 1.6 : growth;
        }

        int Tiring(int fatigue) => Me.Traits.Contains(Traits.Iron) ? (int)Math.Round(fatigue * 0.7) : fatigue;

        /// <summary>Three traits to pick one from, offered when a slot opens; waits until picked.</summary>
        public List<Trait> PendingTraitOffer { get; private set; }

        public void ChooseTrait(string id)
        {
            if (PendingTraitOffer == null || !PendingTraitOffer.Any(t => t.Id == id)) throw new InvalidOperationException("Not on offer.");
            Me.Traits.Add(id);
            PendingTraitOffer = null;
        }

        static void AddPoints(DayResult result, Stat stat, int points)
        {
            if (points == 0) return;
            result.PointsGained.TryGetValue(stat, out var p);
            result.PointsGained[stat] = p + points;
        }

        /// <summary>When on, my big at bats pause the game for the user to pick an approach. Off plays them straight.</summary>
        public bool DecideAtBats;

        /// <summary>A game of mine paused at a big at bat; resumed by replaying it from its seed with the answers so far.</summary>
        class MyGameState
        {
            public Team Away, Home;
            public Player AwayStarter, HomeStarter;
            public Player Player;
            public int Seed;
            public List<Approach> Decisions = new List<Approach>();
            public DayResult Result;
        }

        MyGameState _game;
        public bool IsMidGame => _game != null;
        public AtBatSituation PendingAtBat => _game?.Result.AtBat;

        /// <summary>Returns false if the game paused at a big at bat.</summary>
        bool StartMyGame(Team away, Team home, DayResult result)
        {
            var awayStarter = League.NextStarter(away);
            var homeStarter = League.NextStarter(home);

            if (Me.IsInjured)
            {
                result.SatOut = true;
                InjuredThisSeason = true;
                result.MyGame = _sim.Play(away, home, awayStarter, homeStarter);
                League.Record(result.MyGame);
                return true;
            }

            _game = new MyGameState
            {
                Away = away,
                Home = home,
                AwayStarter = awayStarter,
                HomeStarter = homeStarter,
                Player = Me.ForGame(_random),
                Seed = (int)(_random.NextDouble() * int.MaxValue),
                Result = result,
            };
            return RunMyGame(result);
        }

        bool RunMyGame(DayResult result)
        {
            var g = _game;
            var sim = _sim;
            if (DecideAtBats)
            {
                int asked = 0;
                sim = new GameSimulator(new SeededRandom(g.Seed))
                {
                    DecideFor = g.Player,
                    Decide = s => asked < g.Decisions.Count ? g.Decisions[asked++] : (Approach?)null,
                };
            }

            var regular = MyTeam.Lineup[LineupSlot];
            MyTeam.Lineup[LineupSlot] = g.Player;
            GameResult game;
            try
            {
                game = sim.Play(g.Away, g.Home, g.AwayStarter, g.HomeStarter);
                // Record while my player is still in the lineup, so his line is filed under my club.
                if (game != null) League.Record(game);
            }
            finally
            {
                MyTeam.Lineup[LineupSlot] = regular;
            }

            var events = game?.Events ?? sim.Partial.Events;
            result.AtBatLog = events
                .Where(e => e.Batter == g.Player && e.Note != null)
                .Select(e => $"{Commentary.HalfInning(e.Inning, e.IsTopHalf)} {Commentary.Describe(e)} ({e.Note})")
                .ToList();
            if (game == null)
            {
                result.AtBat = sim.Suspended;
                return false;
            }

            _game = null;
            result.MyGame = game;
            Me.ReadsRight += game.Events.Count(e => e.Batter == g.Player && e.Note != null && e.Note.EndsWith("적중!"));
            FinishMyGame(result, g.Player);
            return true;
        }

        void FinishMyGame(DayResult result, Player gamePlayer)
        {
            result.MyGamePlayer = gamePlayer;
            result.MyLine = result.MyGame.Batting.TryGetValue(gamePlayer, out var line) ? line : new BattingLine();
            if (result.MyLine.AtBats > 0) HitlessStreak = result.MyLine.Hits == 0 ? HitlessStreak + 1 : 0;
            Accumulate(SeasonLine, result.MyLine);
            Accumulate(StintLine, result.MyLine);
            StintDays++;
            GainGameExp(result, result.MyLine);
            PayForGame(result);
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(GameFatigue));
        }

        // Every game gives some exp, even an 0-for-4, so a bad day never feels wasted.
        void GainGameExp(DayResult result, BattingLine l)
        {
            double mult = AgeGrowth() * DayWeight;
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

        /// <summary>
        /// Plays the postseason, hands out titles, ages the player, renegotiates salary on this
        /// season's OPS, and starts a new season.
        /// </summary>
        public SeasonSummary EndSeason()
        {
            var ranked = League.Ranked();
            var summary = new SeasonSummary
            {
                Rank = ranked.FindIndex(s => s.Team == MyTeam) + 1,
                OldSalary = Me.Salary,
                Postseason = BaseballSim.Career.Postseason.Play(this, _sim, _random),
                Awards = BaseballSim.Career.Awards.For(League, Me.Id, Me.History.Count == 0),
            };

            summary.Record = new SeasonRecord
            {
                Season = Season,
                Age = Me.Age,
                Games = League.GamesPerSeason,
                Line = SeasonLine,
                Ratings = Me.Snapshot(),
                Salary = Me.Salary,
                Titles = summary.Awards.Where(a => a.Mine).Select(a => a.Title).ToList(),
                InjuryFree = !InjuredThisSeason,
                Club = MyTeam.Name,
                Notes = SeasonNotes,
                FarmDays = FarmDays,
            };
            if (summary.Postseason.MyTeamWon) summary.Record.Titles.Add(BaseballSim.Career.Postseason.ChampionTitle);
            Me.History.Add(summary.Record);

            // Salary moves halfway toward what this season's OPS is worth, so one fluke year
            // doesn't make or break a contract. OPS .800 is worth about 8,500, .950 about 24,000.
            // A signed deal holds the salary until it runs out.
            double ops = Ops(SeasonLine);
            double worth = MinSalary * Math.Exp((ops - 0.650) * 7);
            bool underContract = ContractSeasonsLeft > 0;
            Released = ShouldRelease(underContract);
            summary.Released = Released;
            if (Released) summary.Record.Notes.Add("시즌 후 방출");
            if (ContractSeasonsLeft > 0) ContractSeasonsLeft--;
            else
            {
                int next = (int)((Me.Salary + worth) / 2 / 100) * 100;
                Me.Salary = Math.Max(MinSalary, Math.Min(MaxSalary, next));
            }
            summary.NewSalary = Me.Salary;

            Me.Age++;
            if (Me.Age >= 32)
            {
                foreach (var stat in CareerPlayer.AllStats)
                {
                    int drop = (int)(_random.NextDouble() * (Me.Age - 30));
                    if (stat == Stat.Speed) drop += 1; // legs go first
                    drop = (int)Math.Round(drop * (1 - Facilities.DeclineReduction(Me.FacilityLevel(Facility.Conditioning))));
                    int before = Me.Ratings[stat];
                    // The ceiling drops too, or training would simply win the points back.
                    Me.Potential[stat] = Math.Max(20, Me.Potential[stat] - drop);
                    Me.Ratings[stat] = Math.Max(20, Math.Min(Me.Potential[stat], before - drop));
                    if (before != Me.Ratings[stat]) summary.Decline[stat] = before - Me.Ratings[stat];
                }
            }

            summary.NewAchievements = Achievements.Check(this);

            Season++;
            Day = 0;
            InjuredThisSeason = false;
            SeasonLine = new BattingLine();
            ResetSeasonMoves();
            League.ResetStandings();
            PendingOffers = Released ? TryoutOffers() : FreeAgencyOffers(worth);
            summary.Offers = new List<ContractOffer>(PendingOffers);
            return summary;
        }

        /// <summary>
        /// After eight seasons, and whenever a deal runs out after that, my club and three rivals
        /// bid. Rivals pay a little more to pry the player away; younger players get longer deals.
        /// </summary>
        List<ContractOffer> FreeAgencyOffers(double worth)
        {
            var offers = new List<ContractOffer>();
            if (ContractSeasonsLeft > 0 || Me.History.Count < FreeAgencySeasons || MustRetire) return offers;

            int years = Me.Age <= 30 ? 4 : Me.Age <= 34 ? 2 : 1;
            // Same midpoint as a yearly renegotiation, so free agency rewards form rather than compounding.
            double baseline = (Me.Salary + worth) / 2;
            offers.Add(Offer(MyTeam, baseline * (0.95 + 0.1 * _random.NextDouble()), years));
            var rivals = League.Teams.Where(t => t != MyTeam).ToList();
            for (int i = 0; i < 3 && rivals.Count > 0; i++)
            {
                var team = rivals[(int)(_random.NextDouble() * rivals.Count)];
                rivals.Remove(team);
                int length = Math.Max(1, years - (_random.NextDouble() < 0.5 ? 1 : 0));
                offers.Add(Offer(team, baseline * (1.05 + 0.25 * _random.NextDouble()), length));
            }
            return offers;
        }

        static ContractOffer Offer(Team team, double salary, int years) => new ContractOffer
        {
            Team = team,
            Salary = Math.Max(MinSalary, Math.Min(MaxSalary, (int)(salary / 100) * 100)),
            Years = years,
        };

        /// <summary>Takes a free-agency or tryout offer between seasons: moves club and fixes the salary for the deal.</summary>
        public void Sign(ContractOffer offer)
        {
            if (Day != 0) throw new InvalidOperationException("Contracts are signed between seasons.");
            MoveTo(offer.Team);
            Me.Salary = offer.Salary;
            ContractSeasonsLeft = offer.Tryout ? 0 : offer.Years;
            InFarm = offer.Tryout;
            Released = false;
            PendingOffers.Clear();
            StintLine = new BattingLine();
            StintDays = 0;
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

        /// <summary>Buys the next level of a facility; false if maxed or not enough money.</summary>
        public bool UpgradeFacility(Facility facility)
        {
            int next = Me.FacilityLevel(facility) + 1;
            if (next > Facilities.MaxLevel || Me.Money < Facilities.Price(next)) return false;
            Me.Money -= Facilities.Price(next);
            Me.Facilities[facility] = next;
            return true;
        }

        /// <summary>Today's opponent and whether my team is at home; null once the season is over.</summary>
        public (Team Opponent, bool Home)? TodaysGame()
        {
            if (IsSeasonOver) return null;
            foreach (var (away, home) in League.Matchups(Day))
            {
                if (home == MyTeam) return (away, true);
                if (away == MyTeam) return (home, false);
            }
            return null;
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
