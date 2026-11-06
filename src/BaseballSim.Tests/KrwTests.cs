using BaseballSim.Career;
using Xunit;

public class KrwTests
{
    [Theory]
    [InlineData(300, "300만원")]
    [InlineData(5000, "5,000만원")]
    [InlineData(10000, "1억원")]
    [InlineData(12345, "1.2억원")]
    [InlineData(155000, "15.5억원")]
    [InlineData(1234567, "123억원")]
    [InlineData(-300, "-300만원")]
    public void MoneyIsShownShort(long manwon, string shown) => Assert.Equal(shown, Krw.Format(manwon));

    [Fact]
    public void GainsCarryAPlus() => Assert.Equal("+1.5억원", Krw.Signed(15000));
}

public class PopularityPerkTests
{
    [Fact]
    public void PopularityHasTiersAndShowsWhatItGives()
    {
        Assert.Equal("무명", BaseballSim.Career.Career.PopularityTier(5));
        Assert.Equal("스타", BaseballSim.Career.Career.PopularityTier(65));
        Assert.Equal("국민 스타", BaseballSim.Career.Career.PopularityTier(100));
        Assert.Equal(1.25, BaseballSim.Career.Career.PopularityPay(100), 3);
        var perks = BaseballSim.Career.Career.PopularityPerks(55);
        Assert.Contains(perks, p => p.On && p.Text.Contains("홈경기"));
        Assert.Contains(perks, p => !p.On && p.Text.Contains("팬 투표"));
    }
}

public class TrainingCritTests
{
    [Fact]
    public void SomeTrainingDaysClick()
    {
        var random = new BaseballSim.SeededRandom(4);
        var league = new BaseballSim.Career.League(new BaseballSim.RosterGenerator(random), 144);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
        int great = 0, days = 0;
        while (days < 120 && !career.IsSeasonOver)
        {
            var action = career.Me.Fatigue >= 60 ? DailyAction.Rest : career.TrainingChoices[0];
            var r = career.PlayDay(action);
            if (r.Event != null) career.ResolveEvent(0);
            if (r.AtBat != null) continue;
            if (action != DailyAction.Rest) days++;
            if (r.TrainingCrit > 0) great++;
        }
        Assert.InRange(great, 3, 30);
    }
}
