using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum ChallengeTier { Club, AllStars, Legends }

    public class ChallengeResult
    {
        public ChallengeTier Tier;
        public GameResult Game;
        public Team Mine;
        public Team Opponent;
        public bool Won;
        /// <summary>Legacy points for the first win at this tier; 0 after that.</summary>
        public int LegacyPoints;
    }

    /// <summary>
    /// My own team: the players I raised and retired, at their peak, take on harder and harder sides.
    /// Places nobody has filled yet go to ordinary league-level players, so even a first retiree has a
    /// team to play with. The first win at each level pays legacy points.
    /// </summary>
    public static class DreamTeam
    {
        public const string Name = "내 팀";
        static readonly string[] TierNames = { "리그 구단", "리그 올스타", "레전드 올스타" };
        static readonly int[] TierStrength = { 4, 12, 20 };
        static readonly int[] TierReward = { 50, 120, 250 };

        public static string TierName(ChallengeTier tier) => TierNames[(int)tier];
        public static int Reward(ChallengeTier tier) => TierReward[(int)tier];

        /// <summary>My retired hitters (best nine), starters (best five), closer and setup man; the rest ordinary players.</summary>
        public static Team Build(Club club, IRandom random)
        {
            var team = new RosterGenerator(random).CreateTeam(Name);
            int i = 0;
            Player FromRetired(RetiredPlayer r)
            {
                int Peak(Stat s) => r.PeakRatings.TryGetValue(s, out var v) ? v : 40;
                string number = r.ShownNumber > 0 ? $" #{r.ShownNumber}" : "";
                var p = new Player($"club{i++}", r.Name + number);
                if (r.IsPitcher)
                {
                    p.Pitching = new PitchingRatings { Velocity = Peak(Stat.Velocity), Control = Peak(Stat.Control), Breaking = Peak(Stat.Breaking), Stamina = Peak(Stat.Stamina) };
                    return p;
                }
                p.Batting = new BattingRatings { Contact = Peak(Stat.Contact), Power = Peak(Stat.Power), Eye = Peak(Stat.Eye), Speed = Peak(Stat.Speed), Defense = Peak(Stat.Defense) };
                return p;
            }

            var hitters = club.Roster.Where(r => !r.IsPitcher).OrderByDescending(r => r.Overall).Take(team.Lineup.Count).ToList();
            for (int k = 0; k < hitters.Count; k++) team.Lineup[k] = FromRetired(hitters[k]);
            var starters = club.Roster.Where(r => r.IsPitcher && r.Role == PitcherRole.Starter).OrderByDescending(r => r.Overall).Take(team.Rotation.Count).ToList();
            for (int k = 0; k < starters.Count; k++) team.Rotation[k] = FromRetired(starters[k]);
            // The best retired closer and setup man take those bullpen jobs.
            var closer = club.Roster.Where(r => r.IsPitcher && r.Role == PitcherRole.Closer).OrderByDescending(r => r.Overall).FirstOrDefault();
            if (closer != null) team.Bullpen[team.Bullpen.Count - 1] = FromRetired(closer);
            var setup = club.Roster.Where(r => r.IsPitcher && r.Role == PitcherRole.Setup).OrderByDescending(r => r.Overall).FirstOrDefault();
            if (setup != null) team.Bullpen[team.Bullpen.Count - 2] = FromRetired(setup);
            return team;
        }

        public static Team Opponent(ChallengeTier tier, IRandom random)
        {
            var team = new RosterGenerator(random).CreateTeam(TierName(tier));
            int strength = TierStrength[(int)tier];
            int Shift(int rating) => Math.Max(15, Math.Min(95, rating + strength));
            var all = team.Lineup.Concat(team.Rotation).Concat(team.Bullpen).ToList();
            foreach (var p in all)
            {
                p.Id = "challenge-" + p.Id;
                var b = p.Batting;
                b.Contact = Shift(b.Contact); b.Power = Shift(b.Power); b.Eye = Shift(b.Eye); b.Speed = Shift(b.Speed); b.Defense = Shift(b.Defense);
                if (p.Pitching != null)
                {
                    var q = p.Pitching;
                    q.Velocity = Shift(q.Velocity); q.Control = Shift(q.Control); q.Breaking = Shift(q.Breaking); q.Stamina = Shift(q.Stamina);
                }
            }
            // The legends side bats the league's old stars at the top of its order.
            if (tier == ChallengeTier.Legends)
                for (int k = 0; k < Legends.All.Count && k < team.Lineup.Count; k++) team.Lineup[k].Name = Legends.All[k].Name;
            return team;
        }

        /// <summary>Plays one challenge game, my team at home; the first win at a level pays its legacy points.</summary>
        public static ChallengeResult Play(Club club, ChallengeTier tier, IRandom random)
        {
            if (club.Roster.Count == 0) throw new InvalidOperationException("Retire a player first.");
            var mine = Build(club, random);
            var opponent = Opponent(tier, random);
            var sim = new GameSimulator(random);
            GameResult game = null;
            for (int k = 0; k < 3 && (game == null || game.IsTie); k++)
                game = sim.Play(opponent, mine, opponent.Rotation[k % opponent.Rotation.Count], mine.Rotation[k % mine.Rotation.Count]);

            var result = new ChallengeResult { Tier = tier, Game = game, Mine = mine, Opponent = opponent, Won = game.Winner == mine };
            if (result.Won)
            {
                club.ChallengeWins++;
                if (club.ChallengesWon.Add(tier))
                {
                    result.LegacyPoints = Reward(tier);
                    club.LegacyPoints += result.LegacyPoints;
                }
            }
            return result;
        }
    }
}
