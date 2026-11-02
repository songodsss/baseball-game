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
    public void StartingPotentialsStayClose()
    {
        var all = Enumerable.Range(0, 300)
            .SelectMany(s => { var me = CareerState.NewPlayer("x", new SeededRandom(s)); return me.MyStats.Select(st => me.Potential[st]); })
            .ToList();
        double mean = all.Average();
        double sd = System.Math.Sqrt(all.Average(p => (p - mean) * (p - mean)));
        Assert.InRange(mean, 66, 70);
        Assert.InRange(sd, 4.5, 7.5);
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
        foreach (var stat in me.MyStats) { me.Ratings[stat] = 60; me.Potential[stat] = 70; }
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
        Assert.Equal(71, me.Potential[Stat.Eye]);
        Assert.All(summary.Decline.Keys, s => Assert.True(CareerState.IsBodyStat(s)));
    }
}
