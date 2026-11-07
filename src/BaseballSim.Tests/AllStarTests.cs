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
        // A star can still hit a cold spell and be sent down before the break: take a career that made the team.
        CareerState career = null;
        AllStarResult allStar = null;
        for (int seed = 1; seed <= 10 && (allStar == null || !allStar.Selected); seed++)
        {
            career = NewCareer(seed, 95);
            allStar = PlayToAllStar(career);
        }
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
                Line = new BattingLine { Hits = hitsPerSeason, HomeRuns = hitsPerSeason / 6, Doubles = hitsPerSeason / 5, AtBats = hitsPerSeason * 3, Walks = hitsPerSeason / 3, PlateAppearances = hitsPerSeason * 3 + hitsPerSeason / 3 },
                Ratings = career.Me.Snapshot(),
                Club = club,
            });
        return career;
    }

    [Fact]
    public void GreatCareerMakesTheHallAndRetiresTheNumber()
    {
        var career = RetiringCareer(17, 220, "부산 마린스타즈");
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
        var career = RetiringCareer(17, 220, "부산 마린스타즈");
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

public class HomeRunDerbyTests
{
    [Fact]
    public void TheBreakFallsOnAMondayAndTheDerbyCrownsOneOfEight()
    {
        var random = new BaseballSim.SeededRandom(3);
        var league = new BaseballSim.Career.League(new BaseballSim.RosterGenerator(random), 144);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("t", random), league, league.Teams[0], random);
        Assert.Equal(0, career.AllStarDay % 6); // the day after a Sunday game
        BaseballSim.Career.DayResult star = null;
        while (star == null)
        {
            var d = career.PlayDay(BaseballSim.Career.DailyAction.Rest);
            while (d.AtBat != null) d = career.ChooseApproach(BaseballSim.Approach.Normal);
            if (d.AllStar != null) star = d;
            if (career.PendingEvent != null) career.ResolveEvent(0);
        }
        var derby = star.AllStar.Derby;
        Assert.Equal(8, derby.Entrants.Count);
        Assert.Contains(derby.Winner, derby.Entrants);
        Assert.Equal(3, derby.Winner.Rounds.Count);
        Assert.Equal(4, derby.Entrants.Count(e => e.Rounds.Count >= 2));
    }
}
