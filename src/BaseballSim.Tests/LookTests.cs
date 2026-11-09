using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using Xunit;
using CareerState = BaseballSim.Career.Career;

public class LookTests
{
    [Fact]
    public void LookSurvivesASave()
    {
        var random = new SeededRandom(3);
        var club = new Club();
        var look = new PlayerLook { Build = BodyBuild.Big, Skin = 3, Hair = 5, HairColor = 4, Eyes = 2, Brows = 1 };
        var league = new League(new RosterGenerator(random), 36);
        var me = CareerState.NewPlayer("외모", random, 0, look: look);
        var career = new CareerState(me, league, league.Teams[0], random);

        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Equal(look, loaded.Me.Look);
        Assert.NotSame(look, me.Look); // registration's copy, not the picker's
    }

    [Fact]
    public void NoLookStaysNull()
    {
        var random = new SeededRandom(4);
        var club = new Club();
        var league = new League(new RosterGenerator(random), 36);
        var career = new CareerState(CareerState.NewPlayer("옛날", random), league, league.Teams[0], random);
        var loaded = SaveGame.CareerFromBytes(SaveGame.CareerToBytes(career, club), club, new SeededRandom(1));
        Assert.Null(loaded.Me.Look);
    }

    [Fact]
    public void RandomLooksCoverEveryPartAndStayInRange()
    {
        var random = new SeededRandom(9);
        var looks = Enumerable.Range(0, 2000).Select(_ => Looks.Random(random)).ToList();
        Assert.Equal(Looks.Builds.Length, looks.Select(l => l.Build).Distinct().Count());
        Assert.Equal(Looks.Hairs.Length, looks.Select(l => l.Hair).Distinct().Count());
        Assert.Equal(Looks.HairColors.Length, looks.Select(l => l.HairColor).Distinct().Count());
        Assert.All(looks, l => Assert.Equal(l.Key, Looks.Clamp(l.Clone()).Key));
        Assert.True(looks.Select(l => l.Key).Distinct().Count() > 1500);
    }

    [Fact]
    public void ClampFixesOutOfRangeParts()
    {
        var look = Looks.Clamp(new PlayerLook { Build = (BodyBuild)9, Skin = 99, Hair = -1, Eyes = 40 });
        Assert.Equal(BodyBuild.Normal, look.Build);
        Assert.Equal(Looks.Skins.Length - 1, look.Skin);
        Assert.Equal(0, look.Hair);
        Assert.Equal(Looks.Eyes.Length - 1, look.Eyes);
        Assert.Contains("보통", Looks.Describe(look));
    }
}
