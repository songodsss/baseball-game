using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class TrainingBoostTests
{
    static CareerState NewCareer(int seed, int games = 144)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void ToolsCostSalaryAndGoUpThreeTiers()
    {
        var c = NewCareer(1);
        c.Me.Money = 0;
        Assert.False(c.BuyTool(Stat.Power));
        c.Me.Money = 100000;
        for (int t = 1; t <= TrainingTools.Tiers; t++) Assert.True(c.BuyTool(Stat.Power));
        Assert.False(c.BuyTool(Stat.Power));
        Assert.Equal(30, c.ToolPercent(Stat.Power));
        Assert.Equal(100000 - 300 - 1500 - 5000, c.Me.Money);
        Assert.False(c.BuyTool(Stat.Velocity)); // a hitter has no use for it
    }

    [Fact]
    public void BoostsLastTheirDaysAndDoNotStackFromOneSource()
    {
        var c = NewCareer(2);
        c.AddBoost(Stat.Eye, 30, 5, "장마");
        c.AddBoost(Stat.Eye, 30, 5, "장마");
        c.AddBoost(Stat.Eye, 20, 2, "후배");
        Assert.Equal(50, c.BoostPercent(Stat.Eye));
        c.PlayDay(DailyAction.TrainEye);
        c.PlayDay(DailyAction.TrainEye);
        Assert.Equal(30, c.BoostPercent(Stat.Eye));
        for (int d = 0; d < 3; d++) c.PlayDay(DailyAction.Rest);
        Assert.Equal(0, c.BoostPercent(Stat.Eye));
        Assert.Empty(c.Boosts);
    }

    [Fact]
    public void ShorterSeasonsShortenBoosts()
    {
        var c = NewCareer(3, 36);
        Assert.Equal(4, c.AddBoost(Stat.Eye, 30, 14, "x").DaysLeft);
    }

    [Fact]
    public void CardToolAndBoostAddUp()
    {
        var c = NewCareer(4);
        c.CardBoost[Stat.Power] = 100;
        c.ToolTiers[Stat.Power] = 2;
        c.AddBoost(Stat.Power, 25, 10, "x");
        Assert.Equal(145, c.TrainingPercent(Stat.Power));
        Assert.Equal(2.45, c.TrainingMultiplier(Stat.Power), 6);
    }

    [Fact]
    public void ToolsAndBoostsSurviveASave()
    {
        var c = NewCareer(5);
        c.ToolTiers[Stat.Contact] = 2;
        c.AddBoost(Stat.Eye, 30, 7, "장마 영상 공부");
        var club = new Club();
        var bytes = SaveGame.ToBytes(club, c);
        var (_, loaded) = SaveGame.FromBytes(bytes, new SeededRandom(1));
        Assert.Equal(bytes, SaveGame.ToBytes(club, loaded));
        Assert.Equal(2, loaded.ToolTier(Stat.Contact));
        Assert.Equal("장마 영상 공부", loaded.Boosts.Single().Source);
        Assert.Equal(7, loaded.Boosts.Single().DaysLeft);
    }
}
