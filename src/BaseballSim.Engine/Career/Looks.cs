using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum BodyBuild { Slim, Normal, Big }

    /// <summary>
    /// What my player looks like, put together from parts at registration (or rolled at random).
    /// The parts are VRoid models (see AvatarRig in the Unity project): a body per build, hair and faces swapped onto it.
    /// Each number is an index into the matching list in <see cref="Looks"/>. The game draws the
    /// portrait from these parts wherever my player's face shows: dialogue, profile, title screen.
    /// </summary>
    public class PlayerLook
    {
        public BodyBuild Build = BodyBuild.Normal;
        public int Skin;
        public int Hair;
        public int HairColor;
        public int Eyes;
        public int Expression;

        public PlayerLook Clone() => (PlayerLook)MemberwiseClone();

        /// <summary>Same parts give the same key, so a drawn portrait can be cached by it.</summary>
        public string Key => $"{(int)Build}-{Skin}-{Hair}-{HairColor}-{Eyes}-{Expression}";

        public override bool Equals(object obj) => obj is PlayerLook o && o.Key == Key;
        public override int GetHashCode() => Key.GetHashCode();
    }

    /// <summary>The parts a look is made of. Colours are hex for the code to tint grey layers with.</summary>
    public static class Looks
    {
        public static readonly (BodyBuild Build, string Name, string File)[] Builds =
        {
            (BodyBuild.Slim, "호리호리", "slim"),
            (BodyBuild.Normal, "보통", "normal"),
            (BodyBuild.Big, "거구", "big"),
        };

        /// <summary>Hex is the colour shown on the picker; Tint multiplies the model's (lightest) skin.</summary>
        public static readonly (string Name, string Hex, string Tint)[] Skins =
        {
            ("밝은 피부", "#FFE3D0", "#FFFFFF"), ("보통 피부", "#F6CBAA", "#F7E3D3"), ("햇볕에 탄 피부", "#DDA67F", "#E2C2A6"),
            ("구릿빛 피부", "#B97D55", "#C49A78"), ("짙은 피부", "#8A5A3C", "#94694E"),
        };

        /// <summary>File is the hair_&lt;file&gt; model; null is the base model's own hair, always there.</summary>
        public static readonly (string Name, string File)[] Hairs =
        {
            ("스포츠머리", null), ("덮은 앞머리", "bangs"), ("가르마", "sidepart"), ("장발", "long"),
        };

        public static readonly (string Name, string Hex)[] HairColors =
        {
            ("흑발", "#2A2A30"), ("짙은 갈색", "#4E3426"), ("밝은 갈색", "#8A5B3A"),
            ("애쉬 그레이", "#9AA0A8"), ("금발", "#E3C278"), ("붉은 갈색", "#8E3B2A"),
        };

        /// <summary>Eyes and brows come together, from one face_&lt;file&gt; model each; null is the base model's own face.</summary>
        public static readonly (string Name, string File)[] Eyes =
        {
            ("또렷한 눈매", null), ("날카로운 눈매", "sharp"), ("순한 눈매", "gentle"), ("큰 눈매", "round"),
        };

        /// <summary>The portrait's expression, from the face model's own blend shapes (VRoid names), weight 0-1.</summary>
        public static readonly (string Name, string Shape, float Weight)[] Expressions =
        {
            ("무표정", null, 0f), ("미소", "Fcl_ALL_Fun", 0.7f), ("활짝 웃음", "Fcl_ALL_Joy", 0.6f), ("진지함", "Fcl_ALL_Angry", 0.35f),
        };

        public static string BuildName(BodyBuild b) => Builds.First(x => x.Build == b).Name;
        public static string BuildFile(BodyBuild b) => Builds.First(x => x.Build == b).File;

        /// <summary>A whole look at random, for the 랜덤 조합 button.</summary>
        public static PlayerLook Random(IRandom random)
        {
            int Pick(int count) => Math.Min(count - 1, (int)(random.NextDouble() * count));
            return new PlayerLook
            {
                Build = Builds[Pick(Builds.Length)].Build,
                Skin = Pick(Skins.Length),
                Hair = Pick(Hairs.Length),
                HairColor = Pick(HairColors.Length),
                Eyes = Pick(Eyes.Length),
                Expression = Pick(Expressions.Length),
            };
        }

        /// <summary>Keeps every index inside its list, so a save from a build with more parts still loads.</summary>
        public static PlayerLook Clamp(PlayerLook look)
        {
            if (look == null) return null;
            int C(int v, int count) => Math.Max(0, Math.Min(count - 1, v));
            if (!Enum.IsDefined(typeof(BodyBuild), look.Build)) look.Build = BodyBuild.Normal;
            look.Skin = C(look.Skin, Skins.Length);
            look.Hair = C(look.Hair, Hairs.Length);
            look.HairColor = C(look.HairColor, HairColors.Length);
            look.Eyes = C(look.Eyes, Eyes.Length);
            look.Expression = C(look.Expression, Expressions.Length);
            return look;
        }

        /// <summary>"보통 · 투블럭 · 흑발 · 또렷한 눈", for the profile and the registration summary.</summary>
        public static string Describe(PlayerLook look) =>
            look == null ? "" : $"{BuildName(look.Build)} · {Hairs[look.Hair].Name} · {HairColors[look.HairColor].Name} · {Eyes[look.Eyes].Name}";
    }
}
