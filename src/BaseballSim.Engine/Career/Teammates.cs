using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>
    /// Small moments with the people a career is spent beside: the senior who rooms with the rookie,
    /// grows old ahead of him, plays a last game, gets a retirement ceremony and turns up later on TV;
    /// a teammate from the same draft class who doesn't make it; and one day becoming the oldest.
    /// Names are teammates' names, picked once per career (see Career.SeniorName / MateName).
    /// </summary>
    public static partial class StoryEvents
    {
        const string SeniorLeaving = "senior_leaving";
        const string SeniorRetired = "senior_retired";
        const string MateGone = "mate_gone";

        /// <summary>Keeps the first retirement stamp, so "seasons since" counts from the last game, not the ceremony.</summary>
        static void RetireSenior(EventEffects e)
        {
            e.Unmark(SeniorLeaving);
            if (!e.Career.HasMark(SeniorRetired)) e.Mark(SeniorRetired);
        }

        static bool SeniorStillPlays(Career c) => !c.HasMark(SeniorLeaving) && !c.HasMark(SeniorRetired);

        /// <summary>Whole seasons since a mark was made (0 = this season).</summary>
        static int SeasonsSince(Career c, string mark) =>
            c.Me.StoryMarks.TryGetValue(mark, out int stamp) ? c.Season - stamp / 1000 : -1;

        static void AddTeammateEvents(List<StoryEvent> list)
        {
            // ---------- The roommate senior, from the first spring to retirement and after ----------

            list.Add(new StoryEvent
            {
                Id = "senior_roommate",
                Title = "첫 룸메이트",
                Text = "숙소 방문을 열자 {senior} 선배가 침대에 앉아 있었다. 프로 11년 차.\n\"네가 신인이구나. 코 고는 건 이해해 줘라.\"",
                Weight = (c, d) => c.Season == 1 && c.Day >= 2 && !c.Seen("senior_roommate") ? 4 : 0,
                Choices =
                {
                    new EventChoice { Label = "90도로 인사한다", Result = "\"됐다, 됐어. 편하게 해.\" 선배가 웃으며 귤을 던져 줬다.", Apply = e => e.Morale(3) },
                    new EventChoice { Label = "짐 정리를 돕는다", Result = "선배 짐에서 낡은 배팅 장갑이 열 켤레 나왔다. 10년의 흔적이다.", Apply = e => { e.Morale(2); e.Exp(Stat.Contact, 20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_meal",
                Title = "선배가 사 주는 고기",
                Text = "경기 끝나고 {senior} 선배가 불렀다.\n\"고기 먹으러 가자. 신인이 지갑 꺼내면 혼난다.\"",
                Weight = (c, d) => c.Seen("senior_roommate") && SeniorStillPlays(c) && c.Me.Age <= 24 && !c.SeenThisSeason("senior_meal") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "야구 이야기를 묻는다", Result = "\"직구 노리다 변화구 오면? 그냥 커트해. 다음 공이 진짜야.\"", Apply = e => { e.Exp(Stat.Eye, 35); e.Morale(2); } },
                    new EventChoice { Label = "그냥 웃고 떠든다", Result = "선배의 신인 시절 흑역사에 배가 아프도록 웃었다.", Apply = e => { e.Morale(5); e.Fatigue(-5); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_wedding",
                Title = "선배의 결혼식",
                Text = "{senior} 선배가 청첩장을 건넸다.\n\"축가는 네가 해라. 거절은 안 받는다.\"",
                Weight = (c, d) => c.Seen("senior_roommate") && SeniorStillPlays(c) && c.Me.Age <= 23 && !c.Seen("senior_wedding") ? 0.6 : 0,
                Choices =
                {
                    new EventChoice { Label = "축가를 부른다", Result = "고음에서 음이 확 나갔다. 하객석이 뒤집어졌고 선배는 울다 웃었다.", Apply = e => { e.Money(-50); e.Morale(4); e.Fans(10); } },
                    new EventChoice { Label = "축가 대신 사회를 본다", Result = "준비한 멘트가 하나도 안 웃겼다. 그래도 선배는 고맙다고 했다.", Apply = e => { e.Money(-50); e.Morale(3); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_slump",
                Title = "흔들리는 선배",
                Text = "요즘 {senior} 선배가 경기에 잘 못 나온다.\n밤늦게 혼자 실내 연습장에 불이 켜져 있다.",
                Weight = (c, d) => c.Seen("senior_roommate") && SeniorStillPlays(c) && c.Me.Age >= 21 && !c.Seen("senior_slump") ? 0.7 : 0,
                Choices =
                {
                    new EventChoice { Label = "말없이 공을 던져 준다", Result = "한 시간 동안 둘 다 아무 말도 안 했다. 끝나고 선배가 \"고맙다\" 한마디 했다.", Apply = e => { e.Fatigue(10); e.Morale(3); e.Mark("senior_owes"); } },
                    new EventChoice { Label = "모른 척 지나간다", Result = "선배도 혼자 있고 싶을 거라고 생각했다.", Apply = e => e.Fatigue(-5) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_announce",
                Title = "은퇴 발표",
                Text = "구단 발표: {senior}, 올 시즌을 끝으로 은퇴.\n라커룸에서 선배가 웃으며 말했다. \"이제 너희가 해라.\"",
                Weight = (c, d) => c.Seen("senior_roommate") && SeniorStillPlays(c) && c.Me.Age >= 23 && Progress(c) >= 0.55 && Progress(c) < 0.85
                    ? (c.Me.Age >= 25 ? 8 : 1.5) : 0,
                Choices =
                {
                    new EventChoice { Label = "\"1년만 더 하세요\"", Result = "\"몸이 먼저 알더라.\" 선배가 머리를 헝클어뜨렸다.", Apply = e => { e.Mark(SeniorLeaving); e.Morale(-1); } },
                    new EventChoice { Label = "\"수고 많으셨습니다\"", Result = "선배가 오래 손을 잡았다.", Apply = e => { e.Mark(SeniorLeaving); e.Morale(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_last_game",
                Title = "선배의 마지막 경기",
                Text = "오늘은 {senior} 선배의 마지막 경기다.\n마지막 타석, 관중 전체가 일어나 이름을 불렀다.\n경기가 끝나고 선배가 {name}에게 왔다.",
                Weight = (c, d) => c.HasMark(SeniorLeaving) && c.GamesSince(SeniorLeaving) >= 1 && SeasonsSince(c, SeniorLeaving) == 0 ? 8 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "꽃다발을 건넨다", Result = "선배가 {name}의 어깨를 두드렸다. \"이제 네 차례다.\"",
                        Apply = e => { RetireSenior(e); e.Morale(6); },
                    },
                    new EventChoice
                    {
                        Label = "마지막 조언을 청한다", Result = "\"아프면 쉬어. 그게 오래 하는 비결이야.\"",
                        Apply = e => { RetireSenior(e); e.Exp(Stat.Eye, 40); e.Morale(3); },
                    },
                    new EventChoice
                    {
                        Label = "선배의 배트를 물려받는다", Result = "손잡이 테이프가 닳아 반질반질하다. 10년이 넘는 시간이 손에 잡혔다.",
                        Apply = e => { RetireSenior(e); e.Exp(Stat.Power, 35); e.Morale(4); },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_locker",
                Title = "비워진 사물함",
                Text = "{senior} 선배의 사물함이 비었다.\n맨 안쪽에 쪽지 한 장이 붙어 있다. '{name}에게.'",
                Weight = (c, d) => c.HasMark(SeniorRetired) && !c.Seen("senior_locker") ? 3 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "쪽지를 읽는다", Result = "'네가 처음 왔을 때 나보다 잘할 줄 알았다. 틀리지 마라.'",
                        Apply = e => { e.Morale(6); if (e.Career.HasMark("senior_owes")) { e.Unmark("senior_owes"); e.Say("뒷면에 한 줄이 더 있었다. '그날 밤 공 던져 줘서 고마웠다.'"); e.Morale(3); } },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_ceremony",
                Title = "선배의 은퇴식",
                Text = "새 시즌 홈 개막 시리즈. 오늘은 {senior} 선배의 은퇴식이다.\n시구는 선배, 시포는 {name}이 맡게 됐다.",
                Weight = (c, d) => (c.HasMark(SeniorRetired) || c.HasMark(SeniorLeaving)) && SeasonsSince(c, c.HasMark(SeniorRetired) ? SeniorRetired : SeniorLeaving) >= 1
                    && Progress(c) < 0.35 && !c.InFarm && !c.Seen("senior_ceremony") ? 6 : 0,
                Choices =
                {
                    new EventChoice
                    {
                        Label = "시구를 받는다", Result = "공이 원바운드로 들어왔다. 둘 다 웃음이 터졌고, 선배 가족이 울었다.",
                        Apply = e => { RetireSenior(e); e.Morale(6); e.Fans(30); },
                    },
                    new EventChoice
                    {
                        Label = "영상 편지를 준비한다", Result = "전광판에 룸메이트 시절 사진이 나오자 선배가 고개를 돌렸다.",
                        Apply = e => { RetireSenior(e); e.Morale(5); e.Fans(40); },
                    },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_broadcast",
                Title = "해설위원 선배",
                Text = "오늘 중계 해설은 {senior} 위원이었다.\n\"{name} 선수, 신인 때 제 룸메이트였어요. 코를 엄청 골았죠.\"",
                Weight = (c, d) => SeasonsSince(c, SeniorRetired) >= 1 && Played(d) && !c.SeenThisSeason("senior_broadcast") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "다시보기로 챙겨 본다", Result = "칭찬 사이사이 날카로운 지적이 숨어 있었다.", Apply = e => { e.Exp(Stat.Eye, 30); e.Morale(2); } },
                    new EventChoice { Label = "\"코는 선배가 골았잖아요\" 문자", Result = "\"방송은 원래 그런 거다 ㅋㅋ\" 답장이 왔다.", Apply = e => e.Morale(5) },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_shop",
                Title = "선배의 가게",
                Text = "{senior} 선배가 야구장 근처에 고깃집을 열었다.\n벽에는 현역 시절 유니폼이 걸려 있다.",
                Weight = (c, d) => SeasonsSince(c, SeniorRetired) >= 2 && !c.Seen("senior_shop") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "후배들을 데리고 간다 (100만원)", Result = "\"신인이 지갑 꺼내면 혼난다\" 그 말을 이제 내가 하고 있었다.", Apply = e => { e.Money(-100); e.Morale(5); e.Trust(1); } },
                    new EventChoice { Label = "사인 액자를 선물한다", Result = "선배가 계산대 바로 위에 걸었다.", Apply = e => { e.Morale(3); e.Fans(10); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "senior_call",
                Title = "선배의 전화",
                Text = "밤늦게 {senior} 선배에게서 전화가 왔다.\n\"너도 이제 그 나이구나. 몸은 괜찮냐?\"",
                Weight = (c, d) => c.HasMark(SeniorRetired) && c.Me.Age >= 33 && !c.Seen("senior_call") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "\"솔직히 좀 힘들어요\"", Result = "\"아프면 쉬어. 그래야 오래 한다. 내가 그거 못 해서 일찍 관뒀다.\"", Apply = e => { e.Fatigue(-15); e.Morale(5); } },
                    new EventChoice { Label = "\"아직 쌩쌩합니다\"", Result = "\"거짓말하는 거 다 들린다.\" 둘이 한참 웃었다.", Apply = e => e.Morale(4) },
                },
            });

            // ---------- The draft-class mate ----------

            list.Add(new StoryEvent
            {
                Id = "mate_first",
                Title = "입단 동기",
                Text = "같은 해 입단한 {mate}가 1군 첫 출전을 앞두고 덜덜 떨고 있다.\n\"야, 나 손 떨리는 거 보여?\"",
                Weight = (c, d) => c.Season <= 2 && !c.Seen("mate_first") ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "등을 세게 두드려 준다", Result = "\"아파!\" 그래도 떨림은 멈췄다.", Apply = e => e.Morale(3) },
                    new EventChoice { Label = "같이 캐치볼을 한다", Result = "공 몇 개 주고받으니 둘 다 어깨가 풀렸다.", Apply = e => { e.Morale(2); e.Exp(Stat.Defense, 20); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "mate_released",
                Title = "동기의 방출",
                Text = "입단 동기 {mate}가 방출 통보를 받았다.\n짐 상자를 든 채 {name}을 보고 웃는다. \"넌 끝까지 해라.\"",
                Weight = (c, d) => c.Seen("mate_first") && c.Me.Age >= 23 && c.Me.Age <= 27 && !c.HasMark(MateGone) && Progress(c) >= 0.5 ? 0.8 : 0,
                Choices =
                {
                    new EventChoice { Label = "짐을 같이 옮긴다", Result = "주차장까지 걷는 길이 유난히 길었다.", Apply = e => { e.Mark(MateGone); e.Morale(-1); e.Exp(Stat.Contact, 25); } },
                    new EventChoice { Label = "밤새 술잔을 기울인다", Result = "\"우리 둘 중 하나는 끝까지 가야지.\" 그 말이 오래 남았다.", Apply = e => { e.Mark(MateGone); e.Morale(3); e.Fatigue(15); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "mate_visit",
                Title = "동기의 제자들",
                Text = "{mate}가 고교 야구부 코치가 되어 제자들을 데리고 야구장에 왔다.\n\"얘들아, 저 선수가 내 동기야!\"",
                Weight = (c, d) => SeasonsSince(c, MateGone) >= 2 && !c.Seen("mate_visit") ? 1 : 0,
                Choices =
                {
                    new EventChoice { Label = "아이들에게 배팅 시범", Result = "\"우와!\" 탄성 속에서 {mate}가 제일 크게 박수를 쳤다.", Apply = e => { e.Morale(6); e.Fans(20); e.Fatigue(5); } },
                    new EventChoice { Label = "야구부에 공 한 상자 기부 (100만원)", Result = "{mate}가 \"이 녀석들 중에 너 같은 애 나오게 할게\" 했다.", Apply = e => { e.Money(-100); e.Morale(5); e.Fans(30); } },
                },
            });

            // ---------- Getting old ----------

            list.Add(new StoryEvent
            {
                Id = "oldest_now",
                Title = "어느새 최고참",
                Text = "라커룸에 들어서자 신인들이 일제히 일어나 인사했다.\n돌아보니 {name}이 팀 최고참이다.",
                Weight = (c, d) => c.Me.Age >= 33 && !c.Seen("oldest_now") ? 1.5 : 0,
                Choices =
                {
                    new EventChoice { Label = "후배들에게 밥을 산다 (200만원)", Result = "{senior} 선배가 사 주던 그 고깃집으로 데려갔다.", Apply = e => { e.Money(-200); e.Morale(4); e.Trust(1); } },
                    new EventChoice { Label = "말 대신 훈련으로 보여 준다", Result = "제일 먼저 나와 제일 늦게 들어갔다. 후배들이 하나둘 따라 나왔다.", Apply = e => { e.Exp(Stat.Defense, 30); e.Fatigue(10); e.Trust(2); } },
                },
            });

            list.Add(new StoryEvent
            {
                Id = "birthday_surprise",
                Title = "깜짝 생일상",
                Text = "훈련을 마치고 라커룸 불이 갑자기 꺼졌다.\n케이크를 든 후배들이 노래를 부르며 들어온다.",
                Weight = (c, d) => c.Me.Age >= 28 && !c.SeenThisSeason("birthday_surprise") ? 0.4 : 0,
                Choices =
                {
                    new EventChoice { Label = "촛불을 끈다", Result = "케이크 크림이 얼굴에 날아왔다. 이번 생일도 잊지 못할 것 같다.", Apply = e => e.Morale(5) },
                },
            });
        }
    }
}
