using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class PaydayTests
{
    [Fact]
    public void SalaryComesInSixMonthlyPaydaysWithIncentives()
    {
        var random = new SeededRandom(8);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[1], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 75;
        career.Me.Salary = 6001;
        int start = career.Me.Money;
        var paydays = new System.Collections.Generic.List<Payday>();
        int hits = 0;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.TrainContact);
            if (day.Trade != null) career.DeclineTrade();
            if (day.MyLine != null) hits += day.MyLine.Hits;
            if (day.Payday == null) Assert.Equal(0, day.MoneyEarned);
            else
            {
                paydays.Add(day.Payday);
                Assert.Equal(day.Payday.Total, day.MoneyEarned);
            }
        }
        Assert.Equal(CareerState.Paydays, paydays.Count);
        Assert.Equal(new[] { "4월", "5월", "6월", "7월", "8월", "9월" }, paydays.Select(p => p.Month));
        Assert.Equal(6001, paydays.Sum(p => p.Salary));
        Assert.True(paydays.Sum(p => p.Bonus) >= 5 * hits * (League.FullSeason / 36));
        Assert.Equal(0, career.PendingBonus);

        var club = new Club();
        career.EndSeason();
        Assert.Equal(0, career.PaidMonths);
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(0, loaded.PaidMonths);
    }
}
