namespace BaseballSim.Career
{
    /// <summary>One month's pay: the salary share plus the incentives earned that month.</summary>
    public class Payday
    {
        public string Month;
        public int Salary;
        public int Bonus;
        public int Total => Salary + Bonus;
    }

    /// <summary>
    /// Like a real club, salary is paid monthly through the season (April to September, six paydays)
    /// rather than per game. Performance incentives build up day by day and come with the next payday.
    /// A soldier gets a small allowance instead.
    /// </summary>
    public partial class Career
    {
        public const int Paydays = 6;
        const int MilitaryAllowancePerGame = 20;

        /// <summary>Incentives earned since the last payday.</summary>
        public int PendingBonus;
        /// <summary>Paydays already made this season, 0 to 6.</summary>
        public int PaidMonths;

        /// <summary>Months of the season fully played so far.</summary>
        int MonthsPlayed => Day * Paydays / League.GamesPerSeason;

        void PayMonths(DayResult result)
        {
            while (PaidMonths < MonthsPlayed)
            {
                PaidMonths++;
                int salary = InMilitary ? MilitaryAllowancePerGame * League.GamesPerSeason / Paydays
                    // The last payday rounds off so the year adds up to the contract exactly.
                    : PaidMonths == Paydays ? Me.Salary - (Paydays - 1) * (Me.Salary / Paydays) : Me.Salary / Paydays;
                var pay = new Payday { Month = $"{3 + PaidMonths}월", Salary = salary, Bonus = PendingBonus };
                PendingBonus = 0;
                Me.Money += pay.Total;
                result.MoneyEarned += pay.Total;
                result.Payday = pay;
            }
        }
    }
}
