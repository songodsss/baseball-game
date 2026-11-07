using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class BattingOrderTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    [Fact]
    public void RookiesStartAtTheBottom()
    {
        Assert.Equal(8, NewCareer(1, 40).LineupSlot);
        Assert.Equal("9번", CareerState.SlotName(8));
    }

    [Fact]
    public void BestHitterOnTheClubBatsCleanup()
    {
        var career = NewCareer(2, 95);
        career.Me.Ratings[Stat.Speed] = career.Me.Potential[Stat.Speed] = 40; // not a leadoff type
        int? old = null;
        while (old == null) old = career.PlayDay(DailyAction.Rest).OldSlot;
        Assert.Equal(8, old);
        Assert.Equal(3, career.LineupSlot);
        Assert.Contains(career.SeasonNotes, n => n.EndsWith("4번 타자"));
    }

    [Fact]
    public void FastGoodHitterLeadsOff()
    {
        var career = NewCareer(3, 80);
        career.Me.Ratings[Stat.Power] = career.Me.Potential[Stat.Power] = 60;
        career.Me.Ratings[Stat.Speed] = career.Me.Potential[Stat.Speed] = 95;
        while (career.PlayDay(DailyAction.Rest).OldSlot == null) { }
        Assert.Equal(0, career.LineupSlot);
    }

    [Fact]
    public void WeakHitterStaysLow()
    {
        var career = NewCareer(4, 25);
        for (int i = 0; i < 12; i++) career.PlayDay(DailyAction.Rest);
        Assert.True(career.LineupSlot >= 6);
    }
}
