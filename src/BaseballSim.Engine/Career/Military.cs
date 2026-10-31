using System;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// Military service: one season with the armed forces club, playing 2군-level games and still
    /// training, but no pay and no 1군. It must start by the season the player turns 28 unless he
    /// won tournament gold, and he may go earlier, for example while stuck in 2군.
    /// </summary>
    public partial class Career
    {
        public const string MilitaryClub = "국군 야구단";
        public const int EnlistByAge = 28;
        const int EarliestEnlistAge = 20;

        /// <summary>Served, or exempted by gold.</summary>
        public bool MilitaryDone;
        /// <summary>Serving this season.</summary>
        public bool InMilitary;

        /// <summary>Between seasons, the player can choose to serve next season rather than wait.</summary>
        public bool CanEnlist => Day == 0 && !MilitaryDone && !InMilitary && !Released && Me.Age >= EarliestEnlistAge && Me.Age < EnlistByAge;

        public void Enlist()
        {
            if (!CanEnlist) throw new InvalidOperationException("Can't enlist now.");
            StartService();
        }

        void StartService()
        {
            InMilitary = true;
            InFarm = true;
            Goal = null;
            // Free agency and salary talks wait until he's back.
            PendingOffers.Clear();
            PendingSalary = null;
        }

        /// <summary>
        /// End of season: a finished service ends; then anyone who hasn't served or won gold and is about
        /// to turn 28 goes in. Returns whether service starts next season.
        /// </summary>
        bool SettleMilitary(SeasonSummary summary, bool served)
        {
            if (served)
            {
                InMilitary = false;
                InFarm = false;
                MilitaryDone = true;
                summary.Record.Military = true;
                summary.Record.Notes.Add($"{MilitaryClub} 복무 마침");
            }
            if (MilitaryDone || Released || Me.Age < EnlistByAge) return false;
            StartService();
            summary.Record.Notes.Add("입대 영장");
            return true;
        }

        /// <summary>Seasons that count toward free agency: service seasons don't.</summary>
        int SeasonsPlayed => Me.History.Count(h => !h.Military);

        void ExemptByGold()
        {
            if (MilitaryDone) return;
            MilitaryDone = true;
            SeasonNotes.Add("금메달로 병역 면제");
        }
    }
}
