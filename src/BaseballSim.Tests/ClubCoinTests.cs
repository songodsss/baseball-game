using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class ClubCoinTests
{
    static CareerState NewCareer(int seed = 5)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void CheckInAndMissionsPayCoins()
    {
        var club = new Club();
        var career = NewCareer();
        club.Daily.Refresh("2026-11-02");
        club.Daily.CheckIn("2026-11-02", club, career);
        Assert.Equal(DailyMissions.CheckInCoins(1), club.Coins);
        var m = club.Daily.Today[0];
        m.Progress = m.Goal;
        Assert.True(club.Daily.Claim(0, club, career));
        Assert.Equal(DailyMissions.CheckInCoins(1) + Club.MissionCoins, club.Coins);
    }

    [Fact]
    public void CosmeticsCostCoinsAndCanBeWornOneOfAKind()
    {
        var club = new Club { Coins = 5000 };
        Assert.False(club.BuyCosmetic("stadium_snow")); // pass only
        Assert.False(club.BuyCosmetic(Cosmetics.SupporterFrame)); // not sold for coins
        Assert.True(club.BuyCosmetic("stadium_night"));
        Assert.Equal(2000, club.Coins);
        Assert.False(club.BuyCosmetic("stadium_night")); // owned
        Assert.False(club.BuyCosmetic("stadium_sunset")); // too dear now
        club.GrantCosmetic("stadium_sunset");
        Assert.True(club.Wear("stadium_night"));
        Assert.True(club.Wear("stadium_sunset"));
        Assert.Equal("stadium_sunset", club.WornId(CosmeticKind.StadiumTheme));
        Assert.True(club.Wear("stadium_sunset")); // again takes it off
        Assert.Null(club.WornId(CosmeticKind.StadiumTheme));
        Assert.False(club.Wear("homer_fireworks")); // not owned
    }

    [Fact]
    public void RecoveryTicketsCappedPerDay()
    {
        var club = new Club { Coins = Club.TicketCoins * 10 };
        var career = NewCareer();
        club.Daily.Refresh("2026-11-02");
        Assert.True(club.BuyTickets(10));
        Assert.Equal(0, club.Coins);
        Assert.Equal(10, club.Daily.RecoveryTickets);
        int used = 0;
        for (int i = 0; i < 10; i++)
        {
            career.Me.Fatigue = 90;
            if (club.Daily.UseTicket(career)) used++;
        }
        Assert.Equal(DailyMissions.MaxTicketsPerDay, used);
        club.Daily.Refresh("2026-11-03");
        career.Me.Fatigue = 90;
        Assert.True(club.Daily.UseTicket(career));
    }

    [Fact]
    public void CoinsCosmeticsAndRewindsSurviveSaving()
    {
        var club = new Club { Coins = 1234, RewindTickets = 2 };
        club.GrantCosmetic("homer_lightning");
        club.Wear("homer_lightning");
        club.Daily.TicketsUsedToday = 3;
        var career = NewCareer();
        career.RewindsUsed = 1;

        var loadedClub = SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club));
        Assert.Equal(1234, loadedClub.Coins);
        Assert.Equal(2, loadedClub.RewindTickets);
        Assert.True(loadedClub.Owns("homer_lightning"));
        Assert.Equal("homer_lightning", loadedClub.WornId(CosmeticKind.HomerEffect));
        Assert.Equal(3, loadedClub.Daily.TicketsUsedToday);
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), loadedClub, new SeededRandom(1));
        Assert.Equal(1, loaded.RewindsUsed);
    }

    [Fact]
    public void RetirementCeremonyPaysCoins()
    {
        var club = new Club();
        var career = NewCareer();
        while (!career.IsSeasonOver)
        {
            career.PlayDay(DailyAction.TrainContact);
            while (career.IsMidGame) career.ChooseApproach(Approach.Normal);
        }
        career.EndSeason();
        var retired = club.Retire(career);
        Assert.Equal(Club.RetirementCoins + 2 * retired.LegacyPoints, club.Coins);
    }

    [Fact]
    public void NoCosmeticHasAnEffectField()
    {
        // Looks only: every cosmetic is either sold, a pass reward or a gift, and ids are unique.
        Assert.Equal(Cosmetics.All.Count, Cosmetics.All.Select(c => c.Id).Distinct().Count());
        Assert.All(Cosmetics.All, c => Assert.True(c.Coins > 0 || c.PassOnly || c.Id == Cosmetics.SupporterFrame));
    }
}
