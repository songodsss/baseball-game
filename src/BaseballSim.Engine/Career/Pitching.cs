using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// A pitcher's career: he takes a turn in the rotation every fifth game, in place of one of the
    /// club's starters, and rests in between. Everything the club decides on form (2군, trades,
    /// release, salary) reads his ERA through the same OPS scale a hitter is judged on.
    /// </summary>
    public partial class Career
    {
        public const int StartEvery = 5;
        /// <summary>A league starter's ERA on average; also the zero point of the OPS scale.</summary>
        const double LeagueEra = 4.20;

        public PitcherLine SeasonPitching = new PitcherLine();
        /// <summary>2군 pitching this season.</summary>
        public PitcherLine FarmPitching = new PitcherLine();
        /// <summary>Pitching since the last call-up, send-down or trade.</summary>
        public PitcherLine StintPitching = new PitcherLine();
        /// <summary>Team games since my last start; a rookie takes the ball on opening day.</summary>
        public int DaysSinceStart = StartEvery - 1;

        public bool IsStartDay => DaysSinceStart >= StartEvery - 1;
        public int DaysToNextStart => Math.Max(0, StartEvery - 1 - DaysSinceStart);

        /// <summary>ERA on the OPS scale: league average is .734, each run of ERA is worth .100.</summary>
        public static double PitchingForm(PitcherLine l) =>
            l.Outs == 0 ? 0.734 : Math.Max(0.3, Math.Min(1.3, 0.734 + (LeagueEra - l.Era) * 0.1));

        /// <summary>ERA a starter with this stuff gives up on average (calibrated from the game model).</summary>
        public static double ExpectedEra(int velocity, int control, int breaking) =>
            4.13 - 0.096 * ((velocity + control + breaking) / 3.0 - 49.5);

        /// <summary>How well I'm doing on the OPS scale, from batting or pitching as fits the player.</summary>
        double Form(BattingLine batting, PitcherLine pitching) =>
            Me.IsPitcher ? PitchingForm(pitching ?? new PitcherLine()) : Ops(batting ?? new BattingLine());

        /// <summary>How much there is to judge, in plate appearances; a starter's out is worth 1.2 of them.</summary>
        int Sample(BattingLine batting, PitcherLine pitching) =>
            Me.IsPitcher ? (pitching?.Outs ?? 0) * 6 / 5 : batting?.PlateAppearances ?? 0;

        /// <summary>A 1군 day for a pitcher: his start, or a day on the bench while the club plays.</summary>
        bool PlayPitcherDay(Team away, Team home, Player awayStarter, Player homeStarter, DayResult result)
        {
            StintDays++;
            bool start = IsStartDay && !Me.IsInjured;
            if (!start)
            {
                if (DaysSinceStart < StartEvery - 1) DaysSinceStart++;
                if (Me.IsInjured) { result.SatOut = true; InjuredThisSeason = true; }
                else result.BetweenStarts = true;
                result.MyGame = _sim.Play(away, home, awayStarter, homeStarter);
                League.Record(result.MyGame);
                result.RivalLine = result.MyGame.Batting.FirstOrDefault(b => b.Key.Id == RivalState.PlayerId).Value;
                PayForGame(result);
                return true;
            }

            DaysSinceStart = 0;
            var mine = Me.ForGame(_random);
            bool atHome = home == MyTeam;
            var replaced = atHome ? homeStarter : awayStarter;
            int slot = MyTeam.Rotation.IndexOf(replaced);
            MyTeam.Rotation[slot] = mine;
            GameResult game;
            try
            {
                game = _sim.Play(away, home, atHome ? awayStarter : mine, atHome ? mine : homeStarter);
                // Record while my player is in the rotation, so his line is filed under my club.
                League.Record(game);
            }
            finally
            {
                MyTeam.Rotation[slot] = replaced;
            }

            result.MyGame = game;
            result.MyGamePlayer = mine;
            result.RivalLine = game.Batting.FirstOrDefault(b => b.Key.Id == RivalState.PlayerId).Value;
            var raw = game.Pitching.TryGetValue(mine, out var l) ? l : new PitchingLine();
            int ours = atHome ? game.HomeScore : game.AwayScore, theirs = atHome ? game.AwayScore : game.HomeScore;
            var (win, loss) = PitcherLine.Decision(raw, ours, theirs);
            var line = new PitcherLine();
            line.Add(raw, true, win, loss);
            FinishStart(result, line);
            return true;
        }

        void FinishStart(DayResult result, PitcherLine line)
        {
            result.MyPitching = line;
            HitlessStreak = line.Runs >= 4 ? HitlessStreak + 1 : 0;
            if (result.InFarm) FarmPitching.Add(line);
            else SeasonPitching.Add(line);
            StintPitching.Add(line);
            GainPitchingExp(result, line);
            if (!result.InFarm)
            {
                PayForGame(result);
                FansForGame(null, line);
            }
            Me.Fatigue = Math.Min(100, Me.Fatigue + Tiring(GameFatigue * 4));
        }

        // Every start gives exp, even a bad one: going deep builds stamina, strikeouts velocity,
        // few walks control, and few hits the breaking ball.
        void GainPitchingExp(DayResult result, PitcherLine l)
        {
            double mult = AgeGrowth() * DayWeight;
            int Exp(double raw) => (int)Math.Round(raw * mult);
            AddPoints(result, Stat.Stamina, Me.GainExp(Stat.Stamina, Exp(2 + l.Outs / 2.0)));
            AddPoints(result, Stat.Velocity, Me.GainExp(Stat.Velocity, Exp(2 + 3 * l.Strikeouts)));
            AddPoints(result, Stat.Control, Me.GainExp(Stat.Control, Exp(2 + 3 * Math.Max(0, 5 - l.Walks))));
            AddPoints(result, Stat.Breaking, Me.GainExp(Stat.Breaking, Exp(2 + 2 * Math.Max(0, 2 * l.Outs / 3 - l.Hits))));
        }

        /// <summary>A starter's season so far on the OPS scale once he's pitched enough, else what his stuff promises.</summary>
        double ArmRating(Player p)
        {
            if (League.Pitching.TryGetValue(p.Id, out var s) && s.Line.Outs >= Day * 2) return PitchingForm(s.Line);
            return 0.734 + (LeagueEra - ExpectedEra(p.Pitching.Velocity, p.Pitching.Control, p.Pitching.Breaking)) * 0.1;
        }

        /// <summary>
        /// A squad of the best from these clubs: hitters by OPS so far, starters by ERA so far, relievers
        /// by stuff. <paramref name="me"/> (my player's game copy, or null) is in the running as a hitter
        /// or a starter; <paramref name="mine"/> is set if he made it.
        /// </summary>
        internal Team PickSquad(string name, List<Team> teams, Player me, int hitters, int starters, int relievers, out Player mine)
        {
            int minPa = Day * 2;
            double Ops(string id) => League.Batting.TryGetValue(id, out var s) && s.Line.PlateAppearances >= minPa ? Career.Ops(s.Line) : 0;
            int Stuff(Player p) => p.Pitching.Velocity + p.Pitching.Control + p.Pitching.Breaking;

            var bats = teams.SelectMany(t => t.Lineup).Select(p => (Player: p, Score: Ops(p.Id))).ToList();
            var arms = teams.SelectMany(t => t.Rotation).Select(p => (Player: p, Score: ArmRating(p))).ToList();
            if (me != null && me.IsPitcher)
            {
                bool pitched = League.Pitching.TryGetValue(Me.Id, out var s) && s.Line.Outs >= Day * 2;
                arms.Add((me, pitched ? PitchingForm(s.Line) : 0));
            }
            else if (me != null) bats.Add((me, Ops(Me.Id)));

            var team = new Team(name)
            {
                Lineup = bats.OrderByDescending(c => c.Score).Take(hitters).Select(c => c.Player).ToList(),
                Rotation = arms.OrderByDescending(c => c.Score).Take(starters).Select(c => c.Player).ToList(),
                Bullpen = teams.SelectMany(t => t.Bullpen).OrderByDescending(Stuff).Take(relievers).ToList(),
            };
            mine = me != null && (team.Lineup.Contains(me) || team.Rotation.Contains(me)) ? me : null;
            return team;
        }

        static readonly Player FarmHitter = new Player("farm-bat", "2군 타자")
        {
            Batting = new BattingRatings { Contact = FarmBatting, Power = FarmBatting, Eye = FarmBatting, Speed = 45, Defense = 45 },
        };
        const int FarmBatting = 42;

        /// <summary>A 2군 start against weaker hitters, played out at bat by bat with a simple base tracker.</summary>
        void PlayFarmStart(DayResult result)
        {
            var me = Me.ForGame(_random);
            var raw = new PitchingLine();
            int target = 15 + (me.Pitching.Stamina - 50) / 5;
            bool first = false, second = false, third = false;
            int outsThisInning = 0;
            while (raw.OutsRecorded < target && raw.Runs < 7)
            {
                var outcome = PlateAppearanceModel.Compute(FarmHitter, me, FarmDefense, 0).Pick(_random);
                int runs = 0;
                switch (outcome)
                {
                    case Outcome.Strikeout: raw.Strikeouts++; raw.OutsRecorded++; outsThisInning++; break;
                    case Outcome.Walk:
                    case Outcome.HitByPitch:
                        raw.Walks++;
                        if (first && second && third) runs++;
                        else if (first && second) third = true;
                        else if (first) second = true;
                        first = true;
                        break;
                    case Outcome.Single:
                        raw.Hits++;
                        runs += (third ? 1 : 0) + (second ? 1 : 0);
                        third = first; second = false; first = true;
                        break;
                    case Outcome.Double:
                        raw.Hits++;
                        runs += (third ? 1 : 0) + (second ? 1 : 0) + (first ? 1 : 0);
                        first = false; second = true; third = false;
                        break;
                    case Outcome.Triple:
                        raw.Hits++;
                        runs += (third ? 1 : 0) + (second ? 1 : 0) + (first ? 1 : 0);
                        first = second = false; third = true;
                        break;
                    case Outcome.HomeRun:
                        raw.Hits++; raw.HomeRuns++;
                        runs += 1 + (third ? 1 : 0) + (second ? 1 : 0) + (first ? 1 : 0);
                        first = second = third = false;
                        break;
                    default: raw.OutsRecorded++; outsThisInning++; break;
                }
                raw.Runs += runs;
                if (outsThisInning == 3) { outsThisInning = 0; first = second = third = false; }
            }
            // The 2군 side scores like an average team, so the decision follows from the runs allowed.
            int ours = (int)Math.Round(_random.NextDouble() * 9);
            var (win, loss) = PitcherLine.Decision(raw, ours, raw.Runs + (_random.NextDouble() < 0.5 ? 1 : 0));
            var line = new PitcherLine();
            line.Add(raw, true, win, loss);
            result.FarmPitching = line;
            FinishStart(result, line);
        }
    }
}
