using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class AllStarTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    static AllStarResult PlayToAllStar(CareerState career)
    {
        while (true)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
            if (day.AllStar != null) return day.AllStar;
        }
    }

    [Fact]
    public void StarPlaysInTheMidseasonGameWithoutItCounting()
    {
        var career = NewCareer(1, 95);
        var allStar = PlayToAllStar(career);
        Assert.Equal(career.League.GamesPerSeason / 2, career.Day);

        int played = career.League.Standings.Values.Sum(s => s.Wins + s.Losses + s.Ties);
        Assert.Equal(career.Day * career.League.Teams.Count, played); // only league games in the standings

        Assert.True(allStar.Selected);
        Assert.NotNull(allStar.MyLine);
        Assert.Contains(allStar.Game.Batting.Keys, p => p.Id == career.Me.Id);
        Assert.Equal(new[] { AllStar.EastName, AllStar.WestName }.OrderBy(n => n), new[] { allStar.Game.Home.Name, allStar.Game.Away.Name }.OrderBy(n => n));
        Assert.Equal(9, allStar.East.Lineup.Count);
        Assert.Equal(1, career.Me.AllStarSelections);
        Assert.Contains("all_star", career.Me.Achievements);
        Assert.Contains(career.SeasonNotes, n => n.Contains("올스타"));
        Assert.False(string.IsNullOrEmpty(allStar.Mvp));
    }

    [Fact]
    public void PlayerIn2군IsNotPicked()
    {
        var career = NewCareer(2, 95);
        career.InFarm = true;
        career.Me.Ratings[Stat.Contact] = career.Me.Potential[Stat.Contact] = 20; // stay down
        var allStar = PlayToAllStar(career);
        if (career.InFarm)
        {
            Assert.False(allStar.Selected);
            Assert.Equal(0, allStar.Bonus);
        }
        Assert.DoesNotContain(allStar.East.Lineup.Concat(allStar.West.Lineup), p => p.Id == career.Me.Id && career.InFarm);
    }

    static CareerState RetiringCareer(int seasons, int hitsPerSeason, string club)
    {
        var career = NewCareer(3, 80);
        for (int i = 0; i < seasons; i++)
            career.Me.History.Add(new SeasonRecord
            {
                Season = i + 1,
                Games = 144,
                Line = new BattingLine { Hits = hitsPerSeason, HomeRuns = hitsPerSeason / 6, AtBats = hitsPerSeason * 3 },
                Ratings = career.Me.Snapshot(),
                Club = club,
            });
        return career;
    }

    [Fact]
    public void GreatCareerMakesTheHallAndRetiresTheNumber()
    {
        var career = RetiringCareer(12, 200, "부산 마린스타즈");
        career.Me.Money = 123_456;
        var club = new Club();
        var retired = club.Retire(career);

        Assert.True(retired.HallOfFame);
        Assert.True(retired.HallOfFameScore >= Club.HallOfFameLine);
        Assert.Equal("부산 마린스타즈", retired.NumberRetiredBy);
        Assert.Equal(123_456, retired.Donation);
        Assert.Equal(12, retired.DonationPoints);
        Assert.Equal(0, career.Me.Money);
        Assert.Equal(retired.LegacyPoints + retired.DonationPoints, club.LegacyPoints);
    }

    [Fact]
    public void OrdinaryCareerStaysOutOfTheHall()
    {
        var retired = new Club().Retire(RetiringCareer(6, 90, null));
        Assert.False(retired.HallOfFame);
        Assert.Null(retired.NumberRetiredBy);
    }

    [Fact]
    public void HonoursSurviveSaving()
    {
        var career = RetiringCareer(12, 200, "부산 마린스타즈");
        career.Me.Money = 50_000;
        var club = new Club();
        club.Retire(career);
        var next = NewCareer(4, 50);
        next.Me.AllStarSelections = 3;
        next.Me.AllStarMvps = 1;

        var (loadedClub, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(club, next), new SeededRandom(1));
        var p = Assert.Single(loadedClub.Roster);
        var original = club.Roster[0];
        Assert.True(p.HallOfFame);
        Assert.Equal(original.HallOfFameScore, p.HallOfFameScore);
        Assert.Equal("부산 마린스타즈", p.NumberRetiredBy);
        Assert.Equal(50_000, p.Donation);
        Assert.Equal(5, p.DonationPoints);
        Assert.Equal(3, loaded.Me.AllStarSelections);
        Assert.Equal(1, loaded.Me.AllStarMvps);
    }
}
