using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>
    /// The evenings between Korea's games: one of many small moments (the roommate, the food abroad, a
    /// kid from the Korean community, a teammate sent home hurt, a bad start and the manager's talk, a rain
    /// delay, an old school friend on the other side...). Each tournament draws its own, and moments not
    /// seen in earlier call-ups come first, so a career with three or four call-ups rarely repeats one.
    /// </summary>
    public static partial class NationalStory
    {
        class MomentDef
        {
            public string Id;
            public Func<Career, TournamentResult, NationalTeam.Run, bool> Fits = (_, __, ___) => true;
            public Func<Career, TournamentResult, NationalTeam.Run, StoryEvent> Build;
        }

        static StoryEvent Moment(Career c, TournamentResult t, NationalTeam.Run run, string key, int salt)
        {
            var fitting = Moments.Where(m => !c.SeenThisSeason(m.Id) && m.Fits(c, t, run)).ToList();
            var fresh = fitting.Where(m => !c.Seen(m.Id)).ToList();
            var pool = fresh.Count > 0 ? fresh : fitting;
            if (pool.Count == 0) return null;
            return Make(key, pool[Pick(c, pool.Count, 11 + salt)].Build(c, t, run));
        }

        static bool LostLast(TournamentResult t) => t.Games.Count > 0 && !t.Games[t.Games.Count - 1].Won;
        static bool WonLast(TournamentResult t) => t.Games.Count > 0 && t.Games[t.Games.Count - 1].Won;
        static string Abroad(TournamentResult t) => t.Wbc ? "미국" : "타이베이";
        static Stat Read(Career c) => c.Me.IsPitcher ? Stat.Control : Stat.Eye;

        static readonly List<MomentDef> Moments = new List<MomentDef>
        {
            new MomentDef { Id = "nt_room", Build = (c, t, run) => Roommate(c, t) },

            new MomentDef
            {
                Id = "nt_food",
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_food",
                    Title = "입에 안 맞는 밥",
                    Text = $"{Abroad(t)} 숙소 식당. 사흘째 기름진 음식만 나온다.\n막내 하나가 젓가락만 굴리고 있다. 누군가 중얼거렸다. \"김치찌개 한 그릇만 먹으면 홈런 칠 텐데.\"",
                    Choices =
                    {
                        new EventChoice { Label = "한인 식당을 수소문해 다 같이 간다", Result = "된장찌개 냄새에 다들 말이 없어졌다. 사장님이 계산을 끝내 안 받았다.", Apply = e => { e.Morale(6); e.Trust(1); e.Fatigue(5); } },
                        new EventChoice { Label = "챙겨 온 컵라면을 방에 푼다", Result = "새벽 한 시, 내 방이 대표팀 야식 창고가 됐다.", Apply = e => { e.Morale(4); e.Fans(20); } },
                        new EventChoice { Label = "주는 대로 먹고 컨디션을 지킨다", Result = "맛은 없어도 몸은 정직하다. 다음 날 몸이 가벼웠다.", Apply = e => { e.Fatigue(-10); e.Morale(1); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_kid",
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_kid",
                    Title = "교민 꼬마의 태극기",
                    Text = "경기 후 출구에서 교민 아이 하나가 철망에 매달려 있었다.\n손에 든 건 직접 그린 삐뚤빼뚤한 태극기. \"여기 사인해 주세요! 한국 가 본 적은 없는데, 우리 팀이에요!\"",
                    Choices =
                    {
                        new EventChoice { Label = "태극기에 사인하고 모자를 씌워 준다", Result = "아이 엄마가 몇 번이고 고개를 숙였다. 그 사진이 교민 신문 1면에 실렸다.", Apply = e => { e.Fans(60); e.Morale(5); } },
                        new EventChoice { Label = "\"내일 이기는 거 보러 와\"", Result = "다음 날 관중석 맨 앞줄에 그 태극기가 걸려 있었다.", Apply = e => { e.Morale(6); e.Fans(30); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_hurt",
                Build = (c, t, run) =>
                {
                    string mate = Mate(c, t, 2);
                    return new StoryEvent
                    {
                        Id = "nt_hurt",
                        Title = "짐을 싸는 동료",
                        Text = $"{mate}이(가) 햄스트링을 다쳐 대회를 접게 됐다.\n로비에서 캐리어를 끄는 뒷모습. \"내 몫까지 해 줘. 금메달 걸고 와.\"",
                        Choices =
                        {
                            new EventChoice { Label = "모자에 등번호를 적는다", Result = $"모자챙 안쪽에 {mate}의 번호를 적었다. 타석마다 그 숫자를 봤다.", Apply = e => { e.Morale(6); e.Fans(40); } },
                            new EventChoice { Label = "빈자리를 메우려 훈련량을 늘린다", Result = "한 사람이 빠진 만큼 더 뛰었다. 다리가 묵직하다.", Apply = e => { e.Exp(Main(c), 60); e.Fatigue(15); } },
                        },
                    };
                },
            },

            new MomentDef
            {
                Id = "nt_slump",
                Fits = (c, t, run) => LostLast(t),
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_slump",
                    Title = "감독실 노크",
                    Text = "경기가 끝나고 대표팀 감독이 {name}를 불렀다.\n\"리그에서 하던 것 반만 해. 여기 온 건 네가 잘해서야. 증명하러 온 게 아니라.\"",
                    Choices =
                    {
                        new EventChoice { Label = "\"내일은 제 야구 하겠습니다\"", Result = "어깨에 힘이 빠졌다. 오랜만에 공이 크게 보였다.", Apply = e => { e.Morale(6); e.Exp(Read(c), 40); } },
                        new EventChoice { Label = "밤새 영상을 돌려 본다", Result = "문제는 타이밍이었다. 새벽 세 시에야 찾았다.", Apply = e => { e.Exp(Read(c), 70); e.Boost(Read(c), 15, 10, "대표팀 감독의 조언"); e.Fatigue(10); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_rain",
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_rain",
                    Title = "우천 지연",
                    Text = "경기 직전 하늘이 열렸다. 방수포가 깔리고 두 시간째 대기.\n라커룸 한쪽에선 화투판이, 한쪽에선 노래방 앱이 돌아간다.",
                    Choices =
                    {
                        new EventChoice { Label = "노래 한 곡 뽑는다", Result = "18번이 나오자 라커룸이 뒤집어졌다. 그 영상이 구단 계정을 타고 퍼졌다.", Apply = e => { e.Morale(5); e.Fans(50); } },
                        new EventChoice { Label = "실내 연습장에서 몸을 데운다", Result = "다들 놀 때 땀을 냈다. 비가 그치자 제일 먼저 뛰어나갔다.", Apply = e => { e.Exp(Main(c), 50); e.Fatigue(8); } },
                        new EventChoice { Label = "구석에서 눈을 붙인다", Result = "기다림도 실력이다. 개운하게 일어났다.", Apply = e => { e.Fatigue(-15); e.Morale(2); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_school",
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_school",
                    Title = "건너편 더그아웃의 동창",
                    Text = $"오늘 상대 {run.NextOpponent?.Name ?? run.CurrentOpponent?.Name ?? "팀"}의 명단에 낯익은 이름. 고등학교 때 같이 공 던지던 친구다. 이민 간 뒤로 연락이 끊겼었다.\n경기 전 펜스 너머로 그 녀석이 손을 흔들었다. \"야! 너 진짜 프로 됐네!\"",
                    Choices =
                    {
                        new EventChoice { Label = "경기 끝나고 밥을 산다", Result = "십 년 치 이야기를 두 시간에 했다. 헤어질 때 유니폼을 바꿔 입었다.", Apply = e => { e.Morale(6); e.Money(-30); } },
                        new EventChoice { Label = "\"오늘은 봐주지 마라\"", Result = "\"너야말로.\" 둘 다 웃으면서 이를 갈았다.", Apply = e => { e.Morale(4); e.Exp(Main(c), 40); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_press",
                Fits = (c, t, run) => WonLast(t),
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_press",
                    Title = "외신 인터뷰",
                    Text = "경기 후 믹스트존. 외국 기자 하나가 통역을 끼고 마이크를 내밀었다.\n\"오늘 활약으로 해외 리그 관심이 많습니다. 한국 밖에서 뛸 생각이 있습니까?\"",
                    Choices =
                    {
                        new EventChoice { Label = "\"지금은 우리 팀 생각뿐입니다\"", Result = "기사가 나가자 소속팀 팬 커뮤니티가 들끓었다. 좋은 쪽으로.", Apply = e => { e.Fans(80); e.Trust(1); } },
                        new EventChoice { Label = "\"언젠가는 도전하고 싶습니다\"", Result = "해외 매체에 이름이 실렸다. 세계가 조금 가까워졌다.", Apply = e => { e.Fans(60); e.Morale(5); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_letter",
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_letter",
                    Title = "캐리어 속 쪽지",
                    Text = c.HasMark("married")
                        ? "짐을 풀다 유니폼 사이에서 쪽지가 나왔다. {spouse}의 글씨다.\n'밥 꼭 챙겨 먹고, 다치지만 마. 나머진 다 괜찮아.'"
                        : "짐을 풀다 유니폼 사이에서 쪽지가 나왔다. 엄마 글씨다.\n'태극마크 단 아들, 텔레비전 앞에서 볼게. 아프지만 마라.'",
                    Choices =
                    {
                        new EventChoice { Label = "쪽지를 모자 안에 넣는다", Result = "타석에 설 때마다 모자챙을 한 번씩 만졌다.", Apply = e => { e.Morale(7); } },
                        new EventChoice { Label = "바로 영상 통화를 건다", Result = "화면 너머 얼굴을 보니 긴장이 풀렸다. \"내일 잘할게.\"", Apply = e => { e.Morale(5); e.Fatigue(-8); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_bench",
                Fits = (c, t, run) => PlaceOf(c) != Place.Veteran,
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_bench",
                    Title = "선발 명단에 없는 이름",
                    Text = c.Me.IsPitcher
                        ? "내일 선발 발표. 내 이름이 없다. 불펜 대기다.\n투수코치가 지나가며 말했다. \"제일 급할 때 쓸 거다. 준비해 둬.\""
                        : "내일 라인업 발표. 내 이름이 없다. 벤치 대기다.\n타격코치가 지나가며 말했다. \"승부처에 대타로 쓸 거다. 한 방만 준비해.\"",
                    Choices =
                    {
                        new EventChoice { Label = "한 번의 기회를 위해 준비한다", Result = "몸을 데웠다 식혔다 반복했다. 언제 불러도 나갈 수 있다.", Apply = e => { e.Exp(Main(c), 50); e.Morale(2); } },
                        new EventChoice { Label = "더그아웃에서 목이 쉬도록 응원한다", Result = "목소리가 갈라졌다. 선배들이 '응원단장'이라고 불렀다.", Apply = e => { e.Morale(4); e.Trust(1); e.Fans(30); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_captain",
                Fits = (c, t, run) => PlaceOf(c) == Place.Veteran,
                Build = (c, t, run) =>
                {
                    string kid = Mate(c, t, 4);
                    return new StoryEvent
                    {
                        Id = "nt_captain",
                        Title = "막내의 새벽",
                        Text = $"새벽 두 시, 복도 끝 자판기 앞에 대표팀 막내 {kid}이(가) 쪼그려 앉아 있었다.\n\"선배님... 저 내일 또 못 치면 어떡하죠. 다들 저 때문에 지는 것 같아서.\"",
                        Choices =
                        {
                            new EventChoice { Label = "내 첫 대표팀 실수담을 들려준다", Result = $"{kid}이(가) 처음으로 웃었다. \"선배님도 그랬어요?\" 그래, 다들 그랬다.", Apply = e => { e.Morale(5); e.Trust(1); } },
                            new EventChoice { Label = "아무 말 없이 캔커피를 건넨다", Result = "말은 필요 없었다. 둘이서 해 뜨는 걸 봤다.", Apply = e => { e.Morale(4); e.Fatigue(5); e.Fans(20); } },
                        },
                    };
                },
            },

            new MomentDef
            {
                Id = "nt_rival_night",
                Fits = (c, t, run) => RivalIn(t) && c.Rival != null,
                Build = (c, t, run) => new StoryEvent
                {
                    Id = "nt_rival_night",
                    Title = "라이벌과 한 테이블",
                    Text = $"숙소 식당, 남은 자리는 {c.Rival.Name}의 맞은편뿐이었다.\n한참 말없이 먹다가 {c.Rival.Name}이(가) 먼저 입을 열었다. \"너 몸쪽 공 칠 때 왼쪽 어깨가 먼저 열려. 리그 가면 다시 적이니까 지금만 알려 준다.\"",
                    Choices =
                    {
                        new EventChoice { Label = "\"너도 하나 알려 줄까?\"", Result = "서로의 약점을 털어놨다. 리그로 돌아가면 이게 독이 될지 약이 될지.", Apply = e => { e.Exp(Read(c), 60); e.Morale(4); } },
                        new EventChoice { Label = "말없이 고개만 끄덕인다", Result = "다음 날 타격 연습에서 왼쪽 어깨를 닫았다. 공이 더 오래 보였다.", Apply = e => { e.Exp(Main(c), 50); e.Boost(Read(c), 15, 10, "라이벌의 한마디"); } },
                    },
                },
            },

            new MomentDef
            {
                Id = "nt_clubmate",
                Fits = (c, t, run) => t.Korea.Lineup.Concat(t.Korea.Rotation).Any(p => p.Id != c.Me.Id && c.MyTeam.Lineup.Concat(c.MyTeam.Rotation).Any(m => m.Id == p.Id)),
                Build = (c, t, run) =>
                {
                    var p = t.Korea.Lineup.Concat(t.Korea.Rotation).First(x => x.Id != c.Me.Id && c.MyTeam.Lineup.Concat(c.MyTeam.Rotation).Any(m => m.Id == x.Id));
                    return new StoryEvent
                    {
                        Id = "nt_clubmate",
                        Title = "같은 팀, 다른 유니폼",
                        Text = $"대표팀 버스 맨 뒷자리. 옆에 앉은 건 {c.MyTeam.Name}에서 매일 보는 {p.Name}.\n\"이상하다. 매일 보는 얼굴인데 이 유니폼 입으니까 새롭네.\"",
                        Choices =
                        {
                            new EventChoice { Label = "\"돌아가서 우리 팀 우승도 하자\"", Result = "주먹을 맞댔다. 대표팀에서 생긴 호흡은 리그까지 이어질 것이다.", Apply = e => { e.Morale(5); e.Trust(1); } },
                            new EventChoice { Label = "다른 팀 선수들 정보를 같이 캔다", Result = "\"저 투수 우리랑 붙을 때 이거 던진대.\" 수첩이 빼곡해졌다.", Apply = e => { e.Exp(Read(c), 60); } },
                        },
                    };
                },
            },
        };
    }
}
