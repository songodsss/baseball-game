using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class AtBatChoiceTests
{
    static CareerState NewCareer(int seed, bool decide)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random) { DecideAtBats = decide };
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 70;
        return career;
    }

    static DayResult DayWithBigAtBat(CareerState career)
    {
        for (int i = 0; i < 20; i++)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.AtBat != null) return day;
            if (day.Trade != null) career.DeclineTrade();
        }
        throw new Exception("No big at bat in 20 games.");
    }

    [Fact]
    public void BigAtBatPausesTheGameUntilAnswered()
    {
        var career = NewCareer(1, decide: true);
        var day = DayWithBigAtBat(career);
        int dayIndex = career.Day;
        var s = day.AtBat;

        Assert.True(career.IsMidGame);
        Assert.Null(day.MyGame);
        Assert.Equal(3, s.Bases.Length);
        Assert.True(AtBatChoice.IsBig(s.Inning, s.Bases, s.MyRuns - s.TheirRuns));
        Assert.InRange(s.FastballShare, 0.35, 0.75);
        Assert.Throws<InvalidOperationException>(() => career.PlayDay(DailyAction.Rest));

        int answers = 0;
        while (day.AtBat != null)
        {
            day = career.ChooseApproach(day.AtBat.Hunch == Pitch.Fastball ? Approach.SitFastball : Approach.SitBreaking);
            answers++;
            Assert.Equal(answers, day.AtBatLog.Count);
        }
        Assert.False(career.IsMidGame);
        Assert.NotNull(day.MyGame);
        Assert.Equal(dayIndex + 1, career.Day);
        Assert.Equal(answers, day.MyGame.Events.Count(e => e.Note != null));
        Assert.Contains(day.MyGame.Events, e => e.Note != null && e.Batter == day.MyGamePlayer);
    }

    [Fact]
    public void ResumingReplaysTheSameGameUpToThePause()
    {
        var career = NewCareer(2, decide: true);
        var day = DayWithBigAtBat(career);
        var first = day.AtBat;
        day = career.ChooseApproach(Approach.Patient);
        // The answered at bat happened exactly where the game paused.
        var answered = day.MyGame?.Events.First(e => e.Note != null);
        if (answered != null)
        {
            Assert.Equal(first.Inning, answered.Inning);
            Assert.Equal(first.Outs, answered.OutsBefore);
            Assert.Same(first.Pitcher, answered.Pitcher);
            Assert.Equal("신중하게", answered.Note);
        }
    }

    [Fact]
    public void WithoutDecidingGamesNeverPause()
    {
        var career = NewCareer(3, decide: false);
        for (int i = 0; i < 20; i++)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
            Assert.Null(day.AtBat);
            Assert.False(career.IsMidGame);
        }
    }

    [Fact]
    public void RightReadPaysAndWrongReadHurts()
    {
        var batter = new Player("b", "b");
        var pitcher = new Player("p", "p") { Pitching = new PitchingRatings() };
        OutcomeOdds With(Approach a, Pitch coming)
        {
            var odds = PlateAppearanceModel.Compute(batter, pitcher, 50, 0);
            AtBatChoice.Apply(odds, a, coming);
            return odds;
        }
        var plain = With(Approach.Normal, Pitch.Fastball);
        var right = With(Approach.SitFastball, Pitch.Fastball);
        var wrong = With(Approach.SitFastball, Pitch.Breaking);
        var patient = With(Approach.Patient, Pitch.Breaking);

        Assert.True(right.HomeRun > plain.HomeRun && plain.HomeRun > wrong.HomeRun);
        Assert.True(right.Strikeout < plain.Strikeout && plain.Strikeout < wrong.Strikeout);
        Assert.True(patient.Walk > plain.Walk);

        // Sitting on a 65% pitch beats no read on average.
        double share = 0.65;
        Assert.True(share * right.HomeRun + (1 - share) * wrong.HomeRun > plain.HomeRun);
    }

    [Fact]
    public void BetterEyeGivesBetterHunches()
    {
        Assert.Equal(0.55, AtBatChoice.HunchAccuracy(20), 3);
        Assert.Equal(0.90, AtBatChoice.HunchAccuracy(100), 3);
        Assert.True(AtBatChoice.HunchAccuracy(70) > AtBatChoice.HunchAccuracy(40));

        var hardThrower = new Player("p", "p") { Pitching = new PitchingRatings { Velocity = 80, Breaking = 40 } };
        Assert.True(AtBatChoice.FastballShare(hardThrower) > 0.6);
    }

    [Fact]
    public void SettingAndReadsSurviveSaving()
    {
        var career = NewCareer(4, decide: true);
        career.Me.ReadsRight = 12;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.True(loaded.DecideAtBats);
        Assert.Equal(12, loaded.Me.ReadsRight);
    }
}
