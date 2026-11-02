using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class ConditionTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void ConditionIsKnownBeforeTheDayAndVaries()
    {
        var career = NewCareer(1);
        var stars = new HashSet<int>();
        while (!career.IsSeasonOver)
        {
            int shown = career.ConditionStars;
            Assert.InRange(shown, 1, CareerState.MaxConditionStars);
            Assert.Equal(shown, career.ConditionStars); // asking twice doesn't change it
            stars.Add(shown);
            var day = career.PlayDay(career.Me.Fatigue > 55 ? DailyAction.Rest : DailyAction.TrainContact);
            if (day.Trade != null) career.DeclineTrade();
            if (day.Action != DailyAction.Rest) Assert.Equal(shown, day.ConditionStars);
        }
        Assert.True(stars.Count >= 3, string.Join(",", stars));
    }

    [Fact]
    public void TiredPlayerFeelsWorse()
    {
        var career = NewCareer(2);
        career.Me.Fatigue = 0;
        int fresh = career.TodayCondition;
        career.Me.Fatigue = 100;
        Assert.True(career.TodayCondition < fresh);
    }

    [Fact]
    public void GoodDaysTrainBetter()
    {
        Assert.Equal(1.0, CareerState.ConditionTraining(3));
        Assert.True(CareerState.ConditionTraining(4) > 1.0);
        Assert.True(CareerState.ConditionTraining(5) > CareerState.ConditionTraining(4));
        Assert.Equal("최상", CareerState.ConditionLabel(5));
    }

    [Fact]
    public void LeaderboardsRankTheLeagueAndFindMe()
    {
        var career = NewCareer(3);
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.TrainContact);
            if (day.Trade != null) career.DeclineTrade();
        }
        var boards = Leaderboard.All(career.League, career.Me.Id);
        Assert.Equal(Leaderboard.Boards.Length, boards.Count);
        foreach (var b in boards)
        {
            Assert.InRange(b.Top.Count, 1, Leaderboard.Size);
            Assert.Equal(1, b.Top[0].Rank);
            for (int i = 1; i < b.Top.Count; i++) Assert.True(b.Top[i].Rank >= b.Top[i - 1].Rank);
            if (b.Me != null)
            {
                Assert.True(b.Me.Mine);
                Assert.True(b.Me.Rank > Leaderboard.Size || b.Me.Rank == b.Top.Last().Rank);
                Assert.DoesNotContain(b.Top, r => r.Mine);
            }
        }
        var hr = boards.First(b => b.Title == Leaderboard.HomeRuns);
        int topHr = career.League.Batting.Values.Max(s => s.Line.HomeRuns);
        Assert.Equal(topHr.ToString(), hr.Top[0].Value);
        // My player shows up on the average board somewhere if he qualified.
        var avg = boards.First(b => b.Title == Leaderboard.Average);
        if (career.SeasonLine.PlateAppearances >= career.League.QualifyingPa)
            Assert.True(avg.Top.Any(r => r.Mine) || avg.Me != null);
    }
}
