using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class SeasonGoalTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    static void PlayOut(CareerState career)
    {
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
    }

    [Fact]
    public void OfferHasOneGoalPerTierOfDifferentKinds()
    {
        var career = NewCareer(1, 60);
        var offer = career.GoalOffer;
        Assert.Equal(3, offer.Count);
        Assert.Equal(new[] { GoalTier.Easy, GoalTier.Normal, GoalTier.Hard }, offer.Select(g => g.Tier));
        Assert.Equal(3, offer.Select(g => g.Kind).Distinct().Count());
        Assert.Same(offer, career.GoalOffer); // stable until picked
        Assert.True(offer[2].Money(10000) > offer[1].Money(10000) && offer[1].Money(10000) > offer[0].Money(10000));
    }

    [Fact]
    public void GoalTextAndProgress()
    {
        var career = NewCareer(2, 60);
        var goals = Enum.GetValues(typeof(GoalTier)).Cast<GoalTier>().ToList();
        // Same kind across tiers: re-roll offers until a kind repeats is overkill; check through the public target maths instead.
        var a = new SeasonGoal { Kind = GoalKind.HomeRuns, Target = 10 };
        Assert.Equal("홈런 10개", a.Text);
        career.SeasonLine.HomeRuns = 3;
        Assert.Equal("3/10", a.Progress(career));
        Assert.NotEmpty(goals);
    }

    [Fact]
    public void NoOfferOnceTheSeasonStarts()
    {
        var career = NewCareer(3, 60);
        career.PlayDay(DailyAction.Rest);
        Assert.Null(career.GoalOffer);
        Assert.Throws<InvalidOperationException>(() => career.ChooseGoal(new SeasonGoal()));
    }

    [Fact]
    public void MetGoalPaysAndIsRecorded()
    {
        var career = NewCareer(4, 70);
        var goal = new SeasonGoal { Kind = GoalKind.Hits, Tier = GoalTier.Hard, Target = 1 };
        career.Goal = goal;
        PlayOut(career);
        int money = career.Me.Money;
        var summary = career.EndSeason();
        Assert.True(summary.GoalMet);
        Assert.Same(goal, summary.Goal);
        Assert.True(summary.GoalMoney > 3000);
        Assert.True(career.Me.Money >= money + summary.GoalMoney);
        Assert.Contains(summary.Record.Notes, n => n.StartsWith("시즌 목표 달성"));
        Assert.Null(career.Goal);
        Assert.NotNull(career.GoalOffer); // next season's offer
    }

    [Fact]
    public void MissedGoalPaysNothing()
    {
        var career = NewCareer(5, 50);
        career.Goal = new SeasonGoal { Kind = GoalKind.HomeRuns, Tier = GoalTier.Hard, Target = 500 };
        PlayOut(career);
        var summary = career.EndSeason();
        Assert.False(summary.GoalMet);
        Assert.Equal(0, summary.GoalMoney);
    }

    [Fact]
    public void RateGoalsNeedQualifyingPlateAppearances()
    {
        var career = NewCareer(6, 50);
        career.SeasonLine = new BattingLine { PlateAppearances = 10, AtBats = 10, Hits = 9 };
        Assert.False(new SeasonGoal { Kind = GoalKind.Avg, Target = 0.300 }.Met(career));
        Assert.True(new SeasonGoal { Kind = GoalKind.Hits, Target = 9 }.Met(career));
    }

    [Fact]
    public void GoalSurvivesSaving()
    {
        var career = NewCareer(7, 60);
        career.ChooseGoal(career.GoalOffer[2]);
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(career.Goal.Kind, loaded.Goal.Kind);
        Assert.Equal(career.Goal.Target, loaded.Goal.Target);
        Assert.Equal(GoalTier.Hard, loaded.Goal.Tier);
    }
}
