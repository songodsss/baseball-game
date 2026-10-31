using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>Story 1, "마지막 순번": the last pick of the draft, the coach who fought for him, a roommate, a little sibling and a rookie reporter.</summary>
    internal static class LastPickStory
    {
        public static StoryRouteDef Build() => new StoryRouteDef
        {
            Id = StoryRoute.LastPick,
            Name = "마지막 순번",
            Blurb = "드래프트 마지막 순번으로 겨우 프로에 온 신인이 리그의 전설을 꿈꾼다.",
            BondNames = new[] { "최강훈", "윤도현", "하윤", "한소라" },
            BondRoles = new[] { "나를 뽑은 코치", "동기 · 룸메이트", "동생", "스포츠 데일리 기자" },
            MentorBefore = "최강훈 코치",
            MentorAfter = "최강훈 감독",
            Scenes = Scenes(),
            Opening = (c, r, ending) =>
                ending == StoryEnding.Legend ? new[] { "[스포츠 데일리 · 한소라] '마지막 순번, 명예의 전당에 서다'", $"헌액식 연단에 선 {r.Name}. 준비한 원고는 접어 두고 한 사람씩 이름을 불렀다." }
                : ending == StoryEnding.OneClub ? new[] { $"[스포츠 데일리 · 한소라] '{r.MainClub}의 심장, 그라운드를 떠나다'", $"{r.Seasons}시즌 동안 {r.MainClub} 유니폼만 입었다. 마지막 경기, 외야석이 카드섹션으로 등번호를 그렸다." }
                : new[] { "[스포츠 데일리 · 한소라] '마지막 순번의 두 번째 출발'", "은퇴 1년 뒤, 고향 동네에 작은 유소년 야구 교실이 문을 열었다." },
            BondLine = (c, b, v) =>
            {
                switch (b)
                {
                    case Bond.Mentor:
                        return v >= 70 ? "백발이 된 최강훈이 맨 앞자리에서 모자를 벗었다. \"내 눈은 틀리지 않았다.\""
                            : v <= 30 ? "최강훈은 오지 않았다. 다음 날 짧은 문자 한 통이 왔다. '잘했다.'"
                            : "최강훈은 뒤쪽 자리에서 조용히 박수를 쳤다.";
                    case Bond.Friend:
                        return v >= 70 ? (c.Story.Has("bet") ? "도현이 소리쳤다. \"빨래 내기는 내가 졌다! 인정!\"" : "도현이 누구보다 크게 박수를 쳤다. 눈가가 붉었다.")
                            : v <= 30 ? "도현에게서는 꽃다발만 도착했다. 카드에는 '축하한다, 친구'."
                            : "도현이 손을 흔들었다. \"수고했다, 룸메이트.\"";
                    case Bond.Family:
                        return v >= 70 ? "프로 선수가 된 하윤이 유니폼 차림으로 꽃을 들고 올라왔다. 크레파스 그림 속 그 아이였다."
                            : v <= 30 ? "하윤은 경기 일정 때문에 영상 메시지를 보냈다. \"그래도 내 첫 번째 영웅이야.\""
                            : "하윤이 객석에서 휴대폰으로 사진을 찍고 있었다.";
                    default:
                        return v >= 70 ? "한소라는 첫 인터뷰 때의 수첩을 꺼내 보였다. 첫 장에 그날의 한마디가 적혀 있었다."
                            : "한소라의 기사는 짧았지만 정확했다.";
                }
            },
            Closing = (c, r, ending) =>
                ending == StoryEnding.Legend ? "마지막 순번으로 시작한 이야기는, 리그의 전설로 끝났다."
                : ending == StoryEnding.OneClub ? "한 팀에서 시작해 한 팀에서 끝난 이야기. 팬들은 오래도록 그 이름을 응원가로 불렀다."
                : $"화려한 기록은 없어도, {r.Name} 선수를 보고 야구를 시작한 아이들이 있다. 그거면 충분했다.",
        };

        static List<StoryScene> Scenes() => new List<StoryScene>
        {
            // ----- Prologue -----
            new StoryScene
            {
                Slot = "draft", Title = "마지막 이름",
                Text = c => "드래프트 날, 마지막 순번.\n다들 자리를 뜨던 그때, {team}의 테이블에서 이름이 불렸다.\n\n\"{name}.\"\n\n" +
                            "구단 버스 앞에서 한 사내가 손을 내밀었다. 2군 코치 최강훈.\n\"너 뽑자고 우긴 사람이 나다. 후회하게 만들지 마라.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"반드시 증명하겠습니다\"", "\"눈빛은 합격이다.\" 최강훈이 처음으로 웃었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"왜 저를 뽑으셨어요?\"", "\"고3 여름, 비 오는 날 혼자 남아서 배트 돌리던 놈. 그게 너였다.\"",
                        (e, k) => { e.Exp(Stat.Contact, 30); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "roommate", Title = "룸메이트",
                Text = c => "숙소 첫날 밤. 짐을 푸는데 문이 벌컥 열렸다.\n\"네가 {name}? 나 윤도현. 1라운드.\"\n\n동갑내기 룸메이트가 씩 웃으며 손을 내밀었다.\n" +
                            "\"마지막 순번이랑 1라운드라. 누가 먼저 1군 붙박이 되나 내기할래?\"",
                Options = c => new List<StoryOption>
                {
                    O("내기를 받는다", "\"진 사람이 1년 내내 빨래 담당이다.\" 둘은 새벽까지 떠들었다.",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "bet"); }),
                    O("\"같이 올라가자\"", "도현이 잠깐 멈칫하더니 웃었다. \"...그래. 둘 다 올라가면 되지.\"",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 10)),
                    O("피곤하다며 먼저 잔다", "도현은 머쓱하게 불을 껐다.",
                        (e, k) => { e.Fatigue(-10); ChangeBond(e, k, Bond.Friend, -5); }),
                },
            },

            // ----- 1. Debut -----
            new StoryScene
            {
                Slot = "first", Title = "첫 기록",
                Text = c => (HasFirstRecord(c)
                                ? "마침내 {first}.\n더그아웃에 돌아오자 선배들이 헬멧을 마구 두드렸다.\n\n"
                                : "아직 기록이 없다. {first}까지가 이렇게 멀 줄이야.\n그래도 매일 경기장에 나간다.\n\n") +
                            "경기 후, 수첩을 든 기자가 다가왔다.\n\"스포츠 데일리 한소라입니다. 저도 이번이 첫 취재예요. 한마디만 부탁드려요!\"",
                Options = c => new List<StoryOption>
                {
                    O("\"시작일 뿐입니다\"", "다음 날 신문 귀퉁이에 짧은 기사가 실렸다. '마지막 순번의 당찬 첫걸음'.",
                        (e, k) => { e.Morale(3); e.Fans(20); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("\"코치님 덕분입니다\"", "기사를 본 최강훈은 쓸데없는 소리라며 신문을 접었다. 그런데 그 신문이 코치실 벽에 붙었다.",
                        (e, k) => { ChangeBond(e, k, Bond.Mentor, 10); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("얼어붙는다", "한소라가 웃었다. \"괜찮아요, 저도 떨려요. 다음엔 길게 해 주세요.\"",
                        (e, k) => { e.Morale(1); ChangeBond(e, k, Bond.Voice, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "letter", Title = "동생의 그림",
                Text = c => "집에서 소포가 왔다. 크레파스로 그린 그림 한 장.\n{team} 유니폼을 입은 사람과, 그 옆에 작은 글러브를 낀 아이.\n\n" +
                            "'나도 리틀야구 들어갔어! 나중에 같이 뛰자! — 하윤'\n\n아홉 살 동생 하윤의 글씨다.",
                Options = c => new List<StoryOption>
                {
                    O("영상 통화를 건다", "화면 너머로 하윤이 새 글러브를 자랑했다. 통화를 끊고도 한참 웃음이 났다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 10); }),
                    O("그림을 사물함에 붙인다", "경기 전마다 그림을 보는 게 습관이 됐다.",
                        (e, k) => { e.Morale(3); e.Exp(Stat.Eye, 20); ChangeBond(e, k, Bond.Family, 5); }),
                    O("답장 대신 훈련한다", "'지금은 보여 줄 게 없다.' 배트를 들었다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rival", Title = "같은 해, 다른 유니폼",
                Text = c => "스포츠 데일리 1면, '올해의 신인 경쟁'.\n사진 속에는 나와 {rivalTeam}의 {rival}.\n같은 해 프로에 온 동갑내기. 1라운드 1순위였다고 한다.\n\n" +
                            "한소라의 기사 마지막 줄.\n'두 사람의 이야기는 이제 막 시작됐다.'",
                Options = c => new List<StoryOption>
                {
                    O("{rival}, 기억해 둔다", "그날 밤, 상대 기록을 처음으로 찾아봤다.",
                        (e, k) => { e.Exp(Stat.Power, 30); e.Morale(3); Flag(k, "rival_fire"); }),
                    O("신문을 접는다", "남과 비교하지 않기로 했다. 지금은 내 야구가 먼저다.",
                        (e, k) => e.Morale(4)),
                    O("도현에게 보여 준다", "\"야, 이 사람이 네 라이벌이래. 그럼 나는?\" 도현이 투덜거렸다.",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },

            // ----- 2. The first wall -----
            new StoryScene
            {
                Slot = "wall", Title = "첫 번째 벽",
                Text = c => c.InFarm
                    ? "2군 강등 통보.\n짐을 싸서 2군 구장에 도착하니, 낡은 펑고 배트를 든 사람이 기다리고 있었다.\n\n\"왔냐.\" 최강훈이었다.\n\"내일 새벽 5시. 늦으면 그냥 집에 가라.\""
                    : "몇 경기째 아무것도 되지 않는다.\n{mound}에 서는 게 무서워지기 시작했다.\n\n경기 후 주차장. 2군에 있어야 할 최강훈이 차에 기대 서 있었다.\n\"얼굴 보러 왔다. 내일 새벽 5시, 2군 구장. 올래?\"",
                Options = c => new List<StoryOption>
                {
                    O("새벽 5시에 간다", "첫날은 공 하나 제대로 못 쳤다. 사흘째, 최강훈이 처음으로 고개를 끄덕였다.",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 70)); e.Fatigue(20); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "dawn"); }),
                    O("혼자 해 보겠다고 한다", "\"...그래. 그것도 답이다.\" 최강훈은 말없이 돌아섰다.",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(3); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rooftop", Title = "옥상의 캔커피",
                Text = c => "숙소 옥상. 도현이 캔커피 두 개를 들고 올라왔다.\n\"너 요즘 얼굴 장난 아니다.\"\n\n한참 말이 없다가, 도현이 툭 던졌다.\n" +
                            "\"나도 1라운드라는 거 때문에 매일 무서워. 너만 그런 거 아니야.\"",
                Options = c => new List<StoryOption>
                {
                    O("속마음을 털어놓는다", "밤새 이야기했다. 어깨가 조금 가벼워졌다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("\"괜찮아\"라고만 한다", "도현은 더 묻지 않았다. 캔커피가 미지근해졌다.",
                        (e, k) => e.Morale(2)),
                    O("\"같이 야간 훈련 가자\"", "둘이서 불 꺼진 실내 연습장에 불을 켰다.",
                        (e, k) => { e.Exp(Stat.Power, 40); e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "breakthrough", Title = "벽을 넘는 날",
                Text = c => "오늘은 뭔가 달랐다.\n{mound}에서 몸이 먼저 움직였다. 머리가 아니라 몸이 기억하고 있었다.\n\n" +
                            (c.Story.Has("dawn") ? "새벽 5시의 2군 구장이 떠올랐다.\n" : "") +
                            "관중석 맨 위, 모자를 눌러 쓴 최강훈이 보였다.\n그는 엄지를 한 번 들어 보이고 자리를 떴다.",
                Options = c => new List<StoryOption>
                {
                    O("벽은 넘으라고 있는 거다", "어딘가에서 한소라가 이 장면을 수첩에 적고 있었다.",
                        (e, k) => { e.Morale(6); e.Fans(20); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("코치님께 전화한다", "\"한 경기 잘했다고 전화하냐.\" 목소리가 웃고 있었다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new StoryScene
            {
                Slot = "spot", Title = "이름이 불리는 자리",
                Text = c => "구단 발표. 새 감독은 최강훈.\n취임 첫날, 감독실로 불려 갔다.\n\n\"올해부터 네가 우리 팀 {spot}다.\"\n" +
                            (c.Story.Has("dawn") ? "\"새벽 5시 기억하지? 주장 완장도 같이 받아라.\"\n" : "") +
                            "\"마지막 순번으로 들어온 놈이 그 자리에 서는 거. 그거 보려고 여기까지 왔다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"맡겨 주십시오\"", "그날 밤, 사물함 이름표 위에 새 글씨가 붙었다.",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"부담됩니다\"", "\"부담 없는 자리는 없다. 그러니까 너다.\"",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(2); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "injury", Title = "도현의 부상",
                Text = c => (c.Me.IsPitcher ? "홈 플레이트에서 주자와 부딪힌 도현이 일어나지 못했다.\n" : "수비하던 도현이 그대로 쓰러졌다.\n") +
                            "들것이 들어오고, 경기장이 조용해졌다.\n\n진단은 무릎 인대 파열. 시즌 아웃, 어쩌면 그 이상.\n" +
                            "병실에서 도현이 천장만 보며 말했다.\n\"...야. 나 이제 끝난 걸까.\"",
                Options = c => new List<StoryOption>
                {
                    O("매일 병문안을 간다", "훈련이 끝나면 매일 병원에 갔다. 도현이 조금씩 다시 웃기 시작했다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 20); Flag(k, "visited"); }),
                    O("\"네 몫까지 뛴다\"", "그날 경기, 모자 안쪽에 도현의 등번호를 적었다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("아무 말도 못 한다", "말 대신 어깨를 짚었다. 도현이 눈을 감았다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 5)),
                },
            },
            new StoryScene
            {
                Slot = "race", Title = "타이틀 레이스",
                Text = c => "시즌 후반. 개인 타이틀 경쟁이 뜨겁다.\n그 한가운데에 나와 {rivalTeam}의 {rival}.\n\n" +
                            "경기 전 그라운드에서 {rival} 선수와 눈이 마주쳤다.\n\"이번엔 내가 이긴다.\" 입 모양만 봐도 알 수 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("\"덤벼\"", "그날 경기, 공 하나하나에 온 힘을 실었다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); e.Morale(4); }),
                    O("\"좋은 경기 하자\"", "{rival} 선수가 잠깐 놀라더니 모자를 살짝 들어 올렸다.",
                        (e, k) => { e.Morale(5); e.Fans(30); Flag(k, "rival_respect"); }),
                },
            },

            // ----- 4. The summit -----
            new StoryScene
            {
                Slot = "summit", Title = "정상",
                Text = c => (HasSummit(c)
                                ? "시상식. 무대 위에서 내려다본 객석에 아는 얼굴들이 보였다.\n최강훈, 지팡이를 짚은 도현, 그리고 부쩍 자란 하윤.\n\n"
                                : "스물여덟. 아직 트로피는 없다.\n그래도 이제 리그에서 내 이름을 모르는 사람은 없다.\n\n") +
                            "한소라가 이번엔 표지 인터뷰를 요청했다.\n\"마지막 순번에서 여기까지. 무슨 말을 하고 싶으세요?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"아직 멀었습니다\"", "표지 제목은 '아직 배고프다'였다.",
                        (e, k) => { e.Morale(5); e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("고마운 사람들 이름을 부른다", "기사에는 네 사람의 이름이 실렸다.",
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
                Slot = "family", Title = "하윤의 지명",
                Text = c => "올해 드래프트. 온 가족이 TV 앞에 모였다.\n그리고 3라운드에서, 그 이름이 불렸다.\n\n\"하윤.\"\n\n" +
                            "열여덟 살 동생이 화면 속에서 울고 있었다.\n곧 전화가 울렸다. \"나 이제 진짜 같은 리그야! 봐주는 거 없다?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"봐주는 거 없다\"", "그날 밤, 둘은 처음으로 선수 대 선수로 이야기했다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 10); }),
                    O("글러브를 선물한다 (200만원)", "어릴 적 크레파스 그림 속 그 글러브처럼.",
                        (e, k) => { e.Money(-200); ChangeBond(e, k, Bond.Family, 15); }),
                    O("프로는 다르다고 충고한다", "하윤이 잠깐 조용해졌다. \"...알아. 그러니까 지켜봐.\"",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },

            // ----- 5. Veteran -----
            new StoryScene
            {
                Slot = "body", Title = "몸이 먼저 안다",
                Text = c => "아침에 일어나는 데 시간이 걸린다.\n같은 훈련인데 회복이 하루 더 걸린다.\n\n트레이너가 조심스럽게 말했다.\n\"이제는 덜 하는 게 더 하는 거예요.\"",
                Options = c => new List<StoryOption>
                {
                    O("요령을 익힌다", "힘 대신 경험으로 싸우는 법을 배우기 시작했다.",
                        (e, k) => { e.Exp(Stat.Eye, 60); e.Fatigue(-20); }),
                    O("나이는 숫자다, 더 한다", "새벽 러닝을 늘렸다. 몸이 비명을 질렀지만 버텼다.",
                        (e, k) =>
                        {
                            e.Exp(Stat.Power, 60);
                            e.Fatigue(25);
                            if (e.MaybeInjure(0.15, 3, InjuryKind.Hamstring)) e.Say("...결국 허벅지가 탈이 났다.");
                        }),
                    O("{mentor}을 찾아간다", "\"나도 서른둘에 그랬다. 그때부터가 진짜 야구다.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "friend", Title = "도현의 선택",
                Text = c => c.Story.Has("visited")
                    ? "도현에게서 문자가 왔다.\n'재활 끝. 다음 주 1군 복귀. 너 병원 매일 왔던 거, 그거 갚으러 간다.'\n\n복귀전, 도현이 그라운드에 들어서자 관중이 모두 일어섰다."
                    : "도현의 은퇴식. 끝내 무릎은 예전으로 돌아오지 않았다.\n\n마이크를 잡은 도현이 관중석이 아니라 나를 봤다.\n" +
                      "\"제 몫까지 뛰어 준 친구가 있어서, 후회 없습니다.\"\n\n도현은 구단 트레이너로 남는다고 한다.",
                Options = c => c.Story.Has("visited")
                    ? new List<StoryOption>
                    {
                        O("달려가 끌어안는다", "둘 다 유니폼이 흙투성이가 됐다. 상관없었다.",
                            (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 10); Flag(k, "dohyeon_back"); }),
                        O("\"늦었잖아\"", "\"주인공은 원래 늦게 나오는 거야.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "dohyeon_back"); }),
                    }
                    : new List<StoryOption>
                    {
                        O("꽃다발을 건넨다", "도현이 꽃다발로 내 머리를 툭 쳤다. \"울지 마, 바보야.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 10); }),
                        O("\"네 몫은 아직 안 끝났어\"", "다음 날부터 도현이 짠 훈련표를 받았다.",
                            (e, k) => { e.Morale(8); e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Friend, 5); }),
                    },
            },
            new StoryScene
            {
                Slot = "family_game", Title = "하윤과의 맞대결",
                Text = c => (c.Me.IsPitcher ? "9회, 타석에 하윤이 들어섰다.\n" : "오늘 상대 마운드에 하윤이 서 있었다.\n") +
                            "어릴 적 마당에서 공을 던져 주던 기억이 스쳤다.\n\n하윤이 모자를 고쳐 쓰며 고개를 끄덕였다. 진짜 승부를 하자는 뜻이다.",
                Options = c => new List<StoryOption>
                {
                    O("전력으로 상대한다", "결과보다 하윤의 웃는 얼굴이 더 오래 기억에 남았다.",
                        (e, k) => { e.Morale(5); e.Fans(30); ChangeBond(e, k, Bond.Family, 10); }),
                    O("경기 후 같이 밥을 먹는다", "부모님께 보낼 사진을 찍었다. 둘 다 유니폼 차림으로.",
                        (e, k) => { e.Fatigue(-10); ChangeBond(e, k, Bond.Family, 10); }),
                },
            },

            // ----- 6. The last season -----
            new StoryScene
            {
                Slot = "last", Title = "마지막 대화",
                Text = c => "서른일곱의 봄, 스프링캠프 마지막 날.\n{mentor}이 불렀다. 자기도 올해가 마지막 시즌이라고 했다.\n\n" +
                            "\"마지막 순번으로 너 뽑던 날 말이다. 사람들이 다 웃었다.\"\n\"지금은 아무도 안 웃지.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"끝까지 달리겠습니다\"", "\"그래야 내가 뽑은 놈이지.\"",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("고개 숙여 인사한다", "최강훈이 등을 돌렸다. 어깨가 조금 떨리고 있었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 15); }),
                },
            },
        };
    }
}
