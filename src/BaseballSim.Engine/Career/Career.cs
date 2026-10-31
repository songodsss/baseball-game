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
        // A pitcher's
        TrainVelocity, // 구속 훈련 (러닝·웨이트)
        TrainControl,  // 제구 훈련 (불펜 피칭)
        TrainBreaking, // 변화구 연마
        TrainStamina,  // 체력 훈련 (장거리 러닝)
    }

    public class DayResult
    {
        public int Season;
        public int Day;
        public DailyAction Action;
        /// <summary>Condition stars when training today (0 on a rest day); 4–5 stars train better.</summary>
        public int ConditionStars;
        /// <summary>Rating points gained today, from training and from the game.</summary>
        public Dictionary<Stat, int> PointsGained = new Dictionary<Stat, int>();
        public bool GotInjured;
        public InjuryKind Injury;
        public bool SatOut;
        public GameResult MyGame;
        public Player MyGamePlayer;
        public BattingLine MyLine;
        /// <summary>A pitcher's start today (1군 or 2군); null on other days and for hitters.</summary>
        public PitcherLine MyPitching;
        /// <summary>Career milestones reached today, e.g. "통산 100안타".</summary>
        public List<string> Milestones = new List<string>();
        /// <summary>A pitcher resting between starts while his club played.</summary>
        public bool BetweenStarts;
        /// <summary>Money paid today: only on a payday (see <see cref="Payday"/>).</summary>
        public int MoneyEarned;
        /// <summary>Set on the day a monthly salary is paid.</summary>
        public Payday Payday;
        /// <summary>A story event to answer before the next day, or null.</summary>
        public StoryEvent Event;
        public List<Achievement> NewAchievements = new List<Achievement>();
        /// <summary>Played in 2군 today; FarmLine is my batting there (null if hurt).</summary>
        public bool InFarm;
        public BattingLine FarmLine;
        public PitcherLine FarmPitching;
        /// <summary>Sent down, called up or traded today, or null.</summary>
        public RosterMove Move;
        /// <summary>A trade offer to answer before the next day, or null.</summary>
        public TradeOffer Trade;
        /// <summary>A big at bat waiting for <see cref="Career.ChooseApproach"/>; null once the day is done.</summary>
        public AtBatSituation AtBat;
        /// <summary>The rival's line in today's game when my club played his; null otherwise.</summary>
        public BattingLine RivalLine;
        /// <summary>Stats whose potential went up today through training past the limit.</summary>
        public List<Stat> Breakthroughs = new List<Stat>();
        /// <summary>A trait slot opened: pick one with ChooseTrait. Stays on offer until picked.</summary>
        public List<Trait> TraitOffer;
        /// <summary>Set when the batting order changed today: the slot I batted in before. LineupSlot is the new one.</summary>
        public int? OldSlot;
        /// <summary>My big at bats so far today with the approach taken, e.g. "7회 ... 2루타 (직구 노림 적중!)".</summary>
        public List<string> AtBatLog = new List<string>();
        /// <summary>The Asian tournament, every four years a little after midseason; null on other days.</summary>
        public TournamentResult Tournament;
        /// <summary>The all-star game, played on the midseason day; null on other days.</summary>
        public AllStarResult AllStar;
    }

    public class SeasonSummary
    {
        public SeasonRecord Record;
        /// <summary>Popularity before the winter drift, and the sponsor money it brought.</summary>
        public int Popularity;
        public int Sponsor;
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
        /// <summary>The club's salary offer to negotiate; null in a year with a running deal, free agency or a release.</summary>
        public SalaryTalk SalaryTalk;
        /// <summary>Military service starts next season (the age limit came).</summary>
        public bool Enlisted;
        /// <summary>The player may choose to serve next season (Career.Enlist).</summary>
        public bool CanEnlist;
        public SeasonGoal Goal;
        public bool GoalMet;
        /// <summary>The rival's season, club and titles; null without a rival.</summary>
        public BattingLine RivalLine;
        public Team RivalTeam;
        public List<string> RivalTitles = new List<string>();
        public int GoalMoney;
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
        public static CareerPlayer NewPlayer(string name, IRandom random, int potentialBonus = 0, Position position = Position.Outfield, int number = 0,
            PitcherRole role = PitcherRole.Starter)
        {
            var me = new CareerPlayer { Id = "me", Name = name, Age = 18, Position = position, Number = number, Role = position == Position.Pitcher ? role : PitcherRole.Starter };
            foreach (var stat in me.MyStats)
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
            if (action != DailyAction.Rest && IsPitcherAction(action) != Me.IsPitcher)
                throw new ArgumentException(Me.IsPitcher ? "Pitchers train pitching." : "Hitters train hitting.");
            PendingOffers.Clear(); // free-agency offers left unanswered: stays on a yearly deal
            PendingSalary = null;  // salary offer left unanswered: accepted
            PendingTrade = null;

            var result = new DayResult { Season = Season, Day = Day, Action = action };
            if (Me.IsInjured) result.Action = action = DailyAction.Rest;
            PendingEvent = null;

            if (Rival == null && Day == 0) CreateRival();
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
            else if (Me.LingerDays > 0) Me.LingerDays--;
            Me.Morale -= Math.Sign(Me.Morale);
            Day++;
            result.Milestones = CheckMilestones(result);
            ReviewRoster(result);
            if (Day == AllStarDay) PlayAllStar(result);
            if (Day == TournamentDay && NationalTeam.IsTournamentYear(Year)) PlayTournament(result);
            PayMonths(result);
            WriteNews(result);

            // A main story scene, when its moment has come, takes the place of the day's random event.
            // The random one is still rolled, so the story doesn't shift the rest of the day's luck.
            if (PendingTrade == null && result.AllStar == null && result.Tournament == null)
            {
                var random = _random.NextDouble() < Math.Min(0.5, EventChancePerDay * DayWeight) ? StoryEvents.Pick(this, result, _random) : null;
                PendingEvent = MainStory.Next(this, result) ?? random;
            }
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
                var kind = Injuries.Roll(_random);
                Injure(kind, (int)Math.Ceiling(Injuries.Days(kind, _random) * rehab / DayWeight));
                result.GotInjured = true;
                result.Injury = kind;
                return;
            }

            var (main, side) = TrainingTargets(action);
            result.ConditionStars = ConditionStars;
            double mult = ConditionTraining(result.ConditionStars) * AgeGrowth() * (tired ? 0.5 : 1.0) * (Me.Traits.Contains(Traits.Grinder) ? 1.25 : 1.0);
            AddPoints(result, main, Me.GainExp(main, (int)Math.Round(TrainMainExp * DayWeight * mult)));
            AddPoints(result, side, Me.GainExp(side, (int)Math.Round(TrainSideExp * DayWeight * mult)));
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(TrainFatigue));
        }

        public static bool IsPitcherAction(DailyAction action) => action >= DailyAction.TrainVelocity;

        /// <summary>The training choices for this player, rest last.</summary>
        public List<DailyAction> TrainingChoices => (Me.IsPitcher
            ? new[] { DailyAction.TrainVelocity, DailyAction.TrainControl, DailyAction.TrainBreaking, DailyAction.TrainStamina, DailyAction.Rest }
            : new[] { DailyAction.TrainContact, DailyAction.TrainPower, DailyAction.TrainEye, DailyAction.TrainSpeed, DailyAction.TrainDefense, DailyAction.Rest }).ToList();

        public static (Stat Main, Stat Side) TrainingTargets(DailyAction action)
        {
            switch (action)
            {
                case DailyAction.TrainContact: return (Stat.Contact, Stat.Eye);
                case DailyAction.TrainPower: return (Stat.Power, Stat.Contact);
                case DailyAction.TrainEye: return (Stat.Eye, Stat.Contact);
                case DailyAction.TrainSpeed: return (Stat.Speed, Stat.Defense);
                case DailyAction.TrainVelocity: return (Stat.Velocity, Stat.Stamina);
                case DailyAction.TrainControl: return (Stat.Control, Stat.Breaking);
                case DailyAction.TrainBreaking: return (Stat.Breaking, Stat.Control);
                case DailyAction.TrainStamina: return (Stat.Stamina, Stat.Velocity);
                default: return (Stat.Defense, Stat.Speed);
            }
        }

        double AgeGrowth()
        {
            int age = Me.Age;
            double growth = age <= 22 ? 1.3 : age <= 27 ? 1.0 : age <= 31 ? 0.7 : 0.4;
            return age >= 28 && Me.Traits.Contains(Traits.LateBloomer) ? growth * 1.6 : growth;
        }

        /// <summary>Puts the player out for some days (fewer with the quick-recovery trait); the injury lingers as long again.</summary>
        internal int Injure(InjuryKind kind, int days)
        {
            if (Me.Traits.Contains(Traits.Recovery)) days = (int)Math.Ceiling(days * 0.6);
            days = Math.Max(1, days);
            Me.Injury = kind;
            Me.InjuryDaysLeft = days;
            Me.LingerDays = days;
            InjuredThisSeason = true;
            SeasonNotes.Add($"{MonthText} {Injuries.Name(kind)} ({days}일)");
            return days;
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
        /// <summary>The club my team is playing in the paused game, or null.</summary>
        public Team MidGameOpponent => _game == null ? null : _game.Home == MyTeam ? _game.Away : _game.Home;

        /// <summary>Returns false if the game paused at a big at bat.</summary>
        bool StartMyGame(Team away, Team home, DayResult result)
        {
            var awayStarter = League.NextStarter(away);
            var homeStarter = League.NextStarter(home);
            if (Me.IsPitcher) return PlayPitcherDay(away, home, awayStarter, homeStarter, result);

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
                Player = Me.ForGame(_random, TodayForm),
                Seed = (int)(_random.NextDouble() * int.MaxValue),
                Result = result,
            };
            return RunMyGame(result);
        }

        bool RunMyGame(DayResult result)
        {
            if (_game.Player.IsPitcher) return RunPitcherGame(result);
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
            result.RivalLine = game.Batting.FirstOrDefault(b => b.Key.Id == RivalState.PlayerId).Value;
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
            Me.CareerStolenBases += result.MyLine.StolenBases;
            Accumulate(StintLine, result.MyLine);
            StintDays++;
            GainGameExp(result, result.MyLine);
            PayForGame(result);
            FansForGame(result.MyLine, null);
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(GameFatigue + Positions.ExtraGameFatigue(Me.Position)));
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

        /// <summary>
        /// Banks today's performance bonuses (contract incentives) toward the next payday; nothing is
        /// paid on the day itself.
        /// </summary>
        void PayForGame(DayResult result)
        {
            var l = result.MyLine;
            var p = result.MyPitching;
            int bonus = 0;
            if (l != null) bonus += DayWeight * (5 * l.Hits + 30 * l.HomeRuns + 5 * l.RunsBattedIn);
            if (p != null) bonus += DayWeight * (60 * p.Wins + 40 * p.Saves + 25 * p.Holds + 5 * p.Strikeouts);
            PendingBonus += bonus;
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
                Awards = BaseballSim.Career.Awards.For(League, Me.Id,
                    Me.History.Count == 0 ? new[] { Me.Id, RivalState.PlayerId } : new string[0], FieldingOf),
            };

            summary.Record = new SeasonRecord
            {
                Season = Season,
                Age = Me.Age,
                Games = League.GamesPerSeason,
                Line = SeasonLine,
                Pitching = Me.IsPitcher ? SeasonPitching : null,
                Ratings = Me.Snapshot(),
                Salary = Me.Salary,
                Titles = summary.Awards.Where(a => a.Mine).Select(a => a.Title).ToList(),
                InjuryFree = !InjuredThisSeason,
                Club = MyTeam.Name,
                Notes = SeasonNotes,
                FarmDays = FarmDays,
                Postseason = BaseballSim.Career.Postseason.Describe(summary.Postseason, MyTeam),
            };
            if (summary.Postseason.MyTeamWon) summary.Record.Titles.Add(BaseballSim.Career.Postseason.ChampionTitle);
            Me.History.Add(summary.Record);
            summary.GoalMet = SettleGoal(summary);
            SettleFans(summary);

            // Salary moves halfway toward what this season's OPS is worth, so one fluke year
            // doesn't make or break a contract. OPS .800 is worth about 8,500, .950 about 24,000.
            // A signed deal holds the salary until it runs out.
            double ops = Form(SeasonLine, SeasonPitching);
            double worth = MinSalary * Math.Exp((ops - 0.650) * 7);
            bool underContract = ContractSeasonsLeft > 0;
            bool served = InMilitary;
            Released = !served && ShouldRelease(underContract);
            summary.Released = Released;
            if (Released) summary.Record.Notes.Add("시즌 후 방출");
            if (served) { } // contract and salary wait for the return
            else if (ContractSeasonsLeft > 0) ContractSeasonsLeft--;
            else
            {
                int next = (int)((Me.Salary + worth) / 2 / 100) * 100;
                Me.Salary = Math.Max(MinSalary, Math.Min(MaxSalary, next));
            }
            summary.NewSalary = Me.Salary;

            Me.Age++;
            if (Me.Age >= 32)
            {
                foreach (var stat in Me.MyStats)
                {
                    int drop = (int)(_random.NextDouble() * (Me.Age - 30));
                    if (stat == Stat.Speed || stat == Stat.Velocity) drop += 1; // legs and fastball go first
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
            PaidMonths = 0;
            PendingBonus = 0;
            SeasonPitching = new PitcherLine();
            DaysSinceStart = StartEvery - 1;
            ResetSeasonMoves();
            if (Rival != null)
            {
                summary.RivalLine = RivalLine;
                summary.RivalTeam = RivalTeam;
                summary.RivalTitles = summary.Awards.Where(a => a.Winner == Rival.Name).Select(a => a.Title).ToList();
                summary.Record.RivalLine = RivalLine;
                AgeRival();
            }
            League.ResetStandings();
            PendingOffers = Released ? TryoutOffers() : served ? new List<ContractOffer>() : FreeAgencyOffers(worth);
            PendingSalary = served ? null : OpenSalaryTalk(summary, worth);
            summary.Enlisted = SettleMilitary(summary, served);
            summary.Offers = new List<ContractOffer>(PendingOffers);
            summary.SalaryTalk = PendingSalary;
            summary.CanEnlist = CanEnlist;
            WriteSeasonNews(summary);
            return summary;
        }

        /// <summary>
        /// After eight seasons, and whenever a deal runs out after that, my club and three rivals
        /// bid. Rivals pay a little more to pry the player away; younger players get longer deals.
        /// </summary>
        List<ContractOffer> FreeAgencyOffers(double worth)
        {
            var offers = new List<ContractOffer>();
            if (ContractSeasonsLeft > 0 || SeasonsPlayed < FreeAgencySeasons || MustRetire) return offers;

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
            total.StolenBases += add.StolenBases; total.CaughtStealing += add.CaughtStealing;
        }

        static double Normal(IRandom random, double mean, double sd)
        {
            double u1 = 1 - random.NextDouble();
            double u2 = random.NextDouble();
            return mean + sd * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }
    }
}
