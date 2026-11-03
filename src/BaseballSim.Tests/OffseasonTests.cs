using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class OffseasonTests
{
    static CareerState Career(int seed, bool pitcher = false)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트" + seed, random, 0, pitcher ? Position.Pitcher : Position.CenterField), league, league.Teams[0], random);
    }

    static List<string> StartScenes(CareerState c)
    {
        var ids = new List<string>();
        for (int k = 0; k < 30; k++)
        {
            var ev = c.SeasonStartEvent();
            if (ev == null) break;
            ids.Add(ev.Id);
            c.ResolveEvent(k % ev.Choices.Count);
        }
        return ids;
    }

    static void PlaySeason(CareerState c)
    {
        StartScenes(c);
        while (!c.IsSeasonOver)
        {
            var r = c.PlayDay(c.Me.Fatigue > 60 ? DailyAction.Rest : c.Me.IsPitcher ? DailyAction.TrainVelocity : DailyAction.TrainPower);
            while (r.AtBat != null) r = c.ChooseApproach(Approach.Normal);
            if (c.PendingEvent != null) c.ResolveEvent(0);
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void EveryWinterHasFansAPlanAndAHappeningBeforeOpeningDay(int seed, bool pitcher)
    {
        var c = Career(seed, pitcher);
        var winters = new List<List<string>>();
        for (int season = 0; season < 4; season++)
        {
            PlaySeason(c);
            var summary = c.EndSeason();
            Assert.NotEmpty(summary.FanComments);
            c.PendingOffers.Clear();
            c.PendingSalary = null;
            var ids = StartScenes(c);
            Assert.Contains(ids, id => id.StartsWith("fans_"));
            Assert.Contains("winter_plan", ids);
            Assert.Contains(ids, id => id.StartsWith("winter_") && id != "winter_plan");
            if (!c.InFarm && !c.InMilitary) Assert.Equal("opening", ids.Last());
            Assert.Null(c.SeasonStartEvent());
            winters.Add(ids);
        }
        // Not the same winter twice in a row.
        Assert.True(winters.Select(w => string.Join(",", w)).Distinct().Count() >= 3);
    }

    [Fact]
    public void ThePlanChangesFromYearToYearAndFitsTheRole()
    {
        var hitter = Career(5);
        var labels = new HashSet<string>();
        for (int season = 2; season <= 6; season++)
        {
            hitter.Season = season;
            hitter.Me.History.Add(new SeasonRecord { Season = season - 1, Games = 36, Line = new BattingLine() });
            var plan = Offseason.Next(hitter);
            hitter.Me.StoryMarks["winter:fans"] = hitter.Stamp;
            plan = Offseason.Next(hitter);
            Assert.NotNull(plan);
            Assert.InRange(plan.Choices.Count, 3, 4);
            Assert.DoesNotContain(plan.Choices, ch => ch.Label.Contains("구속") || ch.Label.Contains("변화구"));
            foreach (var ch in plan.Choices) labels.Add(ch.Label);
        }
        Assert.True(labels.Count >= 8);
    }

    [Fact]
    public void FansReactToHowTheSeasonWent()
    {
        var bad = new SeasonRecord { Games = 144, Line = new BattingLine { PlateAppearances = 400, AtBats = 380, Hits = 70, Walks = 15 } };
        var great = new SeasonRecord { Games = 144, Line = new BattingLine { PlateAppearances = 500, AtBats = 440, Hits = 150, Walks = 60, HomeRuns = 35, Doubles = 30 } };
        var hurt = new SeasonRecord { Games = 144, InjuryDays = 60, Line = new BattingLine() };
        Assert.Equal(FanMood.Bad, Offseason.Mood(bad));
        Assert.Equal(FanMood.Great, Offseason.Mood(great));
        Assert.Equal(FanMood.Hurt, Offseason.Mood(hurt));
        Assert.Equal(FanMood.Great, Offseason.Mood(new SeasonRecord { Games = 144, Line = new BattingLine(), Titles = { Postseason.ChampionTitle } }));
    }
}

public class MoraleReadTests
{
    [Fact]
    public void ConfidenceSharpensTheHunchAndASlumpDullsIt()
    {
        Assert.Equal(0.10, AtBatChoice.MoraleRead(7), 6);
        Assert.Equal(0.0, AtBatChoice.MoraleRead(6), 6);
        Assert.Equal(-0.10, AtBatChoice.MoraleRead(-7), 6);
        var me = BaseballSim.Career.Career.NewPlayer("t", new SeededRandom(1));
        me.Morale = 8;
        Assert.Equal(8, me.ForGame(new SeededRandom(1)).Morale);
    }
}
