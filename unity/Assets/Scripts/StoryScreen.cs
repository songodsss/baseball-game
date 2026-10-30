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
            screen.Build("메인 스토리");
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
            Section($"{MainStory.ChapterName(career)}  ({story.Seen.Count}/{MainStory.SceneCount} 장면)");

            Section("사람들");
            foreach (var bond in MainStory.AllBonds)
            {
                int value = story.BondOf(bond);
                var row = UiKit.Empty(_content, "Bond");
                UiKit.Height(row, 70);
                UiKit.Row(row, 16);
                UiKit.Width(UiKit.Label(row, $"<b>{MainStory.BondName(bond)}</b> <size=24>{MainStory.BondRole(bond)}</size>", 30), 430);
                var fill = UiKit.Bar(row, value >= 70 ? Gold : UiKit.Accent);
                UiKit.Height(fill.parent, 26);
                UiKit.Width(fill.parent, 0, 1);
                UiKit.SetBar(fill, value / 100f);
                UiKit.Width(UiKit.Label(row, value.ToString(), 30, TextAnchor.MiddleRight), 70);
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
