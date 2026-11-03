using System.Collections.Generic;
using System.Linq;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>The main story so far: the chapter, how close the player is to each of the four people, and the scenes answered.</summary>
    public class StoryScreen : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        RectTransform _content;

        public static void Open(CareerState career)
        {
            var screen = new GameObject("StoryScreen").AddComponent<StoryScreen>();
            screen.Build("메인 스토리 · " + MainStory.RouteName(career.Story.Route));
            screen.ShowProgress(career);
        }

        /// <summary>The ending told at retirement.</summary>
        public static void OpenEnding(string title, List<string> lines)
        {
            var screen = new GameObject("StoryScreen").AddComponent<StoryScreen>();
            screen.Build(title);
            foreach (var line in lines)
            {
                bool headline = line.StartsWith("[");
                var label = UiKit.Label(screen._content, line, headline ? 34 : 32, TextAnchor.MiddleLeft, headline ? Gold : (Color?)null);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                UiKit.Height(label, headline ? 110 : 130);
            }
        }

        void Build(string title)
        {
            var canvasGo = new GameObject("StoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var root = UiKit.SafeRoot(canvasGo.transform, UiKit.Background);
            UiKit.Column(root, 40, 20);
            UiKit.Backdrop(root, ArtLibrary.Background("story"));

            var heading = UiKit.Label(root, title, 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 90);

            var card = UiKit.Panel(root, "Card", UiKit.Card);
            UiKit.Height(card, 0, 1);
            UiKit.Column(card, 30, 0);
            _content = UiKit.ScrollList(card, 10);
            UiKit.Height(_content.parent, 0, 1);

            UiKit.Height(UiKit.Button(root, "닫기", () => Destroy(gameObject), UiKit.Muted), 110);
        }

        void ShowProgress(CareerState career)
        {
            var story = career.Story;
            Line(MainStory.RouteBlurb(story.Route));
            Section($"{MainStory.ChapterName(career)}  ({story.Seen.Count}/{MainStory.SceneCount} 장면)");

            var promises = StoryPromises.Active(career);
            if (promises.Count > 0)
            {
                Section("이번 시즌 약속");
                foreach (var promise in promises) Line($"{promise.Text}  ({promise.Progress(career)})");
            }

            Section("사람들");
            foreach (var bond in MainStory.AllBonds)
            {
                int value = story.BondOf(bond);
                var row = UiKit.Empty(_content, "Bond");
                UiKit.Height(row, 70);
                UiKit.Row(row, 16);
                UiKit.Width(UiKit.Label(row, $"<b>{MainStory.BondName(career, bond)}</b> <size=24>{MainStory.BondRole(career, bond)}</size>", 30), 430);
                var fill = UiKit.Bar(row, value >= 70 ? Gold : UiKit.Accent);
                UiKit.Height(fill.parent, 26);
                UiKit.Width(fill.parent, 0, 1);
                UiKit.SetBar(fill, value / 100f);
                UiKit.Width(UiKit.Label(row, value.ToString(), 30, TextAnchor.MiddleRight), 70);
            }

            Line($"유대 {MainStory.BondEndingAt} 이상인 사람이 있으면 은퇴 후 그 사람을 따라가는 엔딩이 열려요. (명예의 전당·원클럽맨이 먼저)");
            if (career.IsFinalSeason) Line("<color=#FFD24D>올해가 마지막 시즌이에요.</color>");

            var met = Romance.Met(career);
            if (met.Count > 0)
            {
                Section("인연");
                var lover = Romance.Lover(career);
                foreach (var her in met)
                {
                    int heart = Romance.Heart(career, her.Key);
                    var row = UiKit.Empty(_content, "Heart");
                    UiKit.Height(row, 70);
                    UiKit.Row(row, 16);
                    string tag = lover == null ? "" : lover.Key == her.Key ? (Romance.Married(career) ? " <color=#FF8FB0>♥ 아내</color>" : " <color=#FF8FB0>♥ 연인</color>") : "";
                    UiKit.Width(UiKit.Label(row, $"<b>{her.Name}</b>{tag} <size=24>{her.Role}</size>", 30), 430);
                    var fill = UiKit.Bar(row, new Color(1f, 0.56f, 0.69f));
                    UiKit.Height(fill.parent, 26);
                    UiKit.Width(fill.parent, 0, 1);
                    UiKit.SetBar(fill, heart / 100f);
                    UiKit.Width(UiKit.Label(row, heart.ToString(), 30, TextAnchor.MiddleRight), 70);
                }
                if (lover == null) Line($"마음 {Romance.ConfessAt} 이상이면 고백할 수 있어요. 고르면 연애·프러포즈·결혼 엔딩으로 이어져요.");
            }

            Section("지나온 장면");
            if (story.Log.Count == 0) Line("아직 없어요. 첫 경기를 치르면 이야기가 시작돼요.");
            foreach (var entry in Enumerable.Reverse(story.Log)) Line(entry);
        }

        void Section(string title) => UiKit.Height(UiKit.Label(_content, title, 34, TextAnchor.LowerLeft, Gold), 80);

        void Line(string text)
        {
            var label = UiKit.Label(_content, text, 28, TextAnchor.MiddleLeft, UiKit.Muted);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiKit.Height(label, 70);
        }
    }
}
