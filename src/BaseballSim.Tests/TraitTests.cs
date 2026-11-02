using System;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class TraitTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    [Fact]
    public void SlotsOpenAtRatingMilestones()
    {
        var me = NewCareer(1, 40).Me; // total 200
        Assert.Equal(0, Traits.SlotsEarned(me));
        Assert.Equal(210, Traits.NextMilestone(me));
        foreach (var stat in CareerPlayer.AllStats) me.Ratings[stat] = 60; // total 300
        Assert.Equal(3, Traits.SlotsEarned(me));
        foreach (var stat in CareerPlayer.AllStats) me.Ratings[stat] = 80;
        Assert.Equal(Traits.Milestones.Length, Traits.SlotsEarned(me));
        Assert.Null(Traits.NextMilestone(me));
    }

    [Fact]
    public void OffersFollowTheBuild()
    {
        var career = NewCareer(2, 40);
        career.Me.Ratings[Stat.Power] = 70;
        for (int i = 0; i < 30; i++)
        {
            var offer = Traits.Offer(career.Me, new SeededRandom(i));
            Assert.Equal(3, offer.Count);
            Assert.DoesNotContain(offer, t => t.Id == Traits.Contact || t.Id == Traits.Eye || t.Id == Traits.Speed);
            Assert.All(offer, t => Assert.True(t.Eligible(career.Me)));
        }
    }

    [Fact]
    public void ATraitIsOfferedWhenASlotOpensAndWaitsToBePicked()
    {
        var career = NewCareer(3, 45); // total 225: one slot
        var day = career.PlayDay(DailyAction.Rest);
        Assert.NotNull(day.TraitOffer);
        var again = career.PlayDay(DailyAction.Rest);
        Assert.Same(day.TraitOffer, again.TraitOffer); // same offer until picked

        Assert.Throws<InvalidOperationException>(() => career.ChooseTrait("nope"));
        var pick = day.TraitOffer[0];
        career.ChooseTrait(pick.Id);
        Assert.Equal(new[] { pick.Id }, career.Me.Traits);
        Assert.Null(career.PendingTraitOffer);
        Assert.Null(career.PlayDay(DailyAction.Rest).TraitOffer); // no second slot yet
    }

    [Fact]
    public void BuildTraitsBoostGameRatingsPastThePotential()
    {
        var me = NewCareer(4, 60).Me;
        me.Traits.Add(Traits.Slugger);
        Assert.Equal(60 + Traits.Boost, me.EffectiveRating(Stat.Power));
        var p = me.ForGame(new SeededRandom(1));
        Assert.Equal(60 + Traits.Boost, p.Batting.Power);
        Assert.Contains(Traits.Slugger, p.Traits);
    }

    [Fact]
    public void ReaderSharpensHunchesAndGamblerSoftensMisses()
    {
        var batter = new Player("b", "b");
        var pitcher = new Player("p", "p") { Pitching = new PitchingRatings() };
        double plainAccuracy = AtBatChoice.Read(batter, pitcher, new SeededRandom(1)).HunchAccuracy;
        batter.Traits.Add(AtBatChoice.Reader);
        Assert.Equal(plainAccuracy + 0.15, AtBatChoice.Read(batter, pitcher, new SeededRandom(1)).HunchAccuracy, 3);

        OutcomeOdds Miss(Player b)
        {
            var odds = PlateAppearanceModel.Compute(b, pitcher, 50, 0);
            AtBatChoice.Apply(odds, Approach.SitFastball, Pitch.Breaking, b);
            return odds;
        }
        var gambler = new Player("g", "g");
        gambler.Traits.Add(AtBatChoice.Gambler);
        Assert.True(Miss(gambler).Strikeout < Miss(new Player("n", "n")).Strikeout);
    }

    [Fact]
    public void TrainingNeverHitsAWall()
    {
        var career = NewCareer(5, 50);
        career.Me.GainExp(Stat.Power, 100000);
        Assert.True(career.Me.Ratings[Stat.Power] > 70);
        career.Me.GainExp(Stat.Power, 10_000_000);
        Assert.True(career.Me.Ratings[Stat.Power] > 99, career.Me.Ratings[Stat.Power].ToString()); // no ceiling at 99 any more
    }

    [Fact]
    public void TraitsAndBreakthroughsSurviveSaving()
    {
        var career = NewCareer(6, 50);
        career.Me.Traits.Add(Traits.Iron);
        career.Me.Traits.Add(AtBatChoice.Clutch);
        career.Me.BreakthroughExp[Stat.Eye] = 321;
        career.Me.Breakthroughs[Stat.Power] = 2;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(new[] { Traits.Iron, AtBatChoice.Clutch }, loaded.Me.Traits);
        Assert.Equal(321, loaded.Me.BreakthroughProgress(Stat.Eye));
        Assert.Equal(2, loaded.Me.BreakthroughCount(Stat.Power));
    }
}
