using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>Story 3, "연습생 신화": undrafted, signed from a tryout, a container dorm, a fellow trainee, a grandmother's soup shop and the club staffer who wrote the first contract.</summary>
    internal static class WalkonStory
    {
        public static StoryRouteDef Build() => new StoryRouteDef
        {
            Id = StoryRoute.Walkon,
            Name = "연습생 신화",
            Blurb = "드래프트에서 이름이 불리지 않은 연습생이 바닥부터 올라간다.",
            BondNames = new[] { "도봉구", "김반석", "할머니", "이다온" },
            BondRoles = new[] { "나를 뽑은 2군 감독", "연습생 동기", "국밥집 할머니", "구단 운영팀 매니저" },
            MentorBefore = "도봉구 2군 감독",
            MentorAfter = "도봉구 수석코치",
            Scenes = Scenes(),
            Opening = (c, r, ending) =>
                ending == StoryEnding.Legend ? new[] { "[구단 소식지 · 이다온] '연습생 테스트에서 명예의 전당까지'", $"헌액식. {r.Name} 선수는 연단에 컨테이너 숙소 사진 한 장을 올려놓고 이야기를 시작했다." }
                : ending == StoryEnding.OneClub ? new[] { $"[구단 소식지 · 이다온] '연습생으로 와서, {r.MainClub}의 전설로'", $"{r.Seasons}시즌. 연습생 테스트를 봤던 그 구장에서 은퇴 경기가 열렸다." }
                : new[] { "[구단 소식지 · 이다온] '연습생 신화, 두 번째 테스트'", "은퇴 후, 할머니 국밥집 옆 건물에 작은 야구 교실이 문을 열었다. 드래프트에서 이름이 불리지 않은 아이들도 받는다." },
            BondLine = (c, b, v) =>
            {
                switch (b)
                {
                    case Bond.Mentor:
                        return v >= 70 ? "도봉구가 낡은 연습생 테스트 지원서를 꺼내 보였다. 200장 중 맨 위에 있던 한 장이었다."
                            : v <= 30 ? "도봉구는 오지 않았다. 대신 펑고 배트 하나가 택배로 왔다."
                            : "도봉구가 뒷줄에서 팔짱을 낀 채 고개를 끄덕였다.";
                    case Bond.Friend:
                        return v >= 70
                            ? (c.Story.Has("friend_back") ? "반석이 다른 구단 유니폼을 입은 채 꽃다발을 들고 왔다. \"컵라면 값은 이걸로 퉁치자.\""
                                : c.Story.Has("bet") ? "반석이 컵라면 한 박스를 들고 왔다. \"내기는 내가 졌다.\""
                                : "반석이 맨 앞줄에서 누구보다 크게 이름을 불렀다.")
                            : v <= 30 ? "반석에게서 짧은 문자가 왔다. '신화 완결 축하한다.'"
                            : "반석은 가게 문을 닫고 손님들과 TV로 지켜봤다고 했다.";
                    case Bond.Family:
                        return v >= 70 ? "할머니가 국밥 한 그릇을 들고 왔다. \"우리 손주, 고생했다.\""
                            : v <= 30 ? "할머니는 가게 때문에 못 왔다며 국밥 쿠폰을 보내왔다."
                            : "할머니가 객석에서 앞치마로 눈가를 닦았다.";
                    default:
                        return v >= 70 ? "이다온이 첫 계약서 사본을 액자에 넣어 선물했다. 귀퉁이에 '내가 처음 계약한 선수'라고 적혀 있었다."
                            : "이다온의 소식지 기사는 짧았지만 정확했다.";
                }
            },
            Closing = (c, r, ending) =>
                ending == StoryEnding.Legend ? "이름 없이 시작한 이야기는, 모두가 아는 이름으로 끝났다."
                : ending == StoryEnding.OneClub ? "연습생으로 와서 전설로 떠난 사람. 구단은 매년 연습생 테스트 날, 그 이름을 먼저 부른다."
                : "이름이 불리지 않아도 괜찮다고, 오늘도 아이들에게 말한다.",
        };

        static List<StoryScene> Scenes() => new List<StoryScene>
        {
            // ----- Prologue -----
            new StoryScene
            {
                Slot = "draft", Title = "불리지 않은 이름",
                Text = c => "드래프트에서 끝내 이름이 불리지 않았다.\n그리고 한 달 뒤, {team} 연습생 테스트. 지원자 200명 중 뽑힌 건 두 명.\n\n" +
                            "2군 감독 도봉구가 서류를 덮으며 말했다.\n\"연봉은 최저, 숙소는 컨테이너. 그래도 하겠나?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"하겠습니다\"", "\"대답은 좋다. 내일 아침 6시.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"1군은 언제 갈 수 있습니까?\"", "도봉구가 처음으로 웃었다. \"그 배짱이면 생각보다 빠를지도.\"",
                        (e, k) => { e.Exp(Stat.Contact, 30); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "roommate", Title = "컨테이너 숙소",
                Text = c => "컨테이너 숙소, 2층 침대. 아래 칸에는 같이 뽑힌 연습생 김반석.\n\"나 3수째야. 이번에 안 되면 진짜 끝이다.\"\n\n" +
                            "반석이 컵라면 두 개에 물을 부었다.\n\"먹고 하자. 우리 둘이 신화 한번 써 보자고.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"같이 정식 선수 되자\"", "컵라면 국물로 건배를 했다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 10)),
                    O("누가 먼저 되나 내기한다", "\"진 사람이 1년 동안 컵라면 산다.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "bet"); }),
                    O("일찍 잔다", "반석은 혼자 컵라면 두 개를 먹었다.",
                        (e, k) => { e.Fatigue(-10); ChangeBond(e, k, Bond.Friend, -5); }),
                },
            },

            // ----- 1. Debut -----
            new StoryScene
            {
                Slot = "first", Title = "정식 선수 등록",
                Text = c => "구단 운영팀 이다온 매니저가 서류 한 장을 내밀었다. '정식 선수 등록'.\n" +
                            "\"축하해요. 연습생에서 정식 선수로 올라온 건 우리 구단 3년 만이에요.\"\n\n" +
                            (HasFirstRecord(c) ? "그리고 며칠 뒤, {first}." : "{first}까지는 아직 멀다. 그래도 이제 등번호가 있다."),
                Options = c => new List<StoryOption>
                {
                    O("\"감사합니다, 매니저님\"", "이다온이 서류 귀퉁이에 작게 적었다. '내가 처음 계약한 선수.'",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("할머니께 전화한다", "\"우리 손주, 이제 진짜 선수네.\" 국밥집 손님들 박수 소리가 들렸다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Family, 10); }),
                    O("반석부터 찾는다", "반석이 축하한다며 웃었다. 2층 침대 위 칸이 왠지 높아 보였다.",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "letter", Title = "할머니의 국밥",
                Text = c => "할머니는 경기장 앞에서 국밥집을 하신다.\n오늘 가 보니 가게 벽에 내 사진이 붙어 있었다. '우리 손주 응원하면 국밥 천 원 할인.'\n\n" +
                            "\"손님이 두 배다. 너 덕분이야.\"",
                Options = c => new List<StoryOption>
                {
                    O("가게 일을 돕는다", "사인해 달라는 손님 때문에 설거지가 밀렸다.",
                        (e, k) => { e.Fatigue(5); e.Fans(20); ChangeBond(e, k, Bond.Family, 10); }),
                    O("\"할인은 그만하세요\"", "할머니가 국자를 휘둘렀다. \"내 가게다!\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Family, -5); }),
                    O("국밥 두 그릇을 비운다", "든든하다. 이게 힘이다.",
                        (e, k) => { e.Fatigue(-10); e.Morale(4); ChangeBond(e, k, Bond.Family, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "rival", Title = "1순위의 그 녀석",
                Text = c => "같은 해 1라운드 1순위로 프로에 온 동갑내기, {rivalTeam}의 {rival}.\n계약금 수억 원. 내 연봉의 수십 배다.\n\n" +
                            "이다온이 기사를 내밀며 말했다.\n\"비교당하는 게 싫으면 보지 마요. 근데 저는 둘 다 같은 출발선이라고 생각해요.\"",
                Options = c => new List<StoryOption>
                {
                    O("{rival}, 따라잡는다", "그날 밤, 상대 기록을 처음으로 찾아봤다.",
                        (e, k) => { e.Exp(Stat.Power, 30); e.Morale(3); Flag(k, "rival_fire"); }),
                    O("내 페이스대로 간다", "남과 비교하지 않기로 했다.",
                        (e, k) => e.Morale(4)),
                    O("그 말을 수첩에 적는다", "'같은 출발선.' 수첩 첫 장에 적었다.",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },

            // ----- 2. The first wall -----
            new StoryScene
            {
                Slot = "wall", Title = "다시 바닥",
                Text = c => c.InFarm
                    ? "1군에서 버틴 건 몇 주뿐이었다. 다시 2군.\n컨테이너 숙소로 돌아오니 도봉구가 펑고 배트를 들고 서 있었다.\n\n\"올라갔다 내려오는 게 제일 아프다. 그러니까 더 단단해져서 올라가.\""
                    : "몇 경기째 아무것도 안 된다. '연습생 출신의 한계'라는 말이 들렸다.\n\n도봉구에게서 전화가 왔다.\n\"한계는 남이 정하는 게 아니다. 쉬는 날 2군 구장으로 와라.\"",
                Options = c => new List<StoryOption>
                {
                    O("도봉구에게 간다", "연습생 시절처럼 굴렀다. 손바닥이 다시 까졌다.",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 70)); e.Fatigue(20); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "dawn"); }),
                    O("혼자 버틴다", "\"...그래. 너도 이제 혼자 설 때가 됐지.\"",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(3); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rooftop", Title = "컨테이너 지붕",
                Text = c => "컨테이너 지붕 위. 반석이 별을 보며 누워 있었다. 반석은 아직 정식 선수가 아니다.\n" +
                            "\"야. 너는 올라갔다가 힘든 거잖아. 나는 아직 한 번도 못 올라가 봤어.\"\n\n" +
                            "\"그러니까 포기하지 마. 너 보고 나도 버티는 거니까.\"",
                Options = c => new List<StoryOption>
                {
                    O("속마음을 털어놓는다", "밤새 이야기했다. 별이 많았다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("\"같이 야간 훈련 가자\"", "둘이서 불 꺼진 실내 연습장에 불을 켰다.",
                        (e, k) => { e.Exp(Stat.Power, 40); e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 10); }),
                    O("\"괜찮아\"라고만 한다", "반석은 더 묻지 않았다.",
                        (e, k) => e.Morale(2)),
                },
            },
            new StoryScene
            {
                Slot = "breakthrough", Title = "연습생의 하루",
                Text = c => "오늘은 모든 게 맞아떨어졌다.\n컵라면으로 버틴 밤, 펑고에 까진 손바닥. 그 모든 게 오늘을 위해 있었다.\n\n" +
                            (c.Story.Has("dawn") ? "쉬는 날마다 갔던 2군 구장이 떠올랐다.\n" : "") +
                            "관중석 구석, 2군 유니폼을 입은 도봉구가 팔짱을 끼고 서 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("\"이게 연습생의 야구다\"", "이다온이 구단 소식지 1면을 비워 두겠다고 했다.",
                        (e, k) => { e.Morale(6); e.Fans(20); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("도봉구에게 전화한다", "\"전화할 시간에 스윙 한 번 더 해라.\" 끊기 직전 작게 들렸다. \"...잘했다.\"",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new StoryScene
            {
                Slot = "spot", Title = "연습생 출신 {spot}",
                Text = c => "도봉구가 1군 수석코치로 올라왔다.\n첫 미팅에서 그가 라인업 카드를 들어 보였다.\n\n" +
                            "\"연습생 테스트 200명 중에 내가 뽑은 놈이다. 오늘부터 우리 팀 {spot}.\"\n" +
                            (c.Story.Has("dawn") ? "\"쉬는 날마다 2군 구장에 오던 놈이다. 주장도 맡긴다고 감독님께 말해 뒀다.\"" : ""),
                Options = c => new List<StoryOption>
                {
                    O("\"맡겨 주십시오\"", "컨테이너 숙소 벽에 새긴 '1군' 두 글자가 떠올랐다.",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"제가 정말 이 자리에...\"", "\"연습생 시절부터 이 자리는 네 거였다.\"",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(2); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "injury", Title = "반석의 방출",
                Text = c => "반석이 방출 통보를 받았다. 4년 동안 한 번도 1군에 올라가지 못했다.\n\n" +
                            "컨테이너 앞에서 반석이 짐을 싸고 있었다.\n\"신화는 너 혼자 써라. 나는 여기까지인가 봐.\"",
                Options = c => new List<StoryOption>
                {
                    O("다른 구단 테스트를 같이 알아본다", "쉬는 날마다 반석과 테스트를 보러 다녔다. 세 번째 구단에서 연락이 왔다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 20); Flag(k, "visited"); }),
                    O("\"네 몫까지 뛴다\"", "그날 경기, 모자 안쪽에 반석의 등번호를 적었다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("짐을 같이 싼다", "말없이 박스 테이프를 붙였다. 컨테이너가 넓어 보였다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 5)),
                },
            },
            new StoryScene
            {
                Slot = "race", Title = "계약금 수억 대 연습생",
                Text = c => "타이틀 경쟁 마지막 주. 상대는 1순위 {rival}.\n기사 제목은 '계약금 수억 원 대 연습생 출신'.\n\n" +
                            "이다온이 기사를 접으며 말했다.\n\"이 기사, 끝은 제가 다시 쓰게 해 줘요.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"덤벼\"", "그날 경기, 공 하나하나에 온 힘을 실었다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); e.Morale(4); }),
                    O("{rival}에게 먼저 인사한다", "{rival} 선수가 놀란 눈으로 고개를 숙였다. \"연습생 신화, 저도 응원했어요.\"",
                        (e, k) => { e.Morale(5); e.Fans(30); Flag(k, "rival_respect"); }),
                },
            },

            // ----- 4. The summit -----
            new StoryScene
            {
                Slot = "summit", Title = "신화",
                Text = c => (HasSummit(c)
                                ? "시상식. 이름이 불렸다. 연습생 테스트 200명 중 한 명이 여기까지 왔다.\n객석에는 도봉구, 반석, 그리고 국밥집 앞치마 그대로 온 할머니.\n\n" +
                                  "이다온이 다가왔다.\n\"다음 계약서, 제가 쓸게요. 이번엔 숫자가 좀 클 거예요.\""
                                : "스물여덟. 트로피는 아직이다.\n그래도 이제 '연습생 출신'이라고 부르는 사람은 없다.\n\n" +
                                  "이다온이 계약서를 내밀었다.\n\"다음 목표는 뭐예요?\""),
                Options = c => new List<StoryOption>
                {
                    O("\"아직 배고픕니다\"", "소식지 제목은 '연습생, 아직 배고프다'였다.",
                        (e, k) => { e.Morale(5); e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("고마운 사람들 이름을 부른다", "소식지에는 네 사람의 이름이 실렸다.",
                        (e, k) =>
                        {
                            e.Fans(40);
                            ChangeBond(e, k, Bond.Mentor, 5);
                            ChangeBond(e, k, Bond.Friend, 5);
                            ChangeBond(e, k, Bond.Family, 5);
                            ChangeBond(e, k, Bond.Voice, 10);
                        }),
                },
            },
            new StoryScene
            {
                Slot = "family", Title = "할머니의 가게",
                Text = c => "할머니가 쓰러지셨다는 연락이 왔다. 다행히 큰일은 아니었다.\n\n" +
                            "병실에서 할머니가 말했다.\n\"가게는 접어야겠다. 근데 손님들이 너 응원할 데가 없어진다고 난리야.\"",
                Options = c => new List<StoryOption>
                {
                    O("가게를 이어 갈 사람을 구한다 (300만원)", "가게는 '손주네 국밥'이라는 새 이름으로 다시 문을 열었다.",
                        (e, k) => { e.Money(-300); ChangeBond(e, k, Bond.Family, 15); }),
                    O("매일 병문안을 간다", "경기가 끝나면 매일 병원에 들렀다. 할머니가 금방 기운을 차렸다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Family, 15); }),
                    O("할머니 몫까지 뛴다", "그날 경기 후 병실 TV에서 하이라이트가 나왔다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Family, 5); }),
                },
            },

            // ----- 5. Veteran -----
            new StoryScene
            {
                Slot = "body", Title = "몸이 먼저 안다",
                Text = c => "서른둘. 컨테이너에서 밤새 버티던 몸이, 이제는 하루를 쉬어야 돌아온다.\n\n" +
                            "트레이너가 말했다. \"이제는 덜 하는 게 더 하는 거예요.\"\n도봉구가 덧붙였다. \"연습생 근성은 좋다. 이제는 오래 가는 법을 배워라.\"",
                Options = c => new List<StoryOption>
                {
                    O("요령을 익힌다", "힘 대신 경험으로 싸우는 법을 배우기 시작했다.",
                        (e, k) => { e.Exp(Stat.Eye, 60); e.Fatigue(-20); }),
                    O("근성으로 버틴다", "새벽 러닝을 늘렸다. 몸이 비명을 질렀지만 버텼다.",
                        (e, k) =>
                        {
                            e.Exp(Stat.Power, 60);
                            e.Fatigue(25);
                            if (e.MaybeInjure(0.15, 3, InjuryKind.Hamstring)) e.Say("...결국 허벅지가 탈이 났다.");
                        }),
                    O("도봉구에게 조언을 구한다", "\"나도 서른둘에 그랬다. 그때부터가 진짜 야구다.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "friend", Title = "반석의 소식",
                Text = c => c.Story.Has("visited")
                    ? "반석에게서 사진 한 장이 왔다. 다른 구단 1군 엔트리 명단.\n\n'4년 걸렸다. 너 덕분이다. 다음 주에 맞대결이다, 봐주지 마.'"
                    : "반석이 고향에서 야구용품점을 열었다고 했다. 가게 한쪽 벽에 내 유니폼이 걸려 있다고.\n\n'너 경기 날엔 문 닫고 다 같이 본다. 신화 끝까지 써라.'",
                Options = c => c.Story.Has("visited")
                    ? new List<StoryOption>
                    {
                        O("맞대결에서 전력을 다한다", "경기 후 둘은 2층 침대 시절 얘기로 밤을 새웠다.",
                            (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 10); Flag(k, "friend_back"); }),
                        O("\"컵라면은 내가 산다\"", "\"이제 컵라면 말고 고기 사라, 스타야.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "friend_back"); }),
                    }
                    : new List<StoryOption>
                    {
                        O("사인 배트를 보낸다", "배트는 가게 계산대 위에 걸렸다고 했다.",
                            (e, k) => { e.Fans(20); ChangeBond(e, k, Bond.Friend, 10); }),
                        O("\"가게 대박 나라\"", "\"네가 잘하면 대박 나.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); }),
                    },
            },
            new StoryScene
            {
                Slot = "family_game", Title = "할머니의 시구",
                Text = c => "구단이 할머니를 시구자로 초대했다. 이다온이 기획한 이벤트였다.\n\n" +
                            "국밥집 앞치마 차림 그대로 마운드에 오른 할머니가 원바운드 공을 던졌다.\n관중석이 웃음과 박수로 가득 찼다.",
                Options = c => new List<StoryOption>
                {
                    O("할머니를 업고 내려온다", "그 장면이 그날 스포츠 뉴스 마지막을 장식했다.",
                        (e, k) => { e.Morale(5); e.Fans(40); ChangeBond(e, k, Bond.Family, 10); }),
                    O("이다온에게 고맙다고 한다", "\"제가 처음 계약한 선수 할머니니까요.\" 이다온이 어깨를 으쓱했다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },

            // ----- 6. The last season -----
            new StoryScene
            {
                Slot = "last", Title = "마지막 대화",
                Text = c => "서른일곱의 봄. 은퇴를 앞둔 도봉구가 옛 컨테이너 숙소 자리에 서 있었다. 이제 그 자리엔 실내 연습장이 있다.\n\n" +
                            "\"200명 중에 너를 고른 날, 다들 미쳤다고 했다.\"\n\"내 야구 인생 최고의 선택이었다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"끝까지 달리겠습니다\"", "\"그래야 내가 뽑은 놈이지.\"",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("고개 숙여 인사한다", "도봉구가 등을 돌렸다. 어깨가 조금 떨리고 있었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 15); }),
                },
            },
        };
    }
}
