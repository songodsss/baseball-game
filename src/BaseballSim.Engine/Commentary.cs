using System.Linq;

namespace BaseballSim
{
    /// <summary>Korean one-line play-by-play for a game event.</summary>
    public static class Commentary
    {
        public static string Describe(GameEvent ev)
        {
            if (ev.Kind == EventKind.PitchingChange)
                return $"투수 교체: {ev.Pitcher.Name} 마운드에 오릅니다.";
            if (ev.Kind == EventKind.StolenBase)
                return ev.OutsAfter > ev.OutsBefore ? $"{ev.Batter.Name}, 2루 도루 실패! 태그 아웃." : $"{ev.Batter.Name}, 2루 도루 성공!";

            string who = ev.Batter.Name;
            string text;
            switch (ev.Outcome)
            {
                case Outcome.Strikeout: text = $"{who}, 삼진 아웃."; break;
                case Outcome.Walk: text = $"{who}, 볼넷으로 출루."; break;
                case Outcome.HitByPitch: text = $"{who}, 몸에 맞는 공."; break;
                case Outcome.Single: text = ev.Note == "번트 안타!" ? $"{who}, 기습 번트 안타!" : $"{who}, 안타!"; break;
                case Outcome.Double: text = $"{who}, 2루타!"; break;
                case Outcome.Triple: text = $"{who}, 3루타!"; break;
                case Outcome.HomeRun:
                    text = ev.RunnersScored.Count == 4 ? $"{who}, 만루 홈런!!" : ev.RunnersScored.Count == 1 ? $"{who}, 솔로 홈런!" : $"{who}, {ev.RunnersScored.Count}점 홈런!";
                    break;
                case Outcome.GroundOut:
                    text = ev.SacrificeBunt ? $"{who}, 희생번트." : ev.Note != null && ev.Note.StartsWith("번트 실패") ? $"{who}, 번트 실패. 선행 주자 아웃."
                        : ev.DoublePlay ? $"{who}, 병살타." : $"{who}, 땅볼 아웃.";
                    break;
                case Outcome.FlyOut: text = ev.SacrificeFly ? $"{who}, 희생 플라이." : $"{who}, 뜬공 아웃."; break;
                default: text = $"{who}, 직선타 아웃."; break;
            }

            if (ev.RunnersScored.Count > 0 && ev.Outcome != Outcome.HomeRun)
                text += $" {string.Join(", ", ev.RunnersScored.Select(r => r.Name))} 홈인.";
            return text;
        }

        public static string HalfInning(int inning, bool top) => $"{inning}회{(top ? "초" : "말")}";
    }
}
