using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class SpouseStoryTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void MarriedLifeHasItsOwnScenesSpacedApart()
    {
        var career = NewCareer(3);
        career.Day = 10;
        career.Me.InjuryDaysLeft = 8;
        var nurse = StoryEvents.Find("spouse_injury");
        Assert.Equal(0, nurse.Weight(career, new DayResult()));

        career.Me.StoryMarks["married"] = 0; // last season
        Assert.True(nurse.Weight(career, new DayResult()) > 0);
        Assert.Contains("아내", nurse.TextFor(career));

        career.Me.InjuryDaysLeft = 0;
        career.HitlessStreak = 3;
        var lunch = StoryEvents.Find("spouse_lunchbox");
        Assert.True(lunch.Weight(career, new DayResult()) > 0);
        // Right after one married-life scene, the next waits a few weeks.
        career.Me.StoryMarks["seen:spouse_injury"] = career.Stamp;
        Assert.Equal(0, lunch.Weight(career, new DayResult()));
    }
}
