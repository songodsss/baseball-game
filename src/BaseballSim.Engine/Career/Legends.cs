using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>What a legend's visit is: their first visit, the return visit, the day their record falls, or both rivals together.</summary>
    public enum LegendStage { None = 0, Met = 1, Returned = 2, Surpassed = 3 }

    /// <summary>
    /// A star of the fictional league from before 2026. Not one of the user's own players. Each comes
    /// back three times in a career: when the player gets good at their speciality, when the player is
    /// halfway to their record, and on the day the player breaks it.
    /// </summary>
    public class Legend
    {
        public string Id;
        public string Name;
        public string Nickname;
        public string Years;
        /// <summary>What they're remembered for, e.g. "통산 452홈런".</summary>
        public string Record;
        public Stat Speciality;
        /// <summary>Visits pitchers instead of hitters.</summary>
        public bool ForPitchers;
        /// <summary>Their career and what they never got to do, for the album.</summary>
        public string Story;
        /// <summary>What they say on arrival.</summary>
        public string Line;
        /// <summary>What the player takes away from the lesson.</summary>
        public string Lesson;

        /// <summary>The record the player is chasing, worded for this player ("통산 452홈런").</summary>
        public Func<Career, string> Goal;
        /// <summary>How far the player has come toward it: 1 or more means broken.</summary>
        public Func<Career, double> Progress;
        /// <summary>The return visit: halfway there (or a first MVP, for 오세찬).</summary>
        public Func<Career, bool> Halfway = c => false;
        /// <summary>Whether this player can chase the record at all (the pitchers' depend on role).</summary>
        public Func<Career, bool> Chases = c => true;

        public string ReturnText, ReturnLabelA, ReturnResultA, ReturnLabelB, ReturnResultB;
        public string SurpassText, SurpassLabel, SurpassResult;
        /// <summary>Lines for the retirement article: came close, or broke it.</summary>
        public string NearMiss, Ceremony;

        public bool Visits(CareerPlayer me) => me.IsPitcher == ForPitchers;
    }

    public static class Legends
    {
        /// <summary>A count summed over every season so far, each scaled to a full 144 games.</summary>
        static double Scaled(Career c, Func<BattingLine, int> bat, Func<PitcherLine, int> pitch)
        {
            double total = 0;
            foreach (var h in c.Me.History)
            {
                int n = bat != null ? (h.Line != null ? bat(h.Line) : 0) : (h.Pitching != null ? pitch(h.Pitching) : 0);
                total += (double)n * League.FullSeason / Math.Max(1, h.Games);
            }
            int now = bat != null ? bat(c.SeasonLine) : (c.SeasonPitching != null ? pitch(c.SeasonPitching) : 0);
            return total + (double)now * League.FullSeason / c.League.GamesPerSeason;
        }

        /// <summary>A record in this player's season length: 452 homers is 113 in 36-game seasons.</summary>
        static int InSeason(Career c, int fullSeasonCount) => (int)Math.Round((double)fullSeasonCount * c.League.GamesPerSeason / League.FullSeason);

        static string CountGoal(Career c, int record, string unit) =>
            c.League.GamesPerSeason == League.FullSeason ? $"통산 {record:N0}{unit}" : $"통산 {record:N0}{unit} (지금 시즌 길이로 {InSeason(c, record):N0}{unit})";

        /// <summary>Career batting average; counts once the player has played ten seasons.</summary>
        static double Average(Career c)
        {
            int hits = c.SeasonLine.Hits + c.Me.History.Sum(h => h.Line?.Hits ?? 0);
            int atBats = c.SeasonLine.AtBats + c.Me.History.Sum(h => h.Line?.AtBats ?? 0);
            return atBats == 0 ? 0 : (double)hits / atBats;
        }

        static int Seasons(Career c) => c.Me.History.Count + 1;

        static int Mvps(Career c) => c.Me.History.Count(h => h.Titles.Contains(Awards.Mvp));

        public const int JangHomeRuns = 452, HanWalks = 1488, SeoSteals = 979, OhMvps = 3, YoonWins = 319, MaSaves = 650, NohHolds = 640;
        public const double MinAverage = 0.319;
        public const int AverageSeasons = 10;

        public static readonly List<Legend> All = new List<Legend>
        {
            new Legend
            {
                Id = "jang", Name = "장태산", Nickname = "미스터 홈런", Years = "1994~2012", Record = "통산 452홈런", Speciality = Stat.Power,
                Story = "리그 통산 홈런 1위. 덩치만큼 목소리도 크고 호탕하다. 19년 동안 홈런왕 7번, 그런데 우승 반지는 끝내 하나도 없다. \"내 기록 깨는 놈 보면 은퇴식 한 번 더 하겠다\"는 게 입버릇.",
                Line = "자네 스윙, 힘은 있는데 땅에서 안 올라와. 잠깐 배트 줘 봐.",
                Lesson = "\"공은 팔로 치는 게 아니라 엉덩이로 치는 거야.\" 타구에 힘이 실리기 시작했다.",
                Goal = c => CountGoal(c, JangHomeRuns, "홈런"),
                Progress = c => Scaled(c, l => l.HomeRuns, null) / (JangHomeRuns + 1),
                Halfway = c => Scaled(c, l => l.HomeRuns, null) >= JangHomeRuns / 2,
                ReturnText = "장태산이 또 왔다. 이번엔 손에 신문을 들고.\n\"벌써 반이야, 반! 내 기록을 넘보는 놈은 처음 본다.\"\n신문을 접더니 눈빛이 달라졌다. \"남은 반이 진짜다. 다리 힘 빠지는 서른 넘어서가.\"",
                ReturnLabelA = "하체 훈련법을 배운다", ReturnResultA = "\"스쿼트는 거짓말을 안 해.\" 그날 다리가 후들거릴 때까지 따라 했다.",
                ReturnLabelB = "\"반드시 넘겠습니다\"", ReturnResultB = "장태산이 껄껄 웃었다. \"그 말 녹음해 둔다. 넘으면 내가 제일 먼저 박수 친다.\"",
                SurpassText = "{name}의 타구가 담장을 넘어갔다. 통산 홈런 신기록.\n관중석 맨 앞에서 거구의 남자가 벌떡 일어섰다. 장태산이었다.\n\n경기 후 장태산이 더그아웃까지 내려왔다.\n\"이제 기록표에 내 이름 위에 네 이름이다. 좋다. 이 맛에 은퇴식 한 번 더 하는 기분이다.\"",
                SurpassLabel = "장태산과 함께 홈런볼을 든다", SurpassResult = "두 사람이 홈런볼을 함께 든 사진이 다음 날 모든 신문 1면을 덮었다.",
                NearMiss = "장태산은 '조금만 더 했으면 내 기록이었는데, 아깝다!'라며 크게 웃었다.",
                Ceremony = "장태산이 꽃다발을 들고 올라왔다. \"내 기록 깬 놈 은퇴식이라 안 올 수가 있나.\"",
            },
            new Legend
            {
                Id = "min", Name = "민경호", Nickname = "안타 제조기", Years = "1990~2009", Record = "통산 타율 .319", Speciality = Stat.Contact,
                Story = "통산 타율 1위(.319), 2,318안타. 낡은 타격 노트를 늘 들고 다니는 잔소리꾼. 1999년 시즌 타율 .397, 4할에 3리가 모자랐던 그해를 아직도 이야기한다.",
                Line = "안타는 세게 치는 게 아니라 빈 데로 보내는 거다.",
                Lesson = "밀어치는 요령을 배웠다. 수비 사이로 공이 빠져나가는 그림이 보인다.",
                Goal = c => $"통산 타율 .319 넘기 ({AverageSeasons}시즌 이상)",
                Progress = c => Seasons(c) < AverageSeasons ? Math.Min(0.99, Average(c) / (MinAverage + 0.001)) : Average(c) / (MinAverage + 0.001),
                Halfway = c => Seasons(c) >= 5 && Average(c) >= 0.300,
                ReturnText = "민경호가 낡은 노트를 펼쳤다. 거기에 내 타율이 시즌별로 적혀 있었다.\n\"꾸준하네. 3할을 5년 넘게 치는 놈은 흔치 않아.\"\n그러더니 빨간 펜으로 한 줄을 그었다. \"근데 2스트라이크 이후가 약해. 여기서 갈린다.\"",
                ReturnLabelA = "2스트라이크 대처법을 배운다", ReturnResultA = "\"짧게, 더 짧게.\" 배트를 한 뼘 짧게 쥐었다. 공이 더 오래 보였다.",
                ReturnLabelB = "노트를 빌려 달라고 한다", ReturnResultB = "\"이건 안 돼. 네가 내 기록 넘으면 그때 준다.\"",
                SurpassText = "시즌 마지막 날, 전광판에 통산 타율이 떴다. 민경호의 기록보다 높다.\n\n경기 후, 민경호가 낡은 노트를 내밀었다.\n\"약속했지. 이제 이건 네 거다. 맨 뒷장은 비워 뒀다. 네가 채워라.\"",
                SurpassLabel = "노트를 받는다", SurpassResult = "노트 맨 뒷장에 첫 줄을 적었다. '빈 데로 보낸다.'",
                NearMiss = "민경호의 노트에는 내 타율이 마지막 시즌까지 빼곡히 적혀 있었다고 한다.",
                Ceremony = "민경호가 객석에서 낡은 노트를 흔들었다. 이제는 내 노트가 된 그 노트의 짝이었다.",
            },
            new Legend
            {
                Id = "han", Name = "한지석", Nickname = "선구안의 교과서", Years = "1997~2016", Record = "통산 1,488볼넷", Speciality = Stat.Eye,
                Story = "통산 볼넷 1위(1,488개). 말수가 적어 '침묵의 해설위원'으로 불린다. 눈빛 하나로 투수를 무너뜨렸다는 전설이 있다. 은퇴 후 지금까지 중계석에서 칭찬을 한 번도 한 적이 없다.",
                Line = "치고 싶은 공이 아니라 칠 수 있는 공을 기다려라.",
                Lesson = "투수의 손끝을 보는 법을 배웠다. 볼이 조금 더 잘 보인다.",
                Goal = c => CountGoal(c, HanWalks, "볼넷"),
                Progress = c => Scaled(c, l => l.Walks, null) / (HanWalks + 1),
                Halfway = c => Scaled(c, l => l.Walks, null) >= HanWalks / 2,
                ReturnText = "중계석에서 한지석이 내려왔다. 한참 나를 보더니 짧게 말했다.\n\"공 고르는 건 됐다.\"\n잠깐 침묵. \"이제 기다린 공을 놓치지 마라. 그게 반이다.\"",
                ReturnLabelA = "같이 투구 영상을 본다", ReturnResultA = "한지석은 세 시간 동안 일곱 마디를 했다. 일곱 마디가 전부 정답이었다.",
                ReturnLabelB = "\"칭찬하신 거죠?\"", ReturnResultB = "한지석이 처음으로 입꼬리를 올렸다. \"착각이다.\"",
                SurpassText = "볼 넷. 1루로 걸어가는데 관중석이 술렁였다. 통산 볼넷 신기록.\n\n그날 중계석. 한지석이 마이크를 잡았다.\n\"...저 선수는 제 기록을 넘었습니다. 그리고 저보다 공을 더 잘 봅니다.\"\n해설 20년 만의 첫 칭찬이었다.",
                SurpassLabel = "중계석을 향해 모자를 든다", SurpassResult = "카메라에 잡힌 한지석이 조용히 고개를 끄덕였다.",
                NearMiss = "한지석은 중계석에서 한마디만 남겼다. \"좋은 눈이었습니다.\"",
                Ceremony = "한지석이 마이크를 잡았다. \"이 선수 덕분에 해설하는 게 즐거웠습니다.\" 객석이 웃음과 박수로 들썩였다.",
            },
            new Legend
            {
                Id = "seo", Name = "서은빈", Nickname = "번개", Years = "1999~2014", Record = "통산 979도루", Speciality = Stat.Speed,
                Story = "단일 시즌 84도루, 통산 979도루. 장난기 많고 늘 웃는다. 서른다섯에 무릎을 다쳐 1,000도루를 21개 앞두고 은퇴했다. \"남은 21개는 누가 대신 뛰어 주겠지.\"",
                Line = "발이 빠른 것보다 먼저 출발하는 게 중요하지.",
                Lesson = "투수의 견제 버릇을 읽는 법을 배웠다. 첫 발이 가벼워졌다.",
                Goal = c => CountGoal(c, SeoSteals, "도루"),
                Progress = c => Scaled(c, l => l.StolenBases, null) / (SeoSteals + 1),
                Halfway = c => Scaled(c, l => l.StolenBases, null) >= SeoSteals / 2,
                ReturnText = "서은빈이 무릎 보호대를 툭툭 치며 웃었다.\n\"벌써 반이나 훔쳤네? 도둑놈 다 됐다.\"\n그러다 진지해졌다. \"나는 여기서 멈췄어. 너는 무릎 아끼면서 뛰어. 내 남은 몫까지.\"",
                ReturnLabelA = "슬라이딩 요령을 배운다", ReturnResultA = "\"무릎이 아니라 엉덩이로 미끄러져.\" 흙투성이가 됐지만 몸이 덜 아팠다.",
                ReturnLabelB = "\"1,000개 채워 드릴게요\"", ReturnResultB = "서은빈이 손가락으로 21을 그렸다. \"약속이다?\"",
                SurpassText = "2루에 먼지가 피어올랐다. 세이프. 통산 도루 신기록.\n\n심판이 경기를 잠시 멈췄다. 서은빈이 그라운드로 걸어 나왔다.\n구단 직원이 뽑아 온 2루 베이스를 건네며 서은빈이 웃었다.\n\"내가 못 뛴 길, 네가 다 뛰었네.\"",
                SurpassLabel = "베이스를 함께 든다", SurpassResult = "관중들이 일제히 \"번개!\"를 외쳤다. 이제 그 별명은 두 사람의 것이다.",
                NearMiss = "서은빈은 '거의 다 왔었어. 그 정도면 내 몫까지 충분히 뛰었다'고 말했다.",
                Ceremony = "서은빈이 그날의 2루 베이스를 들고 왔다. 베이스에는 두 사람의 사인이 나란히 있었다.",
            },
            new Legend
            {
                Id = "oh", Name = "오세찬", Nickname = "철벽 유격수", Years = "1992~2010", Record = "MVP 투표 2위 3회", Speciality = Stat.Defense,
                Story = "수비상 9회에 통산 타율 .305, 2,104안타를 친 공수 겸장 유격수. 그런데 MVP 투표에서는 늘 2위였다. 세 번 모두 장태산에게 밀렸다. 과묵하고 성실하며, 장태산과는 지금도 티격태격한다.",
                Line = "글러브 말고 발로 잡는 거야. 펑고 몇 개만 쳐 줄게.",
                Lesson = "타구 방향을 미리 읽는 스텝을 배웠다. 공이 글러브로 빨려 들어온다.",
                Goal = c => $"MVP {OhMvps}회 (오세찬이 2위 했던 만큼)",
                Progress = c => (double)Mvps(c) / OhMvps,
                Halfway = c => Mvps(c) >= 1,
                ReturnText = "첫 MVP 트로피를 받은 다음 날, 오세찬이 찾아왔다.\n\"축하한다. ...나는 끝내 못 받아 봤다. 세 번 다 그 덩치한테 밀렸지.\"\n트로피를 오래 바라보다가 말했다. \"한 번은 운일 수 있다. 세 번은 실력이다.\"",
                ReturnLabelA = "같이 펑고를 받는다", ReturnResultA = "오세찬은 쉰이 넘었는데도 펑고가 날카로웠다. 마지막 공을 잡자 짧게 \"됐다\"고 했다.",
                ReturnLabelB = "\"세 번 받겠습니다\"", ReturnResultB = "\"말은 쉽지.\" 오세찬이 돌아섰다. 어깨가 조금 웃고 있었다.",
                SurpassText = "세 번째 MVP. 시상식 무대 아래 오세찬이 앉아 있었다.\n\n무대에서 내려오자 오세찬이 손을 내밀었다.\n\"내가 2위만 세 번 했던 상을, 네가 세 번 받았다. 이걸로 내 몫은 끝났다.\"\n처음 보는 환한 얼굴이었다.",
                SurpassLabel = "트로피를 오세찬에게 건넨다", SurpassResult = "오세찬은 트로피를 한 번 들어 보고 돌려줬다. \"무겁네. 이런 거였구나.\"",
                NearMiss = "오세찬은 '그 정도면 2위 세 번보다 낫다'며 어깨를 두드려 줬다.",
                Ceremony = "오세찬이 맨 앞자리에서 일어나 박수를 쳤다. 그 옆자리의 장태산이 무언가 투덜거렸다.",
            },
            new Legend
            {
                Id = "yoon", Name = "윤대혁", Nickname = "철완", Years = "1991~2011", Record = "통산 319승", Speciality = Stat.Stamina, ForPitchers = true,
                Story = "통산 최다승(319승) 선발 투수. 21년 동안 한 번도 로테이션을 거르지 않았다. 말이 적고 고집이 세다. \"완투는 투수의 자존심\"이라는 말을 아직도 한다.",
                Line = "공 빠른 놈은 많아. 9회까지 같은 공 던지는 놈이 드물지.",
                Lesson = "힘을 아끼는 투구 리듬을 배웠다. 6회가 지나도 팔이 가볍다.",
                Goal = c => CountGoal(c, YoonWins, "승"),
                Progress = c => Scaled(c, null, p => p.Wins) / (YoonWins + 1),
                Halfway = c => Scaled(c, null, p => p.Wins) >= YoonWins / 2,
                Chases = c => c.Me.Role == PitcherRole.Starter,
                ReturnText = "윤대혁이 불펜에 나타났다. 내 투구를 말없이 스무 개쯤 봤다.\n\"반은 왔네. 근데 나머지 반은 팔이 아니라 머리로 던지는 거다.\"\n\"서른 넘으면 공이 느려진다. 그때 이기는 법을 지금부터 익혀라.\"",
                ReturnLabelA = "완급 조절을 배운다", ReturnResultA = "\"같은 직구도 세 가지 속도로.\" 타자들의 타이밍이 어긋나기 시작했다.",
                ReturnLabelB = "완투 비결을 묻는다", ReturnResultB = "\"비결은 없다. 내려오기 싫었을 뿐이지.\"",
                SurpassText = "마지막 타자를 잡았다. 통산 최다승 신기록.\n\n포수가 달려오기도 전에, 관중석에서 한 노인이 일어섰다. 윤대혁이었다.\n경기 후 윤대혁이 낡은 글러브를 내밀었다.\n\"319승을 함께한 글러브다. 이제 쉬게 해 줘도 되겠다.\"",
                SurpassLabel = "글러브를 받는다", SurpassResult = "낡은 가죽에서 21년의 냄새가 났다.",
                NearMiss = "윤대혁은 '그 정도면 철완 소리 들을 만하다'고 짧게 말했다.",
                Ceremony = "윤대혁이 319승의 글러브를 끼고 마운드 옆에 섰다. 시구는 두 사람이 함께 했다.",
            },
            new Legend
            {
                Id = "ma", Name = "마석규", Nickname = "자물쇠", Years = "1996~2015", Record = "통산 650세이브", Speciality = Stat.Velocity, ForPitchers = true,
                Story = "통산 최다 세이브(650개). 8회를 지켜 준 단짝 셋업맨 노진우 덕분이라는 말을 입에 달고 산다. 늘 껌을 씹고 농담을 던지지만, 9회만 되면 눈빛이 바뀌었다. \"9회는 공이 아니라 배짱으로 던진다.\"",
                Line = "마지막 아웃 하나가 제일 무거워. 그거 드는 법 알려 줄까?",
                Lesson = "위기에서 숨 고르는 법을 배웠다. 9회의 공이 덜 떨린다.",
                Goal = c => CountGoal(c, MaSaves, "세이브"),
                Progress = c => Scaled(c, null, p => p.Saves) / (MaSaves + 1),
                Halfway = c => Scaled(c, null, p => p.Saves) >= MaSaves / 2,
                Chases = c => c.Me.Role == PitcherRole.Closer,
                ReturnText = "마석규가 껌을 씹으며 불펜 문을 열었다.\n\"벌써 반? 이러다 내 기록 날아가겠네.\"\n그러다 껌을 뱉고 진지해졌다. \"블론 하나에 무너지지 마라. 다음 날 또 올라가는 게 마무리다.\"",
                ReturnLabelA = "위기 상황 훈련을 받는다", ReturnResultA = "만루, 풀카운트. 마석규가 만든 가상 상황에서 백 번을 던졌다.",
                ReturnLabelB = "같이 껌을 씹는다", ReturnResultB = "\"오, 배짱 있네.\" 마석규가 껌 한 통을 통째로 줬다.",
                SurpassText = "마지막 공. 헛스윙. 경기 끝. 통산 신기록.\n\n불펜 문이 열리고 마석규가 껌을 씹으며 걸어 나왔다.\n\"내 자물쇠, 오늘부로 네 거다.\"\n마석규가 내 목에 작은 열쇠 목걸이를 걸어 줬다.",
                SurpassLabel = "열쇠 목걸이를 꽉 쥔다", SurpassResult = "관중석에서 '자물쇠' 응원가가 울렸다. 이번엔 내 이름이 들어갔다.",
                NearMiss = "마석규는 껌을 씹으며 '그 정도면 자물쇠 2호 자격 충분'이라고 했다.",
                Ceremony = "마석규가 열쇠 목걸이를 흔들며 올라왔다. \"자물쇠 반납받으러 왔다. ...농담이야.\"",
            },
            new Legend
            {
                Id = "noh", Name = "노진우", Nickname = "8회의 남자", Years = "1998~2017", Record = "통산 640홀드", Speciality = Stat.Breaking, ForPitchers = true,
                Story = "통산 최다 홀드(640개). 옆으로 던지는 싱커 하나로 20년 동안 8회만 던졌다. 땅볼 병살이 주특기. 세이브는 단 하나도 없다. 마석규의 650세이브 중 절반은 자기 몫이라고 농담하지만, 정작 은퇴식 날엔 마석규가 그 말을 대신 했다. 조용하고 꼼꼼해서, 지금도 상대 타자 노트를 손으로 쓴다.",
                Line = "박수는 9회가 받지. 근데 경기는 8회에 끝나는 거야.",
                Lesson = "팔을 옆으로 내려 공을 가라앉히는 싱커 그립을 배웠다. 타구가 땅으로 깔리기 시작했다.",
                Goal = c => CountGoal(c, NohHolds, "홀드"),
                Progress = c => Scaled(c, null, p => p.Holds) / (NohHolds + 1),
                Halfway = c => Scaled(c, null, p => p.Holds) >= NohHolds / 2,
                Chases = c => c.Me.Role == PitcherRole.Setup,
                ReturnText = "노진우가 손때 묻은 노트를 들고 불펜에 왔다.\n\"벌써 반이네. 이 노트, 너 나오는 경기는 다 적어 놨다.\"\n페이지를 넘기며 말했다. \"너는 2아웃 이후에 공이 높아져. 거기서 한 번만 버티면 된다.\"",
                ReturnLabelA = "노트를 같이 본다", ReturnResultA = "타자마다 빨간 동그라미가 하나씩 있었다. 그 코스로만 던졌더니 공이 방망이를 피해 갔다.",
                ReturnLabelB = "\"세이브는 왜 하나도 없어요?\"", ReturnResultB = "노진우가 웃었다. \"9회엔 석규가 있었으니까. 너도 믿을 사람 하나는 있어야 돼.\"",
                SurpassText = "8회 2아웃, 마지막 타자 삼진. 통산 홀드 신기록.\n\n더그아웃으로 들어오는데 전광판에 노진우의 얼굴이 떴다.\n경기 후 노진우가 노트 한 권을 내밀었다. 표지에 내 이름이 적혀 있었다.\n\"네 경기만 따로 적은 거다. 이제 네가 이어서 써라.\"",
                SurpassLabel = "노트를 받는다", SurpassResult = "첫 장에 노진우의 글씨로 적혀 있었다. '8회를 지키는 사람이 경기를 지킨다.'",
                NearMiss = "노진우는 노트 마지막 장에 '거의 다 왔었다. 충분히 잘 지켰다'고 적어 보냈다.",
                Ceremony = "노진우와 마석규가 나란히 올라왔다. \"8회랑 9회가 같이 왔다. 이제 네가 둘 다 해라.\"",
            },
        };

        public static Legend Find(string id) => All.FirstOrDefault(l => l.Id == id);
    }
}
