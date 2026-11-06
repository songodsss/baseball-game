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
        /// <summary>The Asian tournament (gold exempts from service) or the WBC (the world's best, no exemption).</summary>
        public bool Wbc;
        public string Name => Wbc ? NationalTeam.WbcName : NationalTeam.Name;
        public bool Selected;
        public Team Korea;
        public List<TournamentGame> Games = new List<TournamentGame>();
        /// <summary>Asia: "금메달", "은메달", "동메달" or null. WBC: "우승", "준우승", "4강", "8강" or null (out in the first round).</summary>
        public string Medal;
        public BattingLine MyLine = new BattingLine();
        public PitcherLine MyPitching = new PitcherLine();
        public int Bonus;
    }

    /// <summary>
    /// Two national-team tournaments, each every four years in the league's midseason break: the Asian
    /// tournament from 2026, and the World Baseball Classic from 2028 against the world's best (Japan,
    /// the USA, the Dominican Republic...). Only Asian gold exempts from military service. The league's best
    /// hitters (my player too, if he's among them) play a group stage and knockout games.
    /// The league takes a break while it's on. When my player is picked, Korea's games are played one
    /// at a time with my big at bats; otherwise it is played at once. Nothing counts in league stats.
    /// </summary>
    public static class NationalTeam
    {
        public const string Name = "아시아 야구 대회";
        public const string WbcName = "WBC";
        public const string Gold = "금메달";
        public const string WbcChampion = "우승";
        const int GoldBonus = 3000, SilverBonus = 1000, BronzeBonus = 500, SelectionBonus = 300;
        const int WbcWinBonus = 5000, WbcFinalBonus = 2000, WbcSemiBonus = 1000, WbcSelectionBonus = 500;

        /// <summary>A tournament of either kind this year.</summary>
        public static bool IsTournamentYear(int year) => IsAsiaYear(year) || IsWbcYear(year);
        /// <summary>The Asian tournament: the one whose gold exempts from service.</summary>
        public static bool IsAsiaYear(int year) => (year - Career.StartYear) % 4 == 0;
        public static bool IsWbcYear(int year) => (year - Career.StartYear) % 4 == 2;
        public static string NameOf(int year) => IsWbcYear(year) ? WbcName : Name;

        static readonly (string Country, int Strength, string[] Family, string[] Given)[] Rivals =
        {
            ("홍콩", -14, new[] { "찬", "웡", "렁", "쳉", "라우" }, new[] { "카만", "치훙", "워이", "킨록" }),
            ("대만", 1, new[] { "천", "린", "황", "장", "차이", "양" }, new[] { "즈하오", "위런", "관위", "보웨이", "젠밍" }),
            ("중국", -6, new[] { "왕", "리", "류", "자오", "쑨" }, new[] { "웨이", "하오", "레이", "젠", "펑" }),
            ("일본", 6, new[] { "사토", "스즈키", "다카하시", "다나카", "야마모토", "나카무라", "고바야시" }, new[] { "쇼타", "하루토", "료", "유토", "다이키", "겐타" }),
        };

        // The WBC: the first round's group, then the knockout sides.
        static readonly (string Country, int Strength, string[] Family, string[] Given)[] WbcGroup =
        {
            ("체코", -10, new[] { "노박", "스보보다", "드보르자크", "체르니", "프로하스카" }, new[] { "얀", "토마시", "마르틴", "야쿠프", "온드르제이" }),
            ("호주", -3, new[] { "스미스", "존스", "윌리엄스", "브라운", "켈리" }, new[] { "잭", "올리버", "라이언", "미첼", "벤" }),
            ("일본", 9, new[] { "사토", "스즈키", "다카하시", "다나카", "야마모토", "나카무라", "오타니" }, new[] { "쇼타", "하루토", "료", "유토", "다이키", "쇼헤이" }),
        };
        static readonly (string Country, int Strength, string[] Family, string[] Given)[] WbcKnockout =
        {
            ("도미니카공화국", 9, new[] { "페레스", "로드리게스", "마르티네스", "곤살레스", "산체스" }, new[] { "후안", "라파엘", "미겔", "호세", "넬슨" }),
            ("베네수엘라", 7, new[] { "카브레라", "에르난데스", "가르시아", "토레스", "라미레스" }, new[] { "루이스", "카를로스", "안드레스", "윌슨", "에두아르도" }),
            ("미국", 11, new[] { "존슨", "밀러", "데이비스", "윌슨", "앤더슨", "테일러" }, new[] { "마이크", "브라이스", "애런", "트레버", "클레이튼" }),
            ("멕시코", 4, new[] { "로페스", "플로레스", "레예스", "모랄레스", "오르티스" }, new[] { "알레한드로", "훌리오", "이삭", "라몬", "디에고" }),
        };

        /// <summary>Where a tournament replay stopped: before Korea's next game, at my big at bat, or at the end.</summary>
        public class Run
        {
            public TournamentResult Result;
            /// <summary>Korea's latest game: finished, or paused at <see cref="DayResult.AtBat"/>.</summary>
            public DayResult Current;
            public string CurrentStage;
            public Team CurrentOpponent;
            /// <summary>The next game's stage and opponent; null when the tournament is over.</summary>
            public string NextStage;
            public Team NextOpponent;
            public bool Done;
            public int GamesPlayed => Result.Games.Count;
        }

        /// <summary>
        /// Plays the tournament from <paramref name="seed"/>: Korea's games up to <paramref name="games"/> of
        /// them, my big at bats answered from <paramref name="decisions"/> in order; it stops at the first
        /// unanswered one or before the next game. With <paramref name="auto"/> it plays to the end.
        /// Same seed and answers, same tournament, so it can be played a game at a time and survive a save.
        /// Rewards are not handed out here (see <see cref="Reward"/>).
        /// </summary>
        public static Run Play(Career career, int seed, IList<Approach> decisions, int games, bool auto)
        {
            var random = new SeededRandom(seed);
            var run = new Run { Result = new TournamentResult { Year = career.Year, Wbc = IsWbcYear(career.Year) } };
            var result = run.Result;
            result.Korea = KoreaSquad(career, random, out var mine);
            result.Selected = mine != null;
            var rivals = (result.Wbc ? WbcGroup : Rivals).Select(r => RivalTeam(r, random)).ToList();
            // The WBC's knockout sides, in a random order: who Korea meets in the quarterfinal, semifinal and final.
            var knockout = WbcKnockout.Select(r => RivalTeam(r, random)).OrderBy(_ => random.NextDouble()).ToList();
            int asked = 0;

            // True/false: won or lost; null: the replay stopped here.
            bool? Play(string stage, Team opponent)
            {
                if (!auto && result.Games.Count >= games)
                {
                    run.NextStage = stage;
                    run.NextOpponent = opponent;
                    return null;
                }
                var day = new DayResult { Season = career.Season, Day = career.Day, Action = DailyAction.Rest };
                run.Current = day;
                run.CurrentStage = stage;
                run.CurrentOpponent = opponent;
                GameResult game = null;
                for (int i = 0; i < 3 && (game == null || game.IsTie); i++) // knockout ties replay
                {
                    var theirs = opponent.Rotation[(int)(random.NextDouble() * opponent.Rotation.Count)];
                    var ours = result.Korea.Rotation[result.Games.Count % result.Korea.Rotation.Count];
                    var sim = new GameSimulator(new SeededRandom((int)(random.NextDouble() * int.MaxValue)));
                    if (mine != null)
                    {
                        sim.DecideFor = mine;
                        sim.Decide = s => asked < decisions.Count ? decisions[asked++] : auto ? Approach.Normal : (Approach?)null;
                    }
                    game = sim.Play(opponent, result.Korea, theirs, ours);
                    var events = game?.Events ?? sim.Partial.Events;
                    if (mine != null)
                        day.AtBatLog = events.Where(e => (e.Batter == mine || e.Pitcher == mine) && e.Note != null)
                            .Select(e => $"{Commentary.HalfInning(e.Inning, e.IsTopHalf)} {Commentary.Describe(e)} ({e.Note})").ToList();
                    if (game == null)
                    {
                        day.AtBat = sim.Suspended;
                        return null;
                    }
                }
                bool won = game.Winner == result.Korea || game.IsTie && (stage.StartsWith("조별") || stage.StartsWith("1라운드"));
                result.Games.Add(new TournamentGame { Stage = stage, Game = game, Won = won });
                day.MyGame = game;
                if (mine != null && game.Batting.TryGetValue(mine, out var line))
                {
                    Career.Accumulate(result.MyLine, line);
                    day.MyLine = line;
                    day.MyGamePlayer = mine;
                }
                if (mine != null && game.Pitching.TryGetValue(mine, out var arm))
                {
                    var g = new PitcherLine();
                    g.Add(arm, career.Me.Role == PitcherRole.Starter, false, false);
                    result.MyPitching.Add(g);
                    day.MyPitching = g;
                    day.MyGamePlayer = mine;
                }
                return won;
            }

            if (result.Wbc) return PlayWbc(run, Play, rivals, knockout);

            var g1 = Play("조별리그 1차전", rivals[0]);
            if (g1 == null) return run;
            var g2 = Play("조별리그 2차전", rivals[1]);
            if (g2 == null) return run;
            int groupWins = (g1.Value ? 1 : 0) + (g2.Value ? 1 : 0);
            if (groupWins > 0)
            {
                var semi = Play("준결승", rivals[2]);
                if (semi == null) return run;
                if (!semi.Value) result.Medal = "동메달";
                else
                {
                    var final = Play("결승", rivals[3]);
                    if (final == null) return run;
                    result.Medal = final.Value ? Gold : "은메달";
                }
            }
            run.Done = true;
            return run;
        }

        /// <summary>
        /// The WBC: three first-round games (two wins go through), then a quarterfinal, semifinal and
        /// final against the strongest sides.
        /// </summary>
        static Run PlayWbc(Run run, Func<string, Team, bool?> play, List<Team> group, List<Team> knockout)
        {
            var result = run.Result;
            int wins = 0;
            for (int i = 0; i < 3; i++)
            {
                var g = play($"1라운드 {i + 1}차전", group[i]);
                if (g == null) return run;
                if (g.Value) wins++;
            }
            if (wins >= 2)
            {
                result.Medal = "8강";
                var quarter = play("8강", knockout[0]);
                if (quarter == null) return run;
                if (quarter.Value)
                {
                    result.Medal = "4강";
                    var semi = play("준결승", knockout[1]);
                    if (semi == null) return run;
                    if (semi.Value)
                    {
                        var final = play("결승", knockout[2]);
                        if (final == null) return run;
                        result.Medal = final.Value ? WbcChampion : "준우승";
                    }
                }
            }
            run.Done = true;
            return run;
        }

        /// <summary>The best finish of the tournament: Asian gold or WBC title.</summary>
        public static bool Won(TournamentResult t) => t.Wbc ? t.Medal == WbcChampion : t.Medal == Gold;

        /// <summary>Selection money, medal money, the medal count and the mood: handed out once, when the tournament ends.</summary>
        public static void Reward(Career career, TournamentResult result)
        {
            if (!result.Selected) return;
            result.Bonus = result.Wbc
                ? WbcSelectionBonus + (result.Medal == WbcChampion ? WbcWinBonus : result.Medal == "준우승" ? WbcFinalBonus : result.Medal == "4강" ? WbcSemiBonus : 0)
                : SelectionBonus + (result.Medal == Gold ? GoldBonus : result.Medal == "은메달" ? SilverBonus : result.Medal == "동메달" ? BronzeBonus : 0);
            career.Me.Money += result.Bonus;
            if (!result.Wbc && result.Medal == Gold) career.Me.GoldMedals++;
            if (result.Wbc && result.Medal == WbcChampion) career.Me.WbcTitles++;
            career.Me.Morale = Math.Max(-CareerPlayer.MaxMorale, Math.Min(CareerPlayer.MaxMorale, career.Me.Morale + (Won(result) ? 5 : result.Medal == null ? -2 : 1)));
        }

        /// <summary>How the tournament ended for Korea, in words ("금메달", "WBC 4강", "1라운드 탈락").</summary>
        public static string Finish(TournamentResult t) => t.Wbc
            ? (t.Medal == null ? "1라운드 탈락" : t.Medal == "8강" ? "8강 탈락" : t.Medal == "4강" ? "4강" : t.Medal)
            : t.Medal ?? "조별리그 탈락";

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
