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
            var east = Squad(career, EastName, league.Teams.Where((t, i) => i % 2 == 0).ToList(), out var eastMine);
            var west = Squad(career, WestName, league.Teams.Where((t, i) => i % 2 == 1).ToList(), out var westMine);
            var game = sim.Play(west, east, west.Rotation[0], east.Rotation[0]);
            var mine = eastMine ?? westMine;

            var result = new AllStarResult { Game = game, East = east, West = west, Selected = mine != null };
            var winners = game.Winner == null ? game.Batting.Keys.ToList() : game.Winner.Lineup;
            var mvp = winners
                .Where(p => game.Batting.ContainsKey(p))
                .OrderByDescending(p => Score(game.Batting[p]))
                .First();
            result.Mvp = mvp == mine ? career.Me.Name : $"{mvp.Name} ({TeamOf(league, mvp)?.Name})";

            if (mine != null)
            {
                result.MyLine = game.Batting.TryGetValue(mine, out var line) ? line : new BattingLine();
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
        static Team Squad(Career career, string name, List<Team> teams, out Player mine)
        {
            var league = career.League;
            int minPa = career.Day * 2;
            double Ops(string id) => league.Batting.TryGetValue(id, out var s) && s.Line.PlateAppearances >= minPa ? Career.Ops(s.Line) : 0;

            var candidates = teams.SelectMany(t => t.Lineup).Select(p => (Player: p, Ops: Ops(p.Id))).ToList();
            mine = null;
            if (teams.Contains(career.MyTeam) && !career.InFarm && !career.Me.IsInjured)
            {
                mine = career.Me.ForGame(new SeededRandom(career.Season));
                candidates.Add((mine, Ops(career.Me.Id)));
            }
            var lineup = candidates.OrderByDescending(c => c.Ops).Take(9).Select(c => c.Player).ToList();
            if (!lineup.Contains(mine)) mine = null;

            int Stuff(Player p) => p.Pitching.Velocity + p.Pitching.Control + p.Pitching.Breaking;
            return new Team(name)
            {
                Lineup = lineup,
                Rotation = teams.SelectMany(t => t.Rotation).OrderByDescending(Stuff).Take(2).ToList(),
                Bullpen = teams.SelectMany(t => t.Bullpen).OrderByDescending(Stuff).Take(5).ToList(),
            };
        }
    }
}
