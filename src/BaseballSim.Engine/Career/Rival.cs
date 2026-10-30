using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// A rookie the same age as my player, drafted by another club the same year. He grows season by
    /// season like my player does, plays in the league for real (and can win titles), and the season
    /// card compares the two.
    /// </summary>
    public class RivalState
    {
        public const string PlayerId = "rival";
        public string Name;
        public int Age;
        public Dictionary<Stat, int> Potential = new Dictionary<Stat, int>();
    }

    public partial class Career
    {
        public RivalState Rival;

        /// <summary>The rival's game player, wherever he is in the league; null if there's no rival.</summary>
        public Player RivalPlayer => Rival == null ? null : League.Teams.SelectMany(t => t.Lineup).FirstOrDefault(p => p.Id == RivalState.PlayerId);
        public Team RivalTeam => Rival == null ? null : League.Teams.FirstOrDefault(t => t.Lineup.Any(p => p.Id == RivalState.PlayerId));
        public BattingLine RivalLine => League.Batting.TryGetValue(RivalState.PlayerId, out var s) ? s.Line : new BattingLine();

        /// <summary>Signs the rival to a random other club, in place of one of its regulars. Called before a season's first game.</summary>
        void CreateRival()
        {
            var clubs = League.Teams.Where(t => t != MyTeam).ToList();
            var club = clubs[(int)(_random.NextDouble() * clubs.Count)];
            var player = new Player(RivalState.PlayerId, new RosterGenerator(_random).RandomName());
            Rival = new RivalState { Name = player.Name, Age = Me.Age };
            foreach (var stat in CareerPlayer.AllStats)
            {
                int start = 35 + (int)(_random.NextDouble() * 14);
                Set(player, stat, start);
                Rival.Potential[stat] = Math.Max(start + 10, Math.Min(95, (int)Math.Round(Normal(_random, 70, 6))));
            }
            club.Lineup[(int)(_random.NextDouble() * club.Lineup.Count)] = player;
        }

        /// <summary>Between seasons: the rival closes part of the gap to his potential while young, and declines from 32 like anyone.</summary>
        void AgeRival()
        {
            var p = RivalPlayer;
            if (p == null) return;
            double rate = Rival.Age <= 22 ? 0.22 : Rival.Age <= 27 ? 0.18 : Rival.Age <= 31 ? 0.08 : 0;
            foreach (var stat in CareerPlayer.AllStats)
            {
                int rating = Get(p, stat);
                rating += (int)Math.Round((Rival.Potential[stat] - rating) * rate);
                if (Rival.Age >= 32) rating -= (int)(_random.NextDouble() * (Rival.Age - 30)) + (stat == Stat.Speed ? 1 : 0);
                Set(p, stat, Math.Max(20, Math.Min(99, rating)));
            }
            Rival.Age++;
        }

        static int Get(Player p, Stat stat)
        {
            switch (stat)
            {
                case Stat.Contact: return p.Batting.Contact;
                case Stat.Power: return p.Batting.Power;
                case Stat.Eye: return p.Batting.Eye;
                case Stat.Speed: return p.Batting.Speed;
                default: return p.Batting.Defense;
            }
        }

        static void Set(Player p, Stat stat, int value)
        {
            switch (stat)
            {
                case Stat.Contact: p.Batting.Contact = value; break;
                case Stat.Power: p.Batting.Power = value; break;
                case Stat.Eye: p.Batting.Eye = value; break;
                case Stat.Speed: p.Batting.Speed = value; break;
                default: p.Batting.Defense = value; break;
            }
        }
    }
}
