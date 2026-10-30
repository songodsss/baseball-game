using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class DailyMissionTests
{
    static CareerState NewCareer(int seed = 3)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void ThreeDifferentMissionsAndTheSameForADate()
    {
        var a = DailyMissions.Generate("2026-10-31");
        var b = DailyMissions.Generate("2026-10-31");
        Assert.Equal(DailyMissions.PerDay, a.Count);
        Assert.Equal(a.Count, a.Select(m => m.Kind).Distinct().Count());
        Assert.Equal(a.Select(m => m.Text), b.Select(m => m.Text));
        // Over a month the pool rotates.
        var kinds = Enumerable.Range(0, 30).SelectMany(d => DailyMissions.Generate(DailyMissions.DateOf(new DateTime(2026, 11, 1).AddDays(d)))).Select(m => m.Kind).Distinct();
        Assert.Equal(Enum.GetValues(typeof(MissionKind)).Length, kinds.Count());
    }

    [Fact]
    public void RefreshOnlyOnANewDate()
    {
        var d = new DailyMissions();
        Assert.True(d.Refresh("2026-10-31"));
        d.Today[0].Progress = 1;
        Assert.False(d.Refresh("2026-10-31"));
        Assert.Equal(1, d.Today[0].Progress);
        Assert.True(d.Refresh("2026-11-01"));
    }

    [Fact]
    public void PlayedDaysCountTowardMissionsAndRewardsPayOnce()
    {
        var career = NewCareer();
        var club = new Club();
        var d = club.Daily;
        d.Today = new System.Collections.Generic.List<Mission>
        {
            new Mission { Kind = MissionKind.Train, Goal = 3, Reward = RewardKind.Money, Amount = 800 },
            new Mission { Kind = MissionKind.Games, Goal = 2, Reward = RewardKind.RecoveryTicket, Amount = 1 },
            new Mission { Kind = MissionKind.WatchGame, Goal = 1, Reward = RewardKind.Legacy, Amount = 5 },
        };
        for (int i = 0; i < 4; i++) d.Record(career.PlayDay(DailyAction.TrainContact));
        Assert.True(d.Today[0].Done);
        Assert.True(d.Today[1].Done);
        Assert.False(d.Claim(2, club, career));

        int money = career.Me.Money;
        Assert.True(d.Claim(0, club, career));
        Assert.Equal(money + 800, career.Me.Money);
        Assert.False(d.Claim(0, club, career));
        Assert.True(d.Claim(1, club, career));
        Assert.Equal(1, d.RecoveryTickets);

        d.Count(MissionKind.WatchGame);
        Assert.True(d.Claim(2, club, career));
        Assert.Equal(5, club.LegacyPoints);
        Assert.Equal(0, d.Unclaimed);

        career.Me.Fatigue = 50;
        Assert.True(d.UseTicket(career));
        Assert.Equal(50 - DailyMissions.TicketRecovery, career.Me.Fatigue);
        Assert.False(d.UseTicket(career));
    }

    [Fact]
    public void MoneyBetweenCareersBecomesLegacy()
    {
        var club = new Club();
        club.Daily.Today = new System.Collections.Generic.List<Mission> { new Mission { Kind = MissionKind.Rest, Goal = 1, Progress = 1, Reward = RewardKind.Money, Amount = 1000 } };
        Assert.True(club.Daily.Claim(0, club, null));
        Assert.Equal(10, club.LegacyPoints);
    }

    [Fact]
    public void CheckInStreakCyclesAndResetsAfterAMissedDay()
    {
        var club = new Club();
        var career = NewCareer();
        var d = club.Daily;
        var start = new DateTime(2026, 11, 1);
        for (int i = 0; i < 7; i++)
        {
            var reward = d.CheckIn(DailyMissions.DateOf(start.AddDays(i)), club, career);
            Assert.NotNull(reward);
            Assert.Equal(DailyMissions.CheckInReward(i + 1), reward.Value);
        }
        Assert.Equal(7, d.Streak);
        Assert.Null(d.CheckIn(DailyMissions.DateOf(start.AddDays(6)), club, career));
        Assert.True(club.LegacyPoints >= 40);

        d.CheckIn(DailyMissions.DateOf(start.AddDays(7)), club, career);
        Assert.Equal(8, d.Streak);
        Assert.Equal(DailyMissions.CheckInReward(1), DailyMissions.CheckInReward(8));
        d.CheckIn(DailyMissions.DateOf(start.AddDays(10)), club, career);
        Assert.Equal(1, d.Streak);
        Assert.Equal(9, d.TotalCheckIns);
    }

    [Fact]
    public void ClubAndCareerSaveApartAndKeepMissionsAndAlbum()
    {
        var random = new SeededRandom(9);
        var club = new Club();
        var first = NewCareer(4);
        first.Me.MetLegends.Add("jang");
        while (!first.IsSeasonOver) first.PlayDay(DailyAction.TrainPower);
        first.EndSeason();
        club.Retire(first);
        Assert.Contains("jang", club.LegendsMet);
        Assert.Equal(first.Rival != null ? 1 : 0, club.RivalsMet.Count);

        club.Daily.Refresh("2026-10-31");
        club.Daily.Today[1].Progress = 1;
        club.Daily.CheckIn("2026-10-31", club, null);
        club.Daily.RecoveryTickets = 3;

        var career = NewCareer(6);
        for (int i = 0; i < 5; i++) career.PlayDay(DailyAction.TrainEye);
        career.Me.MetLegends.Add("han");
        Assert.Equal(2, club.AllLegendsMet(career).Count);

        var loadedClub = SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club));
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), loadedClub, random);

        Assert.Equal(club.Roster.Count, loadedClub.Roster.Count);
        Assert.Equal(club.LegacyPoints, loadedClub.LegacyPoints);
        Assert.Equal("2026-10-31", loadedClub.Daily.Date);
        Assert.Equal(club.Daily.Today.Select(m => m.Text), loadedClub.Daily.Today.Select(m => m.Text));
        Assert.Equal(1, loadedClub.Daily.Today[1].Progress);
        Assert.Equal(1, loadedClub.Daily.Streak);
        Assert.Equal(3, loadedClub.Daily.RecoveryTickets);
        Assert.Equal(club.LegendsMet, loadedClub.LegendsMet);
        Assert.Equal(club.RivalsMet, loadedClub.RivalsMet);
        Assert.Equal(career.Day, loaded.Day);
        Assert.Equal(career.Me.Name, loaded.Me.Name);
        Assert.Contains("han", loaded.Me.MetLegends);

        Assert.Throws<System.IO.InvalidDataException>(() => SaveGame.ClubFromBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }
}
