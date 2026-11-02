using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// Home: the player in focus with one big button, every save slot as a small card, and
    /// four tiles for what belongs to the whole club (my team, collection, missions, shop).
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        public const string GameTitle = "나의 야구 인생";

        // Press Play in any scene and the prototype starts here.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<TitleScreen>() == null && FindAnyObjectByType<GrowthScreen>() == null) Show();
        }

        public static void Show() => new GameObject("TitleScreen").AddComponent<TitleScreen>();

        IRandom _random;
        Club _club;
        RectTransform _root, _overlay;

        void Start()
        {
            _random = new SeededRandom(System.Environment.TickCount);
            var canvas = UiKit.Canvas(transform, "TitleCanvas");
            _root = UiKit.SafeRoot(canvas.transform, UiKit.Background);
            UiKit.Backdrop(_root, ArtLibrary.Background("title") ?? ArtLibrary.Background("growth"), 0.75f);
            UiKit.Column(_root, 40, 18);
            Build();
        }

        int _selected;

        void Build()
        {
            CloseOverlay();
            foreach (Transform child in _root) Destroy(child.gameObject);
            _club = SaveStore.LoadClub(_random);
            if (_club.Daily.Refresh(Today)) SaveStore.SaveClub(_club);

            var slots = Enumerable.Range(1, SaveStore.SlotCount).Select(slot => SaveStore.Peek(slot, _club, _random)).ToList();
            // The player in focus: the one picked, else the last one played, else the first with a player.
            if (_selected < 1 || _selected > slots.Count)
                _selected = !slots[Mathf.Clamp(SaveStore.CurrentSlot, 1, slots.Count) - 1].IsEmpty ? Mathf.Clamp(SaveStore.CurrentSlot, 1, slots.Count)
                    : (slots.FirstOrDefault(x => !x.IsEmpty)?.Slot ?? 1);
            var hero = slots[_selected - 1];

            TopBar();
            HeroCard(hero);
            Heading("선수 슬롯", $"{slots.Count(x => !x.IsEmpty)}/{slots.Count}명");
            SlotStrip(slots);
            Heading("메뉴", null);
            Menu();
            UiKit.Height(UiKit.Empty(_root, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Label(_root, "프로토타입 0.1", 24, TextAnchor.MiddleCenter, UiKit.Muted), 34);
        }

        // ---------- Home pieces ----------

        /// <summary>Game name on the left; what my whole club holds on the right.</summary>
        void TopBar()
        {
            var bar = UiKit.Empty(_root, "TopBar");
            UiKit.Height(bar, 100);
            var row = UiKit.Row(bar, 14);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            ArtLibrary.Icon(bar, ArtLibrary.Get("App/app_icon"), 84);
            var title = UiKit.Label(bar, GameTitle, 46);
            title.fontStyle = FontStyle.Bold;
            UiKit.Width(title, 0, 1);
            Chip(bar, $"명예 {_club.LegacyPoints:N0}", Gold);
            Chip(bar, $"회복권 {_club.Daily.RecoveryTickets}", new Color(0.75f, 0.62f, 1f));
        }

        static void Chip(Transform parent, string text, Color color)
        {
            var chip = UiKit.Panel(parent, "Chip", new Color(0, 0, 0, 0.35f));
            UiKit.Width(chip, 200);
            UiKit.Height(chip, 64);
            var label = UiKit.Label(chip, text, 26, TextAnchor.MiddleCenter, color);
            label.fontStyle = FontStyle.Bold;
            UiKit.Stretch(label.rectTransform);
        }

        void Heading(string title, string note)
        {
            var label = UiKit.Label(_root, $"<b>{title}</b>" + (note == null ? "" : $"  <size=24><color=#BAC4D8>{note}</color></size>"), 32, TextAnchor.LowerLeft, Gold);
            UiKit.Height(label, 46);
        }

        /// <summary>The player in focus, big, in the club's colours, with the one button that matters.</summary>
        void HeroCard(SlotSummary summary)
        {
            var career = summary.Career;
            Color team = career == null ? UiKit.Accent : UiKit.TeamColor(career.MyTeam.Name);
            // A rounded card in the club colour, darkening toward the bottom so the button stands out.
            var card = UiKit.Panel(_root, "Hero", UiKit.Shade(team, 0.8f));
            card.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shade = UiKit.Empty(card, "Shade");
            UiKit.Stretch(shade);
            shade.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var shadeImage = shade.gameObject.AddComponent<Image>();
            shadeImage.sprite = UiKit.GradientSprite(new Color(1, 1, 1, 0.08f), new Color(0, 0, 0, 0.6f));
            shadeImage.raycastTarget = false;
            UiKit.Height(card, 520);
            UiKit.Column(card, 0, 0);
            UiAnim.PopIn(card, 0.3f);

            // The upper part: portrait on the left, who and where on the right, badge in the corner.
            var upper = UiKit.Empty(card, "Upper");
            UiKit.Height(upper, 0, 1);
            var row = UiKit.Row(upper, 20);
            row.padding = new RectOffset(28, 28, 28, 0);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;

            var face = UiKit.Panel(upper, "Face", new Color(0, 0, 0, 0.25f));
            UiKit.Width(face, 300);
            var art = career == null ? null : ArtLibrary.Portrait(career.Me);
            var pic = UiKit.Empty(face, "Picture");
            UiKit.Stretch(pic);
            var picImage = pic.gameObject.AddComponent<Image>();
            picImage.raycastTarget = false;
            picImage.preserveAspect = true;
            if (art != null) picImage.sprite = art;
            else
            {
                picImage.enabled = false;
                var plus = UiKit.Label(face, career == null ? "+" : career.Me.Name.Substring(0, 1), 140, TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.8f));
                UiKit.Stretch(plus.rectTransform);
            }

            var info = UiKit.Empty(upper, "Info");
            UiKit.Width(info, 0, 1);
            var column = UiKit.Column(info, 0, 6);
            column.childAlignment = TextAnchor.MiddleLeft;
            if (career == null)
            {
                Line(info, $"<b>슬롯 {summary.Slot}</b>", 30, new Color(1, 1, 1, 0.7f), 44);
                Line(info, "<b>새 선수 만들기</b>", 54, Color.white, 80);
                Line(info, "이름, 포지션, 입단할 구단을 정하고\n신인 선수부터 키워요.", 28, new Color(1, 1, 1, 0.85f), 90);
            }
            else
            {
                var me = career.Me;
                string position = me.IsPitcher ? Positions.RoleName(me.Role) : Positions.Name(me.Position);
                string where = career.InMilitary ? "군 복무 중" : career.InFarm ? "2군" : career.MyTeam.Name;
                var top = UiKit.Empty(info, "Top");
                UiKit.Height(top, 90);
                var topRow = UiKit.Row(top, 12);
                topRow.childForceExpandWidth = false;
                topRow.childAlignment = TextAnchor.MiddleLeft;
                var slotLabel = UiKit.Label(top, $"<b>슬롯 {summary.Slot}</b>", 28, TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.7f));
                UiKit.Width(slotLabel, 0, 1);
                ArtLibrary.Emblem(top, career.MyTeam.Name, 86);
                Line(info, $"<b>{me.Name}</b>" + (me.Number > 0 ? $"  <size=36>#{me.Number}</size>" : ""), 58, Color.white, 80);
                Line(info, $"{position} · {me.Age}세 · {where}", 30, new Color(1, 1, 1, 0.9f), 46);
                Line(info, $"{career.Year} 시즌 {career.Day + 1}일차 · 프로 {me.History.Count + 1}년차", 26, new Color(1, 1, 1, 0.75f), 40);
                Line(info, $"보유 {me.Money:N0}만원 · 인기 {me.Popularity}", 26, Gold, 40);
            }

            // The lower part: the main button, and a quiet delete for a filled slot.
            var lower = UiKit.Empty(card, "Lower");
            UiKit.Height(lower, 150);
            var buttons = UiKit.Row(lower, 16);
            buttons.padding = new RectOffset(28, 28, 18, 28);
            buttons.childForceExpandWidth = false;
            int slot = summary.Slot;
            var play = UiKit.Button(lower, career == null ? "새 선수 등록" : "이어하기", () => Play(slot), career == null ? UiKit.Accent : new Color(0.2f, 0.65f, 0.45f));
            play.GetComponentInChildren<Text>().fontSize = 40;
            UiKit.Width(play, 0, 1);
            UiAnim.Pulse(play, 1.04f, 0.6f);
            if (career != null)
            {
                var delete = UiKit.Button(lower, "삭제", () => ConfirmDelete(slot, career.Me.Name), new Color(0, 0, 0, 0.35f));
                delete.GetComponentInChildren<Text>().fontSize = 26;
                UiKit.Width(delete, 130);
            }
        }

        static void Line(Transform parent, string text, int size, Color color, float height) =>
            UiKit.Height(UiKit.Label(parent, text, size, TextAnchor.MiddleLeft, color), height);

        /// <summary>Every slot as a small card; tap one to bring it into focus above.</summary>
        void SlotStrip(System.Collections.Generic.List<SlotSummary> slots)
        {
            var strip = UiKit.Empty(_root, "Slots");
            UiKit.Height(strip, 160);
            UiKit.Row(strip, 14);
            foreach (var summary in slots)
            {
                var s = summary;
                bool selected = s.Slot == _selected;
                Color team = s.IsEmpty ? UiKit.Muted : UiKit.TeamColor(s.Career.MyTeam.Name);
                var button = UiKit.Button(strip, "", () => { _selected = s.Slot; Build(); }, selected ? UiKit.Shade(UiKit.Card, 1.6f) : UiKit.Card);
                var bar = UiKit.Panel(button.transform, "Team", s.IsEmpty ? UiKit.Background : team);
                bar.anchorMin = new Vector2(0, 1);
                bar.anchorMax = new Vector2(1, 1);
                bar.offsetMin = new Vector2(10, -16);
                bar.offsetMax = new Vector2(-10, -6);
                var label = button.GetComponentInChildren<Text>();
                label.fontSize = 28;
                label.text = s.IsEmpty ? $"<size=24><color=#BAC4D8>슬롯 {s.Slot}</color></size>\n+ 새 선수"
                    : $"<size=24><color=#BAC4D8>슬롯 {s.Slot}</color></size>\n<b>{s.Career.Me.Name}</b>\n<size=22>{s.Career.Me.Age}세 · {Short(s.Career.MyTeam.Name)}</size>";
                if (selected) button.gameObject.AddComponent<Outline>().effectColor = Gold;
            }
            if (!Premium.Owns(Premium.ExtraSlots))
            {
                var more = UiKit.Button(strip, $"+{Premium.ExtraSlotCount}\n<size=22>슬롯 추가</size>", () => PremiumScreen.Open(Build), new Color(0.55f, 0.4f, 0.8f, 0.6f));
                more.GetComponentInChildren<Text>().fontSize = 30;
                UiKit.Width(more, 150);
            }
        }

        /// <summary>"서울 블루윙스" → "블루윙스".</summary>
        static string Short(string team)
        {
            int space = team.IndexOf(' ');
            return space >= 0 && space + 1 < team.Length ? team.Substring(space + 1) : team;
        }

        /// <summary>Things that belong to me rather than to one player, as four even tiles.</summary>
        void Menu()
        {
            var daily = _club.Daily;
            int done = daily.Today.Count(m => m.Done);
            int claimable = daily.Today.Count(m => m.Done && !m.Claimed && m.Reward != RewardKind.Money);
            var grid = UiKit.Empty(_root, "Menu");
            UiKit.Height(grid, 400);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(482, 190);
            layout.spacing = new Vector2(20, 20);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperCenter;
            Tile(grid, "나만의 팀", $"은퇴 선수 {_club.Roster.Count}명", "팀", new Color(0.95f, 0.55f, 0.25f), ArtLibrary.Get("Icons/UI/records"),
                () => RecordsScreen.OpenMyTeam(_club, Build));
            Tile(grid, "도감", $"레전드 {_club.AllLegendsMet(null).Count}/{Legends.All.Count}", "도", new Color(0.35f, 0.65f, 1f), null,
                () => CollectionScreen.Open(null, _club));
            Tile(grid, "오늘의 미션", $"완료 {done}/{daily.Today.Count} · 출석 {daily.Streak}일", "M", new Color(0.3f, 0.8f, 0.5f), null,
                ShowMissions, claimable);
            Tile(grid, "상점", "유료 상품 (테스트)", "₩", new Color(0.7f, 0.5f, 1f), ArtLibrary.Get("Icons/UI/shop"),
                () => PremiumScreen.Open(Build));
        }

        /// <summary>A dark tile with a coloured icon disc, a title, a one-line status and an optional badge.</summary>
        void Tile(Transform parent, string title, string sub, string letter, Color color, Sprite icon, UnityEngine.Events.UnityAction onClick, int badge = 0)
        {
            var button = UiKit.Button(parent, "", onClick, UiKit.Card);
            var text = button.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 36;
            text.text = $"<b>{title}</b>\n<size=24><color=#BAC4D8>{sub}</color></size>";
            text.rectTransform.offsetMin = new Vector2(150, 0);

            var disc = UiKit.Panel(button.transform, "Disc", UiKit.Shade(color, 0.85f));
            disc.GetComponent<Image>().sprite = UiKit.CircleSprite;
            disc.GetComponent<Image>().type = Image.Type.Simple;
            disc.anchorMin = disc.anchorMax = new Vector2(0, 0.5f);
            disc.pivot = new Vector2(0, 0.5f);
            disc.anchoredPosition = new Vector2(26, 0);
            disc.sizeDelta = new Vector2(104, 104);
            if (icon != null)
            {
                var img = UiKit.Empty(disc, "Icon").gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                UiKit.Stretch(rt);
                rt.offsetMin = new Vector2(20, 20);
                rt.offsetMax = new Vector2(-20, -20);
            }
            else
            {
                var l = UiKit.Label(disc, letter, 46, TextAnchor.MiddleCenter);
                l.fontStyle = FontStyle.Bold;
                UiKit.Stretch(l.rectTransform);
            }

            if (badge > 0)
            {
                var dot = UiKit.Panel(button.transform, "Badge", new Color(0.9f, 0.25f, 0.3f));
                dot.GetComponent<Image>().sprite = UiKit.CircleSprite;
                dot.GetComponent<Image>().type = Image.Type.Simple;
                dot.anchorMin = dot.anchorMax = new Vector2(1, 1);
                dot.anchoredPosition = new Vector2(-30, -30);
                dot.sizeDelta = new Vector2(48, 48);
                var n = UiKit.Label(dot, badge.ToString(), 28, TextAnchor.MiddleCenter);
                n.fontStyle = FontStyle.Bold;
                UiKit.Stretch(n.rectTransform);
                UiAnim.Pulse(dot, 1.2f, 0.5f);
            }
        }

        static string Today => DailyMissions.DateOf(System.DateTime.Now);

        /// <summary>Today's missions from home. Rewards that are money need a player, so those wait for the growth screen.</summary>
        void ShowMissions()
        {
            CloseOverlay();
            _overlay = UiKit.Dim(_root.parent.parent);
            var card = UiKit.PopupCard(_overlay, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f));
            UiKit.Column(card, 40, 18);
            var heading = UiKit.Label(card, "오늘의 미션", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);
            var daily = _club.Daily;
            UiKit.Height(UiKit.Label(card, $"출석 {daily.Streak}일 연속 · 피로 회복권 {daily.RecoveryTickets}장\n선수를 골라 들어가면 출석 보상을 받아요. 돈 보상은 육성 화면에서 받아요.",
                26, TextAnchor.MiddleCenter, UiKit.Muted), 90);
            for (int i = 0; i < daily.Today.Count; i++)
            {
                int index = i;
                var m = daily.Today[i];
                bool money = m.Reward == RewardKind.Money;
                var row = UiKit.Panel(card, "Mission", UiKit.Shade(UiKit.Card, 1.3f));
                UiKit.Height(row, 140);
                var rowLayout = UiKit.Row(row, 16);
                rowLayout.padding = new RectOffset(24, 24, 14, 14);
                rowLayout.childForceExpandWidth = false;
                var text = UiKit.Label(row, $"<b>{m.Text}</b>\n<size=24><color=#BAC4D8>보상 {m.RewardText} · {m.Progress}/{m.Goal}</color></size>", 30);
                UiKit.Width(text, 0, 1);
                bool canClaim = m.Done && !m.Claimed && !money;
                var claim = UiKit.Button(row, m.Claimed ? "받음" : !m.Done ? "진행 중" : money ? "육성에서" : "받기", () =>
                {
                    if (!daily.Claim(index, _club, null)) return;
                    SaveStore.SaveClub(_club);
                    ShowMissions();
                }, canClaim ? new Color(0.2f, 0.75f, 0.45f) : UiKit.Background);
                claim.interactable = canClaim;
                claim.GetComponentInChildren<Text>().fontSize = 26;
                UiKit.Width(claim, 180);
            }
            UiKit.Height(UiKit.Empty(card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(card, "닫기", Build, UiKit.Muted), 110);
        }

        void Play(int slot)
        {
            GrowthScreen.Open(slot);
            Destroy(gameObject);
        }

        void ConfirmDelete(int slot, string playerName)
        {
            CloseOverlay();
            _overlay = UiKit.Dim(_root.parent.parent);
            var card = UiKit.PopupCard(_overlay, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.7f));
            UiKit.Column(card, 40, 20);
            var heading = UiKit.Label(card, $"슬롯 {slot} 지우기", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);
            UiKit.Height(UiKit.Label(card, $"{playerName} 선수의 진행이 사라져요. 되돌릴 수 없어요.\n이미 은퇴해서 내 팀에 들어온 선수와 도감은 그대로예요.", 30, TextAnchor.MiddleCenter), 0, 1);
            UiKit.Height(UiKit.Button(card, "지우기", () =>
            {
                SaveStore.DeleteSlot(slot);
                CloseOverlay();
                Build();
            }, UiKit.Warning), 110);
            UiKit.Height(UiKit.Button(card, "취소", CloseOverlay, UiKit.Muted), 110);
        }

        void CloseOverlay()
        {
            if (_overlay != null) Destroy(_overlay.gameObject);
            _overlay = null;
        }
    }
}
