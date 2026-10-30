using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using Xunit.Abstractions;
using CareerState = BaseballSim.Career.Career;

public class NewsTests
{
    readonly ITestOutputHelper _out;
    public NewsTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void ThePaperFillsUpThroughASeasonAndSaves()
    {
        var random = new SeededRandom(21);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[2], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 80;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.TrainPower);
            if (day.Trade != null) career.DeclineTrade();
            Assert.True(career.News.Count(n => n.Day == career.Day && n.Season == career.Season) <= 3);
        }
        career.EndSeason();

        var news = career.News;
        Assert.InRange(news.Count, 10, CareerState.MaxNews);
        Assert.StartsWith($"{career.Year - 1} 시즌 결산", news[0].Headline);
        Assert.Contains(news, n => n.Kind == NewsKind.League);
        Assert.Contains(news, n => n.Kind == NewsKind.Me);
        // Newest first.
        for (int i = 2; i < news.Count; i++) Assert.True(news[i - 1].Day >= news[i].Day);
        foreach (var n in news.Take(15)) _out.WriteLine($"{n.Day} [{n.Kind}] {n.Headline}");

        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(news.Select(n => n.Headline), loaded.News.Select(n => n.Headline));
        Assert.Equal(news.Select(n => n.Kind), loaded.News.Select(n => n.Kind));
    }
}
