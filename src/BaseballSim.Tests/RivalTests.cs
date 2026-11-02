using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class RivalTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    static SeasonSummary PlaySeason(CareerState career)
    {
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        return career.EndSeason();
    }

    [Fact]
    public void RivalJoinsAnotherClubOnOpeningDay()
    {
        var career = NewCareer(1);
        Assert.Null(career.Rival);
        career.PlayDay(DailyAction.Rest);
        Assert.NotNull(career.Rival);
        Assert.Equal(career.Me.Age, career.Rival.Age);
        Assert.NotSame(career.MyTeam, career.RivalTeam);
        Assert.Equal(9, career.RivalTeam.Lineup.Count);
        Assert.Single(career.League.Teams.SelectMany(t => t.Lineup), p => p.Id == RivalState.PlayerId);
    }

    [Fact]
    public void RivalPlaysGrowsAndIsCompared()
    {
        var career = NewCareer(2);
        var summary = PlaySeason(career);
        Assert.True(summary.RivalLine.PlateAppearances > 0);
        Assert.Same(summary.RivalLine, summary.Record.RivalLine);
        Assert.Equal(19, career.Rival.Age);

        int before = career.RivalPlayer.Batting.Contact;
        PlaySeason(career);
        Assert.True(career.RivalPlayer.Batting.Contact >= before); // still growing at 19
    }

    [Fact]
    public void HeadToHeadGamesShowTheRival()
    {
        var career = NewCareer(6);
        for (int i = 0; i < 36; i++)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
            if (day.MyGame != null && (day.MyGame.Home == career.RivalTeam || day.MyGame.Away == career.RivalTeam))
            {
                Assert.NotNull(day.RivalLine);
                return;
            }
        }
        Assert.Fail("Never played the rival's club.");
    }

    [Fact]
    public void RivalSurvivesSaving()
    {
        var career = NewCareer(4);
        PlaySeason(career);
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(career.Rival.Name, loaded.Rival.Name);
        Assert.Equal(career.Rival.Age, loaded.Rival.Age);
        Assert.Equal(career.Rival.Potential[Stat.Power], loaded.Rival.Potential[Stat.Power]);
        Assert.Equal(career.RivalTeam.Name, loaded.RivalTeam.Name);
        Assert.Equal(career.Me.History[0].RivalLine.Hits, loaded.Me.History[0].RivalLine.Hits);
    }
}
