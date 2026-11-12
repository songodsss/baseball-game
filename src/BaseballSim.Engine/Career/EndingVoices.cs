using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>One person's goodbye at the end of a career: who says it and what.</summary>
    public class EndingVoice
    {
        public string Who;
        /// <summary>Who they were to the player ("룸메이트 선배", "아내"), shown small under the name.</summary>
        public string Role;
        public string Line;
    }

    /// <summary>
    /// After the ending article: the people the career touched each say a few words, one after
    /// another, like voices at a retirement ceremony. Only the ones the player really met or grew
    /// close to speak; the closer they were, the warmer the line.
    /// </summary>
    public static class EndingVoices
    {
        public static List<EndingVoice> For(Career c)
        {
            var voices = new List<EndingVoice>();
            void Say(string who, string role, string line)
            {
                if (string.IsNullOrEmpty(who) || voices.Any(v => v.Who == who)) return;
                voices.Add(new EndingVoice { Who = who, Role = role, Line = StoryEvent.Fill(line, c) });
            }
            var story = c.Story;

            // The main story's people.
            foreach (var b in MainStory.AllBonds)
            {
                int bond = story.BondOf(b);
                if (bond < 40) continue;
                bool close = bond >= MainStory.BondEndingAt;
                string name = MainStory.BondName(c, b);
                switch (b)
                {
                    case Bond.Mentor:
                        Say(name, "스승", close ? "처음 봤을 때부터 알았다. 너는 끝까지 갈 놈이라고. ...잘 버텼다, 정말로." : "수고했다. 네 스윙은 오래 기억될 거다.");
                        break;
                    case Bond.Friend:
                        Say(name, "친구", close ? "야, 은퇴해도 캐치볼은 계속하는 거다? 약속했잖아. 우리 둘이 할아버지 될 때까지." : "고생했다, 친구. 다음엔 술 한잔하자.");
                        break;
                    case Bond.Family:
                        Say(name, "가족", close ? "텔레비전으로 네 경기 하나도 안 빼고 다 봤다. 이제 집밥 먹으러 자주 와라." : "다치지 않고 끝내서 다행이다. 고생 많았다.");
                        break;
                    case Bond.Voice:
                        Say(name, "기록하는 사람", close ? "당신 기사를 쓰는 게 내 일의 절반이었어요. 마지막 기사는... 쓰기 싫네요." : "좋은 기사거리를 많이 줘서 고마웠습니다.");
                        break;
                }
            }

            // Teammates.
            if (c.Seen("senior_roommate"))
                Say(c.SeniorName, "첫 룸메이트 선배", c.IsClose("senior") ? "코 고는 신인이 은퇴식을 다 하네. 내 귤 아직 기억하냐? 자랑스럽다, 진짜로." : "신인 때 그 녀석이 벌써 은퇴라니. 축하한다.");
            if (c.Me.StoryMarks.Keys.Any(k => k.StartsWith("mate:")) && c.Closeness("mate") > 0)
                Say(c.MateName, "입단 동기", "같이 들어와서 너만 끝까지 남았네. 내 몫까지 뛰어 줘서 고마워.");
            foreach (var role in new[] { "junior", "oldjunior" })
                if (c.Closeness(role) >= Career.CloseBond)
                    Say(c.BondName(role) ?? "", "아끼던 후배", "선배님한테 배운 거, 이제 제가 후배들한테 물려줄게요. 그게 제 보답이에요.");
            foreach (var role in new[] { "veteran", "exveteran", "oldveteran" })
                if (c.Closeness(role) >= Career.CloseBond)
                    Say(Clean(c.BondName(role)), "함께 뛴 선배", "옆자리 경쟁할 때가 제일 재밌었다. 너 덕분에 나도 몇 년 더 뛰었어.");
            foreach (var role in new[] { "coach", "excoach", "oldcoach" })
                if (c.Closeness(role) >= Career.CloseBond)
                    Say(Clean(c.BondName(role)) + " 코치", "코치", c.Me.IsPitcher ? "네 공 받아 주던 불펜이 그리울 거다. 마지막까지 좋은 공이었다." : "남아서 같이 스윙하던 밤들, 나한텐 그게 제일 좋은 기억이다.");

            // Legends whose records were in reach.
            foreach (var l in Legends.All.Where(l => l.Visits(c.Me)))
            {
                var stage = c.StageOf(l.Id);
                if (stage == LegendStage.Surpassed) Say(l.Name, $"레전드 · {l.Nickname}", "내 기록을 넘은 게 너라서 다행이다. 이제 사람들이 네 이름을 기억하겠지.");
                else if (stage >= LegendStage.Returned) Say(l.Name, $"레전드 · {l.Nickname}", "끝까지 쫓아오는 걸 보는 게 즐거웠다. 좋은 선수였다. 아니, 좋은 사람이었다.");
            }

            // The rival.
            if (c.Rival != null)
                Say(c.Rival.Name, "동갑내기 라이벌", story.Has("rival_respect") ? "네가 있어서 끝까지 뛸 수 있었다. 고맙다, 진심으로." : "끝까지 지기 싫었다. 다음 생에도 너랑 붙고 싶다.");

            // The manager who trusted him.
            if (c.Me.ManagerTrust >= 4) Say("감독님", "감독", "핑계 한 번 안 댄 선수였다. 그런 선수를 가르쳐 본 건 행운이었다.");

            // The one beside him.
            var lover = Romance.Lover(c);
            if (lover != null)
                Say(lover.Name, Romance.Married(c) ? "배우자" : "연인", Romance.Married(c)
                    ? "수고했어. 이제 내 차례야. 그동안 못 간 여행, 하나씩 다 가자."
                    : "오늘은 그냥 옆에 있을게. 정말 멋있었어.");

            // And the stands, always last.
            voices.Add(new EndingVoice { Who = "팬클럽 회장", Role = "팬", Line = StoryEvent.Fill("{name} 선수 덕분에 행복했습니다. 우리 마음속 등번호는 영원히 그대로예요.", c) });
            return voices;
        }

        static string Clean(string name) => name == null ? "" : name.Replace("veteran", "선배").Replace("coach", "코치");
    }
}
