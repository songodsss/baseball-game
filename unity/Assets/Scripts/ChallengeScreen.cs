using System;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>
    /// The clubhouse: today's missions, this week's challenges and the season pass. Every slot
    /// shares them. Money rewards need a player, so from home they wait for the growth screen.
    /// </summary>
    public class ChallengeScreen : MonoBehaviour
    {
        public enum Tab { Today, Week, Pass }

        static readonly Color Green = new Color(0.2f, 0.75f, 0.45f);
        static readonly Color Purple = new Color(0.55f, 0.4f, 0.8f);
        static readonly Color CoinGold = new Color(1f, 0.82f, 0.3f);

        Club _club;
        CareerState _career;
        Action _onClose;
        Tab _tab;
        RectTransform _card;

        static string Today => DailyMissions.DateOf(DateTime.Now);

        /// <summary>Opens the clubhouse; career is null from home. Changes are saved to the club as they happen.</summary>
        public static void Open(Club club, CareerState career, Action onClose = null, Tab tab = Tab.Today)
        {
            var screen = new GameObject("ChallengeScreen").AddComponent<ChallengeScreen>();
            screen._club = club;
            screen._career = career;
            screen._onClose = onClose;
            screen._tab = tab;
            screen.Build();
        }

        /// <summary>Red-dot count for the clubhouse: rewards ready on any tab.</summary>
        public static int Ready(Club club, bool hasPlayer) =>
            club.Daily.Today.Count(m => m.Done && !m.Claimed && (hasPlayer || m.Reward != RewardKind.Money)) +
            club.Daily.UnclaimedWeekly + club.Daily.Pass.Unclaimed;

        void Build()
        {
            _club.Daily.Refresh(Today);
            var canvas = UiKit.Canvas(transform, "ChallengeCanvas", 15);
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
                if (layout == null || !layout.ignoreLayout) Destroy(child.gameObject);
            }
            var daily = _club.Daily;

            var heading = UiKit.Label(_card, "클럽하우스", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 80);
            UiKit.Height(UiKit.Label(_card,
                $"<color=#FFD24D>구단 코인 {_club.Coins:N0}</color>   ·   회복권 {daily.RecoveryTickets}장   ·   출석 {daily.Streak}일 연속",
                28, TextAnchor.MiddleCenter, UiKit.Muted), 60);

            var tabs = UiKit.Empty(_card, "Tabs");
            UiKit.Height(tabs, 96);
            UiKit.Row(tabs, 12);
            int today = daily.Today.Count(m => m.Done && !m.Claimed && (_career != null || m.Reward != RewardKind.Money));
            TabButton(tabs, Tab.Today, "오늘", today);
            TabButton(tabs, Tab.Week, "이번 주", daily.UnclaimedWeekly);
            TabButton(tabs, Tab.Pass, "시즌 패스", daily.Pass.Unclaimed);

            var list = UiKit.ScrollList(_card, 14);
            UiKit.Height(list.parent, 0, 1);
            switch (_tab)
            {
                case Tab.Today: FillToday(list); break;
                case Tab.Week: FillWeek(list); break;
                default: FillPass(list); break;
            }
            UiKit.Height(UiKit.Button(_card, "닫기", Close, UiKit.Muted), 100);
        }

        void TabButton(RectTransform parent, Tab tab, string label, int ready)
        {
            string text = ready > 0 ? $"{label} <color=#FFD24D>●{ready}</color>" : label;
            UiKit.Button(parent, text, () => { _tab = tab; Fill(); }, _tab == tab ? UiKit.Accent : UiKit.Card);
        }

        void MissionRow(RectTransform list, Mission m, bool canClaim, string waitLabel, Func<bool> claim)
        {
            var row = UiKit.Panel(list, "Mission", UiKit.Shade(UiKit.Card, 1.3f));
            UiKit.Height(row, 170);
            var layout = UiKit.Row(row, 16);
            layout.padding = new RectOffset(24, 24, 14, 14);
            layout.childForceExpandWidth = false;
            var info = UiKit.Empty(row, "Info");
            UiKit.Width(info, 0, 1);
            UiKit.Column(info, 0, 6);
            UiKit.Height(UiKit.Label(info, $"<b>{m.Text}</b>", 30), 50);
            UiKit.Height(UiKit.Label(info, $"보상 {m.RewardText}  ·  {m.Progress}/{m.Goal}", 24, TextAnchor.MiddleLeft, UiKit.Muted), 40);
            var bar = UiKit.Bar(info, m.Done ? Green : UiKit.Accent);
            UiKit.Height(bar.parent, 20);
            UiKit.SetBar(bar, (float)m.Progress / m.Goal);
            var button = UiKit.Button(row, m.Claimed ? "받음" : !m.Done ? "진행 중" : canClaim ? "받기" : waitLabel, () =>
            {
                if (!claim()) return;
                Save();
                Fill();
            }, m.Done && !m.Claimed && canClaim ? Green : UiKit.Background);
            button.interactable = m.Done && !m.Claimed && canClaim;
            UiKit.Width(button, 190);
        }

        void FillToday(RectTransform list)
        {
            var daily = _club.Daily;
            UiKit.Height(UiKit.Label(list,
                $"매일 자정에 새 미션 3개. 하나 받을 때마다 코인 {Club.MissionCoins} + 패스 점수 {SeasonPass.MissionPoints}." +
                (_career == null ? "\n돈 보상은 선수를 골라 육성 화면에서 받아요." : ""),
                24, TextAnchor.MiddleCenter, UiKit.Muted), 80);
            for (int i = 0; i < daily.Today.Count; i++)
            {
                int index = i;
                var m = daily.Today[i];
                bool canClaim = _career != null || m.Reward != RewardKind.Money;
                MissionRow(list, m, canClaim, "육성에서", () => daily.Claim(index, _club, _career));
            }
        }

        void FillWeek(RectTransform list)
        {
            var daily = _club.Daily;
            UiKit.Height(UiKit.Label(list,
                $"월요일마다 새 도전 {DailyMissions.PerWeek}개. 하나 깰 때마다 패스 점수 {SeasonPass.WeeklyPoints}.",
                24, TextAnchor.MiddleCenter, UiKit.Muted), 60);
            for (int i = 0; i < daily.Week.Count; i++)
            {
                int index = i;
                MissionRow(list, daily.Week[i], true, "", () => daily.ClaimWeekly(index, _club, _career));
            }
        }

        void FillPass(RectTransform list)
        {
            var pass = _club.Daily.Pass;
            int intoLevel = pass.Level >= SeasonPass.Levels ? SeasonPass.PointsPerLevel : pass.Points % SeasonPass.PointsPerLevel;
            UiKit.Height(UiKit.Label(list,
                $"<b>레벨 {pass.Level} / {SeasonPass.Levels}</b>   ·   {SeasonPass.LastDayOf(pass.SeasonId)}까지\n" +
                "<size=24><color=#BAC4D8>미션·출석·주간 도전으로 점수를 모아요. 꾸미기·코인·회복권뿐, 선수가 세지는 보상은 없어요.</color></size>",
                30, TextAnchor.MiddleCenter), 100);
            var bar = UiKit.Bar(list, CoinGold);
            UiKit.Height(bar.parent, 24);
            UiKit.SetBar(bar, intoLevel / (float)SeasonPass.PointsPerLevel);

            if (!pass.Premium)
            {
                var buyRow = UiKit.Panel(list, "Buy", UiKit.Shade(Purple, 0.6f));
                UiKit.Height(buyRow, 150);
                var layout = UiKit.Row(buyRow, 14);
                layout.padding = new RectOffset(24, 20, 14, 14);
                layout.childForceExpandWidth = false;
                var text = UiKit.Label(buyRow, "<b>유료 보상 줄 열기</b>\n<size=24>지난 레벨 보상도 바로 받을 수 있어요. 패스 전용 꾸미기 3종.</size>", 30);
                UiKit.Width(text, 0, 1);
                var buy = UiKit.Button(buyRow, Monetization.Store.PriceOf(Premium.SeasonPassId), () =>
                    Premium.Buy(Premium.SeasonPassId, _club, bought => { if (bought) Save(); if (this != null) Fill(); }), Purple);
                UiKit.Width(buy, 200);
            }

            var header = UiKit.Empty(list, "Header");
            UiKit.Height(header, 50);
            UiKit.Row(header, 12);
            UiKit.Width(UiKit.Label(header, "레벨", 24, TextAnchor.MiddleCenter, UiKit.Muted), 110);
            UiKit.Width(UiKit.Label(header, "무료", 24, TextAnchor.MiddleCenter, UiKit.Muted), 0, 1);
            UiKit.Width(UiKit.Label(header, "유료", 24, TextAnchor.MiddleCenter, CoinGold), 0, 1);

            for (int level = 1; level <= SeasonPass.Levels; level++)
            {
                int lv = level;
                bool reached = level <= pass.Level;
                var row = UiKit.Panel(list, "Level", reached ? UiKit.Shade(UiKit.Card, 1.4f) : UiKit.Shade(UiKit.Card, 0.9f));
                UiKit.Height(row, 110);
                var layout = UiKit.Row(row, 12);
                layout.padding = new RectOffset(12, 12, 10, 10);
                UiKit.Width(UiKit.Label(row, $"<b>{level}</b>", 34, TextAnchor.MiddleCenter, reached ? Color.white : UiKit.Muted), 110);
                RewardButton(row, SeasonPass.FreeReward(level), pass.ClaimedFree.Contains(level), pass.CanClaim(level, false), () => pass.Claim(lv, false, _club));
                RewardButton(row, SeasonPass.PremiumReward(level), pass.ClaimedPremium.Contains(level), pass.CanClaim(level, true), () => pass.Claim(lv, true, _club),
                    locked: !pass.Premium);
            }
        }

        void RewardButton(RectTransform row, PassReward reward, bool claimed, bool canClaim, Func<PassReward?> claim, bool locked = false)
        {
            string label = reward.Text + (claimed ? "  ✓" : locked ? "  (잠김)" : "");
            var button = UiKit.Button(row, $"<size=26>{label}</size>", () =>
            {
                if (claim() == null) return;
                Save();
                Fill();
            }, canClaim ? Green : reward.Kind == PassRewardKind.Cosmetic ? UiKit.Shade(Purple, 0.7f) : UiKit.Background);
            button.interactable = canClaim;
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
