using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class GachaTests
{
    static CareerState NewCareer(int seed)
    {
        var random = new SeededRandom(seed);
        var league = new League(new RosterGenerator(random), 36);
        return new CareerState(CareerState.NewPlayer("테스트", random), league, league.Teams[0], random);
    }

    [Fact]
    public void OddsAddUpAndEveryStatHasACardAndGear()
    {
        Assert.Equal(100, Gacha.Odds.Sum(), 6);
        Assert.Equal(36, Gacha.Cards.Count);
        Assert.Equal(36, Gacha.Gear.Count);
        Assert.Equal(36, Gacha.Cards.Select(c => c.Id).Distinct().Count());
        Assert.Equal(4, Gacha.OddsTable(GachaBox.Cards).Count());
    }

    [Fact]
    public void DrawsCostCoinsOrTicketsAndNeverGoNegative()
    {
        var club = new Club { Coins = Gacha.SingleCoins - 1 };
        Assert.Null(Gacha.Draw(club, GachaBox.Cards, 1, new SeededRandom(1)));
        Assert.Equal(Gacha.SingleCoins - 1, club.Coins);

        club.Coins = Gacha.TenCoins;
        var ten = Gacha.Draw(club, GachaBox.Cards, 10, new SeededRandom(1));
        Assert.Equal(10, ten.Count);
        Assert.Equal(ten.Sum(x => x.Refund), club.Coins);

        club.DrawTickets = 1;
        Assert.Single(Gacha.Draw(club, GachaBox.Gear, 1, new SeededRandom(2), useTickets: true));
        Assert.Equal(0, club.DrawTickets);
        Assert.Null(Gacha.Draw(club, GachaBox.Gear, 1, new SeededRandom(2), useTickets: true));
        Assert.Equal(11, club.Draws);
    }

    [Fact]
    public void EveryTenDrawHasAnSrAndPityGivesAnSsr()
    {
        var club = new Club { Coins = 1_000_000 };
        var random = new SeededRandom(7);
        int sinceSsr = 0, worst = 0;
        for (int k = 0; k < 60; k++)
        {
            var ten = Gacha.Draw(club, GachaBox.Cards, 10, random);
            Assert.Contains(ten, x => x.Rarity >= Rarity.SR);
            foreach (var x in ten)
            {
                sinceSsr = x.Rarity == Rarity.SSR ? 0 : sinceSsr + 1;
                worst = System.Math.Max(worst, sinceSsr);
            }
        }
        Assert.True(worst < Gacha.Pity);
    }

    [Fact]
    public void ThePitySsrIsAPickOfMyChoice()
    {
        var club = new Club { Coins = 1_000_000 };
        club.SetPityCount(GachaBox.Gear, Gacha.Pity - 1);
        var r = Gacha.Draw(club, GachaBox.Gear, 1, new SeededRandom(1));
        Assert.True(r[0].Pick);
        Assert.Equal(1, club.SsrPicks[(int)GachaBox.Gear]);
        Assert.Equal(0, club.PityCount(GachaBox.Gear));
        Assert.Null(club.UsePick(GachaBox.Cards, Stat.Power)); // picks stay in their own box
        var got = club.UsePick(GachaBox.Gear, Stat.Power);
        Assert.Equal(Rarity.SSR, got.Rarity);
        Assert.Equal(5, club.TeamGearBonus(Stat.Power));
        Assert.Equal(0, club.SsrPicks[(int)GachaBox.Gear]);
        club.SsrPicks[0] = 2;
        Assert.Equal(2, SaveGame.FromBytes(SaveGame.ToBytes(club, null), new SeededRandom(1)).Item1.SsrPicks[0]);
    }

    [Fact]
    public void RatesRoughlyMatchThePublishedOdds()
    {
        var club = new Club { Coins = int.MaxValue / 2 };
        var random = new SeededRandom(3);
        var counts = new int[4];
        for (int k = 0; k < 20000; k++) counts[(int)Gacha.Draw(club, GachaBox.Gear, 1, random)[0].Rarity]++;
        Assert.InRange(counts[(int)Rarity.N] / 200.0, 55, 63);
        Assert.InRange(counts[(int)Rarity.R] / 200.0, 26, 33);
        Assert.InRange(counts[(int)Rarity.SSR] / 200.0, 1.2, 3.5); // a bit over 1.5% with the pity
    }

    [Fact]
    public void CopiesLevelUpThenTurnIntoCoins()
    {
        var club = new Club();
        string id = Gacha.Cards[0].Id;
        for (int k = 1; k <= Gacha.MaxLevel; k++) Assert.Equal(k, club.AddCopy(GachaBox.Cards, id, Rarity.N).Level);
        var extra = club.AddCopy(GachaBox.Cards, id, Rarity.N);
        Assert.True(extra.Refund > 0);
        Assert.Equal(extra.Refund, club.Coins);
        Assert.Equal(Gacha.MaxLevel, club.LevelOf(GachaBox.Cards, id));
    }

    [Fact]
    public void CardsSpeedUpTrainingOfTheirStat()
    {
        var plain = NewCareer(5);
        var carded = NewCareer(5);
        var club = new Club();
        var card = Gacha.Cards.First(c => c.Stat == Stat.Eye && c.Rarity == Rarity.SSR);
        club.AddCopy(GachaBox.Cards, card.Id, card.Rarity);
        Assert.False(club.EquipCard(carded, 0, Gacha.Cards[1].Id)); // not owned
        Assert.True(club.EquipCard(carded, 0, card.Id));
        Assert.Equal(1.15, carded.CardMultiplier(Stat.Eye), 6);
        Assert.Equal(1.0, carded.CardMultiplier(Stat.Power), 6);

        int Progress(CareerState c) => c.Me.Ratings[Stat.Eye] * 1000 + c.Me.Exp.GetValueOrDefault(Stat.Eye);
        for (int d = 0; d < 15; d++) { plain.PlayDay(DailyAction.TrainEye); carded.PlayDay(DailyAction.TrainEye); }
        Assert.True(Progress(carded) > Progress(plain));

        // Level ups reach the worn card.
        club.AddCopy(GachaBox.Cards, card.Id, card.Rarity);
        club.RefreshCards(carded);
        Assert.Equal(1.16, carded.CardMultiplier(Stat.Eye), 6);
    }

    [Fact]
    public void GearLevelsNeedMoreCopiesEachTimeAndGoToTen()
    {
        Assert.Equal(4, Gacha.GearBonus(Rarity.N, 99));
        Assert.Equal(10, Gacha.GearBonus(Rarity.SSR, 99));
        Assert.Equal(new[] { 1, 2, 4, 7, 11, 16 }, Enumerable.Range(1, 6).Select(Gacha.GearCopiesFor));
        var club = new Club();
        var ssr = Gacha.Gear.First(g => g.Rarity == Rarity.SSR);
        var bonuses = new List<int>();
        for (int k = 0; k < 16; k++)
        {
            club.AddCopy(GachaBox.Gear, ssr.Id, Rarity.SSR);
            bonuses.Add(club.TeamGearBonus(ssr.Stat));
        }
        Assert.Equal(new[] { 5, 6, 6, 7, 7, 7, 8, 8, 8, 8, 9, 9, 9, 9, 9, 10 }, bonuses);
        Assert.True(club.AddCopy(GachaBox.Gear, ssr.Id, Rarity.SSR).Refund > 0);
        Assert.Equal(16, club.CopiesOf(ssr.Id));
    }

    [Fact]
    public void TeamGearLiftsMyTeamAndPicksTheBestPiece()
    {
        var club = new Club();
        club.Roster.Add(new RetiredPlayer
        {
            Name = "은퇴", Seasons = 15, CareerLine = new BattingLine(),
            PeakRatings = new Dictionary<Stat, int> { [Stat.Contact] = 70, [Stat.Power] = 70, [Stat.Eye] = 70, [Stat.Speed] = 70, [Stat.Defense] = 70 },
        });
        club.AddCopy(GachaBox.Gear, Gacha.Gear.First(g => g.Stat == Stat.Power && g.Rarity == Rarity.N).Id, Rarity.N);
        club.AddCopy(GachaBox.Gear, Gacha.Gear.First(g => g.Stat == Stat.Power && g.Rarity == Rarity.SSR).Id, Rarity.SSR);
        Assert.Equal(5, club.TeamGearBonus(Stat.Power));
        Assert.Equal(0, club.TeamGearBonus(Stat.Eye));
        var team = DreamTeam.Build(club, new SeededRandom(1));
        Assert.Equal(75, team.Lineup[0].Batting.Power);
    }

    [Fact]
    public void DrawsAndCardsSurviveASave()
    {
        var club = new Club { Coins = 5000, DrawTickets = 3 };
        Gacha.Draw(club, GachaBox.Cards, 10, new SeededRandom(1));
        Gacha.Draw(club, GachaBox.Gear, 10, new SeededRandom(2));
        var career = NewCareer(2);
        club.EquipCard(career, 1, club.CardLevels.Keys.First());
        var bytes = SaveGame.ToBytes(club, career);
        var (c2, career2) = SaveGame.FromBytes(bytes, new SeededRandom(1));
        Assert.Equal(bytes, SaveGame.ToBytes(c2, career2));
        Assert.Equal(club.CardLevels, c2.CardLevels);
        Assert.Equal(club.GearCopies, c2.GearCopies);
        Assert.Equal(club.Pity, c2.Pity);
        Assert.Equal(3, c2.DrawTickets);
        Assert.Equal(career.Cards, career2.Cards);
        Assert.Equal(career.CardBoost, career2.CardBoost);
    }
}
