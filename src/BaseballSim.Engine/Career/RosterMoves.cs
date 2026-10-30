using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum RosterMoveKind
    {
        Demoted,
        Promoted,
        Traded,
    }

    /// <summary>Something the club did with my player today: sent down, called up, or traded away.</summary>
    public class RosterMove
    {
        public RosterMoveKind Kind;
        public Team From;
        public Team To;
        public string Text;
    }

    /// <summary>A club asking for my player at the trade deadline; accept or decline before the next day.</summary>
    public class TradeOffer
    {
        public Team Team;
        public string Text;
    }

    /// <summary>
    /// The business side of a career: 2군 and 1군, the mid-season trade deadline, and being released.
    /// Thresholds scale with the season length so a 36-game season moves at the same pace as 144.
    /// </summary>
    public partial class Career
    {
        // Demoted when the OPS since the last call-up is this low; called up when the 2군 OPS is this high.
        const double DemoteOps = 0.580;
        const double PromoteOps = 0.780;
        const double ReleaseOps = 0.600;
        const int FarmArm = 42;
        const int FarmDefense = 45;

        /// <summary>Playing in 2군: no 1군 games, but still trains and plays farm games.</summary>
        public bool InFarm;
        /// <summary>2군 batting this season.</summary>
        public BattingLine FarmLine = new BattingLine();
        public int FarmDays;
        /// <summary>Batting and days since the last call-up, send-down or trade; decides the next move.</summary>
        public BattingLine StintLine = new BattingLine();
        public int StintDays;
        public bool TradedThisSeason;
        /// <summary>This season's moves, e.g. "5월 2군 강등". Kept on the season record.</summary>
        public List<string> SeasonNotes = new List<string>();

        /// <summary>Let go after the season; only tryout offers in PendingOffers, and none means the end of the career.</summary>
        public bool Released;
        /// <summary>Offers to answer before the next season: free agency, or tryouts after a release. Saved, so a restart shows them again.</summary>
        public List<ContractOffer> PendingOffers = new List<ContractOffer>();

        /// <summary>Set at the trade deadline now and then; answer it with AcceptTrade or DeclineTrade.</summary>
        public TradeOffer PendingTrade { get; private set; }

        int ReviewEvery => Math.Max(3, League.GamesPerSeason / 12);
        int TradeDeadline => League.GamesPerSeason * 3 / 5;
        public string MonthText => $"{Math.Min(9, 4 + Day * 6 / League.GamesPerSeason)}월";

        static readonly Player FarmPitcher = new Player("farm", "2군 투수")
        {
            Pitching = new PitchingRatings { Velocity = FarmArm, Control = FarmArm, Breaking = FarmArm, Stamina = FarmArm },
        };

        /// <summary>The 1군 plays without me while I bat a few times against weaker 2군 pitching.</summary>
        void PlayFarmGame(Team away, Team home, DayResult result)
        {
            result.InFarm = true;
            League.Record(_sim.Play(away, home, League.NextStarter(away), League.NextStarter(home)));
            FarmDays++;
            StintDays++;
            if (Me.IsInjured)
            {
                result.SatOut = true;
                InjuredThisSeason = true;
                return;
            }
            if (Me.IsPitcher)
            {
                // A 2군 starter pitches every fifth day too; the 2군 pays the salary share either way.
                if (Me.Role != PitcherRole.Starter)
                {
                    // A 2군 reliever gets an inning about every other day while fresh.
                    if (Me.Fatigue < ReliefFatigueLimit && _random.NextDouble() < 0.45) PlayFarmStart(result, relief: true);
                    else result.BetweenStarts = true;
                }
                else if (IsStartDay) { DaysSinceStart = 0; PlayFarmStart(result); }
                else { DaysSinceStart++; result.BetweenStarts = true; }
                return;
            }

            var me = Me.ForGame(_random);
            var line = new BattingLine();
            int plateAppearances = 4 + (_random.NextDouble() < 0.4 ? 1 : 0);
            for (int i = 0; i < plateAppearances; i++)
                Tally(line, PlateAppearanceModel.Compute(me, FarmPitcher, FarmDefense, 0).Pick(_random));

            result.FarmLine = line;
            Accumulate(FarmLine, line);
            Accumulate(StintLine, line);
            GainGameExp(result, line);
            // 2군 days earn no incentives; the salary still comes on payday.
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(GameFatigue + Positions.ExtraGameFatigue(Me.Position)));
        }

        void Tally(BattingLine line, Outcome outcome)
        {
            line.PlateAppearances++;
            switch (outcome)
            {
                case Outcome.Walk:
                case Outcome.HitByPitch:
                    line.Walks++;
                    return;
                case Outcome.Strikeout:
                    line.AtBats++;
                    line.Strikeouts++;
                    return;
                case Outcome.HomeRun:
                    line.AtBats++;
                    line.Hits++;
                    line.HomeRuns++;
                    line.Runs++;
                    line.RunsBattedIn += 1 + (_random.NextDouble() < 0.4 ? 1 : 0);
                    return;
                case Outcome.Single:
                case Outcome.Double:
                case Outcome.Triple:
                    line.AtBats++;
                    line.Hits++;
                    if (outcome == Outcome.Double) line.Doubles++;
                    if (outcome == Outcome.Triple) line.Triples++;
                    if (_random.NextDouble() < 0.25) line.RunsBattedIn++;
                    if (_random.NextDouble() < 0.35) line.Runs++;
                    return;
                default:
                    line.AtBats++;
                    return;
            }
        }

        /// <summary>After each day: the club looks at form every few days, and the trade deadline comes once a season.</summary>
        void ReviewRoster(DayResult result)
        {
            if (InMilitary) return;
            if (Day == TradeDeadline && !TradedThisSeason && Season > 1) TradeDeadlineDay(result);
            if (Day % ReviewEvery == 0 && !InFarm) result.OldSlot = UpdateBattingOrder();
            if (result.Move != null || Day < League.GamesPerSeason / 6 || Day % ReviewEvery != 0 || Me.IsInjured) return;

            int games = League.GamesPerSeason;
            // A regular coming off a good year gets a longer leash before a cold spell sends him down.
            var last = Me.History.Count > 0 ? Me.History[Me.History.Count - 1] : null;
            bool established = last != null && Form(last.Line, last.Pitching) >= 0.800;
            int needPa = established ? games * 3 / 2 : games / 2;
            double demoteBelow = established ? 0.500 : DemoteOps;
            double form = Form(StintLine, StintPitching);
            int sample = Sample(StintLine, StintPitching);
            if (!InFarm && sample >= needPa && form < demoteBelow)
            {
                InFarm = true;
                result.Move = Note(RosterMoveKind.Demoted, MyTeam, MyTeam, $"{MonthText} 2군 강등",
                    $"부진 끝에 2군으로 내려갔어요. ({StintText()})");
            }
            else if (InFarm && (sample >= games / 3 && form >= PromoteOps || StintDays >= games / 3))
            {
                InFarm = false;
                result.Move = Note(RosterMoveKind.Promoted, MyTeam, MyTeam, $"{MonthText} 1군 복귀",
                    form >= PromoteOps && sample >= games / 3 ? $"2군 활약({StintText()})으로 1군에 올라왔어요!" : "1군에서 다시 기회를 받았어요.");
            }
        }

        string StintText() => Me.IsPitcher ? $"평균자책 {StintPitching.EraText}" : $"OPS {Stats.Rate(Ops(StintLine))}";

        RosterMove Note(RosterMoveKind kind, Team from, Team to, string note, string text)
        {
            SeasonNotes.Add(note);
            StintLine = new BattingLine();
            StintPitching = new PitcherLine();
            StintDays = 0;
            return new RosterMove { Kind = kind, From = from, To = to, Text = text };
        }

        /// <summary>
        /// A losing club sells a star to a contender; a struggling or buried player gets moved on.
        /// Otherwise a club may simply ask, and the player decides.
        /// </summary>
        void TradeDeadlineDay(DayResult result)
        {
            var ranked = League.Ranked().Select(s => s.Team).ToList();
            bool losing = ranked.IndexOf(MyTeam) >= ranked.Count / 2;
            double ops = Form(SeasonLine, SeasonPitching);
            int sample = Sample(SeasonLine, SeasonPitching);
            bool star = !InFarm && sample >= League.GamesPerSeason && ops >= 0.850;
            bool struggling = InFarm || sample >= League.GamesPerSeason && ops < 0.650;

            double forced = 0.05;
            if (star && losing && Me.Age >= 25) forced += 0.12;
            if (struggling && Me.Age >= 24) forced += 0.10;

            var others = ranked.Where(t => t != MyTeam).ToList();
            var contenders = others.Take(4).ToList();
            Team PickFrom(List<Team> teams) => teams[(int)(_random.NextDouble() * teams.Count)];

            if (_random.NextDouble() < forced)
            {
                var to = PickFrom(star ? contenders : others);
                string why = star ? "우승을 노리는 구단이 당신을 원했어요" : "구단이 분위기 쇄신을 위해 결정했어요";
                result.Move = Trade(to, $"트레이드! {MyTeam.Name} → {to.Name}. {why}.");
                return;
            }

            double asked = 0.12 + (losing ? 0.15 : 0) + (InFarm ? 0.15 : 0);
            if (_random.NextDouble() < asked)
            {
                var team = PickFrom(star ? contenders : others);
                string text = InFarm
                    ? $"{team.Name}이 1군 자리를 약속하며 트레이드를 제안했어요. 옮길까요?"
                    : star
                        ? $"우승을 노리는 {team.Name}이 당신을 데려가고 싶어 해요. 옮길까요?"
                        : $"{team.Name}이 트레이드를 제안했어요. 옮길까요?";
                PendingTrade = new TradeOffer { Team = team, Text = text };
                result.Trade = PendingTrade;
            }
        }

        RosterMove Trade(Team to, string text)
        {
            var from = MyTeam;
            MoveTo(to);
            InFarm = false;
            TradedThisSeason = true;
            return Note(RosterMoveKind.Traded, from, to, $"{MonthText} 트레이드 {from.Name} → {to.Name}", text);
        }

        void MoveTo(Team to)
        {
            if (to == MyTeam) return;
            Me.Clubs.Add(to.Name);
            MyTeam = to;
            if (League.Batting.TryGetValue(Me.Id, out var season)) season.Team = to;
            if (League.Pitching.TryGetValue(Me.Id, out var arm)) arm.Team = to;
        }

        public RosterMove AcceptTrade()
        {
            if (PendingTrade == null) throw new InvalidOperationException("No trade offer to answer.");
            var offer = PendingTrade;
            PendingTrade = null;
            Me.Morale = Math.Min(CareerPlayer.MaxMorale, Me.Morale + 2);
            return Trade(offer.Team, $"{offer.Team.Name}로 이적했어요. 새 출발!");
        }

        public void DeclineTrade()
        {
            if (PendingTrade == null) throw new InvalidOperationException("No trade offer to answer.");
            PendingTrade = null;
        }

        /// <summary>
        /// Out of contract and not good enough: buried in 2군 for half the season, or a poor 1군 year
        /// once established, or a veteran in decline. Never in the first two seasons.
        /// </summary>
        bool ShouldRelease(bool underContract)
        {
            if (underContract || Me.History.Count < 3 || Me.Age >= RetirementAge) return false;
            int games = League.GamesPerSeason;
            bool fullYear = Sample(SeasonLine, SeasonPitching) >= games * 2;
            double ops = Form(SeasonLine, SeasonPitching);
            return FarmDays >= games / 2 && Me.Age >= 22
                || fullYear && ops < ReleaseOps && Me.Age >= 24
                || fullYear && ops < 0.680 && Me.Age >= 34;
        }

        /// <summary>Clubs willing to give a released player a 1-year tryout in 2군; fewer for older, weaker players.</summary>
        List<ContractOffer> TryoutOffers()
        {
            double overall = Me.MyStats.Average(s => Me.Ratings[s]);
            double chance = Math.Max(0.03, Math.Min(0.5, (overall - 40) / 50 - Math.Max(0, Me.Age - 30) * 0.06));
            int salary = Math.Max(MinSalary, (int)(Me.Salary * 0.6 / 100) * 100);
            return League.Teams
                .Where(t => t != MyTeam && _random.NextDouble() < chance)
                .Take(3)
                .Select(t => new ContractOffer { Team = t, Salary = salary, Years = 1, Tryout = true })
                .ToList();
        }

        int AllStarDay => League.GamesPerSeason / 2;

        void PlayAllStar(DayResult result)
        {
            result.AllStar = AllStar.Play(this, _sim);
            if (!result.AllStar.Selected) return;
            SeasonNotes.Add(result.AllStar.MyMvp ? $"{MonthText} 올스타전 MVP" : result.AllStar.FanVote ? $"{MonthText} 팬 투표 올스타" : $"{MonthText} 올스타 선정");
            AddFans(result.AllStar.MyMvp ? 200 : 100);
            Me.Morale = Math.Min(CareerPlayer.MaxMorale, Me.Morale + 2);
        }

        int TournamentDay => League.GamesPerSeason * 11 / 20;

        void PlayTournament(DayResult result)
        {
            result.Tournament = NationalTeam.Play(this, _random);
            if (result.Tournament.Selected && result.Tournament.Medal == NationalTeam.Gold) { ExemptByGold(); AddFans(300); }
            if (result.Tournament.Selected)
                SeasonNotes.Add($"{MonthText} {NationalTeam.Name} " + (result.Tournament.Medal ?? "대표 출전"));
        }

        void ResetSeasonMoves()
        {
            FarmLine = new BattingLine();
            FarmPitching = new PitcherLine();
            StintPitching = new PitcherLine();
            FarmDays = 0;
            TradedThisSeason = false;
            StintLine = new BattingLine();
            StintDays = 0;
            SeasonNotes = new List<string>();
        }
    }
}
