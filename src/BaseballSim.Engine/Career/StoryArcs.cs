using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>
    /// The thread that runs through each story from the first wall to the last game: something the
    /// mentor kept quiet (a hint in chapter 2, the truth in chapter 3), a crisis at the top of the
    /// career, a last choice that hands something on, and the story's own telling of the last season.
    /// What was chosen along the way changes later scenes and the ending's epilogue.
    /// </summary>
    internal static class StoryArcs
    {
        static bool Close(Career c, Bond b) => c.Story.BondOf(b) >= 70;

        // =====================================================================
        // 마지막 순번: 최강훈 staked his job on the last pick, because twenty years ago he was one.
        // =====================================================================

        /// <summary>최강훈 kept his job through the crisis: someone stood up for him, or he was close enough to trust.</summary>
        internal static bool CoachStayed(Career c) => c.Story.Has("stood_up") || c.Story.Has("prove_it") && Close(c, Bond.Mentor);

        public static List<StoryScene> LastPick() => new List<StoryScene>
        {
            new StoryScene
            {
                Slot = "arc_hint", Title = "각서",
                Text = c => "2군 구장 장비실. 오래 일한 장비 담당 아저씨가 공을 닦다가 말을 꺼냈다.\n" +
                            "\"너 그거 아냐? 드래프트 날 스카우트 팀은 전부 너 반대했어.\"\n\n" +
                            "\"최 코치가 단장실에 들어가서 각서를 썼다더라. '이 녀석 1군 주전 못 만들면 내가 옷 벗겠다'고.\"",
                Options = c => new List<StoryOption>
                {
                    O("코치에게 직접 묻는다", "최강훈은 대답 대신 펑고 배트를 내밀었다.\n\"쓸데없는 소리 들을 시간에 공이나 받아.\"\n그날 펑고는 평소보다 두 배 길었다.",
                        (e, k) => { e.Exp(Stat.Defense, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "pledge_known"); }),
                    O("모른 척하고 이를 악문다", "그날 밤, 숙소에 불이 꺼진 뒤에도 혼자 배트를 돌렸다. 이제 내 야구에 걸린 게 나 하나가 아니다.",
                        (e, k) => { e.Exp(Stat.Contact, 50); e.Fatigue(15); Flag(k, "pledge_known"); }),
                    O("도현에게 털어놓는다", "도현이 한참 말이 없다가 내 어깨를 쳤다.\n\"그럼 더 올라가야지. 코치님 옷 벗게 하면 내가 가만 안 둔다.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Friend, 10); Flag(k, "pledge_known"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_reveal", Title = "스무 해 전의 마지막 순번",
                Text = c => "한소라가 누렇게 바랜 신문 한 장을 내밀었다.\n\"자료실에서 찾았어요. 20년 전 드래프트 기사.\"\n\n" +
                            "맨 아래 귀퉁이. '마지막 순번 지명, 최강훈'.\n그리고 1년 뒤의 짧은 한 줄. '최강훈, 방출'.\n\n" +
                            "한소라가 조용히 말했다.\n\"코치님이 왜 그렇게까지 했는지, 이제 알 것 같아요.\"",
                Options = c => new List<StoryOption>
                {
                    O("{mentor:을} 찾아간다", "최강훈은 신문을 한참 내려다봤다.\n\"그때 나한테 두 번째 기회를 준 사람은 아무도 없었다. 너한테는 주고 싶었다. 그게 다다.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "mentor_truth"); }),
                    O("기사로 쓰지 말아 달라고 한다", "한소라가 수첩을 덮었다.\n\"원래 안 쓰려고 했어요. 이건 기사가 아니라 두 사람 이야기니까.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "sora_kept"); }),
                    O("\"써 주세요. 끝은 제가 쓸게요\"", "다음 날 기사 제목은 '두 개의 마지막 순번'.\n최강훈은 신문을 구겼다. 그런데 그 신문이 다시 펴진 채 코치실 서랍에 들어 있었다.",
                        (e, k) => { e.Fans(40); ChangeBond(e, k, Bond.Voice, 5); ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "story_out"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_crisis", Title = "경질설",
                Text = c => "팀이 길게 흔들리고 있다.\n아침 신문 1면. '{team}, {mentor} 경질 검토'.\n\n" +
                            "라커룸이 조용했다. 몇몇 선수는 벌써 다음 감독 이야기를 하고 있었다.\n" +
                            (c.Story.Has("pledge_known") ? "각서 이야기가 떠올랐다. 그때도 그는 나 때문에 모든 걸 걸었다.\n" : "") +
                            "\n최강훈은 평소처럼 펑고 배트를 들고 그라운드에 나와 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("선수단 앞에서 감독을 지지한다", "\"감독님 바꿀 시간에 우리가 바뀌면 됩니다.\"\n라커룸이 잠깐 조용해졌다가, 누군가 박수를 쳤다. 그날 밤 기사가 바뀌었다. '선수단, 감독 지지 선언'.",
                        (e, k) => { e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "stood_up"); }),
                    O("\"결과로 말하겠습니다\"", "말 대신 야구로 답하기로 했다. 그날부터 훈련이 끝나면 혼자 남았다.",
                        (e, k) => { e.Exp(Stat.Power, Scaled(k, Bond.Mentor, 70)); e.Fatigue(15); Flag(k, "prove_it"); }),
                    O("내 야구에만 집중한다", "감독 일은 감독 일이다. 그렇게 생각하려 했다.",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Mentor, -10); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_choice", Title = "하준의 새벽",
                Text = c => "하준에게서 전화가 왔다. 목소리가 낮았다.\n\"형... 나 2군 내려왔어. 벌써 두 번째야.\"\n\n" +
                            "\"사람들이 그러더라. 형 동생이라서 뽑힌 거라고. 나 진짜 그만둘까 봐.\"\n\n" +
                            "어릴 적 크레파스 그림이 떠올랐다. 나란히 선 두 사람.",
                Options = c => new List<StoryOption>
                {
                    O("\"내일 새벽 5시, 2군 구장\"", c.Story.Has("dawn")
                            ? "다음 날 새벽, 2군 구장에 불이 켜져 있었다. 최강훈이 펑고 배트를 들고 기다리고 있었다.\n\"형이 하던 거, 동생도 해 보자.\""
                            : "다음 날 새벽, 둘이서 빈 구장에 섰다. 공을 주고받는 소리만 울렸다. 어릴 적 마당처럼.",
                        (e, k) => { e.Fatigue(15); e.Exp(Stat.Defense, 40); ChangeBond(e, k, Bond.Family, 15); if (k.Story.Has("dawn")) ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "hajun_dawn"); }),
                    O("\"그 말 들을 시간에 하나 더 쳐\"", "하준이 전화를 끊었다. 사흘 뒤 문자가 왔다.\n'형 말 듣고 화나서 500개 쳤다. 손바닥 다 까졌다. 고마워.'",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Family, 5); Flag(k, "hajun_tough"); }),
                    O("밤에 찾아가 밥을 산다", "국밥 두 그릇. 하준은 말없이 먹다가 갑자기 웃었다.\n\"형도 이랬어? 마지막 순번 때.\"\n밤새 옛날 이야기를 했다.",
                        (e, k) => { e.Money(-30); e.Fatigue(-10); ChangeBond(e, k, Bond.Family, 10); Flag(k, "hajun_meal"); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "펑고 배트", Only = true,
                Text = c => CoachStayed(c)
                    ? "경질설은 그해 겨울 끝났다. 구단은 최강훈과 재계약을 발표했다.\n\n시즌 전 첫 훈련. 백발이 늘어난 최강훈이 여전히 펑고 배트를 들고 서 있었다.\n" +
                      "\"그때 네가 나서 준 거, 고맙다는 말은 안 한다. 대신 한 바퀴 더 뛰어라.\""
                    : "최강훈은 감독직에서 물러났다. 그리고 한 달 뒤, 다시 2군 코치로 돌아왔다.\n\n2군 구장. 그는 아무렇지 않게 신인들에게 펑고를 치고 있었다.\n" +
                      "\"원래 내 자리다. 너 같은 놈을 또 찾으려면 여기 있어야지.\"",
                Options = c => new List<StoryOption>
                {
                    O("한 바퀴 더 뛴다", "숨이 턱까지 찼다. 최강훈이 등을 돌린 채 웃는 게 보였다.",
                        (e, k) => { e.Exp(Stat.Speed, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("펑고를 받아 본다", "신인 시절처럼 흙바닥을 굴렀다. 최강훈이 마지막 공을 치고 말했다.\n\"아직 쓸 만하네.\"",
                        (e, k) => { e.Exp(Stat.Defense, 50); e.Morale(4); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "책 한 권", Only = true,
                Text = c => "한소라가 이제는 스포츠 데일리의 야구팀장이 됐다.\n그녀가 원고 뭉치를 내밀었다. 제목은 '마지막 순번'.\n\n" +
                            "한소라가 말했다.\n\"첫 인터뷰부터 지금까지 쓴 걸 모았어요. 책으로 내고 싶어요. 근데 마지막 장은 비워 뒀어요.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"지금 내세요\"", "책은 그해 겨울 스포츠 분야 1위가 됐다. 사인회 줄이 구장 밖까지 이어졌다.",
                        (e, k) => { e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "book_now"); }),
                    O("\"마지막 장은 은퇴하고 같이 써요\"", "한소라가 원고를 다시 가방에 넣었다.\n\"약속이에요. 그때까지 다치지 마요.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "book_after"); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "이번엔 내가 그렸어", Only = true,
                Text = c => "원정 숙소로 소포가 왔다. 하준의 글씨.\n안에는 그림 한 장이 들어 있었다. 크레파스가 아니라 사인펜으로 그린.\n\n" +
                            "{team} 유니폼을 입은 사람 둘이 나란히 서 있다. 이번엔 둘 다 키가 같았다.\n\n" +
                            "'형. 이번엔 내가 그렸어. " +
                            (c.Story.Has("hajun_dawn") ? "그때 새벽에 형이 불러 주지 않았으면 나 진작 그만뒀을 거야. " : c.Story.Has("hajun_meal") ? "그날 국밥 맛 아직도 기억해. " : "") +
                            "마지막 시즌, 끝까지 봐 줄게. — 하준'",
                Options = c => new List<StoryOption>
                {
                    O("그림을 사물함에 붙인다", "신인 시절 붙였던 크레파스 그림 옆에 나란히. 20년 차이가 나는 두 장.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Family, 10); }),
                    O("바로 전화를 건다", "\"뭐야, 그림 실력은 그대로네.\" 하준이 웃다가 울었다. 나도 그랬다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "마지막 이름", Only = true,
                Text = c => "마지막 경기. 라인업 카드를 받으러 감독실에 가니, " +
                            (CoachStayed(c) ? "최강훈 감독이 직접 카드를 들고 있었다.\n" : "2군에 있어야 할 최강훈이 카드를 들고 서 있었다. 감독이 오늘만 부탁했다고 했다.\n") +
                            "\n최강훈이 카드를 읽었다. 드래프트 날처럼.\n\"{name}.\"\n\n" +
                            (c.Story.Has("dohyeon_back") ? "더그아웃에는 도현이 있었다. 무릎 보호대를 찬 채로, 끝까지 같은 팀이었다.\n"
                                : "더그아웃 뒤 트레이너실에서 도현이 테이핑을 감아 주었다. \"마지막이니까 특별히 꽉 감았다.\"\n") +
                            "관중석 맨 앞줄에는 하준과 부모님, 그리고 수첩을 든 한소라.",
                Options = c => new List<StoryOption>
                {
                    O("\"마지막 순번, 다녀오겠습니다\"", "최강훈이 모자를 벗었다. 20년 전 버스 앞에서처럼 손을 내밀었다.\n\"후회는 한 번도 안 했다.\"",
                        (e, k) => { e.Morale(10); e.Fans(50); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "last_called"); }),
                    O("라인업 카드를 달라고 한다", "최강훈이 카드를 건넸다. 맨 위에 연필로 작게 적혀 있었다.\n'내 인생 최고의 지명.'",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "kept_card"); }),
                },
            },
            new StoryScene
            {
                Slot = "body", Title = "서른둘의 루틴",
                Text = c => "서른둘. 몸은 아직 멀쩡하다. 오히려 지금이 제일 잘 안다.\n그런데 라커룸에서 내가 제일 나이가 많아졌다.\n\n" +
                            "신인 하나가 쭈뼛거리며 다가왔다.\n\"선배님, 훈련 루틴 좀 따라 해도 될까요? 선배님처럼 오래 하고 싶어서요.\"",
                Options = c => new List<StoryOption>
                {
                    O("새벽 루틴을 같이 한다", "다음 날 새벽, 신인 셋이 구장 앞에 서 있었다. 최강훈이 그걸 보고 혀를 찼다.\n\"새벽 5시가 유행이 됐네.\"",
                        (e, k) => { e.Exp(Stat.Eye, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("관리하는 법부터 가르친다", "\"많이 하는 것보다 오래 하는 게 어렵다.\" 언젠가 들었던 말을 그대로 돌려줬다.",
                        (e, k) => { e.Fatigue(-20); e.Morale(4); }),
                    O("\"내 루틴은 비밀이다\"", "신인이 시무룩하게 돌아갔다. 대신 그날 훈련은 혼자 두 배로 했다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(15); }),
                },
            },
        };

        public static List<string> LastPickEpilogue(Career c)
        {
            var lines = new List<string>();
            if (c.Story.Has("story_out") || c.Story.Has("mentor_truth") || c.Story.Has("sora_kept"))
                lines.Add(CoachStayed(c)
                    ? "단장실 금고에는 낡은 각서 한 장이 아직 남아 있다. 최강훈은 그걸 한 번도 찢지 않았다."
                    : "2군 구장 코치실 벽에는 두 장의 기사가 붙어 있다. 20년 전의 마지막 순번과, 오늘의 마지막 순번.");
            if (c.Story.Has("book_after")) lines.Add("한소라의 책 '마지막 순번'은 은퇴 1년 뒤 나왔다. 마지막 장은 두 사람이 함께 썼다.");
            else if (c.Story.Has("book_now")) lines.Add("한소라는 '마지막 순번' 개정판을 냈다. 새로 붙은 마지막 장의 제목은 '끝까지'.");
            if (c.Story.Has("hajun_dawn")) lines.Add("하준은 지금도 새벽 5시에 구장에 나간다. 형한테 배운 거라고 했다.");
            return lines;
        }

        // =====================================================================
        // 천재의 무게: 백무진 once pushed a prodigy of his own, his son 백승호, until the arm gave out.
        // =====================================================================

        public static List<StoryScene> Prodigy() => new List<StoryScene>
        {
            new StoryScene
            {
                Slot = "arc_hint", Title = "뒤집힌 액자",
                Text = c => "감독실 책상 위에 액자 하나가 있었다.\n1순위 모자를 쓴 앳된 선수. 얼굴이 어딘가 백무진을 닮았다.\n\n" +
                            "눈길이 머문 걸 본 백무진이 말없이 액자를 엎어 놓았다.\n\"그리고 너, 오늘 추가 훈련 금지다. 투구 수, 스윙 수 전부 보고해.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"누구예요?\"", "백무진은 대답하지 않았다.\n\"나가 봐.\"\n문을 닫는데 액자를 다시 세우는 소리가 들렸다.",
                        (e, k) => { ChangeBond(e, k, Bond.Mentor, -3); Flag(k, "photo_seen"); }),
                    O("시키는 대로 쉰다", "몸은 쉬었는데 머리는 계속 그 액자를 떠올렸다.",
                        (e, k) => { e.Fatigue(-20); ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "photo_seen"); }),
                    O("우진에게 물어본다", "우진이 잠깐 생각하다 말했다.\n\"고교 때 들은 적 있어. 백승호라고, 15년 전 1순위. 갑자기 사라졌대.\"",
                        (e, k) => { ChangeBond(e, k, Bond.Friend, 5); Flag(k, "photo_seen"); Flag(k, "seungho_name"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_reveal", Title = "15년 전 1순위",
                Text = c => "소이에게서 메시지가 왔다. 오래된 경기 영상 링크와 함께.\n\n" +
                            "15년 전 1순위 신인, 백승호. 3년 동안 팔꿈치 수술 두 번. 스물셋에 은퇴.\n당시 투수코치는 그의 아버지, 백무진이었다.\n\n" +
                            "소이가 다시 메시지를 보냈다.\n\"이거 올리면 조회수 엄청날 거예요. 근데 안 올릴게요. 선수님이 정해 주세요.\"",
                Options = c => new List<StoryOption>
                {
                    O("감독실 문을 두드린다", "백무진은 처음으로 오래 말했다.\n\"내가 그 애를 망쳤다. 재능이 아까워서 더, 더 시켰다. 그래서 너한테는 천재라는 말 안 쓴다. 다시는.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "mujin_open"); }),
                    O("백승호를 찾아간다", "동네 배팅센터. 카운터에 앉아 있던 백승호가 나를 알아보고 웃었다.\n\"아버지 팀 1순위구나. 아버지... 잘 부탁해요. 나한테는 못 했던 걸 너한테는 하고 있을 거예요.\"",
                        (e, k) => { e.Exp(Stat.Eye, 40); Flag(k, "seungho_met"); }),
                    O("소이에게 영상을 지워 달라고 한다", "소이가 바로 답장했다.\n\"벌써 지웠어요. 저 그런 채널 아니에요.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "soi_kept"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_crisis", Title = "팔의 신호",
                Text = c => (c.Me.IsPitcher ? "공을 던질 때마다 팔꿈치 안쪽이 찌릿했다.\n" : "스윙할 때마다 손목이 시큰거렸다.\n") +
                            "큰 부상은 아니라고 했다. 하지만 팀은 순위 싸움 한가운데였다.\n\n" +
                            "백무진이 의무 기록지를 책상에 내려놓았다.\n\"열흘 쉰다. 이건 상의가 아니다.\"\n" +
                            (c.Story.Has("mujin_open") || c.Story.Has("seungho_met") ? "\n그 목소리가 조금 떨리고 있었다. 15년 전이 겹쳐 보였다." : ""),
                Options = c =>
                {
                    var list = new List<StoryOption>
                    {
                        O("감독 말대로 쉰다", "열흘 동안 TV로 팀 경기를 봤다. 백무진은 매일 저녁 짧은 문자를 보냈다. '오늘도 쉬었냐.'",
                            (e, k) => { e.Fatigue(-40); e.Morale(-2); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "rested"); }),
                        O("\"뛰겠습니다\"", "주사를 맞고 경기에 나갔다. 경기는 이겼다. 그런데 더그아웃의 백무진은 한 번도 웃지 않았다.",
                            (e, k) =>
                            {
                                e.Fans(40);
                                e.Exp(Stat.Power, 50);
                                ChangeBond(e, k, Bond.Mentor, -10);
                                Flag(k, "pushed");
                                if (e.MaybeInjure(0.4, 12, InjuryKind.Shoulder)) e.Say("...며칠 뒤, 결국 팔이 올라가지 않았다.");
                            }),
                    };
                    if (c.Story.Has("seungho_met"))
                        list.Add(O("백승호에게 전화한다", "백승호가 낮게 말했다.\n\"쉬어. 나는 그때 아무도 말려 주지 않았어. 너한텐 말려 주는 사람이 있잖아.\"\n전화를 끊고 감독실에 가서 고개를 숙였다.",
                            (e, k) => { e.Fatigue(-40); e.Morale(4); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "rested"); Flag(k, "seungho_call"); }));
                    return list;
                },
            },
            new StoryScene
            {
                Slot = "arc_choice", Title = "아버지와 아들",
                Text = c => "백무진이 올 시즌을 끝으로 물러난다고 발표했다.\n\n" +
                            (c.Story.Has("seungho_met")
                                ? "배팅센터의 백승호가 말했다.\n\"15년 동안 아버지랑 밥 한 번 안 먹었어요. 이제 와서 뭐라고 해야 할지 모르겠어요.\""
                                : "소이가 조심스럽게 말했다.\n\"백승호 씨, 감독님 은퇴 소식 듣고 구장 앞까지 왔다가 그냥 갔대요.\""),
                Options = c => new List<StoryOption>
                {
                    O("두 사람을 시구 행사에 세운다", "은퇴 경기 시구. 마운드에는 백승호, 포수 자리에는 백무진.\n공은 원바운드였다. 백무진이 그 공을 오래 쥐고 있었다.",
                        (e, k) => { e.Fans(40); ChangeBond(e, k, Bond.Mentor, 15); ChangeBond(e, k, Bond.Voice, 5); Flag(k, "reunion"); }),
                    O("감독님께 배팅센터 주소만 건넨다", "백무진은 쪽지를 받고 아무 말도 하지 않았다.\n일주일 뒤, 배팅센터 앞에 감독의 차가 서 있었다는 이야기를 들었다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "address"); }),
                    O("남의 가족 일이다", "끼어들지 않기로 했다. 그게 맞는지는 끝내 알 수 없었다.",
                        (e, k) => e.Morale(1)),
                },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "마지막 메모", Only = true,
                Text = c => "백무진의 은퇴식. 그는 마이크를 잡고 짧게 말했다.\n\"감독으로서 잘한 건 별로 없습니다. 다만 한 선수를 망치지 않은 건 다행입니다.\"\n\n" +
                            (c.Story.Has("reunion") ? "객석 맨 앞에 백승호가 앉아 있었다. 15년 만에 같은 자리였다.\n"
                                : c.Story.Has("address") ? "객석 뒤쪽에 모자를 눌러 쓴 백승호가 서 있었다.\n" : "") +
                            "\n행사가 끝나고, 백무진이 봉투 하나를 건넸다. 매일 아침 6시에 쓰던 메모의 마지막 장이었다.",
                Options = c => new List<StoryOption>
                {
                    O("그 자리에서 읽는다", "'네가 잘해서 기쁜 게 아니다. 네가 다치지 않고 여기까지 와서 기쁘다.'",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("마지막 시즌에 열겠다고 한다", "백무진이 고개를 끄덕였다.\n\"그래라. 그때까지 다치지 마라.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "memo_kept"); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "다큐멘터리 '무게'", Only = true,
                Text = c => "'야구하는 소이' 구독자 100만. 방송국에서 소이에게 연락이 왔다고 한다.\n\n" +
                            "소이가 기획서를 내밀었다. 제목은 '무게'.\n\"1순위 신인 때부터 찍어 둔 영상이 다 있어요. 근데 선수님이 싫다면 안 해요.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"악플 시절도 다 넣어 주세요\"", "다큐멘터리는 그해 최고 시청률을 찍었다. 마지막 장면은 댓글창을 닫던 신인 시절의 나였다.",
                        (e, k) => { e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "docu"); }),
                    O("\"어머니 이야기는 빼 주세요\"", "소이가 고개를 끄덕였다.\n\"알겠어요. 대신 떡볶이는 한 컷만 넣을게요.\"",
                        (e, k) => { e.Fans(30); ChangeBond(e, k, Bond.Voice, 5); ChangeBond(e, k, Bond.Family, 5); Flag(k, "docu"); }),
                    O("\"은퇴하고 찍어요\"", "소이가 웃었다.\n\"그럼 마지막 경기는 제가 제일 앞에서 찍을게요.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "어머니의 쪽지", Only = true,
                Text = c => "원정 숙소로 택배가 왔다. 반찬통 다섯 개. 신인 시절과 똑같았다.\n\n" +
                            "쪽지 한 장. '1순위 아니어도 된다고 했지. 근데 엄마는 네가 1순위라서 좋은 게 아니라, 네가 웃으면서 야구해서 좋았어.'\n\n" +
                            (c.Story.Has("memo_kept") ? "가방 깊숙한 곳에는 아직 열지 않은 백무진의 봉투도 있었다." : ""),
                Options = c =>
                {
                    var list = new List<StoryOption>
                    {
                        O("어머니께 전화한다", "\"엄마, 마지막 경기 보러 와.\" 어머니가 웃었다. \"떨려서 못 본다니까.\" 그리고 표를 두 장 달라고 했다.",
                            (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 15); }),
                    };
                    if (c.Story.Has("memo_kept"))
                        list.Add(O("백무진의 봉투를 연다", "'네가 잘해서 기쁜 게 아니다. 네가 다치지 않고 여기까지 와서 기쁘다.'\n몇 년을 기다린 한 줄이었다.",
                            (e, k) => { e.Morale(10); ChangeBond(e, k, Bond.Mentor, 15); }));
                    else
                        list.Add(O("반찬을 우진과 나눠 먹는다", "우진이 젓가락을 멈췄다.\n\"고3 때 그 맛이다. 너 이제 진짜 끝이구나.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 10); ChangeBond(e, k, Bond.Family, 5); }));
                    return list;
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "그냥 내 이름", Only = true,
                Text = c => "마지막 경기. 장내 아나운서가 이름을 불렀다.\n'10년에 한 번 나올 재능'도, '1순위'도 아니었다. 그냥 내 이름이었다.\n\n" +
                            (c.Story.Has("friend_back") ? "홈 플레이트 뒤에서 우진이 미트를 두드렸다. 고3 여름의 사인이 왔다.\n"
                                : "관중석 한쪽에서 우진의 제자들이 직접 만든 현수막을 흔들었다.\n") +
                            (c.Story.Has("reunion") ? "본부석에는 백무진과 백승호가 나란히 앉아 있었다.\n" : "본부석 맨 뒷줄, 백무진이 팔짱을 끼고 앉아 있었다.\n") +
                            "그리고 포수 뒤 관중석, 어머니 옆자리에서 소이가 카메라를 들고 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("모자를 벗어 인사한다", "구장 전체가 일어섰다. 어머니가 소리쳤다. \"우리 애다!\" 처음 직관 왔던 날처럼.",
                        (e, k) => { e.Morale(10); e.Fans(50); ChangeBond(e, k, Bond.Family, 10); }),
                    O("고3 때처럼 웃으며 들어간다", "소이의 카메라가 그 얼굴을 잡았다. 영상 제목은 '천재의 무게, 내려놓다'.",
                        (e, k) => { e.Morale(10); e.Fans(40); ChangeBond(e, k, Bond.Voice, 10); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "body", Title = "무게가 바뀌었다",
                Text = c => "서른둘. 몸은 그대로인데, 붙는 말이 바뀌었다. '1순위' 대신 '베테랑'.\n\n" +
                            "백무진이 지나가다 멈춰 섰다.\n\"재능 얘기는 이제 아무도 안 한다. 대신 네가 어떻게 버텼는지를 묻겠지. 대답할 준비 해 둬라.\"",
                Options = c => new List<StoryOption>
                {
                    O("후배들에게 메모를 나눠 준다", "백무진에게 받았던 메모가 이제 다음 사람에게 건너갔다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("관리 루틴을 새로 짠다", "투구 수, 스윙 수, 잠자는 시간까지 적었다. 백무진이 그 노트를 보고 처음으로 고개를 끄덕였다.",
                        (e, k) => { e.Fatigue(-20); e.Exp(Stat.Eye, 40); }),
                    O("\"재능은 아직 남았습니다\"", "훈련량을 늘렸다. 백무진은 말리지 않았다. 대신 매일 몸 상태를 직접 물었다.",
                        (e, k) => { e.Exp(Stat.Power, 60); e.Fatigue(20); }),
                },
            },
        };

        public static List<string> ProdigyEpilogue(Career c)
        {
            var lines = new List<string>();
            if (c.Story.Has("reunion")) lines.Add("백무진과 백승호는 지금 같은 동네 배팅센터에서 아이들을 가르친다. 투구 수는 아버지가, 웃는 법은 아들이 맡는다.");
            else if (c.Story.Has("address")) lines.Add("백무진은 일주일에 한 번 배팅센터에 들른다고 한다. 아직 말은 많지 않지만, 밥은 같이 먹는다.");
            if (c.Story.Has("rested")) lines.Add(c.Me.IsPitcher ? "그해 팔꿈치는 끝내 칼을 대지 않았다. 은퇴하는 날까지." : "그해 손목은 끝내 수술 없이 버텼다. 은퇴하는 날까지.");
            if (c.Story.Has("docu")) lines.Add("다큐멘터리 '무게'는 지금도 신인 교육 시간에 틀어 준다.");
            return lines;
        }

        // =====================================================================
        // 연습생 신화: 도봉구 was a walk-on too, thirty years ago, fed on credit at 할머니's soup shop.
        // =====================================================================

        /// <summary>The walk-on tryout survived the new owners: someone spoke up, paid for it, or 도봉구 was trusted enough.</summary>
        internal static bool TryoutSaved(Career c) => c.Story.Has("spoke_up") || c.Story.Has("fund") || Close(c, Bond.Mentor);

        public static List<StoryScene> Walkon() => new List<StoryScene>
        {
            new StoryScene
            {
                Slot = "arc_hint", Title = "화요일의 단골",
                Text = c => "쉬는 날 할머니 국밥집. 구석 자리에 낯익은 등이 보였다. 도봉구였다.\n\n" +
                            "할머니가 그릇을 내려놓으며 속삭였다.\n\"저 양반, 화요일마다 와서 특으로 먹고 늘 두 배로 놓고 가. 너 오기 훨씬 전부터.\"",
                Options = c => new List<StoryOption>
                {
                    O("가서 인사한다", "도봉구가 숟가락을 멈췄다.\n\"...국밥은 여기가 제일이다.\"\n그게 다였다. 그런데 계산은 내 것까지 되어 있었다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "regular"); }),
                    O("할머니께 더 묻는다", "할머니가 웃었다.\n\"옛날에 돈 없던 연습생이 하나 있었지. 맨날 외상이었어. 이름은... 비밀이다.\"",
                        (e, k) => { ChangeBond(e, k, Bond.Family, 10); Flag(k, "regular"); }),
                    O("모른 척 국밥만 먹는다", "도봉구가 먼저 일어나 나갔다. 나가면서 내 어깨를 한 번 쳤다.",
                        (e, k) => e.Morale(2)),
                },
            },
            new StoryScene
            {
                Slot = "arc_reveal", Title = "30년 전 연습생",
                Text = c => "이다온이 구단 문서고에서 찾은 서류를 들고 왔다. 30년 전 연습생 계약서.\n\n" +
                            "'연습생 도봉구. 1군 출전 0경기. 방출 사유, 기량 미달.'\n\n" +
                            "이다온이 서류를 내려놓았다.\n\"할머니 가게 벽에 걸린 옛날 사진 있잖아요. 컨테이너 앞에서 찍은 거. 거기 감독님도 있어요.\"",
                Options = c => new List<StoryOption>
                {
                    O("감독님 외상값을 대신 갚는다 (100만원)", "할머니가 장부를 꺼냈다. 30년 묵은 이름 옆에 줄이 그어졌다.\n그날 저녁 도봉구에게서 전화가 왔다.\n\"...쓸데없는 짓 했다. 고맙다.\"",
                        (e, k) => { e.Money(-100); ChangeBond(e, k, Bond.Mentor, 15); ChangeBond(e, k, Bond.Family, 5); Flag(k, "paid_debt"); }),
                    O("\"감독님 몫까지 1군에서 뛰겠습니다\"", "도봉구가 한참 하늘을 봤다.\n\"내 몫은 됐다. 네 몫이나 해. 그게 내 몫이다.\"",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "carry_bonggu"); }),
                    O("서류를 액자에 넣자고 한다", "이다온이 웃었다.\n\"제가 할게요. 운영팀 벽에 걸어 둘게요. 연습생 테스트 날 다들 보게.\"",
                        (e, k) => { e.Fans(20); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "framed"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_crisis", Title = "연습생 테스트 폐지",
                Text = c => "구단 주인이 바뀌었다. 새 경영진의 첫 발표.\n'비용 절감을 위해 연습생 테스트 폐지, 2군 컨테이너 숙소 철거.'\n\n" +
                            "이다온이 굳은 얼굴로 말했다.\n\"도봉구 감독님 재계약도 보류래요. 연습생 제도 만든 사람이니까.\"",
                Options = c => new List<StoryOption>
                {
                    O("공개 인터뷰로 반대한다", "\"저는 연습생 테스트가 없었으면 여기 없습니다.\"\n인터뷰는 하루 만에 퍼졌다. 구장 앞에 '연습생 테스트 지켜 주세요' 현수막이 걸렸다.",
                        (e, k) => { e.Fans(50); e.Morale(4); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "spoke_up"); }),
                    O("연봉 일부를 연습생 기금으로 (500만원)", "이다온이 서류를 들고 경영진 회의에 들어갔다. 나올 때 그녀가 엄지를 들었다.",
                        (e, k) => { e.Money(-500); ChangeBond(e, k, Bond.Mentor, 10); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "fund"); }),
                    O("말 대신 성적으로 보여 준다", "연습생 출신이 얼마나 할 수 있는지, 숫자로 보여 주기로 했다.",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 70)); e.Fatigue(15); Flag(k, "prove_it"); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_choice", Title = "다음 연습생",
                Text = c => (TryoutSaved(c)
                                ? "연습생 테스트는 살아남았다. 올해 뽑힌 건 한 명. 열아홉 살, 말이 없는 아이.\n"
                                : "연습생 테스트는 1년을 쉬었다가 겨우 돌아왔다. 올해 뽑힌 건 한 명. 열아홉 살, 말이 없는 아이.\n") +
                            "\n도봉구가 전화했다.\n\"일주일만 그 녀석이랑 같이 지내 봐라. 너 처음 왔을 때랑 똑같은 눈이다.\"",
                Options = c => new List<StoryOption>
                {
                    O("2군 숙소에서 일주일 지낸다", "2층 침대 아래 칸. 컵라면 두 개에 물을 부었다.\n\"먹고 하자. 신화 한번 써 보자고.\" 반석에게 들었던 말을 그대로 했다.",
                        (e, k) => { e.Fatigue(15); e.Morale(8); ChangeBond(e, k, Bond.Mentor, 10); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "next_walkon"); }),
                    O("장비 한 세트를 선물한다 (200만원)", "아이가 새 글러브를 끌어안고 한참 고개를 숙였다. 나도 처음엔 남이 쓰던 글러브였다.",
                        (e, k) => { e.Money(-200); e.Fans(20); ChangeBond(e, k, Bond.Mentor, 5); Flag(k, "gave_gear"); }),
                    O("\"지금은 내 시즌이 먼저예요\"", "도봉구가 잠깐 말이 없다가 전화를 끊었다.\n\"...그래. 그것도 맞다.\"",
                        (e, k) => { e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "외상 장부", Only = true,
                Text = c => "도봉구가 올해를 끝으로 유니폼을 벗는다고 했다.\n마지막 화요일, 할머니 국밥집 구석 자리.\n\n" +
                            (c.Story.Has("paid_debt") ? "도봉구가 장부를 펼쳤다. 30년 전 이름 옆에 그어진 줄.\n\"이거 보려고 30년 버틴 것 같다.\"\n"
                                : "할머니가 낡은 장부를 꺼냈다. 30년 전 외상이 그대로 적혀 있었다.\n\"이건 안 받을란다. 대신 우리 손주 끝까지 봐 줘.\"\n") +
                            (TryoutSaved(c) ? "\n도봉구가 덧붙였다.\n\"연습생 테스트, 이름이 바뀐다더라. '도봉구 테스트'. 창피해 죽겠다.\"" : ""),
                Options = c => new List<StoryOption>
                {
                    O("국밥 두 그릇을 시킨다", "도봉구가 특을 시키고 두 배를 놓았다. 30년 동안 그래 왔던 것처럼.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); ChangeBond(e, k, Bond.Family, 5); }),
                    O("\"마지막 펑고 쳐 주세요\"", "밤의 2군 구장. 도봉구가 펑고를 쳤다. 처음 테스트 날처럼 흙바닥을 굴렀다.",
                        (e, k) => { e.Exp(Stat.Defense, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "구단 50년사", Only = true,
                Text = c => "이다온이 이제 운영팀장이 됐다.\n그녀가 두꺼운 원고를 내밀었다. '구단 50년사'.\n\n" +
                            "이다온이 한 장을 펼쳤다.\n\"연습생 출신 이야기는 한 장으로 따로 쓰고 싶어요. 제목은 선수님이 정해 줘요.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"이름이 불리지 않은 사람들\"", "그 장에는 나와 반석, 30년 전의 도봉구, 그리고 테스트에서 떨어진 수백 명의 이야기가 실렸다.",
                        (e, k) => { e.Fans(40); ChangeBond(e, k, Bond.Voice, 10); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "chapter_named"); }),
                    O("\"컨테이너\"", "이다온이 웃었다.\n\"딱이네요. 표지 사진도 그걸로 할게요.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "chapter_named"); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "컵라면 한 박스", Only = true,
                Text = c => "숙소로 택배가 왔다. 컵라면 한 박스. 보낸 사람은 김반석.\n\n" +
                            (FriendCameBack(c) || c.Story.Has("friend_back")
                                ? "'다른 유니폼 입고 1군에서 뛰어 보니까 알겠더라. 네가 그 컨테이너에서 나 붙잡아 준 게 내 야구 인생 전부였다.'"
                                : "'가게 TV 앞에 손님들이랑 매일 모여서 본다. 연습생 둘이서 시작한 신화, 끝은 네가 써라.'") +
                            "\n\n'마지막 경기 날은 가게 문 닫는다. — 반석'",
                Options = c => new List<StoryOption>
                {
                    O("컵라면에 물을 붓는다", "혼자 먹는데 이상하게 둘이 먹던 맛이 났다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("마지막 경기 표를 보낸다", "반석에게서 답장이 왔다. '맨 앞줄이면 간다. 아니면 안 간다.' 맨 앞줄로 보냈다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "수고했다, 연습생", Only = true,
                Text = c => "마지막 경기. 구장 앞 광장에는 할머니의 국밥 천막이 서 있었다. 이다온이 구단에 부탁해 하루만 허락을 받았다고 했다.\n\n" +
                            "천막 구석 자리에서 도봉구가 국밥을 먹고 있었다. 그 옆에 반석이 앉아 있었다.\n" +
                            (c.Story.Has("next_walkon") ? "그리고 컨테이너에서 같이 지냈던 그 아이가, 이제 2군 유니폼을 입고 내 등번호를 달고 서 있었다.\n" : "") +
                            "\n경기 직전, 도봉구가 서류 한 장을 덮었다. 연습생 테스트 날처럼.\n\"수고했다, 연습생.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"마지막 테스트, 다녀오겠습니다\"", "도봉구가 웃었다. 30년 전 컨테이너 앞 사진 속 얼굴이었다.",
                        (e, k) => { e.Morale(10); e.Fans(50); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("할머니께 국밥 한 그릇을 받는다", "할머니가 그릇을 내밀었다.\n\"먹고 해라. 우리 손주 마지막 경기다.\"\n천막 안 사람들이 모두 박수를 쳤다.",
                        (e, k) => { e.Morale(10); e.Fatigue(-10); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },
            new StoryScene
            {
                Slot = "body", Title = "컨테이너에서 배운 것",
                Text = c => "서른둘. 몸은 아직 컨테이너 시절처럼 버틴다. 다만 이제 혼자 버티는 게 아니다.\n\n" +
                            "도봉구가 말했다.\n\"연습생 근성은 좋다. 이제는 그 근성을 남한테 나눠 줄 차례다.\"",
                Options = c => new List<StoryOption>
                {
                    O("2군 연습생들과 같이 훈련한다", "쉬는 날 2군 구장. 연습생들이 줄을 섰다. 펑고에 까진 손바닥은 다들 똑같았다.",
                        (e, k) => { e.Exp(Stat.Defense, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("오래 가는 루틴을 만든다", "할머니가 싸 준 보온병을 들고 다니기 시작했다. 아침마다 국물 한 컵.",
                        (e, k) => { e.Fatigue(-20); ChangeBond(e, k, Bond.Family, 5); }),
                    O("더 독하게 한다", "\"근성은 나눠 주는 게 아니라 보여 주는 거다.\" 새벽 훈련을 늘렸다.",
                        (e, k) => { e.Exp(Stat.Power, 60); e.Fatigue(20); }),
                },
            },
        };

        public static List<string> WalkonEpilogue(Career c)
        {
            var lines = new List<string>();
            lines.Add(TryoutSaved(c)
                ? "구단 연습생 테스트는 지금 '도봉구 테스트'라고 불린다. 매년 지원자가 늘고 있다."
                : "연습생 테스트는 한 해를 쉬고 돌아왔다. 다시는 없애지 못하게, 이다온이 규정집 맨 앞에 적어 두었다.");
            if (c.Story.Has("paid_debt")) lines.Add("할머니 국밥집 외상 장부에는 이제 이름이 하나도 없다.");
            if (c.Story.Has("next_walkon")) lines.Add("그해 컨테이너에서 함께 지낸 연습생은 3년 뒤 1군 무대를 밟았다. 첫 인터뷰에서 컵라면 이야기를 했다.");
            return lines;
        }

        // =====================================================================
        // Endings that follow the person the player ended up closest to.
        // =====================================================================

        /// <summary>The headline and first line for an ending that follows a person; null for the others.</summary>
        internal static string[] Opening(Career c, RetiredPlayer r, StoryEnding ending)
        {
            switch (c.Story.Route)
            {
                case StoryRoute.LastPick:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return new[] { "[스포츠 데일리 · 한소라] '마지막 순번, 마이크를 잡다'", "은퇴 이듬해 봄, 중계석에 낯익은 얼굴이 앉았다. 첫 중계의 첫마디는 이랬다. \"오늘도 마지막 순번으로 들어온 선수가 한 명 있죠.\"" };
                        case StoryEnding.Hometown: return new[] { "[스포츠 데일리 · 한소라] '마지막 순번, 고향으로'", "은퇴 후 고향으로 돌아갔다. 어릴 적 하준에게 공을 던져 주던 그 마당에 작은 연습장이 생겼다." };
                        case StoryEnding.Scout: return new[] { "[스포츠 데일리 · 한소라] '마지막 순번을 찾는 사람'", $"은퇴 후 {r.MainClub} 스카우트가 됐다. 첫 드래프트 날, 모두가 자리를 뜨던 마지막 순번에서 손을 들었다." };
                        case StoryEnding.Partner: return new[] { "[스포츠 데일리 · 한소라] '룸메이트, 다시 한 팀'", "은퇴 후 도현과 함께 유소년 야구단을 차렸다. 이름은 '마지막 순번 야구단'." };
                    }
                    break;
                case StoryRoute.Prodigy:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return new[] { "['야구하는 소이' 특집] '1순위, 해설석에 앉다'", "은퇴 후 첫 해설. 소이 채널 라이브에서 시작한 해설이 곧 방송국 중계로 이어졌다." };
                        case StoryEnding.Hometown: return new[] { "['야구하는 소이' 특집] '1순위, 분식집으로 돌아가다'", "은퇴 후 어머니의 분식집을 다시 열었다. 벽에는 신인 시절 1순위 유니폼이 걸렸다." };
                        case StoryEnding.Scout: return new[] { "['야구하는 소이' 특집] '천재를 지키는 사람'", $"은퇴 후 {r.MainClub} 육성 총괄을 맡았다. 첫 지시는 신인 투구 수와 훈련량 상한제였다." };
                        case StoryEnding.Partner: return new[] { "['야구하는 소이' 특집] '배터리, 다시 한 번'", "은퇴 후 우진과 함께 동네에 야구 아카데미를 열었다. 받는 건 우진, 던지는 건 나." };
                    }
                    break;
                default:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return new[] { "[구단 소식지 · 이다온] '컨테이너에서 중계석까지'", "은퇴 후 구단 중계 해설을 맡았다. 첫 방송에서 연습생 시절 컨테이너 사진을 띄웠다." };
                        case StoryEnding.Hometown: return new[] { "[구단 소식지 · 이다온] '국밥집의 두 번째 주인'", "은퇴 후 할머니 국밥집을 물려받았다. 메뉴판 맨 위에는 '연습생 특 · 외상 가능'." };
                        case StoryEnding.Scout: return new[] { "[구단 소식지 · 이다온] '200명 중 한 명을 찾는 사람'", "은퇴 후 연습생 테스트 심사위원 자리에 앉았다. 도봉구가 앉던 바로 그 자리였다." };
                        case StoryEnding.Partner: return new[] { "[구단 소식지 · 이다온] '연습생 둘이 쓴 신화'", "은퇴 후 반석과 함께 야구용품점을 넓혔다. 2층은 연습생 지망생을 위한 무료 연습장이다." };
                    }
                    break;
            }
            return null;
        }

        /// <summary>The last line for an ending that follows a person; null for the others.</summary>
        internal static string Closing(Career c, RetiredPlayer r, StoryEnding ending)
        {
            switch (c.Story.Route)
            {
                case StoryRoute.LastPick:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return "그라운드는 떠났지만 야구는 떠나지 않았다. 이제는 누군가의 첫 기록을 가장 먼저 불러 준다.";
                        case StoryEnding.Hometown: return "매일 저녁 동네 아이들과 캐치볼을 한다. 크레파스 그림 속 그 마당에서.";
                        case StoryEnding.Scout: return "\"너 뽑자고 우긴 사람이 나다.\" 오래전 들었던 그 말을, 이번엔 내가 했다.";
                        case StoryEnding.Partner: return "빨래 당번은 아직도 정하지 못했다.";
                    }
                    break;
                case StoryRoute.Prodigy:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return "악플을 견뎌 본 사람의 해설은, 언제나 선수 편에서 시작한다.";
                        case StoryEnding.Hometown: return "떡볶이 레시피는 결국 은퇴하고 나서 배웠다.";
                        case StoryEnding.Scout: return "백무진에게 배운 건 던지는 법이 아니라 멈추는 법이었다.";
                        case StoryEnding.Partner: return "고3 여름의 그 소리가, 오늘도 아카데미에 울린다.";
                    }
                    break;
                default:
                    switch (ending)
                    {
                        case StoryEnding.Broadcaster: return "연습생 테스트 날이면 중계석을 비우고 구장으로 내려간다. 떨고 있는 아이들 어깨를 두드려 주려고.";
                        case StoryEnding.Hometown: return "돈 없는 연습생은 오늘도 외상으로 먹고 간다. 장부에는 이름만 적는다.";
                        case StoryEnding.Scout: return "\"연봉은 최저, 숙소는 컨테이너. 그래도 하겠나?\" 이번엔 내가 물었다.";
                        case StoryEnding.Partner: return "컵라면은 여전히 1층 계산대 옆에 쌓여 있다.";
                    }
                    break;
            }
            return null;
        }
    }
}
