using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim
{
    /// <summary>
    /// Plays one full game plate appearance by plate appearance. A bullpen of three or more has roles:
    /// its last arm closes (the 9th with a lead of one to three), the one before sets up (the 8th, tied
    /// or up by three or less), and the rest relieve a tired pitcher in order.
    /// </summary>
    public class GameSimulator
    {
        // KBO rules: a game still tied after the 12th inning ends in a tie.
        public int MaxInnings = 12;

        readonly IRandom _random;

        /// <summary>
        /// When set, big at bats by <see cref="DecideFor"/> ask for an approach. Returning null pauses the
        /// game: Play returns null and <see cref="Suspended"/> holds the at bat. Replaying with the same
        /// seed and the answers so far reaches the same spot, so a game can be resumed by playing it again.
        /// </summary>
        public Func<AtBatSituation, Approach?> Decide;
        public Player DecideFor;
        public AtBatSituation Suspended { get; private set; }
        /// <summary>Big spots asked about in one game when <see cref="DecideFor"/> pitches, so a starter isn't stopped all night.</summary>
        public int MaxPitchingDecisions = 4;
        int _pitchingAsks;
        /// <summary>The game so far; after a pause, everything up to the paused at bat.</summary>
        public GameResult Partial => _result;

        class SuspendGame : Exception { }

        public GameSimulator(IRandom random)
        {
            _random = random;
        }

        class Side
        {
            public Team Team;
            public int BattingIndex;
            public Player Pitcher;
            public bool PitcherIsStarter = true;
            public int PitchCount;
            public int NextReliever;
            public double Defense;
            public bool UsedSetup, UsedCloser;
            /// <summary>The current reliever's lead when he came in, if it was a save situation (1-3).</summary>
            public int? EntryLead;
            public bool LostLead;
            public int StintOuts;
            public Player Setup => Team.Bullpen.Count >= 3 ? Team.Bullpen[Team.Bullpen.Count - 2] : null;
            public Player Closer => Team.Bullpen.Count >= 3 ? Team.Bullpen[Team.Bullpen.Count - 1] : null;
            public int OrdinaryRelievers => Team.Bullpen.Count >= 3 ? Team.Bullpen.Count - 2 : Team.Bullpen.Count;
        }

        GameResult _result;
        Side _away, _home;
        Player[] _bases;
        int _outs;
        int _inning;
        bool _top;

        public GameResult Play(Team away, Team home, Player awayStarter, Player homeStarter)
        {
            _result = new GameResult { Away = away, Home = home };
            _away = NewSide(away, awayStarter);
            _home = NewSide(home, homeStarter);
            Suspended = null;
            _pitchingAsks = 0;

            try
            {
                for (_inning = 1; _inning <= MaxInnings; _inning++)
                {
                    PlayHalf(top: true);
                    // Home team leading after the top of the 9th or later doesn't bat.
                    if (_inning >= 9 && _result.HomeScore > _result.AwayScore) break;
                    PlayHalf(top: false);
                    if (_inning >= 9 && _result.HomeScore != _result.AwayScore) break;
                }
            }
            catch (SuspendGame)
            {
                return null;
            }

            _result.Innings = Math.Min(_inning, MaxInnings);
            // The save goes to the winning side's last arm, if he came in to protect a small lead and kept it.
            var winner = _result.HomeScore > _result.AwayScore ? _home : _result.AwayScore > _result.HomeScore ? _away : null;
            if (winner != null && !winner.PitcherIsStarter && winner.EntryLead != null && !winner.LostLead)
                PitchingFor(winner.Pitcher).Saves = 1;
            return _result;
        }

        Side NewSide(Team team, Player starter)
        {
            return new Side
            {
                Team = team,
                Pitcher = starter,
                Defense = team.Lineup.Average(p => (double)p.Batting.Defense),
            };
        }

        void PlayHalf(bool top)
        {
            _top = top;
            _outs = 0;
            _bases = new Player[3];
            var batting = top ? _away : _home;
            var fielding = top ? _home : _away;

            BringInLateRelief(fielding);
            while (_outs < 3)
            {
                MaybeChangePitcher(fielding);
                PlayPlateAppearance(batting, fielding);
                if (IsWalkOff()) return;
            }
        }

        bool IsWalkOff() => !_top && _inning >= 9 && _result.HomeScore > _result.AwayScore;

        static int MaxPitches(Side side) => side.PitcherIsStarter
            ? 60 + (int)(side.Pitcher.Pitching.Stamina * 0.6)
            : 15 + (int)(side.Pitcher.Pitching.Stamina * 0.3);

        int LeadOf(Side side) => side == _home ? _result.HomeScore - _result.AwayScore : _result.AwayScore - _result.HomeScore;

        /// <summary>At the top of a half inning: the closer for a save, the setup man for the 8th, or the setup man's inning is done.</summary>
        void BringInLateRelief(Side fielding)
        {
            int lead = LeadOf(fielding);
            if (_inning >= 9 && lead >= 1 && lead <= 3 && fielding.Closer != null && !fielding.UsedCloser && fielding.Pitcher != fielding.Closer)
            {
                fielding.UsedCloser = true;
                ChangePitcher(fielding, fielding.Closer);
            }
            else if (_inning == 8 && lead >= 0 && lead <= 3 && fielding.Setup != null && !fielding.UsedSetup && fielding.Pitcher != fielding.Setup)
            {
                fielding.UsedSetup = true;
                ChangePitcher(fielding, fielding.Setup);
            }
            else if (fielding.Pitcher == fielding.Setup && fielding.StintOuts >= 3 && fielding.NextReliever < fielding.OrdinaryRelievers)
            {
                ChangePitcher(fielding, fielding.Team.Bullpen[fielding.NextReliever++]);
            }
        }

        void MaybeChangePitcher(Side fielding)
        {
            if (fielding.PitchCount < MaxPitches(fielding)) return;
            if (fielding.NextReliever >= fielding.OrdinaryRelievers) return; // nobody left, keep going tired
            ChangePitcher(fielding, fielding.Team.Bullpen[fielding.NextReliever++]);
        }

        void ChangePitcher(Side fielding, Player next)
        {
            // A reliever who hands on the small lead he was given gets a hold.
            if (!fielding.PitcherIsStarter && fielding.EntryLead != null && !fielding.LostLead && fielding.StintOuts > 0 && LeadOf(fielding) > 0)
                PitchingFor(fielding.Pitcher).Holds = 1;
            int lead = LeadOf(fielding);
            fielding.Pitcher = next;
            fielding.PitcherIsStarter = false;
            fielding.PitchCount = 0;
            fielding.StintOuts = 0;
            fielding.EntryLead = lead >= 1 && lead <= 3 ? lead : (int?)null;
            fielding.LostLead = false;
            _result.Events.Add(new GameEvent
            {
                Kind = EventKind.PitchingChange,
                Inning = _inning,
                IsTopHalf = _top,
                Pitcher = fielding.Pitcher,
                OutsBefore = _outs,
                OutsAfter = _outs,
                BasesBefore = (Player[])_bases.Clone(),
                BasesAfter = (Player[])_bases.Clone(),
                AwayScore = _result.AwayScore,
                HomeScore = _result.HomeScore,
            });
        }

        void PlayPlateAppearance(Side batting, Side fielding)
        {
            var batter = batting.Team.Lineup[batting.BattingIndex];
            batting.BattingIndex = (batting.BattingIndex + 1) % batting.Team.Lineup.Count;
            var pitcher = fielding.Pitcher;

            // Past 80% of his limit a pitcher starts to lose effectiveness.
            int limit = MaxPitches(fielding);
            double fatigue = Math.Max(0, (fielding.PitchCount - limit * 0.8) / (limit * 0.2)) * 0.3;

            var odds = PlateAppearanceModel.Compute(batter, pitcher, fielding.Defense, fatigue);
            string note = Decide == null ? null
                : batter == DecideFor ? AskApproach(batter, pitcher, odds)
                : pitcher == DecideFor ? AskPitch(batter, pitcher, odds, fielding) : null;
            var outcome = odds.Pick(_random);

            var ev = new GameEvent
            {
                Kind = EventKind.PlateAppearance,
                Inning = _inning,
                IsTopHalf = _top,
                Batter = batter,
                Pitcher = pitcher,
                Outcome = outcome,
                OutsBefore = _outs,
                BasesBefore = (Player[])_bases.Clone(),
                Note = note,
            };

            ResolveOutcome(ev, batter);

            int pitches = PitchesFor(outcome);
            fielding.PitchCount += pitches;
            ev.PitchesThrown = pitches;
            ev.OutsAfter = _outs;
            ev.BasesAfter = (Player[])_bases.Clone();

            if (_top) _result.AwayScore += ev.RunnersScored.Count;
            else _result.HomeScore += ev.RunnersScored.Count;
            ev.AwayScore = _result.AwayScore;
            ev.HomeScore = _result.HomeScore;

            _result.Events.Add(ev);
            RecordStats(ev);

            fielding.StintOuts += ev.OutsAfter - ev.OutsBefore;
            if (fielding.EntryLead != null && !fielding.LostLead && LeadOf(fielding) <= 0)
            {
                fielding.LostLead = true;
                PitchingFor(pitcher).BlownSaves = 1;
            }
        }

        /// <summary>My pitcher in a big spot: read the batter and pick the pitch; pauses the game if there's no answer yet.</summary>
        string AskPitch(Player batter, Player pitcher, OutcomeOdds odds, Side fielding)
        {
            var bases = _bases.Select(b => b != null).ToArray();
            int lead = LeadOf(fielding);
            if (_pitchingAsks >= MaxPitchingDecisions || !AtBatChoice.IsBigForPitcher(_inning, bases, lead)) return null;
            _pitchingAsks++;

            var situation = AtBatChoice.ReadBatter(batter, pitcher, _random);
            situation.Inning = _inning;
            situation.IsTopHalf = _top;
            situation.Outs = _outs;
            situation.Bases = bases;
            situation.MyRuns = lead + (fielding == _home ? _result.AwayScore : _result.HomeScore);
            situation.TheirRuns = fielding == _home ? _result.AwayScore : _result.HomeScore;
            var approach = Decide(situation);
            if (approach == null)
            {
                Suspended = situation;
                throw new SuspendGame();
            }
            AtBatChoice.ApplyPitch(odds, approach.Value, situation.Coming);
            return AtBatChoice.PitchNote(approach.Value, situation.Coming);
        }

        /// <summary>In a big spot, reads the pitcher and applies the chosen approach; pauses the game if there's no answer yet.</summary>
        string AskApproach(Player batter, Player pitcher, OutcomeOdds odds)
        {
            var bases = _bases.Select(b => b != null).ToArray();
            int mine = _top ? _result.AwayScore : _result.HomeScore;
            int theirs = _top ? _result.HomeScore : _result.AwayScore;
            if (!AtBatChoice.IsBig(_inning, bases, mine - theirs)) return null;

            var situation = AtBatChoice.Read(batter, pitcher, _random);
            situation.Inning = _inning;
            situation.IsTopHalf = _top;
            situation.Outs = _outs;
            situation.Bases = bases;
            situation.MyRuns = mine;
            situation.TheirRuns = theirs;
            var approach = Decide(situation);
            if (approach == null)
            {
                Suspended = situation;
                throw new SuspendGame();
            }
            AtBatChoice.Apply(odds, approach.Value, situation.Coming, batter);
            return AtBatChoice.Note(approach.Value, situation.Coming);
        }

        int PitchesFor(Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Strikeout: return 4 + (_random.NextDouble() < 0.6 ? 1 : 0);
                case Outcome.Walk: return 5 + (_random.NextDouble() < 0.7 ? 1 : 0);
                case Outcome.HitByPitch: return 1 + (int)(_random.NextDouble() * 4);
                default: return 1 + (int)(_random.NextDouble() * 6);
            }
        }

        static double SpeedOf(Player runner) => (runner.Batting.Speed - 50) / 50.0;

        bool Chance(double p) => _random.NextDouble() < p;

        void Score(GameEvent ev, Player runner)
        {
            if (runner != null) ev.RunnersScored.Add(runner);
        }

        void ResolveOutcome(GameEvent ev, Player batter)
        {
            var first = _bases[0];
            var second = _bases[1];
            var third = _bases[2];

            switch (ev.Outcome)
            {
                case Outcome.Strikeout:
                case Outcome.LineOut:
                    _outs++;
                    break;

                case Outcome.Walk:
                case Outcome.HitByPitch:
                    // Only forced runners move.
                    if (first != null)
                    {
                        if (second != null)
                        {
                            if (third != null) Score(ev, third);
                            _bases[2] = second;
                        }
                        _bases[1] = first;
                    }
                    _bases[0] = batter;
                    break;

                case Outcome.HomeRun:
                    Score(ev, third); Score(ev, second); Score(ev, first); Score(ev, batter);
                    _bases = new Player[3];
                    break;

                case Outcome.Triple:
                    Score(ev, third); Score(ev, second); Score(ev, first);
                    _bases = new Player[] { null, null, batter };
                    break;

                case Outcome.Double:
                    Score(ev, third); Score(ev, second);
                    _bases = new Player[] { null, batter, null };
                    if (first != null)
                    {
                        if (Chance(0.4 + 0.3 * SpeedOf(first))) Score(ev, first);
                        else _bases[2] = first;
                    }
                    break;

                case Outcome.Single:
                    _bases = new Player[] { batter, null, null };
                    Score(ev, third);
                    if (second != null)
                    {
                        if (Chance(0.6 + 0.25 * SpeedOf(second))) Score(ev, second);
                        else _bases[2] = second;
                    }
                    if (first != null)
                    {
                        if (_bases[2] == null && Chance(0.25 + 0.2 * SpeedOf(first))) _bases[2] = first;
                        else _bases[1] = first;
                    }
                    break;

                case Outcome.GroundOut:
                    if (first != null && _outs < 2 && Chance(0.45 - 0.2 * SpeedOf(batter)))
                    {
                        ev.DoublePlay = true;
                        _outs += 2;
                        _bases[0] = null;
                    }
                    else
                    {
                        _outs++;
                        // Runners move up on the out; the batter is thrown out at first.
                        _bases = new Player[3];
                        if (third != null)
                        {
                            if (_outs < 3 && Chance(0.5)) Score(ev, third);
                            else _bases[2] = third;
                        }
                        if (second != null)
                        {
                            if (_bases[2] == null && Chance(0.5)) _bases[2] = second;
                            else _bases[1] = second;
                        }
                        if (first != null)
                        {
                            if (_bases[1] == null) _bases[1] = first;
                            else _bases[0] = first;
                        }
                        break;
                    }
                    // Double play: the others advance one base if the inning isn't over.
                    if (_outs < 3)
                    {
                        var newBases = new Player[3];
                        if (third != null) Score(ev, third);
                        if (second != null) newBases[2] = second;
                        _bases = newBases;
                    }
                    break;

                case Outcome.FlyOut:
                    _outs++;
                    if (_outs < 3)
                    {
                        if (third != null && Chance(0.55 + 0.2 * SpeedOf(third)))
                        {
                            ev.SacrificeFly = true;
                            Score(ev, third);
                            _bases[2] = null;
                        }
                        if (second != null && _bases[2] == null && Chance(0.25))
                        {
                            _bases[2] = second;
                            _bases[1] = null;
                        }
                    }
                    break;
            }
        }

        BattingLine BattingFor(Player p)
        {
            if (!_result.Batting.TryGetValue(p, out var line)) _result.Batting[p] = line = new BattingLine();
            return line;
        }

        PitchingLine PitchingFor(Player p)
        {
            if (!_result.Pitching.TryGetValue(p, out var line)) _result.Pitching[p] = line = new PitchingLine();
            return line;
        }

        void RecordStats(GameEvent ev)
        {
            var bat = BattingFor(ev.Batter);
            var pit = PitchingFor(ev.Pitcher);
            bat.PlateAppearances++;
            pit.Pitches += ev.PitchesThrown;
            pit.OutsRecorded += ev.OutsAfter - ev.OutsBefore;

            bool atBat = ev.Outcome != Outcome.Walk && ev.Outcome != Outcome.HitByPitch && !ev.SacrificeFly;
            if (atBat) bat.AtBats++;

            switch (ev.Outcome)
            {
                case Outcome.Strikeout: bat.Strikeouts++; pit.Strikeouts++; break;
                case Outcome.Walk: case Outcome.HitByPitch: bat.Walks++; pit.Walks++; break;
                case Outcome.Single: bat.Hits++; pit.Hits++; break;
                case Outcome.Double: bat.Hits++; bat.Doubles++; pit.Hits++; break;
                case Outcome.Triple: bat.Hits++; bat.Triples++; pit.Hits++; break;
                case Outcome.HomeRun: bat.Hits++; bat.HomeRuns++; pit.Hits++; pit.HomeRuns++; break;
            }

            if (!ev.DoublePlay) bat.RunsBattedIn += ev.RunnersScored.Count;
            pit.Runs += ev.RunnersScored.Count;
            foreach (var runner in ev.RunnersScored) BattingFor(runner).Runs++;
        }
    }
}
