using System.Collections.Generic;

namespace BaseballSim
{
    public class Team
    {
        public string Name;
        /// <summary>Nine batters in batting order.</summary>
        public List<Player> Lineup = new List<Player>();
        public List<Player> Rotation = new List<Player>();
        public List<Player> Bullpen = new List<Player>();

        public Team(string name)
        {
            Name = name;
        }
    }
}
