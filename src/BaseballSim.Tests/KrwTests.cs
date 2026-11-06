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
