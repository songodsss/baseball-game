using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BaseballSim.Career
{
    public enum MissionKind { Train, Games, Performance, Rest, BigMoment, WatchGame, DailyDone }

    public enum RewardKind { Money, RecoveryTicket, Legacy, Coins }

    public class Mission
    {
        public MissionKind Kind;
        public int Goal;
        public int Progress;
        public bool Claimed;
        /// <summary>A weekly challenge: no extra mission coins, its reward is the coins.</summary>
        public bool Weekly;
        public RewardKind Reward;
        public int Amount;

        public bool Done => Progress >= Goal;
        public string Text => DailyMissions.Describe(Kind, Goal);
        public string RewardText => DailyMissions.DescribeReward(Reward, Amount) + (Weekly ? "" : $" + 코인 {Club.MissionCoins}");
    }

    /// <summary>
    /// Real-calendar dailies: three missions a day and a seven-day check-in cycle. They live on the
    /// club, so every player slot feeds the same missions. Rewards never raise ratings: money for the
    /// active player, fatigue recovery tickets, or legacy points for the club.
    /// The game passes today's date in, so this stays testable and clock-free.
    /// </summary>
    public class DailyMissions
    {
        public const int PerDay = 3;
        /// <summary>Fatigue one recovery ticket takes off.</summary>
        public const int TicketRecovery = 30;

        public string Date = "";
        public List<Mission> Today = new List<Mission>();
        public string LastCheckIn = "";
        /// <summary>Days in a row checked in; the cycle reward is Streak's place in a week.</summary>
        public int Streak;
        public int TotalCheckIns;
        public int RecoveryTickets;
        /// <summary>Tickets used on Date; at most MaxTicketsPerDay, however many are held.</summary>
        public int TicketsUsedToday;
        public const int MaxTicketsPerDay = 5;
        public int TicketsLeftToday => Math.Max(0, MaxTicketsPerDay - TicketsUsedToday);

        /// <summary>Club coins for the check-in on a day of the week cycle (1-7): 20, 30, ... 80.</summary>
        public static int CheckInCoins(int streak) => 20 + 10 * ((streak - 1) % 7);

        /// <summary>This week's challenges (weeks start on Monday), shared by every slot like the dailies.</summary>
        public string WeekKey = "";
        public List<Mission> Week = new List<Mission>();
        public const int PerWeek = 4;

        /// <summary>The 30-day season pass, fed by missions, check-ins and weekly challenges.</summary>
        public SeasonPass Pass = new SeasonPass();

        /// <summary>The Monday of a date's week, as yyyy-MM-dd.</summary>
        public static string WeekOf(string date)
        {
            var day = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            int back = ((int)day.DayOfWeek + 6) % 7;
            return DateOf(day.AddDays(-back));
        }

        static readonly (RewardKind Kind, int Amount)[] CheckInRewards =
        {
            (RewardKind.Money, 500), (RewardKind.RecoveryTicket, 1), (RewardKind.Money, 1000), (RewardKind.RecoveryTicket, 2),
            (RewardKind.Money, 2000), (RewardKind.Legacy, 10), (RewardKind.Legacy, 30),
        };

        public static (RewardKind Kind, int Amount) CheckInReward(int streak) => CheckInRewards[(streak - 1) % CheckInRewards.Length];

        public static string DateOf(DateTime day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>Starts a new day's missions when the date has changed. True if it did.</summary>
        public bool Refresh(string today)
        {
            if (WeekOf(today) != WeekKey)
            {
                WeekKey = WeekOf(today);
                Week = GenerateWeek(WeekKey);
            }
            Pass.Refresh(today);
            if (Date == today) return false;
            Date = today;
            Today = Generate(today);
            TicketsUsedToday = 0;
            return true;
        }

        /// <summary>
        /// Today's check-in; null if already done today. Missing a day starts the week over.
        /// Reward is applied to the club, and money to the career if there is one.
        /// </summary>
        public (RewardKind Kind, int Amount)? CheckIn(string today, Club club, Career career)
        {
            if (LastCheckIn == today) return null;
            bool consecutive = LastCheckIn.Length > 0 &&
                DateTime.ParseExact(LastCheckIn, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1) ==
                DateTime.ParseExact(today, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Streak = consecutive ? Streak + 1 : 1;
            LastCheckIn = today;
            TotalCheckIns++;
            var reward = CheckInReward(Streak);
            Give(reward.Kind, reward.Amount, club, career);
            club.Coins += CheckInCoins(Streak);
            Pass.Refresh(today);
            Pass.Add(SeasonPass.CheckInPoints);
            return reward;
        }

        public bool CheckedInToday(string today) => LastCheckIn == today;

        /// <summary>Counts what a played day did toward today's missions.</summary>
        public void Record(DayResult day)
        {
            if (day.Action == DailyAction.Rest) Count(MissionKind.Rest);
            else Count(MissionKind.Train);
            if (day.MyGame != null || day.MyPitching != null || day.FarmLine != null || day.FarmPitching != null) Count(MissionKind.Games);
            int performance = (day.MyLine?.Hits ?? 0) + (day.FarmLine?.Hits ?? 0) +
                (day.MyPitching?.Strikeouts ?? 0) + (day.FarmPitching?.Strikeouts ?? 0);
            if (performance > 0) Count(MissionKind.Performance, performance);
        }

        public void Count(MissionKind kind, int amount = 1)
        {
            foreach (var m in Today.Concat(Week))
                if (m.Kind == kind && !m.Claimed) m.Progress = Math.Min(m.Goal, m.Progress + amount);
        }

        /// <summary>Collects a finished mission's reward; false if not done or already claimed.</summary>
        public bool Claim(int index, Club club, Career career)
        {
            if (index < 0 || index >= Today.Count) return false;
            var m = Today[index];
            if (!m.Done || m.Claimed) return false;
            // Money needs a player to hold it; between careers it turns into legacy points.
            if (m.Reward == RewardKind.Money && career == null) Give(RewardKind.Legacy, Math.Max(1, m.Amount / 100), club, null);
            else Give(m.Reward, m.Amount, club, career);
            club.Coins += Club.MissionCoins;
            m.Claimed = true;
            Pass.Add(SeasonPass.MissionPoints);
            Count(MissionKind.DailyDone);
            return true;
        }

        /// <summary>Collects a finished weekly challenge: its reward plus pass points.</summary>
        public bool ClaimWeekly(int index, Club club, Career career)
        {
            if (index < 0 || index >= Week.Count) return false;
            var m = Week[index];
            if (!m.Done || m.Claimed) return false;
            Give(m.Reward, m.Amount, club, career);
            m.Claimed = true;
            Pass.Add(SeasonPass.WeeklyPoints);
            return true;
        }

        public int UnclaimedWeekly => Week.Count(m => m.Done && !m.Claimed);

        public bool UseTicket(Career career)
        {
            if (RecoveryTickets <= 0 || TicketsLeftToday <= 0 || career == null || career.Me.Fatigue == 0) return false;
            RecoveryTickets--;
            TicketsUsedToday++;
            career.RecoverFatigue(TicketRecovery);
            return true;
        }

        public int Unclaimed => Today.Count(m => m.Done && !m.Claimed);

        void Give(RewardKind kind, int amount, Club club, Career career)
        {
            switch (kind)
            {
                case RewardKind.Money:
                    if (career != null) career.Me.Money += amount;
                    else club.LegacyPoints += Math.Max(1, amount / 100);
                    break;
                case RewardKind.RecoveryTicket: RecoveryTickets += amount; break;
                case RewardKind.Coins: club.Coins += amount; break;
                default: club.LegacyPoints += amount; break;
            }
        }

        static readonly (MissionKind Kind, int Goal, RewardKind Reward, int Amount)[] Pool =
        {
            (MissionKind.Train, 5, RewardKind.Money, 800),
            (MissionKind.Train, 10, RewardKind.RecoveryTicket, 1),
            (MissionKind.Games, 3, RewardKind.Money, 1000),
            (MissionKind.Games, 6, RewardKind.Legacy, 5),
            (MissionKind.Performance, 3, RewardKind.Money, 1200),
            (MissionKind.Performance, 8, RewardKind.Legacy, 8),
            (MissionKind.Rest, 2, RewardKind.RecoveryTicket, 1),
            (MissionKind.BigMoment, 1, RewardKind.Money, 1500),
            (MissionKind.WatchGame, 1, RewardKind.RecoveryTicket, 1),
        };

        static readonly (MissionKind Kind, int Goal, RewardKind Reward, int Amount)[] WeekPool =
        {
            (MissionKind.Train, 40, RewardKind.Coins, 200),
            (MissionKind.Games, 20, RewardKind.Coins, 200),
            (MissionKind.Performance, 40, RewardKind.Coins, 250),
            (MissionKind.Rest, 10, RewardKind.RecoveryTicket, 2),
            (MissionKind.BigMoment, 5, RewardKind.Coins, 200),
            (MissionKind.WatchGame, 3, RewardKind.RecoveryTicket, 1),
            (MissionKind.DailyDone, 12, RewardKind.Coins, 300),
        };

        /// <summary>Four challenges of different kinds for a week, the same for everyone.</summary>
        public static List<Mission> GenerateWeek(string weekKey)
        {
            int seed = weekKey.Aggregate(31, (h, ch) => unchecked(h * 37 + ch));
            var random = new Random(seed);
            return WeekPool.OrderBy(_ => random.Next()).Take(PerWeek)
                .Select(p => new Mission { Kind = p.Kind, Goal = p.Goal, Reward = p.Reward, Amount = p.Amount, Weekly = true })
                .ToList();
        }

        /// <summary>Three missions of different kinds, the same for everyone on a given date.</summary>
        public static List<Mission> Generate(string date)
        {
            int seed = date.Aggregate(17, (h, ch) => unchecked(h * 31 + ch));
            var random = new Random(seed);
            var picks = new List<Mission>();
            foreach (var p in Pool.OrderBy(_ => random.Next()))
            {
                if (picks.Any(m => m.Kind == p.Kind)) continue;
                picks.Add(new Mission { Kind = p.Kind, Goal = p.Goal, Reward = p.Reward, Amount = p.Amount });
                if (picks.Count == PerDay) break;
            }
            return picks;
        }

        public static string Describe(MissionKind kind, int goal)
        {
            switch (kind)
            {
                case MissionKind.Train: return $"훈련 {goal}번 하기";
                case MissionKind.Games: return $"경기 {goal}번 나가기";
                case MissionKind.Performance: return $"안타 또는 탈삼진 {goal}개";
                case MissionKind.Rest: return $"휴식 {goal}번 하기";
                case MissionKind.BigMoment: return goal == 1 ? "찬스 타석이나 위기 승부 한 번 맞이하기" : $"찬스 타석이나 위기 승부 {goal}번 맞이하기";
                case MissionKind.DailyDone: return $"오늘의 미션 {goal}개 완료하기";
                default: return goal == 1 ? "오늘 경기 직접 보기" : $"경기 직접 보기 {goal}번";
            }
        }

        public static string DescribeReward(RewardKind kind, int amount)
        {
            switch (kind)
            {
                case RewardKind.Money: return $"{amount:N0}만원";
                case RewardKind.RecoveryTicket: return $"피로 회복권 {amount}장";
                case RewardKind.Coins: return $"코인 {amount}";
                default: return $"명예 포인트 {amount}";
            }
        }
    }
}
