using System;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// The club store: paid conveniences, cosmetics for club coins or money, and the wardrobe to
    /// wear them. Purchases go through the test store for now, so nothing is charged.
    /// Nothing here makes a player or a team stronger.
    /// </summary>
    public class PremiumScreen : MonoBehaviour
    {
        public enum Tab { Convenience, Cosmetics, Wardrobe }

        static readonly Color Purple = new Color(0.55f, 0.4f, 0.8f);
        static readonly Color CoinGold = new Color(1f, 0.82f, 0.3f);

        Action _onClose;
        Club _club;
        Tab _tab;
        RectTransform _card;

        /// <summary>Opens the store on the club given; changes are saved to it as they happen.</summary>
        public static void Open(Club club, Action onClose = null, Tab tab = Tab.Convenience)
        {
            var screen = new GameObject("PremiumScreen").AddComponent<PremiumScreen>();
            screen._club = club;
            screen._onClose = onClose;
            screen._tab = tab;
            screen.Build();
        }

        void Build()
        {
            var canvas = UiKit.Canvas(transform, "PremiumCanvas", 20);
            var dim = UiKit.Dim(canvas.transform);
            _card = UiKit.PopupCard(dim, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f));
            UiKit.Column(_card, 36, 16);
            Fill();
        }

        void Save() => SaveStore.SaveClub(_club);

        void Fill()
        {
            foreach (Transform child in _card)
            {
                var layout = child.GetComponent<LayoutElement>();
                if (layout == null || !layout.ignoreLayout) Destroy(child.gameObject); // keep the card's accent strip
            }

            var heading = UiKit.Label(_card, "구단 스토어", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 80);
            UiKit.Height(UiKit.Label(_card,
                $"<color=#FFD24D>구단 코인 {_club.Coins:N0}</color>   ·   회복권 {_club.Daily.RecoveryTickets}장   ·   되감기권 {_club.RewindTickets}장\n" +
                "<size=24>능력치·성장 한계·연봉·장비는 팔지 않아요. 지금은 테스트라 결제되지 않아요.</size>",
                28, TextAnchor.MiddleCenter, UiKit.Muted), 90);

            var tabs = UiKit.Empty(_card, "Tabs");
            UiKit.Height(tabs, 96);
            UiKit.Row(tabs, 12);
            TabButton(tabs, Tab.Convenience, "편의");
            TabButton(tabs, Tab.Cosmetics, "꾸미기");
            TabButton(tabs, Tab.Wardrobe, "내 꾸미기");

            var list = UiKit.ScrollList(_card, 14);
            UiKit.Height(list.parent, 0, 1);
            switch (_tab)
            {
                case Tab.Convenience: FillConvenience(list); break;
                case Tab.Cosmetics: FillCosmetics(list); break;
                default: FillWardrobe(list); break;
            }

            var bottom = UiKit.Empty(_card, "Bottom");
            UiKit.Height(bottom, 100);
            UiKit.Row(bottom, 12);
            UiKit.Button(bottom, "구매 기록 초기화 (테스트)", () => { Premium.ResetPurchases(); Fill(); }, UiKit.Background);
            UiKit.Button(bottom, "닫기", Close, UiKit.Muted);
        }

        void TabButton(RectTransform parent, Tab tab, string label)
        {
            UiKit.Button(parent, label, () => { _tab = tab; Fill(); }, _tab == tab ? UiKit.Accent : UiKit.Card);
        }

        RectTransform ItemRow(RectTransform list, string title, string detail, float height = 190)
        {
            var row = UiKit.Panel(list, title, UiKit.Shade(UiKit.Card, 1.3f));
            UiKit.Height(row, height);
            var layout = UiKit.Row(row, 14);
            layout.padding = new RectOffset(24, 20, 14, 14);
            layout.childForceExpandWidth = false;
            var text = UiKit.Label(row, $"<b>{title}</b>\n<size=24><color=#BAC4D8>{detail}</color></size>", 32);
            UiKit.Width(text, 0, 1);
            return row;
        }

        void Section(RectTransform list, string title) =>
            UiKit.Height(UiKit.Label(list, title, 30, TextAnchor.LowerLeft, CoinGold), 64);

        void FillConvenience(RectTransform list)
        {
            void Product(PremiumProduct p)
            {
                if (p.Id == Premium.StarterPack && !Premium.StarterPackAvailable) return;
                bool owned = p.Kind == ProductKind.Permanent ? Premium.Owns(p.Id) : !Premium.CanBuy(p.Id, _club);
                var row = ItemRow(list, p.Name, $"{p.Description}\n쓰는 곳: {p.Where}");
                var buy = UiKit.Button(row, owned ? "보유 중" : Monetization.Store.PriceOf(p.Id), () =>
                    Premium.Buy(p.Id, _club, bought => { if (bought) Save(); if (this != null) Fill(); }), owned ? UiKit.Background : Purple);
                buy.interactable = !owned;
                UiKit.Width(buy, 210);
            }

            Section(list, "한 번 사면 계속");
            foreach (var p in Premium.All.Where(p => p.Kind == ProductKind.Permanent)) Product(p);

            Section(list, "쓸 때마다");
            foreach (var p in Premium.All.Where(p => p.Kind == ProductKind.Consumable)) Product(p);
            // The same tickets, slowly, for coins.
            var coinRow = ItemRow(list, "피로 회복권 1장 (코인)",
                $"코인으로도 살 수 있어요. 하루 {DailyMissions.MaxTicketsPerDay}장까지 쓸 수 있어요.\n쓰는 곳: 트레이닝 센터");
            var coinBuy = UiKit.Button(coinRow, $"{Club.TicketCoins}코인", () =>
            {
                if (_club.BuyTickets(1)) { Save(); Fill(); }
            }, new Color(0.7f, 0.55f, 0.15f));
            coinBuy.interactable = _club.Coins >= Club.TicketCoins;
            UiKit.Width(coinBuy, 210);

            Section(list, "응원하기");
            foreach (var p in Premium.All.Where(p => p.Kind == ProductKind.Support)) Product(p);

            UiKit.Height(UiKit.Label(list,
                "구단 코인은 미션·주간 도전·시즌 패스·출석·은퇴식으로 모아요. 현금으로는 팔지 않아요.",
                24, TextAnchor.MiddleCenter, UiKit.Muted), 100);
        }

        void FillCosmetics(RectTransform list)
        {
            UiKit.Height(UiKit.Label(list, "내 팀 리그·결투장에서 상대도 보는 꾸미기예요. 코인으로 천천히, 또는 바로 살 수 있어요.",
                24, TextAnchor.MiddleCenter, UiKit.Muted), 70);
            foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind)))
            {
                Section(list, Cosmetics.KindName(kind));
                foreach (var c in Cosmetics.All.Where(c => c.Kind == kind && c.Id != Cosmetics.SupporterFrame))
                {
                    var cosmetic = c;
                    bool owned = _club.Owns(c.Id);
                    var row = ItemRow(list, c.Name, c.Description, 170);
                    if (owned || c.PassOnly)
                    {
                        var label = UiKit.Button(row, owned ? "보유 중" : "패스 보상", () => { }, UiKit.Background);
                        label.interactable = false;
                        UiKit.Width(label, 300);
                        continue;
                    }
                    var coins = UiKit.Button(row, $"{c.Coins:N0}코인", () =>
                    {
                        if (_club.BuyCosmetic(cosmetic.Id)) { Save(); Fill(); }
                    }, new Color(0.7f, 0.55f, 0.15f));
                    coins.interactable = _club.Coins >= c.Coins;
                    UiKit.Width(coins, 170);
                    var money = UiKit.Button(row, Premium.PriceText(c.Won), () =>
                        Premium.BuyCosmetic(cosmetic.Id, _club, bought => { if (bought) Save(); if (this != null) Fill(); }), Purple);
                    UiKit.Width(money, 150);
                }
            }
        }

        void FillWardrobe(RectTransform list)
        {
            if (_club.OwnedCosmetics.Count == 0)
            {
                UiKit.Height(UiKit.Label(list, "아직 가진 꾸미기가 없어요.\n꾸미기 탭에서 코인으로 사 보세요.", 30, TextAnchor.MiddleCenter, UiKit.Muted), 200);
                return;
            }
            foreach (CosmeticKind kind in Enum.GetValues(typeof(CosmeticKind)))
            {
                var owned = Cosmetics.All.Where(c => c.Kind == kind && _club.Owns(c.Id)).ToList();
                if (owned.Count == 0) continue;
                Section(list, Cosmetics.KindName(kind));
                foreach (var c in owned)
                {
                    var cosmetic = c;
                    bool worn = _club.WornId(kind) == c.Id;
                    var row = ItemRow(list, c.Name, c.Description, 150);
                    var wear = UiKit.Button(row, worn ? "착용 중" : "착용", () =>
                    {
                        if (_club.Wear(cosmetic.Id)) { Save(); Fill(); }
                    }, worn ? UiKit.Accent : UiKit.Card);
                    UiKit.Width(wear, 200);
                }
            }
            UiKit.Height(UiKit.Label(list, "착용 중을 다시 누르면 벗어요. 구장·연출은 내 팀 리그와 결투장이 열리면 보여요.",
                24, TextAnchor.MiddleCenter, UiKit.Muted), 80);
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
