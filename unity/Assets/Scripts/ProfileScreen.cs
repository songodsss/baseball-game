using System.Linq;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>My player on one card: a radar of ratings against potential, traits, gear and honours.</summary>
    public class ProfileScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        CareerState _career;

        public static void Open(CareerState career)
        {
            var screen = new GameObject("ProfileScreen").AddComponent<ProfileScreen>();
            screen._career = career;
            screen.Build();
        }

        void Build()
        {
            var me = _career.Me;
            var team = UiKit.TeamColor(_career.MyTeam.Name);
            var canvas = UiKit.Canvas(transform, "ProfileCanvas", 5);
            var root = UiKit.SafeRoot(canvas.transform, UiKit.Shade(team, 0.35f) + new Color(0.03f, 0.03f, 0.06f, 0));
            UiKit.Column(root, 40, 18);

            // Name band: badge with the number, then name, position, club.
            var band = UiKit.Panel(root, "Band", UiKit.Shade(team, 0.6f));
            UiKit.Height(band, 200);
            var row = UiKit.Row(band, 20);
            row.padding = new RectOffset(24, 24, 20, 20);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            var emblem = ArtLibrary.Emblem(band, _career.MyTeam.Name, 150);
            UiKit.Width(emblem, 150);
            string position = me.IsPitcher ? $"{Positions.RoleName(me.Role)} 투수" : Positions.Name(me.Position);
            var title = UiKit.Label(band, $"<b>{me.Name}</b>  {(me.Number > 0 ? "#" + me.Number : "")}\n" +
                $"<size=30><color=#DDE3EE>{position} · {me.Age}세 · {_career.MyTeam.Name} · {me.History.Count + 1}년차</color></size>", 52);
            UiKit.Width(title, 0, 1);

            var body = UiKit.Panel(root, "Body", UiKit.Card);
            UiKit.Height(body, 0, 1);
            UiKit.Column(body, 20, 0);
            var content = UiKit.ScrollList(body, 10);
            UiKit.Height(content.parent, 0, 1);

            Radar(content);

            Section(content, "정보");
            Line(content, $"연봉 {me.Salary:N0}만원 · 보유 {me.Money:N0}만원 · 인기 {me.Popularity}");
            if (_career.ContractSeasonsLeft > 0) Line(content, $"{_career.MyTeam.Name} 계약 {_career.ContractSeasonsLeft}시즌 남음");
            if (me.Clubs.Count > 0) Line(content, "거쳐 온 구단: " + string.Join(" → ", me.Clubs));
            if (_career.Rival != null) Line(content, $"라이벌: {_career.Rival.Name} ({_career.RivalTeam?.Name})");

            Section(content, $"특성 {me.Traits.Count}개");
            if (me.Traits.Count == 0) Line(content, "능력치 총합이 오르면 특성을 고를 수 있어요.", UiKit.Muted);
            foreach (var id in me.Traits)
            {
                var t = Traits.Find(id);
                if (t != null) Line(content, $"<b>{t.Name}</b>  <color=#9EABC2>{t.Description}</color>");
            }

            Section(content, "장비");
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                me.Equipped.TryGetValue(slot, out var gear);
                string bonus = gear == null ? "" : "  <color=#5FA8FF>" + string.Join(" ", gear.Bonus.Select(b => $"{Stats.Name(b.Key)}+{b.Value}")) + "</color>";
                Line(content, gear == null ? "<color=#5C667A>비어 있음</color>" : gear.Name + bonus);
            }

            var titles = me.History.SelectMany(h => h.Titles).ToList();
            Section(content, $"수상 {titles.Count}개");
            if (titles.Count == 0) Line(content, "시즌이 끝나고 타이틀을 따면 여기에 쌓여요.", UiKit.Muted);
            foreach (var g in titles.GroupBy(t => t)) Line(content, $"<color=#FFD24D>★</color> {g.Key}" + (g.Count() > 1 ? $" ×{g.Count()}" : ""));
            if (me.AllStarSelections > 0) Line(content, $"올스타 {me.AllStarSelections}회" + (me.AllStarMvps > 0 ? $" (MVP {me.AllStarMvps}회)" : ""));
            if (me.GoldMedals > 0) Line(content, $"{NationalTeam.Name} 금메달 {me.GoldMedals}개");

            UiKit.Height(UiKit.Button(root, "닫기", () => Destroy(gameObject), UiKit.Muted), 110);
        }

        void Radar(RectTransform content)
        {
            var me = _career.Me;
            var stats = me.MyStats.ToList();
            var holder = UiKit.Empty(content, "Radar");
            UiKit.Height(holder, 620);
            var chartRt = UiKit.Empty(holder, "Chart");
            chartRt.anchorMin = chartRt.anchorMax = new Vector2(0.5f, 0.5f);
            chartRt.sizeDelta = new Vector2(440, 440);
            var chart = chartRt.gameObject.AddComponent<RadarGraphic>();
            chart.raycastTarget = false;
            chart.Values = stats.Select(s => me.EffectiveRating(s) / 100f).ToList();
            chart.Ceiling = stats.Select(s => me.Potential[s] / 100f).ToList();
            chart.SetVerticesDirty();
            for (int i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                var label = UiKit.Label(chartRt, $"{Stats.Name(s)}\n<b><size=34>{me.EffectiveRating(s)}</size></b><size=22><color=#9EABC2>/{me.Potential[s]}</color></size>", 26, TextAnchor.MiddleCenter);
                label.rectTransform.sizeDelta = new Vector2(180, 90);
                label.rectTransform.anchoredPosition = chart.AxisPoint(i, 1.3f);
            }
            UiAnim.PopIn(chartRt, 0.35f);
            var legend = UiKit.Label(holder, "<color=#5FA8FF>■</color> 현재 능력치(장비 포함)   <color=#FFD24D>■</color> 잠재력", 24, TextAnchor.LowerCenter, UiKit.Muted);
            UiKit.Stretch(legend.rectTransform);
        }

        static void Section(RectTransform content, string title) =>
            UiKit.Height(UiKit.Label(content, title, 34, TextAnchor.LowerLeft, Gold), 70);

        static void Line(RectTransform content, string text, Color? color = null) =>
            UiKit.Height(UiKit.Label(content, text, 28, TextAnchor.MiddleLeft, color), 52);
    }
}
