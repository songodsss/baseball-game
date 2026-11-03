using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>
    /// Story 4, "아버지의 등번호": the son of the team's retired legend (number 10, retired at 33 at his
    /// peak, "a knee" they said). The manager is the father's old teammate who never forgave him for
    /// leaving; a childhood friend from the father's baseball school; a radio caster who called the
    /// father's last game. The truth: he left to raise his son alone.
    /// </summary>
    internal static class LegacyStory
    {
        /// <summary>권태수 forgave the father and stayed by the son: the reunion, or the team reached the autumn in the crisis.</summary>
        internal static bool Reconciled(Career c) => c.Story.Has("reconciled") || StoryPromises.WasKept(c, "arc_crisis") && c.Story.BondOf(Bond.Mentor) >= 60;

        public static StoryRouteDef Build() => new StoryRouteDef
        {
            Id = StoryRoute.Legacy,
            Name = "아버지의 등번호",
            Blurb = "영구결번 레전드의 아들. 모두가 아버지 이름으로 부르는 선수가, 자기 이름을 찾아간다.",
            BondNames = new[] { "권태수", "차민혁", "아버지", "오지영" },
            BondRoles = new[] { "아버지의 옛 동료", "어릴 적 친구", "영구결번 레전드", "라디오 캐스터" },
            MentorBefore = "권태수 수석코치",
            MentorAfter = "권태수 감독",
            Scenes = Scenes(),
            Epilogue = Epilogue,
            Opening = (c, r, ending) =>
            {
                switch (ending)
                {
                    case StoryEnding.Legend: return new[] { "[라디오 야구 중계 · 오지영] '등번호 10번의 아들, 명예의 전당에'", $"헌액식. {r.Name} 선수는 첫마디를 이렇게 시작했다. \"오늘은 아버지 이름 말고, 제 이름으로 서 있겠습니다.\"" };
                    case StoryEnding.OneClub: return new[] { $"[라디오 야구 중계 · 오지영] '아버지와 같은 유니폼, {r.Seasons}시즌'", $"{r.MainClub}에서만 {r.Seasons}시즌. 외야 펜스에는 이제 등번호 두 개가 나란히 걸려 있다." };
                    case StoryEnding.Broadcaster: return new[] { "[라디오 야구 중계 · 오지영] '마이크를 넘겨받다'", "은퇴 후 라디오 중계석. 오지영의 옆자리였다. 첫 방송에서 아버지의 마지막 경기 녹음을 틀었다." };
                    case StoryEnding.Hometown: return new[] { "[라디오 야구 중계 · 오지영] '아버지의 야구 교실로'", "은퇴 후 아버지가 운영하던 동네 야구 교실을 함께 맡았다. 간판 이름은 그대로 두었다." };
                    case StoryEnding.Scout: return new[] { "[라디오 야구 중계 · 오지영] '권태수의 후임'", $"은퇴 후 {r.MainClub} 프런트에 들어갔다. 첫 업무는 2군 선수들 면담. '누구의 아들'이 아니라 이름부터 물었다." };
                    case StoryEnding.Partner: return new[] { "[라디오 야구 중계 · 오지영] '어릴 적 그 운동장으로'", "은퇴 후 민혁과 함께 어릴 적 뛰던 운동장에 독립 야구단을 만들었다. 단장은 민혁, 감독은 나." };
                    default: return new[] { "[라디오 야구 중계 · 오지영] '10번의 아들, 두 번째 출발'", "은퇴 1년 뒤, 아버지 야구 교실 옆에 작은 교실이 하나 더 생겼다. 등번호는 아무거나 골라도 된다." };
                }
            },
            BondLine = (c, b, v) =>
            {
                switch (b)
                {
                    case Bond.Mentor:
                        return v >= 70 ? (Reconciled(c) ? "권태수와 아버지가 나란히 앉아 박수를 쳤다. 30년 만에 같은 줄이었다." : "권태수가 맨 앞에서 모자를 벗었다. \"너는 네 이름으로 끝냈다.\"")
                            : v <= 30 ? "권태수는 오지 않았다. 다음 날 구단을 통해 짧은 메모가 왔다. '수고했다, 선수.'"
                            : "권태수는 뒤쪽 자리에서 팔짱을 낀 채 고개를 끄덕였다.";
                    case Bond.Friend:
                        return v >= 70 ? (FriendCameBack(c) ? "민혁이 유니폼 차림으로 꽃다발을 들고 올라왔다. \"우리 둘 다 끝까지 왔다.\"" : "민혁이 야구 교실 아이들을 데리고 와서 누구보다 크게 이름을 불렀다.")
                            : v <= 30 ? "민혁에게서 문자 한 통이 왔다. '어릴 때 운동장, 가끔 생각난다.'"
                            : "민혁이 객석에서 손을 흔들었다.";
                    case Bond.Family:
                        return v >= 70 ? "아버지가 낡은 10번 유니폼을 꺼내 내 어깨에 걸쳐 주었다. \"이제 이건 네 거 말고, 우리 거다.\""
                            : v <= 30 ? "아버지는 객석 맨 뒤에 있다가 행사가 끝나기 전에 조용히 나갔다."
                            : "아버지가 객석에서 오래 박수를 쳤다.";
                    default:
                        return v >= 70 ? "오지영이 라디오 녹음 두 개를 이어 틀었다. 아버지의 마지막 타석과, 나의 마지막 타석."
                            : "오지영의 클로징 멘트는 짧았지만 따뜻했다.";
                }
            },
            Closing = (c, r, ending) =>
            {
                switch (ending)
                {
                    case StoryEnding.Legend: return "아버지의 이름으로 시작한 이야기는, 내 이름으로 끝났다.";
                    case StoryEnding.OneClub: return "같은 유니폼, 다른 이름. 팬들은 이제 둘을 헷갈리지 않는다.";
                    case StoryEnding.Broadcaster: return "\"10번의 아들\"이라는 소개는 이제 아무도 하지 않는다. 다들 그냥 내 목소리를 안다.";
                    case StoryEnding.Hometown: return "아버지는 투수를, 나는 타자를 가르친다. 점심은 둘이서 짜장면.";
                    case StoryEnding.Scout: return "누구의 아들이든, 누구의 제자든. 그라운드에 선 사람은 자기 이름으로 불려야 한다.";
                    case StoryEnding.Partner: return "관중은 매 경기 백 명 남짓. 그래도 둘은 어릴 적처럼 매일 해 질 때까지 공을 던진다.";
                    default: return "아이들에게 제일 먼저 가르치는 건 자기 이름을 크게 말하는 법이다.";
                }
            },
        };

        static List<StoryScene> Scenes() => new List<StoryScene>
        {
            // ----- Prologue -----
            new StoryScene
            {
                Slot = "draft", Title = "아버지의 이름",
                Text = c => "드래프트 1라운드. {team}의 테이블에서 이름이 불렸다.\n그런데 카메라는 나보다 객석의 아버지를 먼저 잡았다.\n\n" +
                            "{team} 영구결번 10번. 서른셋, 전성기에 무릎 부상으로 은퇴한 레전드.\n기사 제목은 전부 같았다. '10번의 아들, 아버지의 팀으로'.\n\n" +
                            "입단식이 끝나고 수석코치 권태수가 악수를 청했다. 아버지의 옛 동료라고 했다.\n\"아버지 얘기는 여기서 하지 마라. 나는 그 사람 싫어한다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"저는 아버지가 아닙니다\"", "권태수가 잠깐 눈썹을 올렸다.\n\"그럼 증명해 봐라. 이름부터.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("\"왜 싫어하세요?\"", "권태수가 돌아섰다.\n\"그건 네 아버지한테 물어봐라. 대답은 안 해 주겠지만.\"",
                        (e, k) => { e.Exp(Stat.Eye, 30); Flag(k, "asked_why"); }),
                    O("말없이 고개를 숙인다", "권태수는 손을 놓고 말없이 걸어갔다. 아버지는 멀리서 그 장면을 보고 있었다.",
                        (e, k) => { ChangeBond(e, k, Bond.Family, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "roommate", Title = "운동장 친구",
                Text = c => "숙소 첫날, 문을 여니 낯익은 얼굴이 있었다. 차민혁.\n아버지가 운영하던 동네 야구 교실에서 같이 자란 친구다. 드래프트에서는 떨어지고 육성선수로 들어왔다고 했다.\n\n" +
                            "민혁이 웃었다.\n\"너는 1라운드, 나는 육성선수. 어릴 때부터 늘 이랬지, 뭐.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"같이 1군 가자\"", "민혁이 피식 웃었다.\n\"말은 쉽지. 그래도... 그래, 같이 가자.\"",
                        (e, k) => ChangeBond(e, k, Bond.Friend, 10)),
                    O("어릴 때처럼 캐치볼을 하자고 한다", "해 질 무렵 숙소 앞마당. 공을 주고받는 소리가 야구 교실 시절 그대로였다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Defense, 30); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("\"너도 금방 올라올 거야\"", "민혁의 얼굴이 굳었다.\n\"...그런 말은 네가 하면 좀 그렇다.\"",
                        (e, k) => ChangeBond(e, k, Bond.Friend, -5)),
                },
            },

            // ----- 1. Debut -----
            new StoryScene
            {
                Slot = "first", Title = "라디오 속 목소리",
                Text = c => (HasFirstRecord(c)
                                ? "{first}. 숙소로 돌아오는 버스에서 라디오 다시 듣기를 틀었다.\n"
                                : "{first}까지는 아직이다. 숙소로 돌아오는 버스에서 라디오를 틀었다.\n") +
                            "\n15년 전 아버지의 마지막 경기를 중계했던 사람, 캐스터 오지영의 목소리였다.\n" +
                            "오지영이 말했다.\n\"오늘 {name} 선수, 아버지 얘기는 빼겠습니다. 이 선수는 이 선수 이름으로 불러야 하니까요.\"",
                Options = c => new List<StoryOption>
                {
                    O("방송국에 감사 문자를 보낸다", "다음 날 오지영의 클로징 멘트. '어제 한 선수에게서 문자를 받았습니다. 짧았지만, 좋은 선수일 것 같습니다.'",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("다시 듣기를 몇 번이고 돌린다", "아버지 이름이 한 번도 나오지 않는 중계. 처음이었다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("이어폰을 빼고 영상을 본다", "라디오 대신 오늘 타석 영상을 열 번 돌려 봤다.",
                        (e, k) => e.Exp(Stat.Eye, 40)),
                },
            },
            new StoryScene
            {
                Slot = "letter", Title = "세 글자 문자",
                Text = c => "아버지에게서 문자가 왔다. 프로에 온 뒤 처음이었다.\n\n'봤다.'\n\n세 글자. 아버지다웠다.",
                Options = c => new List<StoryOption>
                {
                    O("전화를 건다", "\"...봤다니까.\" 아버지는 그 말만 세 번 했다. 끊기 직전에 덧붙였다. \"무릎 조심해라.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Family, 10); }),
                    O("'더 잘할게요'라고 답한다", "읽음 표시가 떴다. 답은 없었다. 그래도 그게 아버지의 대답이었다.",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Family, 5); }),
                    O("답하지 않는다", "아버지 그늘에서 벗어나고 싶었다. 휴대폰을 엎어 두고 배트를 들었다.",
                        (e, k) => { e.Exp(Stat.Power, 40); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rival", Title = "누구의 아들도 아닌",
                Text = c => "같은 해 프로에 온 {rivalTeam}의 {rival}.\n인터뷰에서 그가 말했다.\n\"저는 아버지 덕 본 거 없습니다. 그래서 더 지기 싫어요.\"\n\n" +
                            "누구를 두고 한 말인지는 모두가 알았다.",
                Options = c => new List<StoryOption>
                {
                    O("{rival}, 기억해 둔다", "그날 밤 상대 기록을 처음으로 다 찾아봤다.",
                        (e, k) => { e.Exp(Stat.Power, 30); e.Morale(3); Flag(k, "rival_fire"); }),
                    O("\"맞는 말이에요\"라고 웃는다", "오지영이 라디오에서 그 장면을 소개했다. '오늘의 명장면은 그라운드 밖에서 나왔습니다.'",
                        (e, k) => { e.Fans(20); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("민혁에게 털어놓는다", "민혁이 어깨를 으쓱했다.\n\"그 말 듣는 거, 나는 평생 반대로 들었다. '10번 야구 교실 출신인데 왜 그것밖에 못 해.'\"",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Friend, 10); }),
                },
            },

            // ----- 2. The first wall -----
            new StoryScene
            {
                Slot = "wall", Title = "10번의 그림자",
                Text = c => c.InFarm
                    ? "2군행. 기사 제목은 '10번의 아들, 아버지 근처에도 못 가'.\n짐을 싸는데 권태수가 문 앞에 서 있었다.\n\n\"억울하냐. 그럼 내일 아침 6시, 2군 구장. 네 아버지가 안 하던 거 가르쳐 주마.\""
                    : "몇 경기째 아무것도 안 된다. 관중석에서 누군가 소리쳤다. '아버지 반만 해라!'\n\n경기 후 권태수가 불렀다.\n\"아버지랑 비교당하는 게 싫으면 아버지가 안 하던 걸 해라. 내일 아침 6시.\"",
                Options = c => new List<StoryOption>
                {
                    O("아침 6시에 간다", "권태수의 훈련은 아버지와 정반대였다. 화려한 스윙 대신 짧고 단단한 것만.\n사흘째 권태수가 말했다. \"너는 네 아버지보다 끈질기다. 그건 칭찬이다.\"",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 70)); e.Fatigue(20); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "dawn"); }),
                    O("아버지에게 전화한다", "아버지는 한참 말이 없다가 딱 한마디 했다.\n\"권태수한테 가라. 그 사람이 나보다 잘 가르친다.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Family, 10); Flag(k, "father_sent"); }),
                    O("혼자 해 보겠다고 한다", "\"...그 고집은 네 아버지 닮았네.\" 권태수가 돌아섰다.",
                        (e, k) => { e.Exp(Stat.Eye, 30); e.Morale(2); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "rooftop", Title = "운동장 불빛",
                Text = c => "밤늦게 민혁이 불러냈다. 어릴 적 아버지 야구 교실이 있던 운동장.\n조명이 하나만 켜져 있었다.\n\n" +
                            "민혁이 말했다.\n\"너 요즘 아버지 얘기만 나오면 얼굴 굳는 거 알아? 여기서는 그냥 우리였잖아.\"",
                Options = c => new List<StoryOption>
                {
                    O("어릴 때처럼 펑고를 받는다", "흙투성이가 될 때까지 굴렀다. 둘 다 웃고 있었다.",
                        (e, k) => { e.Exp(Stat.Defense, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("속마음을 털어놓는다", "아버지가 자랑스러운데, 미워서 미안하다고 말했다. 민혁은 그냥 고개만 끄덕였다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 10); }),
                    O("\"넌 몰라\"", "민혁이 공을 내려놓았다.\n\"...그래, 나는 모르지. 10번 아들이 아니니까.\"",
                        (e, k) => { e.Morale(-1); ChangeBond(e, k, Bond.Friend, -10); }),
                },
            },

            // ----- 2-3. The thread -----
            new StoryScene
            {
                Slot = "arc_hint", Title = "무릎이 아니었다",
                Text = c => "구단 의무실. 오래 일한 주치의가 내 무릎을 보다가 혼잣말처럼 말했다.\n\"너는 아버지 무릎 닮았네. 튼튼해.\"\n\n" +
                            "\"...아버지는 무릎 때문에 은퇴하신 거 아니에요?\"\n주치의가 잠깐 멈칫했다.\n\"그 무릎은 멀쩡했지. 아무튼, 끝.\"",
                Options = c => new List<StoryOption>
                {
                    O("아버지에게 묻는다", "아버지는 신문을 접지도 않고 말했다.\n\"무릎이 아팠다. 그게 다다.\" 신문을 넘기는 손이 조금 빨랐다.",
                        (e, k) => { ChangeBond(e, k, Bond.Family, -3); Flag(k, "knee_doubt"); }),
                    O("권태수에게 묻는다", "권태수가 차갑게 웃었다.\n\"그 사람이 무릎이라고 했으면 무릎이겠지. 그 시즌에 우리 팀이 어떻게 됐는지는 기록을 찾아봐라.\"",
                        (e, k) => { ChangeBond(e, k, Bond.Mentor, 3); Flag(k, "knee_doubt"); Flag(k, "that_season"); }),
                    O("모른 척한다", "알고 싶지 않았다. 지금은 내 야구만 생각하기로 했다.",
                        (e, k) => e.Morale(2)),
                },
            },
            new StoryScene
            {
                Slot = "spot", Title = "아버지의 자리",
                Text = c => "권태수가 감독으로 올라왔다.\n첫 미팅, 그가 라인업 카드를 들었다.\n\n\"올해 우리 팀 {spot}, {name}.\"\n" +
                            (c.Story.Has("dawn") ? "\"아침 6시에 한 번도 안 빠진 놈이다. 주장도 맡긴다.\"\n" : "") +
                            "\n15년 전 아버지가 서던 자리. 회의실이 조용해졌다. 권태수가 덧붙였다.\n\"아버지 자리라서가 아니다. 네 자리라서다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"맡겨 주십시오\"", "그날 밤 라디오에서 오지영이 말했다. '오늘부터 이 자리의 이름이 바뀝니다.'",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"아버지보다 잘하겠습니다\"", "권태수가 처음으로 소리 내어 웃었다.\n\"그래, 그 말 듣고 싶었다.\"",
                        (e, k) => { Flag(k, "manager"); if (k.Story.Has("dawn")) Flag(k, "captain"); e.Morale(4); e.Exp(Stat.Power, 40); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_reveal", Title = "15년 전의 진실",
                Text = c => "오지영이 방송국 자료실에서 찾았다며 녹음 파일 하나를 건넸다.\n15년 전, 아버지 은퇴 기자회견 직전의 대기실 녹음. 방송되지 않은 부분이었다.\n\n" +
                            "아버지의 목소리.\n\"애 엄마가 떠났어. 애가 이제 세 살이다. 원정 다니면서는 못 키운다. 무릎이라고 해 줘. 애가 나중에 자기 탓이라고 생각하면 안 되니까.\"\n\n" +
                            "그리고 권태수의 목소리.\n\"우승 문턱이야. 너 없으면 끝이라고!\"",
                Options = c => new List<StoryOption>
                {
                    O("아버지를 찾아간다", "아버지는 녹음을 다 듣고 한참 창밖을 봤다.\n\"...네 탓 아니다. 내가 고른 거다. 한 번도 후회 안 했다.\"\n처음으로 아버지가 우는 걸 봤다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 20); Flag(k, "truth_father"); }),
                    O("권태수를 찾아간다", "권태수가 녹음을 듣고 오래 말이 없었다.\n\"...애 때문이었냐. 나는 15년 동안 그 사람이 팀을 버린 줄 알았다.\"",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "truth_coach"); }),
                    O("오지영에게 묻어 달라고 한다", "오지영이 파일을 삭제했다.\n\"원래 틀 생각 없었어요. 아버님이 15년 지킨 비밀이니까.\"",
                        (e, k) => { e.Morale(2); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "voice_kept"); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new StoryScene
            {
                Slot = "injury", Title = "민혁의 마지막 기회",
                Text = c => "민혁에게 1군 콜업 소식이 왔다. 입단 5년 만이었다.\n그런데 데뷔전 사흘 전, 수비 훈련 중 손목이 꺾였다.\n\n" +
                            "병원 복도에서 민혁이 웃었다.\n\"역시 나는 이런 팔자다. 10번 야구 교실 출신 중에 제일 안 풀리는 놈.\"",
                Options = c => new List<StoryOption>
                {
                    O("매일 재활실에 간다", "훈련이 끝나면 매일 재활실에 갔다. 어릴 적 운동장처럼, 둘이서 공 하나를 굴렸다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Friend, 20); Flag(k, "visited"); }),
                    O("아버지에게 부탁한다", "아버지가 다음 날 재활실에 나타났다. 민혁이 벌떡 일어섰다.\n\"감독님... 아니, 선생님.\"\n아버지는 어릴 때처럼 민혁의 머리를 쓰다듬었다.",
                        (e, k) => { ChangeBond(e, k, Bond.Friend, 10); ChangeBond(e, k, Bond.Family, 10); }),
                    O("\"내년엔 무조건 된다\"", "민혁이 고개를 들었다. \"...그래. 내년.\"",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },

            // ----- 4. The summit -----
            new StoryScene
            {
                Slot = "summit", Title = "같은 무대",
                Text = c => (HasSummit(c)
                                ? "시상식. 15년 전 아버지가 섰던 바로 그 무대.\n객석에 아버지, 권태수, 민혁이 보였다. 아버지와 권태수 사이에는 빈자리 하나가 있었다.\n\n"
                                : "스물여덟. 트로피는 아직이다.\n그래도 이제 '10번의 아들'보다 내 이름이 먼저 불린다.\n\n") +
                            "오지영이 생방송 인터뷰를 요청했다.\n\"아버님께 한마디 해 주시겠어요?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"아버지, 이제 제 차례예요\"", "방송이 끝나고 아버지에게서 문자가 왔다. '들었다.' 이번엔 두 글자였다. 그래도 알았다.",
                        (e, k) => { e.Fans(50); ChangeBond(e, k, Bond.Family, 10); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("고마운 사람들 이름을 부른다", "권태수, 민혁, 오지영, 그리고 마지막에 아버지. 라디오 청취율이 그날 최고를 찍었다.",
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
                Slot = "arc_crisis", Title = "아버지가 쓰러졌다",
                Text = c => "경기 중 전화가 울렸다. 아버지가 야구 교실에서 쓰러졌다고 했다. 심장이었다.\n수술은 내일 아침. 팀은 순위 싸움 한가운데였다.\n\n" +
                            "권태수가 말했다.\n\"가라. 15년 전에 네 아버지도 너 때문에 갔다. 이번엔 네가 갈 차례다.\"",
                Options = c => new List<StoryOption>
                {
                    O("병원으로 간다", "수술실 앞에서 밤을 새웠다. 마취에서 깬 아버지의 첫마디.\n\"...경기는?\"\n웃음이 나왔다. 눈물도.",
                        (e, k) => { e.Fatigue(-10); e.Morale(4); ChangeBond(e, k, Bond.Family, 20); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "went_hospital"); }),
                    O("경기를 마치고 간다", "그날 타석에 모든 걸 걸었다. 경기가 끝나자마자 유니폼 그대로 병원에 달려갔다.\n아버지가 병실 TV를 가리켰다. \"봤다.\"",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fans(30); ChangeBond(e, k, Bond.Family, 10); Flag(k, "played_through"); }),
                    O("\"아버지라면 뛰라고 했을 거예요\"", "권태수가 한참 나를 봤다.\n\"...아니. 네 아버지는 15년 전에 반대로 했다.\" 그 말이 오래 남았다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Mentor, -5); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "family", Title = "기록의 숫자",
                Text = c => "구단 기록실에서 연락이 왔다. 이번 시즌 안에 아버지의 통산 기록을 넘을 수도 있다고.\n\n" +
                            "아버지에게 그 얘기를 했더니 짧게 웃었다.\n\"넘어라. 내 기록은 넘으라고 있는 거다. 대신 그날 나는 안 간다. 쑥스러워서.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"꼭 오세요\"", "아버지는 대답하지 않았다. 그런데 그날 외야석 맨 뒤에, 모자를 눌러 쓴 사람이 있었다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 10); Flag(k, "record_father"); }),
                    O("10번 유니폼을 빌려 달라고 한다", "아버지가 옷장 깊숙이서 낡은 유니폼을 꺼냈다. \"냄새 난다.\" 그래도 손이 떨리고 있었다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 15); Flag(k, "borrowed_ten"); }),
                    O("기록은 신경 쓰지 않겠다고 한다", "\"그래, 그게 맞다.\" 아버지는 그렇게 말했지만 조금 서운해 보였다.",
                        (e, k) => { e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Family, -3); }),
                },
            },

            // ----- 5. Veteran -----
            new StoryScene
            {
                Slot = "body", Title = "서른셋의 문턱",
                Text = c => "아버지가 은퇴한 나이, 서른셋이 가까워졌다.\n몸은 멀쩡하다. 오히려 지금이 제일 좋다.\n\n" +
                            "아버지가 말했다.\n\"나는 서른셋 다음을 못 봤다. 네가 대신 봐 줘라. 거기 뭐가 있는지.\"",
                Options = c => new List<StoryOption>
                {
                    O("관리 루틴을 새로 짠다", "아버지와 둘이서 훈련표를 짰다. 서른셋 이후의 야구를, 처음으로 같이.",
                        (e, k) => { e.Fatigue(-20); e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Family, 5); }),
                    O("권태수에게 묻는다", "권태수가 말했다.\n\"서른셋 다음은 네 아버지가 아니라 내가 안다. 나는 서른아홉까지 했으니까. 따라와라.\"",
                        (e, k) => { e.Exp(Stat.Contact, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("더 독하게 한다", "새벽 훈련을 늘렸다. 아버지가 못 본 곳까지 가 보고 싶었다.",
                        (e, k) => { e.Exp(Stat.Power, 60); e.Fatigue(20); }),
                },
            },
            new StoryScene
            {
                Slot = "arc_choice", Title = "빈자리",
                Text = c => "아버지와 권태수. 15년 동안 같은 야구장에 와도 한 번도 같은 줄에 앉지 않았다.\n\n" +
                            (c.Story.Has("truth_coach") || c.Story.Has("truth_father")
                                ? "이제 둘 다 진실을 안다. 남은 건 누가 먼저 말을 거느냐였다.\n"
                                : "둘 사이에 무슨 일이 있었는지, 이제는 어렴풋이 알 것 같았다.\n") +
                            "\n올스타 휴식기. 둘을 한자리에 부를 수 있는 마지막 기회일지도 몰랐다.",
                Options = c => new List<StoryOption>
                {
                    O("셋이서 저녁을 먹자고 한다", "고깃집 구석 자리. 한참 말이 없다가 권태수가 먼저 소주를 따랐다.\n\"...그때 애 키운다고 왜 말을 안 했냐.\"\n아버지가 잔을 받았다. \"말하면 네가 말렸을 거잖아.\"\n둘이 동시에 웃었다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 15); ChangeBond(e, k, Bond.Family, 10); Flag(k, "reconciled"); }),
                    O("아버지 야구 교실에 권태수를 초대한다", "권태수가 운동장에 들어서자 아버지가 펑고 배트를 던져 줬다.\n\"아직 칠 줄 아냐?\"\n아이들 앞에서 두 레전드가 펑고 대결을 했다.",
                        (e, k) => { e.Fans(30); ChangeBond(e, k, Bond.Mentor, 10); ChangeBond(e, k, Bond.Family, 10); Flag(k, "reconciled"); }),
                    O("두 사람의 일이다", "끼어들지 않기로 했다. 그게 두 사람에 대한 예의라고 생각했다.",
                        (e, k) => e.Morale(1)),
                },
            },
            new StoryScene
            {
                Slot = "friend", Title = "민혁의 선택",
                Text = c => FriendCameBack(c)
                    ? "민혁의 이름이 선발 라인업에 올라갔다. 서른한 살의 첫 선발.\n\n경기 전 민혁이 다가왔다.\n\"오늘 나 2번, 너 4번. 어릴 때 운동장 라인업 그대로다.\""
                    : "민혁이 은퇴를 결정했다. 아버지 야구 교실을 물려받기로 했다고 한다.\n\n\"너희 아버지가 그러시더라. 나한테 가르치는 재능이 있대. 처음으로 들은 칭찬이었다.\"",
                Options = c => FriendCameBack(c)
                    ? new List<StoryOption>
                    {
                        O("\"오늘 둘이서 끝내자\"", "민혁이 출루하고 내가 불러들였다. 어릴 적 운동장에서 수백 번 했던 그대로.",
                            (e, k) => { e.Morale(8); e.Fans(30); ChangeBond(e, k, Bond.Friend, 10); Flag(k, "friend_back"); }),
                        O("\"늦었잖아\"", "\"원래 좋은 2번 타자는 늦게 핀다.\" 민혁이 헬멧을 썼다.",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 5); Flag(k, "friend_back"); }),
                    }
                    : new List<StoryOption>
                    {
                        O("야구 교실 첫 수업에 간다", "아이들이 줄을 섰다. 민혁이 내 소개를 했다. \"이 형도 여기 출신이다. 근데 나보다 공 못 받았다.\"",
                            (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 10); }),
                        O("\"네 몫까지 뛴다\"", "그날 경기, 배트 손잡이에 민혁의 등번호를 적었다.",
                            (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 5); }),
                    },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "권태수의 펑고", Only = true,
                Text = c => "권태수가 올 시즌을 끝으로 지휘봉을 내려놓는다고 했다.\n\n" +
                            (Reconciled(c) ? "발표 다음 날, 그는 아버지와 함께 2군 구장에 나타났다. 둘이 번갈아 신인들에게 펑고를 치고 있었다.\n\"30년 만에 같은 팀이다.\" 아버지가 웃었다.\n"
                                : "발표 다음 날, 그는 혼자 2군 구장에서 신인들에게 펑고를 치고 있었다.\n") +
                            "\n권태수가 나를 불렀다.\n\"마지막으로 하나만 받아 봐라. 네 아버지한테 못 친 거다.\"",
                Options = c => new List<StoryOption>
                {
                    O("펑고를 받는다", "흙바닥을 굴러 겨우 잡았다. 권태수가 배트를 내렸다.\n\"됐다. 이제 너는 누구 아들도 아니다. 그냥 내 선수다.\"",
                        (e, k) => { e.Exp(Stat.Defense, 50); e.Morale(6); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"감독님 덕분입니다\"", "권태수가 고개를 저었다.\n\"아니. 6시에 나온 건 너다.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "두 개의 녹음", Only = true,
                Text = c => "오지영이 라디오 특집을 제안했다. 제목은 '10번, 그리고 그 아들'.\n\n" +
                            "오지영이 말했다.\n\"아버님 마지막 경기 녹음이랑, 선수님 첫 경기 녹음을 이어 붙이고 싶어요. 아버님 허락은 받았어요. 선수님만 괜찮다면.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"좋아요. 대신 제 이름으로 끝내 주세요\"", "특집의 마지막 멘트. '이제부터 이 등번호 뒤에는 다른 이름이 있습니다.' 청취자 게시판이 하루 종일 마비됐다.",
                        (e, k) => { e.Fans(60); ChangeBond(e, k, Bond.Voice, 10); Flag(k, "radio_special"); }),
                    O("\"은퇴하고 해 주세요\"", "오지영이 녹음기를 껐다.\n\"그럼 그날 마지막 중계는 제가 할게요. 약속해요.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "last_call_promise"); }),
                },
            },
            new StoryScene
            {
                Slot = "family_game", Title = "아버지의 시구",
                Text = c => "구단이 아버지를 시구자로 초대했다. 은퇴 후 15년 만의 마운드.\n\n" +
                            (c.Me.IsPitcher ? "포수 자리에 앉은 사람은 나였다. 아버지가 던진 공이 미트에 꽂혔다.\n" : "타석에 선 사람은 나였다. 아버지가 던진 공을 일부러 헛스윙했다.\n") +
                            "관중석 전체가 일어섰다. 외야 펜스의 10번이 조명을 받고 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("마운드로 걸어가 안는다", "아버지가 귓속말을 했다.\n\"공 아직 쓸 만하지?\"\n둘 다 웃었다. 전광판에 그 장면이 오래 잡혔다.",
                        (e, k) => { e.Morale(8); e.Fans(40); ChangeBond(e, k, Bond.Family, 10); }),
                    O("시구 공을 아버지께 돌려드린다", "아버지가 공을 한참 들여다봤다.\n\"이건 네가 가져라. 나는 15년 전에 받은 게 있다.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },

            // ----- 6. The last season -----
            new StoryScene
            {
                Slot = "last", Title = "마지막 대화",
                Text = c => "마지막 시즌의 봄. 아버지가 캠프에 찾아왔다.\n" +
                            (Reconciled(c) ? "옆에는 권태수가 있었다. 둘은 이제 같은 줄에 앉는다.\n" : "") +
                            "\n아버지가 말했다.\n\"나는 서른셋에 멈췄다. 너는 여기까지 왔다. 이제 끝은 네가 정해라. 나처럼 남이 정하게 하지 말고.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"끝까지 달리겠습니다\"", "아버지가 처음으로 내 어깨를 두 번 두드렸다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Family, 5); }),
                    O("\"아버지 덕분이에요\"", "아버지가 고개를 저었다.\n\"아니다. 나는 그냥 옆에 있었을 뿐이다. 그거 하려고 그만둔 거고.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "두 글자 편지", Only = true,
                Text = c => "숙소로 편지가 왔다. 아버지의 글씨. 편지는 처음이었다.\n\n" +
                            "'세 살 때 너를 안고 야구장을 나왔다. 그날 한 번도 뒤돌아보지 않았다.\n" +
                            (c.Story.Has("truth_father") ? "네가 그 녹음을 들었다고 했을 때, 15년 만에 처음으로 뒤를 돌아봤다.\n" : "") +
                            "이번엔 네가 야구장을 나올 차례다. 천천히 나와라. 뒤돌아봐도 된다.'\n\n" +
                            "맨 아래에 두 글자가 더 있었다. '고맙다.'",
                Options = c => new List<StoryOption>
                {
                    O("편지를 사물함에 붙인다", "신인 시절 받은 '봤다' 문자 캡처 옆에 나란히.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Family, 10); }),
                    O("바로 전화를 건다", "내가 먼저 말했다.\n\"편지 받았어요.\"\n아버지가 헛기침을 했다.\n\"...봤냐.\"\n이번엔 내가 웃었다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 15); }),
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "내 이름", Only = true,
                Text = c => "마지막 경기. 라디오 중계석에는 오지영이 앉아 있었다.\n" +
                            (c.Story.Has("last_call_promise") ? "약속대로, 그녀가 마지막 중계를 맡았다.\n" : "") +
                            "\n장내 아나운서가 이름을 불렀다. '10번의 아들'이 아니라, 내 이름이었다.\n" +
                            (c.Story.Has("friend_back") ? "더그아웃에서 민혁이 헬멧을 두드렸다.\n" : "외야석에서 민혁이 야구 교실 아이들과 현수막을 흔들었다.\n") +
                            (Reconciled(c) ? "본부석 맨 앞줄, 아버지와 권태수가 나란히 앉아 있었다." : "본부석에는 아버지가, 더그아웃 끝에는 권태수가 서 있었다."),
                Options = c => new List<StoryOption>
                {
                    O("외야 펜스의 10번을 향해 모자를 벗는다", "구장 전체가 일어섰다. 오지영의 목소리가 떨렸다.\n\"15년 전 저 번호를 보내던 날, 오늘이 올 줄 몰랐습니다.\"",
                        (e, k) => { e.Morale(10); e.Fans(50); ChangeBond(e, k, Bond.Family, 10); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("\"이름을 불러 줘서 고맙습니다\"", "오지영이 중계 마지막에 내 이름을 세 번 불렀다. 아버지 이름은 한 번도 나오지 않았다.",
                        (e, k) => { e.Morale(10); e.Fans(40); ChangeBond(e, k, Bond.Voice, 15); }),
                },
            },
        };

        static List<string> Epilogue(Career c)
        {
            var lines = new List<string>();
            if (Reconciled(c)) lines.Add("아버지와 권태수는 지금 매주 화요일 2군 구장에 같이 나간다. 펑고는 번갈아 친다.");
            if (c.Story.Has("truth_father")) lines.Add("아버지 은퇴 기자회견 기록에는 이제 주석이 하나 붙어 있다. '진짜 이유는 아들이었다.'");
            if (c.Story.Has("borrowed_ten")) lines.Add("낡은 10번 유니폼은 지금 구단 박물관에 있다. 옆에는 내 유니폼이 걸려 있다.");
            if (c.Story.Has("radio_special")) lines.Add("라디오 특집 '10번, 그리고 그 아들'은 그해 방송 대상을 받았다.");
            return lines;
        }
    }
}
