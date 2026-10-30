using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class WardrobeTests
{
    [Fact]
    public void PremiumUniformsNeedBuyingFirst()
    {
        var club = new Club();
        Assert.Equal(Wardrobe.DefaultId, club.SelectedUniform);
        Assert.True(club.SelectUniform("navy"));
        Assert.False(club.SelectUniform("pink"));
        club.GrantUniform("pink");
        Assert.True(club.SelectUniform("pink"));
        Assert.False(club.SelectUniform("nope"));
        Assert.All(Wardrobe.All.Where(u => u.Premium), u => Assert.False(string.IsNullOrEmpty(u.ProductId)));
    }

    [Fact]
    public void UniformsSurviveSaving()
    {
        var club = new Club();
        club.GrantUniform("black");
        club.SelectUniform("black");
        var (loaded, _) = SaveGame.FromBytes(SaveGame.ToBytes(club, null), new SeededRandom(1));
        Assert.Equal("black", loaded.SelectedUniform);
        Assert.Contains("black", loaded.OwnedUniforms);
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
