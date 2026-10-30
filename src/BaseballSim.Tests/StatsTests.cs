using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class StatsTests
{
    [Fact]
    public void RateStats()
    {
        var l = new BattingLine { PlateAppearances = 10, AtBats = 8, Hits = 3, Doubles = 1, HomeRuns = 1, Walks = 2 };
        Assert.Equal(0.375, Stats.Avg(l));
        Assert.Equal(0.5, Stats.Obp(l));
        Assert.Equal(0.875, Stats.Slg(l)); // 3 + 1 + 3 = 7 bases
        Assert.Equal(1.375, Stats.Ops(l));
        Assert.Equal(".375", Stats.Rate(Stats.Avg(l)));
        Assert.Equal("1.375", Stats.Rate(Stats.Ops(l)));
        Assert.Equal(".000", Stats.Rate(Stats.Avg(new BattingLine())));
    }

    [Fact]
    public void CareerTotalIncludesTheSeasonInProgress()
    {
        var random = new SeededRandom(8);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        while (!career.IsSeasonOver) career.PlayDay(DailyAction.TrainContact);
        int firstSeasonHits = career.SeasonLine.Hits;
        career.EndSeason();
        for (int i = 0; i < 10; i++) career.PlayDay(DailyAction.TrainContact);

        Assert.Equal(firstSeasonHits + career.SeasonLine.Hits, Stats.CareerTotal(career).Hits);
    }

    [Fact]
    public void EveryCareerStartsIn2026()
    {
        Assert.Equal(2026, CareerState.YearOf(1));
        Assert.Equal(2045, CareerState.YearOf(20));
    }

    [Fact]
    public void GamesBehindTheLeader()
    {
        var random = new SeededRandom(9);
        var league = new League(new RosterGenerator(random), 36);
        var a = league.Standings[league.Teams[0]];
        var b = league.Standings[league.Teams[1]];
        a.Wins = 10; a.Losses = 4;
        b.Wins = 8; b.Losses = 7;
        Assert.Equal(0, league.GamesBehind(a));
        Assert.Equal(2.5, league.GamesBehind(b));
    }
}
