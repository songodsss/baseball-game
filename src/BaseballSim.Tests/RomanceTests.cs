using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class RomanceTests
{
    static CareerState NewCareer(int seed, StoryRoute route, Position position = Position.CenterField)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random, 0, position, 7, PitcherRole.Starter), league, league.Teams[0], random);
        career.Story.Route = route;
        return career;
    }

    /// <summary>Plays to retirement answering every event with the given choice (clamped). Returns the event ids seen in order.</summary>
    static List<string> Play(CareerState career, Func<StoryEvent, int> choose)
    {
        var seen = new List<string>();
        for (int season = 0; season < 30 && !career.MustRetire; season++)
        {
            for (var ev = career.SeasonStartEvent(); ev != null; ev = career.SeasonStartEvent())
                career.ResolveEvent(Math.Min(choose(ev), ev.Choices.Count - 1));
            while (!career.IsSeasonOver)
            {
                var action = career.Me.Fatigue > 60 ? DailyAction.Rest : career.TrainingChoices[career.Day % career.TrainingChoices.Count];
                var day = career.PlayDay(action);
                if (day.Event == null) continue;
                seen.Add(day.Event.Id);
                career.ResolveEvent(Math.Min(choose(day.Event), day.Event.Choices.Count - 1));
            }
            career.EndSeason();
            if (career.Released && career.PendingOffers.Count > 0) career.Sign(career.PendingOffers[0]);
        }
        return seen;
    }

    [Theory]
    [InlineData(StoryRoute.LastPick)]
    [InlineData(StoryRoute.Prodigy)]
    [InlineData(StoryRoute.Walkon)]
    [InlineData(StoryRoute.Legacy)]
    public void WarmAnswersLeadToAWedding(StoryRoute route)
    {
        var career = NewCareer(11, route);
        var seen = Play(career, _ => 0);
        var heroines = Romance.Of(route);
        Assert.Equal(2, heroines.Count);
        foreach (var h in heroines) Assert.Contains($"her_{h.Key}_3", seen);
        Assert.Contains("her_choice", seen);
        var lover = Romance.Lover(career);
        Assert.NotNull(lover);
        Assert.Equal(route, lover.Route);
        Assert.True(Romance.Married(career));
        Assert.DoesNotContain("blind_date", seen);
        Assert.DoesNotContain("proposal", seen);
        var retired = new Club().Retire(career);
        Assert.Contains(MainStory.EndingLines(career, retired), l => l.Contains("결혼 엔딩") && l.Contains(lover.Name));
    }

    [Fact]
    public void ScenesComeInTurnsAndOnlyOnce()
    {
        var career = NewCareer(12, StoryRoute.LastPick);
        var seen = Play(career, _ => 0).Where(id => id.StartsWith("her_")).ToList();
        Assert.Equal(new[] { "her_han_sora_1", "her_kang_yujin_1", "her_han_sora_2", "her_kang_yujin_2", "her_han_sora_3", "her_kang_yujin_3", "her_choice" }, seen.Take(7));
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public void ColdAnswersCloseTheRomance()
    {
        var career = NewCareer(13, StoryRoute.Walkon);
        // Last choice everywhere: the cold answers in the heroine scenes.
        var seen = Play(career, ev => ev.Id.StartsWith("her_") ? ev.Choices.Count - 1 : 0);
        Assert.Contains("her_choice", seen);
        Assert.Null(Romance.Lover(career));
        Assert.All(Romance.Of(StoryRoute.Walkon), h => Assert.True(Romance.Heart(career, h.Key) < Romance.ConfessAt));
    }

    [Fact]
    public void LoverNameFillsTheGenericScenes()
    {
        var career = NewCareer(14, StoryRoute.Prodigy);
        Assert.Equal("연인", StoryEvent.Fill("{lover}", career));
        career.Me.StoryMarks["lover:soi"] = career.Stamp;
        Assert.Equal("소이가", StoryEvent.Fill("{lover}가", career));
    }
}
