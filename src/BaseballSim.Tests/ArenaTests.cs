using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;

public class ArenaTests
{
    static Club ClubWithRetirees(int count = 3, int level = 70)
    {
        var club = new Club();
        for (int i = 0; i < count; i++)
            club.Roster.Add(new RetiredPlayer
            {
                Name = "은퇴 " + i, Seasons = 15, CareerLine = new BattingLine(),
                PeakRatings = new Dictionary<Stat, int> { [Stat.Contact] = level, [Stat.Power] = level, [Stat.Eye] = level, [Stat.Speed] = level, [Stat.Defense] = level },
            });
        return club;
    }

    [Fact]
    public void OpponentsMatchMyWeightClass()
    {
        var club = ClubWithRetirees();
        var arena = club.Arena;
        arena.Refresh("2026-11-03", club);
        var found = arena.FindOpponents(club, new SeededRandom(4));
        Assert.Equal(3, found.Count);
        int mine = Arena.MyPower(club);
        foreach (var rival in found)
        {
            int power = Arena.Power(Arena.BuildOpponent(rival));
            Assert.InRange(power, mine - 6, mine + 6);
        }
    }

    [Fact]
    public void PlayingUsesEntriesAndMovesRating()
    {
        var club = ClubWithRetirees();
        var arena = club.Arena;
        var random = new SeededRandom(8);
        arena.Refresh("2026-11-03", club);
        for (int k = 0; k < Arena.FreeEntriesPerDay; k++)
        {
            arena.FindOpponents(club, random);
            int before = arena.Rating, coins = club.Coins;
            var result = arena.Play(club, 1, random);
            Assert.NotNull(result);
            Assert.Equal(before + result.RatingChange, arena.Rating);
            if (result.Won) { Assert.True(result.RatingChange > 0); Assert.Equal(coins + Arena.WinCoins, club.Coins); }
        }
        arena.FindOpponents(club, random);
        Assert.Null(arena.Play(club, 0, random)); // out of entries
        club.Coins = Arena.ExtraEntryCoins;
        Assert.True(arena.BuyEntry(club));
        Assert.Equal(1, arena.EntriesLeft);
        arena.Refresh("2026-11-04", club);
        Assert.Equal(Arena.FreeEntriesPerDay, arena.EntriesLeft);
    }

    [Fact]
    public void EloRewardsUpsets()
    {
        Assert.True(Arena.Change(1000, 1200, true) > Arena.Change(1000, 800, true));
        Assert.True(Arena.Change(1000, 800, false) < Arena.Change(1000, 1200, false));
        Assert.Equal(-Arena.Change(1000, 1000, false), Arena.Change(1000, 1000, true));
    }

    [Fact]
    public void SeasonEndPaysBestTierAndEasesRating()
    {
        var club = new Club();
        var arena = club.Arena;
        arena.Refresh("2026-11-03", club);
        arena.Rating = 1300;
        arena.SeasonBest = 1420;
        arena.Wins = 10;
        arena.Refresh("2026-11-16", club); // same two-week season (Nov 2-15)? Nov 16 starts the next
        Assert.Equal(Arena.TierCoinsOf(1420), club.Coins);
        Assert.Equal(1150, arena.Rating);
        Assert.Equal(0, arena.Wins);
        Assert.NotNull(arena.LastSeasonNote);
        Assert.Equal("다이아", Arena.TierOf(1420));
    }

    [Fact]
    public void ArenaSurvivesSaving()
    {
        var club = new Club();
        var arena = club.Arena;
        arena.Refresh("2026-11-03", club);
        arena.Rating = 1111; arena.Wins = 3; arena.Losses = 2; arena.EntriesUsed = 4; arena.ExtraEntriesBought = 1;
        var loaded = SaveGame.ClubFromBytes(SaveGame.ClubToBytes(club)).Arena;
        Assert.Equal((1111, 3, 2, 4, 1, arena.SeasonId, arena.Date), (loaded.Rating, loaded.Wins, loaded.Losses, loaded.EntriesUsed, loaded.ExtraEntriesBought, loaded.SeasonId, loaded.Date));
    }
}
