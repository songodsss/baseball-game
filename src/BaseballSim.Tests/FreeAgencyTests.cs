using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class FreeAgencyTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    static SeasonSummary PlaySeason(CareerState career)
    {
        while (!career.IsSeasonOver) career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainPower);
        return career.EndSeason();
    }

    [Fact]
    public void OffersComeAfterEightSeasons()
    {
        var career = NewCareer(1);
        for (int season = 1; season < CareerState.FreeAgencySeasons; season++)
            Assert.Empty(PlaySeason(career).Offers);

        var offers = PlaySeason(career).Offers;
        Assert.Equal(4, offers.Count);
        Assert.Same(career.MyTeam, offers[0].Team);
        Assert.Equal(4, offers.Select(o => o.Team).Distinct().Count());
        Assert.All(offers, o => Assert.InRange(o.Salary, CareerState.MinSalary, CareerState.MaxSalary));
        Assert.All(offers, o => Assert.InRange(o.Years, 1, 4));
    }

    [Fact]
    public void SigningMovesTheClubAndFixesSalary()
    {
        var career = NewCareer(2);
        for (int season = 0; season < CareerState.FreeAgencySeasons - 1; season++) PlaySeason(career);
        var offer = PlaySeason(career).Offers[1];
        career.Sign(offer);
        Assert.Same(offer.Team, career.MyTeam);
        Assert.Equal(offer.Salary, career.Me.Salary);
        Assert.Equal(new[] { offer.Team.Name }, career.Me.Clubs);

        // Salary holds for the deal; no offers until it runs out.
        for (int season = 0; season < offer.Years - 1; season++)
        {
            var summary = PlaySeason(career);
            Assert.Equal(offer.Salary, summary.NewSalary);
            Assert.Empty(summary.Offers);
        }
        var last = PlaySeason(career);
        Assert.Equal(offer.Salary, last.NewSalary);
        Assert.Equal(career.MustRetire ? 0 : 4, last.Offers.Count);
        // My new club's games now include my player.
        var day = career.PlayDay(DailyAction.Rest);
        Assert.True(day.MyGame.Home == offer.Team || day.MyGame.Away == offer.Team);
    }

    [Fact]
    public void ContractsSurviveSaving()
    {
        var career = NewCareer(3);
        career.ContractSeasonsLeft = 3;
        career.Me.Clubs.Add("부산 마린스타즈");
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(3, loaded.ContractSeasonsLeft);
        Assert.Equal(new[] { "부산 마린스타즈" }, loaded.Me.Clubs);
    }
}
