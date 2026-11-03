using System;
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
    /// The scout center: two draw boxes. Training cards make one stat train faster for the player I'm
    /// raising; team gear makes my retired players' team stronger. Odds are always one tap away, a pity
    /// counter guarantees an SSR, and every draw can be paid with club coins earned by playing.
    /// Opened with a career, it also lets that player wear cards.
    /// </summary>
    public class GachaScreen : MonoBehaviour
    {
        static readonly Color CoinBrown = new Color(0.7f, 0.55f, 0.15f);
        static readonly Color TicketPurple = new Color(0.55f, 0.4f, 0.8f);
        static readonly string[] RarityColors = { "#BAC4D8", "#5FA8FF", "#C58CFF", "#FFD24D" };

        Club _club;
        CareerState _career;
        Action _onClose;
        IRandom _random;
        RectTransform _card;
        GachaBox _box;
        List<DrawResult> _last;
        bool _showOdds;
        int _pickSlot = -1;

        public static string Colored(Rarity r, string text) => $"<color={RarityColors[(int)r]}>{text}</color>";

        public static void Open(Club club, CareerState career = null, Action onClose = null, GachaBox box = GachaBox.Cards)
        {
            var screen = new GameObject("GachaScreen").AddComponent<GachaScreen>();
            screen._club = club;
            screen._career = career;
            screen._onClose = onClose;
            screen._box = box;
            screen._random = new SeededRandom(Environment.TickCount);
            var canvas = UiKit.Canvas(screen.transform, "GachaCanvas", 16);
            var dim = UiKit.Dim(canvas.transform);
            screen._card = UiKit.PopupCard(dim, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            UiKit.Column(screen._card, 32, 14);
            screen.Fill();
        }

        void Save()
        {
            if (_career != null) SaveStore.Save(_club, _career);
            else SaveStore.SaveClub(_club);
        }

        void Fill()
        {
            foreach (Transform child in _card)
            {
                var layout = child.GetComponent<LayoutElement>();
                if (layout == null || !layout.ignoreLayout) Destroy(child.gameObject);
            }

            var heading = UiKit.Label(_card, "스카우트 센터", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 70);

            var tabs = UiKit.Empty(_card, "Tabs");
            UiKit.Height(tabs, 90);
            UiKit.Row(tabs, 12);
            UiKit.Button(tabs, "훈련 카드", () => Switch(GachaBox.Cards), _box == GachaBox.Cards ? UiKit.Accent : UiKit.Card);
            UiKit.Button(tabs, "내 팀 장비", () => Switch(GachaBox.Gear), _box == GachaBox.Gear ? UiKit.Accent : UiKit.Card);

            string what = _box == GachaBox.Cards
                ? "장착한 선수가 그 능력치를 훈련할 때 경험치가 더 쌓여요 (+5~15%, 겹치면 레벨업)."
                : "은퇴 선수들로 만든 내 팀 전원의 능력치가 올라가요 (+1~5). 내 팀 리그·결투장에 그대로 들어가요.";
            UiKit.Height(UiKit.Label(_card,
                $"코인 <color=#FFD24D>{_club.Coins:N0}</color>  ·  뽑기권 {_club.DrawTickets}장  ·  SSR 골라 받기까지 {_club.DrawsToPity(_box)}회\n" +
                $"<size=24><color=#BAC4D8>{what}</color></size>", 30, TextAnchor.MiddleCenter), 110);

            var draws = UiKit.Empty(_card, "Draws");
            UiKit.Height(draws, 100);
            UiKit.Row(draws, 12);
            if (_club.DrawTickets > 0)
            {
                DrawButton(draws, $"뽑기권 1장", 1, true, _club.DrawTickets >= 1);
                DrawButton(draws, $"뽑기권 10장", 10, true, _club.DrawTickets >= 10);
            }
            DrawButton(draws, $"1회 ({Gacha.SingleCoins}코인)", 1, false, _club.Coins >= Gacha.SingleCoins);
            DrawButton(draws, $"10회 ({Gacha.TenCoins:N0}코인)", 10, false, _club.Coins >= Gacha.TenCoins);

            var body = UiKit.ScrollList(_card, 10);
            UiKit.Height(body.parent, 0, 1);

            if (_club.SsrPicks[(int)_box] > 0) PickBox(body);
            if (_showOdds) OddsBox(body);
            if (_last != null) ResultBox(body, _last);
            if (_box == GachaBox.Cards) CardsList(body);
            else GearList(body);

            var buttons = UiKit.Empty(_card, "Buttons");
            UiKit.Height(buttons, 96);
            UiKit.Row(buttons, 12);
            UiKit.Button(buttons, _showOdds ? "확률 닫기" : "확률 보기", () => { _showOdds = !_showOdds; Fill(); }, UiKit.Muted);
            UiKit.Button(buttons, "뽑기권 사기", () => PremiumScreen.Open(_club, () => { if (this != null) Fill(); }), TicketPurple);
            UiKit.Button(buttons, "닫기", Close, UiKit.Muted);
        }

        /// <summary>Pick tickets from the pity counter: one button per stat, its SSR and what it would do.</summary>
        void PickBox(RectTransform body)
        {
            UiKit.Height(UiKit.Label(body, $"<color=#FFD24D><b>SSR 선택권 {_club.SsrPicks[(int)_box]}장</b></color>  받고 싶은 SSR을 고르세요", 30, TextAnchor.MiddleLeft), 56);
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                string id = Gacha.IdOf(_box, stat, Rarity.SSR);
                int level = _club.LevelOf(_box, id);
                string name = _box == GachaBox.Cards ? Gacha.Card(id).Name : Gacha.GearPiece(id).Name;
                string have = level == 0 ? "없음" : $"Lv.{level}" + (_box == GachaBox.Gear && level < Gacha.GearMaxLevel(Rarity.SSR) ? $" ({_club.CopiesOf(id)}/{Gacha.GearCopiesFor(level + 1)}장)" : "");
                var pick = stat;
                var b = UiKit.Button(body, $"{name}  ·  지금 {have}", () =>
                {
                    var got = _club.UsePick(_box, pick);
                    if (got == null) return;
                    _last = new List<DrawResult> { got };
                    if (_career != null) _club.RefreshCards(_career);
                    GameAudio.Sfx(GameAudio.Achievement);
                    Save();
                    Fill();
                }, new Color(0.55f, 0.42f, 0.12f));
                UiKit.Height(b, 80);
            }
        }

        void Switch(GachaBox box)
        {
            _box = box;
            _last = null;
            _pickSlot = -1;
            Fill();
        }

        void DrawButton(RectTransform row, string label, int count, bool tickets, bool can)
        {
            var b = UiKit.Button(row, label, () => Draw(count, tickets), tickets ? TicketPurple : CoinBrown);
            b.interactable = can;
        }

        void Draw(int count, bool tickets)
        {
            var results = Gacha.Draw(_club, _box, count, _random, tickets);
            if (results == null) return;
            _last = results;
            if (_career != null) _club.RefreshCards(_career);
            GameAudio.Sfx(results.Any(r => r.Rarity == Rarity.SSR) ? GameAudio.Achievement : results.Any(r => r.Rarity == Rarity.SR) ? GameAudio.LevelUp : GameAudio.Coin);
            Save();
            Fill();
        }

        void OddsBox(RectTransform body)
        {
            var lines = new List<string> { "<b>확률 공개</b>  (두 뽑기 모두 같아요)" };
            foreach (var (rarity, percent, items) in Gacha.OddsTable(_box))
                lines.Add($"{Colored(rarity, rarity.ToString())}  {percent:0.0}%  ·  {items}종, 각 {percent / items:0.000}%");
            lines.Add($"<size=24><color=#BAC4D8>SSR 없이 {Gacha.Pity - 1}번 뽑으면 {Gacha.Pity}번째는 원하는 SSR을 골라 받는 선택권 (뽑기마다 따로 셈)" +
                      "\n10회 뽑기는 SR 이상 1장 이상 보장 · 같은 것이 나오면 레벨업, 최고 레벨 뒤엔 코인으로 돌려받아요" +
                      $"\n카드 최대 Lv.{Gacha.MaxLevel} · 장비는 같은 장비를 모아 강화: 레벨마다 +1, 강화할수록 필요 장수 증가(누적 1·2·4·7·11·16장), 최대 N+4 R+6 SR+8 SSR+10" +
                      "\n카드·장비는 능력치를 바로 올리지 않아요. 훈련 효율과 내 팀 전력에만 쓰여요.</color></size>");
            Box(body, string.Join("\n", lines), 360, TextAnchor.MiddleLeft);
        }

        void ResultBox(RectTransform body, List<DrawResult> results)
        {
            var lines = results.OrderByDescending(r => r.Rarity).Select(r =>
            {
                if (r.Pick) return $"{Colored(Rarity.SSR, "[SSR] 선택권")}  <color=#FFD24D>원하는 SSR을 골라요!</color>";
                string tag = r.Refund > 0 ? $"<color=#FFD24D>코인 +{r.Refund}</color>" : r.New ? "<color=#5FA8FF>NEW</color>"
                    : r.LevelUp || r.Box == GachaBox.Cards ? $"<color=#7CD992>강화! Lv.{r.Level}</color>" : $"Lv.{r.Level} (강화 재료 +1)";
                return $"{Colored(r.Rarity, $"[{r.Rarity}] {r.Name}")}  {tag}" + (r.FromPity ? "  <size=22>(확정)</size>" : "");
            });
            Box(body, string.Join("\n", lines), 60 + 44 * results.Count, TextAnchor.MiddleLeft);
        }

        void Box(RectTransform body, string text, float height, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var box = UiKit.Panel(body, "Box", UiKit.Shade(UiKit.Card, 1.5f));
            UiKit.Height(box, height);
            var label = UiKit.Label(box, text, 28, anchor);
            UiKit.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24, 12);
            label.rectTransform.offsetMax = new Vector2(-24, -12);
            UiAnim.PopIn(box);
        }

        // ---------- Cards ----------

        void CardsList(RectTransform body)
        {
            if (_career != null)
            {
                while (_career.Cards.Count < Gacha.CardSlots) _career.Cards.Add(null);
                UiKit.Height(UiKit.Label(body, $"{_career.Me.Name} 카드 슬롯", 28, TextAnchor.LowerLeft, UiKit.Muted), 50);
                for (int s = 0; s < Gacha.CardSlots; s++) SlotRow(body, s);
            }
            else
            {
                UiKit.Height(UiKit.Label(body, "카드 장착은 선수 육성 화면의 상점에서 해요.", 26, TextAnchor.MiddleCenter, UiKit.Muted), 60);
            }

            UiKit.Height(UiKit.Label(body, _pickSlot >= 0 ? $"슬롯 {_pickSlot + 1}에 넣을 카드를 고르세요" : $"가진 카드 {_club.CardLevels.Count}/{Gacha.Cards.Count}",
                28, TextAnchor.LowerLeft, _pickSlot >= 0 ? UiKit.Accent : UiKit.Muted), 50);
            var owned = Gacha.Cards.Where(c => _club.LevelOf(GachaBox.Cards, c.Id) > 0)
                .OrderByDescending(c => c.Rarity).ThenBy(c => c.Stat).ToList();
            if (owned.Count == 0)
                UiKit.Height(UiKit.Label(body, "아직 카드가 없어요. 위에서 뽑아 보세요.", 26, TextAnchor.MiddleCenter, UiKit.Muted), 80);
            foreach (var c in owned) CardRow(body, c);
        }

        void SlotRow(RectTransform body, int slot)
        {
            var id = _career.Cards[slot];
            var c = id == null ? null : Gacha.Card(id);
            var row = UiKit.Panel(body, "Slot", UiKit.Shade(UiKit.Card, slot == _pickSlot ? 1.8f : 1.3f));
            UiKit.Height(row, 110);
            var layout = UiKit.Row(row, 12);
            layout.padding = new RectOffset(24, 16, 10, 10);
            layout.childForceExpandWidth = false;
            string text = c == null ? $"슬롯 {slot + 1}  <color=#BAC4D8>비어 있음</color>"
                : $"슬롯 {slot + 1}  {Colored(c.Rarity, c.Name)}  <size=24>Lv.{_club.LevelOf(GachaBox.Cards, id)} · {Stats.Name(c.Stat)} 훈련 +{Gacha.CardBonus(c.Rarity, _club.LevelOf(GachaBox.Cards, id))}%</size>";
            UiKit.Width(UiKit.Label(row, text, 28), 0, 1);
            UiKit.Width(UiKit.Button(row, slot == _pickSlot ? "취소" : "바꾸기", () => { _pickSlot = slot == _pickSlot ? -1 : slot; Fill(); }, UiKit.Accent), 150);
            if (c != null)
                UiKit.Width(UiKit.Button(row, "빼기", () => { _club.EquipCard(_career, slot, null); Save(); Fill(); }, UiKit.Muted), 120);
        }

        void CardRow(RectTransform body, TrainingCard c)
        {
            int level = _club.LevelOf(GachaBox.Cards, c.Id);
            bool worn = _career != null && _career.Cards.Contains(c.Id);
            var row = UiKit.Panel(body, "Card", UiKit.Shade(UiKit.Card, 1.15f));
            UiKit.Height(row, 96);
            var layout = UiKit.Row(row, 12);
            layout.padding = new RectOffset(24, 16, 8, 8);
            layout.childForceExpandWidth = false;
            string useful = _career != null && !_career.Me.MyStats.Contains(c.Stat) ? "  <color=#F28C40>(이 선수는 안 쓰는 능력치)</color>" : "";
            UiKit.Width(UiKit.Label(row,
                $"{Colored(c.Rarity, $"[{c.Rarity}] {c.Name}")}  Lv.{level}\n<size=24><color=#BAC4D8>{Stats.Name(c.Stat)} 훈련 +{Gacha.CardBonus(c.Rarity, level)}%</color>{useful}</size>", 28), 0, 1);
            if (_pickSlot >= 0 && !worn)
            {
                int slot = _pickSlot;
                UiKit.Width(UiKit.Button(row, "넣기", () => { _club.EquipCard(_career, slot, c.Id); _pickSlot = -1; Save(); Fill(); }, UiKit.Accent), 130);
            }
            else if (worn) UiKit.Width(UiKit.Label(row, "장착 중", 26, TextAnchor.MiddleCenter, UiKit.Accent), 130);
        }

        // ---------- Gear ----------

        void GearList(RectTransform body)
        {
            var worn = _club.TeamGearWorn();
            UiKit.Height(UiKit.Label(body, "내 팀이 쓰는 장비 (능력치마다 가장 좋은 것이 자동으로)", 28, TextAnchor.LowerLeft, UiKit.Muted), 50);
            if (_club.Roster.Count > 0)
                UiKit.Height(UiKit.Label(body, $"내 팀 전력 {Arena.MyPower(_club)}", 26, TextAnchor.MiddleLeft, UiKit.Muted), 44);
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                bool has = worn.TryGetValue(stat, out var w);
                string text = has
                    ? $"{Stats.Name(stat)}  {Colored(w.Gear.Rarity, w.Gear.Name)}  Lv.{w.Level}/{Gacha.GearMaxLevel(w.Gear.Rarity)}  <color=#5FA8FF>+{w.Bonus}</color>"
                    : $"{Stats.Name(stat)}  <color=#BAC4D8>없음</color>";
                var row = UiKit.Panel(body, "Gear", UiKit.Shade(UiKit.Card, 1.15f));
                UiKit.Height(row, 90);
                var layout = UiKit.Row(row, 12);
                layout.padding = new RectOffset(24, 16, 8, 8);
                layout.childForceExpandWidth = false;
                UiKit.Width(UiKit.Label(row, text, 28), 0, 1);
                if (!has) continue;
                int copies = _club.CopiesOf(w.Gear.Id);
                string next = w.Level >= Gacha.GearMaxLevel(w.Gear.Rarity) ? "최고"
                    : $"다음 강화\n{copies}/{Gacha.GearCopiesFor(w.Level + 1)}장";
                UiKit.Width(UiKit.Label(row, $"<size=24>{next}</size>", 24, TextAnchor.MiddleCenter, UiKit.Muted), 160);
            }
            UiKit.Height(UiKit.Label(body, $"모은 장비 {_club.GearCopies.Count}/{Gacha.Gear.Count}", 26, TextAnchor.MiddleLeft, UiKit.Muted), 50);
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
