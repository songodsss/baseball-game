using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class AllStarResult
    {
        public GameResult Game;
        public Team East, West;
        /// <summary>My player made a squad and played.</summary>
        public bool Selected;
        public BattingLine MyLine;
        /// <summary>My pitcher's outing, if he pitched.</summary>
        public PitcherLine MyPitching;
        /// <summary>Picked by the fans rather than on the numbers.</summary>
        public bool FanVote;
        /// <summary>Best batter on the winning side, "Name (Team)".</summary>
        public string Mvp;
        public bool MyMvp;
        public int Bonus;
    }

    /// <summary>
    /// Midseason exhibition: the league splits into 동군 and 서군, each fielding its nine best hitters
    /// by OPS and its best arms. Doesn't count in the standings or anyone's season line.
    /// </summary>
    public static class AllStar
    {
        public const string EastName = "동군 올스타";
        public const string WestName = "서군 올스타";
        const int SelectionBonus = 500;
        const int MvpBonus = 1500;

        public static AllStarResult Play(Career career, GameSimulator sim)
        {
            var league = career.League;
            var east = Squad(career, EastName, league.Teams.Where((t, i) => i % 2 == 0).ToList(), out var eastMine, out var eastFans);
            var west = Squad(career, WestName, league.Teams.Where((t, i) => i % 2 == 1).ToList(), out var westMine, out var westFans);
            var game = sim.Play(west, east, west.Rotation[0], east.Rotation[0]);
            var mine = eastMine ?? westMine;

            var result = new AllStarResult { Game = game, East = east, West = west, Selected = mine != null, FanVote = eastFans || westFans };
            var winners = game.Winner == null ? game.Batting.Keys.ToList() : game.Winner.Lineup;
            var mvp = winners
                .Where(p => game.Batting.ContainsKey(p))
                .OrderByDescending(p => Score(game.Batting[p]))
                .First();
            result.Mvp = mvp == mine ? career.Me.Name : $"{mvp.Name} ({TeamOf(league, mvp)?.Name})";

            if (mine != null)
            {
                result.MyLine = game.Batting.TryGetValue(mine, out var line) ? line : new BattingLine();
                if (game.Pitching.TryGetValue(mine, out var arm))
                {
                    result.MyPitching = new PitcherLine();
                    result.MyPitching.Add(arm, career.Me.Role == PitcherRole.Starter, false, false);
                }
                result.MyMvp = mvp == mine;
                result.Bonus = SelectionBonus + (result.MyMvp ? MvpBonus : 0);
                career.Me.Money += result.Bonus;
                career.Me.AllStarSelections++;
                if (result.MyMvp) career.Me.AllStarMvps++;
            }
            return result;
        }

        static int Score(BattingLine l) => l.Hits + l.Doubles + 2 * l.Triples + 3 * l.HomeRuns + l.RunsBattedIn + l.Walks;

        static Team TeamOf(League league, Player p) => league.Teams.FirstOrDefault(t => t.Lineup.Contains(p));

        /// <summary>
        /// Nine hitters ranked by OPS so far (my player too, unless hurt or in 2군), the two best starters
        /// and five best relievers. <paramref name="mine"/> is my player's game copy if picked.
        /// </summary>
        static Team Squad(Career career, string name, List<Team> teams, out Player mine, out bool fanVote)
        {
            fanVote = false;
            Player me = teams.Contains(career.MyTeam) && !career.InFarm && !career.Me.IsInjured
                ? career.Me.ForGame(new SeededRandom(career.Season)) : null;
            var team = career.PickSquad(name, teams, me, 9, 2, 5, out mine);
            // A favourite the numbers left out goes in on the fans' vote, in place of the last pick.
            if (me != null && mine == null && career.Me.Popularity >= Career.FanVotePopularity)
            {
                if (me.IsPitcher && career.Me.Role != PitcherRole.Starter) team.Bullpen[career.Me.Role == PitcherRole.Closer ? team.Bullpen.Count - 1 : team.Bullpen.Count - 2] = me;
                else if (me.IsPitcher) team.Rotation[team.Rotation.Count - 1] = me;
                else team.Lineup[team.Lineup.Count - 1] = me;
                mine = me;
                fanVote = true;
            }
            // My pitcher, once picked, gets the start.
            if (mine != null && team.Rotation.Contains(mine))
            {
                team.Rotation.Remove(mine);
                team.Rotation.Insert(0, mine);
            }
            return team;
        }
    }
}
