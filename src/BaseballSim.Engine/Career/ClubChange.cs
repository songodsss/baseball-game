using System.Linq;

namespace BaseballSim.Career
{
    public partial class Career
    {
        /// <summary>The club the player last left, for the scenes after a move ("" before any move).</summary>
        public string FormerClub => Me.StoryMarks.Keys.Where(k => k.StartsWith("from:")).Select(k => k.Substring(5)).FirstOrDefault() ?? "";

        /// <summary>
        /// Moving clubs: the people of the old clubhouse stay behind. The veteran, the junior and the coach
        /// are old-club people from now on (closeness kept under "old" + role for the ending), and new
        /// ones are picked from the new club as everyday scenes need them. The roommate senior and the
        /// draft-class mate stay friends, but their clubhouse scenes stop.
        /// </summary>
        void LeaveClubPeople(Team from)
        {
            if (from == null) return;
            // The people of this clubhouse are remembered per club, so coming back later finds them again.
            foreach (var k in Me.StoryMarks.Keys.Where(k => k.StartsWith(KeepPrefix(from.Name))).ToList()) Me.StoryMarks.Remove(k);
            foreach (var role in new[] { "veteran", "junior", "coach" })
            {
                var key = Me.StoryMarks.Keys.FirstOrDefault(k => k.StartsWith(role + ":"));
                if (key == null) continue;
                Me.StoryMarks[KeepPrefix(from.Name) + key] = Me.StoryMarks[key];
                Me.StoryMarks[KeepPrefix(from.Name) + "close:" + role] = Closeness(role);
                // Replace any earlier club's person in the same role: only the latest goodbye is kept.
                foreach (var old in Me.StoryMarks.Keys.Where(k => k.StartsWith("old" + role + ":")).ToList()) Me.StoryMarks.Remove(old);
                Me.StoryMarks["old" + key] = Me.StoryMarks[key];
                Me.StoryMarks.Remove(key);
                if (Me.StoryMarks.TryGetValue("close:" + role, out int close))
                {
                    Me.StoryMarks["close:old" + role] = close;
                    Me.StoryMarks.Remove("close:" + role);
                }
            }
            foreach (var role in new[] { "senior", "mate" })
                if (Me.StoryMarks.Keys.Any(k => k.StartsWith(role + ":")) && !HasMark("apart:" + role))
                {
                    Me.StoryMarks["apart:" + role] = Stamp;
                    Me.StoryMarks[KeepPrefix(from.Name) + "together:" + role] = Stamp;
                }
            foreach (var k in Me.StoryMarks.Keys.Where(k => k.StartsWith("from:")).ToList()) Me.StoryMarks.Remove(k);
            Me.StoryMarks["from:" + from.Name] = Stamp;
            Me.StoryMarks["moved"] = Stamp;
            Me.StoryMarks.Remove("homecoming");
        }

        static string KeepPrefix(string club) => "keep:" + club + "|";

        /// <summary>Played for this club before (the first club too, which Me.Clubs leaves out).</summary>
        bool PlayedFor(string club) =>
            Me.StoryMarks.Keys.Any(k => k.StartsWith(KeepPrefix(club))) || Me.History.Any(h => h.Club == club);

        /// <summary>
        /// Coming back to a club played for before: the veteran, the junior and the coach of those days are
        /// there again with the closeness they had, the roommate senior and the draft-class mate are
        /// teammates again, and the scenes after the move are a homecoming rather than a first day.
        /// </summary>
        void ReturnClubPeople(Team to)
        {
            if (to == null || !PlayedFor(to.Name)) return;
            Me.StoryMarks["homecoming"] = Stamp;
            string prefix = KeepPrefix(to.Name);
            var kept = Me.StoryMarks.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var role in new[] { "veteran", "junior", "coach" })
            {
                var key = kept.FirstOrDefault(k => k.Substring(prefix.Length).StartsWith(role + ":"));
                if (key == null) continue;
                string person = key.Substring(prefix.Length);
                foreach (var now in Me.StoryMarks.Keys.Where(k => k.StartsWith(role + ":")).ToList()) Me.StoryMarks.Remove(now);
                Me.StoryMarks[person] = Me.StoryMarks[key];
                if (Me.StoryMarks.TryGetValue(prefix + "close:" + role, out int close)) Me.StoryMarks["close:" + role] = close;
                // The one just left behind in this role is the "old" one now; the same person can't be both.
                string name = person.Substring(role.Length + 1);
                if (Me.StoryMarks.ContainsKey("old" + role + ":" + name))
                {
                    Me.StoryMarks.Remove("old" + role + ":" + name);
                    Me.StoryMarks.Remove("close:old" + role);
                }
            }
            foreach (var role in new[] { "senior", "mate" })
                if (kept.Contains(prefix + "together:" + role)) Me.StoryMarks.Remove("apart:" + role);
            foreach (var k in kept) Me.StoryMarks.Remove(k);
        }

        /// <summary>Back at a club played for before, since the last move.</summary>
        public bool CameHome => HasMark("homecoming");
    }

    public static partial class StoryEvents
    {
        /// <summary>A clubhouse scene about the senior or the draft-class mate, after the player has left their club.</summary>
        static bool AboutSomeoneLeftBehind(Career c, StoryEvent e) =>
            e.Text != null && (c.HasMark("apart:senior") && e.Text.Contains("{senior}") || c.HasMark("apart:mate") && e.Text.Contains("{mate}"));

        /// <summary>Seen since the last move.</summary>
        static bool SeenSinceMove(Career c, string id) =>
            c.Me.StoryMarks.TryGetValue("seen:" + id, out int seen) && c.Me.StoryMarks.TryGetValue("moved", out int moved) && seen >= moved;

        /// <summary>After a move: the first day, the first dinner, the first game against the old club, the new fans, a text from an old friend.</summary>
        static StoryEvent ClubChangeMoment(Career c, DayResult day)
        {
            if (!c.HasMark("moved") || c.InFarm) return null;
            int since = c.GamesSince("moved");
            string from = c.FormerClub;
            if (!SeenSinceMove(c, "club_arrival")) return since <= 6 ? (c.CameHome ? Homecoming(c) : Arrival(c)) : null;
            if (!SeenSinceMove(c, "club_dinner") && since >= 4 && since <= 40) return c.CameHome ? HomeDinner : Dinner;
            if (!SeenSinceMove(c, "club_old_foe") && from != "" && day?.MyGame != null && (day.MyGame.Home.Name == from || day.MyGame.Away.Name == from))
                return OldFoe(c, day.MyGame.Home.Name == from);
            if (!SeenSinceMove(c, "club_old_friend") && since >= 10 && since <= 60 && (c.HasMark("apart:senior") || c.HasMark("apart:mate")))
                return OldFriend(c);
            if (!SeenSinceMove(c, "club_new_song") && since >= 20 && since <= 80 && Played(day)) return c.CameHome ? OldSong : NewSong;
            return null;
        }

        /// <summary>The trade rumor in the papers, the day after it starts.</summary>
        static StoryEvent RumorMoment(Career c)
        {
            if (string.IsNullOrEmpty(c.TradeRumor) || c.Day <= c.RumorDay || c.Day >= c.TradeDeadline || c.SeenThisSeason("trade_rumor")) return null;
            return new StoryEvent
            {
                Id = "trade_rumor",
                Title = "트레이드 루머",
                Text = $"아침 스포츠면에 내 이름이 실렸다. '{c.TradeRumor}, {{name}} 영입 추진설.'\n훈련장 분위기가 어색하다. 감독은 눈을 마주치지 않았고, {{junior}}가 조심스레 물었다. \"선배님, 진짜 가세요?\"\n트레이드 마감까지 며칠 남지 않았다.",
                Weight = (_, __) => 0,
                Choices =
                {
                    new EventChoice { Label = "단장실 문을 두드린다", Result = c.TradeRumorReal ? "단장은 한참 말이 없다가 \"아직 정해진 건 없다\"고만 했다. 그 말이 더 불안했다." : "\"신문 보지 마라. 너는 우리 팀 선수다.\" 단장이 웃으며 어깨를 두드렸다.", Apply = e => { e.Morale(c.TradeRumorReal ? -1 : 3); e.Trust(1); } },
                    new EventChoice { Label = "신경 끄고 야구에 집중한다", Result = "어디서 뛰든 야구는 야구다. 배트를 더 세게 쥐었다.", Apply = e => { e.Exp(Stat.Contact, 40); e.Morale(1); } },
                    new EventChoice { Label = "동료들과 밥을 먹는다", Result = "\"가도 연락해라.\" \"안 간다니까.\" 웃음 속에 묘한 쓸쓸함이 섞였다.", Apply = e => { e.Morale(3); e.Close("junior", 1); e.Close("veteran", 1); } },
                },
            };
        }

        static StoryEvent Arrival(Career c)
        {
            bool traded = c.SeasonNotes.Any(n => n.Contains("트레이드")) && c.Day > 0;
            return new StoryEvent
            {
                Id = "club_arrival",
                Title = "새 유니폼",
                Text = (traded
                        ? "어제까지 상대 팀이던 라커룸. 짐 가방을 들고 들어서자 대화가 뚝 끊겼다.\n사물함에는 급하게 붙인 이름표, 옷걸이엔 처음 입어 보는 {team} 유니폼."
                        : "{team}에서의 첫 출근. 구단 버스, 식당, 라커룸까지 전부 낯설다.\n사물함 위에 새 등번호가 박힌 유니폼이 걸려 있었다.")
                    + "\n{veteran} 선배가 먼저 손을 내밀었다. \"잘 왔다. 여기 사람들 생각보다 착해.\"",
                Weight = (_, __) => 0,
                Choices =
                {
                    new EventChoice { Label = "한 명 한 명 찾아가 인사한다", Result = "이름을 다 외우는 데 이틀이 걸렸다. 사흘째엔 장난을 걸어오는 동료가 생겼다.", Apply = e => { e.Morale(4); e.Close("veteran", 2); e.Trust(1); } },
                    new EventChoice { Label = "말보다 훈련으로 보여 준다", Result = "첫날부터 가장 늦게까지 남았다. 코치들이 수첩에 뭔가를 적었다.", Apply = e => { e.Exp(Stat.Contact, 40); e.Fatigue(10); e.Close("coach", 1); } },
                },
            };
        }

        static StoryEvent Homecoming(Career c)
        {
            bool traded = c.SeasonNotes.Any(n => n.Contains("트레이드")) && c.Day > 0;
            bool senior = !c.HasMark("apart:senior") && c.Me.StoryMarks.Keys.Any(k => k.StartsWith("senior:")) && !c.HasMark("gone:senior");
            return new StoryEvent
            {
                Id = "club_arrival",
                Title = "다시, 친정팀",
                Text = (traded
                        ? "짐 가방을 들고 다시 이 라커룸 문을 열었다. 몇 년 만인데도 냄새부터 그대로다."
                        : "다시 {team}의 출근길. 구단 버스 기사님이 먼저 알아봤다. \"어이, 돌아왔네!\"")
                    + "\n내 옛 사물함 자리엔 누가 붙여 놨는지 '{name} 지정석' 이름표가 붙어 있었다."
                    + "\n{veteran} 선배가 팔짱을 끼고 서 있었다. \"늦었다. 밥 먹으러 가자.\""
                    + (senior ? "\n뒤에서 {senior} 선배가 웃었다. \"이 방 아직 네 자리 비워 놨다.\"" : ""),
                Weight = (_, __) => 0,
                Choices =
                {
                    new EventChoice { Label = "\"다녀왔습니다\" 꾸벅 인사한다", Result = "\"다녀왔으면 됐다.\" 그 한마디에 떠나 있던 시간이 접혔다.", Apply = e => { e.Morale(6); e.Close("veteran", 1); e.Close("coach", 1); e.Fans(40); } },
                    new EventChoice { Label = "\"그동안 더 늘어서 왔습니다\"", Result = "\"어디 보자.\" {coach} 코치가 배팅 케이지 문을 열었다. 옛날처럼.", Apply = e => { e.Exp(Stat.Contact, 50); e.Close("coach", 2); e.Morale(3); } },
                },
            };
        }

        static readonly StoryEvent HomeDinner = new StoryEvent
        {
            Id = "club_dinner",
            Title = "돌아온 날의 회식",
            Text = "단골 고깃집, 늘 앉던 구석 자리. 사장님이 말없이 내 앞에 공깃밥을 두 그릇 놨다.\n{junior}가 잔을 들었다. \"선배님 없을 때 저 혼자 얼마나 힘들었는지 아세요?\"",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "떠나 있던 동안의 이야기를 풀어 놓는다", Result = "다른 팀 이야기에 다들 귀를 쫑긋 세웠다. \"거기선 그렇게 해?\" 밤이 짧았다.", Apply = e => { e.Morale(5); e.Close("junior", 2); e.Close("veteran", 1); } },
                new EventChoice { Label = "\"이제 안 떠난다\"", Result = "\"그 말 녹음했습니다.\" {junior}가 휴대폰을 흔들었다. 다들 웃었다.", Apply = e => { e.Morale(4); e.Close("junior", 1); e.Trust(1); e.Fans(40); } },
            },
        };

        static readonly StoryEvent OldSong = new StoryEvent
        {
            Id = "club_new_song",
            Title = "그 응원가",
            Text = "첫 타석에 들어서는데 외야석에서 익숙한 전주가 흘렀다.\n떠나기 전 그 응원가. 팬들이 한 글자도 잊지 않고 있었다. 누군가 '다시 우리 {name}'이라고 쓴 현수막을 흔들었다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "헬멧을 벗고 외야석을 향해 고개 숙인다", Result = "응원가가 더 커졌다. 돌아올 곳이 있다는 건 이런 거구나.", Apply = e => { e.Fans(120); e.Morale(6); } },
                new EventChoice { Label = "응원가에 맞춰 배트를 흔든다", Result = "옛날 하던 그 동작에 관중석이 뒤집어졌다.", Apply = e => { e.Fans(80); e.Morale(5); e.Trust(1); } },
            },
        };

        static readonly StoryEvent Dinner = new StoryEvent
        {
            Id = "club_dinner",
            Title = "새 팀 첫 회식",
            Text = "원정 숙소 근처 고깃집. {junior}가 쭈뼛거리며 물었다.\n\"선배님, 전 팀에선 어떤 분위기였어요?\" 테이블이 조용해졌다. 다들 궁금했던 모양이다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "옛 팀 이야기를 웃기게 풀어 준다", Result = "전 팀 감독 성대모사에 다들 숨이 넘어갔다. 그날부터 '우리 팀 사람'이 됐다.", Apply = e => { e.Morale(5); e.Close("junior", 2); e.Close("veteran", 1); } },
                new EventChoice { Label = "\"이제 여기가 내 팀이야\"", Result = "잔이 부딪쳤다. {junior}가 제일 크게 외쳤다. \"우리 팀!\"", Apply = e => { e.Morale(4); e.Close("junior", 1); e.Trust(1); e.Fans(20); } },
            },
        };

        static StoryEvent OldFoe(Career c, bool atTheirPark) => new StoryEvent
        {
            Id = "club_old_foe",
            Title = "친정팀과의 첫 맞대결",
            Text = (atTheirPark
                    ? $"{c.FormerClub}의 홈구장. 원정 더그아웃은 처음이다.\n첫 타석에 들어서자 3루 쪽 관중석에서 박수가 쏟아졌다. 옛 팬들이 아직 응원가를 기억하고 있었다."
                    : $"오늘 상대는 {c.FormerClub}. 건너편 더그아웃에 익숙한 얼굴들이 보인다.\n경기 전 옛 동료들이 하나둘 찾아와 등을 두드렸다. \"오늘은 봐주지 마라.\"")
                + "\n\n묘한 기분이다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "옛 팬들에게 모자를 벗어 인사한다", Result = "박수가 더 커졌다. 다음 날 사진 한 장이 양 팀 팬 커뮤니티에 동시에 올라왔다.", Apply = e => { e.Fans(80); e.Morale(4); } },
                new EventChoice { Label = "\"오늘은 적이다\" 이를 악문다", Result = "경기 내내 집중력이 남달랐다. 보여 주고 싶었다, 나를 보낸 팀에게.", Apply = e => { e.Morale(6); e.Exp(Stat.Eye, 40); } },
            },
        };

        static StoryEvent OldFriend(Career c)
        {
            bool senior = c.HasMark("apart:senior");
            string who = senior ? "{senior} 선배" : "{mate}";
            return new StoryEvent
            {
                Id = "club_old_friend",
                Title = "옛 동료의 문자",
                Text = $"밤늦게 휴대폰이 울렸다. {c.FormerClub}의 {who}였다.\n\"새 팀 유니폼 잘 어울리더라. 근데 우리 팀이랑 할 땐 살살 해라. ㅋㅋ\"",
                Weight = (_, __) => 0,
                Choices =
                {
                    new EventChoice { Label = "영상 통화를 건다", Result = "한 시간 넘게 떠들었다. 팀은 바뀌어도 사람은 남는다.", Apply = e => { e.Morale(5); e.Close(senior ? "senior" : "mate", 1); } },
                    new EventChoice { Label = "\"다음 맞대결 때 밥 사요\"", Result = "\"이기는 팀이 사는 걸로.\" 답장이 바로 왔다.", Apply = e => { e.Morale(3); e.Close(senior ? "senior" : "mate", 1); } },
                },
            };
        }

        static readonly StoryEvent NewSong = new StoryEvent
        {
            Id = "club_new_song",
            Title = "새 응원가",
            Text = "경기 중 외야석에서 처음 듣는 노래가 울려 퍼졌다.\n{team} 응원단이 만든 {name}의 새 응원가였다. 팬들이 벌써 가사를 다 외우고 있었다.",
            Weight = (_, __) => 0,
            Choices =
            {
                new EventChoice { Label = "다음 타석에서 헬멧을 들어 답한다", Result = "응원가가 두 배로 커졌다. 이제 정말 이 팀 선수다.", Apply = e => { e.Fans(100); e.Morale(5); } },
                new EventChoice { Label = "응원단장을 찾아가 감사 인사", Result = "응원단장이 울먹였다. \"이 노래 만들려고 밤샜어요.\"", Apply = e => { e.Fans(60); e.Morale(4); e.Trust(1); } },
            },
        };
    }
}
