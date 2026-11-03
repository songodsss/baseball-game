using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class MainStoryTests
{
    static CareerState NewCareer(int seed, Position position = Position.CenterField, PitcherRole role = PitcherRole.Starter, int games = 36,
        StoryRoute route = StoryRoute.LastPick)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        var career = new CareerState(CareerState.NewPlayer("테스트", random, 0, position, 7, role), league, league.Teams[0], random);
        career.Story.Route = route;
        return career;
    }

    /// <summary>Plays until the career must end, answering every event with a rotating choice. Returns the main scenes in the order they came.</summary>
    static List<string> PlayToRetirement(CareerState career, int maxSeasons = 30)
    {
        var scenes = new List<string>();
        int pick = 0;
        for (int season = 0; season < maxSeasons && !career.MustRetire; season++)
        {
            // Scenes that belong before the first game: the prologue, winter ceremonies, opening day.
            for (var ev = career.SeasonStartEvent(); ev != null; ev = career.SeasonStartEvent())
            {
                if (MainStory.IsMain(ev))
                {
                    Assert.DoesNotContain(ev.Id, scenes);
                    scenes.Add(ev.Id);
                }
                career.ResolveEvent(pick++ % ev.Choices.Count);
            }
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
    [InlineData(Position.CenterField, PitcherRole.Starter, StoryRoute.LastPick)]
    [InlineData(Position.Pitcher, PitcherRole.Starter, StoryRoute.Prodigy)]
    [InlineData(Position.Pitcher, PitcherRole.Closer, StoryRoute.Walkon)]
    [InlineData(Position.CenterField, PitcherRole.Starter, StoryRoute.Walkon)]
    public void ScenesComeOnceEachInOrder(Position position, PitcherRole role, StoryRoute route)
    {
        for (int seed = 1; seed <= 3; seed++)
        {
            var career = NewCareer(seed, position, role, route: route);
            var scenes = PlayToRetirement(career);
            // In story order; a scene whose moment went by unplayed is skipped, never told late.
            var order = MainStory.SceneIds.Select(id => MainStory.IdPrefix + id).ToList();
            var positions = scenes.Select(id => order.IndexOf(id)).ToList();
            Assert.Equal(positions.OrderBy(p => p), positions);
            Assert.Equal(scenes.Count, career.Story.Log.Count);
            Assert.Equal(new[] { "main_draft", "main_roommate", "main_camp" }, scenes.Take(3));
            // A career that lasts to 37 sees nearly the whole story; any career gets past the first wall.
            if (career.Me.Age >= 38) Assert.True(scenes.Count >= MainStory.SceneCount - 6, $"seed {seed}: {scenes.Count} scenes: {string.Join(",", scenes)} | {string.Join(",", career.Story.Seen)} age {career.Me.Age}");
            Assert.True(scenes.Count >= 8, $"seed {seed}: only {scenes.Count} scenes");
        }
    }

    [Fact]
    public void PrologueOpensTheFirstSeason()
    {
        var career = NewCareer(11);
        var ev = career.SeasonStartEvent();
        Assert.Equal("main_draft", ev?.Id);
        Assert.StartsWith("프롤로그", ev.Title);
        career.ResolveEvent(0);
        Assert.Equal(new[] { "draft" }, career.Story.Seen);
        Assert.True(career.Story.BondOf(Bond.Mentor) > MainStoryState.StartBond);
        Assert.Equal("main_roommate", career.SeasonStartEvent()?.Id);
        career.ResolveEvent(0);
        Assert.Equal("main_camp", career.SeasonStartEvent()?.Id);
        career.ResolveEvent(0);
        Assert.Equal("opening", career.SeasonStartEvent()?.Id);
        career.ResolveEvent(0);
        Assert.Null(career.SeasonStartEvent());
    }

    [Fact]
    public void APrologueNotPlayedBeforeTheFirstGameIsSkippedNotToldLate()
    {
        var career = NewCareer(13);
        var day = career.PlayDay(DailyAction.TrainContact);
        Assert.False(day.Event != null && day.Event.Id.StartsWith("main_") && day.Event.Id != "main_first");
        for (int k = 0; k < 10 && !career.IsSeasonOver; k++)
        {
            if (career.PendingEvent != null) career.ResolveEvent(0);
            day = career.PlayDay(DailyAction.TrainContact);
            Assert.False(day.Event != null && (day.Event.Id == "main_draft" || day.Event.Id == "main_camp"));
        }
        Assert.Contains("camp", career.Story.Seen);
        Assert.DoesNotContain(career.Story.Log, l => l.Contains("스프링캠프"));
    }

    [Fact]
    public void TheFirstHitSceneComesRightAfterThatGame()
    {
        var career = NewCareer(14, games: 144);
        for (var ev = career.SeasonStartEvent(); ev != null; ev = career.SeasonStartEvent()) career.ResolveEvent(0);
        while (!career.IsSeasonOver)
        {
            int before = career.SeasonLine.Hits;
            var day = career.PlayDay(DailyAction.TrainContact);
            bool firstHit = before == 0 && career.SeasonLine.Hits > 0;
            if (firstHit)
            {
                Assert.Equal("main_first", day.Event?.Id);
                return;
            }
            Assert.NotEqual("main_first", day.Event?.Id);
            if (career.PendingEvent != null) career.ResolveEvent(0);
        }
        Assert.Fail("no hit all season");
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
        Assert.InRange(main, 3, 9);
        Assert.True(random > 0);
    }

    [Theory]
    [InlineData(Position.CenterField, PitcherRole.Starter)]
    [InlineData(Position.Pitcher, PitcherRole.Setup)]
    public void EveryChoiceOfEverySceneAppliesWithNoBlanks(Position position, PitcherRole role)
    {
        foreach (var route in MainStory.AllRoutes)
        foreach (var id in MainStory.SceneIds)
        for (int variant = 0; variant < MainStory.VariantCount(NewCareer(3, position, role, route: route), id); variant++)
            foreach (bool flags in new[] { false, true })
            {
                var preview = NewCareer(3, position, role, route: route);
                preview.PlayDay(DailyAction.Rest); // creates the rival
                if (flags) preview.Story.Flags.AddRange(new[] { "dawn", "visited", "manager", "bet", "rival_respect" });
                var ev = MainStory.Preview(preview, id, variant);
                Assert.DoesNotContain("{", ev.Title + ev.TextFor(preview));
                for (int choice = 0; choice < ev.Choices.Count; choice++)
                {
                    var career = NewCareer(3, position, role, route: route);
                    career.PlayDay(DailyAction.Rest);
                    if (flags) career.Story.Flags.AddRange(new[] { "dawn", "visited", "manager", "bet", "rival_respect" });
                    var card = MainStory.Preview(career, id, variant);
                    Assert.DoesNotContain("{", card.Choices[choice].Label + card.Choices[choice].Result);
                    card.Choices[choice].Apply(EffectsFor(career, out var outcome));
                    Assert.Contains(id, career.Story.Seen);
                    Assert.NotEmpty(card.Choices[choice].Result);
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
        career.Story.Bonds[Bond.Friend] = 95;
        var card = MainStory.Preview(career, "injury");
        card.Choices[0].Apply(EffectsFor(career, out var outcome));
        Assert.Equal(100, career.Story.BondOf(Bond.Friend));
        Assert.Contains("윤도현 유대 +5", outcome.Changes);
    }

    [Fact]
    public void ScenesWaitForTheirMoment()
    {
        var career = NewCareer(5);
        career.Story.Seen.AddRange(new[] { "draft", "roommate", "camp", "first", "first_pay", "letter", "rival", "first_winter", "sophomore" });
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
        Assert.Contains("최강훈 코치", MainStory.Fill("{mentor}", career));
        MainStory.Preview(career, "spot").Choices[0].Apply(EffectsFor(career, out _));
        Assert.Contains("최강훈 감독", MainStory.Fill("{mentor}", career));
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

        career.Story.Bonds[Bond.Mentor] = 90;
        var lines = MainStory.EndingLines(career, plain);
        Assert.Contains(lines, l => l.Contains("내 눈은 틀리지 않았다"));
        Assert.Contains(lines, l => l.Contains(career.Rival.Name));
        Assert.DoesNotContain(lines, l => l.Contains("{"));

        foreach (var route in MainStory.AllRoutes)
            foreach (var r in new[] { legend, loyal, plain })
            {
                career.Story.Route = route;
                r.MainClub = "테스트 구단";
                r.Ending = StoryEnding.None;
                var told = MainStory.EndingLines(career, r);
                Assert.True(told.Count >= 7);
                Assert.DoesNotContain(told, l => l.Contains("{") || string.IsNullOrEmpty(l));
            }
    }

    [Fact]
    public void EachStoryHasItsOwnPeople()
    {
        var names = MainStory.AllRoutes.Select(r =>
        {
            var c = NewCareer(1, route: r);
            return string.Join(",", MainStory.AllBonds.Select(b => MainStory.BondName(c, b)));
        }).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.Equal(MainStory.AllRoutes.Length, MainStory.AllRoutes.Select(MainStory.RouteName).Distinct().Count());
    }

    [Fact]
    public void NewPlayersGetTheStoryTheClubHasSeenLeast()
    {
        var club = new Club();
        Assert.Equal(StoryRoute.LastPick, MainStory.NextRoute(club));
        club.Roster.Add(new RetiredPlayer { Route = StoryRoute.LastPick, Ending = StoryEnding.BaseballSchool });
        Assert.Equal(StoryRoute.Prodigy, MainStory.NextRoute(club));
        club.Roster.Add(new RetiredPlayer { Route = StoryRoute.Prodigy, Ending = StoryEnding.Legend });
        Assert.Equal(StoryRoute.Walkon, MainStory.NextRoute(club));
        club.Roster.Add(new RetiredPlayer { Route = StoryRoute.Walkon, Ending = StoryEnding.OneClub });
        Assert.Equal(StoryRoute.LastPick, MainStory.NextRoute(club));
        // Players retired before the story existed don't count.
        club.Roster.Add(new RetiredPlayer());
        Assert.Equal(StoryRoute.LastPick, MainStory.NextRoute(club));
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
        var career = NewCareer(10, route: StoryRoute.Walkon);
        for (int i = 0; i < 25; i++)
        {
            var day = career.PlayDay(DailyAction.TrainPower);
            if (day.Event != null) career.ResolveEvent(day.Event.Choices.Count - 1);
        }
        career.Story.Flags.Add("visited");
        Assert.NotEmpty(career.Story.Seen);

        var (loadedClub, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(club, career), new SeededRandom(1));
        Assert.Equal(club.Roster[0].Ending, loadedClub.Roster[0].Ending);
        Assert.Equal(StoryRoute.Walkon, loaded.Story.Route);
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

    [Fact]
    public void EveryNewSlotHasMoreThanOneVersion()
    {
        var career = NewCareer(3);
        foreach (var id in MainStory.SceneIds) Assert.True(MainStory.VariantCount(career, id) >= 1, id);
        Assert.True(MainStory.SceneCount >= 30);
        Assert.Equal(2, MainStory.VariantCount(career, "camp"));
        Assert.Equal(2, MainStory.VariantCount(career, "body"));
    }

    [Fact]
    public void TheVersionPickedStaysTheSameForACareerButDiffersBetweenPlayers()
    {
        var titles = new HashSet<string>();
        for (int seed = 1; seed <= 12; seed++)
        {
            var career = NewCareer(seed);
            string first = MainStory.Preview(career, "camp").Title;
            Assert.Equal(first, MainStory.Preview(career, "camp").Title);
            titles.Add(first);
        }
        Assert.Equal(2, titles.Count);
    }

    [Fact]
    public void AnOldSaveSkipsNewScenesEarlierThanWhereItIs()
    {
        var career = NewCareer(5);
        // An old career that had answered up to "spot" before the new slots existed.
        career.Story.Seen.AddRange(new[] { "draft", "roommate", "first", "letter", "rival", "wall", "rooftop", "breakthrough", "spot" });
        career.Story.LastSeason = 0;
        career.Story.LastDay = 0;
        career.Me.Age = 26;
        var next = MainStory.Next(career, new DayResult());
        Assert.True(next == null || next.Id == "main_arc_reveal" || next.Id.StartsWith(LegendStory.IdPrefix), next?.Id);
        Assert.Equal(3, MainStory.Chapter(career));
    }

    [Fact]
    public void ParticlesFollowTheName()
    {
        Assert.Equal("이", Josa.After("윤도현", "가"));
        Assert.Equal("가", Josa.After("소이", "이"));
        Assert.Equal("를", Josa.After("하윤이", "을"));
        Assert.Equal("로", Josa.After("서울", "으로"));
        var career = NewCareer(3, route: StoryRoute.Prodigy);
        Assert.Equal("어머니가 웃었다", MainStory.Fill("{kin:이} 웃었다", career));
        Assert.Equal("백무진 감독이", MainStory.Fill("{mentor:가}", career));
    }

    [Fact]
    public void EveryStoryTellsItsOwnThreadAndClimax()
    {
        foreach (var route in MainStory.AllRoutes)
        {
            var career = NewCareer(21, route: route);
            foreach (var slot in new[] { "arc_hint", "arc_reveal", "arc_crisis", "arc_choice" })
                Assert.True(MainStory.VariantCount(career, slot) >= 1, $"{route} {slot}");
            // The climax is the story's own: the shared versions are left out.
            foreach (var slot in new[] { "mentor_old", "voice_feature", "last_letter", "last_game" })
                Assert.Equal(1, MainStory.VariantCount(career, slot));
        }
    }

    [Fact]
    public void ANamedLastSeasonTellsTheLastChapterAndEndsTheCareer()
    {
        var career = NewCareer(22);
        career.Story.Seen.AddRange(new[] { "draft", "roommate", "camp", "first", "first_pay", "letter" });
        career.Me.Age = 31;
        Assert.False(career.IsFinalSeason);
        Assert.True(career.DeclareLastSeason());
        Assert.False(career.DeclareLastSeason());
        Assert.True(career.IsFinalSeason);
        // The earlier chapters left untold are passed over: the last chapter opens the season.
        Assert.Equal("main_last", career.SeasonStartEvent()?.Id);
        career.ResolveEvent(0);
        Assert.Equal(6, MainStory.Chapter(career));
        Assert.False(career.MustRetire);
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainContact);
            if (day.Event != null) career.ResolveEvent(0);
        }
        Assert.Contains("last_game", career.Story.Seen);
        career.EndSeason();
        Assert.True(career.MustRetire);
    }

    [Fact]
    public void ChoicesAlongTheThreadChangeTheEnding()
    {
        var career = NewCareer(23, route: StoryRoute.Walkon);
        var retired = new RetiredPlayer { Name = "테스트", Seasons = 12, MainClub = career.MyTeam.Name };
        var plain = MainStory.EndingLines(career, retired);
        career.Story.Flags.Add("spoke_up");
        career.Story.Flags.Add("paid_debt");
        var saved = MainStory.EndingLines(career, retired);
        Assert.Contains(saved, l => l.Contains("도봉구 테스트"));
        Assert.Contains(saved, l => l.Contains("외상 장부"));
        Assert.DoesNotContain(plain, l => l.Contains("도봉구 테스트"));
    }

    [Fact]
    public void TheClosestPersonDecidesTheEndingWhenItIsNotLegendOrOneClub()
    {
        var career = NewCareer(24, route: StoryRoute.Prodigy);
        var plain = new RetiredPlayer { Name = "테스트", Seasons = 6, MainClub = career.MyTeam.Name };
        Assert.Equal(StoryEnding.BaseballSchool, MainStory.EndingFor(career, plain));
        career.Story.Bonds[Bond.Voice] = 90;
        Assert.Equal(StoryEnding.Broadcaster, MainStory.EndingFor(career, plain));
        career.Story.Bonds[Bond.Family] = 95;
        Assert.Equal(StoryEnding.Hometown, MainStory.EndingFor(career, plain));
        plain.Ending = StoryEnding.Hometown;
        var lines = MainStory.EndingLines(career, plain);
        Assert.Contains("분식집", lines[0]);
        Assert.Equal("엔딩 · 고향", MainStory.EndingTitle(StoryEnding.Hometown));
        foreach (var route in MainStory.AllRoutes)
            foreach (var ending in new[] { StoryEnding.Broadcaster, StoryEnding.Hometown, StoryEnding.Scout, StoryEnding.Partner })
            {
                var c = NewCareer(25, route: route);
                var r = new RetiredPlayer { Name = "테스트", Seasons = 6, MainClub = c.MyTeam.Name, Ending = ending };
                Assert.DoesNotContain("작은 야구 교실", string.Join("\n", MainStory.EndingLines(c, r)));
            }
    }

    [Fact]
    public void AColdOrCloseBondBringsItsOwnVersionOfAScene()
    {
        var career = NewCareer(26);
        Assert.DoesNotContain("받지 않은 전화", Enumerable.Range(0, MainStory.VariantCount(career, "kin_visit")).Select(v => MainStory.Preview(career, "kin_visit", v).Title));
        career.Story.Bonds[Bond.Family] = 20;
        Assert.Equal(1, MainStory.VariantCount(career, "kin_visit"));
        var ev = MainStory.Preview(career, "kin_visit");
        Assert.Contains("받지 않은 전화", ev.Title);
        Assert.Contains("하준", ev.Text);
        foreach (var route in MainStory.AllRoutes)
        {
            var c = NewCareer(27, route: route);
            foreach (var b in MainStory.AllBonds)
                foreach (int v in new[] { 10, 95 })
                {
                    c.Story.Bonds[b] = v;
                    foreach (var slot in new[] { "kin_visit", "pressure", "role_change", "milestone" })
                        for (int i = 0; i < MainStory.VariantCount(c, slot); i++)
                        {
                            var e = MainStory.Preview(c, slot, i);
                            Assert.DoesNotContain("{", e.Text + e.Title);
                            Assert.NotEmpty(Dialogue.Lines(e.Text, c));
                        }
                    c.Story.Bonds[b] = 50;
                }
        }
    }
}
