using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class PostseasonRecordTests
{
    [Fact]
    public void EachSeasonKeepsItsAutumnThroughASave()
    {
        var random = new SeededRandom(4);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        var summary = career.EndSeason();
        var lines = summary.Record.Postseason;
        Assert.Equal(2 + summary.Postseason.Series.Count, lines.Count); // my result, each series, the KS MVP
        Assert.Contains(summary.Postseason.Champion.Name, string.Join(" ", lines));
        if (summary.Postseason.MyTeamWon) Assert.StartsWith("우승", lines[0]);
        else Assert.Contains("우승", lines[0]);

        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(lines, loaded.Me.History.Last().Postseason);
    }
}

public class OutfieldPositionTests
{
    [Fact]
    public void OutfieldSplitsIntoThreeSpotsThatShareTheGloves()
    {
        Assert.Contains(Position.LeftField, Positions.Fielders);
        Assert.Contains(Position.CenterField, Positions.Fielders);
        Assert.Contains(Position.RightField, Positions.Fielders);
        Assert.DoesNotContain(Position.Outfield, Positions.Fielders);
        Assert.Equal("CF", Positions.Short(Position.CenterField));
        Assert.Equal("RF", Positions.Name(Position.RightField));
        Assert.Equal("우익수", Positions.KoreanName(Position.RightField));
        Assert.Equal("CP", Positions.RoleName(PitcherRole.Closer));
        Assert.Equal(Position.LeftField, Positions.OfLineupIndex(5));
        Assert.Equal(Position.RightField, Positions.OfLineupIndex(7));
        Assert.Equal(Position.DesignatedHitter, Positions.OfLineupIndex(8));
        Assert.Equal(Position.Outfield, Positions.GloveGroup(Position.LeftField));
        Assert.Equal(Position.CenterField, Positions.Upgrade(Position.Outfield));
    }

    [Fact]
    public void AnOldOutfielderLoadsAsACenterFielder()
    {
        var random = new SeededRandom(2);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random, 0, Position.Outfield), league, league.Teams[0], random);
        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(Position.CenterField, loaded.Me.Position);
    }
}
