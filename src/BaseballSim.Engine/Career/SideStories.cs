using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>
    /// Short serial side stories, three to five scenes each, spread over seasons and branching on earlier
    /// choices: the high-school coach, the old fan who never misses a game, the stadium cat, a romance that
    /// leads to the marriage event, a foreign teammate, and the training aide who throws batting practice.
    /// They stay clear of the main story's people.
    /// </summary>
    public static partial class StoryEvents
    {
        /// <summary>The next scene of a side story: the previous one was answered at least gap games ago and this one hasn't been.</summary>
        static bool Next(Career c, string previous, string id, int gap) => c.Seen(previous) && c.GamesSince("seen:" + previous) >= gap && !c.Seen(id);

        static int SeasonGames(Career c) => c.League.GamesPerSeason;

        static void AddSideStories(List<StoryEvent> list)
        {
            // ---------- 은사: the high-school coach ----------

            list.Add(new StoryEvent
            {
                Id = "teacher_call",
                Title = "은사의 전화",
                Text = "고등학교 때 감독님에게서 전화가 왔다.\n\"TV로 다 보고 있다. 밥은 잘 먹고 다니냐?\"",
                Weight = (c, d) => c.Season <= 3 && !c.Seen("teacher_call") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"감독님 덕분입니다\"", Result = "\"덕분은 무슨. 네가 한 거지.\" 목소리가 떨렸다.", Apply = e => e.Morale(4) },
                    new EventChoice { Label = "\"모교에 한번 찾아갈게요\"", Result = "\"애들한테 말해 둔다. 거짓말이면 혼난다.\"", Apply = e => { e.Morale(3); e.Mark("teacher_promise_visit"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "teacher_visit",
                Title = "모교 방문",
                Text = "모교 운동장. 흙먼지 날리는 그라운드는 그대로다.\n후배들이 줄을 서 있고, 감독님이 뒷짐을 지고 웃는다.",
                Weight = (c, d) => Next(c, "teacher_call", "teacher_visit", SeasonGames(c)) ? (c.HasMark("teacher_promise_visit") ? 3 : 1) : 0,
                Choices =
                {
                    new EventChoice { Label = "후배들에게 직접 시범", Result = "\"우와!\" 함성 속에서 감독님이 고개를 끄덕였다. 기본기를 다시 되새겼다.", Apply = e => { e.Unmark("teacher_promise_visit"); e.Exp(Stat.Contact, 30); e.Morale(5); } },
                    new EventChoice { Label = "야구부에 장비 기증 (300만원)", Result = "새 배트와 공 상자가 들어오자 후배들이 펄쩍 뛰었다.", Apply = e => { e.Unmark("teacher_promise_visit"); e.Money(-300); e.Morale(6); e.Fans(30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "teacher_ill",
                Title = "감독님의 입원",
                Text = "모교 후배에게서 연락이 왔다. 감독님이 쓰러져 입원하셨다고 한다.\n수술을 앞두고 계신다.",
                Weight = (c, d) => c.Me.Age >= 23 && Next(c, "teacher_visit", "teacher_ill", SeasonGames(c)) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "경기 끝나고 병원으로 달려간다", Result = "감독님이 링거를 꽂은 채 웃었다. \"바쁜데 뭐 하러 왔어.\"", Apply = e => { e.Fatigue(15); e.Morale(4); e.Mark("teacher_visited"); } },
                    new EventChoice { Label = "\"수술날 꼭 이길게요\" 영상을 보낸다", Result = "답장이 왔다. '약속했다.'", Apply = e => e.Mark("teacher_promise") },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "teacher_kept",
                Title = "병실의 TV",
                Text = "수술 다음 날. 오늘 경기를 감독님이 병실 TV로 보셨다고 한다.\n간호사가 보낸 영상 속 감독님이 주먹을 불끈 쥐고 있다.",
                Weight = (c, d) => c.HasMark("teacher_promise") && PlayedWell(d) && !c.Seen("teacher_kept") ? 8 : 0,
                Choices =
                {
                    new EventChoice { Label = "영상을 몇 번이고 돌려 본다", Result = "\"거봐라, 내 제자다.\" 병실이 떠나가라 외치셨다고 한다.", Apply = e => { e.Unmark("teacher_promise"); e.Morale(10); e.Fans(40); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "teacher_back",
                Title = "다시 벤치로",
                Text = "감독님이 퇴원해서 다시 모교 벤치에 앉으셨다.\n문자가 왔다. '네 덕에 살았다. 이번엔 내가 응원한다.'",
                Weight = (c, d) => !c.Seen("teacher_back") && (c.HasMark("teacher_visited") || c.HasMark("teacher_promise"))
                    && c.GamesSince("seen:teacher_ill") >= Gap(c) * 2 ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "답장을 보낸다", Result = "'감독님 졸업식 날 하신 말, 아직 기억합니다. 끝까지 해라.'", Apply = e => { e.Unmark("teacher_visited"); e.Unmark("teacher_promise"); e.Morale(6); } },
                },
            });

            // ---------- The old fan who never misses a home game ----------

            list.Add(new StoryEvent
            {
                Id = "grandpa_fan",
                Title = "외야석 할아버지",
                Text = "외야 맨 앞줄에 늘 같은 할아버지가 앉아 있다.\n구단 직원 말로는 40년째 개근이라고 한다. 오늘 {name}을 향해 모자를 흔들었다.",
                Weight = (c, d) => c.Season <= 2 && !c.InFarm && !c.Seen("grandpa_fan") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "모자를 벗고 인사한다", Result = "할아버지가 두 팔을 번쩍 들었다.", Apply = e => { e.Morale(3); e.Mark("grandpa_friend"); } },
                    new EventChoice { Label = "사인볼을 건넨다", Result = "할아버지가 공을 손수건에 소중히 쌌다.", Apply = e => { e.Morale(2); e.Fans(10); e.Mark("grandpa_friend"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "grandpa_banner",
                Title = "손으로 쓴 플래카드",
                Text = "외야석 할아버지가 직접 붓으로 쓴 플래카드를 들고 왔다.\n'{name}, 내 생애 마지막 스타'",
                Weight = (c, d) => c.HasMark("grandpa_friend") && Next(c, "grandpa_fan", "grandpa_banner", SeasonGames(c)) && PlayedWell(d) ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "경기 뒤 같이 사진을 찍는다", Result = "할아버지 손이 생각보다 단단했다. 사진은 구단 SNS 메인에 걸렸다.", Apply = e => { e.Fans(50); e.Morale(5); } },
                    new EventChoice { Label = "홈런 공을 드린다", Result = "\"이건 관에 넣어 갈 거다.\" 할아버지가 껄껄 웃었다.", Apply = e => { e.Morale(6); e.Fans(30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "grandpa_absent",
                Title = "비어 있는 자리",
                Text = "오늘 외야 맨 앞줄 그 자리가 비어 있다.\n40년 동안 한 번도 없던 일이라고 한다.",
                Weight = (c, d) => c.Me.Age >= 23 && Next(c, "grandpa_banner", "grandpa_absent", SeasonGames(c)) && !c.InFarm ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "구단에 연락처를 수소문한다", Result = "할아버지가 입원하셨다고 한다. 주소를 받아 적었다.", Apply = e => e.Mark("grandpa_found") },
                    new EventChoice { Label = "그 자리를 보며 경기한다", Result = "타석에 들어설 때마다 빈자리에 눈이 갔다.", Apply = e => e.Morale(-1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "grandpa_hospital",
                Title = "병실 직관",
                Text = "병실 문을 열자 할아버지가 놀라서 일어나려 했다.\n작은 TV에서 {team} 경기가 나오고 있다.",
                Weight = (c, d) => c.HasMark("grandpa_found") && c.GamesSince("grandpa_found") >= 1 && !c.Seen("grandpa_hospital") ? 6 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "같이 경기를 본다", Result = "할아버지가 투수 교체 타이밍을 감독보다 먼저 맞혔다. 둘이 한참 웃었다.",
                        Apply = e => { e.Unmark("grandpa_found"); e.Mark("grandpa_recovering"); e.Morale(6); e.Fatigue(5); },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "grandpa_return",
                Title = "돌아온 할아버지",
                Text = "새 시즌 첫 홈경기. 외야 맨 앞줄에 낯익은 모자가 보인다.\n할아버지가 지팡이를 짚고 돌아왔다.",
                Weight = (c, d) => c.HasMark("grandpa_recovering") && SeasonsSince(c, "grandpa_recovering") >= 1 && Progress(c) < 0.4 && !c.InFarm && !c.Seen("grandpa_return") ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "외야까지 뛰어가 인사한다", Result = "관중석이 박수로 할아버지를 맞았다. 41년째 개근이 시작됐다.", Apply = e => { e.Unmark("grandpa_recovering"); e.Morale(8); e.Fans(60); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "grandpa_farewell",
                Title = "할아버지의 손자",
                Text = "그 자리에 처음 보는 청년이 앉아 있었다. 경기가 끝나고 {name}을 찾아왔다.\n\"할아버지가 마지막까지 선수님 경기를 보셨어요. 이걸 꼭 전해 달라고 하셨어요.\"\n손수건에 싼 사인볼이었다.",
                Weight = (c, d) => c.Seen("grandpa_absent") && !c.Seen("grandpa_hospital") && !c.HasMark("grandpa_found") && c.GamesSince("seen:grandpa_absent") >= Gap(c) && !c.Seen("grandpa_farewell") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "그 자리에 사인 유니폼을 걸어 둔다", Result = "구단이 그 좌석에 할아버지 이름을 새겼다. 경기 전마다 그쪽을 보게 됐다.", Apply = e => { e.Morale(5); e.Fans(80); } },
                    new EventChoice { Label = "손자를 시구에 초대한다", Result = "손자가 던진 공을 받았다. 할아버지가 보고 계실 것 같았다.", Apply = e => { e.Morale(6); e.Fans(50); } },
                },
            });

            // ---------- The stadium cat ----------

            list.Add(new StoryEvent
            {
                Id = "stadium_cat",
                Title = "불펜 뒤 고양이",
                Text = "불펜 뒤 창고 앞에 비쩍 마른 고양이가 웅크리고 있다.\n{name}을 보더니 작게 울었다.",
                Weight = (c, d) => !c.InFarm && !c.Seen("stadium_cat") ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "편의점에서 간식을 사 온다", Result = "고양이가 허겁지겁 먹고는 발목에 몸을 비볐다.", Apply = e => { e.Morale(3); e.Money(-5); e.Mark("cat_fed"); } },
                    new EventChoice { Label = "그냥 지나친다", Result = "몇 걸음 가다 뒤를 돌아봤다. 고양이가 아직 보고 있다.", Apply = e => { } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cat_name",
                Title = "덕아웃의 손님",
                Text = "그 고양이가 덕아웃까지 따라 들어왔다. 선수들이 다 몰려들었다.\n\"{name}이 밥 준 애래! 이름은 네가 지어.\"",
                Weight = (c, d) => c.HasMark("cat_fed") && Next(c, "stadium_cat", "cat_name", Gap(c)) ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"홈런이\"", Result = "\"홈런아!\" 부르자 고양이가 꼬리를 세웠다.", Apply = e => { e.Mark("cat:홈런이"); e.Morale(4); } },
                    new EventChoice { Label = "\"도루\"", Result = "이름을 부르자마자 쏜살같이 1루 쪽으로 뛰어갔다. 딱 맞는 이름이다.", Apply = e => { e.Mark("cat:도루"); e.Morale(4); } },
                    new EventChoice { Label = "\"직구\"", Result = "\"직구야!\" 고양이가 고개를 갸웃했다. 마음에 드는 모양이다.", Apply = e => { e.Mark("cat:직구"); e.Morale(4); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cat_lucky",
                Title = "행운의 고양이",
                Text = "오늘 아침 고양이가 {name}의 사물함 안에서 자고 있었다.\n그리고 오늘 경기는 대폭발이었다.",
                Weight = (c, d) => c.Seen("cat_name") && PlayedWell(d) && !c.Seen("cat_lucky") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "고양이에게 특식을 바친다", Result = "기자가 이 이야기를 기사로 썼다. '{team}의 행운 고양이'", Apply = e => { e.Fans(60); e.Morale(4); } },
                    new EventChoice { Label = "우연일 뿐이라고 웃는다", Result = "그래도 다음 날 아침 사물함 문은 살짝 열어 뒀다.", Apply = e => e.Morale(3) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cat_mascot",
                Title = "명예 마스코트",
                Text = "구단이 그 고양이를 {team}의 명예 마스코트로 임명했다.\n이제 고양이 굿즈까지 나온다고 한다. 그런데 집이 필요하다.",
                Weight = (c, d) => c.Seen("cat_name") && SeasonsSince(c, "seen:cat_name") >= 1 && !c.Seen("cat_mascot") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "내가 입양한다", Result = "이제 집에 가면 고양이가 현관에서 기다린다. 피로가 싹 풀린다.", Apply = e => { e.Morale(6); e.Fatigue(-15); e.Fans(40); } },
                    new EventChoice { Label = "구장에서 다 같이 키운다", Result = "구단이 불펜 뒤에 작은 집을 지어 줬다. 경기 전 쓰다듬는 게 루틴이 됐다.", Apply = e => { e.Morale(4); e.Fans(60); e.Trust(1); } },
                },
            });

            // ---------- A romance, leading to the proposal ----------

            list.Add(new StoryEvent
            {
                Id = "blind_date",
                Title = "소개팅",
                Text = "동료가 휴대폰 사진을 내밀었다.\n\"내 와이프 친구야. 야구 하나도 몰라. 그래서 좋을 걸?\"",
                Weight = (c, d) => c.Me.Age >= 21 && c.Me.Age <= 28 && !c.HasMark("married") && !c.Seen("blind_date") ? 0.7 : 0,
                Choices =
                {
                    new EventChoice { Label = "나가 본다", Result = "\"야구 선수면... 공 던지는 거죠?\" 첫마디에 웃음이 터졌다. 세 시간이 금방 갔다.", Apply = e => { e.Morale(5); e.Mark("dating"); } },
                    new EventChoice { Label = "지금은 야구만 한다", Result = "\"아깝다, 진짜 괜찮은 사람인데.\"", Apply = e => e.Morale(1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "date_anniversary",
                Title = "원정 중 기념일",
                Text = "오늘이 만난 지 100일인데 원정 경기다.\n휴대폰에 '경기 잘해! 다치지 말고' 메시지가 와 있다.",
                Weight = (c, d) => c.HasMark("dating") && Next(c, "blind_date", "date_anniversary", Gap(c) * 2) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "경기 끝나고 KTX로 깜짝 방문", Result = "새벽에 문을 열어 준 얼굴이 놀라서 울다 웃었다. 피곤해도 하나도 안 피곤하다.", Apply = e => { e.Fatigue(20); e.Morale(8); } },
                    new EventChoice { Label = "영상 통화로 케이크를 분다", Result = "화면 너머로 같이 촛불을 껐다. 다음엔 꼭 옆에 있겠다고 했다.", Apply = e => e.Morale(5) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "dating_news",
                Title = "열애설",
                Text = "아침에 휴대폰이 터질 듯 울렸다.\n'[단독] {team} {name}, 비연예인과 열애 중'",
                Weight = (c, d) => c.HasMark("dating") && Next(c, "date_anniversary", "dating_news", SeasonGames(c) / 2) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"네, 예쁘게 만나고 있습니다\"", Result = "댓글창이 축하로 가득 찼다. 연인이 '멋있었어' 문자를 보냈다.", Apply = e => { e.Morale(6); e.Fans(50); } },
                    new EventChoice { Label = "\"사생활은 지켜 주세요\"", Result = "기사는 금방 잠잠해졌다. 연인이 고맙다고 했다.", Apply = e => e.Morale(3) },
                },
            });

            // ---------- A foreign teammate ----------

            list.Add(new StoryEvent
            {
                Id = "foreign_arrive",
                Title = "새 외국인 선수",
                Text = "새로 온 외국인 선수 라파엘이 구내식당 구석에서 혼자 밥을 먹고 있다.\n젓가락질이 서툴러 밥알이 자꾸 떨어진다.",
                Weight = (c, d) => c.Season >= 2 && !c.Seen("foreign_arrive") ? 0.7 : 0,
                Choices =
                {
                    new EventChoice { Label = "식판을 들고 옆에 앉는다", Result = "\"Thank you, 친구!\" 라파엘이 아는 한국어를 전부 썼다.", Apply = e => { e.Morale(3); e.Mark("rafael"); } },
                    new EventChoice { Label = "포크를 갖다 준다", Result = "라파엘이 엄지를 들었다. 다음 날부터 먼저 인사를 했다.", Apply = e => { e.Morale(2); e.Mark("rafael"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_korean",
                Title = "한국어 수업",
                Text = "라파엘이 노트를 들고 왔다. \"한국어, 가르쳐 줘.\"\n뒤에서 동료들이 이상한 말부터 가르치려고 킥킥댄다.",
                Weight = (c, d) => c.HasMark("rafael") && Next(c, "foreign_arrive", "foreign_korean", Gap(c)) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"수고하셨습니다\"부터 가르친다", Result = "그날 밤 라파엘이 청소 아주머니께 90도로 인사했다. 다들 감동했다.", Apply = e => { e.Morale(4); e.Trust(1); } },
                    new EventChoice { Label = "같이 장난에 끼어든다", Result = "라파엘이 감독에게 \"밥 사 주세요, 형!\" 했다. 감독도 웃음을 참지 못했다.", Apply = e => { e.Morale(5); e.Fans(20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_homesick",
                Title = "향수병",
                Text = "요즘 라파엘이 말이 없다. 휴대폰 속 가족사진만 들여다본다.\n고국에서는 오늘이 명절이라고 한다.",
                Weight = (c, d) => c.HasMark("rafael") && Next(c, "foreign_korean", "foreign_homesick", Gap(c) * 2) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "고향 음식점을 찾아 데려간다 (50만원)", Result = "고향 음식을 한 입 먹더니 라파엘이 울다 웃었다. \"너, 내 형제.\"", Apply = e => { e.Money(-50); e.Morale(5); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "가족 영상 통화를 같이 한다", Result = "화면 너머 라파엘의 어머니가 {name}에게 손 키스를 보냈다.", Apply = e => { e.Morale(4); e.Mark("rafael_brother"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_leave",
                Title = "공항에서",
                Text = "라파엘이 재계약을 하지 못했다. 내일 고국으로 돌아간다.\n\"한국, 좋았어. 너 때문에.\"",
                Weight = (c, d) => c.HasMark("rafael") && c.Seen("foreign_homesick") && SeasonsSince(c, "seen:foreign_arrive") >= 2 && Progress(c) >= 0.85 && !c.Seen("foreign_leave") ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "공항까지 배웅한다", Result = "출국장 앞에서 라파엘이 \"수고하셨습니다!\" 하고 90도로 인사했다.", Apply = e => { e.Morale(4); e.Fatigue(5); } },
                    new EventChoice { Label = "유니폼을 바꿔 입는다", Result = "라파엘이 {name}의 유니폼을 입고 출국장으로 들어갔다.", Apply = e => e.Morale(5) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_letter",
                Title = "지구 반대편에서 온 편지",
                Text = "라파엘에게서 소포가 왔다. 고국 리그 우승 메달 사진과 편지.\n'이번 시즌, 네 등번호 달고 뛰었어. 우승했어, 형제!'",
                Weight = (c, d) => c.HasMark("rafael_brother") && c.Seen("foreign_leave") && SeasonsSince(c, "seen:foreign_leave") >= 2 && !c.Seen("foreign_letter") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "사진을 사물함에 붙인다", Result = "한국어로 쓴 맞춤법 틀린 편지가 이상하게 눈물 나게 웃겼다.", Apply = e => { e.Unmark("rafael"); e.Morale(8); } },
                },
            });

            // ---------- The training aide ----------

            list.Add(new StoryEvent
            {
                Id = "aide_meet",
                Title = "훈련 보조 형",
                Text = "매일 가장 먼저 나와 공을 던져 주고 받아 주는 훈련 보조 형이 있다.\n고교 시절 유망주였지만 프로 지명을 못 받았다고 한다.",
                Weight = (c, d) => c.Season <= 3 && !c.Seen("aide_meet") ? 0.7 : 0,
                Choices =
                {
                    new EventChoice { Label = "고맙다고 음료를 건넨다", Result = "\"이게 내 일인데 뭘.\" 형이 쑥스럽게 웃었다.", Apply = e => { e.Morale(2); e.Mark("aide_friend"); } },
                    new EventChoice { Label = "남아서 형과 같이 연습한다", Result = "형의 공은 생각보다 훨씬 좋았다. 어깨가 아직 살아 있다.", Apply = e => { e.Exp(Stat.Contact, 30); e.Fatigue(10); e.Mark("aide_friend"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "aide_dream",
                Title = "형의 고민",
                Text = "훈련 보조 형이 머뭇거리다 말했다.\n\"독립리그 입단 테스트가 있는데... 나 같은 게 가도 될까?\"",
                Weight = (c, d) => c.HasMark("aide_friend") && Next(c, "aide_meet", "aide_dream", SeasonGames(c)) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"형 공 내가 제일 잘 알아요\" 글러브를 선물한다 (100만원)", Result = "형이 새 글러브를 한참 만지작거렸다. \"해 볼게.\"", Apply = e => { e.Money(-100); e.Morale(3); e.Mark("aide_tryout"); } },
                    new EventChoice { Label = "현실적인 조언을 한다", Result = "\"맞아, 그렇지.\" 형은 웃었지만 표정이 조금 쓸쓸했다.", Apply = e => e.Morale(-1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "aide_result",
                Title = "형의 소식",
                Text = "훈련 보조 형에게서 문자가 왔다.",
                Weight = (c, d) => c.Seen("aide_dream") && c.GamesSince("seen:aide_dream") >= Gap(c) * 2 && !c.Seen("aide_result") ? 3 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "문자를 연다", Result = "",
                        Apply = e =>
                        {
                            if (e.Career.HasMark("aide_tryout"))
                            {
                                e.Unmark("aide_tryout");
                                e.Say("'나 붙었다. 독립리그 선발 투수다. 네가 준 글러브 끼고 던진다.'");
                                e.Say("괜히 눈가가 뜨거워졌다.");
                                e.Morale(8);
                            }
                            else
                            {
                                e.Say("'오늘도 내가 제일 먼저 나와 있다. 빨리 와라, 공 던져 줄게.'");
                                e.Say("그날 형의 공은 유난히 좋았다.");
                                e.Exp(Stat.Contact, 40);
                                e.Morale(3);
                            }
                        },
                    },
                },
            });
        }
    }
}
