using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public partial class Career
    {
        /// <summary>The autumn under way: its seed (0 = not started), my club's games let through, and my answers so far.</summary>
        public int PostSeed;
        public int PostGames;
        public List<Approach> PostDecisions = new List<Approach>();

        Postseason.Run _postRun;

        /// <summary>The regular season is over and the postseason has been started but not settled by EndSeason.</summary>
        public bool InPostseason => IsSeasonOver && PostSeed != 0;

        /// <summary>Where the postseason stands now (starts it if needed).</summary>
        public Postseason.Run PostseasonNow
        {
            get
            {
                if (PostSeed == 0) StartPostseason();
                return _postRun ?? (_postRun = Postseason.Play(this, PostSeed, PostDecisions, PostGames, false));
            }
        }

        /// <summary>Starts the postseason once the regular season is over: nothing is played until <see cref="NextPostseasonGame"/>.</summary>
        public Postseason.Run StartPostseason()
        {
            if (PostSeed == 0)
            {
                PostSeed = 1 + (int)(_random.NextDouble() * (int.MaxValue - 1));
                PostGames = 0;
                PostDecisions.Clear();
                _postRun = null;
            }
            return PostseasonNow;
        }

        /// <summary>Plays my club's next postseason game: finished, or paused at a big at bat (answer with <see cref="ChooseApproach"/>).</summary>
        public DayResult NextPostseasonGame()
        {
            var now = PostseasonNow;
            if (now.Done || now.Current?.AtBat != null) return now.Current;
            PostGames++;
            _postRun = null;
            return PostseasonNow.Current;
        }

        DayResult AnswerPostseason(Approach approach)
        {
            PostDecisions.Add(approach);
            _postRun = null;
            return PostseasonNow.Current;
        }

        /// <summary>Plays out whatever is left (big at bats the usual way) and returns the final postseason.</summary>
        PostseasonResult FinishPostseason()
        {
            if (PostSeed == 0) StartPostseason();
            var run = Postseason.Play(this, PostSeed, PostDecisions, PostGames, true);
            PostSeed = 0;
            PostGames = 0;
            PostDecisions.Clear();
            _postRun = null;
            return run.Result;
        }
    }
}
