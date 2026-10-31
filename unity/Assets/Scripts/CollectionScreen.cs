using System.Collections.Generic;
using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// The collection (도감): legends met, rivals faced, trophies won and the players on my team,
    /// across every career and every save slot. Works from the title screen too, with no career.
    /// </summary>
    public class CollectionScreen : MonoBehaviour
    {
        enum Tab { Legends, Rivals, Trophies, MyTeam }

        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        static readonly Color Locked = new Color(0.36f, 0.4f, 0.48f);

        CareerState _career;
        Club _club;
        RectTransform _content;
        readonly Dictionary<Tab, Button> _tabs = new Dictionary<Tab, Button>();

        /// <param name="career">The career in progress, or null from the title screen.</param>
        public static void Open(CareerState career, Club club)
        {
            var screen = new GameObject("CollectionScreen").AddComponent<CollectionScreen>();
            screen._career = career;
            screen._club = club;
            screen.Build();
            screen.Show(Tab.Legends);
        }

        void Build()
        {
            var canvas = UiKit.Canvas(transform, "CollectionCanvas", 5);
            var root = UiKit.SafeRoot(canvas.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);
            UiKit.Backdrop(root, ArtLibrary.Background("collection"));

            UiKit.Height(UiKit.Label(root, "도감", 48), 80);

            var tabs = UiKit.Empty(root, "Tabs");
            UiKit.Height(tabs, 100);
            UiKit.Row(tabs, 16);
            _tabs[Tab.Legends] = UiKit.Button(tabs, "레전드", () => Show(Tab.Legends), UiKit.Card);
            _tabs[Tab.Rivals] = UiKit.Button(tabs, "라이벌", () => Show(Tab.Rivals), UiKit.Card);
            _tabs[Tab.Trophies] = UiKit.Button(tabs, "트로피", () => Show(Tab.Trophies), UiKit.Card);
            _tabs[Tab.MyTeam] = UiKit.Button(tabs, "내 팀", () => Show(Tab.MyTeam), UiKit.Card);

            var card = UiKit.Panel(root, "Card", UiKit.Card);
            UiKit.Height(card, 0, 1);
            UiKit.Column(card, 20, 0);
            var scroll = UiKit.ScrollList(card, 16);
            UiKit.Height(scroll.parent, 0, 1);
            _content = scroll;

            UiKit.Height(UiKit.Button(root, "닫기", () => Destroy(gameObject), UiKit.Muted), 110);
        }

        void Show(Tab tab)
        {
            foreach (var pair in _tabs) pair.Value.GetComponent<Image>().color = pair.Key == tab ? UiKit.Accent : UiKit.Background;
            foreach (Transform child in _content) Destroy(child.gameObject);
            switch (tab)
            {
                case Tab.Legends: ShowLegends(); break;
                case Tab.Rivals: ShowRivals(); break;
                case Tab.Trophies: ShowTrophies(); break;
                default: ShowMyTeam(); break;
            }
        }

        // ---------- Tabs ----------

        void ShowLegends()
        {
            var met = _club.AllLegendsMet(_career);
            Section($"만난 레전드 {met.Count(id => Legends.All.Any(l => l.Id == id))}/{Legends.All.Count}");
            Note("2026년 이전 리그를 빛낸 전설들이에요. 시즌 중 이벤트로 찾아와 한 수 가르쳐 줘요.");
            foreach (var pair in Pairs(Legends.All))
            {
                var row = GridRow(360);
                foreach (var legend in pair) LegendCard(row, legend, met.Contains(legend.Id));
            }
        }

        void LegendCard(RectTransform row, Legend legend, bool met)
        {
            var card = Cell(row, met ? UiKit.Shade(UiKit.Card, 1.35f) : UiKit.Shade(UiKit.Card, 0.8f));
            var portrait = met ? ArtLibrary.Get("Legends/" + legend.Id) : null;
            if (portrait != null) ArtLibrary.Banner(card, portrait, 150);
            else Badge(card, met ? legend.Name.Substring(0, 1) : "?", met ? UiKit.Shade(Gold, 0.75f) : Locked, 120);
            if (met)
            {
                Text(card, $"<b>{legend.Name}</b>", 34, Color.white, 50);
                Text(card, $"\"{legend.Nickname}\" · {legend.Years}", 24, Gold, 36);
                Text(card, $"{legend.Record} · 주특기 {Stats.Name(legend.Speciality)}", 24, UiKit.Muted, 70);
            }
            else
            {
                Text(card, "<b>???</b>", 34, Locked, 50);
                Text(card, "아직 만나지 못한 레전드", 24, Locked, 36);
                Text(card, $"주특기 {Stats.Name(legend.Speciality)} · {legend.Years}", 24, Locked, 70);
            }
        }

        void ShowRivals()
        {
            var current = _career?.Rival;
            var past = _club.RivalsMet.Where(name => current == null || name != current.Name).ToList();
            Section($"라이벌 {past.Count + (current != null ? 1 : 0)}명");
            Note("선수마다 같은 해에 입단한 동갑 라이벌이 생겨요. 은퇴할 때까지 함께 커요.");

            if (current != null)
            {
                Section("지금의 라이벌");
                var card = UiKit.Panel(_content, "Rival", UiKit.Shade(UiKit.Card, 1.35f));
                UiKit.Height(card, 230);
                var layout = UiKit.Row(card, 20);
                layout.padding = new RectOffset(24, 24, 20, 20);
                layout.childForceExpandWidth = false;
                var team = _career.RivalTeam;
                var badge = Badge(card, current.Name.Substring(0, 1), team == null ? Locked : UiKit.TeamColor(team.Name), 150);
                UiKit.Width(badge, 150);
                var l = _career.RivalLine;
                var me = _career.Me;
                int years = me.History.Count(h => h.RivalLine != null);
                string text = $"<b>{current.Name}</b>  <size=28>{current.Age}세 · {team?.Name ?? "소속 없음"}</size>\n" +
                    $"<size=26><color=#9EABC2>이번 시즌 타율 {Stats.Rate(Stats.Avg(l))} · {l.HomeRuns}홈런 · OPS {Stats.Rate(Stats.Ops(l))}</color></size>\n" +
                    $"<size=26><color=#9EABC2>{me.Name}와(과) {years + 1}번째 시즌</color></size>";
                UiKit.Width(UiKit.Label(card, text, 36), 0, 1);
            }

            if (past.Count > 0)
            {
                Section("지나간 라이벌");
                foreach (var pair in Pairs(past))
                {
                    var row = GridRow(200);
                    foreach (var name in pair)
                    {
                        var cell = Cell(row, UiKit.Shade(UiKit.Card, 1.2f));
                        Badge(cell, name.Substring(0, 1), UiKit.Shade(UiKit.Warning, 0.7f), 90);
                        Text(cell, $"<b>{name}</b>", 32, Color.white, 50);
                    }
                }
            }
            else if (current == null) Note("아직 만난 라이벌이 없어요. 첫 시즌이 시작되면 라이벌이 나타나요.");
        }

        void ShowTrophies()
        {
            // Every title won: by retired players, in finished seasons of the career in progress.
            var titles = _club.Roster.SelectMany(p => p.Titles).ToList();
            if (_career != null) titles.AddRange(_career.Me.History.SelectMany(h => h.Titles));
            var counts = titles.GroupBy(t => t).OrderByDescending(g => g.Key == Postseason.ChampionTitle).ThenByDescending(g => g.Count()).ToList();

            int allStars = _club.Roster.Sum(p => p.AllStars) + (_career?.Me.AllStarSelections ?? 0);
            int famers = _club.Roster.Count(p => p.HallOfFame);
            int retiredNumbers = _club.Roster.Count(p => p.NumberRetiredBy != null);
            var collection = new HashSet<string>(_club.Collection);
            if (_career != null) collection.UnionWith(_career.Me.Achievements);
            int achievements = Achievements.All.Count(a => collection.Contains(a.Id));

            Section($"트로피 {titles.Count}개");
            var highlights = new List<(string Name, string Value, bool Got)>
            {
                ("우승 반지", $"{titles.Count(t => t == Postseason.ChampionTitle)}개", titles.Contains(Postseason.ChampionTitle)),
                ("올스타 선정", $"{allStars}회", allStars > 0),
                ("명예의 전당", $"{famers}명", famers > 0),
                ("영구결번", $"{retiredNumbers}명", retiredNumbers > 0),
                ("업적", $"{achievements}/{Achievements.All.Count}", achievements > 0),
                ("도전 경기", $"{_club.ChallengesWon.Count}/{System.Enum.GetValues(typeof(ChallengeTier)).Length}단계", _club.ChallengesWon.Count > 0),
            };
            if (_career != null && _career.Me.GoldMedals > 0) highlights.Insert(1, ("국가대표 금메달", $"{_career.Me.GoldMedals}개", true));
            foreach (var pair in Pairs(highlights))
            {
                var row = GridRow(190);
                foreach (var h in pair) Trophy(row, h.Name, h.Value, h.Got);
            }

            Section("개인 타이틀");
            if (counts.Count == 0) Note("시즌이 끝나고 타이틀(홈런왕, MVP, 골든글러브…)을 따면 여기에 진열돼요.");
            foreach (var pair in Pairs(counts.Select(g => (g.Key, $"×{g.Count()}", true)).ToList()))
            {
                var row = GridRow(190);
                foreach (var t in pair) Trophy(row, t.Item1, t.Item2, true);
            }
        }

        void Trophy(RectTransform row, string name, string value, bool got)
        {
            var cell = Cell(row, got ? UiKit.Shade(UiKit.Card, 1.35f) : UiKit.Shade(UiKit.Card, 0.8f));
            var icon = ArtLibrary.Get("Icons/UI/trophy");
            if (icon != null) ArtLibrary.Banner(cell, icon, 70).color = got ? Gold : Locked;
            else Badge(cell, "★", got ? UiKit.Shade(Gold, 0.75f) : Locked, 70);
            Text(cell, $"<b>{name}</b>", 30, got ? Color.white : Locked, 44);
            Text(cell, value, 28, got ? Gold : Locked, 40);
        }

        void ShowMyTeam()
        {
            var roster = _club.Roster.OrderByDescending(p => p.HallOfFame).ThenByDescending(p => p.Overall).ToList();
            Section($"내 팀 선수 {roster.Count}명");
            if (roster.Count == 0)
            {
                Note("선수가 은퇴하면 전성기 능력치 그대로 내 팀 카드가 돼요. 모든 슬롯이 함께 모아요.");
                return;
            }
            foreach (var pair in Pairs(roster))
            {
                var row = GridRow(330);
                foreach (var p in pair) PlayerCard(row, p);
            }
        }

        void PlayerCard(RectTransform row, RetiredPlayer p)
        {
            var club = UiKit.TeamColor(p.MainClub ?? "");
            var cell = Cell(row, UiKit.Shade(club, 0.45f) + new Color(0.06f, 0.06f, 0.09f, 0));
            var top = UiKit.Empty(cell, "Top");
            UiKit.Height(top, 110);
            var layout = UiKit.Row(top, 12);
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var ovr = Badge(top, $"{p.Overall}", p.HallOfFame ? UiKit.Shade(Gold, 0.85f) : UiKit.Shade(club, 0.9f), 100);
            UiKit.Width(ovr, 100);
            var number = UiKit.Label(top, p.ShownNumber > 0 ? $"#{p.ShownNumber}" : "", 44, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.85f));
            number.fontStyle = FontStyle.Bold;
            UiKit.Width(number, 120);

            Text(cell, (p.HallOfFame ? "<color=#FFD24D>★</color> " : "") + $"<b>{p.Name}</b>", 34, Color.white, 50);
            Text(cell, $"{(p.IsPitcher ? Positions.RoleName(p.Role) + " 투수" : Positions.Name(p.Position))} · {p.Seasons}시즌", 24, UiKit.Muted, 36);
            var arm = p.CareerPitching;
            string totals = !p.IsPitcher || arm == null ? $"{p.CareerLine.Hits}안타 {p.CareerLine.HomeRuns}홈런"
                : p.Role == PitcherRole.Closer ? $"{arm.Saves}세이브 {arm.Strikeouts}K"
                : p.Role == PitcherRole.Setup ? $"{arm.Holds}홀드 {arm.Strikeouts}K"
                : $"{arm.Wins}승 {arm.Strikeouts}K";
            Text(cell, totals + (p.Titles.Count > 0 ? $" · 타이틀 {p.Titles.Count}" : ""), 24, Gold, 36);
            Text(cell, p.MainClub ?? "", 22, UiKit.Muted, 30);
        }

        // ---------- Pieces ----------

        static IEnumerable<List<T>> Pairs<T>(IList<T> items)
        {
            for (int i = 0; i < items.Count; i += 2) yield return items.Skip(i).Take(2).ToList();
        }

        RectTransform GridRow(float height)
        {
            var row = UiKit.Empty(_content, "Row");
            UiKit.Height(row, height);
            var layout = UiKit.Row(row, 16);
            layout.childForceExpandWidth = true;
            return row;
        }

        /// <summary>One card in a two-wide grid row.</summary>
        RectTransform Cell(RectTransform row, Color color)
        {
            var cell = UiKit.Panel(row, "Cell", color);
            UiKit.Width(cell, 0, 1);
            var layout = UiKit.Column(cell, 16, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            // An empty twin on the right keeps a lone last card half-width; the next card takes its place.
            var twin = row.Find("Twin");
            if (twin == null) UiKit.Width(UiKit.Empty(row, "Twin"), 0, 1);
            else
            {
                twin.SetAsLastSibling();
                if (row.childCount > 2)
                {
                    twin.gameObject.SetActive(false);
                    Destroy(twin.gameObject);
                }
            }
            UiAnim.FadeIn(cell);
            return cell;
        }

        /// <summary>A round badge with a letter or number in it.</summary>
        static RectTransform Badge(Transform parent, string text, Color color, float size)
        {
            var holder = UiKit.Empty(parent, "Badge");
            UiKit.Height(holder, size);
            var disc = UiKit.Panel(holder, "Disc", color);
            var image = disc.GetComponent<Image>();
            image.sprite = UiKit.CircleSprite;
            image.type = Image.Type.Simple;
            disc.anchorMin = disc.anchorMax = new Vector2(0.5f, 0.5f);
            disc.sizeDelta = new Vector2(size, size);
            var label = UiKit.Label(disc, text, (int)(size * 0.45f), TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            UiKit.Stretch(label.rectTransform);
            return holder;
        }

        static void Text(Transform parent, string text, int size, Color color, float height) =>
            UiKit.Height(UiKit.Label(parent, text, size, TextAnchor.MiddleCenter, color), height);

        void Section(string title) => UiKit.Height(UiKit.Label(_content, title, 34, TextAnchor.LowerLeft, Gold), 70);

        void Note(string text) => UiKit.Height(UiKit.Label(_content, text, 26, TextAnchor.MiddleLeft, UiKit.Muted), 80);
    }
}
