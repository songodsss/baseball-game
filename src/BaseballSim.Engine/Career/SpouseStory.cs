using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// After the wedding the romance scenes stop, so married life gets its own small scenes:
    /// a lunchbox in a slump, nursing an injury, the anniversary, a sign from the stands, a long road trip,
    /// her own big day, moving house after a trade, the late-career talk. One every few weeks at most.
    /// </summary>
    public static partial class StoryEvents
    {
        /// <summary>Married, and no married-life scene for a while.</summary>
        static bool SpouseReady(Career c)
        {
            if (!c.HasMark("married") || c.InMilitary) return false;
            int last = c.Me.StoryMarks.Keys.Where(k => k.StartsWith("seen:spouse_")).Select(k => c.GamesSince(k)).DefaultIfEmpty(int.MaxValue).Min();
            return last >= Gap(c) * 2 && c.GamesSince("married") >= Gap(c);
        }

        static Stat Main(EventEffects e) => e.Career.Me.MyStats.OrderByDescending(s => e.Career.Me.Ratings[s]).First();

        static void AddSpouseStories(List<StoryEvent> list)
        {
            list.Add(new StoryEvent
            {
                Id = "spouse_lunchbox",
                Title = "도시락 쪽지",
                Text = "요즘 경기가 안 풀린다. 출근 가방을 여니 {spouse}가 싸 준 도시락이 들어 있었다.\n뚜껑 위에 쪽지 한 장. '오늘 못 쳐도 저녁은 맛있는 거 해 줄게. 그러니까 마음 편히.'",
                Weight = (c, d) => SpouseReady(c) && (c.HitlessStreak >= 2 || c.Me.Morale <= -3) && !c.SeenThisSeason("spouse_lunchbox") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "쪽지를 사물함 안쪽에 붙인다", Result = "타석에 들어가기 전마다 사물함을 한 번씩 봤다. 어깨에 힘이 빠졌다.", Apply = e => { e.Morale(6); e.Exp(Main(e), 40); } },
                    new EventChoice { Label = "\"오늘은 꼭 칠게\" 답장을 보낸다", Result = "\"못 쳐도 된다니까 ㅋㅋ 근데 치면 더 좋고.\"", Apply = e => { e.Morale(5); e.Boost(Main(e), 15, 10, "도시락 쪽지"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_injury",
                Title = "재활의 동반자",
                Text = "부상으로 집에 있는 날이 길어졌다. 소파에 누워 경기 중계만 보고 있는데, {spouse}가 냉장고에 표를 붙였다.\n'재활 체크표. 하루 다 하면 스티커 하나.' 유치하다고 웃었는데, 스티커가 꽤 갖고 싶어졌다.",
                Weight = (c, d) => c.HasMark("married") && c.Me.InjuryDaysLeft >= 5 && !c.SeenThisSeason("spouse_injury") ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "스티커를 다 모은다", Result = "매일 체크표를 채웠다. 트레이너가 회복이 빠르다며 놀랐다.", Apply = e => { e.ShortenInjury(3); e.Morale(5); } },
                    new EventChoice { Label = "같이 영상 분석을 한다", Result = "{spouse}가 내 타석 영상을 보다가 물었다. \"이때 왜 꼭 고개가 들려?\" 맞는 말이었다.", Apply = e => { e.Exp(Stat.Eye, 60); e.Morale(4); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_anniversary",
                Title = "결혼기념일",
                Text = "달력에 동그라미가 쳐져 있다. 결혼기념일. 하필 원정 일정 한가운데다.\n{spouse}는 \"괜찮아, 야구 선수랑 결혼한 거 알고 했어\"라고 했지만, 목소리가 조금 작았다.",
                Weight = (c, d) => SpouseReady(c) && c.GamesSince("married") >= c.League.GamesPerSeason / 2 && InWindow(c, 0.35, 0.7) && !c.SeenThisSeason("spouse_anniversary") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "꽃과 케이크를 집으로 보낸다 (300만원)", Result = "문 앞에 놓인 꽃바구니 사진이 왔다. '반칙이야. 울었잖아.'", Apply = e => { e.Money(-300); e.Morale(6); } },
                    new EventChoice { Label = "경기 끝나고 첫 차로 올라간다", Result = "새벽 세 시, 식탁에 마주 앉아 컵라면을 먹었다. 그게 제일 좋은 기념일이었다.", Apply = e => { e.Morale(7); e.Fatigue(15); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_stands",
                Title = "관중석의 피켓",
                Text = "오늘 잘 풀린 경기, 전광판 카메라가 관중석을 비췄다.\n{spouse}가 직접 만든 피켓을 들고 있었다. '우리 집 에이스. 집에서도 에이스.' 관중석이 웃음바다가 됐다.",
                Weight = (c, d) => SpouseReady(c) && HomeGame(c, d) && PlayedWell(d) && !c.SeenThisSeason("spouse_stands") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "카메라를 향해 손 하트", Result = "다음 날 그 장면이 '오늘의 명장면' 1위가 됐다.", Apply = e => { e.Fans(80); e.Morale(5); } },
                    new EventChoice { Label = "\"집에서 몇 번인데?\" 문자를 보낸다", Result = "\"설거지 담당 1번.\" 답장을 보고 더그아웃에서 혼자 웃었다.", Apply = e => { e.Morale(6); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_road",
                Title = "긴 원정",
                Text = "열흘째 원정이다. 호텔 방에서 영상 통화를 걸었더니 {spouse}가 화면 너머로 집 안을 비춰 줬다.\n\"네 자리 비어 있는 거 보여? 빨리 와.\" 웃는데 조금 쓸쓸한 얼굴이었다.",
                Weight = (c, d) => SpouseReady(c) && d.MyGame != null && d.MyGame.Home != c.MyTeam && c.Me.Fatigue >= 40 && !c.SeenThisSeason("spouse_road") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "통화를 켜 둔 채 잠든다", Result = "아침에 일어나니 화면 너머에서도 {spouse}가 자고 있었다. 오랜만에 푹 잤다.", Apply = e => { e.Fatigue(-20); e.Morale(5); } },
                    new EventChoice { Label = "\"돌아가면 하루 온전히 너랑 있을게\"", Result = "약속은 꼭 지켰다. 휴일 하루, 휴대폰은 꺼 뒀다.", Apply = e => { e.Fatigue(-10); e.Morale(4); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_her_day",
                Title = "그녀의 큰 날",
                Text = "오늘은 내 경기보다 {spouse}의 날이었다. 몇 달을 매달린 일이 드디어 결과가 나왔다.\n늘 내 경기를 챙겨 주던 사람. 오늘만큼은 내가 챙길 차례다.",
                Weight = (c, d) => SpouseReady(c) && c.GamesSince("married") >= c.League.GamesPerSeason && !c.Seen("spouse_her_day") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "꽃다발 들고 데리러 간다", Result = "퇴근길에 꽃다발을 든 나를 보고 {spouse}가 눈을 동그랗게 떴다. \"너 경기 있잖아!\" \"오늘은 네 경기가 더 중요해.\"", Apply = e => { e.Morale(7); e.Fatigue(5); } },
                    new EventChoice { Label = "깜짝 축하 파티를 연다 (200만원)", Result = "동료들까지 불렀다. {spouse}가 그날 밤 내내 웃었다.", Apply = e => { e.Money(-200); e.Morale(6); e.Close("veteran", 1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_move",
                Title = "다시 싸는 이삿짐",
                Text = "새 팀으로 옮기고 첫 휴일. 아직 풀지 못한 이삿짐 상자 사이에서 {spouse}가 테이프를 뜯고 있었다.\n\"여기 동네 괜찮더라. 야구장도 가깝고.\" 먼저 괜찮다고 말해 주는 사람이었다.",
                Weight = (c, d) => SpouseReady(c) && c.HasMark("moved") && c.GamesSince("moved") >= 3 && c.GamesSince("moved") <= 40 && !SeenSinceMove(c, "spouse_move") ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "같이 짐을 푼다", Result = "상자 맨 밑에서 신인 때 유니폼이 나왔다. 둘이 한참을 웃었다. 새 집이 조금 우리 집 같아졌다.", Apply = e => { e.Morale(6); e.Fatigue(5); } },
                    new EventChoice { Label = "\"미안해, 또 옮기게 해서\"", Result = "\"사과하지 마. 어디든 네가 야구하는 데가 우리 집이야.\"", Apply = e => { e.Morale(5); e.Boost(Main(e), 20, 14, "새 출발"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_kitchen",
                Title = "식단 관리",
                Text = "아침 식탁에 낯선 메뉴가 올라왔다. {spouse}가 영양사 강의를 몰래 들었다고 했다.\n\"선수는 몸이 재산이라며. 이제 내가 관리할 거야.\"",
                Weight = (c, d) => SpouseReady(c) && !c.Seen("spouse_kitchen") && Active(c) ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "남김없이 먹는다", Result = "한 달 뒤, 트레이너가 체성분 표를 보고 휘파람을 불었다.", Apply = e => { e.Boost(e.Career.Me.IsPitcher ? Stat.Stamina : Stat.Power, 30, 30, "집밥 식단"); e.Morale(3); } },
                    new EventChoice { Label = "\"치킨은 일주일에 한 번만 허락해 줘\"", Result = "협상 끝에 2주에 한 번으로 타결됐다. 그래도 몸은 확실히 가벼워졌다.", Apply = e => { e.Boost(e.Career.Me.IsPitcher ? Stat.Stamina : Stat.Power, 20, 30, "집밥 식단"); e.Morale(5); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_film",
                Title = "녹화 중계",
                Text = "밤늦게 집에 오니 {spouse}가 오늘 경기 녹화를 돌려 보고 있었다.\n\"있잖아, 너 2스트라이크 되면 발이 살짝 열려. 해설은 아무 말 안 하던데.\"",
                Weight = (c, d) => SpouseReady(c) && Played(d) && !c.Me.IsPitcher && c.Seen("spouse_lunchbox") && !c.Seen("spouse_film") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "같이 끝까지 돌려 본다", Result = "새벽까지 리모컨을 주고받았다. 다음 날 타석에서 발을 단단히 붙였다.", Apply = e => { e.Exp(Stat.Eye, 50); e.Boost(Stat.Contact, 20, 14, "집에서 한 분석"); } },
                    new EventChoice { Label = "\"우리 집에 전력분석원이 있었네\"", Result = "\"월급은 설거지로 받을게.\"", Apply = e => { e.Morale(5); e.Exp(Stat.Eye, 30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "spouse_later",
                Title = "언제까지 할 거야?",
                Text = "산책길에 {spouse}가 불쑥 물었다. \"언제까지 할 거야, 야구?\"\n대답을 못 하고 있으니 {spouse}가 손을 꼭 잡았다. \"재촉하는 거 아니야. 네가 그만두고 싶을 때까지 옆에 있겠다는 거야.\"",
                Weight = (c, d) => SpouseReady(c) && c.Me.Age >= 33 && !c.Seen("spouse_later") ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"몸이 허락할 때까지\"", Result = "\"그럼 몸 관리는 내가 할게.\" 그날부터 아침 스트레칭을 같이 했다.", Apply = e => { e.Morale(5); e.Boost(Main(e), 25, 30, "둘이 하는 스트레칭"); } },
                    new EventChoice { Label = "\"마지막 시즌은 꼭 같이 정하자\"", Result = "\"약속.\" 새끼손가락을 걸었다. 남은 시즌이 조금 더 소중해졌다.", Apply = e => { e.Morale(7); } },
                },
            });
        }
    }
}
