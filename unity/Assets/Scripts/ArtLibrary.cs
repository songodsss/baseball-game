using System.Collections.Generic;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using UnityEngine.UI;

namespace BaseballGame
{
    /// <summary>
    /// Finds drawn art under Assets/Resources/Art/ by the names in the art prompt list.
    /// Every lookup can come back null: screens then fall back to what UiKit draws in code,
    /// so the game works with any subset of the pictures in place.
    /// </summary>
    public static class ArtLibrary
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>The sprite at Art/&lt;path&gt; (no extension), or null if there's no such picture.</summary>
        public static Sprite Get(string path)
        {
            if (Cache.TryGetValue(path, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>("Art/" + path);
            if (sprite == null)
            {
                // Pictures dropped in without the Sprite import type still load as textures.
                var tex = Resources.Load<Texture2D>("Art/" + path);
                if (tex != null) sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
            Cache[path] = sprite;
            return sprite;
        }

        static readonly Dictionary<string, string> TeamIds = new Dictionary<string, string>
        {
            { "서울 블루윙스", "seoul_bluewings" }, { "부산 마린스타즈", "busan_marinestars" },
            { "인천 시호크스", "incheon_seahawks" }, { "대구 썬더볼츠", "daegu_thunderbolts" },
            { "광주 타이거릴리", "gwangju_tigerlilies" }, { "대전 이글아이즈", "daejeon_eagleeyes" },
            { "수원 나이츠", "suwon_knights" }, { "창원 다이노스톰", "changwon_dinostorm" },
            { "고양 레드폭스", "goyang_redfox" }, { "울산 웨일스", "ulsan_whales" },
            { AllStar.EastName, "allstar_east" }, { AllStar.WestName, "allstar_west" },
        };

        public static Sprite TeamEmblem(string teamName) =>
            TeamIds.TryGetValue(teamName, out var id) ? Get("Teams/" + id) : null;

        // The prompt list named some pictures before the event ids were settled.
        static readonly Dictionary<string, string> EventNames = new Dictionary<string, string>
        {
            { "coach_extra", "coach_lesson" }, { "commercial", "ad_offer" }, { "night_out", "away_night" },
            { "family_visit", "family" }, { "gear_sponsor", "sponsor" }, { "defense_drill", "fungo" },
        };

        public static Sprite EventArt(string eventId)
        {
            if (eventId.StartsWith("legend_")) return Get("Events/legend_visit");
            return Get("Events/" + (EventNames.TryGetValue(eventId, out var name) ? name : eventId));
        }

        public static Sprite LegendPortrait(string eventId) =>
            eventId.StartsWith("legend_") ? Get("Legends/" + eventId.Substring("legend_".Length)) : null;

        static readonly Dictionary<DailyAction, string> TrainingIcons = new Dictionary<DailyAction, string>
        {
            { DailyAction.TrainContact, "contact" }, { DailyAction.TrainPower, "power" }, { DailyAction.TrainEye, "eye" },
            { DailyAction.TrainSpeed, "speed" }, { DailyAction.TrainDefense, "defense" }, { DailyAction.Rest, "rest" },
            { DailyAction.TrainVelocity, "velocity" }, { DailyAction.TrainControl, "control" },
            { DailyAction.TrainBreaking, "breaking" }, { DailyAction.TrainStamina, "stamina" },
        };

        public static Sprite TrainingIcon(DailyAction action) =>
            TrainingIcons.TryGetValue(action, out var name) ? Get("Icons/Training/" + name) : null;

        public static Sprite Background(string screen) => Get("Backgrounds/" + screen);

        // ---------- Pieces built from art, with code fallbacks ----------

        /// <summary>
        /// A club badge: the emblem picture if there is one, otherwise a disc in the club colour
        /// with the club's first letter. Call SetEmblem to switch clubs later.
        /// </summary>
        public static RectTransform Emblem(Transform parent, string teamName, float size)
        {
            var disc = UiKit.Empty(parent, "Emblem");
            var image = disc.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            UiKit.Width(disc, size);
            UiKit.Height(disc, size);
            var letter = UiKit.Label(disc, "", (int)(size * 0.45f), TextAnchor.MiddleCenter);
            letter.fontStyle = FontStyle.Bold;
            letter.raycastTarget = false;
            UiKit.Stretch(letter.rectTransform);
            SetEmblem(disc, teamName);
            return disc;
        }

        public static void SetEmblem(RectTransform emblem, string teamName)
        {
            var image = emblem.GetComponent<Image>();
            var letter = emblem.GetComponentInChildren<Text>();
            var art = TeamEmblem(teamName);
            if (art != null)
            {
                image.sprite = art;
                image.color = Color.white;
                letter.text = "";
                return;
            }
            image.sprite = UiKit.CircleSprite;
            image.color = UiKit.TeamColor(teamName);
            // "서울 블루윙스" → "블": the nickname reads better than the city.
            int space = teamName.IndexOf(' ');
            letter.text = teamName.Length == 0 ? "" : teamName.Substring(space >= 0 && space + 1 < teamName.Length ? space + 1 : 0, 1);
        }

        /// <summary>A picture on top of a card, or nothing at all when it isn't drawn yet.</summary>
        public static Image Banner(Transform parent, Sprite art, float height)
        {
            if (art == null) return null;
            var rt = UiKit.Empty(parent, "Art");
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiKit.Height(rt, height);
            return image;
        }
    }
}
