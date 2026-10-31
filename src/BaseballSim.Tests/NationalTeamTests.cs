using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class NationalTeamTests
{
    static CareerState NewCareer(int seed, int rating)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = rating;
        return career;
    }

    static TournamentResult PlaySeasonFindingTournament(CareerState career)
    {
        TournamentResult found = null;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
            if (day.Tournament != null) found = day.Tournament;
        }
        return found;
    }

    [Fact]
    public void TournamentEveryFourYearsFrom2026()
    {
        Assert.True(NationalTeam.IsTournamentYear(2026));
        Assert.False(NationalTeam.IsTournamentYear(2027));
        Assert.True(NationalTeam.IsTournamentYear(2030));

        var career = NewCareer(1, 60);
        Assert.NotNull(PlaySeasonFindingTournament(career));
        career.EndSeason();
        Assert.Null(PlaySeasonFindingTournament(career));
    }

    [Fact]
    public void StarMakesTheTeamAndPlaysTheBracket()
    {
        var career = NewCareer(5, 95);
        var t = PlaySeasonFindingTournament(career);
        Assert.True(t.Selected, $"farm={career.InFarm} inj={career.Me.IsInjured} day={career.Day}");
        Assert.Contains(t.Korea.Lineup, p => p.Id == career.Me.Id);
        Assert.InRange(t.Games.Count, 2, 4);
        Assert.All(t.Games.Take(2), g => Assert.Equal("조별리그", g.Stage));
        if (t.Medal == NationalTeam.Gold)
        {
            Assert.Equal("결승", t.Games.Last().Stage);
            Assert.True(t.Games.Last().Won);
            Assert.Equal(1, career.Me.GoldMedals);
            Assert.Contains("gold", career.Me.Achievements);
        }
        Assert.True(t.MyLine.PlateAppearances > 0);
        Assert.Contains(career.Me.History.Count == 0 ? career.SeasonNotes : career.Me.History.Last().Notes, n => n.Contains(NationalTeam.Name));
        // Tournament games stay out of league stats.
        Assert.Equal(career.SeasonLine.PlateAppearances, career.League.Batting[career.Me.Id].Line.PlateAppearances);
    }

    [Fact]
    public void WeakPlayerWatchesFromHome()
    {
        var career = NewCareer(3, 25);
        var t = PlaySeasonFindingTournament(career);
        Assert.False(t.Selected);
        Assert.Equal(0, t.Bonus);
        Assert.Equal(0, t.MyLine.PlateAppearances);
    }

    [Fact]
    public void MedalsSurviveSaving()
    {
        var career = NewCareer(4, 50);
        career.Me.GoldMedals = 2;
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(2, loaded.Me.GoldMedals);
    }
}
