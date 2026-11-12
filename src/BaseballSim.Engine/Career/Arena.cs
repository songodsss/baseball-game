using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>An arena opponent: another club's team, matched to my team's weight class.</summary>
    public class ArenaOpponent
    {
        public string Owner;
        public int Rating;
        /// <summary>Team power, for the weight-class label.</summary>
        public int Power;
        public int Seed;
        /// <summary>-1 a little weaker, 0 even, +1 a little stronger: more rating for a harder win.</summary>
        public int Edge;
    }

    public class ArenaResult
    {
        public GameResult Game;
        public Team Mine;
        public Team Opponent;
        public ArenaOpponent Rival;
        public bool Won => Game.Winner == Mine;
        public int RatingChange;
        public int Coins;
    }

    /// <summary>
    /// The arena: my team (exactly as built from my retired players) against other clubs' teams of
    /// the same weight class. A rating rises and falls with results, its tier pays coins when the
    /// two-week arena season ends, and the rating then eases back toward the middle.
    /// Until an online server keeps real players' teams, opponents are generated here to match my
    /// team's power, so the screens, ratings and rewards can be played through. IArenaOpponents is
    /// where a server list slots in later.
    /// </summary>
    public class Arena
    {
        public const int StartRating = 1000;
        public const int FreeEntriesPerDay = 5;
        public const int ExtraEntriesPerDay = 5;
        public const int ExtraEntryCoins = 100;
        public const int WinCoins = 30;
        public const int SeasonDays = 14;
        static readonly DateTime Epoch = new DateTime(2026, 11, 2);

        static readonly string[] TierNames = { "브론즈", "실버", "골드", "플래티넘", "다이아", "챔피언" };
        static readonly int[] TierFloors = { 0, 1050, 1150, 1250, 1400, 1550 };
        static readonly int[] TierCoins = { 100, 200, 350, 500, 800, 1200 };
        static readonly string[] OwnerNames =
        {
            "푸른 야구단", "새벽 타격회", "번개 구단", "한밤 홈런클럽", "골목 리그", "강철 불펜", "은하 베이스볼",
            "바람 구단", "돌직구 클럽", "여름 야구단", "구름 타선", "별빛 구단", "단풍 리그", "파도 야구단",
        };

        public int Rating = StartRating;
        public int SeasonId = -1;
        public int SeasonBest = StartRating;
        public int Wins, Losses;
        public string Date = "";
        public int EntriesUsed;
        public int ExtraEntriesBought;
        /// <summary>Coins and tier from the season that just ended, to show once; null if none.</summary>
        public string LastSeasonNote;
        public List<ArenaOpponent> Candidates = new List<ArenaOpponent>();

        public static string TierOf(int rating)
        {
            int t = 0;
            for (int k = 0; k < TierFloors.Length; k++) if (rating >= TierFloors[k]) t = k;
            return TierNames[t];
        }

        public static int TierCoinsOf(int rating)
        {
            int t = 0;
            for (int k = 0; k < TierFloors.Length; k++) if (rating >= TierFloors[k]) t = k;
            return TierCoins[t];
        }

        public string Tier => TierOf(Rating);

        public static int SeasonOf(string date)
        {
            var day = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            return (int)Math.Floor((day - Epoch).TotalDays / SeasonDays);
        }

        public static string LastDayOf(int seasonId) => DailyMissions.DateOf(Epoch.AddDays((seasonId + 1) * SeasonDays - 1));

        /// <summary>
        /// A new date resets the day's entries; a new arena season pays the old one's best tier in
        /// coins and pulls the rating halfway back to the start.
        /// </summary>
        public void Refresh(string today, Club club)
        {
            if (Date != today)
            {
                Date = today;
                EntriesUsed = 0;
                ExtraEntriesBought = 0;
            }
            int season = SeasonOf(today);
            if (season == SeasonId) return;
            if (SeasonId >= 0 && Wins + Losses > 0)
            {
                int coins = TierCoinsOf(SeasonBest);
                club.Coins += coins;
                LastSeasonNote = $"지난 결투장 시즌 최고 {TierOf(SeasonBest)} · 코인 +{coins}";
            }
            SeasonId = season;
            Rating = StartRating + (Rating - StartRating) / 2;
            SeasonBest = Rating;
            Wins = Losses = 0;
            Candidates.Clear();
        }

        public int EntriesLeft => Math.Max(0, FreeEntriesPerDay + ExtraEntriesBought - EntriesUsed);
        public bool CanBuyEntry => ExtraEntriesBought < ExtraEntriesPerDay;

        public bool BuyEntry(Club club)
        {
            if (!CanBuyEntry || club.Coins < ExtraEntryCoins) return false;
            club.Coins -= ExtraEntryCoins;
            ExtraEntriesBought++;
            return true;
        }

        /// <summary>Average of the lineup's batting and the rotation's pitching ratings.</summary>
        public static int Power(Team team)
        {
            double bat = team.Lineup.Average(p => (p.Batting.Contact + p.Batting.Power + p.Batting.Eye + p.Batting.Speed + p.Batting.Defense) / 5.0);
            double pitch = team.Rotation.Where(p => p.Pitching != null)
                .Select(p => (p.Pitching.Velocity + p.Pitching.Control + p.Pitching.Breaking + p.Pitching.Stamina) / 4.0)
                .DefaultIfEmpty(bat).Average();
            return (int)Math.Round(bat * 0.6 + pitch * 0.4);
        }

        /// <summary>My team's power, building it the way a game would (no randomness in my retirees).</summary>
        public static int MyPower(Club club) => Power(DreamTeam.Build(club, new SeededRandom(1)));

        /// <summary>Three opponents near my rating and power: a little weaker, even and a little stronger.</summary>
        public List<ArenaOpponent> FindOpponents(Club club, IRandom random)
        {
            int power = MyPower(club);
            Candidates = new[] { -1, 0, 1 }.Select(edge => new ArenaOpponent
            {
                Owner = OwnerNames[(int)(random.NextDouble() * OwnerNames.Length)],
                Rating = Math.Max(0, Rating + edge * 40 + (int)(random.NextDouble() * 30) - 15),
                Power = power + edge * 3,
                Seed = (int)(random.NextDouble() * int.MaxValue),
                Edge = edge,
            }).ToList();
            return Candidates;
        }

        /// <summary>An opponent's team, rebuilt from its seed and shifted to its power.</summary>
        public static Team BuildOpponent(ArenaOpponent rival)
        {
            var random = new SeededRandom(rival.Seed);
            var team = new RosterGenerator(random).CreateTeam(rival.Owner);
            int shift = rival.Power - Power(team);
            int Shift(int rating) => Math.Max(15, Math.Min(99, rating + shift));
            foreach (var p in team.Lineup.Concat(team.Rotation).Concat(team.Bullpen))
            {
                p.Id = "arena-" + p.Id;
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

        /// <summary>Elo-style change: beating a higher-rated side gains more, losing to a lower one costs more.</summary>
        public static int Change(int mine, int theirs, bool won)
        {
            double expected = 1 / (1 + Math.Pow(10, (theirs - mine) / 400.0));
            return (int)Math.Round(32 * ((won ? 1 : 0) - expected));
        }

        /// <summary>Plays a candidate, my team away; null with no retirees, no entries left or a stale pick.</summary>
        public ArenaResult Play(Club club, int candidate, IRandom random)
        {
            if (club.Roster.Count == 0 || EntriesLeft == 0 || candidate < 0 || candidate >= Candidates.Count) return null;
            var rival = Candidates[candidate];
            var mine = DreamTeam.Build(club, random);
            var opponent = BuildOpponent(rival);
            var sim = new GameSimulator(random);
            GameResult game = null;
            for (int k = 0; k < 3 && (game == null || game.IsTie); k++)
                game = sim.Play(mine, opponent, mine.Rotation[k % mine.Rotation.Count], opponent.Rotation[k % opponent.Rotation.Count]);

            EntriesUsed++;
            var result = new ArenaResult { Game = game, Mine = mine, Opponent = opponent, Rival = rival };
            bool won = game.Winner == mine;
            result.RatingChange = game.IsTie ? 0 : Change(Rating, rival.Rating, won);
            Rating = Math.Max(0, Rating + result.RatingChange);
            SeasonBest = Math.Max(SeasonBest, Rating);
            if (won) { Wins++; result.Coins = WinCoins; club.Coins += WinCoins; }
            else if (!game.IsTie) Losses++;
            Candidates.Clear();
            return result;
        }
    }
}
