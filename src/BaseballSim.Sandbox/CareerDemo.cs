using BaseballSim.Career;

namespace BaseballSim.Sandbox
{
    /// <summary>
    /// Plays a whole career with a simple autopilot standing in for the user, so growth speed,
    /// money and injuries can be checked season by season.
    /// </summary>
    public static class CareerDemo
    {
        public static void Run(int seed, int gamesPerSeason)
        {
            var random = new SeededRandom(seed);
            var club = new Club();

            for (int generation = 1; generation <= 2; generation++)
            {
                var league = new League(new RosterGenerator(random), gamesPerSeason);
                var me = Career.Career.NewPlayer(generation == 1 ? "강하늘" : "윤바다", random, club.PotentialBonus);
                // From the second player on, the best retired player coaches.
                var career = new Career.Career(me, league, league.Teams[0], random)
                {
                    Coach = club.Roster.OrderByDescending(p => p.Overall).FirstOrDefault(),
                };

                Console.WriteLine($"=== {generation}대 선수 {me.Name} ({league.Teams[0].Name}, 시즌 {gamesPerSeason}경기) ===");
                Console.WriteLine($"시작 능력치  {Ratings(me.Ratings)}");
                Console.WriteLine($"잠재력       {Ratings(me.Potential)}   (명예 포인트 보너스 +{club.PotentialBonus})");
                if (career.Coach != null) Console.WriteLine($"코치         {career.Coach.Name} (종합 {career.Coach.Overall})");
                Console.WriteLine();
                Console.WriteLine("시즌 나이  컨택 파워 선구 주루 수비 | 타율   홈런 OPS   | 연봉(만원) 보유금 부상일 이벤트 | 장비");

                while (true)
                {
                    int injuredDays = 0, events = 0;
                    while (!career.IsSeasonOver)
                    {
                        var day = career.PlayDay(Choose(me));
                        if (day.SatOut) injuredDays++;
                        if (day.Event != null)
                        {
                            // Autopilot answers story events at random.
                            career.ResolveEvent((int)(random.NextDouble() * day.Event.Choices.Count));
                            events++;
                        }
                        BuyUpgrades(career);
                    }

                    var l = career.SeasonLine;
                    var r = me.Ratings;
                    string avg = l.AtBats == 0 ? "-" : ((double)l.Hits / l.AtBats).ToString("F3");
                    string gear = string.Join(" ", me.Equipped.Values.OrderBy(e => e.Slot).Select(e => e.Id))
                        + "  시설 " + string.Join("", Facilities.All.Select(f => me.FacilityLevel(f)));
                    string row =
                        $"{career.Season,3}  {me.Age,3}   {r[Stat.Contact],3}  {r[Stat.Power],3}  {r[Stat.Eye],3}  {r[Stat.Speed],3}  {r[Stat.Defense],3} | " +
                        $"{avg}  {l.HomeRuns,3}  {Career.Career.Ops(l):F3} | {me.Salary,8}  {me.Money,6}  {injuredDays,4}  {events,4} | {gear}";
                    var summary = career.EndSeason();
                    Console.WriteLine(row + (summary.Record.Titles.Count > 0 ? "  " + string.Join(", ", summary.Record.Titles) : ""));

                    // Retire at 37 to keep the demo short, or when forced.
                    if (me.Age >= 38 || career.MustRetire) break;
                }

                var retired = club.Retire(career);
                var t = retired.CareerLine;
                Console.WriteLine();
                Console.WriteLine($"은퇴: {retired.Name}, {retired.Seasons}시즌, 통산 {t.Hits}안타 {t.HomeRuns}홈런 타율 {(double)t.Hits / t.AtBats:F3}");
                Console.WriteLine($"내 팀 합류, 전성기 능력치 {Ratings(retired.PeakRatings)} (종합 {retired.Overall}), 명예 포인트 +{retired.LegacyPoints}");
                Console.WriteLine($"업적 {retired.Achievements.Count}/{Achievements.All.Count}: {string.Join(", ", retired.Achievements.Select(id => Achievements.Find(id).Name))}");
                Console.WriteLine();
            }
        }

        // Autopilot: rest when tired, otherwise train the stat with the most room left to grow.
        static DailyAction Choose(CareerPlayer me)
        {
            if (me.IsInjured || me.Fatigue >= 65) return DailyAction.Rest;
            var stat = CareerPlayer.AllStats.OrderByDescending(s => me.Potential[s] - me.Ratings[s]).First();
            return stat switch
            {
                Stat.Contact => DailyAction.TrainContact,
                Stat.Power => DailyAction.TrainPower,
                Stat.Eye => DailyAction.TrainEye,
                Stat.Speed => DailyAction.TrainSpeed,
                _ => DailyAction.TrainDefense,
            };
        }

        // Autopilot: buy the next tier in any slot it can afford, then facilities once gear is maxed.
        static void BuyUpgrades(Career.Career career)
        {
            if (career.Me.Equipped.Count == 5 && career.Me.Equipped.Values.All(e => e.Price == EquipmentCatalog.All.Max(x => x.Price)))
                foreach (var f in Facilities.All.OrderBy(f => career.Me.FacilityLevel(f)))
                    career.UpgradeFacility(f);

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                career.Me.Equipped.TryGetValue(slot, out var current);
                var next = EquipmentCatalog.All
                    .Where(e => e.Slot == slot && (current == null || e.Price > current.Price))
                    .OrderBy(e => e.Price)
                    .FirstOrDefault();
                if (next != null) career.Buy(next.Id);
            }
        }

        static string Ratings(Dictionary<Stat, int> r) =>
            $"컨택 {r[Stat.Contact]} 파워 {r[Stat.Power]} 선구 {r[Stat.Eye]} 주루 {r[Stat.Speed]} 수비 {r[Stat.Defense]}";
    }
}
