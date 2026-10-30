using System;
using System.IO;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class ReliefTests
{
    static CareerState NewReliever(int seed, PitcherRole role, int rating = 65)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("불펜", random, 0, Position.Pitcher, 50, role), league, league.Teams[0], random);
        foreach (var stat in career.Me.MyStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    [Fact]
    public void LeagueBullpensHaveClosersAndSetupMen()
    {
        var random = new SeededRandom(1);
        var league = new League(new RosterGenerator(random), 36);
        var sim = new GameSimulator(random);
        for (int d = 0; d < 36; d++)
            foreach (var (a, h) in league.Matchups(d)) league.Record(sim.Play(a, h, league.NextStarter(a), league.NextStarter(h)));
        foreach (var team in league.Teams)
        {
            var closer = league.Pitching[team.Bullpen.Last().Id].Line;
            var setup = league.Pitching[team.Bullpen[team.Bullpen.Count - 2].Id].Line;
            Assert.True(closer.Saves > 0, team.Name);
            Assert.True(setup.Holds + setup.BlownSaves > 0, team.Name);
            Assert.Equal(0, closer.Starts);
        }
        Assert.True(league.Pitching.Values.Sum(p => p.Line.Saves) > 36 * 5 / 2); // a save in most wins
    }

    [Fact]
    public void ACloserPitchesTheNinthAndCollectsSaves()
    {
        var career = NewReliever(2, PitcherRole.Closer);
        career.DecideAtBats = false;
        int outings = 0;
        while (!career.IsSeasonOver)
        {
            var d = career.PlayDay(DailyAction.Rest);
            if (d.Trade != null) career.DeclineTrade();
            if (d.MyPitching != null && !d.InFarm)
            {
                outings++;
                Assert.Equal(0, d.MyPitching.Starts);
                Assert.Contains(d.MyGame.Events, e => e.Pitcher == d.MyGamePlayer && e.Inning >= 9);
            }
        }
        Assert.InRange(outings, 4, 25);
        Assert.True(career.SeasonPitching.Saves > 0);
        Assert.Equal(0, career.SeasonPitching.Wins);
        var summary = career.EndSeason();
        Assert.Contains(summary.Awards, a => a.Title == Awards.SavesTitle);
    }

    [Fact]
    public void ASetupManPitchesTheEighthForHolds()
    {
        var career = NewReliever(3, PitcherRole.Setup);
        career.DecideAtBats = false;
        while (!career.IsSeasonOver)
        {
            var d = career.PlayDay(DailyAction.Rest);
            if (d.Trade != null) career.DeclineTrade();
            if (d.MyPitching != null && !d.InFarm)
                Assert.Contains(d.MyGame.Events, e => e.Pitcher == d.MyGamePlayer && e.Inning == 8);
        }
        Assert.True(career.SeasonPitching.Holds + career.SeasonPitching.BlownSaves > 0);
        Assert.True(career.SeasonPitching.Games > 0);
    }

    [Fact]
    public void ATiredRelieverSitsOut()
    {
        var career = NewReliever(4, PitcherRole.Closer);
        career.Me.Fatigue = CareerState.ReliefFatigueLimit;
        Assert.False(career.CanPitchToday);
        var d = career.PlayDay(DailyAction.Rest);
        Assert.Null(d.MyPitching);
    }

    [Fact]
    public void RelieverGoalsAreSavesOrHolds()
    {
        Assert.Contains(NewReliever(5, PitcherRole.Closer).GoalOffer, g => g.Kind == GoalKind.Saves);
        Assert.Contains(NewReliever(6, PitcherRole.Setup).GoalOffer, g => g.Kind == GoalKind.Holds);
        Assert.DoesNotContain(NewReliever(7, PitcherRole.Closer).GoalOffer, g => g.Kind == GoalKind.Wins);
    }

    [Fact]
    public void BigSpotsPauseForAPitchAndResume()
    {
        var career = NewReliever(8, PitcherRole.Closer, 55);
        career.DecideAtBats = true;
        AtBatSituation asked = null;
        for (int i = 0; i < 36 && asked == null; i++)
        {
            var d = career.PlayDay(DailyAction.Rest);
            if (d.Trade != null) career.DeclineTrade();
            asked = d.AtBat;
        }
        Assert.NotNull(asked);
        Assert.True(asked.Pitching);
        Assert.NotNull(asked.Batter);
        Assert.InRange(asked.SitsFastball, 0.3, 0.7);
        Assert.True(career.IsMidGame);
        DayResult result;
        do result = career.ChooseApproach(asked.Hunch == Pitch.Fastball ? Approach.ThrowBreaking : Approach.ThrowFastball);
        while (result.AtBat != null && (asked = result.AtBat) != null);
        Assert.False(career.IsMidGame);
        Assert.NotNull(result.MyPitching);
        Assert.NotEmpty(result.AtBatLog);
    }

    [Fact]
    public void ThrowingWhatTheBatterIsntSittingOnHelps()
    {
        OutcomeOdds Odds() => PlateAppearanceModel.Compute(new Player("b", "b"), new Player("p", "p") { Pitching = new PitchingRatings() }, 50, 0);
        var plain = Odds();
        var fooled = Odds();
        AtBatChoice.ApplyPitch(fooled, Approach.ThrowBreaking, Pitch.Fastball);
        var guessed = Odds();
        AtBatChoice.ApplyPitch(guessed, Approach.ThrowFastball, Pitch.Fastball);
        Assert.True(fooled.Strikeout > plain.Strikeout && fooled.HomeRun < plain.HomeRun);
        Assert.True(guessed.Strikeout < plain.Strikeout && guessed.HomeRun > plain.HomeRun);
        var around = Odds();
        AtBatChoice.ApplyPitch(around, Approach.PitchAround, Pitch.Fastball);
        Assert.True(around.Walk > plain.Walk && around.HomeRun < plain.HomeRun);
    }

    [Fact]
    public void SaveKeepsTheRoleAndRelief()
    {
        var career = NewReliever(9, PitcherRole.Setup);
        career.DecideAtBats = false;
        for (int i = 0; i < 15; i++) career.PlayDay(DailyAction.Rest);
        var club = new Club();
        club.Roster.Add(new RetiredPlayer { Name = "마무리", Position = Position.Pitcher, Role = PitcherRole.Closer, CareerLine = new BattingLine(), PeakRatings = CareerPlayer.PitcherStats.ToDictionary(s => s, s => 80) });
        var stream = new MemoryStream();
        SaveGame.Write(stream, club, career);
        stream.Position = 0;
        var (loadedClub, loaded) = SaveGame.Read(stream, new SeededRandom(1));
        Assert.Equal(PitcherRole.Setup, loaded.Me.Role);
        Assert.Equal(career.SeasonPitching.Holds, loaded.SeasonPitching.Holds);
        Assert.Equal(career.SeasonPitching.BlownSaves, loaded.SeasonPitching.BlownSaves);
        Assert.Equal(PitcherRole.Closer, loadedClub.Roster[0].Role);
        // A retired closer closes for my team.
        var team = DreamTeam.Build(loadedClub, new SeededRandom(2));
        Assert.StartsWith("마무리", team.Bullpen.Last().Name);
    }
}
