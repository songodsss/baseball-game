using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// The first screen: the game's name, the save slots (one player each, all sharing my team),
    /// and the way into the collection and the paid product tests.
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
            UiKit.Backdrop(_root, ArtLibrary.Background("title"), 0.45f);
            UiKit.Column(_root, 48, 22);
            Build();
        }

        void Build()
        {
            CloseOverlay();
            foreach (Transform child in _root) Destroy(child.gameObject);
            _club = SaveStore.LoadClub(_random);
            if (_club.Daily.Refresh(Today)) SaveStore.SaveClub(_club);

            BuildLogo();

            int legends = _club.AllLegendsMet(null).Count;
            string club = _club.Roster.Count == 0
                ? "첫 선수를 키워 보세요. 은퇴한 선수는 모든 슬롯이 함께 쓰는 나만의 팀에 들어와요."
                : $"나만의 팀 {_club.Roster.Count}명  ·  명예 포인트 {_club.LegacyPoints:N0}  ·  만난 레전드 {legends}/{Legends.All.Count}";
            UiKit.Height(UiKit.Label(_root, club, 28, TextAnchor.MiddleCenter, UiKit.Muted), 60);

            UiKit.Height(UiKit.Label(_root, "내 선수", 34, TextAnchor.LowerLeft, Gold), 56);
            for (int slot = 1; slot <= SaveStore.SlotCount; slot++) SlotCard(SaveStore.Peek(slot, _club, _random));
            if (!Premium.Owns(Premium.ExtraSlots))
            {
                var more = UiKit.Button(_root, $"+ 슬롯 {Premium.ExtraSlotCount}개 더 (유료 테스트)", () => PremiumScreen.Open(Build), UiKit.Shade(UiKit.Card, 1.2f));
                more.GetComponentInChildren<Text>().fontSize = 26;
                UiKit.Height(more, 80);
            }

            // The hub: things that belong to me rather than to one player.
            var daily = _club.Daily;
            int done = daily.Today.Count(m => m.Done);
            var grid = UiKit.Empty(_root, "Hub");
            UiKit.Height(grid, 330);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(486, 155);
            layout.spacing = new Vector2(20, 20);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.MiddleCenter;
            HubButton(grid, "나만의 팀", $"은퇴 선수 {_club.Roster.Count}명 · 도전 경기", ArtLibrary.Get("Icons/UI/records"),
                new Color(0.85f, 0.45f, 0.2f), () => RecordsScreen.OpenMyTeam(_club, Build));
            HubButton(grid, "도감", "레전드 · 라이벌 · 트로피", null, UiKit.Accent, () => CollectionScreen.Open(null, _club));
            HubButton(grid, "오늘의 미션", $"완료 {done}/{daily.Today.Count} · 출석 {daily.Streak}일 연속", null,
                new Color(0.2f, 0.6f, 0.45f), ShowMissions);
            HubButton(grid, "상점", "유료 상품 (테스트)", ArtLibrary.Get("Icons/UI/shop"), new Color(0.55f, 0.4f, 0.8f), () => PremiumScreen.Open(Build));

            UiKit.Height(UiKit.Empty(_root, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Label(_root, "프로토타입 0.1", 22, TextAnchor.MiddleCenter, UiKit.Muted), 36);
        }

        static string Today => DailyMissions.DateOf(System.DateTime.Now);

        void HubButton(Transform parent, string title, string sub, Sprite icon, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var button = UiKit.Button(parent, $"{title}\n<size=24><color=#E8ECF4>{sub}</color></size>", onClick, color);
            button.GetComponentInChildren<Text>().fontSize = 38;
            UiKit.ButtonIcon(button, icon, 70);
        }

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
                var text = UiKit.Label(row, $"<b>{m.Text}</b>\n<size=24><color=#9EABC2>보상 {m.RewardText} · {m.Progress}/{m.Goal}</color></size>", 30);
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

        void BuildLogo()
        {
            var logo = ArtLibrary.Get("UI/logo");
            if (logo != null)
            {
                ArtLibrary.Banner(_root, logo, 300).preserveAspect = true;
                return;
            }
            // Until there's a logo picture: a ball, the name and a tagline.
            var top = UiKit.Empty(_root, "Logo");
            UiKit.Height(top, 300);
            UiKit.Column(top, 0, 8).childAlignment = TextAnchor.MiddleCenter;
            var ball = UiKit.Empty(top, "Ball");
            UiKit.Height(ball, 120);
            var disc = UiKit.Panel(ball, "Disc", Color.white);
            disc.GetComponent<Image>().sprite = UiKit.CircleSprite;
            disc.GetComponent<Image>().type = Image.Type.Simple;
            disc.anchorMin = disc.anchorMax = new Vector2(0.5f, 0.5f);
            disc.sizeDelta = new Vector2(110, 110);
            var seams = UiKit.Label(disc, "(   )", 64, TextAnchor.MiddleCenter, new Color(0.85f, 0.2f, 0.25f));
            UiKit.Stretch(seams.rectTransform);
            UiAnim.PopIn(disc, 0.4f);
            var title = UiKit.Label(top, GameTitle, 84, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.25f, 0.6f, 0.9f);
            UiKit.Height(title, 110);
            UiKit.Height(UiKit.Label(top, "신인부터 레전드까지, 한 선수의 야구 인생", 32, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.3f)), 60);
        }

        void SlotCard(SlotSummary summary)
        {
            var career = summary.Career;
            Color team = career == null ? UiKit.Muted : UiKit.TeamColor(career.MyTeam.Name);
            var card = UiKit.Panel(_root, $"Slot {summary.Slot}", UiKit.Card);
            UiKit.Height(card, 170);
            var row = UiKit.Row(card, 18);
            row.padding = new RectOffset(0, 24, 0, 0);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;

            // A strip in the club colour down the left edge, with the slot number.
            var strip = UiKit.Panel(card, "Strip", career == null ? UiKit.Background : UiKit.Shade(team, 0.8f));
            UiKit.Width(strip, 110);
            var number = UiKit.Label(strip, summary.Slot.ToString(), 56, TextAnchor.MiddleCenter);
            number.fontStyle = FontStyle.Bold;
            UiKit.Stretch(number.rectTransform);

            string text;
            if (career == null) text = $"<b>빈 슬롯</b>\n<size=26><color=#9EABC2>새 선수를 등록해서 시작해요</color></size>";
            else
            {
                var me = career.Me;
                string position = me.IsPitcher ? $"{Positions.RoleName(me.Role)} 투수" : Positions.Name(me.Position);
                string where = career.InMilitary ? "군 복무 중" : career.InFarm ? "2군" : career.MyTeam.Name;
                text = $"<b>{me.Name}</b>  <size=28>{position} · {me.Age}세</size>\n" +
                       $"<size=26><color=#9EABC2>{where} · {career.Year} 시즌 {career.Day + 1}일차 · {me.History.Count + 1}년차</color></size>";
            }
            var label = UiKit.Label(card, text, 38);
            UiKit.Width(label, 0, 1);

            int slot = summary.Slot;
            var play = UiKit.Button(card, career == null ? "새로 시작" : "이어하기", () => Play(slot), career == null ? UiKit.Accent : new Color(0.2f, 0.6f, 0.45f));
            play.GetComponentInChildren<Text>().fontSize = 30;
            UiKit.Width(play, 210);
            UiKit.Height(play, 110);
            if (career != null)
            {
                var delete = UiKit.Button(card, "삭제", () => ConfirmDelete(slot, career.Me.Name), UiKit.Background);
                delete.GetComponentInChildren<Text>().fontSize = 26;
                UiKit.Width(delete, 110);
                UiKit.Height(delete, 110);
            }
            UiAnim.FadeIn(card, 0.15f + 0.07f * slot);
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
