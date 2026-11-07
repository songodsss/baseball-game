using BaseballSim.Career;
using Xunit;

public class KrwTests
{
    [Theory]
    [InlineData(300, "300만원")]
    [InlineData(5000, "5,000만원")]
    [InlineData(10000, "1억원")]
    [InlineData(12345, "1.2억원")]
    [InlineData(155000, "15.5억원")]
    [InlineData(1234567, "123억원")]
    [InlineData(-300, "-300만원")]
    public void MoneyIsShownShort(long manwon, string shown) => Assert.Equal(shown, Krw.Format(manwon));

    [Fact]
    public void GainsCarryAPlus() => Assert.Equal("+1.5억원", Krw.Signed(15000));
}

public class PopularityPerkTests
{
    [Fact]
    public void PopularityHasTiersAndShowsWhatItGives()
    {
        Assert.Equal("무명", BaseballSim.Career.Career.PopularityTier(5));
        Assert.Equal("스타", BaseballSim.Career.Career.PopularityTier(65));
        Assert.Equal("국민 스타", BaseballSim.Career.Career.PopularityTier(100));
        Assert.Equal(1.25, BaseballSim.Career.Career.PopularityPay(100), 3);
        var perks = BaseballSim.Career.Career.PopularityPerks(55);
        Assert.Contains(perks, p => p.On && p.Text.Contains("홈경기"));
        Assert.Contains(perks, p => !p.On && p.Text.Contains("팬 투표"));
    }
}

public class TrainingCritTests
{
    [Fact]
    public void SomeTrainingDaysClick()
    {
        var random = new BaseballSim.SeededRandom(4);
        var league = new BaseballSim.Career.League(new BaseballSim.RosterGenerator(random), 144);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
        int great = 0, days = 0;
        while (days < 120 && !career.IsSeasonOver)
        {
            var action = career.Me.Fatigue >= 60 ? DailyAction.Rest : career.TrainingChoices[0];
            var r = career.PlayDay(action);
            if (r.Event != null) career.ResolveEvent(0);
            if (r.AtBat != null) continue;
            if (action != DailyAction.Rest) days++;
            if (r.TrainingCrit > 0) great++;
        }
        Assert.InRange(great, 3, 30);
    }
}

public class GradeAndHighlightTests
{
    [Fact]
    public void GradesGoEveryTenAndANewOneLiftsTraining()
    {
        Assert.Equal("E", BaseballSim.Career.Highlights.Grade(35));
        Assert.Equal("C", BaseballSim.Career.Highlights.Grade(55));
        Assert.Equal("S", BaseballSim.Career.Highlights.Grade(84));
        Assert.Equal("SSS", BaseballSim.Career.Highlights.Grade(140));

        var random = new BaseballSim.SeededRandom(9);
        var league = new BaseballSim.Career.League(new BaseballSim.RosterGenerator(random), 36);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
        var stat = System.Linq.Enumerable.First(career.Me.MyStats);
        career.Me.Ratings[stat] = 51;
        career.Me.PeakRatings[stat] = 51;
        var day = career.PlayDay(DailyAction.Rest);
        while (day.AtBat != null) day = career.ChooseApproach(BaseballSim.Approach.Normal);
        Assert.Contains(day.GradeUps, g => g.Stat == stat && g.Grade == "C");
        var next = career.PlayDay(DailyAction.Rest);
        while (next.AtBat != null) next = career.ChooseApproach(BaseballSim.Approach.Normal);
        Assert.DoesNotContain(next.GradeUps, g => g.Stat == stat);
    }
}

public class OppositionAndStatusTests
{
    static BaseballSim.Career.Career NewCareer(int seed)
    {
        var random = new BaseballSim.SeededRandom(seed);
        var league = new BaseballSim.Career.League(new BaseballSim.RosterGenerator(random), 36);
        return new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void TheLeagueGetsALittleStrongerEachSeasonAndRookiesGetAHeadStart()
    {
        var c = NewCareer(1);
        Assert.Equal(0, c.OppositionLevel);
        Assert.True(c.RookieBlessed);
        Assert.Contains(c.ActiveStatuses(), a => a.Def.Id == BaseballSim.Career.Statuses.Rookie);
        c.Season = 3;
        Assert.False(c.RookieBlessed);
        Assert.Equal(2, c.OppositionLevel);
        c.Season = 30;
        Assert.Equal(BaseballSim.Career.Career.MaxOpposition, c.OppositionLevel);
    }

    [Fact]
    public void AStrongArmAndAGoodCatcherCutSteals()
    {
        var runner = new BaseballSim.Player("r", "주자"); runner.Batting.Speed = 80;
        var soft = new BaseballSim.Player("p", "투수") { Pitching = new BaseballSim.PitchingRatings { Velocity = 30 } };
        var hard = new BaseballSim.Player("p2", "투수2") { Pitching = new BaseballSim.PitchingRatings { Velocity = 90 } };
        var weak = new BaseballSim.Player("c", "포수"); weak.Batting.Defense = 30;
        var strong = new BaseballSim.Player("c2", "포수2"); strong.Batting.Defense = 90;
        double easy = BaseballSim.AtBatChoice.StealOdds(runner, soft, weak);
        double hardest = BaseballSim.AtBatChoice.StealOdds(runner, hard, strong);
        Assert.True(easy - hardest > 0.2, $"{easy} vs {hardest}");
        Assert.True(BaseballSim.AtBatChoice.StealOdds(runner, hard, strong, 10) < hardest);
    }

    [Fact]
    public void ABigAtBatShowsWhatEachChoiceIsWorth()
    {
        var random = new BaseballSim.SeededRandom(5);
        var gen = new BaseballSim.RosterGenerator(random);
        var t = gen.CreateTeam("A");
        var batter = t.Lineup[0];
        var odds = BaseballSim.PlateAppearanceModel.Compute(batter, t.Rotation[0], 50, 0);
        var s = new BaseballSim.AtBatSituation();
        BaseballSim.AtBatChoice.Preview(s, odds, batter);
        Assert.True(s.HitIfRight > s.HitNormal && s.HitNormal > s.HitIfWrong);
        Assert.True(s.HomeRunIfRight > s.HomeRunNormal);
        Assert.True(s.WalkPatient > odds.Walk);
    }
}
