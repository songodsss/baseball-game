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

        /// <summary>The legend's picture for "legend_jang", "legend_jang_2", "legend_jang_3" or "legend_jang_oh".</summary>
        public static Sprite LegendPortrait(string eventId)
        {
            if (!eventId.StartsWith("legend_")) return null;
            string id = eventId.Substring("legend_".Length);
            int cut = id.IndexOf('_');
            return Get("Legends/" + (cut < 0 ? id : id.Substring(0, cut)));
        }

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

        /// <summary>
        /// The drawn face for a player: one of the portrait set, picked from the player's id so the
        /// same player keeps the same face. Pitchers use their own set when it exists.
        /// </summary>
        public static Sprite Portrait(CareerPlayer me)
        {
            string key = me.Id ?? me.Name ?? "";
            int hash = 17;
            foreach (char c in key) hash = unchecked(hash * 31 + c);
            string n = ((hash & 0x7fffffff) % 4 + 1).ToString("00");
            return (me.IsPitcher ? Get("Portraits/pitcher_" + n) : null) ?? Get("Portraits/player_" + n);
        }

        static readonly Dictionary<EquipmentSlot, string> GearIcons = new Dictionary<EquipmentSlot, string>
        {
            { EquipmentSlot.Bat, "bat" }, { EquipmentSlot.BattingGloves, "batting_gloves" }, { EquipmentSlot.Glove, "glove" },
            { EquipmentSlot.Spikes, "spikes" }, { EquipmentSlot.Helmet, "helmet" },
        };

        public static Sprite GearIcon(EquipmentSlot slot) =>
            GearIcons.TryGetValue(slot, out var name) ? Get("Icons/Gear/" + name) : null;

        /// <summary>Practice, pro, premium, luxury: gray, blue, purple, gold. Read from the "bat3" style id.</summary>
        public static Color GearTierColor(Equipment gear)
        {
            if (gear == null) return new Color(0.36f, 0.4f, 0.48f);
            char last = gear.Id[gear.Id.Length - 1];
            switch (last)
            {
                case '2': return new Color(0.37f, 0.66f, 1f);
                case '3': return new Color(0.68f, 0.45f, 0.95f);
                case '4': return new Color(1f, 0.82f, 0.3f);
                default: return new Color(0.75f, 0.78f, 0.84f);
            }
        }

        static readonly Dictionary<Facility, string> FacilityIcons = new Dictionary<Facility, string>
        {
            { Facility.TrainerRoom, "trainer_room" }, { Facility.RehabCenter, "rehab_center" }, { Facility.Conditioning, "conditioning" },
        };

        public static Sprite FacilityIcon(Facility facility) =>
            FacilityIcons.TryGetValue(facility, out var name) ? Get("Icons/Facilities/" + name) : null;

        public static Sprite TraitIcon(string traitId) => Get("Icons/Traits/" + traitId);

        public static Sprite AchievementIcon(string achievementId) => Get("Icons/Achievements/" + achievementId);

        /// <summary>The trophy for a title; every per-stat crown (홈런왕, 다승왕…) shares one plaque.</summary>
        public static Sprite TrophyIcon(string title)
        {
            string name = title == Postseason.ChampionTitle || title == "우승 반지" ? "champion_trophy"
                : title == Awards.Mvp ? "mvp_trophy"
                : title == Awards.Rookie ? "rookie_trophy"
                : title == Awards.GoldenGlove ? "golden_glove_trophy"
                : title == NationalTeam.Gold || title.Contains("금메달") ? "gold_medal"
                : "batting_title";
            return Get("Icons/Trophies/" + name);
        }

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

        /// <summary>
        /// A small picture at the front of a layout row (or a table row), or nothing when it isn't drawn yet.
        /// </summary>
        public static Image Icon(Transform row, Sprite art, float size, bool first = true)
        {
            if (art == null) return null;
            var rt = UiKit.Empty(row, "Icon");
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var layout = rt.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = layout.minWidth = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0;
            if (first) rt.SetAsFirstSibling();
            return image;
        }

        /// <summary>A line of text with an icon in front of it; just the text when there's no picture.</summary>
        public static Text IconLine(RectTransform parent, Sprite art, string text, int fontSize, float height, Color? color = null)
        {
            if (art == null)
            {
                var plain = UiKit.Label(parent, text, fontSize, TextAnchor.MiddleLeft, color);
                UiKit.Height(plain, height);
                return plain;
            }
            var line = UiKit.Empty(parent, "IconLine");
            UiKit.Height(line, height);
            var layout = UiKit.Row(line, 14);
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            Icon(line, art, height - 6);
            var label = UiKit.Label(line, text, fontSize, TextAnchor.MiddleLeft, color);
            UiKit.Width(label, 0, 1);
            return label;
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
