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

        /// <summary>The ending told at retirement, then the people of the career saying goodbye one by one.</summary>
        public static void OpenEnding(string title, List<string> lines, List<EndingVoice> voices = null)
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
            if (voices != null && voices.Count > 0) screen.StartCoroutine(screen.Voices(voices));
        }

        /// <summary>Each goodbye in its own speech card, appearing after the one before, as if said aloud.</summary>
        System.Collections.IEnumerator Voices(List<EndingVoice> voices)
        {
            var head = UiKit.Label(_content, "그리고, 함께한 사람들", 34, TextAnchor.MiddleCenter, Gold);
            head.fontStyle = FontStyle.Bold;
            UiKit.Height(head, 100);
            var scroll = _content.GetComponentInParent<ScrollRect>();
            foreach (var v in voices)
            {
                for (float t = 0; t < 1.3f; t += Time.unscaledDeltaTime) yield return null;
                if (this == null) yield break;
                var card = UiKit.Panel(_content, "Voice", new Color(1, 1, 1, 0.06f));
                UiKit.Column(card, 22, 6);
                var who = UiKit.Label(card, $"<b>{v.Who}</b>  <size=24><color=#BAC4D8>{v.Role}</color></size>", 30, TextAnchor.MiddleLeft, Gold);
                UiKit.Height(who, 44);
                var said = UiKit.Label(card, $"\"{v.Line}\"", 30, TextAnchor.UpperLeft);
                said.horizontalOverflow = HorizontalWrapMode.Wrap;
                said.verticalOverflow = VerticalWrapMode.Overflow;
                float lineHeight = 42 * Mathf.Max(1, Mathf.CeilToInt((v.Line.Length + 2) / 26f));
                UiKit.Height(said, lineHeight);
                UiKit.Height(card, 44 + lineHeight + 22 * 2 + 6);
                UiAnim.FadeIn(card, 0.5f);
                UiAnim.PopIn(card, 0.3f);
                GameAudio.Sfx(GameAudio.Popup);
                if (scroll != null) { Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 0; }
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
                    string tag = lover != null && lover.Key == her.Key ? (Romance.Married(career) ? " <color=#FF8FB0>♥ 아내</color>" : " <color=#FF8FB0>♥ 연인</color>")
                        : Romance.Some(career)?.Key == her.Key ? " <color=#FF8FB0>썸</color>"
                        : Romance.Drifted(career, her.Key) ? " <size=24>(새 출발)</size>" : "";
                    UiKit.Width(UiKit.Label(row, $"<b>{her.Name}</b>{tag} <size=24>{her.Role}</size>", 30), 430);
                    var fill = UiKit.Bar(row, new Color(1f, 0.56f, 0.69f));
                    UiKit.Height(fill.parent, 26);
                    UiKit.Width(fill.parent, 0, 1);
                    UiKit.SetBar(fill, heart / 100f);
                    UiKit.Width(UiKit.Label(row, heart.ToString(), 30, TextAnchor.MiddleRight), 70);
                }
                if (lover == null) Line(Romance.Established(career) ? $"마음 {Romance.ConfessAt} 이상, 다른 사람보다 {Romance.ClearLead} 앞서면 썸이 시작돼요. 갈림길에서 어디로 가는지가 마음을 정해요." : "주전으로 자리 잡으면(3장) 마음을 정할 일들이 생겨요.");
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
