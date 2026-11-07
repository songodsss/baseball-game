using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class StoryEventTests
{
    static CareerState NewCareer(int seed, int games = 36) => NewCareer(seed, Position.CenterField, PitcherRole.Starter, games);

    static CareerState NewCareer(int seed, Position position, PitcherRole role, int games = 36)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        return new CareerState(CareerState.NewPlayer("테스트", random, 0, position, 7, role), league, league.Teams[0], random);
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
                if (!MainStory.IsMain(day.Event)) events++; // main story scenes come on top
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
    public void ALegendComesWhenTheSpecialityReachesFiftyThenReturnsHalfwayThenWhenTheRecordFalls()
    {
        var career = NewCareer(7);
        career.Me.Ratings[Stat.Power] = 49;
        career.Me.Ratings[Stat.Contact] = career.Me.Ratings[Stat.Eye] = career.Me.Ratings[Stat.Speed] = career.Me.Ratings[Stat.Defense] = 40;
        Assert.Null(LegendStory.Next(career, new DayResult()));

        career.Me.Ratings[Stat.Power] = 50;
        var meet = LegendStory.Next(career, new DayResult());
        Assert.Equal("legend_jang", meet.Id);
        Assert.Contains("장태산", meet.TextFor(career));
        Assert.Contains("452홈런", meet.TextFor(career));
        SetPending(career, meet);
        career.ResolveEvent(1);
        Assert.Equal(LegendStage.Met, career.StageOf("jang"));
        Assert.Contains("jang", career.Me.MetLegends);
        Assert.Null(LegendStory.Next(career, new DayResult())); // too soon after the last visit

        // A season later and halfway to the record (36-game seasons: 57 of 113).
        career.LegendLastSeason = career.Season - 1;
        career.SeasonLine.HomeRuns = 57;
        var back = LegendStory.Next(career, new DayResult());
        Assert.Equal("legend_jang_2", back.Id);
        SetPending(career, back);
        career.ResolveEvent(0);
        Assert.Equal(LegendStage.Returned, career.StageOf("jang"));

        career.LegendLastSeason = career.Season - 1;
        career.SeasonLine.HomeRuns = 114;
        Assert.Null(LegendStory.Next(career, new DayResult())); // the record falls on a game day, not on a visit
        var broken = LegendStory.SurpassToday(career, new DayResult { MyLine = new BattingLine { PlateAppearances = 4 } });
        Assert.Equal("legend_jang_3", broken.Id);
        SetPending(career, broken);
        career.ResolveEvent(0);
        Assert.Contains("jang", career.SurpassedLegends);
        Assert.Contains(LegendStory.EndingLines(career), l => l.Contains("장태산"));
    }

    [Fact]
    public void PitchersMeetThePitchingLegendsAndOnlyChaseTheirRolesRecord()
    {
        var career = NewCareer(8, Position.Pitcher, PitcherRole.Closer);
        foreach (var s in career.Me.MyStats) career.Me.Ratings[s] = 40;
        career.Me.Ratings[Stat.Stamina] = 60;
        var meet = LegendStory.Next(career, new DayResult());
        Assert.Equal("legend_yoon", meet.Id);
        Assert.DoesNotContain("기록 도전", meet.TextFor(career)); // 309 wins is a starter's record
        career.Me.Ratings[Stat.Velocity] = 60;
        career.LegendStages["yoon"] = LegendStage.Met;
        var ma = LegendStory.Next(career, new DayResult());
        Assert.Equal("legend_ma", ma.Id);
        Assert.Contains("세이브", ma.TextFor(career));
    }

    [Fact]
    public void LegendsAreLeagueHistoryNotMyPlayers()
    {
        Assert.Equal(8, Legends.All.Count);
        Assert.Equal(5, Legends.All.Count(l => !l.ForPitchers));
        Assert.Equal(CareerPlayer.AllStats.Count(), Legends.All.Where(l => !l.ForPitchers).Select(l => l.Speciality).Distinct().Count());
        Assert.DoesNotContain(StoryEvents.All, e => e.Id.StartsWith("legend_"));
    }

    static void SetPending(CareerState career, StoryEvent ev) =>
        typeof(CareerState).GetProperty(nameof(CareerState.PendingEvent))!.SetValue(career, ev);
}

public class BothKindnessesTests
{
    [Fact]
    public void APaidKindnessAndAFreeOneCanBothBeChosen()
    {
        var ev = new StoryEvent
        {
            Id = "t", Title = "t", Text = "t", Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "쉬는 날 찾아간다", Result = "A", Apply = e => e.Morale(2) },
                new EventChoice { Label = "사인볼 한 상자를 보낸다 (100만원)", Result = "B", Apply = e => e.Money(-100) },
            },
        };
        var both = StoryEvents.WithBoth(ev);
        Assert.Equal(2, ev.Choices.Count); // the shared event is untouched
        Assert.Equal(3, both.Choices.Count);
        Assert.Equal("둘 다 한다 (100만원 · 피로 많이)", both.Choices[2].Label);

        var refusal = new StoryEvent { Id = "u", Title = "u", Text = "u", Weight = (_, __) => 0, Choices =
        {
            new EventChoice { Label = "투자한다 (1,000만원)", Result = "", Apply = e => { } },
            new EventChoice { Label = "정중히 거절한다", Result = "", Apply = e => { } },
        } };
        Assert.Same(refusal, StoryEvents.WithBoth(refusal));
    }
}

public class PartnerSpeechTests
{
    [Fact]
    public void APartnerTalksInBanmal()
    {
        Assert.Equal("\"이거 올리면 조회수 엄청날 거야. 근데 안 올릴게. 네가 정해 줘.\"",
            Dialogue.Casual("\"이거 올리면 조회수 엄청날 거예요. 근데 안 올릴게요. 선수님이 정해 주세요.\""));
        Assert.Equal("\"나 원래 선수랑 사적으로 안 만나. 고마워. 정말 좋아.\"", Dialogue.Casual("\"저 원래 선수랑 사적으로 안 만나요. 고맙습니다. 정말 좋습니다.\""));
        Assert.Equal("\"네 덕분이야. 이게 필요.\"", Dialogue.Casual("\"네 덕분이야. 이게 필요.\"")); // already 반말 stays
    }
}
