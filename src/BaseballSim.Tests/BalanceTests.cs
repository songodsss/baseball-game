using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class BalanceTests
{
    static CareerState NewCareer(int seed, Position position = Position.CenterField, PitcherRole role = PitcherRole.Starter)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random, 0, position, 0, role), league, league.Teams[0], random);
    }

    static SeasonRecord Season(int games = 36, double ops = 0.800, int injuryDays = 0, int farmDays = 0, bool military = false)
    {
        // 100 plate appearances of singles and walks tuned to the wanted OPS.
        int hits = (int)(ops / 2 * 100);
        return new SeasonRecord
        {
            Games = games,
            Line = new BattingLine { PlateAppearances = 100, AtBats = 100, Hits = hits },
            InjuryDays = injuryDays,
            FarmDays = farmDays,
            Military = military,
        };
    }

    [Fact]
    public void NoRandomCeilingAndChosenSpecialties()
    {
        for (int s = 0; s < 50; s++)
        {
            var me = CareerState.NewPlayer("x", new SeededRandom(s), 0, Position.CenterField, 0, PitcherRole.Starter, new[] { Stat.Eye, Stat.Speed });
            Assert.All(me.MyStats, st => Assert.InRange(me.Ratings[st], CareerState.StartRating, CareerState.StartRating + CareerState.StartSpread - 1));
            Assert.All(me.MyStats, st => Assert.Equal(CareerPlayer.MaxRating, me.Potential[st]));
            Assert.Equal(new[] { Stat.Eye, Stat.Speed }, me.Specialties);
        }
        // Missing or wrong picks fall back to the first ratings of the position.
        var pitcher = CareerState.NewPlayer("p", new SeededRandom(1), 0, Position.Pitcher, 0, PitcherRole.Starter, new[] { Stat.Power });
        Assert.Equal(new[] { Stat.Velocity, Stat.Control }, pitcher.Specialties);
    }

    [Fact]
    public void SpecialtiesGrowFaster()
    {
        var me = CareerState.NewPlayer("x", new SeededRandom(2), 0, Position.CenterField, 0, PitcherRole.Starter, new[] { Stat.Power, Stat.Speed });
        foreach (var st in me.MyStats) me.Ratings[st] = 50;
        me.GainExp(Stat.Power, 20000);
        me.GainExp(Stat.Contact, 20000);
        Assert.True(me.Ratings[Stat.Power] > me.Ratings[Stat.Contact]);
    }

    [Fact]
    public void OldSavesLoseTheirCeiling()
    {
        var career = NewCareer(6);
        foreach (var st in career.Me.MyStats) career.Me.Potential[st] = 55;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.All(loaded.Me.MyStats, st => Assert.Equal(CareerPlayer.MaxRating, loaded.Me.Potential[st]));
        Assert.Equal(career.Me.Specialties, loaded.Me.Specialties);
    }

    [Fact]
    public void ComebackYearFollowsABadSeasonOnly()
    {
        var career = NewCareer(1);
        Assert.False(career.ComebackYear); // rookie year

        career.Me.History.Add(Season());
        Assert.False(career.ComebackYear);

        career.Me.History.Add(Season(injuryDays: 1));
        Assert.False(career.ComebackYear); // a small knock doesn't count

        career.Me.History.Add(Season(injuryDays: 3));
        Assert.True(career.ComebackYear);

        career.Me.History.Add(Season(ops: 0.600));
        Assert.True(career.ComebackYear);

        career.Me.History.Add(Season(farmDays: 18));
        Assert.True(career.ComebackYear);

        career.Me.History.Add(Season(ops: 0.600, military: true));
        Assert.False(career.ComebackYear);
    }

    [Fact]
    public void InjuryDaysAreCountedAndSaved()
    {
        var career = NewCareer(2);
        career.Me.InjuryDaysLeft = 3;
        for (int i = 0; i < 4; i++)
        {
            var day = career.PlayDay(DailyAction.Rest);
            while (career.IsMidGame) career.ChooseApproach(Approach.Normal);
            if (day.Trade != null) career.DeclineTrade();
        }
        Assert.Equal(3, career.InjuryDays);

        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(3, loaded.InjuryDays);
    }

    [Fact]
    public void ConsistentSeasonsOutscoreOneHotYear()
    {
        var steady = new CareerPlayer();
        for (int i = 0; i < 10; i++) steady.History.Add(Season(ops: 0.880));
        var streaky = new CareerPlayer();
        for (int i = 0; i < 9; i++) streaky.History.Add(Season(ops: 0.700));
        streaky.History.Add(Season(ops: 1.800));
        Assert.True(Club.HallOfFameScore(steady) > Club.HallOfFameScore(streaky));
    }

    [Fact]
    public void TitlesAndRingsDoNotMoveTheHallScore()
    {
        var a = new CareerPlayer();
        var b = new CareerPlayer();
        for (int i = 0; i < 5; i++)
        {
            a.History.Add(Season());
            var s = Season();
            s.Titles.Add(Postseason.ChampionTitle);
            s.Titles.Add("타격왕");
            b.History.Add(s);
        }
        Assert.Equal(Club.HallOfFameScore(a), Club.HallOfFameScore(b));
    }

    [Fact]
    public void PitchersPayMoreExpPerPoint()
    {
        Assert.True(CareerPlayer.ExpToNext(50, pitcher: true) > CareerPlayer.ExpToNext(50));
        var me = NewCareer(3, Position.Pitcher).Me;
        Assert.Equal(CareerPlayer.ExpToNext(me.Ratings[Stat.Velocity], true), me.ExpToNext(Stat.Velocity));
    }

    [Fact]
    public void VeteransKeepTheirEyeAndOnlyTheBodySlips()
    {
        var career = NewCareer(4);
        var me = career.Me;
        me.Age = 35;
        foreach (var stat in me.MyStats) me.Ratings[stat] = 60;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            while (career.IsMidGame) career.ChooseApproach(Approach.Normal);
            if (day.Trade != null) career.DeclineTrade();
            if (day.Event != null) career.ResolveEvent(0);
        }
        var summary = career.EndSeason();
        Assert.Equal(36, me.Age);
        Assert.DoesNotContain(Stat.Contact, summary.Decline.Keys);
        Assert.DoesNotContain(Stat.Eye, summary.Decline.Keys);
        Assert.Equal(1, summary.Veteran[Stat.Contact]);
        Assert.True(me.Ratings[Stat.Eye] >= 61);
        Assert.All(summary.Decline.Keys, s => Assert.True(CareerState.IsBodyStat(s)));
    }

    [Fact]
    public void LongerSeasonsPayMoreCoins()
    {
        var club = new Club();
        Assert.Equal(Club.SeasonCoins, club.PaySeason(36));
        Assert.Equal(2 * Club.SeasonCoins, club.PaySeason(72));
        Assert.Equal(4 * Club.SeasonCoins, club.PaySeason(144));
        Assert.Equal(7 * Club.SeasonCoins, club.Coins);
    }

    [Fact]
    public void FullSeasonAchievement()
    {
        var career = NewCareer(7);
        career.Me.History.Add(Season(games: 36));
        Assert.DoesNotContain(Achievements.Check(career), a => a.Id == "full_season");
        career.Me.History.Add(Season(games: 144));
        Assert.Contains(Achievements.Check(career), a => a.Id == "full_season");
    }
}
