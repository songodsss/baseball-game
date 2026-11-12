using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum NewsKind { Me, Rival, League }

    /// <summary>One headline in the league paper.</summary>
    public class NewsItem
    {
        public int Season;
        public int Day;
        public NewsKind Kind;
        public string Headline;
    }

    /// <summary>
    /// The league paper: a few headlines a day about my player, the rival, the standings and the
    /// leaderboards, newest first. Only the headline is kept, so it costs little to save.
    /// </summary>
    public partial class Career
    {
        public const string PaperName = "베이스볼 데일리";
        public const int MaxNews = 60;
        const int NewsPerDay = 3;
        const int LeagueNewsEvery = 7;

        public List<NewsItem> News = new List<NewsItem>();

        /// <summary>Headlines from today, most important first; at most three make the paper.</summary>
        void WriteNews(DayResult result)
        {
            var today = new List<(int Priority, NewsKind Kind, string Headline)>();
            void Add(int priority, NewsKind kind, string headline) => today.Add((priority, kind, headline));
            string me = Me.Name;

            if (Day == 1) Add(9, NewsKind.League, $"{Year} 시즌 개막! {MyTeam.Name} {me}, 새 시즌 출발");
            foreach (var m in result.Milestones) Add(10, NewsKind.Me, $"{me}, {m} 달성!");
            if (result.GotInjured) Add(8, NewsKind.Me, $"{me}, {Injuries.Name(result.Injury)} 부상으로 이탈… {Me.InjuryDaysLeft}일 결장 예상");
            if (result.Move != null) Add(9, NewsKind.Me, result.Move.Text);

            var line = result.MyLine;
            if (line != null && !result.InFarm)
            {
                if (line.HomeRuns >= 2) Add(8, NewsKind.Me, $"{me}, 한 경기 홈런 {line.HomeRuns}방! 시즌 {SeasonLine.HomeRuns}호");
                else if (line.HomeRuns == 1) Add(6, NewsKind.Me, $"{me}, 시즌 {SeasonLine.HomeRuns}호 홈런 폭발");
                if (line.Hits >= 3) Add(6, NewsKind.Me, $"{me}, {line.AtBats}타수 {line.Hits}안타 맹타");
                if (line.StolenBases > 0) Add(4, NewsKind.Me, $"{me}, 빠른 발로 시즌 {SeasonLine.StolenBases}번째 도루");
                if (line.RunsBattedIn >= 4) Add(7, NewsKind.Me, $"{me}, 혼자 {line.RunsBattedIn}타점 쓸어 담아");
            }
            var arm = result.MyPitching;
            if (arm != null && !result.InFarm)
            {
                var p = SeasonPitching;
                if (arm.Shutouts > 0) Add(9, NewsKind.Me, $"{me}, 완봉승! 상대 타선 꽁꽁");
                else if (arm.Wins > 0 && arm.Outs >= 21 && arm.Runs <= 1) Add(7, NewsKind.Me, $"{me}, {arm.Innings} 호투로 시즌 {p.Wins}승");
                else if (arm.Wins > 0) Add(5, NewsKind.Me, $"{me}, 시즌 {p.Wins}승째");
                if (arm.Saves > 0) Add(6, NewsKind.Me, $"수호신 {me}, 시즌 {p.Saves}세이브");
                if (arm.Holds > 0) Add(4, NewsKind.Me, $"{me}, 리드 지켜내며 {p.Holds}홀드");
                if (arm.BlownSaves > 0) Add(5, NewsKind.Me, $"{me}, 블론세이브… 아쉬운 9회");
                if (arm.Strikeouts >= 10) Add(7, NewsKind.Me, $"{me}, 탈삼진 {arm.Strikeouts}개 쇼");
            }

            if (Rival != null && result.RivalLine != null)
            {
                var r = result.RivalLine;
                if (r.HomeRuns > 0) Add(5, NewsKind.Rival, $"라이벌 {Rival.Name}, 우리 팀 상대로 홈런포");
                else if (r.Hits >= 3) Add(4, NewsKind.Rival, $"라이벌 {Rival.Name}, {r.Hits}안타로 맞불");
            }

            if (Day % LeagueNewsEvery == 0 && !IsSeasonOver)
            {
                var ranked = League.Ranked();
                var top = ranked[0];
                int rank = ranked.FindIndex(s => s.Team == MyTeam) + 1;
                Add(rank == 1 ? 6 : 3, NewsKind.League, rank == 1
                    ? $"{MyTeam.Name}, {top.Wins}승 {top.Losses}패로 선두 질주"
                    : $"{top.Team.Name} 선두({top.Wins}승 {top.Losses}패)… {MyTeam.Name}{Topic(MyTeam.Name)} {rank}위");

                var qualified = League.Batting.Values.Where(b => b.Line.PlateAppearances >= League.QualifyingPa * Day / League.GamesPerSeason).ToList();
                var slugger = League.Batting.Values.OrderByDescending(b => b.Line.HomeRuns).FirstOrDefault();
                if (slugger != null && slugger.Line.HomeRuns > 0)
                    Add(slugger.Name == me ? 7 : 2, slugger.Name == me ? NewsKind.Me : NewsKind.League, $"홈런 1위 {slugger.Name}({slugger.Team.Name}) {slugger.Line.HomeRuns}개");
                var hitter = qualified.Where(b => b.Line.AtBats > 0).OrderByDescending(b => (double)b.Line.Hits / b.Line.AtBats).FirstOrDefault();
                if (hitter != null)
                    Add(hitter.Name == me ? 7 : 2, hitter.Name == me ? NewsKind.Me : NewsKind.League, $"타율 1위 {hitter.Name} {(double)hitter.Line.Hits / hitter.Line.AtBats:.000}");
            }

            foreach (var item in today.OrderByDescending(t => t.Priority).Take(NewsPerDay))
                News.Insert(0, new NewsItem { Season = Season, Day = Day, Kind = item.Kind, Headline = item.Headline });
            if (News.Count > MaxNews) News.RemoveRange(MaxNews, News.Count - MaxNews);
        }

        /// <summary>은 after a closing consonant, 는 after a vowel: "웨일스는", "시호크스는", "나이츠는".</summary>
        static string Topic(string word)
        {
            char last = word.Length == 0 ? ' ' : word[word.Length - 1];
            bool batchim = last >= '가' && last <= '힣' && (last - '가') % 28 != 0;
            return batchim ? "은" : "는";
        }

        /// <summary>A season-end story so the paper closes the year.</summary>
        void WriteSeasonNews(SeasonSummary summary)
        {
            string titles = summary.Record.Titles.Count > 0 ? $" {string.Join(", ", summary.Record.Titles)}" : "";
            News.Insert(0, new NewsItem
            {
                Season = summary.Record.Season,
                Day = Day,
                Kind = NewsKind.Me,
                Headline = $"{YearOf(summary.Record.Season)} 시즌 결산: {Me.Name}{titles}",
            });
            if (News.Count > MaxNews) News.RemoveRange(MaxNews, News.Count - MaxNews);
        }
    }
}
