using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public class TournamentGame
    {
        public string Stage;
        public GameResult Game;
        public bool Won;
    }

    public class TournamentResult
    {
        public int Year;
        public bool Selected;
        public Team Korea;
        public List<TournamentGame> Games = new List<TournamentGame>();
        /// <summary>"금메달", "은메달", "동메달" or null.</summary>
        public string Medal;
        public BattingLine MyLine = new BattingLine();
        public PitcherLine MyPitching = new PitcherLine();
        public int Bonus;
    }

    /// <summary>
    /// The Asian tournament, every four years from 2026 in the middle of the season. The league's best
    /// hitters (my player too, if he's among them) play two group games, a semifinal and a final.
    /// Like the all-star game, nothing counts in league stats.
    /// </summary>
    public static class NationalTeam
    {
        public const string Name = "아시아 야구 대회";
        public const string Gold = "금메달";
        const int GoldBonus = 3000, SilverBonus = 1000, BronzeBonus = 500, SelectionBonus = 300;

        public static bool IsTournamentYear(int year) => (year - Career.StartYear) % 4 == 0;

        static readonly (string Country, int Strength, string[] Family, string[] Given)[] Rivals =
        {
            ("홍콩", -14, new[] { "찬", "웡", "렁", "쳉", "라우" }, new[] { "카만", "치훙", "워이", "킨록" }),
            ("대만", 1, new[] { "천", "린", "황", "장", "차이", "양" }, new[] { "즈하오", "위런", "관위", "보웨이", "젠밍" }),
            ("중국", -6, new[] { "왕", "리", "류", "자오", "쑨" }, new[] { "웨이", "하오", "레이", "젠", "펑" }),
            ("일본", 6, new[] { "사토", "스즈키", "다카하시", "다나카", "야마모토", "나카무라", "고바야시" }, new[] { "쇼타", "하루토", "료", "유토", "다이키", "겐타" }),
        };

        public static TournamentResult Play(Career career, IRandom random)
        {
            var result = new TournamentResult { Year = career.Year };
            var sim = new GameSimulator(random);
            result.Korea = KoreaSquad(career, random, out var mine);
            result.Selected = mine != null;
            var rivals = Rivals.Select(r => RivalTeam(r, random)).ToList();

            bool Play(string stage, Team opponent)
            {
                GameResult game = null;
                for (int i = 0; i < 3 && (game == null || game.IsTie); i++) // knockout ties replay
                    game = sim.Play(opponent, result.Korea, opponent.Rotation[(int)(random.NextDouble() * opponent.Rotation.Count)], result.Korea.Rotation[result.Games.Count % result.Korea.Rotation.Count]);
                bool won = game.Winner == result.Korea || game.IsTie && stage.StartsWith("조별");
                result.Games.Add(new TournamentGame { Stage = stage, Game = game, Won = won });
                if (mine != null && game.Batting.TryGetValue(mine, out var line)) Career.Accumulate(result.MyLine, line);
                if (mine != null && game.Pitching.TryGetValue(mine, out var arm))
                {
                    var g = new PitcherLine();
                    g.Add(arm, career.Me.Role == PitcherRole.Starter, false, false);
                    result.MyPitching.Add(g);
                }
                return won;
            }

            int groupWins = (Play("조별리그", rivals[0]) ? 1 : 0) + (Play("조별리그", rivals[1]) ? 1 : 0);
            if (groupWins == 0) result.Medal = null;
            else if (!Play("준결승", rivals[2])) result.Medal = "동메달";
            else result.Medal = Play("결승", rivals[3]) ? Gold : "은메달";

            if (mine != null)
            {
                result.Bonus = SelectionBonus + (result.Medal == Gold ? GoldBonus : result.Medal == "은메달" ? SilverBonus : result.Medal == "동메달" ? BronzeBonus : 0);
                career.Me.Money += result.Bonus;
                if (result.Medal == Gold) career.Me.GoldMedals++;
                career.Me.Morale = Math.Max(-CareerPlayer.MaxMorale, Math.Min(CareerPlayer.MaxMorale, career.Me.Morale + (result.Medal == Gold ? 5 : result.Medal == null ? -2 : 1)));
            }
            return result;
        }

        /// <summary>The nine best hitters in the league by OPS, the best arms, and my player if he makes the cut.</summary>
        static Team KoreaSquad(Career career, IRandom random, out Player mine)
        {
            var me = !career.InFarm && !career.Me.IsInjured ? career.Me.ForGame(random) : null;
            return career.PickSquad("대한민국", career.League.Teams, me, 9, 4, 7, out mine);
        }

        static Team RivalTeam((string Country, int Strength, string[] Family, string[] Given) rival, IRandom random)
        {
            var team = new RosterGenerator(random).CreateTeam(rival.Country);
            int Shift(int rating) => Math.Max(15, Math.Min(95, rating + rival.Strength));
            foreach (var p in team.Lineup.Concat(team.Rotation).Concat(team.Bullpen))
            {
                p.Id = $"{rival.Country}-{p.Id}";
                p.Name = rival.Family[(int)(random.NextDouble() * rival.Family.Length)] + " " + rival.Given[(int)(random.NextDouble() * rival.Given.Length)];
                var b = p.Batting;
                b.Contact = Shift(b.Contact); b.Power = Shift(b.Power); b.Eye = Shift(b.Eye); b.Speed = Shift(b.Speed); b.Defense = Shift(b.Defense);
                if (p.Pitching != null)
                {
                    var q = p.Pitching;
                    q.Velocity = Shift(q.Velocity); q.Control = Shift(q.Control); q.Breaking = Shift(q.Breaking); q.Stamina = Shift(q.Stamina);
                }
            }
            return team;
        }
    }
}
