using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>A retired player kept on the user's own team for later team content.</summary>
    public class RetiredPlayer
    {
        public string Name;
        public int Seasons;
        public BattingLine CareerLine;
        /// <summary>Best rating reached in each stat, so a long career isn't punished for its decline years.</summary>
        public Dictionary<Stat, int> PeakRatings;
        public int LegacyPoints;
        public List<string> Achievements = new List<string>();
        public List<string> Titles = new List<string>();
        public int AllStars;
        /// <summary>Career score for the hall of fame: hits and homers scaled to 144-game seasons, plus honours.</summary>
        public int HallOfFameScore;
        public bool HallOfFame;
        /// <summary>Club that retired this player's number: a hall-of-famer's club of ten seasons or more. Null if none.</summary>
        public string NumberRetiredBy;
        /// <summary>Money left at retirement, given to youth baseball for legacy points.</summary>
        public int Donation;
        public int DonationPoints;
        /// <summary>Club coins the retirement ceremony paid; shown on the farewell card, not saved.</summary>
        public int Coins;
        public Position Position = Position.CenterField;
        /// <summary>Number worn in the career; 0 if none was picked.</summary>
        public int Number;
        /// <summary>Number on my own team, set freely there; 0 = the career number.</summary>
        public int TeamNumber;
        public int ShownNumber => TeamNumber > 0 ? TeamNumber : Number;
        /// <summary>The club he played the most seasons for.</summary>
        public string MainClub;
        /// <summary>A pitcher's career line; null for hitters.</summary>
        public PitcherLine CareerPitching;

        public PitcherRole Role = PitcherRole.Starter;
        /// <summary>Main story ending this career reached; None for players retired before the story existed.</summary>
        public StoryEnding Ending;
        public StoryRoute Route;
        public bool IsPitcher => Position == Position.Pitcher;
        public int Overall => (int)Math.Round(PeakRatings.Values.Average());

        /// <summary>The farewell at his last game, longer for a bigger career.</summary>
        public List<string> Ceremony
        {
            get
            {
                var lines = new List<string>();
                string number = Number > 0 ? $"등번호 {Number}번 " : "";
                lines.Add($"{MainClub ?? "구단"} 홈구장. {number}{Name}의 은퇴식이 열렸다.");
                var arm = CareerPitching ?? new PitcherLine();
                string pitched = Role == PitcherRole.Closer ? $"통산 {arm.Saves}세이브" : Role == PitcherRole.Setup ? $"통산 {arm.Holds}홀드" : $"통산 {arm.Wins}승";
                lines.Add(IsPitcher
                    ? $"{Seasons}시즌, {pitched} {arm.Strikeouts}탈삼진. 마지막으로 마운드에 올라 시구를 했다."
                    : $"{Seasons}시즌, 통산 {CareerLine.Hits}안타 {CareerLine.HomeRuns}홈런. 마지막 타석에 들어서자 관중이 이름을 연호했다.");
                if (Titles.Count > 0) lines.Add($"타이틀 {Titles.Count}개의 기억이 전광판을 채웠다.");
                if (NumberRetiredBy != null) lines.Add($"{NumberRetiredBy}이(가) 그의 {(Number > 0 ? Number + "번" : "등번호")}을 영구결번으로 지정했다. 유니폼이 외야 펜스 위에 걸렸다.");
                if (HallOfFame) lines.Add("그리고 명예의 전당 헌액 소식이 전해졌다.");
                lines.Add(Donation > 0 ? $"그는 남은 {Donation:N0}만원을 유소년 야구에 기부하며 그라운드를 떠났다." : "그는 모자를 벗어 인사하고 그라운드를 떠났다.");
                return lines;
            }
        }
    }

    /// <summary>
    /// What outlasts a single career: the retired players the user has raised, and legacy points
    /// that give the next player a head start, so even a disappointing career still pays off.
    /// </summary>
    public class Club
    {
        public List<RetiredPlayer> Roster = new List<RetiredPlayer>();
        public int LegacyPoints;
        /// <summary>Levels my team has beaten at least once, and wins overall.</summary>
        public HashSet<ChallengeTier> ChallengesWon = new HashSet<ChallengeTier>();
        public int ChallengeWins;
        /// <summary>Real-calendar missions and check-ins, shared by every player slot.</summary>
        public DailyMissions Daily = new DailyMissions();
        /// <summary>Legend ids and rival names met by any player of mine, for the album.</summary>
        public HashSet<string> LegendsMet = new HashSet<string>();
        /// <summary>Legend ids whose record a player of mine has broken.</summary>
        public HashSet<string> LegendsSurpassed = new HashSet<string>();
        public List<string> RivalsMet = new List<string>();

        /// <summary>
        /// Club coins: the account-wide currency, apart from a player's salary money. Earned by playing
        /// (missions, check-ins, seasons, retirement ceremonies) and never sold for real money. Spent on
        /// looks, recovery tickets and the draw boxes (see <see cref="Gacha"/>). Ratings are never sold:
        /// draws only give training cards (faster training) and gear for my retired players' team.
        /// </summary>
        public int Coins;
        public HashSet<string> OwnedCosmetics = new HashSet<string>();
        /// <summary>The cosmetic worn for each kind; missing = the plain default.</summary>
        public Dictionary<CosmeticKind, string> Worn = new Dictionary<CosmeticKind, string>();
        /// <summary>Season rewind tickets bought, used from any slot.</summary>
        public int RewindTickets;
        /// <summary>Draw tickets: one draw each, from either box. Earned as rewards or bought.</summary>
        public int DrawTickets;
        /// <summary>Every draw ever made, for missions and records.</summary>
        public int Draws;
        /// <summary>Training cards owned and their level (1 to Gacha.MaxLevel).</summary>
        public Dictionary<string, int> CardLevels = new Dictionary<string, int>();
        /// <summary>Copies of each team gear piece drawn; its level comes from the count (Gacha.GearLevelFrom).</summary>
        public Dictionary<string, int> GearCopies = new Dictionary<string, int>();
        /// <summary>Draws since the last SSR, per box.</summary>
        public int[] Pity = new int[2];

        /// <summary>SSR pick tickets from the pity counter, per box, not used yet.</summary>
        public int[] SsrPicks = new int[2];

        /// <summary>Uses a pick ticket on the SSR of one stat; null if there is no ticket.</summary>
        public DrawResult UsePick(GachaBox box, Stat stat)
        {
            if (SsrPicks[(int)box] <= 0) return null;
            SsrPicks[(int)box]--;
            return AddCopy(box, Gacha.IdOf(box, stat, Rarity.SSR), Rarity.SSR, fromPity: true);
        }

        public int PityCount(GachaBox box) => Pity[(int)box];
        public void SetPityCount(GachaBox box, int count) => Pity[(int)box] = count;
        public int DrawsToPity(GachaBox box) => Gacha.Pity - PityCount(box);

        public int CopiesOf(string gearId) => GearCopies.TryGetValue(gearId, out var c) ? c : 0;

        public int LevelOf(GachaBox box, string id)
        {
            if (box == GachaBox.Cards) return CardLevels.TryGetValue(id, out var l) ? l : 0;
            var g = Gacha.GearPiece(id);
            return g == null ? 0 : Gacha.GearLevelFrom(g.Rarity, CopiesOf(id));
        }

        /// <summary>Adds one copy: a new item, a level up, or coins back once it is at the top level.</summary>
        public DrawResult AddCopy(GachaBox box, string id, Rarity rarity, bool fromPity = false)
        {
            int level = LevelOf(box, id);
            var result = new DrawResult
            {
                Box = box, Id = id, Rarity = rarity, FromPity = fromPity, New = level == 0,
                Name = box == GachaBox.Cards ? Gacha.Card(id)?.Name : Gacha.GearPiece(id)?.Name,
            };
            if (level >= Gacha.MaxLevelOf(box, rarity))
            {
                result.Refund = Gacha.Refund(rarity);
                Coins += result.Refund;
            }
            else if (box == GachaBox.Cards) CardLevels[id] = ++level;
            else
            {
                GearCopies[id] = CopiesOf(id) + 1;
                int after = LevelOf(box, id);
                result.LevelUp = after > level;
                level = after;
            }
            result.Level = Math.Max(1, level);
            return result;
        }

        /// <summary>
        /// The gear my team wears: for each stat, the owned piece that adds the most. Nothing to pick by
        /// hand, so a better piece goes on as soon as it is drawn.
        /// </summary>
        public Dictionary<Stat, (TeamGear Gear, int Level, int Bonus)> TeamGearWorn()
        {
            var worn = new Dictionary<Stat, (TeamGear, int, int)>();
            foreach (var kv in GearCopies)
            {
                var g = Gacha.GearPiece(kv.Key);
                int level = g == null ? 0 : Gacha.GearLevelFrom(g.Rarity, kv.Value);
                if (level == 0) continue;
                int bonus = Gacha.GearBonus(g.Rarity, level);
                if (!worn.TryGetValue(g.Stat, out var best) || bonus > best.Item3) worn[g.Stat] = (g, level, bonus);
            }
            return worn;
        }

        /// <summary>Rating points my retired players get in one stat from team gear.</summary>
        public int TeamGearBonus(Stat stat) => TeamGearWorn().TryGetValue(stat, out var w) ? w.Bonus : 0;

        /// <summary>Puts an owned card in one of the player's card slots (null empties it).</summary>
        /// <remarks>One card per stat: putting in a card takes out any other card of the same stat.
        public bool EquipCard(Career career, int slot, string cardId)
        {
            if (slot < 0 || slot >= Gacha.CardSlots) return false;
            while (career.Cards.Count < Gacha.CardSlots) career.Cards.Add(null);
            if (cardId != null)
            {
                if (LevelOf(GachaBox.Cards, cardId) == 0) return false;
                var stat = Gacha.Card(cardId).Stat;
                for (int k = 0; k < career.Cards.Count; k++)
                    if (career.Cards[k] != null && Gacha.Card(career.Cards[k])?.Stat == stat) career.Cards[k] = null;
            }
            career.Cards[slot] = cardId;
            RefreshCards(career);
            return true;
        }

        /// <summary>Copies the current card levels into the career, so its training sees level ups.</summary>
        public void RefreshCards(Career career)
        {
            career.CardBoost.Clear();
            foreach (var id in career.Cards)
            {
                var card = id == null ? null : Gacha.Card(id);
                int level = card == null ? 0 : LevelOf(GachaBox.Cards, id);
                if (level == 0) continue;
                // One card per stat counts; the best one if an old save has two.
                career.CardBoost.TryGetValue(card.Stat, out var have);
                career.CardBoost[card.Stat] = Math.Max(have, Gacha.CardBonus(card.Rarity, level));
            }
        }

        /// <summary>My retired players' league against AI clubs.</summary>
        public MyTeamLeague TeamLeague = new MyTeamLeague();
        /// <summary>The arena: my team against other clubs' teams of the same weight class.</summary>
        public Arena Arena = new Arena();

        public const int MissionCoins = 30;
        /// <summary>Coins for one recovery ticket.</summary>
        public const int TicketCoins = 200;
        /// <summary>A retirement ceremony pays this plus two coins per legacy point the career earned.</summary>
        public const int RetirementCoins = 500;
        /// <summary>Coins for finishing a 36-game season; longer seasons pay in proportion (x2 at 72, x4 at 144).</summary>
        public const int SeasonCoins = 50;

        public static int SeasonCoinsFor(int games) => SeasonCoins * games / 36;

        /// <summary>Pays the coins for a finished season of this length; returns how many.</summary>
        public int PaySeason(int games)
        {
            int coins = SeasonCoinsFor(games);
            Coins += coins;
            return coins;
        }

        public bool Owns(string cosmeticId) => OwnedCosmetics.Contains(cosmeticId);

        /// <summary>Buys a cosmetic with coins; false if not sold for coins, owned already or too dear.</summary>
        public bool BuyCosmetic(string id)
        {
            var c = Cosmetics.Find(id);
            if (c == null || c.Coins <= 0 || c.PassOnly || Owns(id) || Coins < c.Coins) return false;
            Coins -= c.Coins;
            OwnedCosmetics.Add(id);
            return true;
        }

        /// <summary>Hands over a cosmetic bought with money, earned from the pass or a support pack.</summary>
        public void GrantCosmetic(string id)
        {
            if (Cosmetics.Find(id) != null) OwnedCosmetics.Add(id);
        }

        /// <summary>Wears an owned cosmetic, or takes the kind off when it is already worn.</summary>
        public bool Wear(string id)
        {
            var c = Cosmetics.Find(id);
            if (c == null || !Owns(id)) return false;
            if (WornId(c.Kind) == id) Worn.Remove(c.Kind);
            else Worn[c.Kind] = id;
            return true;
        }

        public string WornId(CosmeticKind kind) => Worn.TryGetValue(kind, out var id) ? id : null;

        /// <summary>Buys recovery tickets with coins.</summary>
        public bool BuyTickets(int count)
        {
            if (count <= 0 || Coins < TicketCoins * count) return false;
            Coins -= TicketCoins * count;
            Daily.RecoveryTickets += count;
            return true;
        }

        /// <summary>Legends met so far, counting the career in progress.</summary>
        public HashSet<string> AllLegendsMet(Career career)
        {
            var all = new HashSet<string>(LegendsMet);
            if (career != null) all.UnionWith(career.Me.MetLegends);
            return all;
        }

        /// <summary>Added to every starting rating of the next new player, at most +4.</summary>
        public int StartBonus => Math.Min(4, LegacyPoints / 400);

        /// <summary>Legends whose record has fallen, counting the career in progress.</summary>
        public HashSet<string> AllLegendsSurpassed(Career career)
        {
            var all = new HashSet<string>(LegendsSurpassed);
            if (career != null) all.UnionWith(career.SurpassedLegends);
            return all;
        }

        /// <summary>
        /// Hall of Fame bar: about half of well-raised careers clear it. The score is career totals plus
        /// steady good seasons; titles, rings and All-Star votes stay out since team and rivals sway them.
        /// </summary>
        public const int HallOfFameLine = 890;
        /// <summary>A season mostly in the first team at OPS .850 or better (pitchers on the same scale) earns this.</summary>
        public const int GoodSeasonPoints = 20;
        /// <summary>And at .950 or better, this much more.</summary>
        public const int GreatSeasonPoints = 10;
        public const double GoodSeasonForm = 0.850, GreatSeasonForm = 0.950;
        const int HallOfFamePoints = 100;
        /// <summary>만원 of leftover money per legacy point when donated at retirement.</summary>
        public const int DonationPerPoint = 10000;

        /// <summary>Achievement ids any retired player of mine ever reached.</summary>
        public HashSet<string> Collection => new HashSet<string>(Roster.SelectMany(p => p.Achievements));

        /// <summary>
        /// Career totals scaled to full seasons (hits and homers, a starter's wins and strikeouts, a
        /// reliever's saves, holds and strikeouts; weighted so each kind of career scores alike) plus
        /// points for every good and great season, so a long steady career counts more than one hot year.
        /// </summary>
        public static int HallOfFameScore(CareerPlayer me)
        {
            double total = 0;
            foreach (var r in me.History)
            {
                double scale = (double)League.FullSeason / Math.Max(1, r.Games);
                double form;
                if (r.Pitching != null)
                {
                    var p = r.Pitching;
                    total += scale * (me.Role == PitcherRole.Starter
                        ? 0.97 * (p.Wins + p.Strikeouts / 10.0)
                        : 0.9 * (p.Saves + p.Holds + p.Strikeouts / 10.0));
                    form = Career.PitchingForm(p);
                }
                else
                {
                    total += scale * 0.85 * (r.Line.Hits / 10.0 + r.Line.HomeRuns / 2.0);
                    form = Career.Ops(r.Line);
                }
                if (!IsSteadySeason(r)) continue;
                if (form >= GoodSeasonForm) total += GoodSeasonPoints;
                if (form >= GreatSeasonForm) total += GreatSeasonPoints;
            }
            return (int)total;
        }

        /// <summary>A season that can count as good: not in the army and at most half of it in 2군.</summary>
        static bool IsSteadySeason(SeasonRecord r) => !r.Military && r.FarmDays * 2 < r.Games;

        public RetiredPlayer Retire(Career career)
        {
            var me = career.Me;
            var line = new BattingLine();
            foreach (var season in me.History) Career.Accumulate(line, season.Line);

            var retired = new RetiredPlayer
            {
                Name = me.Name,
                Seasons = me.History.Count,
                CareerLine = line,
                PeakRatings = new Dictionary<Stat, int>(me.PeakRatings),
                Achievements = new List<string>(me.Achievements),
                Titles = me.History.SelectMany(h => h.Titles).ToList(),
                Position = me.Position,
                Role = me.Role,
                Number = me.Number,
                MainClub = me.History.GroupBy(h => h.Club ?? career.MyTeam.Name).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? career.MyTeam.Name,
            };
            if (me.IsPitcher)
            {
                retired.CareerPitching = new PitcherLine();
                foreach (var season in me.History) if (season.Pitching != null) retired.CareerPitching.Add(season.Pitching);
            }

            // A season's worth: hits and homers, or wins and strikeouts, scaled to a full 144 games so a
            // short-season career earns the same.
            double Worth(SeasonRecord r) => (r.Pitching != null
                ? r.Pitching.Wins + r.Pitching.Saves * 0.6 + r.Pitching.Holds * 0.4 + r.Pitching.Strikeouts / 10.0
                : r.Line.Hits / 10.0 + r.Line.HomeRuns / 2.0) * League.FullSeason / r.Games;
            retired.LegacyPoints = (int)me.History.Sum(r => Worth(r) + 5);
            retired.AllStars = me.AllStarSelections;
            retired.HallOfFameScore = HallOfFameScore(me);
            retired.HallOfFame = retired.HallOfFameScore >= HallOfFameLine;
            if (retired.HallOfFame)
            {
                retired.LegacyPoints += HallOfFamePoints;
                var longest = me.History.GroupBy(h => h.Club ?? career.MyTeam.Name).OrderByDescending(g => g.Count()).FirstOrDefault();
                if (longest != null && longest.Count() >= 10) retired.NumberRetiredBy = longest.Key;
            }

            // Money can't follow the player into retirement; it goes to youth baseball instead.
            retired.Donation = Math.Max(0, me.Money);
            retired.DonationPoints = retired.Donation / DonationPerPoint;
            me.Money = 0;

            LegendsMet.UnionWith(me.MetLegends);
            LegendsSurpassed.UnionWith(career.SurpassedLegends);
            if (career.Rival != null && !RivalsMet.Contains(career.Rival.Name)) RivalsMet.Add(career.Rival.Name);

            retired.Ending = MainStory.EndingFor(career, retired);
            retired.Route = career.Story.Route;
            Roster.Add(retired);
            LegacyPoints += retired.LegacyPoints + retired.DonationPoints;
            retired.Coins = RetirementCoins + 2 * retired.LegacyPoints;
            Coins += retired.Coins;
            return retired;
        }
    }
}
