using System.Collections.Generic;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>The rest of the season by month: every game left, and the big days among them.</summary>
    public class ScheduleScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Today = new Color(0.25f, 0.55f, 0.95f, 0.35f);
        static readonly Color Special = new Color(1f, 0.82f, 0.3f, 0.16f);

        CareerState _career;

        public static void Open(CareerState career)
        {
            var screen = new GameObject("ScheduleScreen").AddComponent<ScheduleScreen>();
            screen._career = career;
            screen.Build();
        }

        void Build()
        {
            int games = _career.League.GamesPerSeason;
            var canvas = UiKit.Canvas(transform, "ScheduleCanvas", 5);
            var root = UiKit.SafeRoot(canvas.transform, UiKit.Background);
            UiKit.Column(root, 40, 18);
            UiKit.Height(UiKit.Label(root, $"{_career.Year} 시즌 일정", 48), 80);
            UiKit.Height(UiKit.Label(root, $"{_career.Day}/{games}경기 진행 · 남은 경기 {games - _career.Day}", 28, TextAnchor.MiddleLeft, UiKit.Muted), 50);

            var keyDays = KeyDays();
            var chips = UiKit.Empty(root, "KeyDays");
            UiKit.Height(chips, 120);
            UiKit.Row(chips, 12);
            foreach (var pair in keyDays)
            {
                int left = pair.Key - _career.Day;
                var chip = UiKit.Panel(chips, "Chip", left >= 0 ? UiKit.Shade(UiKit.Card, 1.35f) : UiKit.Background);
                var text = UiKit.Label(chip, $"<b>{pair.Value}</b>\n<size=24>{(left > 0 ? $"D-{left}" : left == 0 ? "오늘" : "지남")}</size>", 26, TextAnchor.MiddleCenter,
                    left >= 0 ? Gold : UiKit.Muted);
                UiKit.Stretch(text.rectTransform);
            }

            var body = UiKit.Panel(root, "Body", UiKit.Card);
            UiKit.Height(body, 0, 1);
            UiKit.Column(body, 20, 0);
            var content = UiKit.ScrollList(body, 6);
            UiKit.Height(content.parent, 0, 1);

            string month = null;
            int index = 0;
            for (int day = _career.Day; day < games; day++)
            {
                string m = CareerState.MonthOf(day, games);
                if (m != month)
                {
                    month = m;
                    UiKit.Height(UiKit.Label(content, m, 34, TextAnchor.LowerLeft, Gold), 70);
                }
                string opponent = "경기 없음";
                foreach (var (away, home) in _career.League.Matchups(day))
                {
                    if (home == _career.MyTeam) opponent = $"vs {away.Name} <color=#9EABC2>(홈)</color>";
                    else if (away == _career.MyTeam) opponent = $"@ {home.Name} <color=#9EABC2>(원정)</color>";
                }
                keyDays.TryGetValue(day, out var special);
                string label = special == null ? "" : $"<color=#FFD24D>{special}</color>";
                Color? back = day == _career.Day ? Today : special != null ? Special : index % 2 == 1 ? new Color(1, 1, 1, 0.04f) : (Color?)null;
                UiKit.TableRow(content, new float[] { 150, 0, 260 },
                    new[] { day == _career.Day ? "<b>오늘</b>" : $"{day + 1}일차", opponent, label }, 28, Color.white, back);
                index++;
            }
            UiKit.Height(UiKit.Label(content, $"정규시즌이 끝나면 상위 {Postseason.Teams}팀이 가을야구에 나가요.", 26, TextAnchor.MiddleCenter, UiKit.Muted), 90);

            UiKit.Height(UiKit.Button(root, "닫기", () => Destroy(gameObject), UiKit.Muted), 110);
        }

        /// <summary>The season's big days, by day index.</summary>
        SortedDictionary<int, string> KeyDays()
        {
            var days = new SortedDictionary<int, string>
            {
                [_career.AllStarDay] = "올스타전",
                [_career.TradeDeadline] = "트레이드 마감",
            };
            if (NationalTeam.IsTournamentYear(_career.Year)) days[_career.TournamentDay] = NationalTeam.Name;
            days[_career.League.GamesPerSeason - 1] = "정규시즌 최종전";
            return days;
        }
    }
}
