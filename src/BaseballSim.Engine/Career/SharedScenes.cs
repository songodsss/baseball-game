using System.Collections.Generic;
using System.Linq;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>
    /// Scenes any of the stories can use, told through the story's own four people by placeholder
    /// ({coach} {pal} {kin} {voice}). A slot can have several; each career gets one of them, or one
    /// of the story's own, at random.
    /// </summary>
    internal static class SharedScenes
    {
        public static readonly List<StoryScene> All = Build();

        /// <summary>A career count worth a ceremony: hits for a hitter, else wins, saves or holds.</summary>
        static (string Label, int Count) Tally(Career c)
        {
            var me = c.Me;
            if (!me.IsPitcher) return ("안타", c.SeasonLine.Hits + me.History.Sum(h => h.Line?.Hits ?? 0));
            int Count(PitcherLine l) => l == null ? 0 : me.Role == PitcherRole.Closer ? l.Saves : me.Role == PitcherRole.Setup ? l.Holds : l.Wins;
            string label = me.Role == PitcherRole.Closer ? "세이브" : me.Role == PitcherRole.Setup ? "홀드" : "승";
            return (label, Count(c.SeasonPitching) + me.History.Sum(h => Count(h.Pitching)));
        }

        static string Round(int n) => n >= 1000 ? $"{n / 500 * 500:N0}" : n >= 100 ? $"{n / 50 * 50}" : $"{n / 10 * 10}";

        static List<StoryScene> Build() => new List<StoryScene>
        {
            // ----- Prologue: the first spring camp -----
            new StoryScene
            {
                Slot = "camp", Title = "첫 스프링캠프",
                Text = c => "따뜻한 남쪽 섬, 첫 스프링캠프.\n선배들의 공 소리부터가 달랐다.\n\n저녁 미팅이 끝나고 {mentor:이} 명단을 툭 던졌다.\n\"내일 청백전. 너 이름 있다. 긴장하라고 미리 말해 주는 거다.\"",
                Options = c => new List<StoryOption>
                {
                    O("밤새 영상을 본다", "상대 선배들의 버릇을 공책에 빼곡히 적었다. 잠은 세 시간 잤다.",
                        (e, k) => { e.Exp(Stat.Eye, 40); e.Fatigue(10); }),
                    O("푹 자고 나간다", "청백전 첫 타석, 몸이 가벼웠다. {coach:이} 고개를 한 번 끄덕였다.",
                        (e, k) => { e.Fatigue(-10); e.Morale(4); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "camp", Title = "캠프의 막내",
                Text = c => "스프링캠프 막내의 하루는 길다.\n공 줍기, 장비 나르기, 선배들 물병 채우기.\n\n밤 열한 시, 혼자 남은 실내 연습장. 문이 열리고 {pal:이} 들어왔다.\n\"역시 여기 있을 줄 알았다. 같이 하자.\"",
                Options = c => new List<StoryOption>
                {
                    O("같이 배트를 돌린다", "자정이 넘도록 둘이서 번갈아 공을 올려 줬다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Fatigue(10); ChangeBond(e, k, Bond.Friend, 10); }),
                    O("\"내일 위해 들어가자\"", "숙소로 걸어가며 별을 봤다. 서울에선 못 보던 하늘이었다.",
                        (e, k) => { e.Fatigue(-10); e.Morale(3); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },

            // ----- 1. Debut -----
            new StoryScene
            {
                Slot = "first_pay", Title = "첫 월급",
                Text = c => "통장에 처음으로 구단 이름이 찍혔다. 첫 월급.\n생각보다 적고, 생각보다 무거웠다.\n\n{kin}에게서 문자가 왔다. '첫 월급 들어왔지? 너 먹고 싶은 거 사 먹어. 우린 괜찮아.'",
                Options = c => new List<StoryOption>
                {
                    O("{kin}에게 선물을 보낸다 (50만원)", "택배가 도착했다는 사진이 왔다. 사진 속 얼굴이 웃고 있었다.",
                        (e, k) => { e.Money(-50); e.Morale(5); ChangeBond(e, k, Bond.Family, 10); }),
                    O("새 배팅 장갑을 산다 (30만원)", "첫 월급으로 산 장갑. 손에 꼭 맞았다.",
                        (e, k) => { e.Money(-30); e.Exp(Stat.Contact, 40); }),
                    O("고스란히 모아 둔다", "통장을 닫았다. 언젠가 크게 쓸 날이 올 거다.",
                        (e, k) => e.Money(30)),
                },
            },
            new StoryScene
            {
                Slot = "first_pay", Title = "첫 사인",
                Text = c => "경기 전 펜스 앞, 꼬마 하나가 공을 내밀었다.\n\"사인해 주세요!\"\n\n태어나서 처음 해 보는 사인이었다.\n이름 석 자를 쓰는데 손이 떨렸다.",
                Options = c => new List<StoryOption>
                {
                    O("정성껏 이름과 등번호를 쓴다", "꼬마가 공을 머리 위로 들고 뛰어갔다. 첫 팬이 생겼다.",
                        (e, k) => { e.Fans(30); e.Morale(4); }),
                    O("멋진 사인을 연습하기로 한다", "그날 밤 숙소 메모지가 사인으로 가득 찼다. {pal:이} 배를 잡고 웃었다.",
                        (e, k) => { e.Morale(3); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "first_winter", Title = "첫 겨울",
                Text = c => "첫 시즌이 끝났다. 1년이 이렇게 빨리 갈 줄은 몰랐다.\n\n비활동 기간, 선수들은 다들 흩어졌다.\n어디서 겨울을 보낼지 정해야 한다.",
                Options = c => new List<StoryOption>
                {
                    O("{kin}에게 간다", "오랜만에 집밥을 먹었다. 낡은 방에 붙은 어릴 적 사진이 그대로였다.",
                        (e, k) => { e.Morale(6); e.Fatigue(-20); ChangeBond(e, k, Bond.Family, 10); }),
                    O("{coach}의 개인 훈련에 따라간다", "한겨울 새벽 산길 달리기. \"봄에 웃으려면 겨울에 울어야 한다.\"",
                        (e, k) => { e.Exp(Stat.Power, Scaled(k, Bond.Mentor, 60)); e.Fatigue(15); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("해외 야구 캠프에 간다 (200만원)", "말도 잘 안 통했지만, 배트 소리는 어디나 같았다.",
                        (e, k) => { e.Money(-200); e.Exp(Stat.Eye, 60); e.Morale(3); }),
                },
            },
            new StoryScene
            {
                Slot = "first_winter", Title = "연봉 계약서",
                Text = c => "구단 사무실. 2년 차 연봉 계약서가 책상 위에 놓였다.\n\n숫자보다 먼저 눈에 들어온 건 맨 아래 한 줄이었다.\n'계약 기간: 1년.'\n\n프로는 매년 다시 증명하는 곳이다.",
                Options = c => new List<StoryOption>
                {
                    O("말없이 사인한다", "펜을 내려놓고 곧장 훈련장으로 갔다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Morale(3); }),
                    O("계약서를 사진으로 남긴다", "'내년엔 이 숫자를 바꾼다.' 휴대폰 배경 화면이 됐다.",
                        (e, k) => e.Morale(6)),
                },
            },

            // ----- 2. The first wall -----
            new StoryScene
            {
                Slot = "sophomore", Title = "2년 차 징크스",
                Text = c => "\"2년 차 징크스 조심해라.\" 다들 한마디씩 했다.\n\n정말로, 작년에 통하던 게 안 통한다.\n상대 팀 전력분석실에 내 영상이 수십 개는 쌓였을 거다.",
                Options = c => new List<StoryOption>
                {
                    O("내 약점 영상을 직접 본다", "보기 싫은 장면만 골라 봤다. 고칠 곳이 보이기 시작했다.",
                        (e, k) => { e.Exp(Stat.Eye, 50); e.Morale(-2); }),
                    O("{coach}에게 묻는다", "\"상대가 너를 연구한다는 건, 이제 네가 연구할 가치가 있다는 거다.\"",
                        (e, k) => { e.Exp(Stat.Contact, Scaled(k, Bond.Mentor, 40)); e.Morale(3); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "sophomore", Title = "후배가 생겼다",
                Text = c => "새 시즌, 신인들이 들어왔다.\n작년의 나처럼 잔뜩 얼어서 인사를 한다.\n\n\"선배님! 작년에 경기 다 봤습니다!\"\n선배님. 그 말이 아직 어색하다.",
                Options = c => new List<StoryOption>
                {
                    O("밥을 사 준다 (20만원)", "신인들이 고기를 굽다 말고 꾸벅꾸벅 인사를 했다.",
                        (e, k) => { e.Money(-20); e.Morale(4); e.Fans(10); }),
                    O("\"나도 아직 배우는 중이야\"", "그 말을 하고 나니 오히려 마음이 편해졌다.",
                        (e, k) => { e.Morale(3); e.Exp(Stat.Eye, 30); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_past", Title = "{coach}의 옛날 사진",
                Text = c => "구단 역사관 청소를 돕다가 오래된 사진 한 장을 발견했다.\n앳된 얼굴로 헬멧을 쓴 젊은 선수. 낯이 익다.\n\n사진 아래 이름표, '{coach}'.\n\"...그거 어디서 났냐.\" 등 뒤에서 목소리가 들렸다.",
                Options = c => new List<StoryOption>
                {
                    O("현역 시절 이야기를 청한다", "\"세 시즌 만에 어깨가 나갔다. 그래서 너 같은 놈들 보면 아깝다.\" 처음 듣는 이야기였다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 15); Flag(k, "mentor_past"); }),
                    O("모른 척 사진을 제자리에 둔다", "{coach:이} 헛기침을 하고 지나갔다. 다음 날 훈련은 유난히 꼼꼼했다.",
                        (e, k) => { e.Exp(Stat.Defense, 40); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_past", Title = "같은 자리, 다른 시절",
                Text = c => "원정 경기 날, 오래된 지방 구장.\n{mentor:이} 외야 펜스의 움푹 팬 자국 앞에 한참 서 있었다.\n\n\"여기서 공 쫓다가 부딪혔다. 그날로 선수 생활 끝났지.\"\n담담한 목소리였다.",
                Options = c => new List<StoryOption>
                {
                    O("\"그래도 지금 여기 계시잖아요\"", "{coach:이} 피식 웃었다. \"그래. 너 같은 놈 보려고 남았나 보다.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 10); Flag(k, "mentor_past"); }),
                    O("그 자리에서 함께 펑고를 받는다", "펜스 앞에서 공을 받을 때마다 {coach:이} 작게 \"좋다\"고 했다.",
                        (e, k) => { e.Exp(Stat.Defense, Scaled(k, Bond.Mentor, 50)); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "breakthrough", Title = "그날의 감각",
                Text = c => "공이 멈춰 보였다.\n소문으로만 듣던 그 감각. 오늘 {mound}에서, 처음으로 그게 왔다.\n\n경기 후 휴대폰에 메시지가 쌓여 있었다.\n맨 위는 {kin}. '오늘 TV로 봤어. 너 맞지?'",
                Options = c => new List<StoryOption>
                {
                    O("감각을 잊기 전에 훈련한다", "경기가 끝났는데도 몸이 근질거렸다. 실내 연습장 불이 새벽까지 켜져 있었다.",
                        (e, k) => { e.Exp(Stat.Contact, 60); e.Fatigue(15); }),
                    O("{kin}에게 전화한다", "\"나 맞아.\" 그 말에 수화기 너머가 한참 조용했다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 10); }),
                },
            },

            // ----- 3. The spot that calls your name -----
            new StoryScene
            {
                Slot = "pressure", Title = "댓글 창",
                Text = c => "한 경기 못 했을 뿐인데, 댓글 창이 난리다.\n'거품이었네.' '2군 보내라.'\n\n휴대폰을 쥔 손에 힘이 들어갔다.\n그때 {voice}에게서 메시지가 왔다. '오늘 댓글은 보지 마요. 진심으로.'",
                Options = c => new List<StoryOption>
                {
                    O("앱을 지운다", "조용해진 휴대폰. 그 대신 내 스윙 영상을 다시 봤다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 30); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("끝까지 다 읽는다", "아팠다. 그래도 그중 몇 개는 맞는 말이었다.",
                        (e, k) => { e.Morale(-3); e.Exp(Stat.Contact, 60); }),
                },
            },
            new StoryScene
            {
                Slot = "pressure", Title = "유니폼 뒤의 이름",
                Text = c => "구단 스토어에 내 이름이 박힌 유니폼이 걸렸다.\n지나가던 아이가 엄마 손을 끌었다. \"이거! 이 형 거!\"\n\n뿌듯함과 동시에, 등이 조금 무거워졌다.\n이제 내 플레이를 보고 누군가 야구를 배운다.",
                Options = c => new List<StoryOption>
                {
                    O("아이에게 다가가 사인해 준다", "아이는 유니폼을 꼭 안고 한참을 서 있었다.",
                        (e, k) => { e.Fans(40); e.Morale(5); }),
                    O("그날 밤 혼자 훈련한다", "'저 유니폼 입고 실망하는 아이는 없게 하자.'",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); }),
                },
            },
            new StoryScene
            {
                Slot = "contract", Title = "다년 계약 제안",
                Text = c => "구단 사장이 직접 불렀다.\n\"장기 계약 이야기를 하고 싶네. 우리 팀의 얼굴이 되어 줬으면 해.\"\n\n계약서를 들고 나오는데 {pal}에게서 전화가 왔다.\n\"들었다! 너 이제 진짜 거물이다?\"",
                Options = c => new List<StoryOption>
                {
                    O("\"이 팀에서 끝까지 하고 싶습니다\"", "사장이 손을 내밀었다. 손이 생각보다 따뜻했다.",
                        (e, k) => { e.Morale(6); e.Fans(40); Flag(k, "loyal"); }),
                    O("\"시장 평가도 듣고 싶습니다\"", "\"당연하지. 그만큼 자신 있다는 거니까.\" 사장의 웃음이 조금 굳었다.",
                        (e, k) => { e.Morale(3); e.Exp(Stat.Eye, 30); }),
                },
            },
            new StoryScene
            {
                Slot = "contract", Title = "에이전트",
                Text = c => "에이전트라는 사람들이 명함을 들고 찾아오기 시작했다.\n\"선수님 정도면 지금 몸값 두 배는 받아야 합니다.\"\n\n명함 더미를 보며 {kin:이} 걱정스럽게 말했다.\n\"너 돈 때문에 야구 시작한 거 아니잖아.\"",
                Options = c => new List<StoryOption>
                {
                    O("{kin}의 말을 새긴다", "명함은 서랍에 넣었다. 대신 오래 함께할 사람 한 명만 골랐다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 10); }),
                    O("가장 유능한 에이전트를 고른다", "\"맡겨만 주세요.\" 다음 날 협찬 제안이 세 건 들어왔다.",
                        (e, k) => { e.Money(150); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "kin_visit", Title = "{kin}의 직관",
                Text = c => "오늘 관중석 어딘가에 {kin:이} 있다.\n티켓을 보내 줬는데, 꼭 오겠다고 했다.\n\n몸을 푸는데 자꾸 관중석으로 눈이 간다.\n저기, 응원 도구를 어색하게 든 익숙한 실루엣.",
                Options = c => new List<StoryOption>
                {
                    O("손을 크게 흔든다", "{kin:이} 수줍게 손을 흔들었다. 그날 경기는 이상하게 몸이 가벼웠다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Family, 10); }),
                    O("모른 척 경기에 집중한다", "경기 후에 만난 {kin:이} 말했다. \"TV보다 훨씬 멋있더라.\"",
                        (e, k) => { e.Exp(Stat.Eye, 40); ChangeBond(e, k, Bond.Family, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "kin_visit", Title = "늦은 밤 전화",
                Text = c => "원정 숙소, 밤 열두 시. {kin}에게서 전화가 왔다.\n\"별일 아니야. 그냥 목소리 듣고 싶어서.\"\n\n별일 아니라는 목소리가 조금 떨렸다.\n요즘 집에 너무 소홀했다.",
                Options = c => new List<StoryOption>
                {
                    O("휴식일에 집에 간다", "왕복 다섯 시간. 그래도 같이 밥 한 끼 먹고 나니 다 괜찮아졌다.",
                        (e, k) => { e.Fatigue(10); e.Morale(5); ChangeBond(e, k, Bond.Family, 15); }),
                    O("한 시간 넘게 통화한다", "시시콜콜한 이야기만 했다. 그래서 좋았다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Family, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "race", Title = "마지막 한 경기",
                Text = c => "시즌 막바지, 개인 타이틀 경쟁이 한 끗 차이로 좁혀졌다.\n\n{voice:이} 물었다. \"타이틀, 솔직히 욕심나죠?\"\n솔직히, 난다.",
                Options = c => new List<StoryOption>
                {
                    O("\"팀이 먼저예요\"", "그 말이 기사 제목이 됐다. 그날 밤 팬 카페가 뜨거웠다.",
                        (e, k) => { e.Fans(40); e.Morale(4); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("\"네, 갖고 싶어요\"", "{voice:이} 웃었다. \"그런 솔직함, 좋아요.\" 그날 연습량이 두 배였다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },

            // ----- 4. The summit -----
            new StoryScene
            {
                Slot = "bigstage", Title = "가을의 공기",
                Text = c => "시즌 막바지, 순위가 걸린 홈 경기.\n관중석이 끝까지 꽉 찼다. 응원 소리에 발밑이 울린다.\n\n더그아웃에서 {mentor:이} 말했다.\n\"이런 날을 위해 그 많은 날을 버틴 거다. 즐겨라.\"",
                Options = c => new List<StoryOption>
                {
                    O("관중의 함성을 온몸으로 듣는다", "숨이 차오를 만큼 짜릿했다. 이 맛에 야구를 한다.",
                        (e, k) => { e.Morale(8); e.Fans(40); }),
                    O("평소처럼 루틴을 지킨다", "큰 경기일수록 작은 것부터. 손끝이 차분해졌다.",
                        (e, k) => { e.Exp(Stat.Eye, 50); e.Morale(3); ChangeBond(e, k, Bond.Mentor, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "bigstage", Title = "만원 관중",
                Text = c => "오늘 매진. 구단 역사상 가장 빠른 매진이라고 한다.\n전광판에 내 얼굴이 크게 걸렸다.\n\n{pal:이} 옆에서 장비를 챙기며 중얼거렸다.\n\"야, 처음 만났을 때 이런 날 올 줄 알았냐.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"아니. 근데 올 줄은 알았어\"", "{pal:이} 크게 웃으며 등을 쳤다.",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Friend, 10); }),
                    O("말없이 주먹을 맞댄다", "그거면 충분했다. 둘 다 그라운드로 뛰어나갔다.",
                        (e, k) => { e.Morale(4); e.Fans(30); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "다큐멘터리",
                Text = c => "{voice:이} 진지한 얼굴로 찾아왔다.\n\"한 시즌 동안 {name}의 이야기를 기록하고 싶어요. 숙소, 훈련, 경기 전부요.\"\n\n카메라가 따라다니는 1년이라니. 쉽지 않을 거다.",
                Options = c => new List<StoryOption>
                {
                    O("\"좋아요, 다 보여 줄게요\"", "다큐는 공개 첫 주에 화제가 됐다. 새벽 훈련 장면에서 댓글이 가장 많았다.",
                        (e, k) => { e.Fans(80); e.Fatigue(10); ChangeBond(e, k, Bond.Voice, 15); Flag(k, "documentary"); }),
                    O("\"경기장 안만 찍어 주세요\"", "{voice:이} 고개를 끄덕였다. \"선 지켜 주는 게 더 멋있어요.\"",
                        (e, k) => { e.Fans(30); e.Morale(3); ChangeBond(e, k, Bond.Voice, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "voice_feature", Title = "그때 그 한마디",
                Text = c => "{voice:이} 오래된 녹음 파일을 하나 틀어 줬다.\n신인 시절의 내 목소리. 잔뜩 긴장해서 말끝이 떨렸다.\n\n\"그때 이런 말 했던 거 기억나요?\n'언젠가 리그에서 제일 무서운 선수가 되고 싶다.'\"",
                Options = c => new List<StoryOption>
                {
                    O("\"아직 그 꿈 진행 중이에요\"", "{voice:이} 녹음 버튼을 다시 눌렀다. \"그럼 오늘 것도 남겨 둘게요.\"",
                        (e, k) => { e.Morale(6); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("부끄러워 귀를 막는다", "둘이서 한참 웃었다. 그 시절 내가 조금 기특했다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Voice, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "rival_peak", Title = "{rival:과}의 정상 대결",
                Text = c => "리그 최고를 다투는 두 이름. 나와 {rival}.\n스포츠 뉴스는 매일 둘을 나란히 비교했다.\n\n오늘은 직접 맞붙는 날. 경기 전 복도에서 {rival:이} 먼저 말을 걸었다.\n\"10년 넘게 너만 보고 달렸다. 알고 있냐.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"나도 그래\"", "{rival:이} 처음으로 웃었다. 그날 경기는 둘 다 최고였다.",
                        (e, k) => { e.Morale(6); e.Fans(40); Flag(k, "rival_respect"); }),
                    O("\"오늘도 내가 이긴다\"", "{rival}의 눈빛이 번쩍였다. 공 하나하나가 전쟁이었다.",
                        (e, k) => { e.Exp(Stat.Power, 60); e.Fatigue(10); }),
                },
            },
            new StoryScene
            {
                Slot = "rival_peak", Title = "같은 비행기",
                Text = c => "올스타 휴식기, 우연히 같은 비행기를 탔다. 옆자리에 {rival}.\n어색한 침묵이 십 분쯤 흘렀다.\n\n\"...신인왕 경쟁하던 해에, 너 때문에 밤에 잠이 안 왔다.\"\n{rival:이} 창밖을 보며 말했다.",
                Options = c => new List<StoryOption>
                {
                    O("나도 그랬다고 털어놓는다", "착륙할 때까지 이야기가 끊이지 않았다. 적인데, 이상하게 편했다.",
                        (e, k) => { e.Morale(6); Flag(k, "rival_respect"); }),
                    O("이어폰을 낀다", "착륙 후 {rival:이} 먼저 일어나며 말했다. \"다음 경기에서 보자.\"",
                        (e, k) => { e.Exp(Stat.Eye, 40); e.Morale(2); }),
                },
            },

            // ----- 5. Veteran -----
            new StoryScene
            {
                Slot = "body", Title = "트레이닝실의 거울",
                Text = c => "트레이닝실 거울 앞. 이마에 주름이 하나 늘었다.\n옆에서 운동하던 신인이 내 무게의 두 배를 든다.\n\n{pal:이} 슬쩍 다가와 속삭였다. \"우리도 저럴 때가 있었지.\"",
                Options = c => new List<StoryOption>
                {
                    O("유연성 훈련으로 바꾼다", "요가 매트 위에서 땀을 흘렸다. 몸이 조금씩 부드러워졌다.",
                        (e, k) => { e.Fatigue(-20); e.Exp(Stat.Defense, 40); }),
                    O("\"지금도 할 수 있어\"", "{pal:이} 말렸지만 기어이 무게를 올렸다. 다음 날 온몸이 쑤셨다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(20); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "role_change", Title = "달라진 자리",
                Text = c => (c.Me.IsPitcher
                                ? "{mentor}의 호출. 다음 주부터 등판 간격을 조정하겠다고 한다.\n"
                                : "라인업 카드. 오랫동안 지켜 온 타순 자리에 젊은 후배의 이름이 적혀 있다.\n") +
                            "\"네가 못해서가 아니다. 네가 오래 뛰었으면 해서다.\"\n\n머리로는 알겠는데, 마음이 따라오지 않는다.",
                Options = c => new List<StoryOption>
                {
                    O("받아들이고 새 역할을 익힌다", "자리는 바뀌어도 할 일은 많았다. 경기를 보는 눈이 넓어졌다.",
                        (e, k) => { e.Exp(Stat.Eye, 60); e.Morale(3); ChangeBond(e, k, Bond.Mentor, 10); }),
                    O("\"아직 자리를 내줄 생각 없습니다\"", "{mentor:이} 씩 웃었다. \"그럼 증명해 봐라. 그 말 기다렸다.\"",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); e.Morale(4); }),
                },
            },
            new StoryScene
            {
                Slot = "role_change", Title = "덕아웃의 목소리",
                Text = c => "어느새 팀에서 나보다 나이 많은 선수가 둘밖에 없다.\n경기 중 더그아웃이 가라앉을 때, 다들 나를 쳐다본다.\n\n오늘도 3점 차로 끌려가는 7회. 아무도 입을 열지 않는다.",
                Options = c => new List<StoryOption>
                {
                    O("일어서서 소리친다", "\"아직 9회 아니다!\" 후배들이 하나둘 난간에 매달렸다.",
                        (e, k) => { e.Morale(6); e.Fans(30); Flag(k, "leader"); }),
                    O("후배 한 명씩 어깨를 두드린다", "말보다 손이 빨랐다. 더그아웃 공기가 조금 데워졌다.",
                        (e, k) => { e.Morale(4); e.Exp(Stat.Eye, 30); Flag(k, "leader"); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "{coach}의 흰머리",
                Text = c => "오랜만에 {coach:와} 단둘이 저녁을 먹었다.\n어느새 머리가 하얗게 셌다. 숟가락을 든 손도 예전 같지 않다.\n\n\"나 내년이면 유니폼 벗는다. 너한테 제일 먼저 말하는 거다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"마지막 시즌, 우승 선물 드릴게요\"", "\"말이라도 고맙다.\" {coach}의 눈가가 젖어 있었다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 15); }),
                    O("말없이 술잔을 채운다", "그날 {coach:은} 처음으로 자기 선수 시절 꿈 이야기를 했다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "mentor_old", Title = "병원 복도",
                Text = c => "{coach:이} 쓰러졌다는 연락을 받았다. 과로였다.\n병원 복도로 뛰어가니, 링거를 꽂은 채 야구 중계를 보고 있었다.\n\n\"왜 왔냐. 너 내일 경기 있잖아.\"",
                Options = c => new List<StoryOption>
                {
                    O("밤새 곁을 지킨다", "새벽, 잠든 {coach}의 손에 내 경기 기록지가 쥐여 있었다.",
                        (e, k) => { e.Fatigue(15); ChangeBond(e, k, Bond.Mentor, 20); }),
                    O("\"내일 홈런 치고 올게요\"", "\"홈런은 됐고, 다치지나 마라.\" 목소리가 조금 밝아졌다.",
                        (e, k) => { e.Morale(6); e.Exp(Stat.Power, 40); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "milestone", Title = "통산 기록",
                Text = c => { var t = Tally(c); return $"전광판에 숫자가 떴다. 통산 {Round(t.Count)}{t.Label}.\n관중이 모두 일어서 박수를 쳤다.\n\n구단이 준비한 기념 액자를 {{coach:이}} 직접 들고 나왔다.\n\"18살 꼬맹이가 여기까지 왔다.\""; },
                Options = c => new List<StoryOption>
                {
                    O("관중석을 향해 모자를 벗는다", "함성이 한참 그치지 않았다.",
                        (e, k) => { e.Fans(60); e.Morale(6); }),
                    O("{coach:와} 사진을 찍는다", "구단 SNS에 올라간 그 사진은 그해 가장 많은 '좋아요'를 받았다.",
                        (e, k) => { e.Fans(40); ChangeBond(e, k, Bond.Mentor, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "milestone", Title = "기념구",
                Text = c => { var t = Tally(c); return $"통산 {Round(t.Count)}{t.Label} 기념구가 손에 들어왔다.\n공에는 경기 날짜가 적혀 있다.\n\n이 공을 누구에게 줄까. 떠오르는 얼굴이 몇 있다."; },
                Options = c => new List<StoryOption>
                {
                    O("{kin}에게 보낸다", "{kin}의 거실 장식장 맨 가운데에 공이 놓였다는 사진이 왔다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Family, 15); }),
                    O("{pal}에게 준다", "\"이걸 왜 나를 줘.\" 그렇게 말하면서 {pal:은} 공을 놓지 않았다.",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("팬 추첨 선물로 내놓는다", "추첨에 뽑힌 꼬마 팬이 울면서 공을 받았다.",
                        (e, k) => { e.Fans(80); }),
                },
            },
            new StoryScene
            {
                Slot = "rival_end", Title = "{rival}의 문자",
                Text = c => "밤늦게 {rival}에게서 문자가 왔다. 처음 있는 일이다.\n\n'요즘 몸이 예전 같지 않다. 너는 어떠냐.\n우리 둘 중 누가 먼저 그만두나, 그것도 승부겠지.'",
                Options = c => new List<StoryOption>
                {
                    O("'그 승부도 내가 이긴다'", "'ㅋㅋ 역시.' 짧은 답장이 왔다. 이상하게 힘이 났다.",
                        (e, k) => { e.Morale(6); Flag(k, "rival_respect"); }),
                    O("전화를 건다", "한 시간 동안 신인 시절 이야기만 했다. 둘 다 웃다가 조용해졌다.",
                        (e, k) => { e.Morale(5); e.Fatigue(-5); Flag(k, "rival_respect"); }),
                },
            },
            new StoryScene
            {
                Slot = "rival_end", Title = "올스타 벤치",
                Text = c => "올스타전 벤치. 어느새 나와 {rival:이} 최고참이다.\n후배들이 사인을 받으러 줄을 섰다.\n\n{rival:이} 중얼거렸다. \"우리가 사인 받으러 다니던 게 엊그제 같은데.\"",
                Options = c => new List<StoryOption>
                {
                    O("둘이 같이 사진을 찍는다", "그 사진은 다음 날 스포츠면 1면을 장식했다. '라이벌, 같은 프레임에 서다'.",
                        (e, k) => { e.Fans(60); Flag(k, "rival_respect"); }),
                    O("\"아직 끝난 거 아니다\"", "{rival:이} 씩 웃었다. \"당연하지.\"",
                        (e, k) => { e.Morale(6); e.Exp(Stat.Power, 30); }),
                },
            },

            // ----- 6. The last season -----
            new StoryScene
            {
                Slot = "farewell_tour", Title = "마지막 원정",
                Text = c => "원정 구장에 도착하니 상대 팀이 작은 선물을 준비해 두었다.\n'마지막 방문을 환영합니다.' 전광판에 내 신인 시절 사진이 떴다.\n\n상대 팀 관중석에서도 박수가 터졌다.",
                Options = c => new List<StoryOption>
                {
                    O("상대 관중에게도 인사한다", "야유 대신 박수를 받는 원정. 오래 뛴 사람만 받는 선물이었다.",
                        (e, k) => { e.Fans(60); e.Morale(6); }),
                    O("마지막까지 진지하게 싸운다", "\"인사는 경기 끝나고.\" 그날 상대 팀은 혼쭐이 났다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); e.Morale(4); }),
                },
            },
            new StoryScene
            {
                Slot = "farewell_tour", Title = "은퇴 발표",
                Text = c => "기자회견장. 수십 대의 카메라가 나를 향했다.\n준비한 문장은 짧았다. \"올 시즌을 끝으로 유니폼을 벗습니다.\"\n\n맨 앞줄에 앉은 {voice:이} 펜을 내려놓고 고개를 숙였다.",
                Options = c => new List<StoryOption>
                {
                    O("웃으며 마무리한다", "\"남은 경기, 전부 이길 생각입니다.\" 회견장에 웃음이 퍼졌다.",
                        (e, k) => { e.Morale(6); e.Fans(40); ChangeBond(e, k, Bond.Voice, 5); }),
                    O("고마운 사람들을 떠올린다", "말을 잇지 못했다. {voice:이} 대신 질문을 넘겨 줬다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Voice, 10); ChangeBond(e, k, Bond.Family, 5); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "{voice}의 마지막 원고",
                Text = c => "{voice:이} 원고 하나를 보여 줬다.\n'은퇴하는 날 내보낼 글이에요. 먼저 읽어 줬으면 해서요.'\n\n첫 문장은 이랬다.\n'처음 만난 날, 그 선수는 아무것도 아니었다.'",
                Options = c => new List<StoryOption>
                {
                    O("끝까지 읽는다", "마지막 문장에서 눈앞이 흐려졌다. '그리고 그는 모두의 것이 되었다.'",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Voice, 15); }),
                    O("\"은퇴하는 날 읽을게요\"", "{voice:이} 원고를 봉투에 넣어 건넸다. \"약속이에요.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "last_letter", Title = "팬들의 편지",
                Text = c => "구단 사무실에 상자가 쌓였다. 전부 팬 편지라고 한다.\n\n'아빠랑 처음 야구장 간 날, 선수님이 {first:을} 기록했어요.'\n'선수님 덕분에 힘든 시기를 버텼습니다.'\n\n한 장 한 장, 모르는 사람들의 인생에 내가 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("밤새 편지를 읽는다", "새벽녘, 마지막 편지를 덮었다. 남은 경기를 위해 일어섰다.",
                        (e, k) => { e.Morale(8); e.Fatigue(10); }),
                    O("답장 영상을 찍는다", "구단 채널에 올린 영상에 댓글이 수만 개 달렸다.",
                        (e, k) => { e.Fans(80); e.Morale(4); }),
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "마지막 경기 전날",
                Text = c => "내일이 마지막 홈 경기다.\n텅 빈 구장에 혼자 들어왔다. 조명 몇 개만 켜져 있다.\n\n{mound}에 서 보았다. 처음 이곳에 섰던 날이 떠올랐다.\n뒤에서 발소리. {pal:이} 서 있었다.",
                Options = c => new List<StoryOption>
                {
                    O("마지막으로 같이 캐치볼을 한다", "공이 오갈 때마다 지나간 시간이 한 장면씩 지나갔다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 15); }),
                    O("나란히 앉아 그라운드를 본다", "둘 다 아무 말도 하지 않았다. 그래도 다 알았다.",
                        (e, k) => { e.Morale(6); e.Fatigue(-10); ChangeBond(e, k, Bond.Friend, 10); }),
                },
            },
            new StoryScene
            {
                Slot = "last_game", Title = "사물함 정리",
                Text = c => "사물함을 정리했다. 20년 가까이 쌓인 물건들.\n닳은 배팅 장갑, 첫 월급 영수증, 구겨진 라인업 카드.\n\n맨 안쪽에서 낡은 사진 한 장이 나왔다.\n입단식 날, {coach:와} 찍은 사진이었다.",
                Options = c => new List<StoryOption>
                {
                    O("사진을 {coach}에게 보낸다", "답장은 한 줄이었다. '내 인생 최고의 지명이었다.'",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Mentor, 15); }),
                    O("모자 안쪽에 사진을 넣는다", "내일, 이 사진과 함께 마지막 경기에 나간다.",
                        (e, k) => { e.Morale(6); e.Exp(Stat.Eye, 30); }),
                },
            },
        };
    }
}
