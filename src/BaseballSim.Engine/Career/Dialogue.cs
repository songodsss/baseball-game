using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BaseballSim.Career
{
    /// <summary>Who a dialogue line belongs to, which decides the picture shown with it.</summary>
    public enum SpeakerKind
    {
        /// <summary>Narration: no name, no new picture.</summary>
        None,
        /// <summary>A spoken line whose speaker the text doesn't make clear: shown without a name.</summary>
        Unknown,
        /// <summary>My own player.</summary>
        Me,
        /// <summary>A legend of the league, who has a drawn picture.</summary>
        Legend,
        /// <summary>Another ballplayer (a teammate, the rival...): gets one of the drawn player faces.</summary>
        Player,
        /// <summary>Anyone else (a coach, family, a reporter): a silhouette until they get a picture.</summary>
        Person,
    }

    /// <summary>One tap of a visual-novel style scene: a narration box, or a spoken line with its speaker.</summary>
    public class DialogueLine
    {
        public SpeakerKind Kind;
        /// <summary>The name on the name plate; null for narration and unknown speakers.</summary>
        public string Speaker;
        /// <summary>What shows in the text box.</summary>
        public string Text;
        /// <summary>The bit of narration around a spoken line ("도현이 웃었다."), shown small with it; may be null.</summary>
        public string Aside;
        /// <summary>The aside came before the spoken words on its line.</summary>
        public bool AsideFirst;
        /// <summary>Picks the picture: a legend's id, or the name a player face is picked from.</summary>
        public string PortraitKey;

        public bool IsSpeech => Kind != SpeakerKind.None;

        public override string ToString() => IsSpeech ? $"[{Speaker ?? "?"}] {Text}" + (Aside != null ? $" ({Aside})" : "") : Text;
    }

    /// <summary>
    /// Splits an event's text into visual-novel lines: narration, and quoted words with the person who
    /// says them. The speaker is read from the narration around the quote: the person who is the subject
    /// of the same line ("\"왔냐.\" 최강훈이었다."), else of the line before it ("도현이 툭 던졌다."),
    /// else whoever was talking just before. Who counts as a person comes from the career: the main
    /// story's people, the teammates and coach, the rival, the legends, and a few roles ("트레이너").
    /// </summary>
    public static class Dialogue
    {
        /// <summary>Narration boxes hold at most this many lines and characters before a new tap.</summary>
        const int NarrationLines = 3, NarrationChars = 110;

        public class CastMember
        {
            public string Name;
            public SpeakerKind Kind;
            public string PortraitKey;
            public List<string> Aliases = new List<string>();
        }

        /// <summary>
        /// The lines of a text, its people taken from the career. Once she is my girlfriend or wife, the
        /// heroine talks to me as a partner does (반말), and so do I when I answer her, whatever the
        /// scene was written for: the main story's reporter or YouTuber keeps her polite words for others.
        /// </summary>
        public static List<DialogueLine> Lines(string text, Career career, string context = null)
        {
            var lines = Lines(text, Cast(career), context);
            if (!Partnered(career)) return lines;
            string lastSpeaker = context == null ? null : Lines(context, Cast(career)).LastOrDefault(l => l.IsSpeech)?.Speaker;
            foreach (var l in lines)
            {
                if (!l.IsSpeech) continue;
                if (IsPartner(career, l.Speaker) || l.Kind == SpeakerKind.Me && IsPartner(career, lastSpeaker)) l.Text = Casual(l.Text);
                lastSpeaker = l.Speaker;
            }
            return lines;
        }

        /// <summary>The words chosen on a choice button, as my player's own line when they're a quote; null otherwise.
        /// context is the scene just told: an answer to my partner is said the way partners talk.</summary>
        public static DialogueLine MyChoice(string label, Career career, string context = null)
        {
            if (string.IsNullOrEmpty(label)) return null;
            string t = label.Trim();
            if (t.Length < 3 || t[0] != '"' || t[t.Length - 1] != '"' || t.IndexOf('"', 1) != t.Length - 1) return null;
            if (context != null && Partnered(career) && IsPartner(career, Lines(context, Cast(career)).LastOrDefault(l => l.IsSpeech && l.Kind != SpeakerKind.Me)?.Speaker))
                t = Casual(t);
            return new DialogueLine { Kind = SpeakerKind.Me, Speaker = career.Me.Name, Text = t, PortraitKey = career.Me.Name };
        }

        static bool Partnered(Career c) => c?.Me != null && (Romance.Lover(c) != null || c.HasMark("married"));

        static bool IsPartner(Career c, string speaker) =>
            speaker != null && (speaker == Romance.Lover(c)?.Name || c.HasMark("married") && (speaker == "아내" || speaker == "연인"));

        // Polite words (whole words: Word) and endings, with their 반말; longer ones first so "주세요" wins over "세요" and "요".
        static readonly (string Polite, string Casual, bool Word)[] Endings =
        {
            ("선수님이", "네가", true), ("선수님은", "너는", true), ("선수님한테", "너한테", true), ("선수님", "너", true), ("그쪽이", "네가", true), ("그쪽은", "너는", true), ("그쪽", "너", true),
            ("저도요", "나도", true), ("제가", "내가", true), ("저는", "나는", true), ("저를", "나를", true), ("저한테", "나한테", true), ("저랑", "나랑", true), ("저도", "나도", true), ("저희", "우리", true), ("제", "내", true), ("저", "나", true),
            ("네", "응", true),
            ("고맙습니다", "고마워", false), ("감사합니다", "고마워", false), ("죄송합니다", "미안해", false), ("미안합니다", "미안해", false), ("죄송해요", "미안해", false),
            ("겁니다", "거야", false), ("갑니다", "가", false), ("봅니다", "봐", false), ("옵니다", "와", false), ("압니다", "알아", false), ("주십시오", "줘", false), ("하십시오", "해", false), ("입니다", "이야", false), ("합니다", "해", false), ("됩니다", "돼", false), ("습니다", null, false), ("니다", "다", false),
            ("주세요", "줘", false), ("하세요", "해", false), ("보세요", "봐", false), ("오세요", "와", false), ("가세요", "가", false), ("드세요", "먹어", false), ("마세요", "마", false),
            ("지세요", "져", false), ("아세요", "알아", false), ("계세요", "있어", false), ("으세요", "어", false), ("세요", "셔", false),
            ("이에요", "이야", false), ("예요", "야", false), ("에요", "야", false), ("그쵸", "그치", false), ("죠", "지", false), ("군요", "구나", false),
        };

        static readonly string[] ThatOne = { "사람", "선수", "쪽", "집", "아이", "분", "멀리", "앞", "뒤", "위" };

        const string Breaks = " \".?!,~…'\n)";

        /// <summary>The 아/어 that follows a syllable (좋 → 아, 있 → 어): 습니다 → 좋아, 있어.</summary>
        static string Harmony(StringBuilder sb)
        {
            if (sb.Length == 0 || !IsHangul(sb[sb.Length - 1])) return "어";
            int vowel = (sb[sb.Length - 1] - 0xAC00) / 28 % 21;
            return vowel == 0 || vowel == 2 || vowel == 8 ? "아" : "어";
        }

        /// <summary>A quoted line said the way partners talk (반말): "선수님이 정해 주세요" → "네가 정해 줘".</summary>
        public static string Casual(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                // Words: only where a word starts (after a space, a quote or the start), so "제가" inside "문제가" stays.
                bool wordStart = i == 0 || " \"'\n(…".IndexOf(text[i - 1]) >= 0;
                bool done = false;
                foreach (var (polite, casual, word) in Endings)
                {
                    if (string.CompareOrdinal(text, i, polite, 0, polite.Length) != 0) continue;
                    int end = i + polite.Length;
                    bool atEnd = end >= text.Length || Breaks.IndexOf(text[end]) >= 0;
                    // A word must start where a word starts; a lone word like 저 or 네 must also end there.
                    if (word ? !wordStart || polite.Length == 1 && !atEnd : !atEnd) continue;
                    // 네 is "yes" only right before a stop (네 덕분에 is already 반말); 저 사람 is "that person".
                    if (polite == "네" && end < text.Length && text[end] == ' ') continue;
                    if (polite == "저" && ThatOne.Any(w => string.CompareOrdinal(text, end + 1, w, 0, w.Length) == 0)) continue;
                    sb.Append(casual ?? Harmony(sb));
                    i = end;
                    done = true;
                    break;
                }
                if (done) continue;
                // Any other "…요" at the end of a phrase: drop the 요 (있어요 → 있어, 할게요 → 할게).
                if (text[i] == '요' && i > 0 && IsHangul(text[i - 1]) && text[i - 1] != '필' && text[i - 1] != '중' && (i + 1 >= text.Length || Breaks.IndexOf(text[i + 1]) >= 0))
                {
                    i++;
                    continue;
                }
                sb.Append(text[i]);
                i++;
            }
            return sb.ToString();
        }

        /// <summary>Everyone who can speak in this career's scenes.</summary>
        public static List<CastMember> Cast(Career c)
        {
            var cast = new List<CastMember>();
            void Add(string name, SpeakerKind kind, string key = null, params string[] extra)
            {
                if (string.IsNullOrWhiteSpace(name) || cast.Any(m => m.Name == name)) return;
                extra = extra.Where(e => !cast.Any(o => o.Aliases.Contains(e))).ToArray();
                var m = new CastMember { Name = name, Kind = kind, PortraitKey = key ?? name };
                m.Aliases.Add(name);
                // Friends call each other by the given name: 윤도현 → 도현.
                if (name.Length == 3 && IsHangul(name[0]) && kind != SpeakerKind.Legend) m.Aliases.Add(name.Substring(1));
                m.Aliases.AddRange(extra.Where(e => !string.IsNullOrEmpty(e)));
                cast.Add(m);
            }

            if (c.Me != null) Add(c.Me.Name, SpeakerKind.Me);
            foreach (var l in Legends.All) Add(l.Name, SpeakerKind.Legend, l.Id);

            var route = MainStory.RouteOf(c);
            if (route != null)
            {
                Add(route.BondNames[(int)Bond.Mentor], SpeakerKind.Person);
                Add(route.BondNames[(int)Bond.Friend], SpeakerKind.Player);
                Add(route.BondNames[(int)Bond.Family], SpeakerKind.Person);
                Add(route.BondNames[(int)Bond.Voice], SpeakerKind.Person);
            }
            if (c.Rival != null) Add(c.Rival.Name, SpeakerKind.Player);

            // Read the marks rather than the name properties: those make up a name when there's none yet.
            var marks = c.Me?.StoryMarks ?? new Dictionary<string, int>();
            foreach (var key in marks.Keys.ToList())
            {
                int cut = key.IndexOf(':');
                if (cut < 0) continue;
                string role = key.Substring(0, cut), name = key.Substring(cut + 1);
                switch (role)
                {
                    case "senior": Add(name, SpeakerKind.Player, null, "선배", "선배님"); break;
                    case "junior": Add(name, SpeakerKind.Player, null, "후배"); break;
                    case "mate": case "veteran": case "exveteran": Add(name, SpeakerKind.Player); break;
                    case "coach": Add(name, SpeakerKind.Person, "coach", "코치", "코치님"); break;
                    case "excoach": Add(name, SpeakerKind.Person, "coach"); break;
                    case "met":
                        if (name.StartsWith("story:")) Add(name.Substring("story:".Length), SpeakerKind.Player);
                        break;
                }
            }
            foreach (var role in Roles)
                if (!cast.Any(m => m.Aliases.Contains(role))) Add(role, SpeakerKind.Person);
            return cast;
        }

        /// <summary>People the scenes name by what they are rather than who; longer words first so "감독님" wins over "감독".</summary>
        static readonly string[] Roles =
        {
            "감독님", "코치님", "선배님", "할머니", "할아버지", "어머니", "아버지", "트레이너", "인스트럭터", "투수 코치", "타격 코치",
            "기자", "꼬마", "아이", "아나운서", "주치의", "의사", "간호사", "단장", "사장님", "사장", "스카우트", "에이전트", "매니저",
            "캐스터", "해설위원", "통역", "엄마", "아빠", "아내", "연인", "동료", "아주머니", "멘탈 코치", "전력분석팀", "담당자", "청년", "선수들",
            "라파엘", "백승호", "강유진", "서하린", "정가을", "윤세아", "민채은", "한예린", "응원단장", "기사님", "반장", "지우", "아저씨", "형", "감독", "코치", "선배", "후배", "팬",
        };

        // ---------- Splitting ----------

        /// <summary>
        /// The lines of a text. context is what came just before (the scene, then the chosen answer): a quote
        /// nobody is named for falls back to whoever was talking there.
        /// </summary>
        public static List<DialogueLine> Lines(string text, List<CastMember> cast, string context = null)
        {
            var lines = new List<DialogueLine>();
            if (string.IsNullOrWhiteSpace(text)) return lines;
            text = text.Replace("\r", "");
            List<DialogueLine> before = context == null ? null : Lines(context, cast);
            var earlierNarration = new StringBuilder(context ?? "");
            string lastNarrationOfPrevious = null;
            foreach (var paragraph in text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var narration = new List<string>();   // the narration lines of this paragraph so far
                DialogueLine lastSpeech = null;        // spoken line with nothing but speech since
                var pending = new StringBuilder();     // narration waiting to become a box
                int pendingLines = 0;
                void Narrate(string line)
                {
                    if (pendingLines >= NarrationLines || (pending.Length > 0 && pending.Length + line.Length > NarrationChars)) Flush();
                    if (pending.Length > 0) pending.Append('\n');
                    pending.Append(line);
                    pendingLines++;
                }
                void Flush()
                {
                    if (pending.Length == 0) return;
                    lines.Add(new DialogueLine { Kind = SpeakerKind.None, Text = pending.ToString() });
                    pending.Clear();
                    pendingLines = 0;
                }

                foreach (var raw in paragraph.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    var parts = SplitQuotes(line);
                    if (!parts.Any(p => p.Speech))
                    {
                        Narrate(line);
                        narration.Add(line);
                        earlierNarration.Append('\n').Append(line);
                        lastSpeech = null;
                        continue;
                    }

                    string lead = parts[0].Speech ? "" : parts[0].Text;
                    int firstSpeech = parts.FindIndex(p => p.Speech);
                    string follow = firstSpeech + 1 < parts.Count && !parts[firstSpeech + 1].Speech ? parts[firstSpeech + 1].Text : "";
                    // Long narration around a quote: only the sentence touching it rides along as an aside.
                    var (leadRest, leadAside) = SplitLast(lead);
                    var (followAside, followRest) = SplitFirst(follow);
                    if (leadRest.Length > 0) Narrate(leadRest);
                    Flush();

                    // Who says it: someone introducing themselves, the subject of this line, then of the
                    // narration before, then whoever was talking, then whoever was last named.
                    string quotes = string.Join(" ", parts.Where(p => p.Speech).Select(p => p.Text));
                    var who = SelfIntroduced(quotes, cast)
                        ?? (FirstPerson(lead) || FirstPerson(follow) ? cast.FirstOrDefault(m => m.Kind == SpeakerKind.Me) : null)
                        ?? Subject(lead, cast, last: true) ?? Subject(follow, cast, last: false);
                    for (int i = narration.Count - 1; i >= 0 && who == null; i--) who = Subject(narration[i], cast, last: true);
                    if (who == null && lastSpeech != null && lead.Length == 0) who = Find(cast, lastSpeech.Speaker);
                    if (who == null && narration.Count == 0 && lastSpeech == null && lastNarrationOfPrevious != null)
                        who = Subject(lastNarrationOfPrevious, cast, last: true);
                    // A polite line (…습니다, …요) with nobody named is usually my own answer: leave it unnamed
                    // rather than hand it to the last speaker.
                    if (who == null && !Polite(quotes))
                    {
                        var talker = lines.Concat(before ?? new List<DialogueLine>()).Reverse()
                            .FirstOrDefault(l => l.Speaker != null && l.Kind != SpeakerKind.Me && l.Kind != SpeakerKind.None);
                        if (lines.Any(l => l.Speaker != null && l.Kind != SpeakerKind.Me))
                            talker = lines.Last(l => l.Speaker != null && l.Kind != SpeakerKind.Me);
                        who = Find(cast, talker?.Speaker) ?? Subject(earlierNarration.ToString(), cast, last: true, skipMe: true);
                    }

                    bool first = true;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (!parts[i].Speech) continue;
                        var dl = new DialogueLine
                        {
                            Kind = who?.Kind ?? SpeakerKind.Unknown,
                            Speaker = who?.Name,
                            PortraitKey = who?.PortraitKey,
                            Text = parts[i].Text,
                        };
                        if (first && leadAside.Length > 0) { dl.Aside = leadAside; dl.AsideFirst = true; }
                        else if (first && followAside.Length > 0) dl.Aside = followAside;
                        else if (!first && i > 0 && !parts[i - 1].Speech && i - 1 != firstSpeech + 1) { dl.Aside = parts[i - 1].Text; dl.AsideFirst = true; }
                        if (first && leadAside.Length > 0 && followAside.Length > 0) dl.Aside = leadAside + " " + followAside;
                        first = false;
                        lines.Add(dl);
                        lastSpeech = dl;
                    }
                    if (followRest.Length > 0) Narrate(followRest);
                    int lastSpeechAt = parts.FindLastIndex(p => p.Speech);
                    if (lastSpeechAt > firstSpeech + 1 && lastSpeechAt + 1 < parts.Count) Narrate(parts[lastSpeechAt + 1].Text);
                    if (lead.Length > 0) { narration.Add(lead); earlierNarration.Append('\n').Append(lead); }
                    if (follow.Length > 0) { narration.Add(follow); earlierNarration.Append('\n').Append(follow); }
                }
                Flush();
                lastNarrationOfPrevious = paragraph.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
                if (lastNarrationOfPrevious != null && SplitQuotes(lastNarrationOfPrevious).Any(p => p.Speech)) lastNarrationOfPrevious = null;
            }
            return lines;
        }

        /// <summary>"…다. 도현이 웃었다." → ("…다.", "도현이 웃었다."): the last sentence apart from the rest.</summary>
        static (string Before, string Last) SplitLast(string text)
        {
            text = text.Trim();
            int cut = SentenceBreak(text, fromEnd: true);
            return cut < 0 ? ("", text) : (text.Substring(0, cut).Trim(), text.Substring(cut).Trim());
        }

        static (string First, string After) SplitFirst(string text)
        {
            text = text.Trim();
            int cut = SentenceBreak(text, fromEnd: false);
            return cut < 0 ? (text, "") : (text.Substring(0, cut).Trim(), text.Substring(cut).Trim());
        }

        /// <summary>Where a new sentence starts (after ". ", "! ", "? "), the first or the last one; -1 if one sentence.</summary>
        static int SentenceBreak(string text, bool fromEnd)
        {
            int found = -1;
            for (int i = 1; i < text.Length - 1; i++)
                if (text[i] == ' ' && ".!?".IndexOf(text[i - 1]) >= 0)
                {
                    found = i + 1;
                    if (!fromEnd) return found;
                }
            return found;
        }

        static bool Polite(string quotes)
        {
            string t = quotes.TrimEnd('"', ' ', '.', '!', '?', '…', '~');
            return t.EndsWith("요") || t.EndsWith("니다") || t.EndsWith("니까") || t.EndsWith("세요");
        }

        /// <summary>"내가 말했다", "나는 웃었다": the narrator (my player) is the subject.</summary>
        static bool FirstPerson(string narration)
        {
            if (string.IsNullOrEmpty(narration)) return false;
            foreach (var w in new[] { "내가 ", "나는 ", "나도 " })
            {
                int at = narration.IndexOf(w, StringComparison.Ordinal);
                if (at == 0 || at > 0 && !IsHangul(narration[at - 1])) return true;
            }
            return false;
        }

        /// <summary>"나 윤도현.", "스포츠 데일리 한소라입니다": someone saying their own name.</summary>
        static CastMember SelfIntroduced(string quotes, List<CastMember> cast)
        {
            foreach (var m in cast)
            {
                if (m.Kind == SpeakerKind.Me || m.Name.Length < 2 || Roles.Contains(m.Name)) continue;
                foreach (var form in new[] { "나 " + m.Name, "저 " + m.Name, m.Name + "입니다", m.Name + "이에요", m.Name + "예요", m.Name + "이라고 합니다", m.Name + "라고 합니다" })
                    if (quotes.Contains(form)) return m;
            }
            return null;
        }

        struct Part
        {
            public bool Speech;
            public string Text;
        }

        /// <summary>
        /// A line cut into narration and spoken quotes. A quote is speech only when it stands on its own:
        /// at the start of the line or after a finished sentence, and not glued to a particle after it
        /// ("\"...\"는 게 입버릇" is a quoted phrase inside narration).
        /// </summary>
        static List<Part> SplitQuotes(string line)
        {
            var parts = new List<Part>();
            var narration = new StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                int open = line.IndexOf('"', i);
                if (open < 0) break;
                int close = line.IndexOf('"', open + 1);
                if (close < 0) break;
                string before = line.Substring(i, open - i);
                string sofar = (narration.ToString() + before).TrimEnd();
                bool standsAlone = sofar.Length == 0 || ".!?…—)".IndexOf(sofar[sofar.Length - 1]) >= 0;
                bool glued = close + 1 < line.Length && IsHangul(line[close + 1]);
                if (standsAlone && !glued && close - open > 1)
                {
                    narration.Append(before);
                    if (narration.ToString().Trim().Length > 0) parts.Add(new Part { Text = narration.ToString().Trim() });
                    narration.Clear();
                    parts.Add(new Part { Speech = true, Text = line.Substring(open, close - open + 1) });
                }
                else narration.Append(line, i, close + 1 - i);
                i = close + 1;
            }
            narration.Append(line.Substring(Math.Min(i, line.Length)));
            if (narration.ToString().Trim().Length > 0) parts.Add(new Part { Text = narration.ToString().Trim() });
            return parts;
        }

        static readonly string[] Titles = { " 코치님", " 감독님", " 선배님", " 코치", " 감독", " 선배", " 기자", "님" };
        const string SubjectParticles = "이가은는께도";

        /// <summary>
        /// The person a piece of narration is about: one marked as the subject (도현이, 최강훈 감독은,
        /// 감독님께서) if any, else anyone named at all. last picks the one nearest the end.
        /// </summary>
        static CastMember Subject(string narration, List<CastMember> cast, bool last, bool skipMe = false)
        {
            if (string.IsNullOrEmpty(narration)) return null;
            var found = new List<(int At, int End, bool Subject, CastMember Who)>();
            foreach (var m in cast)
                foreach (var alias in m.Aliases.OrderByDescending(a => a.Length))
                {
                    int at = 0;
                    while ((at = narration.IndexOf(alias, at, StringComparison.Ordinal)) >= 0)
                    {
                        int end = at + alias.Length;
                        bool startsWord = at == 0 || !IsHangul(narration[at - 1]);
                        if (startsWord)
                        {
                            foreach (var t in Titles)
                                if (string.CompareOrdinal(narration, end, t, 0, t.Length) == 0) { end += t.Length; break; }
                            bool subject = end < narration.Length && SubjectParticles.IndexOf(narration[end]) >= 0
                                && !(end + 1 < narration.Length && narration[end] == '이' && (narration[end + 1] == '랑' || narration[end + 1] == '나'));
                            bool wordEnds = end >= narration.Length || !IsHangul(narration[end]) || IsParticleStart(narration, end);
                            if (wordEnds && !found.Any(f => at < f.End && end > f.At))
                                found.Add((at, end, subject, m));
                        }
                        at = end;
                    }
                }
            if (skipMe) found.RemoveAll(f => f.Who.Kind == SpeakerKind.Me);
            if (found.Count == 0) return null;
            var pool = found.Any(f => f.Subject) ? found.Where(f => f.Subject) : found.Where(f => f.Who.Kind != SpeakerKind.Me);
            var ordered = pool.OrderBy(f => f.At).ToList();
            if (ordered.Count == 0) return null;
            return (last ? ordered.Last() : ordered.First()).Who;
        }

        static readonly string[] Particles =
        {
            "이었", "였", "이다", "이야", "이랑", "랑", "에게", "한테", "께", "이", "가", "은", "는", "을", "를", "의", "과", "와", "도", "만", "아", "야", "에",
        };

        static bool IsParticleStart(string s, int at) => Particles.Any(p => string.CompareOrdinal(s, at, p, 0, p.Length) == 0);

        static CastMember Find(List<CastMember> cast, string name) => name == null ? null : cast.FirstOrDefault(m => m.Name == name);

        static bool IsHangul(char ch) => ch >= 0xAC00 && ch <= 0xD7A3;
    }
}
