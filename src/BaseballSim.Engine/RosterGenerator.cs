using System;

namespace BaseballSim
{
    /// <summary>Builds random fictional teams for testing and for filling the league around the player.</summary>
    public class RosterGenerator
    {
        static readonly string[] FamilyNames = { "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "한", "오", "서", "신", "권", "황" };
        static readonly string[] GivenNames =
        {
            "민준", "서준", "도윤", "예준", "시우", "하준", "주원", "지호", "지후", "준우",
            "현우", "도현", "건우", "우진", "선우", "서진", "민재", "현준", "연우", "유준",
            "정우", "승우", "승현", "시윤", "준혁", "은우", "지환", "승민", "지우", "유찬",
        };

        readonly IRandom _random;
        int _nextId;

        public RosterGenerator(IRandom random)
        {
            _random = random;
        }

        public Team CreateTeam(string name)
        {
            var team = new Team(name);
            for (int i = 0; i < 9; i++) team.Lineup.Add(CreateBatter());
            for (int i = 0; i < 5; i++) team.Rotation.Add(CreatePitcher());
            for (int i = 0; i < 6; i++) team.Bullpen.Add(CreatePitcher());
            return team;
        }

        public Player CreateBatter()
        {
            var p = new Player(NextId(), RandomName());
            p.Batting.Contact = Rating();
            p.Batting.Power = Rating();
            p.Batting.Eye = Rating();
            p.Batting.Speed = Rating();
            p.Batting.Defense = Rating();
            return p;
        }

        public Player CreatePitcher()
        {
            var p = new Player(NextId(), RandomName());
            p.Pitching = new PitchingRatings
            {
                Velocity = Rating(),
                Control = Rating(),
                Breaking = Rating(),
                Stamina = Rating(),
            };
            return p;
        }

        string NextId() => $"p{++_nextId}";

        string RandomName() =>
            FamilyNames[(int)(_random.NextDouble() * FamilyNames.Length)]
            + GivenNames[(int)(_random.NextDouble() * GivenNames.Length)];

        /// <summary>Roughly normal around 50 with a spread of 12, clamped to 20-90.</summary>
        int Rating()
        {
            double u1 = 1 - _random.NextDouble();
            double u2 = _random.NextDouble();
            double normal = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            return (int)Math.Round(Math.Max(20, Math.Min(90, 50 + 12 * normal)));
        }
    }
}
