using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;

public class TacticTests
{
    static (Team Away, Team Home) Teams(int seed)
    {
        var gen = new RosterGenerator(new SeededRandom(seed));
        return (gen.CreateTeam("원정"), gen.CreateTeam("홈"));
    }

    static GameResult Play(int seed, Player mine, System.Func<AtBatSituation, Approach?> decide, Team away, Team home)
    {
        var sim = new GameSimulator(new SeededRandom(seed)) { DecideFor = mine, Decide = decide };
        return sim.Play(away, home, away.Rotation[0], home.Rotation[0]);
    }

    [Fact]
    public void StealsAreOfferedOnFirstAndGoBySpeed()
    {
        int tries = 0, safe = 0, offered = 0;
        for (int seed = 1; seed <= 120; seed++)
        {
            var (away, home) = Teams(seed);
            var mine = away.Lineup[0];
            mine.Batting.Speed = 95;
            var game = Play(seed, mine, s =>
            {
                if (!s.Stealing) return Approach.Normal;
                offered++;
                Assert.Same(mine, s.Runner);
                Assert.True(s.Bases[0] && !s.Bases[1] && s.Outs < 2);
                Assert.InRange(s.StealOdds, 0.85, 0.95);
                return Approach.Steal;
            }, away, home);
            foreach (var ev in game.Events.Where(e => e.Kind == EventKind.StolenBase))
            {
                tries++;
                Assert.Same(mine, ev.Batter);
                Assert.Null(ev.BasesAfter[0]);
                if (ev.OutsAfter == ev.OutsBefore) { safe++; Assert.Same(mine, ev.BasesAfter[1]); }
                Assert.NotNull(Commentary.Describe(ev));
            }
            var line = game.Batting[mine];
            Assert.Equal(game.Events.Count(e => e.Kind == EventKind.StolenBase && e.OutsAfter == e.OutsBefore), line.StolenBases);
            Assert.True(game.Events.Count(e => e.Kind == EventKind.StolenBase) <= 2);
        }
        Assert.Equal(offered, tries);
        Assert.True(tries > 20, $"only {tries} steal tries");
        Assert.InRange((double)safe / tries, 0.75, 1.0);
        Assert.InRange(AtBatChoice.StealOdds(new Player("x", "x") { Batting = new BattingRatings { Speed = 20 } }), 0.44, 0.46);
    }

    [Fact]
    public void BuntsMoveRunnersAndDontCountAsAtBats()
    {
        int bunts = 0, sacrifices = 0;
        for (int seed = 1; seed <= 150; seed++)
        {
            var (away, home) = Teams(seed);
            var mine = home.Lineup[3];
            var game = Play(seed, mine, s => s.CanBunt ? Approach.Bunt : Approach.Normal, away, home);
            foreach (var ev in game.Events.Where(e => e.Batter == mine && e.Note != null && e.Note.Contains("번트")))
            {
                bunts++;
                Assert.Null(ev.BasesBefore[2]);
                if (ev.SacrificeBunt)
                {
                    sacrifices++;
                    Assert.Equal(ev.OutsBefore + 1, ev.OutsAfter);
                    Assert.Null(ev.BasesAfter[0]);
                    if (ev.BasesBefore[0] != null) Assert.Same(ev.BasesBefore[0], ev.BasesAfter[1]);
                    if (ev.BasesBefore[1] != null) Assert.Same(ev.BasesBefore[1], ev.BasesAfter[2]);
                }
            }
            var line = game.Batting[mine];
            int sacs = game.Events.Count(e => e.Batter == mine && e.SacrificeBunt);
            int walks = game.Events.Count(e => e.Batter == mine && (e.Outcome == Outcome.Walk || e.Outcome == Outcome.HitByPitch));
            int sf = game.Events.Count(e => e.Batter == mine && e.SacrificeFly);
            Assert.Equal(line.PlateAppearances - sacs - walks - sf, line.AtBats);
        }
        Assert.True(bunts > 30, $"only {bunts} bunts");
        Assert.InRange((double)sacrifices / bunts, 0.6, 0.97);
    }

    [Fact]
    public void HitAndRunNeverHitsIntoADoublePlay()
    {
        int tries = 0;
        for (int seed = 1; seed <= 150; seed++)
        {
            var (away, home) = Teams(seed);
            var mine = away.Lineup[4];
            var game = Play(seed, mine, s => s.CanHitAndRun ? Approach.HitAndRun : Approach.Normal, away, home);
            foreach (var ev in game.Events.Where(e => e.Batter == mine && e.Note == "히트앤런"))
            {
                tries++;
                Assert.False(ev.DoublePlay);
                if (ev.Outcome == Outcome.Single && ev.BasesBefore[2] == null && !ev.RunnersScored.Contains(ev.BasesBefore[0]))
                    Assert.Same(ev.BasesBefore[0], ev.BasesAfter[2]);
            }
        }
        Assert.True(tries > 20, $"only {tries} hit and runs");
    }

    [Fact]
    public void CareerPausesForAStealAndCountsIt()
    {
        var random = new SeededRandom(11);
        var league = new League(new RosterGenerator(random), 36);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random) { DecideAtBats = true };
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 85;
        int steals = 0, pauses = 0;
        for (int day = 0; day < 36 && !career.IsSeasonOver; day++)
        {
            var result = career.PlayDay(DailyAction.Rest);
            if (result.Trade != null) career.DeclineTrade();
            while (result.AtBat != null)
            {
                if (result.AtBat.Stealing) pauses++;
                result = career.ChooseApproach(result.AtBat.Stealing ? Approach.Steal : Approach.Normal);
            }
            if (result.MyLine != null) steals += result.MyLine.StolenBases;
        }
        Assert.True(pauses > 0);
        Assert.True(steals > 0);
        Assert.Equal(steals, career.SeasonLine.StolenBases);
        Assert.Equal(steals, career.Me.CareerStolenBases);

        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(steals, loaded.SeasonLine.StolenBases);
        Assert.Equal(steals, loaded.Me.CareerStolenBases);
    }
}
