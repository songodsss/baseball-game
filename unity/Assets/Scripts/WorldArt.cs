using System.Collections.Generic;
using UnityEngine;

namespace BaseballGame
{
    /// <summary>
    /// The free 3D assets for the ballpark under Assets/Resources/World/: tiling ground textures,
    /// the sky, and character models. Like ArtLibrary, every lookup can come back null and
    /// MatchField then builds the old coloured shapes instead.
    /// </summary>
    public static class WorldArt
    {
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, AnimationClip[]> Clips = new Dictionary<string, AnimationClip[]>();
        static readonly string[] Bodies = { "player_a", "player_b", "player_c" };

        /// <summary>World/Textures/&lt;name&gt;: grass (a grey detail map, tinted in code), dirt, sky.</summary>
        public static Texture2D Texture(string name)
        {
            if (!Textures.TryGetValue(name, out var tex)) Textures[name] = tex = Resources.Load<Texture2D>("World/Textures/" + name);
            return tex;
        }

        static readonly Dictionary<string, AnimationClip> Moves = new Dictionary<string, AnimationClip>();

        /// <summary>
        /// A baseball move from Mixamo in World/Moves/&lt;name&gt;.fbx (pitch, swing, stance, catch, throw,
        /// run, pickup, cheer), imported as humanoid so it plays on any of the player models; null until
        /// the file is there, and the figure falls back to its own generic clips.
        /// </summary>
        public static AnimationClip Move(string name)
        {
            if (Moves.TryGetValue(name, out var clip)) return clip;
            foreach (var c in Resources.LoadAll<AnimationClip>("World/Moves/" + name))
                if (!c.name.StartsWith("__preview__")) { clip = c; break; }
            Moves[name] = clip;
            return clip;
        }

        /// <summary>A move or its second version (swing / swing2), picked by any number; the plain name when there's no second.</summary>
        public static string Variant(string name, int pick) =>
            (pick & 1) == 1 && Move(name + "2") != null ? name + "2" : name;

        /// <summary>The skybox material the editor makes from the sky picture.</summary>
        public static Material Sky => Resources.Load<Material>("World/Sky");

        public static int BodyCount => Bodies.Length;

        /// <summary>One of the player models, picked by any number (it wraps), or null without models.</summary>
        public static GameObject Body(int variant, out AnimationClip[] clips)
        {
            string name = Bodies[(variant % Bodies.Length + Bodies.Length) % Bodies.Length];
            var model = Resources.Load<GameObject>("World/Characters/" + name);
            if (model == null)
            {
                clips = null;
                return null;
            }
            if (!Clips.TryGetValue(name, out clips))
            {
                var list = new List<AnimationClip>();
                foreach (var clip in Resources.LoadAll<AnimationClip>("World/Characters/" + name))
                    if (!clip.name.StartsWith("__preview__")) list.Add(clip);
                Clips[name] = clips = list.ToArray();
            }
            return model;
        }
    }
}
