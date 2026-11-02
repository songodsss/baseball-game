using System;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// The arena: my team, exactly as built, against other clubs' teams of the same weight class.
    /// Until the online server exists, opponents are practice teams generated to match.
    /// </summary>
    public class ArenaScreen : MonoBehaviour
    {
        static readonly Color Red = new Color(0.8f, 0.3f, 0.3f);
        static readonly Color CoinBrown = new Color(0.7f, 0.55f, 0.15f);

        Club _club;
        Action _onClose;
        IRandom _random;
        GameObject _canvas;
        RectTransform _card;
        ArenaResult _last;

        static string Today => DailyMissions.DateOf(DateTime.Now);

        public static void Open(Club club, Action onClose = null)
        {
            var screen = new GameObject("ArenaScreen").AddComponent<ArenaScreen>();
            screen._club = club;
            screen._onClose = onClose;
            screen._random = new SeededRandom(Environment.TickCount);
            screen.Build();
        }

        void Build()
        {
            _club.Arena.Refresh(Today, _club);
            Save();
            _canvas = UiKit.Canvas(transform, "ArenaCanvas", 15);
            var dim = UiKit.Dim(_canvas.transform);
            _card = UiKit.PopupCard(dim, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));
            UiKit.Column(_card, 32, 14);
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
            var arena = _club.Arena;

            var heading = UiKit.Label(_card, "결투장", 48, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            UiKit.Height(heading, 76);
            UiKit.Height(UiKit.Label(_card,
                $"<b>{arena.Tier}</b>  ·  점수 {arena.Rating}  ·  {arena.Wins}승 {arena.Losses}패" +
                $"\n<size=24><color=#BAC4D8>시즌 {Arena.LastDayOf(arena.SeasonId)}까지 · 최고 {Arena.TierOf(arena.SeasonBest)} (보상 코인 {Arena.TierCoinsOf(arena.SeasonBest)}) · 오늘 남은 입장 {arena.EntriesLeft} · 코인 {_club.Coins:N0}</color></size>",
                30, TextAnchor.MiddleCenter), 100);

            var body = UiKit.ScrollList(_card, 10);
            UiKit.Height(body.parent, 0, 1);

            if (arena.LastSeasonNote != null)
            {
                Box(body, $"<color=#FFD24D><b>{arena.LastSeasonNote}</b></color>", 110);
                arena.LastSeasonNote = null;
                Save();
            }

            if (_club.Roster.Count == 0)
            {
                UiKit.Height(UiKit.Label(body, "선수를 한 명 은퇴시켜 내 팀에 영입하면 결투장에 나갈 수 있어요.", 30, TextAnchor.MiddleCenter, UiKit.Muted), 240);
            }
            else
            {
                if (_last != null) ResultBox(body, _last);

                UiKit.Height(UiKit.Label(body,
                    $"내 팀 전력 {Arena.MyPower(_club)}  ·  구단 본부에서 짠 내 팀이 그대로 나가요.\n" +
                    "<size=24>지금은 온라인 연결 전이라 같은 체급의 연습 상대와 붙어요.</size>",
                    26, TextAnchor.MiddleCenter, UiKit.Muted), 90);

                if (arena.Candidates.Count == 0)
                {
                    var find = UiKit.Button(body, "상대 찾기", () => { arena.FindOpponents(_club, _random); Fill(); }, UiKit.Accent);
                    UiKit.Height(find, 110);
                    find.interactable = arena.EntriesLeft > 0;
                }
                else
                {
                    for (int i = 0; i < arena.Candidates.Count; i++) OpponentRow(body, i, arena.Candidates[i]);
                }

                if (arena.EntriesLeft == 0)
                {
                    var buy = UiKit.Button(body, arena.CanBuyEntry ? $"입장 1회 추가 ({Arena.ExtraEntryCoins}코인)" : "오늘 입장 추가는 끝났어요", () =>
                    {
                        if (arena.BuyEntry(_club)) { Save(); Fill(); }
                    }, CoinBrown);
                    UiKit.Height(buy, 100);
                    buy.interactable = arena.CanBuyEntry && _club.Coins >= Arena.ExtraEntryCoins;
                }
            }

            var buttons = UiKit.Empty(_card, "Buttons");
            UiKit.Height(buttons, 96);
            UiKit.Row(buttons, 12);
            if (_last != null)
            {
                var game = _last.Game;
                UiKit.Button(buttons, "방금 경기 보기", () => Watch(game), UiKit.Muted);
            }
            UiKit.Button(buttons, "닫기", Close, UiKit.Muted);
        }

        void OpponentRow(RectTransform body, int index, ArenaOpponent rival)
        {
            var arena = _club.Arena;
            string edge = rival.Edge < 0 ? "약간 약함" : rival.Edge > 0 ? "약간 강함" : "비슷함";
            int gain = Arena.Change(arena.Rating, rival.Rating, true), loss = Arena.Change(arena.Rating, rival.Rating, false);
            var row = UiKit.Panel(body, "Rival", UiKit.Shade(UiKit.Card, 1.3f));
            UiKit.Height(row, 170);
            var layout = UiKit.Row(row, 14);
            layout.padding = new RectOffset(24, 20, 14, 14);
            layout.childForceExpandWidth = false;
            var text = UiKit.Label(row,
                $"<b>{rival.Owner}</b>  <size=26>{Arena.TierOf(rival.Rating)} {rival.Rating}</size>\n" +
                $"<size=24><color=#BAC4D8>전력 {rival.Power} ({edge})  ·  이기면 +{gain}, 지면 {loss}</color></size>", 32);
            UiKit.Width(text, 0, 1);
            var go = UiKit.Button(row, "도전", () => Fight(index), rival.Edge > 0 ? Red : UiKit.Accent);
            go.interactable = arena.EntriesLeft > 0;
            UiKit.Width(go, 170);
        }

        void ResultBox(RectTransform body, ArenaResult r)
        {
            var g = r.Game;
            string verdict = g.IsTie ? "무승부" : r.Won ? "<color=#5FA8FF>승리!</color>" : "<color=#F28C40>패배</color>";
            string change = r.RatingChange >= 0 ? $"+{r.RatingChange}" : r.RatingChange.ToString();
            Box(body, $"<b>{verdict}</b>  {g.AwayScore} : {g.HomeScore}  vs {r.Rival.Owner}\n점수 {change}" + (r.Coins > 0 ? $"  ·  코인 +{r.Coins}" : ""), 150);
        }

        void Box(RectTransform body, string text, float height)
        {
            var box = UiKit.Panel(body, "Box", UiKit.Shade(UiKit.Card, 1.5f));
            UiKit.Height(box, height);
            var label = UiKit.Label(box, text, 32, TextAnchor.MiddleCenter);
            UiKit.Stretch(label.rectTransform);
            UiAnim.PopIn(box);
        }

        void Fight(int index)
        {
            var result = _club.Arena.Play(_club, index, _random);
            if (result == null) return;
            _last = result;
            Save();
            Fill();
        }

        void Watch(GameResult game)
        {
            _canvas.SetActive(false);
            MatchScreen.Open(game, null, () => { if (this != null) _canvas.SetActive(true); });
        }

        void Close()
        {
            _onClose?.Invoke();
            Destroy(gameObject);
        }
    }
}
