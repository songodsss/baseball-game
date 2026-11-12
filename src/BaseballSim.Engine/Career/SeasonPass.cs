using System;
using System.Collections.Generic;
using System.Globalization;

namespace BaseballSim.Career
{
    public enum PassRewardKind { Coins, Tickets, Cosmetic }

    public struct PassReward
    {
        public PassRewardKind Kind;
        public int Amount;
        public string CosmeticId;

        public string Text => Kind == PassRewardKind.Coins ? $"코인 {Amount}"
            : Kind == PassRewardKind.Tickets ? $"회복권 {Amount}장"
            : $"{Cosmetics.Find(CosmeticId)?.Name ?? CosmeticId}";
    }

    /// <summary>
    /// The season pass: a real-calendar 30-day season. Daily missions, check-ins and weekly
    /// challenges give pass points; every 100 points is a level with a free reward, and the paid
    /// track (bought for the season) adds coins, tickets and pass-only cosmetics. Never strength.
    /// </summary>
    public class SeasonPass
    {
        public const int Days = 30;
        public const int Levels = 20;
        public const int PointsPerLevel = 100;
        public const int MissionPoints = 15;
        public const int CheckInPoints = 10;
        public const int WeeklyPoints = 60;
        /// <summary>Coins instead of a pass cosmetic already owned from an earlier season.</summary>
        public const int OwnedCosmeticCoins = 1000;

        static readonly DateTime Epoch = new DateTime(2026, 11, 1);

        /// <summary>Which 30-day pass season a date falls in; -1 before none has started.</summary>
        public int SeasonId = -1;
        public int Points;
        /// <summary>The paid track is open for this season.</summary>
        public bool Premium;
        public HashSet<int> ClaimedFree = new HashSet<int>();
        public HashSet<int> ClaimedPremium = new HashSet<int>();

        public int Level => Math.Min(Levels, Points / PointsPerLevel);

        public static int SeasonOf(string date)
        {
            var day = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return (int)Math.Floor((day - Epoch).TotalDays / Days);
        }

        /// <summary>The last day of a pass season, for "ends on" labels.</summary>
        public static string LastDayOf(int seasonId) => DailyMissions.DateOf(Epoch.AddDays((seasonId + 1) * Days - 1));

        /// <summary>A new pass season starts over: points, claims and the paid track. True if it did.</summary>
        public bool Refresh(string today)
        {
            int season = SeasonOf(today);
            if (season == SeasonId) return false;
            SeasonId = season;
            Points = 0;
            Premium = false;
            ClaimedFree.Clear();
            ClaimedPremium.Clear();
            return true;
        }

        public void Add(int points) => Points = Math.Max(0, Points + points);

        public static PassReward FreeReward(int level)
        {
            if (level % 5 == 0) return new PassReward { Kind = PassRewardKind.Coins, Amount = 200 };
            if (level % 3 == 0) return new PassReward { Kind = PassRewardKind.Tickets, Amount = 1 };
            return new PassReward { Kind = PassRewardKind.Coins, Amount = 50 };
        }

        public static PassReward PremiumReward(int level)
        {
            switch (level)
            {
                case 10: return new PassReward { Kind = PassRewardKind.Cosmetic, CosmeticId = "homer_confetti" };
                case 15: return new PassReward { Kind = PassRewardKind.Cosmetic, CosmeticId = "town_snow" };
                case 20: return new PassReward { Kind = PassRewardKind.Cosmetic, CosmeticId = "stadium_snow" };
            }
            if (level == 1) return new PassReward { Kind = PassRewardKind.Coins, Amount = 300 };
            if (level % 2 == 0) return new PassReward { Kind = PassRewardKind.Tickets, Amount = 2 };
            return new PassReward { Kind = PassRewardKind.Coins, Amount = 150 };
        }

        public bool CanClaim(int level, bool premium) =>
            level >= 1 && level <= Level && (premium ? Premium && !ClaimedPremium.Contains(level) : !ClaimedFree.Contains(level));

        /// <summary>Takes a reached level's reward into the club. Returns what was given, or null.</summary>
        public PassReward? Claim(int level, bool premium, Club club)
        {
            if (!CanClaim(level, premium)) return null;
            var reward = premium ? PremiumReward(level) : FreeReward(level);
            if (reward.Kind == PassRewardKind.Cosmetic && club.Owns(reward.CosmeticId))
                reward = new PassReward { Kind = PassRewardKind.Coins, Amount = OwnedCosmeticCoins };
            switch (reward.Kind)
            {
                case PassRewardKind.Coins: club.Coins += reward.Amount; break;
                case PassRewardKind.Tickets: club.Daily.RecoveryTickets += reward.Amount; break;
                default: club.GrantCosmetic(reward.CosmeticId); break;
            }
            (premium ? ClaimedPremium : ClaimedFree).Add(level);
            return reward;
        }

        /// <summary>Rewards reached but not yet taken, on the tracks open to the player.</summary>
        public int Unclaimed
        {
            get
            {
                int n = 0;
                for (int level = 1; level <= Level; level++)
                {
                    if (CanClaim(level, false)) n++;
                    if (CanClaim(level, true)) n++;
                }
                return n;
            }
        }
    }
}
