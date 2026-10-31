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
        Assert.Equal(1 + summary.Postseason.Series.Count, lines.Count);
        Assert.Contains(summary.Postseason.Champion.Name, string.Join(" ", lines));
        if (summary.Postseason.MyTeamWon) Assert.StartsWith("우승", lines[0]);
        else Assert.Contains("우승", lines[0]);

        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(lines, loaded.Me.History.Last().Postseason);
    }
}
