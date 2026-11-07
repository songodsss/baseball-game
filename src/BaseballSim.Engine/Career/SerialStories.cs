using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>
    /// More serial side stories that run across many seasons, four or five small scenes each:
    /// the cheer leader who writes the player's song, a kid fan who grows up into a draft pick,
    /// the groundskeeper, and the team bus driver. Each scene waits for the one before it, some
    /// wait for a moment (a home game, a slump, the last season), and earlier choices come back.
    /// </summary>
    public static partial class StoryEvents
    {
        /// <summary>The previous scene of a serial was answered at least this many seasons ago.</summary>
        static bool SeasonsAfter(Career c, string previous, string id, double seasons) =>
            c.Seen(previous) && !c.Seen(id) && c.GamesSince("seen:" + previous) >= (int)(seasons * SeasonGames(c));

        /// <summary>A serial's payoff that must not be left to chance: the last cheer, the kid's arrival.</summary>
        static StoryEvent SerialMoment(Career c, DayResult d)
        {
            foreach (var id in new[] { "cheer_last", "kid_draft" })
            {
                var ev = Find(id);
                if (ev != null && ev.Weight(c, d) > 0) return ev;
            }
            return null;
        }

        static void AddSerialStories(List<StoryEvent> list)
        {
            // ---------- 응원단장: the song ----------

            list.Add(new StoryEvent
            {
                Id = "cheer_song",
                Title = "내 응원가",
                Text = "경기 전, 응원단장이 기타를 메고 라커룸 앞에 나타났다.\n응원단장이 말했다.\n\"{name} 선수 개인 응원가, 이제 만들 때가 됐어요. 어떤 느낌이 좋아요?\"",
                Weight = (c, d) => c.Season >= 2 && c.Me.Popularity >= 15 && !c.Seen("cheer_song") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "다 같이 뛸 수 있는 신나는 곡", Result = "응원단장이 엄지를 들었다.\n\"외야석 무너지게 만들어 볼게요.\"", Apply = e => { e.Morale(3); e.Mark("cheer_loud"); } },
                    new EventChoice { Label = "내 이야기를 담은 가사", Result = "한 시간 동안 어릴 적 이야기를 했다. 응원단장이 수첩에 빼곡히 적었다.", Apply = e => { e.Morale(4); e.Mark("cheer_story"); } },
                    new EventChoice { Label = "\"알아서 해 주세요\"", Result = "\"그럼 제 맘대로 갑니다?\" 응원단장이 씩 웃었다.", Apply = e => e.Morale(2) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cheer_first",
                Title = "첫 떼창",
                Text = "홈경기, 첫 타석.\n외야석에서 처음 듣는 노래가 터져 나왔다. 내 이름이 들어간 노래였다.",
                Weight = (c, d) => HomeGame(c, d) && Played(d) && Next(c, "cheer_song", "cheer_first", Gap(c)) ? 4 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "가사에 귀를 기울인다", Result = "",
                        Apply = e =>
                        {
                            if (e.Career.HasMark("cheer_story")) e.Say("'비 오는 날에도 혼자 남아 휘두르던 배트.' 내가 했던 이야기가 노래가 되어 있었다. 목이 메었다.");
                            else if (e.Career.HasMark("cheer_loud")) e.Say("만 명이 한꺼번에 뛰었다. 발밑이 울렸다.");
                            else e.Say("엉뚱한 가사에 더그아웃이 웃음바다가 됐다. 그런데 이상하게 귀에서 떠나지 않았다.");
                            e.Morale(6);
                            e.Fans(40);
                        },
                    },
                    new EventChoice { Label = "외야석을 향해 헬멧을 들어 올린다", Result = "노래가 두 배로 커졌다.", Apply = e => { e.Morale(5); e.Fans(50); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cheer_slow",
                Title = "느린 응원가",
                Text = "몇 경기째 안 풀린다. 타석에 들어서는데 응원가가 달랐다.\n템포를 절반으로 늦춘, 조용한 버전이었다.\n\n" +
                       "경기 후 응원단장에게서 문자가 왔다.\n'오늘은 천천히 가도 된다는 뜻이었어요. 저희는 계속 부를게요.'",
                Weight = (c, d) => c.HitlessStreak >= 3 && Next(c, "cheer_first", "cheer_slow", SeasonGames(c) / 2) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "'다음 타석은 원래대로 불러 주세요'", Result = "다음 날, 외야석이 원래 템포로 터졌다. 배트가 조금 가벼워졌다.", Apply = e => { e.Morale(6); } },
                    new EventChoice { Label = "응원단 회식에 커피를 보낸다 (30만원)", Result = "응원단 단체 사진이 왔다. 다들 커피를 들고 내 이름을 쓴 종이를 들고 있었다.", Apply = e => { e.Money(-30); e.Morale(5); e.Fans(20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cheer_handover",
                Title = "응원단장의 마지막 시즌",
                Text = "응원단장이 올해를 끝으로 단상을 내려온다고 했다.\n후임은 응원단 막내였다. 내 응원가를 처음 따라 부르던 그 아이.\n\n" +
                       "응원단장이 말했다.\n\"선수님 은퇴하는 날, 마지막 떼창은 제가 다시 와서 할게요. 그건 양보 못 해요.\"",
                Weight = (c, d) => c.Me.Age >= 31 && SeasonsAfter(c, "cheer_first", "cheer_handover", 3) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"약속이에요\"", Result = "응원단장이 새끼손가락을 내밀었다. 다 큰 어른 둘이 손가락을 걸었다.", Apply = e => { e.Morale(4); e.Mark("cheer_promise"); } },
                    new EventChoice { Label = "응원단 마지막 경기에 홈런을 약속한다", Result = "\"그럼 그날은 응원가 세 번 부를게요.\"", Apply = e => { e.Morale(5); e.Mark("cheer_promise"); e.Exp(Stat.Power, 30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "cheer_last",
                Title = "마지막 떼창",
                Text = "마지막 시즌, 마지막 홈경기.\n응원 단상에 낯익은 사람이 올라섰다. 약속대로 돌아온 전 응원단장이었다.\n\n" +
                       "응원단장이 마이크를 잡았다.\n\"오늘은 처음 버전으로 갑니다. 다 같이!\"",
                Weight = (c, d) => c.HasMark("cheer_promise") && c.IsFinalSeason && HomeGame(c, d) && Progress(c) >= 0.85 && !c.Seen("cheer_last") ? 10 : 0,
                Choices =
                {
                    new EventChoice { Label = "끝까지 다 듣고 타석에 들어간다", Result = "노래가 끝날 때까지 심판도 기다려 줬다. 구장 전체가 한 목소리였다.", Apply = e => { e.Unmark("cheer_promise"); e.Morale(10); e.Fans(60); } },
                    new EventChoice { Label = "같이 따라 부른다", Result = "내 응원가를 처음으로 내가 불렀다. 외야석이 웃다가 울었다.", Apply = e => { e.Unmark("cheer_promise"); e.Morale(10); e.Fans(50); } },
                },
            });

            // ---------- 지우: a kid fan who grows up ----------

            list.Add(new StoryEvent
            {
                Id = "kid_glove",
                Title = "외야 펜스의 꼬마",
                Text = "홈경기마다 외야 펜스 앞에 같은 꼬마가 있다. 자기 몸만 한 글러브를 끼고.\n오늘 경기 후, 꼬마가 펜스 너머로 소리쳤다.\n\n" +
                       "꼬마가 말했다.\n\"저 지우예요! 여덟 살! 나중에 형이랑 같은 팀 할 거예요!\"",
                Weight = (c, d) => c.Season <= 5 && HomeGame(c, d) && !c.Seen("kid_glove") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "펜스 너머로 공을 던져 준다", Result = "지우가 공을 두 손으로 받았다. 잡은 공을 머리 위로 들고 펄쩍 뛰었다.", Apply = e => { e.Morale(4); e.Mark("kid_ball"); } },
                    new EventChoice { Label = "글러브에 사인해 준다", Result = "지우가 글러브를 꼭 끌어안았다.\n\"이거 평생 쓸 거예요!\"", Apply = e => { e.Morale(4); e.Fans(10); e.Mark("kid_sign"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "kid_little",
                Title = "리틀야구 사진",
                Text = "구단으로 편지가 왔다. 삐뚤빼뚤한 글씨.\n'형! 저 리틀야구 들어갔어요! 등번호도 형이랑 같은 걸로 했어요. — 지우'\n\n사진 속 꼬마는 조금 커 있었다.",
                Weight = (c, d) => SeasonsAfter(c, "kid_glove", "kid_little", 2) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "답장을 보낸다", Result = "'기본기부터. 공 끝까지 보고. 형도 아직 그거 연습한다.'", Apply = e => { e.Morale(4); e.Mark("kid_letter"); } },
                    new EventChoice { Label = "리틀야구단에 공 한 상자를 보낸다 (50만원)", Result = "단체 사진이 왔다. 아이들 스무 명이 공을 하나씩 들고 있었다. 맨 앞에 지우.", Apply = e => { e.Money(-50); e.Morale(5); e.Fans(20); e.Mark("kid_letter"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "kid_slump",
                Title = "중학생의 고민",
                Text = "SNS 메시지가 왔다. 지우였다. 이제 중학교 야구부라고 했다.\n'형, 저 요즘 하나도 안 맞아요. 야구 그만둘까 봐요. 형도 이런 적 있어요?'",
                Weight = (c, d) => SeasonsAfter(c, "kid_little", "kid_slump", 3) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "내 슬럼프 이야기를 길게 써 보낸다", Result = "한 시간 걸려 답장을 썼다. 2군 시절, 새벽 훈련, 안 풀리던 날들.\n다음 날 답이 왔다. '형도 그랬구나. 한 번만 더 해 볼게요.'", Apply = e => { e.Morale(5); e.Mark("kid_kept"); } },
                    new EventChoice { Label = "\"그만둬도 괜찮아\"라고 보낸다", Result = "'...근데 저 그만두기 싫어요.' 그 답장이 왔을 때, 됐다 싶었다.", Apply = e => { e.Morale(3); e.Mark("kid_kept"); } },
                    new EventChoice { Label = "바빠서 답을 미룬다", Result = "읽지 않은 메시지가 한 달 동안 그대로 있었다.", Apply = e => e.Morale(-1) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "kid_final",
                Title = "고교 야구 결승",
                Text = "고교 야구 대회 결승전 중계를 틀었다.\n4번 타자, 지우. 펜스 앞에서 글러브를 들고 있던 그 꼬마가.",
                Weight = (c, d) => c.HasMark("kid_kept") && SeasonsAfter(c, "kid_slump", "kid_final", 3) ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "쉬는 날 경기장에 직접 간다", Result = "관중석 구석에 앉았는데, 지우가 타석에서 나를 알아봤다. 헬멧을 살짝 들었다. 그 타석에서 결승타가 나왔다.", Apply = e => { e.Fatigue(5); e.Morale(8); e.Mark("kid_watched"); } },
                    new EventChoice { Label = "응원 영상을 보낸다", Result = "경기 후 지우의 인터뷰. '어릴 때부터 응원한 선배님이 영상을 보내 주셨어요.'", Apply = e => { e.Morale(5); e.Fans(30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "kid_draft",
                Title = "그 공, 아직 있어요",
                Text = "올해 드래프트. {team}의 테이블에서 지우의 이름이 불렸다.\n\n" +
                       "신인 입단식 날, 지우가 내 앞에 섰다. 손에는 낡은 야구공이 들려 있었다.\n" +
                       "지우가 말했다.\n\"선배님. 그 공, 아직 있어요. 약속 지키러 왔습니다.\"",
                Weight = (c, d) => c.Seen("kid_final") && c.Day <= Gap(c) && SeasonsAfter(c, "kid_final", "kid_draft", 0.5) ? 6 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"이제 같은 팀이다\"", Result = "그날 첫 캐치볼 상대는 지우였다. 10년 전 펜스 너머로 공을 던져 주던 그 거리에서.", Apply = e => { e.Morale(10); e.Fans(40); } },
                    new EventChoice { Label = "내 첫 배트를 선물한다", Result = "지우가 배트를 받아 들고 한참 고개를 숙였다. 신인 사물함 위에 그 배트가 걸렸다.", Apply = e => { e.Morale(8); e.Fans(30); } },
                },
            });

            // ---------- 박 반장: the groundskeeper ----------

            list.Add(new StoryEvent
            {
                Id = "keeper_rain",
                Title = "방수포",
                Text = "경기 중 갑자기 비가 쏟아졌다. 그라운드 관리팀이 방수포를 끌고 뛰어나왔다.\n맨 앞에서 소리치는 사람은 30년째 구장을 관리하는 박 반장이었다.",
                Weight = (c, d) => HomeGame(c, d) && c.Season >= 2 && !c.Seen("keeper_rain") ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "같이 방수포를 끈다", Result = "흠뻑 젖었다. 박 반장이 수건을 던져 줬다.\n\"선수가 이런 거 하는 거 처음 본다. 고맙다.\"", Apply = e => { e.Fatigue(5); e.Morale(4); e.Mark("keeper_friend"); } },
                    new EventChoice { Label = "더그아웃에서 박수를 보낸다", Result = "박 반장이 비를 맞으며 손을 흔들었다.", Apply = e => e.Morale(2) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "keeper_secret",
                Title = "내 자리의 흙",
                Text = "요즘 홈경기 때마다 발밑이 유난히 편하다.\n경기 전 일찍 나가 보니, 박 반장이 내 자리 흙을 손으로 고르고 있었다.\n\n" +
                       "박 반장이 머쓱하게 웃었다.\n\"네 스파이크 자국 보고 맞췄다. 너한테만 해 주는 거다.\"",
                Weight = (c, d) => c.HasMark("keeper_friend") && HomeGame(c, d) && Next(c, "keeper_rain", "keeper_secret", SeasonGames(c)) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"반장님 흙이 최고예요\"", Result = "박 반장이 흙을 한 번 더 툭툭 다졌다. 그날 발이 땅에 붙는 것 같았다.", Apply = e => { e.Morale(5); e.Exp(Stat.Defense, 30); } },
                    new EventChoice { Label = "관리팀에 간식을 돌린다 (30만원)", Result = "관리팀 창고 앞에 내 사인 종이가 붙었다. '흙 담당 VIP'.", Apply = e => { e.Money(-30); e.Morale(4); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "keeper_storm",
                Title = "태풍 전야",
                Text = "태풍이 올라온다는 예보. 밤늦게 구장 앞을 지나는데 그라운드에 불이 켜져 있었다.\n박 반장 혼자 모래주머니를 나르고 있었다.",
                Weight = (c, d) => c.HasMark("keeper_friend") && InWindow(c, 0.45, 0.8) && SeasonsAfter(c, "keeper_secret", "keeper_storm", 1) ? 3 : 0,
                Choices =
                {
                    new EventChoice { Label = "차를 세우고 같이 나른다", Result = "새벽 2시까지 모래주머니를 날랐다. 다음 날 그라운드는 멀쩡했다. 박 반장이 컵라면을 끓여 줬다.", Apply = e => { e.Fatigue(15); e.Morale(6); e.Mark("keeper_storm_help"); } },
                    new EventChoice { Label = "동료들에게 연락을 돌린다", Result = "삼십 분 만에 선수 열 명이 모였다. 박 반장이 말을 잇지 못했다.", Apply = e => { e.Fatigue(10); e.Morale(5); e.Fans(20); e.Mark("keeper_storm_help"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "keeper_retire",
                Title = "흙 한 병",
                Text = "박 반장이 은퇴한다고 했다. 30년 만이었다.\n마지막 날, 그가 작은 유리병 하나를 내밀었다. 안에는 흙이 담겨 있었다.\n\n" +
                       "박 반장이 말했다.\n\"네 자리 흙이다. 네가 뛴 날은 다 이 흙 위였다.\"",
                Weight = (c, d) => c.Me.Age >= 30 && SeasonsAfter(c, "keeper_storm", "keeper_retire", 2) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "병을 사물함에 둔다", Result = "그 뒤로 경기 전마다 병을 한 번씩 만졌다.", Apply = e => { e.Morale(8); } },
                    new EventChoice { Label = "관리팀과 기념사진을 찍는다", Result = "", Apply = e => { e.Say(e.Career.HasMark("keeper_storm_help") ? "사진 속 박 반장 옆에 그 태풍 밤의 모래주머니가 쌓여 있었다." : "사진 속 박 반장이 흙 묻은 장갑을 낀 채 웃고 있었다."); e.Morale(6); e.Fans(20); } },
                },
            });

            // ---------- 김 기사님: the team bus ----------

            list.Add(new StoryEvent
            {
                Id = "bus_music",
                Title = "원정 버스의 트로트",
                Text = "원정 버스 안. 기사님이 오늘도 트로트를 틀었다. 선수들이 이어폰을 꽂았다.\n기사님이 룸미러로 나를 봤다.\n\"막내, 노래 하나 골라 봐라. 오늘은 네 차례다.\"",
                Weight = (c, d) => c.Season <= 4 && d.MyGame != null && d.MyGame.Home != c.MyTeam && !c.Seen("bus_music") ? 2 : 0,
                Choices =
                {
                    new EventChoice { Label = "기사님 18번을 고른다", Result = "기사님이 볼륨을 올렸다. 버스 안이 떼창이 됐다.", Apply = e => { e.Morale(4); e.Mark("bus_friend"); } },
                    new EventChoice { Label = "요즘 노래를 고른다", Result = "기사님이 한 소절 듣고 끄덕였다.\n\"이것도 괜찮네.\" 다음 원정부터 그 노래가 기사님 플레이리스트에 들어갔다.", Apply = e => { e.Morale(3); e.Mark("bus_friend"); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "bus_daughter",
                Title = "기사님의 딸",
                Text = "원정길 휴게소. 기사님이 쑥스럽게 청첩장을 내밀었다. 딸이 결혼한다고 했다.\n\"선수들한테 주긴 좀 그런데... 너는 노래 골라 준 놈이니까.\"",
                Weight = (c, d) => c.HasMark("bus_friend") && SeasonsAfter(c, "bus_music", "bus_daughter", 1.5) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "쉬는 날 결혼식에 간다", Result = "신부 입장 때 기사님이 울었다. 하객석에서 선수 하나가 같이 울고 있었다는 건 비밀이다.", Apply = e => { e.Fatigue(5); e.Morale(6); e.Mark("bus_wedding"); } },
                    new EventChoice { Label = "사인 유니폼을 선물한다", Result = "신혼집 거실에 내 유니폼이 걸렸다는 사진이 왔다.", Apply = e => { e.Morale(4); e.Fans(10); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "bus_night",
                Title = "새벽 휴게소",
                Text = "새벽 원정길, 다들 잠든 버스. 휴게소에서 기사님과 둘이 커피를 마셨다.\n\n" +
                       "기사님이 말했다.\n\"30년 운전했다. 내가 처음 태운 신인이 지금 감독이다. 너도 언젠가 맨 앞자리에 앉겠지.\"",
                Weight = (c, d) => c.HasMark("bus_friend") && SeasonsAfter(c, "bus_daughter", "bus_night", 2) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"그때도 기사님이 운전해 주세요\"", Result = "기사님이 웃었다.\n\"그때까지 버티려면 나도 운동해야겠다.\"", Apply = e => { e.Morale(5); } },
                    new EventChoice { Label = "운전석 옆에 앉아 남은 길을 같이 간다", Result = "해가 뜰 때까지 이야기했다. 30년 동안 버스에서 본 선수들 이야기.", Apply = e => { e.Fatigue(5); e.Morale(4); e.Exp(Stat.Eye, 30); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "bus_last",
                Title = "마지막 운행",
                Text = "기사님의 마지막 운행 날. 원정을 마치고 돌아오는 버스.\n구장 주차장에 도착하자 기사님이 시동을 끄고 한참 핸들을 쥐고 있었다.",
                Weight = (c, d) => c.Me.Age >= 31 && SeasonsAfter(c, "bus_night", "bus_last", 2) ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "선수단이 서명한 모자를 건넨다", Result = "기사님이 모자를 쓰고 룸미러를 봤다.\n\"이제 손님으로 타야겠네.\" 버스 안 모두가 박수를 쳤다.", Apply = e => { e.Morale(8); } },
                    new EventChoice
                    {
                        Label = "기사님 18번을 다 같이 부른다", Result = "",
                        Apply = e =>
                        {
                            e.Say(e.Career.HasMark("bus_wedding") ? "맨 뒷자리에서 결혼식 날의 그 노래가 시작됐다. 기사님이 핸들에 이마를 대고 웃었다." : "트로트 떼창이 주차장까지 울렸다. 기사님이 마지막으로 볼륨을 끝까지 올렸다.");
                            e.Morale(8);
                        },
                    },
                },
            });
        }
    }
}
