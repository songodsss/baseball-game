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
    public void CoachBoostsTrainingInTheirBestStats()
    {
        var career = NewCareer(7);
        var coach = new RetiredPlayer
        {
            Name = "코치",
            PeakRatings = new Dictionary<Stat, int> { { Stat.Contact, 85 }, { Stat.Power, 45 }, { Stat.Eye, 70 }, { Stat.Speed, 60 }, { Stat.Defense, 50 } },
        };
        Assert.Equal(0, career.CoachBonus(Stat.Contact));
        career.Coach = coach;
        Assert.Equal(0.3, career.CoachBonus(Stat.Contact), 3);
        Assert.Equal(0.2, career.CoachBonus(Stat.Eye), 3);
        Assert.Equal(0, career.CoachBonus(Stat.Power));
    }

    [Fact]
    public void CoachedCareersGrowFaster()
    {
        int Grown(bool coached)
        {
            var career = NewCareer(8);
            if (coached)
                career.Coach = new RetiredPlayer { Name = "코치", PeakRatings = CareerPlayer.AllStats.ToDictionary(s => s, s => 90) };
            int before = career.Me.Ratings[Stat.Contact];
            for (int i = 0; i < 20; i++) career.PlayDay(i % 3 == 2 ? DailyAction.Rest : DailyAction.TrainContact);
            return career.Me.Ratings[Stat.Contact] - before;
        }
        Assert.True(Grown(true) > Grown(false));
    }

    static void SetPending(CareerState career, StoryEvent ev) =>
        typeof(CareerState).GetProperty(nameof(CareerState.PendingEvent))!.SetValue(career, ev);
}
