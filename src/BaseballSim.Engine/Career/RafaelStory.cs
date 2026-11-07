using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>
    /// 라파엘, the foreign teammate from the first years: more than a few lunches. Between the Korean
    /// lessons and the homesick night come his slump and the kimchi stew he learned to make; if the
    /// player moves clubs while Rafael is still in Korea, they meet as opponents and keep calling; when
    /// he goes home, the news comes in person or, from another club, by phone. After that he keeps
    /// turning up: a video call from home, the letter, a visit to a game, the wedding, the last season.
    /// </summary>
    public static partial class StoryEvents
    {
        /// <summary>Moved to another club after meeting Rafael, while he stayed with the old one.</summary>
        static bool RafaelApart(Career c) =>
            c.Me.StoryMarks.TryGetValue("rafael", out int met) && c.Me.StoryMarks.TryGetValue("moved", out int moved) && moved > met && !c.CameHome;

        /// <summary>Rafael's last Korean season is ending: two seasons after he came, late in the year.</summary>
        static bool RafaelLeaving(Career c) =>
            c.HasMark("rafael") && c.Seen("foreign_homesick") && SeasonsSince(c, "seen:foreign_arrive") >= 2 && Progress(c) >= 0.85 && !RafaelGone(c);

        static bool RafaelGone(Career c) => c.Seen("foreign_leave") || c.Seen("foreign_leave_far");

        static string RafaelGoneMark(Career c) => c.Seen("foreign_leave") ? "seen:foreign_leave" : "seen:foreign_leave_far";

        static bool Brothers(Career c) => c.HasMark("rafael_brother");

        static void AddRafael(List<StoryEvent> list)
        {
            list.Add(new StoryEvent
            {
                Id = "foreign_slump",
                Title = "퇴출설",
                Text = "라파엘이 두 달째 헤매고 있다. 스포츠면에 '외국인 교체 검토'라는 기사가 났다.\n밤 열 시, 실내 연습장 불이 켜져 있었다. 라파엘이 혼자 배트를 휘두르고 있었다.\n라파엘이 웃었다. \"나, 집에 가기 싫어. 아직.\"",
                Weight = (c, d) => c.HasMark("rafael") && !RafaelApart(c) && Next(c, "foreign_korean", "foreign_slump", Gap(c)) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "공을 올려 주며 같이 남는다", Result = "자정까지 티배팅 공을 올렸다. 다음 날 라파엘이 결승타를 쳤다. 더그아웃으로 들어오며 제일 먼저 나를 찾았다.", Apply = e => { e.Fatigue(12); e.Morale(5); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "내 영상 분석 노트를 빌려준다", Result = "라파엘이 번역기를 돌려 가며 밤새 읽었다. \"너 노트, 내 교과서.\"", Apply = e => { e.Exp(Stat.Eye, 30); e.Morale(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_stew",
                Title = "라파엘의 김치찌개",
                Text = "라파엘이 원정 숙소 내 방 문을 두드렸다. 손에 냄비를 들고.\n\"유튜브 보고 만들었어. 김치찌개. 한국 사람 평가 필요해.\"\n국물 위에 파인애플이 떠 있었다.",
                Weight = (c, d) => c.HasMark("rafael") && !RafaelApart(c) && !RafaelGone(c) && Next(c, "foreign_homesick", "foreign_stew", Gap(c) * 2) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "한 그릇 다 비운다", Result = "생각보다... 먹을 만했다. 라파엘이 그날 밤 고향 단톡방에 '한국 형제 인증'이라고 올렸다.", Apply = e => { e.Morale(5); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "\"파인애플은 빼자\" 같이 다시 끓인다", Result = "새벽 한 시, 숙소 공용 주방에서 둘이 찌개를 끓였다. 동료들이 냄새를 맡고 하나둘 모여들었다.", Apply = e => { e.Morale(6); e.Fatigue(5); e.Trust(1); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_rival",
                Title = "적이 된 형제",
                Text = "오늘 상대 더그아웃에 라파엘이 있다. 내가 팀을 옮긴 뒤 처음 맞붙는 날이다.\n경기 전 라파엘이 펜스 너머로 소리쳤다. \"형제! 오늘은 봐주지 않아! 끝나고 밥은 네가 사!\"",
                Weight = (c, d) => RafaelApart(c) && !RafaelGone(c) && d?.MyGame != null && (d.MyGame.Home.Name == c.FormerClub || d.MyGame.Away.Name == c.FormerClub) && !c.Seen("foreign_rival") ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"이기는 사람이 산다!\"", Result = "경기가 끝나고 구장 앞 고깃집. 누가 이겼는지는 상관없었다. 라파엘이 \"새 팀 사람들 착해? 밥 잘 먹어?\" 만 열 번 물었다.", Apply = e => { e.Morale(6); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "말없이 헬멧을 툭 친다", Result = "라파엘이 1루에서 나를 보며 엄지를 들었다. 전광판 너머로도 웃는 게 보였다.", Apply = e => { e.Morale(4); e.Exp(Stat.Contact, 30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_call_apart",
                Title = "밤 열한 시의 전화",
                Text = "밤 열한 시, 라파엘에게서 전화가 왔다.\n\"너 없으니까 밥 혼자 먹어. 한국어 숙제도 검사할 사람 없어.\" 웃는 목소리 뒤로 TV 소리가 들렸다. 내 경기 하이라이트였다.",
                Weight = (c, d) => RafaelApart(c) && !RafaelGone(c) && c.GamesSince("moved") >= Gap(c) * 2 && !c.Seen("foreign_call_apart") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "영상 통화로 바꿔 한국어 숙제를 봐 준다", Result = "받아쓰기 열 문제 중 일곱 개를 맞혔다. '형제'는 한 번도 안 틀렸다.", Apply = e => { e.Morale(5); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "\"쉬는 날 밥 먹으러 갈게\"", Result = "월요일, 두 시간을 달려 갔다. 라파엘이 구장 앞에서 한 시간째 기다리고 있었다.", Apply = e => { e.Morale(6); e.Fatigue(10); e.Mark("rafael_brother"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_leave_far",
                Title = "지구 반대편으로",
                Text = "시즌 막바지, 라파엘에게서 문자가 왔다.\n'나 재계약 못 했어. 다음 주에 집에 가. 너 팀 옮겨서 얼굴 못 보고 가는 거, 그게 제일 아쉬워.'\n\n문자를 몇 번이고 다시 읽었다.",
                Weight = (c, d) => RafaelLeaving(c) && RafaelApart(c) ? 5 : 0,
                Choices =
                {
                    new EventChoice { Label = "새벽 비행기 시간에 맞춰 공항에 간다", Result = "출국장 앞에서 라파엘이 나를 보고 그대로 굳었다. 그러고는 달려와 안았다. \"너, 진짜 형제.\"", Apply = e => { e.Morale(6); e.Fatigue(15); e.Mark("rafael_brother"); } },
                    new EventChoice { Label = "영상 통화로 배웅한다", Result = "\"한국, 좋았어. 너 때문에.\" 화면 너머로 라파엘이 90도로 인사했다. \"수고하셨습니다!\"", Apply = e => e.Morale(4) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_home_call",
                Title = "지구 반대편의 새벽",
                Text = "새벽 다섯 시에 라파엘에게서 영상 통화가 왔다. 그쪽은 저녁이다.\n\"너 어제 경기 봤어! 여기 새벽에 일어나서! 우리 엄마도 봤어!\" 뒤에서 라파엘 어머니가 서툰 한국어로 외쳤다. \"화이팅!\"",
                Weight = (c, d) => Brothers(c) && RafaelGone(c) && SeasonsSince(c, RafaelGoneMark(c)) >= 1 && !c.Seen("foreign_home_call") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "졸린 눈으로 끝까지 수다를 떤다", Result = "해가 뜰 때까지 이야기했다. 고국 리그에서 라파엘은 주장이 됐다고 한다.", Apply = e => { e.Morale(6); e.Fatigue(5); } },
                    new EventChoice { Label = "어머니께 한국어로 인사드린다", Result = "\"아들 잘 부탁해요.\" 라파엘 어머니가 반대로 나를 부탁했다. 둘 다 웃었다.", Apply = e => { e.Morale(5); e.Fans(10); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_visit",
                Title = "관중석의 라파엘",
                Text = "경기 중 외야석에서 어딘가 익숙한 목소리가 들렸다. \"형제! 홈런!\"\n라파엘이었다. 휴가를 내고 몰래 한국에 왔다고 한다. 내 등번호 유니폼을 입고, 손에는 직접 쓴 한글 피켓. '내 형제 {name}'",
                Weight = (c, d) => Brothers(c) && c.Seen("foreign_letter") && SeasonsSince(c, "seen:foreign_letter") >= 1 && d != null && Played(d) && !c.Seen("foreign_visit") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "다음 타석에서 외야석을 가리킨다", Result = "외야석이 뒤집어졌다. 그 장면이 '외국인 형제' 짤로 하루 종일 돌았다.", Apply = e => { e.Fans(60); e.Morale(7); } },
                    new EventChoice { Label = "경기 후 옛 동료들을 모아 회식한다", Result = "라파엘이 그사이 늘어난 한국어로 건배사를 했다. \"수고하셨습니다! 형제들!\"", Apply = e => { e.Morale(6); e.Trust(1); e.Money(-50); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_wedding",
                Title = "축가는 라파엘이",
                Text = "결혼 소식을 듣자마자 라파엘이 비행기 표를 끊었다.\n\"내 형제 결혼식. 안 가면 우리 엄마가 나 집에서 쫓아내.\"\n식장 입구에서 라파엘이 한복을 입고 손님을 맞고 있었다.",
                Weight = (c, d) => Brothers(c) && c.HasMark("married") && c.GamesSince("married") <= c.League.GamesPerSeason && !c.Seen("foreign_wedding") ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "축가를 부탁한다", Result = "라파엘이 고향 노래를 불렀다. 가사는 하나도 몰랐는데 다들 울었다.", Apply = e => { e.Morale(7); e.Fans(20); } },
                    new EventChoice { Label = "사회를 부탁한다", Result = "\"신랑, 입장... 하십시오!\" 한국어 사회에 하객들이 웃다가 박수를 쳤다.", Apply = e => { e.Morale(6); e.Fans(30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "foreign_last",
                Title = "마지막 시즌의 소포",
                Text = "마지막 시즌이라는 기사가 나간 다음 날, 지구 반대편에서 소포가 왔다.\n라파엘이 처음 한국에 왔을 때 쓰던 한국어 노트. 첫 장에 내 글씨가 있었다. '수고하셨습니다.'\n마지막 장에는 라파엘의 글씨. '이번엔 내가 말할게. 수고하셨습니다, 형제.'",
                Weight = (c, d) => Brothers(c) && c.IsFinalSeason && !c.Seen("foreign_last") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "은퇴 경기에 초대한다", Result = "\"당연히 가지. 비행기 벌써 샀어.\" 답장이 1분 만에 왔다.", Apply = e => { e.Morale(8); e.Mark("rafael_last_game"); } },
                    new EventChoice { Label = "노트를 사물함에 넣어 둔다", Result = "마지막 경기 날까지 매일 그 마지막 장을 펴 봤다.", Apply = e => e.Morale(7) },
                },
            });
        }
    }
}
