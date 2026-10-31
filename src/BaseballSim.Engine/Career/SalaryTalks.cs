using System;
using System.Linq;

namespace BaseballSim.Career
{
    public enum SalaryAsk
    {
        Accept,
        /// <summary>About 10% more: usually granted when the season was better than the offer.</summary>
        AskSome,
        /// <summary>About 25% more: a gamble that pays off after a big year.</summary>
        AskBig,
    }

    /// <summary>The club's salary offer after a season, answered with <see cref="Career.Negotiate"/>.</summary>
    public class SalaryTalk
    {
        public int OldSalary;
        /// <summary>The club's number; already in effect if the player simply accepts or never answers.</summary>
        public int Offer;
        /// <summary>What this season's OPS is worth on the market. Above the offer means leverage.</summary>
        public int Worth;
        public int Titles;

        public int Ask(SalaryAsk ask) => ask == SalaryAsk.AskBig ? Round(Offer * 1.25) : ask == SalaryAsk.AskSome ? Round(Offer * 1.1) : Offer;

        /// <summary>Chance the club says yes. A season worth more than the offer, and titles, give leverage.</summary>
        public double Chance(SalaryAsk ask)
        {
            double leverage = (double)Worth / Offer - 1 + 0.1 * Titles;
            switch (ask)
            {
                case SalaryAsk.AskSome: return Clamp(0.6 + leverage, 0.15, 0.95);
                case SalaryAsk.AskBig: return Clamp(0.25 + 1.5 * leverage, 0.05, 0.9);
                default: return 1;
            }
        }

        static int Round(double salary) => Math.Min(Career.MaxSalary, (int)(salary / 100) * 100);
        static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;
    }

    public class SalaryResult
    {
        public bool Granted;
        public int Salary;
        public int MoraleChange;
        public string Text;
    }

    public partial class Career
    {
        /// <summary>Waiting for an answer before the next season; set by EndSeason in a year without a deal, FA or release.</summary>
        public SalaryTalk PendingSalary;

        SalaryTalk OpenSalaryTalk(SeasonSummary summary, double worth)
        {
            if (Released || PendingOffers.Count > 0 || ContractSeasonsLeft > 0 || MustRetire) return null;
            return new SalaryTalk
            {
                OldSalary = summary.OldSalary,
                Offer = Me.Salary,
                Worth = Math.Max(MinSalary, Math.Min(MaxSalary, (int)(worth / 100) * 100)),
                Titles = summary.Record.Titles.Count,
            };
        }

        public SalaryResult Negotiate(SalaryAsk ask)
        {
            var talk = PendingSalary ?? throw new InvalidOperationException("No salary offer to answer.");
            PendingSalary = null;
            if (ask == SalaryAsk.Accept)
                return new SalaryResult { Granted = true, Salary = Me.Salary, Text = $"{Me.Salary:N0}만원에 도장을 찍었어요." };

            int asked = talk.Ask(ask);
            if (_random.NextDouble() < talk.Chance(ask))
            {
                Me.Salary = asked;
                return new SalaryResult { Granted = true, Salary = asked, Text = $"구단이 요구를 받아들였어요! 연봉 {asked:N0}만원." };
            }

            int morale = ask == SalaryAsk.AskBig ? -3 : -1;
            Me.Morale = Math.Max(-CareerPlayer.MaxMorale, Me.Morale + morale);
            string text = ask == SalaryAsk.AskBig
                ? $"협상이 길어졌지만 구단은 끝내 {talk.Offer:N0}만원을 고수했어요. 마음이 상했어요."
                : $"구단이 원래 제시액 {talk.Offer:N0}만원을 고수했어요.";
            return new SalaryResult { Granted = false, Salary = Me.Salary, MoraleChange = morale, Text = text };
        }
    }
}
