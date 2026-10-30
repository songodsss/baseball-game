using System.IO;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class SaveGameTests
{
    static CareerState NewCareer(IRandom random, Club club)
    {
        var league = new League(new RosterGenerator(random), 36);
        var me = CareerState.NewPlayer("테스트", random, club.PotentialBonus);
        return new CareerState(me, league, league.Teams[3], random);
    }

    /// <summary>A club with one retired player and a second career 20 days in, with gear bought.</summary>
    static (Club, CareerState) MidCareer()
    {
        var random = new SeededRandom(5);
        var club = new Club();
        var first = NewCareer(random, club);
        for (int season = 0; season < 2; season++)
        {
            while (!first.IsSeasonOver) first.PlayDay(DailyAction.TrainPower);
            first.EndSeason();
        }
        club.Retire(first);

        var career = NewCareer(random, club);
        career.Me.Money += 5000;
        Assert.True(career.Buy("bat2"));
        Assert.True(career.Buy("glove1"));
        for (int day = 0; day < 20; day++) career.PlayDay(day % 4 == 3 ? DailyAction.Rest : DailyAction.TrainContact);
        career.Me.Morale = 4;
        career.HitlessStreak = 2;
        career.Me.MetLegends.Add("min");
        return (club, career);
    }

    [Fact]
    public void RoundTripKeepsEverything()
    {
        var (club, career) = MidCareer();
        var bytes = SaveGame.ToBytes(club, career);
        var (loadedClub, loaded) = SaveGame.FromBytes(bytes, new SeededRandom(1));

        Assert.Equal(bytes, SaveGame.ToBytes(loadedClub, loaded));

        Assert.Equal(club.LegacyPoints, loadedClub.LegacyPoints);
        Assert.Single(loadedClub.Roster);
        Assert.Equal(club.Roster[0].Overall, loadedClub.Roster[0].Overall);
        Assert.Equal(club.Roster[0].CareerLine.Hits, loadedClub.Roster[0].CareerLine.Hits);

        Assert.Equal(career.Season, loaded.Season);
        Assert.Equal(20, loaded.Day);
        Assert.Equal(career.SeasonLine.Hits, loaded.SeasonLine.Hits);
        Assert.Equal(career.Me.Ratings, loaded.Me.Ratings);
        Assert.Equal(career.Me.Potential, loaded.Me.Potential);
        Assert.Equal(career.Me.Exp, loaded.Me.Exp);
        Assert.Equal(career.Me.Fatigue, loaded.Me.Fatigue);
        Assert.Equal(career.Me.Money, loaded.Me.Money);
        Assert.Equal(4, loaded.Me.Morale);
        Assert.Equal(2, loaded.HitlessStreak);
        Assert.Equal(new[] { "min" }, loaded.Me.MetLegends);
        Assert.Equal(career.Me.EquipmentBonus(Stat.Power), loaded.Me.EquipmentBonus(Stat.Power));
        Assert.Equal(new[] { "bat2", "glove1" }, loaded.Me.Owned.Select(e => e.Id));
        Assert.Equal(career.League.Teams.IndexOf(career.MyTeam), loaded.League.Teams.IndexOf(loaded.MyTeam));
        Assert.Equal(career.League.Ranked().Select(s => s.Team.Name), loaded.League.Ranked().Select(s => s.Team.Name));
        Assert.Equal(
            career.League.Teams.Select(t => career.League.Standings[t].Wins),
            loaded.League.Teams.Select(t => loaded.League.Standings[t].Wins));
    }

    [Fact]
    public void LoadedCareerPlaysOnToNextSeason()
    {
        var (club, career) = MidCareer();
        var (_, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(club, career), new SeededRandom(2));

        while (!loaded.IsSeasonOver) loaded.PlayDay(DailyAction.TrainEye);
        foreach (var team in loaded.League.Teams)
        {
            var s = loaded.League.Standings[team];
            Assert.Equal(36, s.Wins + s.Losses + s.Ties);
        }
        var summary = loaded.EndSeason();
        Assert.Equal(2, loaded.Season);
        Assert.Equal(1, summary.Record.Season);
    }

    [Fact]
    public void SavesAClubWithNoCareer()
    {
        var (club, _) = MidCareer();
        var (loadedClub, loaded) = SaveGame.FromBytes(SaveGame.ToBytes(club, null), new SeededRandom(3));
        Assert.Null(loaded);
        Assert.Equal(club.PotentialBonus, loadedClub.PotentialBonus);
    }

    [Fact]
    public void RejectsOtherFilesAndNewerVersions()
    {
        Assert.Throws<InvalidDataException>(() => SaveGame.FromBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, new SeededRandom(1)));

        var bytes = SaveGame.ToBytes(new Club(), null);
        bytes[4] = 99; // version, right after the 4-byte magic
        Assert.Throws<InvalidDataException>(() => SaveGame.FromBytes(bytes, new SeededRandom(1)));
    }
}
