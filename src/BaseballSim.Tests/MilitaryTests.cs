using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class MilitaryTests
{
    static CareerState NewCareer(int seed, int age)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 70;
        career.Me.Age = age;
        return career;
    }

    static SeasonSummary PlaySeason(CareerState career)
    {
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        return career.EndSeason();
    }

    [Fact]
    public void TurningTwentyEightWithoutGoldMeansService()
    {
        var career = NewCareer(1, 27);
        career.Season = 2; // 2027: no tournament, so no gold to exempt him
        var summary = PlaySeason(career); // now 28
        Assert.True(summary.Enlisted);
        Assert.True(career.InMilitary);
        Assert.Empty(summary.Offers);
        Assert.Null(summary.SalaryTalk);
        Assert.Null(career.GoalOffer);

        int salary = career.Me.Salary;
        int money = career.Me.Money;
        var team = career.MyTeam;
        var day = career.PlayDay(DailyAction.TrainPower);
        Assert.True(day.InFarm);
        Assert.Null(day.MyGame);
        Assert.Equal(20, day.MoneyEarned);

        var back = PlaySeason(career);
        Assert.True(back.Record.Military);
        Assert.Contains(back.Record.Notes, n => n.Contains("복무 마침"));
        Assert.False(career.InMilitary);
        Assert.False(career.InFarm);
        Assert.True(career.MilitaryDone);
        Assert.Same(team, career.MyTeam);
        Assert.Equal(salary, back.Record.Salary); // salary held through service
        Assert.False(PlaySeason(career).Enlisted);
    }

    [Fact]
    public void PlayerCanGoEarly()
    {
        var career = NewCareer(2, 20);
        var summary = PlaySeason(career); // 21
        Assert.True(summary.CanEnlist);
        career.Enlist();
        Assert.True(career.InMilitary);
        Assert.False(career.CanEnlist);
        PlaySeason(career);
        Assert.True(career.MilitaryDone);
    }

    [Fact]
    public void TooYoungToEnlist()
    {
        var career = NewCareer(3, 18);
        PlaySeason(career);
        Assert.False(career.CanEnlist);
        Assert.Throws<InvalidOperationException>(() => career.Enlist());
    }

    [Fact]
    public void GoldMedalExempts()
    {
        var career = NewCareer(4, 27);
        career.MilitaryDone = true; // as set by a gold medal
        Assert.False(PlaySeason(career).Enlisted);
        Assert.False(career.CanEnlist);
    }

    [Fact]
    public void ServiceSeasonsDontCountTowardFreeAgency()
    {
        var career = NewCareer(5, 19);
        PlaySeason(career);
        career.Enlist();
        for (int i = 0; i < CareerState.FreeAgencySeasons - 1; i++) Assert.Empty(PlaySeason(career).Offers.Where(o => !o.Tryout));
        // Eight seasons in history, but one was service.
        Assert.Equal(CareerState.FreeAgencySeasons, career.Me.History.Count);
        Assert.NotEmpty(PlaySeason(career).Offers);
    }

    [Fact]
    public void ServiceSurvivesSaving()
    {
        var career = NewCareer(6, 21);
        PlaySeason(career);
        career.Enlist();
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.True(loaded.InMilitary);
        Assert.True(loaded.InFarm);
        Assert.False(loaded.MilitaryDone);
    }
}
