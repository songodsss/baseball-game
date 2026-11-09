using System;
using System.Collections.Generic;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Picking my player's look part by part: a big preview on the left, one ◀ ▶ row per part on the
    /// right, and a 랜덤 조합 button that rolls every part at once. Used at registration and from the
    /// profile. Changes the look it's given in place.
    /// </summary>
    public static class LookPicker
    {
        const float RowHeight = 84;

        public static void Build(RectTransform parent, PlayerLook look, Func<PlayerLook> roll, Action changed = null)
        {
            var holder = UiKit.Empty(parent, "LookPicker");
            UiKit.Height(holder, 6 * RowHeight + 5 * 8 + 70);
            var outer = UiKit.Column(holder, 0, 10);
            outer.padding = new RectOffset(0, 0, 0, 0);

            var top = UiKit.Empty(holder, "Top");
            UiKit.Height(top, 6 * RowHeight + 5 * 8);
            var row = UiKit.Row(top, 20);
            row.childForceExpandWidth = false;

            // Preview and the dice.
            var left = UiKit.Empty(top, "Left");
            UiKit.Width(left, 300);
            UiKit.Column(left, 0, 12);
            var frame = UiKit.Panel(left, "Frame", UiKit.Background);
            UiKit.Height(frame, 0, 1);
            var previewRt = UiKit.Empty(frame, "Preview");
            UiKit.Stretch(previewRt);
            previewRt.offsetMin = new Vector2(8, 8);
            previewRt.offsetMax = new Vector2(-8, -8);
            var preview = previewRt.gameObject.AddComponent<Image>();
            preview.preserveAspect = true;
            preview.raycastTarget = false;

            var right = UiKit.Empty(top, "Parts");
            UiKit.Width(right, 0, 1);
            UiKit.Column(right, 0, 8);

            var summary = UiKit.Label(holder, "", 26, TextAnchor.MiddleCenter, UiKit.Muted);
            UiKit.Height(summary, 60);

            var refreshers = new List<Action>();
            void Refresh()
            {
                Looks.Clamp(look);
                var sprite = AvatarArt.Portrait(look);
                preview.sprite = sprite;
                preview.color = sprite == null ? Color.clear : Color.white;
                foreach (var r in refreshers) r();
                summary.text = AvatarArt.HasModels(look.Build)
                    ? Looks.Describe(look)
                    : Looks.Describe(look) + "\n<size=24>3D 모델이 들어오기 전까지는 체형이 같은 그림으로 보여요</size>";
                changed?.Invoke();
            }

            Part(right, "체형", Looks.Builds.Length, () => (int)look.Build, v => look.Build = Looks.Builds[v].Build, v => Looks.Builds[v].Name, null, refreshers, Refresh);
            Part(right, "피부", Looks.Skins.Length, () => look.Skin, v => look.Skin = v, v => Looks.Skins[v].Name, v => Looks.Skins[v].Hex, refreshers, Refresh);
            Part(right, "머리", Looks.Hairs.Length, () => look.Hair, v => look.Hair = v, v => Looks.Hairs[v].Name, null, refreshers, Refresh);
            Part(right, "머리색", Looks.HairColors.Length, () => look.HairColor, v => look.HairColor = v, v => Looks.HairColors[v].Name, v => Looks.HairColors[v].Hex, refreshers, Refresh);
            Part(right, "눈매", Looks.Eyes.Length, () => look.Eyes, v => look.Eyes = v, v => Looks.Eyes[v].Name, null, refreshers, Refresh);
            Part(right, "표정", Looks.Expressions.Length, () => look.Expression, v => look.Expression = v, v => Looks.Expressions[v].Name, null, refreshers, Refresh);

            var dice = UiKit.Button(left, "랜덤 조합", () =>
            {
                var rolled = roll();
                look.Build = rolled.Build;
                look.Skin = rolled.Skin;
                look.Hair = rolled.Hair;
                look.HairColor = rolled.HairColor;
                look.Eyes = rolled.Eyes;
                look.Expression = rolled.Expression;
                Refresh();
                UiAnim.PopIn(previewRt);
            }, UiKit.Card);
            dice.GetComponentInChildren<Text>().fontSize = 30;
            UiKit.Height(dice, 96);

            Refresh();
        }

        static void Part(RectTransform parent, string title, int count, Func<int> get, Action<int> set, Func<int, string> name,
            Func<int, string> swatch, List<Action> refreshers, Action refresh)
        {
            var line = UiKit.Panel(parent, title, UiKit.Background);
            UiKit.Height(line, RowHeight);
            var row = UiKit.Row(line, 8);
            row.padding = new RectOffset(16, 8, 6, 6);
            row.childForceExpandWidth = false;
            row.childAlignment = TextAnchor.MiddleLeft;

            var label = UiKit.Label(line, title, 28, TextAnchor.MiddleLeft, UiKit.Muted);
            UiKit.Width(label, 110);
            UiKit.Button(line, "◀", () => { set((get() - 1 + count) % count); refresh(); }, UiKit.Card);
            var value = UiKit.Label(line, "", 28, TextAnchor.MiddleCenter);
            UiKit.Width(value, 0, 1);
            UiKit.Button(line, "▶", () => { set((get() + 1) % count); refresh(); }, UiKit.Card);
            foreach (var b in line.GetComponentsInChildren<Button>())
            {
                UiKit.Width(b, 72);
                b.GetComponentInChildren<Text>().fontSize = 28;
            }
            refreshers.Add(() =>
            {
                int v = get();
                string dot = swatch == null ? "" : $"<color={swatch(v)}>●</color> ";
                value.text = $"{dot}{name(v)} <size=24><color=#BAC4D8>{v + 1}/{count}</color></size>";
            });
        }
    }
}
