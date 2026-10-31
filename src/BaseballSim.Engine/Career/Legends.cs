using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>A star of the fictional league from before 2026. Not one of the user's own players.</summary>
    public class Legend
    {
        public string Id;
        public string Name;
        public string Nickname;
        public string Years;
        /// <summary>What they're remembered for, e.g. "통산 452홈런".</summary>
        public string Record;
        public Stat Speciality;
        /// <summary>What they say on arrival.</summary>
        public string Line;
        /// <summary>What the player takes away from the lesson.</summary>
        public string Lesson;
    }

    public static class Legends
    {
        public static readonly List<Legend> All = new List<Legend>
        {
            new Legend
            {
                Id = "jang", Name = "장태산", Nickname = "미스터 홈런", Years = "1994~2012", Record = "통산 452홈런", Speciality = Stat.Power,
                Line = "자네 스윙, 힘은 있는데 땅에서 안 올라와. 잠깐 배트 줘 봐.",
                Lesson = "\"공은 팔로 치는 게 아니라 엉덩이로 치는 거야.\" 타구에 힘이 실리기 시작했다.",
            },
            new Legend
            {
                Id = "min", Name = "민경호", Nickname = "안타 제조기", Years = "1990~2009", Record = "통산 2,318안타", Speciality = Stat.Contact,
                Line = "안타는 세게 치는 게 아니라 빈 데로 보내는 거다.",
                Lesson = "밀어치는 요령을 배웠다. 수비 사이로 공이 빠져나가는 그림이 보인다.",
            },
            new Legend
            {
                Id = "han", Name = "한지석", Nickname = "선구안의 교과서", Years = "1997~2016", Record = "통산 1,304볼넷", Speciality = Stat.Eye,
                Line = "치고 싶은 공이 아니라 칠 수 있는 공을 기다려라.",
                Lesson = "투수의 손끝을 보는 법을 배웠다. 볼이 조금 더 잘 보인다.",
            },
            new Legend
            {
                Id = "seo", Name = "서은빈", Nickname = "번개", Years = "1999~2014", Record = "단일 시즌 84도루", Speciality = Stat.Speed,
                Line = "발이 빠른 것보다 먼저 출발하는 게 중요하지.",
                Lesson = "투수의 견제 버릇을 읽는 법을 배웠다. 첫 발이 가벼워졌다.",
            },
            new Legend
            {
                Id = "oh", Name = "오세찬", Nickname = "철벽 유격수", Years = "1992~2010", Record = "수비상 9회", Speciality = Stat.Defense,
                Line = "글러브 말고 발로 잡는 거야. 펑고 몇 개만 쳐 줄게.",
                Lesson = "타구 방향을 미리 읽는 스텝을 배웠다. 공이 글러브로 빨려 들어온다.",
            },
        };
    }
}
