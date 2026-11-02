using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>Story 2, "천재의 무게": the first overall pick, a cold manager, an old battery mate who never got drafted, a mother and a fan channel.</summary>
    internal static class ProdigyStory
    {
        public static StoryRouteDef Build() => new StoryRouteDef
        {
            Id = StoryRoute.Prodigy,
            Name = "천재의 무게",
            Blurb = "'10년에 한 번 나올 재능'이라 불린 1순위 신인이 기대의 무게를 이겨 낸다.",
            BondNames = new[] { "백무진", "정우진", "어머니", "소이" },
            BondRoles = new[] { "냉정한 감독", "고교 배터리 짝꿍", "혼자 키워 준 어머니", "팬 유튜버" },
            MentorBefore = "백무진 감독",
            MentorAfter = "백무진 감독",
            Scenes = Scenes(),
            Opening = (c, r, ending) =>
                ending == StoryEnding.Legend ? new[] { "['야구하는 소이' 특집] '10년에 한 번이 아니라, 100년에 한 번'", $"명예의 전당 헌액식. {r.Name} 선수는 첫마디로 신인 시절 댓글 이야기를 꺼냈다. \"그 말들 덕분에 여기까지 왔습니다.\"" }
                : ending == StoryEnding.OneClub ? new[] { $"['야구하는 소이' 특집] '1순위로 와서, 끝까지 {r.MainClub} 사람'", "마지막 경기. 1순위 유니폼을 처음 입던 그 구장에서 은퇴식이 열렸다." }
                : new[] { "['야구하는 소이' 특집] '1순위의 두 번째 직업'", "은퇴 후, 어머니의 분식집 자리에 작은 야구 교실이 문을 열었다." },
            BondLine = (c, b, v) =>
            {
                switch (b)
                {
                    case Bond.Mentor:
                        return v >= 70 ? "백무진이 처음이자 마지막으로 박수를 쳤다. \"내 팀에서 제일 늦게 퇴근하던 놈.\""
                            : v <= 30 ? "백무진은 오지 않았다. 대신 손글씨 메모 한 장이 도착했다. '수고했다.'"
                            : "백무진은 맨 뒷줄에서 팔짱을 낀 채 고개를 끄덕였다.";
                    case Bond.Friend:
                        return v >= 70 ? (c.Story.Has("friend_back") ? "우진이 포수 장비를 메고 와서 마지막 공을 받아 줬다." : "우진이 제자들을 데리고 와서 누구보다 크게 응원가를 불렀다.")
                            : v <= 30 ? "우진에게서 짧은 문자가 왔다. '고3 여름, 고마웠다.'"
                            : "우진이 객석에서 미트를 흔들었다.";
                    case Bond.Family:
                        return v >= 70 ? "어머니가 꽃 대신 떡볶이 한 접시를 들고 왔다. \"고생했다, 우리 1순위.\""
                            : v <= 30 ? "어머니는 떨려서 못 오겠다며 집에서 TV로 봤다고 했다."
                            : "어머니가 손수건으로 눈가를 훔쳤다.";
                    default:
                        return v >= 70 ? "소이는 첫 영상 링크를 다시 올렸다. 구독자 3천 명 시절의 그 영상이었다."
                            : "소이 채널에도 짧은 영상 하나가 올라왔다.";
                }
            },
            Closing = (c, r, ending) =>
                ending == StoryEnding.Legend ? "기대에 짓눌렸던 천재는, 결국 기대를 넘어섰다."
                : ending == StoryEnding.OneClub ? "1순위로 시작한 이야기는 한 팀의 역사로 남았다."
                : "재능보다 즐거움을 먼저 가르치는 교실. 아이들은 오늘도 웃으며 배트를 휘두른다.",
        };

        static List<StoryScene> Scenes() => new List<StoryScene>
        {
            // ----- Prologue -----
            new StoryScene
            {
                Slot = "draft", Title = "1라운드 1순위",
                Text = c => "드래프트 1라운드 1순위. 카메라 플래시가 쏟아졌다.\n'10년에 한 번 나올 재능.' 신문마다 같은 제목이었다.\n\n" +
                            "입단식 날, {team}의 백무진 감독은 악수만 하고 돌아섰다.\n\"1순위든 꼴찌든, 내 팀에선 똑같이 시작한다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"실력으로 보여 드리겠습니다\"", "백무진이 걸음을 멈췄다. \"말은 누구나 한다.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("속으로 발끈한다", "'두고 보세요.' 그날 밤 배트를 300번 휘둘렀다.",
                        (e, k) => { e.Exp(Stat.Power, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "roommate", Title = "낯익은 얼굴",
                Text = c => (c.Me.IsPitcher
                                ? "불펜에 낯익은 얼굴이 있었다. 고교 시절 배터리 짝 정우진.\n지명을 못 받고 불펜 포수로 구단에 들어왔다고 했다.\n\n"
                                : "배팅볼을 던지는 사람이 낯익었다. 고교 시절 배터리 짝 정우진.\n지명을 못 받고 배팅볼 투수로 구단에 들어왔다고 했다.\n\n") +
                            "\"축하한다, 1순위.\"\n우진이 웃었지만 눈은 웃고 있지 않았다.",
                Options = c => new List<StoryOption>
                {
                    O("\"같이 1군 가자\"", "\"...나 같은 놈한테 할 말은 아니지.\" 그래도 우진은 다음 날 제일 먼저 나와 있었다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 10)),
                    O("예전처럼 장난을 건다", "우진이 피식 웃었다. 고교 시절로 돌아간 것 같았다.",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("어색해서 피한다", "그날은 서로 눈을 마주치지 않았다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, -5)),
                },
            },

            // ----- 1. Debut -----
            new StoryScene
            {
                Slot = "first", Title = "댓글",
                Text = c => (HasFirstRecord(c)
                                ? "{first}. 그런데 함성보다 한숨이 컸다.\n'1순위치고는 늦었다.' 인터넷 댓글이 쏟아졌다.\n\n"
                                : "{first}까지 아직이다.\n'1순위 거품'이라는 댓글이 쏟아졌다.\n\n") +
                            "그날 밤, 구독자 3천 명짜리 채널 '야구하는 소이'에 영상이 올라왔다.\n'저는 {name} 선수, 진짜라고 생각해요!'",
                Options = c => new List<StoryOption>
                {
                    O("댓글창을 닫는다", "보지 않기로 했다. 대신 소이의 영상을 한 번 더 봤다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("소이 영상에 댓글을 단다", "'고맙습니다. 더 잘할게요.' 다음 날 소이 채널 구독자가 두 배가 됐다.",
                        (e, k) => { e.Fans(30); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("악플을 하나하나 읽는다", "밤새 읽었다. 이를 악물었다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Morale(-3); }),
                },
            },
            new StoryScene
            {
                Slot = "letter", Title = "어머니의 반찬통",
                Text = c => "원정 숙소로 택배가 왔다. 반찬통 다섯 개.\n분식집을 하며 혼자 키워 준 어머니가 보낸 거다.\n\n" +
                            "쪽지 한 장. '1순위 아니어도 돼. 밥 잘 먹고 다치지만 마.'",
                Options = c => new List<StoryOption>
                {
                    O("전화를 건다", "\"엄마, 나 잘하고 있어.\" 수화기 너머가 한참 조용했다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 10); }),
                    O("우진과 나눠 먹는다", "우진이 떡볶이를 먹다 말했다. \"너희 엄마 맛 그대로다.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Family, 5); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("가게를 고쳐 드린다 (300만원)", "어머니는 화를 냈다. 그리고 며칠 뒤 새 간판 사진을 보내왔다.",
                        (e, k) => { e.Money(-300); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },
            new StoryScene
            {
                Slot = "rival", Title = "2순위",
                Text = c => "스포츠 뉴스 한 꼭지, '1순위보다 빛나는 2순위'.\n{rivalTeam}의 {rival}. 드래프트 때 내 바로 다음에 불린 이름이다.\n\n" +
                            "기자들이 마이크를 들이밀었다.\n\"{rival} 선수, 의식되세요?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"전혀요\"", "웃으며 말했지만, 그날 밤 상대 기록을 전부 찾아봤다.",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(3); }),
                    O("\"배울 게 많은 선수죠\"", "소이가 그 장면을 잘라 올렸다. '이게 1순위의 품격.'",
                        (e, k) => { e.Fans(20); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("\"이길 겁니다\"", "다음 날 1면 제목이 됐다. 이제 물러설 곳이 없다.",
                        (e, k) => { e.Exp(Stat.Power, 30); e.Fatigue(5); e.Morale(4); Flag(k, "rival_fire"); }),
                },
            },

            // ----- 2. The first wall -----
            new StoryScene
            {
                Slot = "wall", Title = "1순위의 무게",
                Text = c => c.InFarm
                    ? "2군행. '1순위의 몰락'이라는 기사가 떴다.\n짐을 싸는데 백무진 감독이 문 앞에 서 있었다.\n\n\"억울하냐? 그럼 증명해. 내일부터 아침 6시, 매일 내 방으로 와라.\""
                    : "연속 부진. 관중석에서 야유가 들렸다. 1순위에게 쏟아지는 야유는 더 컸다.\n\n경기 후 백무진 감독이 불렀다.\n\"남들 눈에 맞춰 야구 하지 마라. 내일부터 아침 6시, 내 앞에서 다시 시작한다.\"",
                Options = c => new List<StoryOption>
                {
                    O("매일 찾아간다", "칭찬은 한마디도 없었다. 대신 백무진의 메모가 매일 한 장씩 늘었다.",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 70)); e.Fatigue(20); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "dawn"); }),
                    O("\"제 방식대로 하겠습니다\"", "\"...그 고집, 결과로 보여라.\"",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(3); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rooftop", Title = "고3 여름",
                Text = c => "밤늦은 불펜. 우진이 혼자 장비를 정리하고 있었다.\n" +
                            (c.Me.IsPitcher ? "\"너 요즘 공이 겁먹었어.\"\n" : "\"너 요즘 스윙이 겁먹었어.\"\n") +
                            "\"나는 1순위 부담 같은 거 몰라. 근데 네가 고3 때 얼마나 즐겁게 야구했는지는 알아.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"그때처럼 같이 하자\"", "탕, 탕. 고3 여름의 소리가 났다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("속마음을 털어놓는다", "처음으로 1순위가 무섭다고 말했다. 우진은 그냥 고개만 끄덕였다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 10); }),
                    O("\"넌 몰라\"", "우진이 장비를 내려놓고 나갔다. 말이 너무 심했다.",
                        (e, k) => { e.Morale(-1); ChangeBond(e, k, Bond.Friend, -10); }),
                },
            },
            new StoryScene
            {
                Slot = "breakthrough", Title = "1순위가 아니라 {name}",
                Text = c => "오늘은 숫자가 보이지 않았다. 1순위도, 계약금도, 댓글도.\n그저 야구만 남았다.\n\n" +
                            "소이 채널 새 영상 제목, '드디어 진짜가 왔다'.\n" +
                            (c.Story.Has("dawn") ? "아침 6시의 메모들이 떠올랐다.\n" : "") +
                            "백무진 감독이 지나가며 어깨를 한 번 쳤다. 처음이었다.",
                Options = c => new List<StoryOption>
                {
                    O("\"야구가 다시 재밌다\"", "그날 밤 오랜만에 푹 잤다.",
                        (e, k) => { e.Morale(6); e.Fans(30); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("감독실을 찾아간다", "\"한 경기로 찾아오지 마라.\" 백무진의 책상에 오늘 경기 기록지가 놓여 있었다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new StoryScene
            {
                Slot = "spot", Title = "진짜 1순위",
                Text = c => "시즌 전 미팅. 백무진 감독이 선수단 앞에 섰다.\n\"올해 우리 팀 {spot}, {name}.\"\n" +
                            (c.Story.Has("dawn") ? "\"매일 아침 6시에 오던 놈이다. 주장도 맡긴다.\"\n" : "") +
                            "\n회의실이 조용해졌다. 처음으로, 1순위라서가 아니라 실력으로 받은 자리였다.",
                Options = c => new List<StoryOption>
                {
                    O("\"맡겨 주십시오\"", "그날 밤 소이 채널에 '진짜 1순위의 시작'이라는 영상이 올라왔다.",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"아직 부족합니다\"", "\"부족한 걸 아는 놈이 그 자리에 서야 한다.\"",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(2); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "injury", Title = "우진의 테스트",
                Text = c => "우진에게 기회가 왔다. 정식 선수 전환 테스트.\n그런데 테스트 일주일 전, 우진의 손가락이 부러졌다.\n\n" +
                            "병원 복도에서 우진이 웃었다.\n\"원래 나 같은 놈한테는 이런 게 안 오나 봐.\"",
                Options = c => new List<StoryOption>
                {
                    O("매일 같이 재활한다", "훈련이 끝나면 매일 재활실에 갔다. 우진이 다시 장비를 챙기기 시작했다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 20); Flag(k, "visited"); }),
                    O("\"내년엔 무조건 된다\"", "우진이 고개를 들었다. \"...그래. 내년.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("감독에게 테스트 연기를 부탁한다", "백무진은 규정은 규정이라며 고개를 저었다. 그래도 우진은 그 마음을 알았다.",
                        (e, k) => { ChangeBond(e, k, Bond.Friend, 10); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "race", Title = "2순위와의 결판",
                Text = c => "시즌 막바지, 타이틀 레이스. 상대는 {rivalTeam}의 {rival}.\n드래프트 날 내 바로 뒤에 불렸던 이름.\n\n" +
                            "경기 전 {rival} 선수가 먼저 다가왔다.\n\"1순위, 이번엔 확실하게 정리하자.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"정리는 내가 한다\"", "그날 경기, 공 하나하나에 온 힘을 실었다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); e.Morale(4); }),
                    O("\"좋은 승부 하자\"", "{rival} 선수가 손을 내밀었다. 처음 맞잡은 손이었다.",
                        (e, k) => { e.Morale(5); e.Fans(30); Flag(k, "rival_respect"); }),
                },
            },

            // ----- 4. The summit -----
            new StoryScene
            {
                Slot = "summit", Title = "10년에 한 번",
                Text = c => (HasSummit(c)
                                ? "시상식. '10년에 한 번 나올 재능'이라던 말이, 이제야 사실이 됐다.\n객석에는 백무진 감독, 우진, 그리고 처음 정장을 입은 어머니.\n\n" +
                                  "소이가 구독자 50만 기념 인터뷰를 요청했다.\n\"그때 그 댓글들, 지금은 어때요?\""
                                : "스물여덟. 트로피는 아직이다.\n그래도 '1순위치고는'이라는 말은 이제 아무도 하지 않는다.\n\n" +
                                  "소이가 인터뷰를 요청했다.\n\"아직 증명할 게 남았나요?\""),
                Options = c => new List<StoryOption>
                {
                    O("\"고마운 댓글만 기억해요\"", "영상 제목은 '1순위, 이제는 웃는다'였다.",
                        (e, k) => { e.Morale(5); e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("어머니를 무대로 모신다", "어머니가 마이크 앞에서 한마디 했다. \"우리 애, 밥은 잘 먹어요.\" 장내가 웃음바다가 됐다.",
                        (e, k) =>
                        {
                            e.Fans(40);
                            ChangeBond(e, k, Bond.Family, 10);
                            ChangeBond(e, k, Bond.Mentor, 5);
                            ChangeBond(e, k, Bond.Friend, 5);
                            ChangeBond(e, k, Bond.Voice, 5);
                        }),
                },
            },
            new StoryScene
            {
                Slot = "family", Title = "어머니의 가게",
                Text = c => "어머니가 분식집을 닫는다고 했다.\n\"이제 나도 좀 쉬어야지. 대신 네 경기는 다 볼 거다.\"\n\n" +
                            "가게 마지막 날, 단골들이 줄을 섰다. 벽에는 신인 시절 사진이 걸려 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("마지막 날 가게 일을 돕는다", "앞치마를 두르고 떡볶이를 저었다. 손님들이 사인 대신 계산을 부탁했다.",
                        (e, k) => { e.Fatigue(10); ChangeBond(e, k, Bond.Family, 15); }),
                    O("새 집을 선물한다 (500만원)", "어머니는 울면서 계약서에 도장을 찍었다.",
                        (e, k) => { e.Money(-500); ChangeBond(e, k, Bond.Family, 10); }),
                    O("\"떡볶이 레시피 알려 줘\"", "어머니가 웃었다. \"그건 은퇴하면 알려 줄게.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Family, 10); }),
                },
            },

            // ----- 5. Veteran -----
            new StoryScene
            {
                Slot = "body", Title = "무게가 바뀌었다",
                Text = c => "서른둘. 1순위라는 말 대신 '베테랑'이라는 말이 붙었다.\n어제와 같은 공이 조금 늦게 보인다.\n\n" +
                            "은퇴를 앞둔 백무진 감독이 말했다.\n\"재능으로 버티는 시간은 끝났다. 이제부터는 머리로 해라.\"",
                Options = c => new List<StoryOption>
                {
                    O("요령을 익힌다", "힘 대신 경험으로 싸우는 법을 배우기 시작했다.",
                        (e, k) => { e.Exp(Stat.Eye, 60); e.Fatigue(-20); }),
                    O("\"재능은 아직 남았습니다\"", "훈련량을 늘렸다. 몸이 비명을 질렀지만 버텼다.",
                        (e, k) =>
                        {
                            e.Exp(Stat.Power, 60);
                            e.Fatigue(25);
                            if (e.MaybeInjure(0.15, 3, InjuryKind.Hamstring)) e.Say("...결국 허벅지가 탈이 났다.");
                        }),
                    O("후배들에게 메모를 나눠 준다", "백무진에게 받았던 메모가 이제 다음 사람에게 건너갔다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "friend", Title = "우진의 자리",
                Text = c => c.Story.Has("visited")
                    ? "우진의 이름이 1군 엔트리에 올라갔다. 서른둘의 늦깎이 데뷔.\n\n첫 출전, 홈 플레이트 뒤에 앉은 우진이 미트를 두드렸다.\n\"이제야 같은 그라운드다.\""
                    : "우진이 구단을 떠난다고 했다. 고향 고등학교 야구부 코치 자리가 났다고.\n\n\"너 덕분에 오래 버텼다. 이제 내 제자들한테 네 얘기 해 줄게.\"",
                Options = c => c.Story.Has("visited")
                    ? new List<StoryOption>
                    {
                        O("고개를 끄덕여 답한다", "고3 여름처럼 사인을 주고받았다.",
                            (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 10); Flag(k, "friend_back"); }),
                        O("\"늦었잖아\"", "\"원래 좋은 포수는 늦게 핀다.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "friend_back"); }),
                    }
                    : new List<StoryOption>
                    {
                        O("송별회를 연다", "고교 동창들이 다 모였다. 우진이 끝까지 웃었다.",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 10); }),
                        O("\"제자들 데리고 경기 보러 와\"", "다음 달, 고등학생 스무 명이 외야석에 앉아 있었다.",
                            (e, k) => { e.Fans(20); e.Morale(4); ChangeBond(e, k, Bond.Friend, 5); }),
                    },
            },
            new StoryScene
            {
                Slot = "family_game", Title = "어머니의 첫 직관",
                Text = c => "어머니는 한 번도 야구장에 온 적이 없었다. 떨려서 못 보겠다고.\n\n" +
                            "그런데 오늘, 포수 뒤 관중석에 어머니가 앉아 있었다.\n옆자리에서는 소이가 카메라를 들고 해설을 해 주고 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("전력을 다한다", "경기가 끝나자 어머니가 벌떡 일어나 소리쳤다. \"우리 애다!\"",
                        (e, k) => { e.Morale(5); e.Fans(30); ChangeBond(e, k, Bond.Family, 10); }),
                    O("경기 후 소이 채널에 함께 나간다", "그 영상은 소이 채널 역대 최고 조회수를 찍었다.",
                        (e, k) => { e.Fans(40); ChangeBond(e, k, Bond.Family, 5); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },

            // ----- 6. The last season -----
            new StoryScene
            {
                Slot = "last", Title = "마지막 대화",
                Text = c => "서른일곱의 봄. 은퇴한 백무진이 캠프를 찾아왔다.\n\"1순위로 왔을 때 내가 뭐라고 했는지 기억하냐.\"\n\n" +
                            "\"1순위든 꼴찌든 똑같이 시작한다고. 그런데 너는 끝을 남들과 다르게 하겠구나.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"끝까지 달리겠습니다\"", "\"그래야 내 선수지.\"",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("고개 숙여 인사한다", "백무진이 처음으로 소리 내어 웃었다. 주름이 깊었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 15); }),
                },
            },
        };
    }
}
