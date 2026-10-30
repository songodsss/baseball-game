using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class MainStoryTests
{
    static CareerState NewCareer(int seed, Position position = Position.Outfield, PitcherRole role = PitcherRole.Starter, int games = 36)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        return new CareerState(CareerState.NewPlayer("테스트", random, 0, position, 7, role), league, league.Teams[0], random);
    }

    /// <summary>Plays until the career must end, answering every event with a rotating choice. Returns the main scenes in the order they came.</summary>
    static List<string> PlayToRetirement(CareerState career, int maxSeasons = 30)
    {
        var scenes = new List<string>();
        int pick = 0;
        for (int season = 0; season < maxSeasons && !career.MustRetire; season++)
        {
            while (!career.IsSeasonOver)
            {
                var action = career.Me.Fatigue > 60 ? DailyAction.Rest : career.TrainingChoices[career.Day % career.TrainingChoices.Count];
                var day = career.PlayDay(action);
                if (day.Event == null) continue;
                if (MainStory.IsMain(day.Event))
                {
                    Assert.DoesNotContain(day.Event.Id, scenes);
                    scenes.Add(day.Event.Id);
                }
                career.ResolveEvent(pick++ % day.Event.Choices.Count);
            }
            career.EndSeason();
            if (career.Released && career.PendingOffers.Count > 0) career.Sign(career.PendingOffers[0]);
        }
        return scenes;
    }

    [Theory]
    [InlineData(Position.Outfield, PitcherRole.Starter)]
    [InlineData(Position.Pitcher, PitcherRole.Starter)]
    [InlineData(Position.Pitcher, PitcherRole.Closer)]
    public void ScenesComeOnceEachInOrder(Position position, PitcherRole role)
    {
        for (int seed = 1; seed <= 3; seed++)
        {
            var career = NewCareer(seed, position, role);
            var scenes = PlayToRetirement(career);
            var expected = MainStory.SceneIds.Select(id => MainStory.IdPrefix + id).Take(scenes.Count).ToList();
            Assert.Equal(expected, scenes);
            Assert.Equal(scenes.Count, career.Story.Seen.Count);
            Assert.Equal(scenes.Count, career.Story.Log.Count);
            // A career that lasts to 37 has seen the whole story; any career gets past the first wall.
            if (career.Me.Age >= 38) Assert.Equal(MainStory.SceneCount, scenes.Count);
            Assert.True(scenes.Count >= 8, $"seed {seed}: only {scenes.Count} scenes");
        }
    }

    [Fact]
    public void PrologueOpensTheFirstSeason()
    {
        var career = NewCareer(11);
        var day = career.PlayDay(DailyAction.TrainContact);
        Assert.Equal("main_draft", day.Event?.Id);
        Assert.StartsWith("프롤로그", day.Event.Title);
        career.ResolveEvent(0);
        Assert.Equal(new[] { "draft" }, career.Story.Seen);
        Assert.True(career.Story.BondOf(Bond.Coach) > MainStoryState.StartBond);
    }

    [Fact]
    public void NoTwoScenesOnTheSameDayAndRandomEventsStillCome()
    {
        var career = NewCareer(12);
        int main = 0, random = 0;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainPower);
            if (day.Event == null) continue;
            if (MainStory.IsMain(day.Event)) main++; else random++;
            career.ResolveEvent(0);
        }
        Assert.InRange(main, 3, 6);
        Assert.True(random > 0);
    }

    [Theory]
    [InlineData(Position.Outfield, PitcherRole.Starter)]
    [InlineData(Position.Pitcher, PitcherRole.Setup)]
    public void EveryChoiceOfEverySceneAppliesWithNoBlanks(Position position, PitcherRole role)
    {
        foreach (var id in MainStory.SceneIds)
            foreach (bool flags in new[] { false, true })
            {
                var preview = NewCareer(3, position, role);
                preview.PlayDay(DailyAction.Rest); // creates the rival
                if (flags) preview.Story.Flags.AddRange(new[] { "dawn", "visited", "manager", "bet", "rival_respect" });
                var ev = MainStory.Preview(preview, id);
                Assert.DoesNotContain("{", ev.Title + ev.TextFor(preview));
                for (int choice = 0; choice < ev.Choices.Count; choice++)
                {
                    var career = NewCareer(3, position, role);
                    career.PlayDay(DailyAction.Rest);
                    if (flags) career.Story.Flags.AddRange(new[] { "dawn", "visited", "manager", "bet", "rival_respect" });
                    var card = MainStory.Preview(career, id);
                    Assert.DoesNotContain("{", card.Choices[choice].Label + card.Choices[choice].Result);
                    card.Choices[choice].Apply(EffectsFor(career, out var outcome));
                    Assert.Contains(id, career.Story.Seen);
                    foreach (var b in MainStory.AllBonds) Assert.InRange(career.Story.BondOf(b), 0, 100);
                    Assert.InRange(career.Me.Fatigue, 0, 100);
                    Assert.True(career.Me.Money >= 0);
                }
            }
    }

    [Fact]
    public void BondsStayBetweenZeroAndAHundred()
    {
        var career = NewCareer(4);
        career.PlayDay(DailyAction.Rest);
        career.Story.Bonds[Bond.Roommate] = 95;
        var card = MainStory.Preview(career, "injury");
        card.Choices[0].Apply(EffectsFor(career, out var outcome));
        Assert.Equal(100, career.Story.BondOf(Bond.Roommate));
        Assert.Contains("윤도현 유대 +5", outcome.Changes);
    }

    [Fact]
    public void ScenesWaitForTheirMoment()
    {
        var career = NewCareer(5);
        career.Story.Seen.AddRange(new[] { "draft", "roommate", "first", "letter", "rival" });
        career.Story.LastSeason = 1;
        career.Story.LastDay = -10;
        Assert.Null(MainStory.Next(career, new DayResult())); // season 1, no slump yet
        career.HitlessStreak = 4;
        Assert.Equal("main_wall", MainStory.Next(career, new DayResult())?.Id);
        career.Story.LastDay = career.Day; // just answered one
        Assert.Null(MainStory.Next(career, new DayResult()));
    }

    [Fact]
    public void TheCoachBecomesManagerInChapterThree()
    {
        var career = NewCareer(6);
        Assert.Contains("최강훈 코치", MainStory.Fill("{coach}", career));
        MainStory.Preview(career, "spot").Choices[0].Apply(EffectsFor(career, out _));
        Assert.Contains("최강훈 감독", MainStory.Fill("{coach}", career));
        Assert.Equal(3, MainStory.Chapter(career));
    }

    [Fact]
    public void EndingsFollowTheCareer()
    {
        var career = NewCareer(7);
        career.PlayDay(DailyAction.Rest);
        var legend = new RetiredPlayer { Name = "테스트", HallOfFame = true, Seasons = 15 };
        var loyal = new RetiredPlayer { Name = "테스트", Seasons = 12 };
        var plain = new RetiredPlayer { Name = "테스트", Seasons = 6 };
        Assert.Equal(StoryEnding.Legend, MainStory.EndingFor(career, legend));
        Assert.Equal(StoryEnding.OneClub, MainStory.EndingFor(career, loyal));
        Assert.Equal(StoryEnding.BaseballSchool, MainStory.EndingFor(career, plain));
        career.Me.Clubs.Add("다른 구단");
        Assert.Equal(StoryEnding.BaseballSchool, MainStory.EndingFor(career, loyal));

        career.Story.Bonds[Bond.Coach] = 90;
        var lines = MainStory.EndingLines(career, plain);
        Assert.Contains(lines, l => l.Contains("내 눈은 틀리지 않았다"));
        Assert.Contains(lines, l => l.Contains(career.Rival.Name));
        Assert.DoesNotContain(lines, l => l.Contains("{"));
    }

    [Fact]
    public void RetiringRecordsTheEnding()
    {
        var club = new Club();
        var career = NewCareer(8);
        PlayToRetirement(career);
        var retired = club.Retire(career);
        Assert.NotEqual(StoryEnding.None, retired.Ending);
        Assert.NotEmpty(MainStory.EndingLines(career, retired));
    }

    [Fact]
    public void StoryProgressSurvivesSaving()
    {
        var club = new Club();
        var first = NewCareer(9);
        for (int i = 0; i < 20; i++)
        {
            var day = first.PlayDay(DailyAction.TrainContact);
            if (day.Event != null) first.ResolveEvent(0);
        }
        club.Retire(first);
        var career = NewCareer(10);
        for (int i = 0; i < 25; i++)
        {
            var day = career.PlayDay(DailyAction.TrainPower);
            if (day.Event != null) career.ResolveEvent(day.Event.Choices.Count - 1);
        }
        career.Story.Flags.Add("visited");
        Assert.NotEmpty(career.Story.Seen);

        var (loadedClub, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(club, career), new SeededRandom(1));
        Assert.Equal(club.Roster[0].Ending, loadedClub.Roster[0].Ending);
        Assert.Equal(career.Story.Seen, loaded.Story.Seen);
        Assert.Equal(career.Story.Flags, loaded.Story.Flags);
        Assert.Equal(career.Story.Log, loaded.Story.Log);
        Assert.Equal(career.Story.LastSeason, loaded.Story.LastSeason);
        Assert.Equal(career.Story.LastDay, loaded.Story.LastDay);
        foreach (var b in MainStory.AllBonds) Assert.Equal(career.Story.BondOf(b), loaded.Story.BondOf(b));

        var slot = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(career.Story.Seen, slot.Story.Seen);
    }

    static EventEffects EffectsFor(CareerState career, out EventOutcome outcome)
    {
        outcome = new EventOutcome { Text = "" };
        var ctor = typeof(EventEffects).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (EventEffects)ctor.Invoke(new object[] { career, new SeededRandom(1), outcome });
    }
}
