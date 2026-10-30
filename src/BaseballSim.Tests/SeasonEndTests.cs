using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class SeasonEndTests
{
    static CareerState PlayedSeason(int seed, int games = 36)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), games);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        while (!career.IsSeasonOver) career.PlayDay(career.Me.Fatigue > 60 ? DailyAction.Rest : DailyAction.TrainContact);
        return career;
    }

    [Fact]
    public void LeagueBattingAddsUpToEveryGame()
    {
        var career = PlayedSeason(1);
        var league = career.League;
        // Ten teams, each game counted once: total runs scored by batters equals total wins' worth of games played.
        int games = league.Standings.Values.Sum(s => s.Wins + s.Losses + s.Ties) / 2;
        Assert.Equal(36 * 5, games);
        Assert.Equal(career.SeasonLine.Hits, league.Batting["me"].Line.Hits);
        Assert.Same(career.MyTeam, league.Batting["me"].Team);
        Assert.All(league.Batting.Values, b => Assert.True(b.Team.Lineup.Any(p => p.Id == b.Id) || b.Id == "me"));
    }

    [Fact]
    public void TitlesGoToTheLeaders()
    {
        var career = PlayedSeason(2);
        var league = career.League;
        var awards = Awards.For(league, "me", new[] { "me" });

        var hr = awards.Single(a => a.Title == "홈런왕");
        Assert.Equal(league.Batting.Values.Max(b => b.Line.HomeRuns).ToString(), hr.Value);
        var avg = awards.Single(a => a.Title == "타격왕");
        var bestAvg = league.Batting.Values.Where(b => b.Line.PlateAppearances >= league.QualifyingPa).Max(b => Stats.Avg(b.Line));
        Assert.Equal(Stats.Rate(bestAvg), avg.Value);
        Assert.Single(awards, a => a.Title == Awards.Mvp);
        Assert.All(awards.Where(a => a.Mine), a => Assert.Equal("테스트", a.Winner));
    }

    [Fact]
    public void RookieOfTheYearOnlyInTheFirstSeason()
    {
        var career = PlayedSeason(3);
        Assert.DoesNotContain(Awards.For(career.League, "me", new string[0]), a => a.Title == Awards.Rookie);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void PostseasonCrownsATopFourTeam(int seed)
    {
        var career = PlayedSeason(seed);
        var topFour = career.League.Ranked().Take(4).Select(s => s.Team).ToList();
        var summary = career.EndSeason();
        var post = summary.Postseason;

        Assert.Equal(3, post.Series.Count);
        Assert.Contains(post.Champion, topFour);
        Assert.Equal(post.Champion, post.Series[2].Winner);
        Assert.Equal(topFour.Contains(career.MyTeam), post.MyTeamIn);
        Assert.Equal(post.MyTeamWon, summary.Record.Titles.Contains(Postseason.ChampionTitle));
        foreach (var s in post.Series)
            Assert.Equal(s.Name == "챔피언 결정전" ? 3 : 2, Math.Max(s.HighWins, s.LowWins));
        Assert.True(post.MyTeamIn ? post.Bonus > 0 : post.Bonus == 0);
    }

    [Fact]
    public void MyTitlesAreRecordedOnTheSeason()
    {
        for (int seed = 1; seed <= 30; seed++)
        {
            var career = PlayedSeason(seed);
            var summary = career.EndSeason();
            Assert.Equal(summary.Awards.Where(a => a.Mine).Select(a => a.Title),
                summary.Record.Titles.Where(t => t != Postseason.ChampionTitle));
            if (summary.Record.Titles.Count > 0) return;
        }
        Assert.Fail("No title in 30 seasons; check the awards.");
    }

    [Fact]
    public void AchievementsUnlockOnce()
    {
        var career = PlayedSeason(7);
        Assert.Contains("first_hit", career.Me.Achievements);
        Assert.Empty(Achievements.Check(career)); // nothing new on a second look
        Assert.Equal(career.Me.Achievements.Count, career.Me.Achievements.Distinct().Count());
        career.Me.Salary = 10000;
        Assert.Contains(Achievements.Check(career), a => a.Id == "salary_1");
    }

    [Fact]
    public void RetiredPlayersKeepAchievementsAndTitles()
    {
        var career = PlayedSeason(8);
        career.EndSeason();
        var club = new Club();
        var retired = club.Retire(career);
        Assert.Equal(career.Me.Achievements, retired.Achievements);
        Assert.Contains("first_hit", club.Collection);

        var (loaded, _) = SaveGame.FromBytes(SaveGame.ToBytes(club, null), new SeededRandom(1));
        Assert.Equal(retired.Achievements, loaded.Roster[0].Achievements);
        Assert.Equal(retired.Titles, loaded.Roster[0].Titles);
    }

    [Fact]
    public void SaveKeepsLeagueBattingMidSeason()
    {
        var random = new SeededRandom(9);
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
        for (int i = 0; i < 10; i++) career.PlayDay(DailyAction.TrainPower);

        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(new Club(), career), new SeededRandom(1));
        Assert.Equal(league.Batting.Count, loaded.League.Batting.Count);
        Assert.Equal(league.Batting["me"].Line.Hits, loaded.League.Batting["me"].Line.Hits);
        Assert.Equal(career.Me.Achievements, loaded.Me.Achievements);
        Assert.Equal(league.Teams.IndexOf(league.Batting["p1"].Team), loaded.League.Teams.IndexOf(loaded.League.Batting["p1"].Team));
    }
}

public class GoldenGloveTests
{
    [Fact]
    public void NineGoldenGlovesGoToTheBestAllRoundPlayers()
    {
        var random = new SeededRandom(9);
        var league = new League(new RosterGenerator(random), 36);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        // Spread the league over positions as league lineups are: C, 1B, 2B, 3B, SS, OF x3, DH.
        (Position, int) Fielding(string id)
        {
            foreach (var team in league.Teams)
            {
                int i = team.Lineup.FindIndex(p => p.Id == id);
                if (i >= 0) return (Positions.OfLineupIndex(i), 50);
            }
            return (Position.DesignatedHitter, 50);
        }
        var awards = Awards.For(league, "me", new string[0], Fielding);
        var gloves = awards.Where(a => a.Title == Awards.GoldenGlove).ToList();
        // Nine fielders and a pitcher.
        Assert.Equal(10, gloves.Count);
        Assert.Equal(10, gloves.Select(g => g.Winner + g.Team).Distinct().Count());
        Assert.Single(gloves, g => g.Position == Position.Pitcher);
        Assert.Equal(3, gloves.Count(g => g.Position == Position.Outfield));
        Assert.Single(gloves, g => g.Position == Position.Catcher);

        // A great glove can tip it.
        var bestDefense = Awards.For(league, "me", new string[0], id => (Position.Shortstop, id == "p1" ? 99 : 50));
        Assert.Equal(2, bestDefense.Count(a => a.Title == Awards.GoldenGlove)); // the shortstop and the pitcher
        Assert.DoesNotContain(Awards.For(league, "me", new string[0]), a => a.Title == Awards.GoldenGlove);
    }

    [Fact]
    public void RivalCanWinRookieOfTheYear()
    {
        var random = new SeededRandom(10);
        var league = new League(new RosterGenerator(random), 36);
        var career = new BaseballSim.Career.Career(BaseballSim.Career.Career.NewPlayer("테스트", random), league, league.Teams[0], random);
        foreach (var stat in BaseballSim.Career.CareerPlayer.AllStats) career.Me.Ratings[stat] = career.Me.Potential[stat] = 20;
        while (!career.IsSeasonOver)
        {
            var day = career.PlayDay(DailyAction.Rest);
            if (day.Trade != null) career.DeclineTrade();
        }
        var rookie = Awards.For(league, "me", new[] { "me", RivalState.PlayerId }).FirstOrDefault(a => a.Title == Awards.Rookie);
        if (rookie != null)
        {
            Assert.False(rookie.Mine);
            Assert.Equal(career.Rival.Name, rookie.Winner);
        }
    }
}
