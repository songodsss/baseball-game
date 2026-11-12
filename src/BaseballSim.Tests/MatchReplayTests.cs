using BaseballSim;
using Xunit;

public class MatchReplayTests
{
    static GameResult PlayGame(int seed)
    {
        var random = new SeededRandom(seed);
        var generator = new RosterGenerator(random);
        var away = generator.CreateTeam("원정");
        var home = generator.CreateTeam("홈");
        return new GameSimulator(random).Play(away, home, away.Rotation[0], home.Rotation[0]);
    }

    public static IEnumerable<object[]> Seeds() => Enumerable.Range(1, 200).Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(Seeds))]
    public void FinalScoreboardMatchesTheGame(int seed)
    {
        var game = PlayGame(seed);
        var replay = new MatchReplay(game);
        var score = replay.ScoreAfter(replay.Steps.Count - 1);

        Assert.Equal(game.AwayScore, score.AwayRuns);
        Assert.Equal(game.HomeScore, score.HomeRuns);
        Assert.Equal(game.AwayScore, score.Away.Sum(r => r ?? 0));
        Assert.Equal(game.HomeScore, score.Home.Sum(r => r ?? 0));
        Assert.Equal(game.Innings, score.Innings);
        Assert.Equal(game.Batting.Where(b => game.Away.Lineup.Contains(b.Key)).Sum(b => b.Value.Hits), score.AwayHits);
        Assert.Equal(game.Batting.Where(b => game.Home.Lineup.Contains(b.Key)).Sum(b => b.Value.Hits), score.HomeHits);
        // Home doesn't bat in the last inning when already ahead.
        if (game.HomeScore > game.AwayScore && score.Home[score.Innings - 1] == null)
            Assert.True(game.Events.Last().IsTopHalf);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void RunnerMovesAccountForEveryRunAndOut(int seed)
    {
        var replay = new MatchReplay(PlayGame(seed));
        foreach (var step in replay.Steps)
        {
            var ev = step.Event;
            if (ev.Kind == EventKind.PitchingChange)
            {
                Assert.Empty(step.Moves);
                continue;
            }
            Assert.Equal(ev.RunnersScored.Count, step.Moves.Count(m => m.To == RunnerMove.Scored));
            Assert.Equal(ev.OutsAfter - ev.OutsBefore, step.Moves.Count(m => m.To == RunnerMove.Out));
            Assert.All(step.Moves, m => Assert.True(m.To == RunnerMove.Out || m.To >= m.From, $"{ev.Outcome}: {m.From}->{m.To}"));
            Assert.Equal(ev.Batter, step.Moves.Last().Runner);
            for (int b = 0; b < 3; b++)
                if (ev.BasesAfter[b] != null) Assert.Contains(step.Moves, m => m.Runner == ev.BasesAfter[b] && m.To == b);
        }
    }

    [Fact]
    public void AdvanceWalksThroughEveryStepOnce()
    {
        var replay = new MatchReplay(PlayGame(7));
        Assert.Null(replay.Current);
        int count = 0;
        while (replay.Advance() is ReplayStep step)
        {
            Assert.Equal(count++, step.Index);
            Assert.Same(step, replay.Current);
        }
        Assert.Equal(replay.Steps.Count, count);
        Assert.True(replay.IsFinished);
        Assert.Null(replay.Advance());
    }

    [Fact]
    public void FindsMyPlateAppearances()
    {
        var game = PlayGame(11);
        var mine = game.Home.Lineup[4];
        var replay = new MatchReplay(game, mine);

        var expected = replay.Steps.Where(s => s.Event.Kind == EventKind.PlateAppearance && s.Event.Batter == mine).Select(s => s.Index).ToList();
        Assert.NotEmpty(expected);
        Assert.Equal(expected, replay.Steps.Where(s => s.IsMine).Select(s => s.Index));
        Assert.Equal(expected[0], replay.NextMineIndex(0));
        Assert.Equal(expected[1], replay.NextMineIndex(expected[0] + 1));
        Assert.Equal(-1, replay.NextMineIndex(expected.Last() + 1));
    }

    [Fact]
    public void FirstStepOfEachHalfIsMarked()
    {
        var replay = new MatchReplay(PlayGame(3));
        Assert.True(replay.Steps[0].StartsHalf);
        Assert.Equal("1회초", replay.Steps[0].HalfLabel);
        int halves = replay.Steps.Count(s => s.StartsHalf);
        var game = replay.Game;
        bool homeSkippedLast = game.Events.Last().IsTopHalf;
        Assert.Equal(game.Innings * 2 - (homeSkippedLast ? 1 : 0), halves);
    }
}
