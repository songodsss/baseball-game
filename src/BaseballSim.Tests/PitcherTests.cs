using System;
using System.IO;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class PitcherTests
{
    static CareerState NewPitcher(int seed, int rating = 0)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("투수", random, 0, Position.Pitcher, 18), league, league.Teams[0], random);
        if (rating > 0) foreach (var stat in career.Me.MyStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    static SeasonSummary PlaySeason(CareerState career, DailyAction action = DailyAction.Rest)
    {
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : action);
            if (day.Trade != null) career.DeclineTrade();
        }
        return career.EndSeason();
    }

    [Fact]
    public void APitcherHasPitchingRatingsOnly()
    {
        var career = NewPitcher(1);
        Assert.True(career.Me.IsPitcher);
        Assert.Equal(CareerPlayer.PitcherStats.OrderBy(s => s), career.Me.Ratings.Keys.OrderBy(s => s));
        Assert.Contains(DailyAction.TrainControl, career.TrainingChoices);
        Assert.DoesNotContain(DailyAction.TrainPower, career.TrainingChoices);
        Assert.Throws<ArgumentException>(() => career.PlayDay(DailyAction.TrainPower));
        var game = career.Me.ForGame(new SeededRandom(1));
        Assert.True(game.IsPitcher);
        Assert.Equal(career.Me.Ratings[Stat.Control], game.Pitching.Control);
    }

    [Fact]
    public void HittersCannotTrainPitching()
    {
        var random = new SeededRandom(2);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("타자", random), league, league.Teams[0], random);
        Assert.Throws<ArgumentException>(() => career.PlayDay(DailyAction.TrainVelocity));
    }

    [Fact]
    public void StartsEveryFifthGameAndTrainsOnTheWay()
    {
        var career = NewPitcher(3, 70);
        int starts = 0, rested = 0;
        for (int day = 0; day < 20; day++)
        {
            var result = career.PlayDay(career.Me.Fatigue > 50 ? DailyAction.Rest : DailyAction.TrainControl);
            if (result.MyPitching != null) starts++;
            if (result.BetweenStarts) rested++;
            Assert.Null(result.AtBat);
        }
        Assert.Equal(4, starts);
        Assert.Equal(16, rested);
        Assert.Equal(4, career.SeasonPitching.Starts);
        Assert.True(career.SeasonPitching.Outs > 0);
        Assert.True(career.League.Pitching.ContainsKey(career.Me.Id));
        Assert.Equal(career.MyTeam, career.League.Pitching[career.Me.Id].Team);
    }

    [Fact]
    public void ASeasonIsRecordedAsPitching()
    {
        var career = NewPitcher(4, 70);
        var summary = PlaySeason(career, DailyAction.TrainBreaking);
        var record = summary.Record;
        Assert.NotNull(record.Pitching);
        Assert.InRange(record.Pitching.Starts, 6, 8);
        Assert.Equal(0, record.Line.PlateAppearances);
        Assert.Equal(0, career.SeasonPitching.Outs); // reset for the new season
        Assert.All(summary.Awards, a => Assert.NotNull(a.Winner));
        Assert.Contains(summary.Awards, a => a.Title == Awards.Wins);
        Assert.Contains(summary.Awards, a => a.Title == Awards.StrikeoutTitle);
    }

    [Fact]
    public void ERAIsReadOnTheOpsScale()
    {
        Assert.Equal(0.734, CareerState.PitchingForm(new PitcherLine()), 3);
        var ace = new PitcherLine { Outs = 27 * 10, Runs = 20 };   // 2.00
        var bad = new PitcherLine { Outs = 27 * 10, Runs = 60 };   // 6.00
        Assert.True(CareerState.PitchingForm(ace) > 0.9);
        Assert.True(CareerState.PitchingForm(bad) < 0.6);
        Assert.Equal("2.00", ace.EraText);
        Assert.Equal("90.0", ace.Innings);
    }

    [Fact]
    public void DecisionsFollowTheScore()
    {
        var five = new PitchingLine { OutsRecorded = 18, Runs = 2 };
        Assert.Equal((true, false), PitcherLine.Decision(five, 4, 2));
        Assert.Equal((false, true), PitcherLine.Decision(five, 1, 3));
        var shortOuting = new PitchingLine { OutsRecorded = 12, Runs = 0 };
        Assert.Equal((false, false), PitcherLine.Decision(shortOuting, 5, 1));
    }

    [Fact]
    public void PitcherGoalsArePitchingGoals()
    {
        var career = NewPitcher(5, 60);
        var offer = career.GoalOffer;
        Assert.Equal(3, offer.Count);
        Assert.All(offer, g => Assert.Contains(g.Kind, SeasonGoal.PitcherKinds));
        var era = offer.First(g => g.Kind == GoalKind.Era);
        Assert.Contains("평균자책", era.Text);
        Assert.Equal("-.--", era.Progress(career));
    }

    [Fact]
    public void PitcherTraitsAreOffered()
    {
        var career = NewPitcher(6, 60);
        var offer = Traits.All.Where(t => t.Eligible(career.Me)).Select(t => t.Id).ToList();
        Assert.Contains(Traits.Fireballer, offer);
        Assert.DoesNotContain(Traits.Slugger, offer);
        Assert.DoesNotContain(AtBatChoice.Reader, offer);
        career.Me.Traits.Add(Traits.Pinpoint);
        Assert.Equal(66, career.Me.EffectiveRating(Stat.Control));
    }

    [Fact]
    public void StoryEventsTrainTheMatchingPitchingStat()
    {
        var career = NewPitcher(7, 40);
        career.Me.Potential[Stat.Velocity] = 70;
        var before = career.Me.Exp[Stat.Velocity];
        var ev = StoryEvents.Find("senior_advice"); // power lesson
        typeof(CareerState).GetProperty("PendingEvent").SetValue(career, ev);
        career.ResolveEvent(0);
        Assert.True(career.Me.Exp[Stat.Velocity] > before || career.Me.Ratings[Stat.Velocity] > 40);
    }

    [Fact]
    public void SaveKeepsThePitcher()
    {
        var career = NewPitcher(8, 60);
        for (int day = 0; day < 12; day++) career.PlayDay(DailyAction.TrainStamina);
        career.Me.FanPoints = 900;
        var club = new Club();
        var stream = new MemoryStream();
        SaveGame.Write(stream, club, career);
        stream.Position = 0;
        var (_, loaded) = SaveGame.Read(stream, new SeededRandom(1));
        Assert.True(loaded.Me.IsPitcher);
        Assert.Equal(18, loaded.Me.Number);
        Assert.Equal(career.SeasonPitching.Outs, loaded.SeasonPitching.Outs);
        Assert.Equal(career.SeasonPitching.Wins, loaded.SeasonPitching.Wins);
        Assert.Equal(career.DaysSinceStart, loaded.DaysSinceStart);
        Assert.Equal(career.League.Pitching.Count, loaded.League.Pitching.Count);
        Assert.Equal(career.League.Pitching[career.Me.Id].Line.Strikeouts, loaded.League.Pitching[career.Me.Id].Line.Strikeouts);
        Assert.Equal(900, loaded.Me.FanPoints);
        // And plays on.
        loaded.PlayDay(DailyAction.TrainVelocity);
    }

    [Fact]
    public void RetiredPitcherKeepsHisLine()
    {
        var career = NewPitcher(9, 65);
        PlaySeason(career, DailyAction.TrainControl);
        var club = new Club();
        var retired = club.Retire(career);
        Assert.True(retired.IsPitcher);
        Assert.NotNull(retired.CareerPitching);
        Assert.True(retired.CareerPitching.Starts > 0);
        Assert.Contains(retired.Ceremony, l => l.Contains("승"));
        Assert.Equal(career.MyTeam.Name, retired.MainClub);
    }
}

public class InjuryTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 144);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 60;
        return career;
    }

    [Fact]
    public void TrainingTiredGetsATypedInjuryThatLingers()
    {
        var career = NewCareer(1);
        DayResult hurt = null;
        for (int day = 0; day < 300 && hurt == null; day++)
        {
            career.Me.Fatigue = 95;
            var d = career.PlayDay(DailyAction.TrainPower);
            if (d.GotInjured) hurt = d;
        }
        Assert.NotNull(hurt);
        Assert.NotEqual(InjuryKind.None, hurt.Injury);
        Assert.Equal(hurt.Injury, career.Me.Injury);
        Assert.True(career.Me.LingerDays > 0);
        Assert.Contains(career.SeasonNotes, n => n.Contains(Injuries.Name(hurt.Injury)));

        // Out: no penalty shown while injured; back: the hurt stat plays a little lower until it heals.
        var stat = Injuries.StatOf(hurt.Injury);
        while (career.Me.IsInjured) career.PlayDay(DailyAction.Rest);
        Assert.Equal(career.Me.EffectiveRating(stat) - Injuries.Penalty(hurt.Injury), career.Me.GameRating(stat));
        while (career.Me.LingerDays > 0) career.PlayDay(DailyAction.Rest);
        Assert.Equal(career.Me.EffectiveRating(stat), career.Me.GameRating(stat));
    }

    [Fact]
    public void KindsHaveTheirOwnLengths()
    {
        var random = new SeededRandom(3);
        var ankle = Enumerable.Range(0, 50).Average(_ => Injuries.Days(InjuryKind.Ankle, random));
        var shoulder = Enumerable.Range(0, 50).Average(_ => Injuries.Days(InjuryKind.Shoulder, random));
        Assert.True(shoulder > ankle * 2);
        var kinds = Enumerable.Range(0, 200).Select(_ => Injuries.Roll(random)).Distinct().ToList();
        Assert.Equal(5, kinds.Count);
    }
}

public class PopularityTests
{
    [Fact]
    public void HitsWinFansAndTheWinterTakesSomeBack()
    {
        var random = new SeededRandom(4);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 75;
        while (!career.IsSeasonOver)
        {
            var d = career.PlayDay(DailyAction.Rest);
            if (d.Trade != null) career.DeclineTrade();
        }
        int before = career.Me.FanPoints;
        Assert.True(before > 0);
        var money = career.Me.Money;
        var summary = career.EndSeason();
        Assert.True(summary.Popularity > 0);
        Assert.Equal(summary.Popularity * 8, summary.Sponsor);
        Assert.True(career.Me.FanPoints < before + 2000);
        Assert.InRange(career.Me.Popularity, 0, 100);
    }

    [Fact]
    public void FansCanVoteAPlayerIntoTheAllStarGame()
    {
        var random = new SeededRandom(5);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 20; // never picked on numbers
        career.Me.FanPoints = 3000;
        AllStarResult allStar = null;
        while (allStar == null)
        {
            career.Me.FanPoints = 3000;
            var d = career.PlayDay(DailyAction.Rest);
            if (d.Trade != null) career.DeclineTrade();
            allStar = d.AllStar;
        }
        if (career.InFarm) return; // sent down: no all-star game for anyone in 2군
        Assert.True(allStar.Selected);
        Assert.True(allStar.FanVote);
    }
}

public class MilestoneTests
{
    [Fact]
    public void TheHundredthHitIsCelebrated()
    {
        var random = new SeededRandom(6);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 80;
        career.Me.History.Add(new SeasonRecord { Season = 1, Games = 36, Line = new BattingLine { Hits = 99, AtBats = 300, PlateAppearances = 330 }, Ratings = career.Me.Snapshot() });
        DayResult day = null;
        for (int i = 0; i < 30 && (day == null || day.Milestones.Count == 0); i++) day = career.PlayDay(DailyAction.Rest);
        Assert.Contains("통산 100안타", day.Milestones);
        Assert.Contains(career.SeasonNotes, n => n.Contains("통산 100안타"));
        Assert.Contains("100안타", CareerState.MilestoneCeremony(day.Milestones[0], career.Me.Name));
    }
}

public class DreamTeamTests
{
    static Club ClubWith(params (string Name, Position Position, int Rating)[] players)
    {
        var club = new Club();
        foreach (var (name, position, rating) in players)
        {
            var stats = position == Position.Pitcher ? CareerPlayer.PitcherStats : CareerPlayer.HitterStats;
            club.Roster.Add(new RetiredPlayer
            {
                Name = name, Position = position, Number = 7, CareerLine = new BattingLine(),
                PeakRatings = stats.ToDictionary(s => s, s => rating),
            });
        }
        return club;
    }

    [Fact]
    public void RetiredPlayersMakeTheTeamAndOrdinaryPlayersFillTheRest()
    {
        var club = ClubWith(("홍길동", Position.Shortstop, 90), ("김투수", Position.Pitcher, 85));
        club.Roster[0].TeamNumber = 1;
        var team = DreamTeam.Build(club, new SeededRandom(1));
        Assert.Equal(9, team.Lineup.Count);
        Assert.Equal("홍길동 #1", team.Lineup[0].Name);
        Assert.Equal(90, team.Lineup[0].Batting.Contact);
        Assert.Equal("김투수 #7", team.Rotation[0].Name);
        Assert.Equal(85, team.Rotation[0].Pitching.Velocity);
        Assert.All(team.Lineup.Skip(1), p => Assert.InRange(p.Batting.Contact, 1, 99));
    }

    [Fact]
    public void TheFirstWinAtALevelPaysLegacyPointsOnce()
    {
        var club = ClubWith(Enumerable.Range(0, 9).Select(i => ($"레전드{i}", Position.Outfield, 95))
            .Concat(Enumerable.Range(0, 5).Select(i => ($"에이스{i}", Position.Pitcher, 95))).ToArray());
        var random = new SeededRandom(2);
        ChallengeResult first = null;
        for (int i = 0; i < 20 && first == null; i++)
        {
            var r = DreamTeam.Play(club, ChallengeTier.Club, random);
            if (r.Won) first = r;
        }
        Assert.NotNull(first);
        Assert.Equal(DreamTeam.Reward(ChallengeTier.Club), first.LegacyPoints);
        Assert.Contains(ChallengeTier.Club, club.ChallengesWon);
        int points = club.LegacyPoints;
        ChallengeResult again = null;
        for (int i = 0; i < 20 && again == null; i++)
        {
            var r = DreamTeam.Play(club, ChallengeTier.Club, random);
            if (r.Won) again = r;
        }
        Assert.Equal(0, again.LegacyPoints);
        Assert.Equal(points, club.LegacyPoints);
    }

    [Fact]
    public void NeedsARetiredPlayer()
    {
        Assert.Throws<InvalidOperationException>(() => DreamTeam.Play(new Club(), ChallengeTier.Club, new SeededRandom(1)));
    }

    [Fact]
    public void SaveKeepsTheTeam()
    {
        var club = ClubWith(("홍길동", Position.Catcher, 70));
        club.Roster[0].TeamNumber = 99;
        club.Roster[0].MainClub = "서울 블루윙스";
        club.ChallengesWon.Add(ChallengeTier.AllStars);
        club.ChallengeWins = 3;
        var stream = new MemoryStream();
        SaveGame.Write(stream, club, null);
        stream.Position = 0;
        var (loaded, _) = SaveGame.Read(stream, new SeededRandom(1));
        var p = loaded.Roster[0];
        Assert.Equal(Position.Catcher, p.Position);
        Assert.Equal(7, p.Number);
        Assert.Equal(99, p.ShownNumber);
        Assert.Equal("서울 블루윙스", p.MainClub);
        Assert.Contains(ChallengeTier.AllStars, loaded.ChallengesWon);
        Assert.Equal(3, loaded.ChallengeWins);
    }
}
