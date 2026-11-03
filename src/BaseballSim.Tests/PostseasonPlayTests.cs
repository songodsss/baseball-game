using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class PostseasonPlayTests
{
    // A season played out, with my club made a top seed so it is in the postseason.
    static CareerState SeasonOver(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var c = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        while (!c.IsSeasonOver)
        {
            var r = c.PlayDay(c.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainContact);
            while (r.AtBat != null) r = c.ChooseApproach(Approach.Normal);
        }
        league.Standings[c.MyTeam].Wins += 100;
        return c;
    }

    [Fact]
    public void MyGamesArePlayedOneAtATimeWithBigAtBats()
    {
        var c = SeasonOver(4);
        var run = c.StartPostseason();
        Assert.True(run.Result.MyTeamIn);
        Assert.False(run.Done);
        Assert.NotNull(run.NextOpponent);
        int games = 0, asked = 0;
        while (!c.PostseasonNow.Done)
        {
            var day = c.NextPostseasonGame();
            games++;
            while (day.AtBat != null) { asked++; day = c.ChooseApproach(Approach.SitFastball); }
            Assert.NotNull(day.MyGame);
            Assert.Null(c.PendingAtBat);
            Assert.True(games < 20);
        }
        Assert.True(games >= 2);
        Assert.True(asked > 0);
        var summary = c.EndSeason();
        Assert.False(c.InPostseason);
        Assert.Equal(games, summary.Postseason.Series.Where(s => s.High == c.MyTeam || s.Low == c.MyTeam).Sum(s => s.Games.Count));
    }

    [Fact]
    public void APostseasonUnderWaySurvivesASaveTheSameWay()
    {
        var c = SeasonOver(6);
        c.StartPostseason();
        var day = c.NextPostseasonGame();
        while (day.AtBat == null && !c.PostseasonNow.Done) day = c.NextPostseasonGame();
        var bytes = SaveGame.ToBytes(new Club(), c);
        var (_, loaded) = SaveGame.FromBytes(bytes, new SeededRandom(99));
        Assert.True(loaded.InPostseason);
        Assert.Equal(c.PendingAtBat?.Inning, loaded.PendingAtBat?.Inning);
        var a = c.EndSeason().Postseason;
        var b = loaded.EndSeason().Postseason;
        Assert.Equal(a.Champion.Name, b.Champion.Name);
        Assert.Equal(a.Series.Select(s => s.HighWins * 10 + s.LowWins), b.Series.Select(s => s.HighWins * 10 + s.LowWins));
    }

    [Fact]
    public void ANewSeasonStartsRested()
    {
        var c = SeasonOver(2);
        c.Me.Fatigue = 70;
        c.EndSeason();
        Assert.Equal(0, c.Me.Fatigue);
    }
}
