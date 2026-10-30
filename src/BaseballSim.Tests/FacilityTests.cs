using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class FacilityTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void UpgradesCostMoneyAndStopAtMax()
    {
        var career = NewCareer(1);
        Assert.False(career.UpgradeFacility(Facility.TrainerRoom)); // broke
        career.Me.Money = 200000;
        for (int level = 1; level <= Facilities.MaxLevel; level++)
        {
            int before = career.Me.Money;
            Assert.True(career.UpgradeFacility(Facility.TrainerRoom));
            Assert.Equal(level, career.Me.FacilityLevel(Facility.TrainerRoom));
            Assert.Equal(before - Facilities.Price(level), career.Me.Money);
        }
        Assert.False(career.UpgradeFacility(Facility.TrainerRoom));
        Assert.Equal(0, career.Me.FacilityLevel(Facility.RehabCenter));
    }

    [Fact]
    public void TrainerRoomSpeedsRecovery()
    {
        int FatigueAfterRest(int level)
        {
            var career = NewCareer(2);
            if (level > 0) career.Me.Facilities[Facility.TrainerRoom] = level;
            career.Me.Fatigue = 90;
            career.PlayDay(DailyAction.Rest);
            return career.Me.Fatigue;
        }
        Assert.True(FatigueAfterRest(3) < FatigueAfterRest(0));
    }

    [Fact]
    public void ConditioningSlowsDecline()
    {
        int TotalDecline(int level)
        {
            int total = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var career = NewCareer(seed);
                career.Me.Age = 35;
                foreach (var s in CareerPlayer.AllStats) { career.Me.Ratings[s] = 70; career.Me.Potential[s] = 80; }
                if (level > 0) career.Me.Facilities[Facility.Conditioning] = level;
                total += career.EndSeason().Decline.Values.Sum();
            }
            return total;
        }
        Assert.True(TotalDecline(3) < TotalDecline(0) / 2);
    }

    [Fact]
    public void SaveKeepsFacilities()
    {
        var career = NewCareer(3);
        career.Me.Facilities[Facility.RehabCenter] = 2;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(2, loaded.Me.FacilityLevel(Facility.RehabCenter));
        Assert.Equal(0, loaded.Me.FacilityLevel(Facility.TrainerRoom));
    }
}
