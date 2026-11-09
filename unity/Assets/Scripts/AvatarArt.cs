using System.Collections.Generic;
using BaseballSim.Career;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// Draws my player's portrait from the parts of his look. The parts are pictures on one shared
    /// canvas (768×1024, head in the same place in every one) under Resources/Art/Avatar/:
    ///   skin_&lt;build&gt;   grey skin (face, neck, arms), tinted with the skin colour
    ///   body_&lt;build&gt;   the uniform over it, in its own colours
    ///   eyes_&lt;kind&gt;    in their own colours
    ///   brows_&lt;kind&gt;   grey, tinted a shade darker than the hair
    ///   hair_&lt;kind&gt;    grey, tinted with the hair colour
    ///   cap            on top of everything
    /// Until the skin and body of a build are in, the look falls back to one of the drawn whole
    /// portraits with the same build, so picking still changes the face.
    /// </summary>
    public static class AvatarArt
    {
        const string Folder = "Art/Avatar/";
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // The whole portraits drawn before looks existed, sorted by build.
        static readonly Dictionary<BodyBuild, string[]> Wholes = new Dictionary<BodyBuild, string[]>
        {
            { BodyBuild.Slim, new[] { "Portraits/player_02", "Portraits/pitcher_02", "Portraits/pitcher_03" } },
            { BodyBuild.Normal, new[] { "Portraits/player_01", "Portraits/player_04", "Portraits/pitcher_01" } },
            { BodyBuild.Big, new[] { "Portraits/player_03", "Portraits/pitcher_04" } },
        };

        /// <summary>True once the parts for this build are in, so the picker can say whether parts show.</summary>
        public static bool HasParts(BodyBuild build)
        {
            string b = Looks.BuildFile(build);
            return Exists("skin_" + b) && Exists("body_" + b);
        }

        public static Sprite Portrait(PlayerLook look)
        {
            if (look == null) return null;
            if (Cache.TryGetValue(look.Key, out var cached) && cached != null) return cached;
            var sprite = Compose(look) ?? Whole(look);
            Cache[look.Key] = sprite;
            return sprite;
        }

        static Sprite Whole(PlayerLook look)
        {
            var list = Wholes[look.Build];
            int hash = unchecked(look.Skin * 7 + look.Hair * 13 + look.HairColor * 5 + look.Eyes * 11 + look.Brows * 3);
            for (int i = 0; i < list.Length; i++)
            {
                var s = ArtLibrary.Get(list[(hash + i) % list.Length]);
                if (s != null) return s;
            }
            return null;
        }

        static readonly Dictionary<string, bool> Present = new Dictionary<string, bool>();

        static bool Exists(string name)
        {
            if (Present.TryGetValue(name, out var yes)) return yes;
            var tex = Resources.Load<Texture2D>(Folder + name);
            yes = tex != null;
            if (tex != null) Resources.UnloadAsset(tex);
            Present[name] = yes;
            return yes;
        }

        static Sprite Compose(PlayerLook look)
        {
            string b = Looks.BuildFile(look.Build);
            var skin = Resources.Load<Texture2D>(Folder + "skin_" + b);
            var body = Resources.Load<Texture2D>(Folder + "body_" + b);
            if (skin == null || body == null || !skin.isReadable) return null;

            int w = skin.width, h = skin.height;
            var pixels = new Color32[w * h];
            Color hair = Hex(Looks.HairColors[look.HairColor].Hex);
            var layers = new (Texture2D Tex, Color Tint)[]
            {
                (skin, Hex(Looks.Skins[look.Skin].Hex)),
                (body, Color.white),
                (Resources.Load<Texture2D>(Folder + "eyes_" + Looks.Eyes[look.Eyes].File), Color.white),
                (Resources.Load<Texture2D>(Folder + "brows_" + Looks.Brows[look.Brows].File), hair * 0.8f),
                (Resources.Load<Texture2D>(Folder + "hair_" + Looks.Hairs[look.Hair].File), hair),
                (Resources.Load<Texture2D>(Folder + "cap"), Color.white),
            };
            foreach (var (tex, tint) in layers)
            {
                if (tex == null || !tex.isReadable || tex.width != w || tex.height != h) continue;
                Over(pixels, tex.GetPixels32(), tint);
            }
            foreach (var (tex, _) in layers)
                if (tex != null) Resources.UnloadAsset(tex);

            var result = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            result.SetPixels32(pixels);
            result.Apply(false, true);
            return Sprite.Create(result, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        /// <summary>Lays one layer, multiplied by its tint, over what's below (straight alpha).</summary>
        static void Over(Color32[] dst, Color32[] src, Color tint)
        {
            float tr = tint.r, tg = tint.g, tb = tint.b;
            for (int i = 0; i < dst.Length; i++)
            {
                var s = src[i];
                if (s.a == 0) continue;
                float sa = s.a / 255f;
                var d = dst[i];
                float da = d.a / 255f;
                float oa = sa + da * (1 - sa);
                float sr = s.r * tr, sg = s.g * tg, sb = s.b * tb;
                float k = da * (1 - sa);
                dst[i] = new Color32(
                    (byte)Mathf.Clamp((sr * sa + d.r * k) / oa, 0, 255),
                    (byte)Mathf.Clamp((sg * sa + d.g * k) / oa, 0, 255),
                    (byte)Mathf.Clamp((sb * sa + d.b * k) / oa, 0, 255),
                    (byte)Mathf.Clamp(oa * 255, 0, 255));
            }
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
