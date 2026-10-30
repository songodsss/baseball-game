using System.Collections.Generic;

namespace BaseballSim
{
    public enum EventKind
    {
        PlateAppearance,
        PitchingChange,
    }

    /// <summary>
    /// One thing that happened in a game, in order. The 3D match scene replays these:
    /// the engine decides what happened, the scene only animates it.
    /// </summary>
    public class GameEvent
    {
        public EventKind Kind;
        public int Inning;
        public bool IsTopHalf;
        public Player Batter;
        public Player Pitcher;
        public Outcome Outcome;
        public bool DoublePlay;
        public bool SacrificeFly;
        public int OutsBefore;
        public int OutsAfter;
        /// <summary>Runners on first, second, third (null = empty).</summary>
        public Player[] BasesBefore;
        public Player[] BasesAfter;
        public List<Player> RunnersScored = new List<Player>();
        public int AwayScore;
        public int HomeScore;
        public int PitchesThrown;
        /// <summary>My approach in a big at bat, e.g. "직구 노림 적중!"; null otherwise.</summary>
        public string Note;
    }

    public class BattingLine
    {
        public int PlateAppearances, AtBats, Hits, Doubles, Triples, HomeRuns, Walks, Strikeouts, Runs, RunsBattedIn;
    }

    public class PitchingLine
    {
        public int OutsRecorded, Hits, Runs, Walks, Strikeouts, HomeRuns, Pitches;
        /// <summary>Relief credits, 0 or 1 a game: finished a win he entered with a lead of three or less,
        /// handed such a lead on intact, or let it slip.</summary>
        public int Saves, Holds, BlownSaves;
    }

    public class GameResult
    {
        public Team Away;
        public Team Home;
        public int AwayScore;
        public int HomeScore;
        public int Innings;
        public List<GameEvent> Events = new List<GameEvent>();
        public Dictionary<Player, BattingLine> Batting = new Dictionary<Player, BattingLine>();
        public Dictionary<Player, PitchingLine> Pitching = new Dictionary<Player, PitchingLine>();

        public bool IsTie => AwayScore == HomeScore;
        public Team Winner => IsTie ? null : HomeScore > AwayScore ? Home : Away;
    }
}
