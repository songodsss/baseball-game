using System.Collections.Generic;
using static BaseballSim.Career.MainStory;

namespace BaseballSim.Career
{
    /// <summary>
    /// Versions of a scene that come only when a bond has gone cold (30 or less) or grown very close
    /// (85 or more). When one fits, it takes the slot instead of the usual versions, so how the player
    /// treated someone shows up in the story, and a cold bond gets one chance to mend.
    /// </summary>
    internal static class BondScenes
    {
        public const int Cold = 30, Warm = 85;

        static bool IsCold(Career c, Bond b) => c.Story.BondOf(b) <= Cold;
        static bool IsWarm(Career c, Bond b) => c.Story.BondOf(b) >= Warm;

        public static readonly List<StoryScene> All = new List<StoryScene>
        {
            // ----- Family -----
            new StoryScene
            {
                Slot = "kin_visit", Title = "받지 않은 전화", Needs = c => IsCold(c, Bond.Family),
                Text = c => "휴대폰에 부재중 전화가 쌓여 있었다. 전부 {kin}.\n언제부터 전화를 미뤄 왔는지 기억도 나지 않았다.\n\n" +
                            "마지막으로 온 문자 한 줄.\n'바쁜 거 알아. 그냥 목소리 한 번 듣고 싶어서.'",
                Options = c => new List<StoryOption>
                {
                    O("지금 바로 찾아간다", "경기가 끝나자마자 차를 몰았다. 문을 연 {kin:이} 아무 말 없이 밥상을 차렸다. 숟가락 소리만 났는데, 그걸로 충분했다.",
                        (e, k) => { e.Fatigue(10); e.Morale(6); ChangeBond(e, k, Bond.Family, 25); Flag(k, "mended_kin"); }),
                    O("전화를 건다", "\"미안해.\" 한마디에 수화기 너머가 한참 조용했다. 그리고 웃음소리가 들렸다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Family, 15); Flag(k, "mended_kin"); }),
                    O("시즌 끝나고 연락한다", "휴대폰을 엎어 두었다. 화면이 몇 번 더 켜졌다 꺼졌다.",
                        (e, k) => { e.Exp(Stat.Contact, 40); ChangeBond(e, k, Bond.Family, -5); }),
                },
            },
            new StoryScene
            {
                Slot = "kin_visit", Title = "도시락 응원단", Needs = c => IsWarm(c, Bond.Family),
                When = (c, d) => d?.MyGame != null && d.MyGame.Home == c.MyTeam,
                Text = c => "홈경기 날, 외야석 한쪽이 시끌벅적했다.\n{kin:이} 동네 사람들을 버스 한 대에 태우고 온 거였다. 손에는 직접 만든 응원 피켓.\n\n" +
                            "전광판에 그 모습이 잡히자 구장 전체가 웃었다.",
                Options = c => new List<StoryOption>
                {
                    O("외야석을 향해 모자를 흔든다", "응원단이 일제히 피켓을 들어 올렸다. 그날 경기는 이상하게 몸이 가벼웠다.",
                        (e, k) => { e.Morale(8); e.Fans(40); ChangeBond(e, k, Bond.Family, 5); }),
                    O("경기 후 응원단과 사진을 찍는다", "버스 앞에서 단체 사진. {kin:이} 그 사진을 가게 벽에 제일 크게 걸었다고 한다.",
                        (e, k) => { e.Fatigue(5); e.Fans(30); ChangeBond(e, k, Bond.Family, 10); }),
                },
            },

            // ----- Mentor -----
            new StoryScene
            {
                Slot = "pressure", Title = "{coach}의 침묵", Needs = c => IsCold(c, Bond.Mentor),
                Text = c => "중요한 경기를 앞둔 미팅. {mentor:이} 한 사람씩 이름을 부르며 조언을 했다.\n내 차례에서만 아무 말이 없었다.\n\n" +
                            "미팅이 끝나고 복도에서 마주쳤다. {coach:이} 걸음을 멈췄다.\n\"할 말 있으면 해라. 없으면 가고.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"그동안 죄송했습니다\"", "{coach:이} 한참 나를 봤다.\n\"...늦었다. 그래도 안 온 것보단 낫다. 내일 아침 일찍 나와라.\"",
                        (e, k) => { e.Morale(5); ChangeBond(e, k, Bond.Mentor, 25); Flag(k, "mended_mentor"); }),
                    O("\"제 방식이 맞다는 걸 보여 드리겠습니다\"", "\"그래. 그럼 보여 줘.\" 차가운 목소리였지만, 눈은 피하지 않았다.",
                        (e, k) => { e.Exp(Stat.Power, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Mentor, 5); }),
                    O("고개만 숙이고 지나간다", "뒤에서 한숨 소리가 들렸다.",
                        (e, k) => { e.Morale(-2); ChangeBond(e, k, Bond.Mentor, -5); }),
                },
            },

            // ----- Friend -----
            new StoryScene
            {
                Slot = "role_change", Title = "어색한 인사", Needs = c => IsCold(c, Bond.Friend),
                Text = c => "구단 행사장에서 {pal:과} 마주쳤다.\n신인 시절엔 매일 붙어 다녔는데, 언제부터인가 연락이 끊겼다.\n\n" +
                            "{pal:이} 먼저 어색하게 웃었다.\n\"오랜만이다. 잘 지내지? ...잘 지내는 거 TV로 다 보고 있다.\"",
                Options = c => new List<StoryOption>
                {
                    O("\"오늘 끝나고 한잔하자\"", "포장마차에서 새벽까지 이야기했다. {pal:이} 웃으며 말했다. \"너 진짜 많이 변했다. 근데 웃는 건 그대로네.\"",
                        (e, k) => { e.Fatigue(10); e.Morale(6); ChangeBond(e, k, Bond.Friend, 25); Flag(k, "mended_pal"); }),
                    O("\"그때 미안했다\"", "{pal:이} 고개를 저었다.\n\"뭐가. 다 지난 일이다.\" 그래도 헤어질 때 어깨를 한 번 쳐 줬다.",
                        (e, k) => { e.Morale(4); ChangeBond(e, k, Bond.Friend, 15); Flag(k, "mended_pal"); }),
                    O("\"응, 잘 지내\"", "짧은 인사로 끝났다. 돌아서는 등이 낯설었다.",
                        (e, k) => ChangeBond(e, k, Bond.Friend, -5)),
                },
            },
            new StoryScene
            {
                Slot = "role_change", Title = "변하지 않은 자리", Needs = c => IsWarm(c, Bond.Friend),
                Text = c => "라커룸 내 자리 옆이 언제부턴가 후배들 차지가 됐다.\n그런데 오늘, 그 자리에 낯익은 가방이 놓여 있었다.\n\n" +
                            "{pal:이} 씩 웃었다.\n\"후배들한테 양보받았다. 이 자리는 원래 내 거라고.\"",
                Options = c => new List<StoryOption>
                {
                    O("신인 시절처럼 장난을 친다", "라커룸이 웃음바다가 됐다. 후배들이 처음 보는 내 모습이라며 휴대폰을 꺼냈다.",
                        (e, k) => { e.Morale(8); ChangeBond(e, k, Bond.Friend, 5); }),
                    O("둘이서 후배들 특훈을 연다", "\"그때 우리가 받던 거, 똑같이 해 주자.\" 후배들 얼굴이 하얗게 질렸다.",
                        (e, k) => { e.Exp(Stat.Eye, 50); e.Fatigue(10); ChangeBond(e, k, Bond.Friend, 5); }),
                },
            },

            // ----- Voice -----
            new StoryScene
            {
                Slot = "milestone", Title = "{voice}의 선물", Needs = c => IsWarm(c, Bond.Voice),
                When = (c, d) => d != null && (d.MyLine?.PlateAppearances > 0 || d.MyPitching != null),
                Text = c => "기념 행사가 끝나고 {voice:이} 작은 상자를 내밀었다.\n안에는 두꺼운 스크랩북이 들어 있었다.\n\n" +
                            "첫 장은 신인 시절의 짧은 기사, 마지막 장은 오늘 경기.\n{voice:이} 쑥스럽게 웃었다.\n\"처음부터 다 모아 뒀어요. 언젠가 드리려고.\"",
                Options = c => new List<StoryOption>
                {
                    O("한 장씩 넘겨 본다", "잊고 있던 장면들이 하나씩 돌아왔다. 첫 안타, 첫 2군행, 첫 야유. 그리고 오늘.",
                        (e, k) => { e.Morale(10); ChangeBond(e, k, Bond.Voice, 10); }),
                    O("\"마지막 장은 비워 두세요\"", "{voice:이} 고개를 끄덕였다.\n\"그럼 그 장은 은퇴하는 날 채울게요.\"",
                        (e, k) => { e.Morale(6); e.Fans(30); ChangeBond(e, k, Bond.Voice, 10); }),
                },
            },
        };
    }
}
