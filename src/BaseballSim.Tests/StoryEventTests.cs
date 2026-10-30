using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class StoryEventTests
{
    static CareerState NewCareer(int seed, int games = 36)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Theory]
    [InlineData(144)]
    [InlineData(36)]
    public void AboutEightEventsASeasonWhateverTheLength(int games)
    {
        var counts = new List<int>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var career = NewCareer(seed, games);
            int events = 0;
            while (!career.IsSeasonOver)
            {
                var day = career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainContact);
                Assert.Same(day.Event, career.PendingEvent);
                if (day.Event == null) continue;
                events++;
                career.ResolveEvent(0);
                Assert.Null(career.PendingEvent);
            }
            counts.Add(events);
        }
        Assert.InRange(counts.Average(), 5, 12);
    }

    [Fact]
    public void EveryChoiceOfEveryEventApplies()
    {
        foreach (var ev in StoryEvents.All)
            for (int choice = 0; choice < ev.Choices.Count; choice++)
            {
                var career = NewCareer(3);
                career.Me.InjuryDaysLeft = 4; // so rehab has something to shorten
                SetPending(career, ev);
                var outcome = career.ResolveEvent(choice);
                Assert.False(string.IsNullOrEmpty(outcome.Text), ev.Id);
                Assert.InRange(career.Me.Fatigue, 0, 100);
                Assert.InRange(career.Me.Morale, -CareerPlayer.MaxMorale, CareerPlayer.MaxMorale);
                Assert.True(career.Me.Money >= 0);
                Assert.DoesNotContain("{", ev.TextFor(career));
            }
    }

    [Fact]
    public void SlumpsComeUpAfterThreeHitlessGames()
    {
        var career = NewCareer(4);
        var slump = StoryEvents.Find("slump");
        career.HitlessStreak = 2;
        Assert.Equal(0, slump.Weight(career, new DayResult()));
        career.HitlessStreak = 3;
        Assert.True(slump.Weight(career, new DayResult()) > 0);
    }

    [Fact]
    public void ResolvingWithNothingPendingThrows()
    {
        Assert.Throws<InvalidOperationException>(() => NewCareer(5).ResolveEvent(0));
    }

    [Fact]
    public void MoraleFadesByOneADay()
    {
        var career = NewCareer(6);
        career.Me.Morale = 3;
        career.PlayDay(DailyAction.Rest);
        // An event that day may have moved it again, so only check the fade when none came.
        if (career.PendingEvent == null) Assert.Equal(2, career.Me.Morale);
    }

    [Fact]
    public void EachLegendVisitsOncePerCareer()
    {
        var career = NewCareer(7);
        var visit = StoryEvents.Find("legend_jang");
        Assert.True(visit.Weight(career, new DayResult()) > 0);
        Assert.Contains("장태산", visit.TextFor(career));
        SetPending(career, visit);
        int before = career.Me.Exp[Stat.Power] + career.Me.Ratings[Stat.Power] * 1000;
        career.ResolveEvent(0);
        Assert.Contains("jang", career.Me.MetLegends);
        Assert.True(career.Me.Exp[Stat.Power] + career.Me.Ratings[Stat.Power] * 1000 > before);
        Assert.Equal(0, visit.Weight(career, new DayResult()));
    }

    [Fact]
    public void LegendsAreLeagueHistoryNotMyPlayers()
    {
        Assert.Equal(5, Legends.All.Count);
        Assert.Equal(CareerPlayer.AllStats.Count(), Legends.All.Select(l => l.Speciality).Distinct().Count());
    }

    static void SetPending(CareerState career, StoryEvent ev) =>
        typeof(CareerState).GetProperty(nameof(CareerState.PendingEvent))!.SetValue(career, ev);
}
