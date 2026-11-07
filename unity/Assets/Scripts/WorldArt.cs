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
