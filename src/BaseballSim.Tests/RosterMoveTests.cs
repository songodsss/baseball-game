using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class RosterMoveTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
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
    public void SlumpingHitterGoesDownAndPlaysIn2군()
    {
        var career = NewCareer(1, 5);
        RosterMove move = null;
        while (move == null && !career.IsSeasonOver) move = career.PlayDay(DailyAction.Rest).Move;
        Assert.Equal(RosterMoveKind.Demoted, move.Kind);
        Assert.True(career.InFarm);
        Assert.Contains(career.SeasonNotes, n => n.EndsWith("2군 강등"));

        var day = career.PlayDay(DailyAction.Rest);
        Assert.True(day.InFarm);
        Assert.Null(day.MyGame);
        Assert.NotNull(day.FarmLine);
        Assert.True(day.FarmLine.PlateAppearances >= 4);
        Assert.True(career.FarmDays > 0);
    }

    [Fact]
    public void HotHitterIn2군GetsCalledUp()
    {
        var career = NewCareer(2, 90);
        career.InFarm = true;
        RosterMove move = null;
        while (move == null && !career.IsSeasonOver) move = career.PlayDay(DailyAction.Rest).Move;
        Assert.Equal(RosterMoveKind.Promoted, move.Kind);
        Assert.False(career.InFarm);
        // Called up well before the season is out.
        Assert.True(career.Day <= career.League.GamesPerSeason / 2);
    }

    [Fact]
    public void ReleasedPlayerGetsTryoutsIn2군()
    {
        var career = NewCareer(3, 60);
        career.Me.Age = 25;
        Assert.False(PlaySeason(career).Released);
        Assert.False(PlaySeason(career).Released);

        while (!career.IsSeasonOver) career.PlayDay(DailyAction.Rest);
        career.FarmDays = career.League.GamesPerSeason; // buried in 2군 all year
        var oldTeam = career.MyTeam;
        var summary = career.EndSeason();

        Assert.True(summary.Released);
        Assert.Contains("시즌 후 방출", summary.Record.Notes);
        Assert.NotEmpty(summary.Offers);
        Assert.All(summary.Offers, o => Assert.True(o.Tryout && o.Team != oldTeam && o.Years == 1));
        Assert.Throws<InvalidOperationException>(() => career.PlayDay(DailyAction.Rest));

        var offer = summary.Offers[0];
        career.Sign(offer);
        Assert.Same(offer.Team, career.MyTeam);
        Assert.True(career.InFarm);
        Assert.False(career.Released);
        Assert.Equal(0, career.ContractSeasonsLeft);
        Assert.True(career.PlayDay(DailyAction.Rest).InFarm);
    }

    [Fact]
    public void ReleasedWithNoOffersEndsTheCareer()
    {
        var career = NewCareer(4, 50);
        career.Released = true;
        Assert.True(career.MustRetire);
        career.PendingOffers.Add(new ContractOffer { Team = career.League.Teams[1], Salary = 3000, Years = 1, Tryout = true });
        Assert.False(career.MustRetire);
    }

    [Fact]
    public void TradeOffersComeAtTheDeadlineAndMoveTheClub()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var career = NewCareer(seed, 60);
            PlaySeason(career); // no trades in a rookie season
            Assert.Empty(career.Me.Clubs);
            while (!career.IsSeasonOver)
            {
                var day = career.PlayDay(DailyAction.Rest);
                if (day.Move?.Kind == RosterMoveKind.Traded)
                {
                    Assert.Same(day.Move.To, career.MyTeam);
                    Assert.True(career.TradedThisSeason);
                    break;
                }
                if (day.Trade == null) continue;

                var to = day.Trade.Team;
                Assert.NotSame(to, career.MyTeam);
                var move = career.AcceptTrade();
                Assert.Equal(RosterMoveKind.Traded, move.Kind);
                Assert.Same(to, career.MyTeam);
                Assert.Equal(to.Name, career.Me.Clubs.Last());
                Assert.Same(to, career.League.Batting[career.Me.Id].Team);
                Assert.Contains(career.SeasonNotes, n => n.Contains("트레이드"));
                Assert.Null(career.PendingTrade);
                return;
            }
        }
        Assert.Fail("No trade offer in 40 careers.");
    }

    [Fact]
    public void MovesSurviveSaving()
    {
        var career = NewCareer(5, 50);
        career.InFarm = true;
        career.FarmDays = 7;
        career.FarmLine.Hits = 9;
        career.StintDays = 4;
        career.TradedThisSeason = true;
        career.SeasonNotes.Add("5월 2군 강등");
        career.Released = true;
        career.PendingOffers.Add(new ContractOffer { Team = career.League.Teams[2], Salary = 4500, Years = 1, Tryout = true });

        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.True(loaded.InFarm);
        Assert.Equal(7, loaded.FarmDays);
        Assert.Equal(9, loaded.FarmLine.Hits);
        Assert.Equal(4, loaded.StintDays);
        Assert.True(loaded.TradedThisSeason);
        Assert.Equal(new[] { "5월 2군 강등" }, loaded.SeasonNotes);
        Assert.True(loaded.Released);
        var offer = Assert.Single(loaded.PendingOffers);
        Assert.Equal(loaded.League.Teams[2].Name, offer.Team.Name);
        Assert.Equal(4500, offer.Salary);
        Assert.True(offer.Tryout);
    }
}
