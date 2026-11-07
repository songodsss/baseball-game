using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;

public class MyTeamLeagueTests
{
    static Club ClubWithRetiree()
    {
        var club = new Club();
        club.Roster.Add(new RetiredPlayer
        {
            Name = "은퇴 선수", Seasons = 15, CareerLine = new BattingLine(),
            PeakRatings = new Dictionary<Stat, int> { [Stat.Contact] = 80, [Stat.Power] = 80, [Stat.Eye] = 75, [Stat.Speed] = 70, [Stat.Defense] = 70 },
        });
        return club;
    }

    [Fact]
    public void EveryPairMeetsTwiceAndEveryClubPlaysEachRound()
    {
        var meetings = new Dictionary<(int, int), int>();
        for (int round = 0; round < MyTeamLeague.Rounds; round++)
        {
            var games = MyTeamLeague.Pairings(round);
            Assert.Equal(MyTeamLeague.Teams, games.SelectMany(g => new[] { g.Home, g.Away }).Distinct().Count());
            foreach (var (h, a) in games)
            {
                var key = (System.Math.Min(h, a), System.Math.Max(h, a));
                meetings[key] = meetings.TryGetValue(key, out var n) ? n + 1 : 1;
            }
        }
        Assert.Equal(MyTeamLeague.Teams * (MyTeamLeague.Teams - 1) / 2, meetings.Count);
        Assert.All(meetings.Values, n => Assert.Equal(2, n));
    }

    [Fact]
    public void ASeasonPlaysTenRoundsPaysCoinsAndMovesDivision()
    {
        var club = ClubWithRetiree();
        var league = club.TeamLeague;
        var random = new SeededRandom(11);
        Assert.Null(league.PlayRound(club, "2026-11-02", random)); // no season yet
        league.StartSeason(random);
        LeagueGameResult last = null;
        for (int k = 0; k < MyTeamLeague.Rounds; k++)
        {
            last = league.PlayRound(club, "2026-11-02", random);
            Assert.NotNull(last);
        }
        Assert.True(league.SeasonOver);
        Assert.Null(league.PlayRound(club, "2026-11-02", random));
        var end = last.SeasonEnd;
        Assert.NotNull(end);
        Assert.Equal(club.Coins, end.Coins);
        Assert.True(end.Coins > 0);
        int games = league.Standings.Sum(s => s.Wins + s.Losses + s.Ties);
        Assert.Equal(MyTeamLeague.Rounds * MyTeamLeague.Teams, games);
        if (end.Rank <= 2) Assert.Equal(1, league.Division);
        else Assert.Equal(0, league.Division);
        Assert.Equal(end.Champion ? 1 : 0, league.Trophies[0]);
    }

    [Fact]
    public void GamesPerDayAreCapped()
    {
        var club = ClubWithRetiree();
        var league = club.TeamLeague;
        var random = new SeededRandom(3);
        league.StartSeason(random);
        league.Round = 0;
        league.Date = "2026-11-02";
        league.GamesToday = MyTeamLeague.GamesPerDay;
        Assert.Null(league.PlayRound(club, "2026-11-02", random));
        Assert.NotNull(league.PlayRound(club, "2026-11-03", random));
        Assert.Equal(1, league.GamesToday);
    }

    [Fact]
    public void NoRetireesNoGames()
    {
        var club = new Club();
        club.TeamLeague.StartSeason(new SeededRandom(1));
        Assert.Null(club.TeamLeague.PlayRound(club, "2026-11-02", new SeededRandom(1)));
    }

    [Fact]
    public void AiClubsAreTheSameWhenRebuilt()
    {
        var league = new MyTeamLeague();
        league.StartSeason(new SeededRandom(5));
        var a = league.AiTeam(2);
        var b = league.AiTeam(2);
        Assert.Equal(a.Lineup.Select(p => p.Batting.Contact), b.Lineup.Select(p => p.Batting.Contact));
        league.Division = 3;
        var strong = league.AiTeam(2);
        Assert.True(strong.Lineup.Average(p => p.Batting.Contact) > a.Lineup.Average(p => p.Batting.Contact));
    }

    [Fact]
    public void LeagueSurvivesSaving()
    {
        var club = ClubWithRetiree();
        var league = club.TeamLeague;
        var random = new SeededRandom(9);
        league.StartSeason(random);
        league.PlayRound(club, "2026-11-02", random);
        league.Trophies[1] = 2;
        var loaded = SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club)).TeamLeague;
        Assert.Equal(league.Season, loaded.Season);
        Assert.Equal(league.Round, loaded.Round);
        Assert.Equal(league.Seed, loaded.Seed);
        Assert.Equal(league.Standings.Select(s => (s.Team, s.Wins, s.Losses)), loaded.Standings.Select(s => (s.Team, s.Wins, s.Losses)));
        Assert.Equal(2, loaded.Trophies[1]);
        Assert.Equal(1, loaded.GamesToday);
        Assert.Equal(league.AiTeam(3).Lineup[0].Batting.Power, loaded.AiTeam(3).Lineup[0].Batting.Power);
    }
}
