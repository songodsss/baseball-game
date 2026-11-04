using System.Collections.Generic;

namespace BaseballSim.Career
{
    public partial class Career
    {
        /// <summary>The tournament under way: its seed (0 = none), Korea's games let through, play-the-rest, and my answers.</summary>
        public int TourSeed;
        public int TourGames;
        public bool TourAuto;
        public List<Approach> TourDecisions = new List<Approach>();

        NationalTeam.Run _tourRun;

        /// <summary>Picked for Korea and the tournament isn't over: the league waits.</summary>
        public bool InTournament => TourSeed != 0;

        public NationalTeam.Run TournamentNow =>
            TourSeed == 0 ? null : _tourRun ?? (_tourRun = NationalTeam.Play(this, TourSeed, TourDecisions, TourGames, TourAuto));

        /// <summary>Korea's next game: finished, or paused at my big at bat (answer with <see cref="ChooseApproach"/>).</summary>
        public DayResult NextTournamentGame()
        {
            var now = TournamentNow;
            if (now.Done || now.Current?.AtBat != null) return now.Current;
            TourGames++;
            _tourRun = null;
            return TournamentNow.Current;
        }

        /// <summary>Plays out the rest of the tournament, my big at bats the usual way.</summary>
        public void AutoTournament()
        {
            TourAuto = true;
            _tourRun = null;
        }

        DayResult AnswerTournament(Approach approach)
        {
            TourDecisions.Add(approach);
            _tourRun = null;
            return TournamentNow.Current;
        }

        /// <summary>The next national-team scene due now (picked, the squad, the final's eve, the medal), made the pending event.</summary>
        public StoryEvent TournamentScene()
        {
            if (!InTournament || PendingEvent != null) return PendingEvent;
            var run = TournamentNow;
            if (run.Current?.AtBat != null) return null;
            PendingEvent = NationalStory.Next(this, run);
            return PendingEvent;
        }

        /// <summary>Ends the tournament once it's played out: rewards, the medal's exemption, the season note.</summary>
        public TournamentResult FinishTournament()
        {
            var run = NationalTeam.Play(this, TourSeed, TourDecisions, TourGames, true);
            var t = run.Result;
            NationalTeam.Reward(this, t);
            if (t.Selected && t.Medal == NationalTeam.Gold) { ExemptByGold(); AddFans(300); }
            if (t.Selected) SeasonNotes.Add($"{MonthText} {NationalTeam.Name} " + (t.Medal ?? "대표 출전"));
            TourSeed = 0;
            TourGames = 0;
            TourAuto = false;
            TourDecisions.Clear();
            _tourRun = null;
            return t;
        }
    }
}
