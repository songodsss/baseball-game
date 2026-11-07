using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class SalaryTalkTests
{
    static CareerState PlayedSeason(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 70;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        return career;
    }

    [Fact]
    public void ClubMakesAnOfferAfterAYearWithoutADeal()
    {
        var career = PlayedSeason(1);
        var summary = career.EndSeason();
        var talk = summary.SalaryTalk;
        Assert.NotNull(talk);
        Assert.Same(talk, career.PendingSalary);
        Assert.Equal(summary.NewSalary, talk.Offer);
        Assert.Equal(talk.Offer, career.Me.Salary); // in effect unless the player pushes
        Assert.True(talk.Ask(SalaryAsk.AskBig) > talk.Ask(SalaryAsk.AskSome));
        Assert.True(talk.Ask(SalaryAsk.AskSome) > talk.Offer);
    }

    [Fact]
    public void LeverageRaisesTheChances()
    {
        var weak = new SalaryTalk { Offer = 10000, Worth = 8000 };
        var strong = new SalaryTalk { Offer = 10000, Worth = 16000, Titles = 2 };
        Assert.True(strong.Chance(SalaryAsk.AskSome) > weak.Chance(SalaryAsk.AskSome));
        Assert.True(strong.Chance(SalaryAsk.AskBig) > weak.Chance(SalaryAsk.AskBig));
        Assert.True(weak.Chance(SalaryAsk.AskBig) < weak.Chance(SalaryAsk.AskSome));
        Assert.Equal(1, weak.Chance(SalaryAsk.Accept));
    }

    [Fact]
    public void AskingEitherRaisesTheSalaryOrCostsMorale()
    {
        int granted = 0, refused = 0;
        for (int seed = 0; seed < 12; seed++)
        {
            var career = PlayedSeason(seed);
            var talk = career.EndSeason().SalaryTalk;
            if (talk == null) continue;
            career.Me.Morale = 0;
            var result = career.Negotiate(SalaryAsk.AskBig);
            Assert.Null(career.PendingSalary);
            if (result.Granted)
            {
                granted++;
                Assert.Equal(talk.Ask(SalaryAsk.AskBig), career.Me.Salary);
            }
            else
            {
                refused++;
                Assert.Equal(talk.Offer, career.Me.Salary);
                Assert.Equal(-3, career.Me.Morale);
            }
        }
        Assert.True(granted + refused > 0);
    }

    [Fact]
    public void UnansweredOfferIsAcceptedWhenTheSeasonStarts()
    {
        var career = PlayedSeason(3);
        var talk = career.EndSeason().SalaryTalk;
        Assert.NotNull(talk);
        career.PlayDay(DailyAction.Rest);
        Assert.Null(career.PendingSalary);
        Assert.Equal(talk.Offer, career.Me.Salary);
        Assert.Throws<InvalidOperationException>(() => career.Negotiate(SalaryAsk.Accept));
    }

    [Fact]
    public void NoTalkUnderContract()
    {
        var career = PlayedSeason(4);
        career.ContractSeasonsLeft = 2;
        Assert.Null(career.EndSeason().SalaryTalk);
    }

    [Fact]
    public void PendingOfferSurvivesSaving()
    {
        var career = PlayedSeason(5);
        var talk = career.EndSeason().SalaryTalk;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(talk.Offer, loaded.PendingSalary.Offer);
        Assert.Equal(talk.Worth, loaded.PendingSalary.Worth);
    }
}
