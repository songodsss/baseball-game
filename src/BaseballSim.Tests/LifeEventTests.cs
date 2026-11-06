using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class LifeEventTests
{
    static CareerState NewCareer(int seed, int games = 36)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    static void SetPending(CareerState career, StoryEvent ev) =>
        typeof(CareerState).GetProperty(nameof(CareerState.PendingEvent))!.SetValue(career, ev);

    static EventOutcome Answer(CareerState career, StoryEvent ev, int choice)
    {
        SetPending(career, ev);
        return career.ResolveEvent(choice);
    }

    [Fact]
    public void AnsweredEventsAreRemembered()
    {
        var career = NewCareer(1);
        career.Day = 5;
        Answer(career, StoryEvents.Find("night_out"), 1);
        Assert.True(career.Seen("night_out"));
        Assert.True(career.SeenThisSeason("night_out"));
        Assert.Equal(0, career.GamesSince("seen:night_out"));
        career.Day = 9;
        Assert.Equal(4, career.GamesSince("seen:night_out"));
        career.Season++;
        Assert.False(career.SeenThisSeason("night_out"));
        Assert.Equal(36 + 4, career.GamesSince("seen:night_out"));
    }

    [Fact]
    public void CoveringForAJuniorComesBackWeeksLater()
    {
        var career = NewCareer(2);
        career.Me.Age = 24;
        var repay = StoryEvents.Find("junior_repay");
        Assert.Equal(0, repay.Weight(career, new DayResult()));

        Answer(career, StoryEvents.Find("junior_sneak"), 0);
        Assert.True(career.HasMark("junior_owes"));
        Assert.Equal(0, repay.Weight(career, new DayResult())); // not yet
        Assert.Equal(0, StoryEvents.Find("junior_sneak").Weight(career, new DayResult())); // once a career

        career.Day += 10;
        Assert.True(repay.Weight(career, new DayResult()) > 0);
        Answer(career, repay, 0);
        Assert.False(career.HasMark("junior_owes"));
        Assert.Equal(0, repay.Weight(career, new DayResult()));
    }

    [Fact]
    public void ReportingTheJuniorRaisesTrustInstead()
    {
        var career = NewCareer(3);
        var outcome = Answer(career, StoryEvents.Find("junior_sneak"), 2);
        Assert.Equal(2, career.Me.ManagerTrust);
        Assert.Contains(outcome.Changes, c => c.StartsWith("감독 신뢰"));
        Assert.False(career.HasMark("junior_owes"));
    }

    [Fact]
    public void HighTrustBringsTheManagerCall()
    {
        var career = NewCareer(4);
        var call = StoryEvents.Find("trust_high");
        Assert.Equal(0, call.Weight(career, new DayResult()));
        career.Me.ManagerTrust = 4;
        Assert.True(call.Weight(career, new DayResult()) > 0);
        Answer(career, call, 0);
        Assert.Equal(0, call.Weight(career, new DayResult())); // once a season
    }

    [Fact]
    public void AProvocationPaysOffOrBackfires()
    {
        var career = NewCareer(5);
        Answer(career, StoryEvents.Find("interview"), 2);
        Assert.True(career.HasMark("provoked"));
        var good = StoryEvents.Find("provoke_good");
        var bad = StoryEvents.Find("provoke_bad");
        var bigDay = new DayResult { MyLine = new BattingLine { PlateAppearances = 4, AtBats = 4, Hits = 3 } };
        Assert.Equal(0, good.Weight(career, bigDay)); // not the same day
        career.Day += 1;
        Assert.True(good.Weight(career, bigDay) > 0);
        career.HitlessStreak = 2;
        Assert.True(bad.Weight(career, new DayResult()) > 0);
        Answer(career, bad, 0);
        Assert.False(career.HasMark("provoked"));
    }

    [Fact]
    public void MarriageLeadsToAChildTheNextYear()
    {
        var career = NewCareer(6);
        career.Me.Age = 28;
        Answer(career, StoryEvents.Find("proposal"), 1);
        var child = StoryEvents.Find("first_child");
        Assert.Equal(0, child.Weight(career, new DayResult()));
        career.Season++;
        Assert.True(child.Weight(career, new DayResult()) > 0);
        Answer(career, child, 0);
        Assert.Equal(0, child.Weight(career, new DayResult()));
        Assert.Equal(0, StoryEvents.Find("proposal").Weight(career, new DayResult()));
    }

    [Fact]
    public void CalendarEventsComeInTheirPartOfTheSeason()
    {
        var career = NewCareer(7);
        var rain = StoryEvents.Find("rainy_season");
        career.Day = 3;
        Assert.Equal(0, rain.Weight(career, new DayResult()));
        career.Day = 18;
        Assert.True(rain.Weight(career, new DayResult()) > 0);
        Answer(career, rain, 2);
        Assert.Equal(0, rain.Weight(career, new DayResult()));
    }

    [Fact]
    public void OpeningDayAndTheLastHomeGameAreScheduled()
    {
        var career = NewCareer(8);
        career.Day = 1;
        var opening = StoryEvents.Scheduled(career, new DayResult());
        Assert.Equal("opening", opening.Id);
        Assert.Equal("프로 첫 개막전", opening.Title);
        Answer(career, opening, 0);
        Assert.Null(StoryEvents.Scheduled(career, new DayResult()));
        int last = StoryEvents.LastHomeDay(career);
        Assert.True(career.League.Matchups(last).Any(m => m.Home == career.MyTeam));
        career.Day = last; // the last home game hasn't been played yet
        Assert.Null(StoryEvents.Scheduled(career, new DayResult()));
        career.Day = last + 1;
        Assert.Equal("last_home", StoryEvents.Scheduled(career, new DayResult()).Id);
    }

    [Fact]
    public void HittersGetTheRealDerbyNotAStoryCard()
    {
        var career = NewCareer(9);
        career.Day = career.AllStarDay + 1;
        career.SeasonNotes.Add("7월 올스타 선정");
        var ev = StoryEvents.Scheduled(career, new DayResult());
        Assert.True(ev == null || ev.Id != "hr_derby"); // the derby is played at the break itself
    }

    [Fact]
    public void PitchersThrowForTheRadarGunInstead()
    {
        var random = new SeededRandom(10);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("투수", random, position: Position.Pitcher), league, league.Teams[0], random);
        career.Day = career.AllStarDay + 2;
        career.SeasonNotes.Add("7월 팬 투표 올스타");
        var derby = StoryEvents.Scheduled(career, new DayResult());
        Assert.Equal("radar_derby", derby.Id);
        Answer(career, derby, 1);
        Assert.InRange(career.Me.DerbyBest, 130, 170);
    }

    [Fact]
    public void HitStreaksAreCountedAndResetEachSeason()
    {
        var career = NewCareer(11);
        int best = 0;
        while (!career.IsSeasonOver)
        {
            career.PlayDay(DailyAction.Rest);
            best = System.Math.Max(best, career.HitStreak);
        }
        Assert.True(best > 0);
        career.EndSeason();
        Assert.Equal(0, career.HitStreak);
    }

    [Fact]
    public void StoryMemorySurvivesASave()
    {
        var career = NewCareer(12);
        career.Day = 7;
        Answer(career, StoryEvents.Find("investment"), 1);
        career.Me.StoryMarks["promise"] = 1003;
        career.Me.ManagerTrust = -3;
        career.Me.DerbyBest = 9;
        career.HitStreak = 5;
        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club)), new SeededRandom(1));
        Assert.True(loaded.Seen("investment"));
        Assert.Equal(1003, loaded.Me.StoryMarks["promise"]);
        Assert.Equal(-3, loaded.Me.ManagerTrust);
        Assert.Equal(9, loaded.Me.DerbyBest);
        Assert.Equal(5, loaded.HitStreak);
    }

    [Fact]
    public void ASeasonMixesManyDifferentEvents()
    {
        var seen = new HashSet<string>();
        for (int seed = 1; seed <= 15; seed++)
        {
            var career = NewCareer(seed);
            for (int season = 0; season < 3; season++)
            {
                while (!career.IsSeasonOver)
                {
                    var day = career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainContact);
                    if (day.AtBat != null) continue;
                    if (day.Event == null) continue;
                    seen.Add(day.Event.Id);
                    career.ResolveEvent(day.Event.Choices.Count - 1);
                }
                if (career.MustRetire) break;
                career.EndSeason();
            }
        }
        Assert.Contains("opening", seen);
        Assert.Contains("last_home", seen);
        Assert.True(seen.Count >= 30, $"only {seen.Count} kinds: {string.Join(",", seen)}");
    }

    [Fact]
    public void TheRoommateSeniorGrowsOldRetiresAndComesBack()
    {
        var career = NewCareer(13);
        career.Day = 3;
        var roommate = StoryEvents.Find("senior_roommate");
        Assert.True(roommate.Weight(career, new DayResult()) > 0);
        string text = roommate.TextFor(career);
        string senior = career.SeniorName;
        Assert.Contains(senior, text);
        Assert.Contains(senior, career.MyTeam.Lineup.Concat(career.MyTeam.Rotation).Concat(career.MyTeam.Bullpen).Select(p => p.Name));
        Assert.NotEqual(senior, career.MateName);
        Answer(career, roommate, 0);
        Assert.Equal(senior, career.SeniorName); // kept for the career
        Answer(career, StoryEvents.Find("senior_meal"), 0);

        var announce = StoryEvents.Find("senior_announce");
        career.Me.Age = 25;
        career.Day = 22;
        Assert.True(announce.Weight(career, new DayResult()) > 0);
        Answer(career, announce, 0);
        Assert.True(career.IsClose("senior"));
        var last = StoryEvents.Find("senior_last_game");
        Assert.Equal(0, last.Weight(career, new DayResult()));
        career.Day = 24;
        Assert.True(last.Weight(career, new DayResult()) > 0);
        var outcome = Answer(career, last, 0);
        Assert.Contains(career.Me.Name, outcome.Text); // {name} filled in results too
        Assert.True(career.HasMark("senior_retired"));
        Assert.True(StoryEvents.Find("senior_locker").Weight(career, new DayResult()) > 0);

        var ceremony = StoryEvents.Find("senior_ceremony");
        Assert.Equal(0, ceremony.Weight(career, new DayResult()));
        career.Season++;
        career.Day = 4;
        Assert.Equal(0, ceremony.Weight(career, AwayDay(career))); // a home opening series
        Assert.True(ceremony.Weight(career, HomeDay(career)) > 0);
        Assert.Contains(senior, ceremony.TextFor(career));
        Answer(career, ceremony, 0);
        Assert.Equal(0, ceremony.Weight(career, new DayResult()));
        career.Season++;
        Assert.True(StoryEvents.Find("senior_shop").Weight(career, new DayResult()) > 0);
    }

    [Fact]
    public void ASeniorWhoNeverGotALastGameStillGetsACeremony()
    {
        var career = NewCareer(14);
        Answer(career, StoryEvents.Find("senior_roommate"), 0);
        Answer(career, StoryEvents.Find("senior_meal"), 1);
        career.Me.Age = 26;
        career.Day = 30;
        Answer(career, StoryEvents.Find("senior_announce"), 0);
        career.Season++;
        career.Day = 2;
        Assert.Equal(0, StoryEvents.Find("senior_last_game").Weight(career, new DayResult()));
        var ceremony = StoryEvents.Find("senior_ceremony");
        Assert.True(ceremony.Weight(career, HomeDay(career)) > 0);
        Answer(career, ceremony, 1);
        Assert.True(career.HasMark("senior_retired"));
        Assert.False(career.HasMark("senior_leaving"));
    }

    [Fact]
    public void StoryCharactersSurviveASave()
    {
        var career = NewCareer(15);
        string senior = career.SeniorName, mate = career.MateName;
        var club = new Club();
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club)), new SeededRandom(1));
        Assert.Equal(senior, loaded.SeniorName);
        Assert.Equal(mate, loaded.MateName);
    }

    [Fact]
    public void ADistantRoommateRetiresQuietly()
    {
        var career = NewCareer(16);
        Answer(career, StoryEvents.Find("senior_roommate"), 0); // closeness 1
        career.Me.Age = 25;
        career.Day = 22;
        Answer(career, StoryEvents.Find("senior_announce"), 1);
        career.Day = 24;
        Answer(career, StoryEvents.Find("senior_last_game"), 0);
        Assert.False(career.IsClose("senior"));
        Assert.Equal(0, StoryEvents.Find("senior_locker").Weight(career, new DayResult()));
        career.Season += 2;
        career.Day = 3;
        Assert.Equal(0, StoryEvents.Find("senior_ceremony").Weight(career, new DayResult()));
        Assert.Equal(0, StoryEvents.Find("senior_shop").Weight(career, new DayResult()));
    }

    [Fact]
    public void ACloseVeteranGetsAFarewellAndAnAfterStory()
    {
        var career = NewCareer(17);
        string veteran = career.VeteranName;
        var outcome = Answer(career, StoryEvents.Find("veteran_return"), 0);
        Assert.Contains(outcome.Changes, c => c.Contains(veteran) && c.Contains("친밀 +2"));
        Answer(career, StoryEvents.Find("senior_advice"), 1);
        Assert.True(career.IsClose("veteran"));

        career.Day = 25;
        Assert.Null(FindBond(career)); // too soon
        career.Season += 6;
        var farewell = FindBond(career);
        Assert.Equal("farewell:veteran", farewell.Id);
        Assert.Contains(veteran, farewell.TextFor(career));
        outcome = Answer(career, farewell, 0);
        Assert.Contains(veteran, outcome.Text);
        Assert.NotEqual(veteran, career.VeteranName); // a new veteran in everyday events
        Assert.Equal(veteran, career.BondName("exveteran"));
        Assert.Null(FindBond(career));

        career.Season += 2;
        var after = FindBond(career);
        Assert.Equal("after:exveteran", after.Id);
        Assert.Contains(veteran, after.TextFor(career));
        Answer(career, after, 0);
        Assert.Null(FindBond(career));
    }

    [Fact]
    public void ACloseCoachLeavesAndComesBackAsTheOtherManager()
    {
        var career = NewCareer(18);
        string coach = career.CoachName;
        Answer(career, StoryEvents.Find("coach_extra"), 0);
        Answer(career, StoryEvents.Find("coach_extra"), 0);
        career.Season += 6;
        career.Day = 25;
        var farewell = FindBond(career);
        Assert.Equal("farewell:coach", farewell.Id);
        Answer(career, farewell, 1);
        Assert.NotEqual(coach, career.CoachName);
        career.Season += 2;
        var after = FindBond(career);
        Assert.Equal("after:excoach", after.Id);
        Assert.Contains(coach, after.TextFor(career));
    }

    [Fact]
    public void MainStoryCharactersCanBeBefriended()
    {
        var career = NewCareer(19);
        career.Befriend("최강훈", 2);
        career.Day = 25;
        career.Season += 6;
        Assert.Null(FindBond(career)); // not close enough
        career.Befriend("최강훈");
        var farewell = FindBond(career);
        Assert.Equal("farewell:story:최강훈", farewell.Id);
        Assert.Contains("최강훈 선배가", farewell.TextFor(career));
        Answer(career, farewell, 2);
        career.Season += 2;
        Assert.Equal("after:story:최강훈", FindBond(career).Id);
    }

    [Fact]
    public void AMentoredJuniorRisesAndBecomesCaptain()
    {
        var career = NewCareer(20);
        career.Me.Age = 27;
        Answer(career, StoryEvents.Find("mentee"), 0);
        Answer(career, StoryEvents.Find("junior_sneak"), 1);
        career.Season += 3;
        career.Day = 20;
        Assert.Equal("junior_rise", FindBond(career).Id);
        Answer(career, FindBond(career), 0);
        career.Me.Age = 34;
        Assert.Equal("junior_captain", FindBond(career).Id);
    }

    [Theory]
    [InlineData("민준", "민준이 왔다. 민준을 봤다.")]
    [InlineData("서아", "서아가 왔다. 서아를 봤다.")]
    public void ParticlesFitTheName(string junior, string expected)
    {
        var career = NewCareer(21);
        career.Me.StoryMarks["junior:" + junior] = 1000;
        Assert.Equal(expected, StoryEvent.Fill("{junior}가 왔다. {junior}를 봤다.", career));
    }

    [Fact]
    public void EveryEventTextIsFilled()
    {
        var career = NewCareer(22);
        foreach (var ev in StoryEvents.All)
        {
            Assert.DoesNotContain("{", ev.TextFor(career));
            foreach (var choice in ev.Choices) Assert.DoesNotContain("{", StoryEvent.Fill(choice.Result, career));
        }
    }

    static DayResult HomeDay(CareerState c) => new DayResult { MyGame = new GameResult { Home = c.MyTeam, Away = c.League.Teams.First(t => t != c.MyTeam) } };
    static DayResult AwayDay(CareerState c) => new DayResult { MyGame = new GameResult { Away = c.MyTeam, Home = c.League.Teams.First(t => t != c.MyTeam) } };

    static DayResult BigDay => new DayResult { MyLine = new BattingLine { PlateAppearances = 4, AtBats = 4, Hits = 3 } };

    static void Step(CareerState career, string id, int choice = 0, DayResult? day = null)
    {
        var ev = StoryEvents.Find(id);
        Assert.True(ev.Weight(career, day ?? new DayResult()) > 0, id + " should be due");
        var outcome = Answer(career, ev, choice);
        Assert.False(string.IsNullOrEmpty(outcome.Text), id);
        Assert.DoesNotContain("{", outcome.Text);
        Assert.Equal(0, ev.Weight(career, day ?? new DayResult()));
    }

    [Fact]
    public void TheHighSchoolCoachStory()
    {
        var career = NewCareer(30);
        career.Me.Age = 20;
        Step(career, "teacher_call", 1);
        career.Season++;
        Step(career, "teacher_visit", 0);
        career.Season++;
        career.Me.Age = 24;
        Step(career, "teacher_ill", 1);
        career.Day += 1;
        Step(career, "teacher_kept", 0, BigDay);
    }

    [Fact]
    public void TheOldFanRecoversIfFoundOrLeavesABall()
    {
        foreach (int choice in new[] { 0, 1 })
        {
            var career = NewCareer(31 + choice);
            Step(career, "grandpa_fan", 0, HomeDay(career));
            career.Season++;
            Step(career, "grandpa_banner", 0, BigDay);
            career.Season++;
            career.Me.Age = 24;
            Step(career, "grandpa_absent", choice);
            career.Day += 10;
            if (choice == 0)
            {
                Assert.Equal(0, StoryEvents.Find("grandpa_farewell").Weight(career, new DayResult()));
                Step(career, "grandpa_hospital");
                career.Season++;
                career.Day = 4;
                career.Day = 2;
                Step(career, "grandpa_return", 0, HomeDay(career));
            }
            else
            {
                Assert.Equal(0, StoryEvents.Find("grandpa_hospital").Weight(career, new DayResult()));
                Step(career, "grandpa_farewell");
            }
        }
    }

    [Fact]
    public void TheStadiumCatStory()
    {
        var career = NewCareer(33);
        Step(career, "stadium_cat");
        career.Day += 10;
        Step(career, "cat_name", 1);
        Assert.True(career.HasMark("cat:도루"));
        Step(career, "cat_lucky", 0, BigDay);
        career.Season++;
        Step(career, "cat_mascot");
    }

    [Fact]
    public void ARomanceLeadsToTheProposal()
    {
        var career = NewCareer(34);
        career.Me.Age = 23;
        Step(career, "blind_date");
        career.Day += 10;
        Step(career, "date_anniversary", 0, AwayDay(career));
        career.Day += 20;
        Step(career, "dating_news", 0);
        var proposal = StoryEvents.Find("proposal");
        Assert.Equal(0, proposal.Weight(career, new DayResult())); // still young
        career.Me.Age = 25;
        Assert.True(proposal.Weight(career, new DayResult()) > 0);
        Answer(career, proposal, 1);
        Assert.True(career.HasMark("married"));
        Assert.False(career.HasMark("dating"));
    }

    [Fact]
    public void WithoutARomanceTheProposalWaitsUntil29()
    {
        var career = NewCareer(35);
        career.Me.Age = 27;
        Assert.Equal(0, StoryEvents.Find("proposal").Weight(career, new DayResult()));
        career.Me.Age = 29;
        Assert.True(StoryEvents.Find("proposal").Weight(career, new DayResult()) > 0);
    }

    [Fact]
    public void TheForeignTeammateStory()
    {
        var career = NewCareer(36);
        career.Season = 2;
        Step(career, "foreign_arrive");
        career.Day += 10;
        Step(career, "foreign_korean");
        career.Day += 10;
        Step(career, "foreign_homesick", 1);
        career.Season += 2;
        career.Day = 32;
        Step(career, "foreign_leave");
        career.Season += 2;
        Step(career, "foreign_letter");
    }

    [Fact]
    public void TheTrainingAideMakesItIfEncouraged()
    {
        var career = NewCareer(37);
        Step(career, "aide_meet", 1);
        career.Season++;
        Step(career, "aide_dream", 0);
        career.Day += 10;
        var ev = StoryEvents.Find("aide_result");
        var outcome = Answer(career, ev, 0);
        Assert.Contains("독립리그", outcome.Text);
    }

    // The bond moment due today, if any, without opening day or the last home game in the way.
    static StoryEvent? FindBond(CareerState career)
    {
        if (career.Day <= 3) career.Day = 4;
        var ev = StoryEvents.Scheduled(career, new DayResult());
        return ev != null && (ev.Id.StartsWith("farewell:") || ev.Id.StartsWith("after:") || ev.Id.StartsWith("junior_")) ? ev : null;
    }
}
