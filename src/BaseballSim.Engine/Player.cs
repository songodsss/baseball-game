using System.Collections.Generic;

namespace BaseballSim
{
    /// <summary>Batting ratings, each 1-100 where 50 is league average.</summary>
    public class BattingRatings
    {
        public int Contact = 50; // 컨택: fewer strikeouts, more balls in play fall for hits
        public int Power = 50;   // 파워: home runs and extra-base hits
        public int Eye = 50;     // 선구안: walks
        public int Speed = 50;   // 주루: extra bases, infield hits, beating double plays
        public int Defense = 50; // 수비: turns balls in play into outs
    }

    /// <summary>Pitching ratings, each 1-100 where 50 is league average.</summary>
    public class PitchingRatings
    {
        public int Velocity = 50; // 구속: strikeouts
        public int Control = 50;  // 제구: fewer walks
        public int Breaking = 50; // 변화구: weak contact, fewer home runs
        public int Stamina = 50;  // 체력: how many pitches before tiring
    }

    public class Player
    {
        public string Id;
        public string Name;
        public BattingRatings Batting = new BattingRatings();
        /// <summary>Null for position players.</summary>
        public PitchingRatings Pitching;
        /// <summary>Day-of-game form, -10..+10, added to every rating while playing.</summary>
        public int Condition;
        /// <summary>Traits that change big at bats (see AtBatChoice); empty for league players.</summary>
        public HashSet<string> Traits = new HashSet<string>();

        public bool IsPitcher => Pitching != null;

        public Player(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
