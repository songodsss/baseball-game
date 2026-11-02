using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class ChallengeAndPassTests
{
    static CareerState NewCareer(int seed = 7)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void WeeksStartOnMonday()
    {
        Assert.Equal("2026-11-02", DailyMissions.WeekOf("2026-11-02")); // Monday
        Assert.Equal("2026-11-02", DailyMissions.WeekOf("2026-11-08")); // Sunday
        Assert.Equal("2026-11-09", DailyMissions.WeekOf("2026-11-09"));
    }

    [Fact]
    public void WeeklyChallengesRenewEachWeekAndCountAlongDailies()
    {
        var d = new DailyMissions();
        d.Refresh("2026-11-03");
        Assert.Equal(DailyMissions.PerWeek, d.Week.Count);
        Assert.Equal(d.Week.Count, d.Week.Select(m => m.Kind).Distinct().Count());
        Assert.All(d.Week, m => Assert.True(m.Weekly));
        var first = d.Week.Select(m => m.Text).ToList();
        d.Count(d.Week[0].Kind, 3);
        d.Refresh("2026-11-05");
        Assert.Equal(3, d.Week[0].Progress); // same week keeps progress
        d.Refresh("2026-11-10");
        Assert.All(d.Week, m => Assert.Equal(0, m.Progress));
        Assert.Equal(first.Count, d.Week.Count);
    }

    [Fact]
    public void ClaimingWeeklyGivesRewardAndPassPoints()
    {
        var club = new Club();
        var d = club.Daily;
        d.Refresh("2026-11-03");
        var m = d.Week[0];
        m.Progress = m.Goal;
        int coins = club.Coins, tickets = d.RecoveryTickets;
        Assert.True(d.ClaimWeekly(0, club, null));
        Assert.False(d.ClaimWeekly(0, club, null));
        Assert.True(club.Coins > coins || d.RecoveryTickets > tickets);
        Assert.Equal(SeasonPass.WeeklyPoints, d.Pass.Points);
    }

    [Fact]
    public void DailyClaimsFeedTheWeeklyDailyDoneChallengeAndPass()
    {
        var club = new Club();
        var d = club.Daily;
        d.Refresh("2026-11-03");
        d.Week = DailyMissions.GenerateWeek("2026-11-02");
        d.Week[0] = new Mission { Kind = MissionKind.DailyDone, Goal = 12, Reward = RewardKind.Coins, Amount = 300, Weekly = true };
        d.Today[0].Progress = d.Today[0].Goal;
        Assert.True(d.Claim(0, club, NewCareer()));
        Assert.Equal(1, d.Week[0].Progress);
        Assert.Equal(SeasonPass.MissionPoints, d.Pass.Points);
    }

    [Fact]
    public void PassLevelsRewardsAndSeasonReset()
    {
        var club = new Club();
        var pass = club.Daily.Pass;
        pass.Refresh("2026-11-03");
        pass.Add(SeasonPass.PointsPerLevel * 10);
        Assert.Equal(10, pass.Level);
        Assert.NotNull(pass.Claim(1, false, club));
        Assert.Null(pass.Claim(1, false, club));
        Assert.Null(pass.Claim(11, false, club)); // not reached
        Assert.Null(pass.Claim(10, true, club));  // paid track closed
        pass.Premium = true;
        var r = pass.Claim(10, true, club);
        Assert.Equal(PassRewardKind.Cosmetic, r.Value.Kind);
        Assert.True(club.Owns("homer_confetti"));
        // Thirty days on, a new pass season starts over.
        Assert.False(pass.Refresh("2026-11-30"));
        Assert.True(pass.Refresh("2026-12-01"));
        Assert.Equal(0, pass.Points);
        Assert.False(pass.Premium);
        Assert.Empty(pass.ClaimedFree);
        // An owned pass cosmetic becomes coins next time.
        pass.Premium = true;
        pass.Add(SeasonPass.PointsPerLevel * 10);
        int coins = club.Coins;
        Assert.Equal(PassRewardKind.Coins, pass.Claim(10, true, club).Value.Kind);
        Assert.Equal(coins + SeasonPass.OwnedCosmeticCoins, club.Coins);
        Assert.Equal(SeasonPass.Levels, Enumerable.Range(1, SeasonPass.Levels).Count());
    }

    [Fact]
    public void WeekAndPassSurviveSaving()
    {
        var club = new Club();
        var d = club.Daily;
        d.Refresh("2026-11-03");
        d.Week[1].Progress = 2;
        d.Pass.Add(250);
        d.Pass.Premium = true;
        d.Pass.Claim(1, false, club);
        var loaded = SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club)).Daily;
        Assert.Equal(d.WeekKey, loaded.WeekKey);
        Assert.Equal(d.Week.Select(m => (m.Kind, m.Goal, m.Progress)), loaded.Week.Select(m => (m.Kind, m.Goal, m.Progress)));
        Assert.All(loaded.Week, m => Assert.True(m.Weekly));
        Assert.Equal(250, loaded.Pass.Points);
        Assert.True(loaded.Pass.Premium);
        Assert.Contains(1, loaded.Pass.ClaimedFree);
        Assert.Equal(d.Pass.SeasonId, loaded.Pass.SeasonId);
    }
}
