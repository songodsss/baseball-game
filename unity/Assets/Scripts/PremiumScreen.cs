using System;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Test buttons for the paid product candidates. Purchases go through the test store, so
    /// nothing is charged; the reset button forgets them to try again.
    /// </summary>
    public class PremiumScreen : MonoBehaviour
    {
        static readonly Color Purple = new Color(0.55f, 0.4f, 0.8f);

        Action _onClose;
        RectTransform _card;

        public static void Open(Action onClose = null)
        {
            var screen = new GameObject("PremiumScreen").AddComponent<PremiumScreen>();
            screen._onClose = onClose;
            screen.Build();
        }

        void Build()
        {
            var canvas = UiKit.Canvas(transform, "PremiumCanvas", 20);
            var dim = UiKit.Dim(canvas.transform);
            _card = UiKit.PopupCard(dim, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
            UiKit.Column(_card, 40, 20);
            Fill();
        }

        void Fill()
        {
            foreach (Transform child in _card)
            {
                var layout = child.GetComponent<LayoutElement>();
                if (layout == null || !layout.ignoreLayout) Destroy(child.gameObject); // keep the card's accent strip
            }

            var heading = UiKit.Label(_card, "유료 상품 (테스트)", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);
            UiKit.Height(UiKit.Label(_card, "출시 후보 상품이에요. 능력치·성장·연봉에는 영향이 없어요.\n지금은 테스트라 실제로 결제되지 않아요.", 28, TextAnchor.MiddleCenter, UiKit.Muted), 100);

            foreach (var product in Premium.All)
            {
                var p = product;
                bool owned = Premium.Owns(p.Id);
                var row = UiKit.Panel(_card, p.Name, UiKit.Shade(UiKit.Card, 1.3f));
                UiKit.Height(row, 190);
                var layout = UiKit.Row(row, 16);
                layout.padding = new RectOffset(24, 24, 16, 16);
                layout.childForceExpandWidth = false;
                var text = UiKit.Label(row, $"<b>{p.Name}</b>\n<size=26><color=#9EABC2>{p.Description}</color></size>", 34);
                UiKit.Width(text, 0, 1);
                var buy = UiKit.Button(row, owned ? "보유 중" : Monetization.Store.PriceOf(p.Id), () =>
                {
                    Premium.Buy(p.Id, _ => { if (this != null) Fill(); });
                }, owned ? UiKit.Background : Purple);
                buy.interactable = !owned;
                UiKit.Width(buy, 220);
            }

            UiKit.Height(UiKit.Empty(_card, "Spacer"), 0, 1);
            UiKit.Height(UiKit.Button(_card, "구매 기록 초기화 (테스트)", () => { Premium.ResetPurchases(); Fill(); }, UiKit.Background), 100);
            UiKit.Height(UiKit.Button(_card, "닫기", Close, UiKit.Muted), 110);
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
