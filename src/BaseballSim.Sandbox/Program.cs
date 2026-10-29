using BaseballSim;
using BaseballSim.Sandbox;

// Sandbox: plays whole seasons with no graphics to check the engine's numbers.
// Usage: dotnet run -- [seed]          league numbers, rating impact, a sample game
//        dotnet run -- career [seed]   auto-plays one full career to check growth pacing
if (args.Length > 0 && args[0] == "career")
{
    CareerDemo.Run(args.Length > 1 ? int.Parse(args[1]) : 42);
    return;
}

int seed = args.Length > 0 ? int.Parse(args[0]) : 42;
var random = new SeededRandom(seed);
var generator = new RosterGenerator(random);
var sim = new GameSimulator(random);

string[] teamNames =
{
    "서울 블루윙스", "부산 마린스타즈", "인천 시호크스", "대구 썬더볼츠", "광주 타이거릴리",
    "대전 이글아이즈", "수원 나이츠", "창원 다이노스톰", "고양 레드폭스", "울산 웨일스",
};

PrintLeagueSeason();
PrintRatingImpact();
PrintSampleGame();

void PrintLeagueSeason()
{
    var teams = teamNames.Select(generator.CreateTeam).ToList();
    var results = PlaySeason(teams);

    var bat = Sum(results.SelectMany(r => r.Batting.Values));
    int games = results.Count;
    int pa = bat.PlateAppearances;
    double obp = (double)(bat.Hits + bat.Walks) / pa;
    double slg = (double)(bat.Hits + bat.Doubles + 2 * bat.Triples + 3 * bat.HomeRuns) / bat.AtBats;

    Console.WriteLine($"=== 리그 한 시즌 ({games}경기, 팀당 144경기) ===");
    Console.WriteLine($"팀당 경기당 득점  {results.Average(r => (r.AwayScore + r.HomeScore) / 2.0):F2}   (KBO 최근 약 4.5~5.5)");
    Console.WriteLine($"리그 타율         {(double)bat.Hits / bat.AtBats:F3}  (약 .260~.280)");
    Console.WriteLine($"출루율 / 장타율   {obp:F3} / {slg:F3}");
    Console.WriteLine($"팀당 경기당 홈런  {bat.HomeRuns / (games * 2.0):F2}   (약 0.7~1.0)");
    Console.WriteLine($"삼진율 / 볼넷율   {100.0 * bat.Strikeouts / pa:F1}% / {100.0 * bat.Walks / pa:F1}%");
    Console.WriteLine($"무승부            {100.0 * results.Count(r => r.IsTie) / games:F1}%");
    Console.WriteLine($"연장 경기         {100.0 * results.Count(r => r.Innings > 9) / games:F1}%");
    Console.WriteLine();

    Console.WriteLine("순위");
    var standings = teams
        .Select(t => new
        {
            Team = t,
            W = results.Count(r => r.Winner == t),
            L = results.Count(r => !r.IsTie && r.Winner != t && (r.Home == t || r.Away == t)),
            T = results.Count(r => r.IsTie && (r.Home == t || r.Away == t)),
        })
        .OrderByDescending(s => (double)s.W / Math.Max(1, s.W + s.L));
    int rank = 1;
    foreach (var s in standings)
        Console.WriteLine($"{rank++,2}. {s.Team.Name,-10} {s.W}승 {s.L}패 {s.T}무  ({(double)s.W / (s.W + s.L):F3})");
    Console.WriteLine();
}

// How much one rating changes a full-time player's season, averaged over many seasons.
void PrintRatingImpact()
{
    const int seasons = 30;
    Console.WriteLine($"=== 능력치가 시즌 성적에 주는 영향 (주전 타자, {seasons}시즌 평균) ===");
    Console.WriteLine("능력치(나머지 50)      타율   출루율  홈런  삼진  볼넷");
    var cases = new (string Label, Action<BattingRatings> Apply)[]
    {
        ("전부 50 (평균)", r => { }),
        ("파워 80", r => r.Power = 80),
        ("파워 95", r => r.Power = 95),
        ("컨택 80", r => r.Contact = 80),
        ("선구안 80", r => r.Eye = 80),
        ("전부 30 (신인급)", r => { r.Contact = 30; r.Power = 30; r.Eye = 30; r.Speed = 30; }),
        ("전부 80 (스타급)", r => { r.Contact = 80; r.Power = 80; r.Eye = 80; r.Speed = 80; }),
    };

    foreach (var c in cases)
    {
        var lines = new List<BattingLine>();
        for (int s = 0; s < seasons; s++)
        {
            var teams = teamNames.Select(generator.CreateTeam).ToList();
            var me = new Player("me", "나");
            c.Apply(me.Batting);
            teams[0].Lineup[3] = me; // cleanup hitter
            var results = PlaySeason(teams);
            lines.Add(Sum(results.Where(r => r.Batting.ContainsKey(me)).Select(r => r.Batting[me])));
        }
        var t = Sum(lines);
        Console.WriteLine($"{c.Label,-18} {(double)t.Hits / t.AtBats:F3}  {(double)(t.Hits + t.Walks) / t.PlateAppearances:F3}  " +
            $"{(double)t.HomeRuns / seasons,4:F1}  {(double)t.Strikeouts / seasons,4:F0}  {(double)t.Walks / seasons,4:F0}");
    }
    Console.WriteLine();
}

void PrintSampleGame()
{
    var away = generator.CreateTeam(teamNames[0]);
    var home = generator.CreateTeam(teamNames[1]);
    var result = sim.Play(away, home, away.Rotation[0], home.Rotation[0]);

    Console.WriteLine($"=== 샘플 경기: {away.Name} vs {home.Name} ===");
    string lastHalf = "";
    foreach (var ev in result.Events)
    {
        string half = Commentary.HalfInning(ev.Inning, ev.IsTopHalf);
        if (half != lastHalf)
        {
            Console.WriteLine($"\n[{half}]");
            lastHalf = half;
        }
        Console.WriteLine($"  {Commentary.Describe(ev)}  ({ev.AwayScore}:{ev.HomeScore}, {ev.OutsAfter}아웃)");
    }
    Console.WriteLine($"\n최종 {away.Name} {result.AwayScore} : {result.HomeScore} {home.Name}");
}

List<GameResult> PlaySeason(List<Team> teams)
{
    // 16 games against each of the 9 others = 144 games each, starters in a 5-man rotation.
    var results = new List<GameResult>();
    var starterIndex = teams.ToDictionary(t => t, t => 0);
    for (int round = 0; round < 8; round++)
        for (int i = 0; i < teams.Count; i++)
            for (int j = 0; j < teams.Count; j++)
            {
                if (i == j) continue;
                var away = teams[i];
                var home = teams[j];
                var awayStarter = away.Rotation[starterIndex[away]++ % away.Rotation.Count];
                var homeStarter = home.Rotation[starterIndex[home]++ % home.Rotation.Count];
                results.Add(sim.Play(away, home, awayStarter, homeStarter));
            }
    return results;
}

static BattingLine Sum(IEnumerable<BattingLine> lines)
{
    var t = new BattingLine();
    foreach (var l in lines)
    {
        t.PlateAppearances += l.PlateAppearances; t.AtBats += l.AtBats; t.Hits += l.Hits;
        t.Doubles += l.Doubles; t.Triples += l.Triples; t.HomeRuns += l.HomeRuns;
        t.Walks += l.Walks; t.Strikeouts += l.Strikeouts; t.Runs += l.Runs; t.RunsBattedIn += l.RunsBattedIn;
    }
    return t;
}
