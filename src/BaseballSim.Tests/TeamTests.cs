using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class TeamTests
{
    [Fact]
    public void EveryClubHasItsOwnColour()
    {
        Assert.Equal(League.DefaultTeamNames.Length, League.DefaultTeamNames.Select(n => League.TeamColors[n]).Distinct().Count());
        Assert.Equal("#888888", League.ColorOf(new Team("없는 팀")));
    }

    [Fact]
    public void TodaysGameMatchesTheSchedule()
    {
        var random = new SeededRandom(2);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[4], random);
        var today = career.TodaysGame().Value;
        Assert.NotSame(career.MyTeam, today.Opponent);
        var day = career.PlayDay(DailyAction.Rest);
        Assert.Same(today.Home ? career.MyTeam : today.Opponent, day.MyGame.Home);
        Assert.Same(today.Home ? today.Opponent : career.MyTeam, day.MyGame.Away);
    }
}
