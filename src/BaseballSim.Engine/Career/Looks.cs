using System;
using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    public enum BodyBuild { Slim, Normal, Big }

    /// <summary>
    /// What my player looks like, put together from parts at registration (or rolled at random).
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
        public int Brows;

        public PlayerLook Clone() => (PlayerLook)MemberwiseClone();

        /// <summary>Same parts give the same key, so a drawn portrait can be cached by it.</summary>
        public string Key => $"{(int)Build}-{Skin}-{Hair}-{HairColor}-{Eyes}-{Brows}";

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

        public static readonly (string Name, string Hex)[] Skins =
        {
            ("밝은 피부", "#FFE3D0"), ("보통 피부", "#F6CBAA"), ("햇볕에 탄 피부", "#DDA67F"),
            ("구릿빛 피부", "#B97D55"), ("짙은 피부", "#8A5A3C"),
        };

        public static readonly (string Name, string File)[] Hairs =
        {
            ("짧은 머리", "short"), ("스포츠머리", "buzz"), ("투블럭", "twoblock"), ("덮은 앞머리", "bangs"),
            ("가르마", "sidepart"), ("곱슬 파마", "curly"), ("장발", "long"), ("빡빡머리", "shaved"),
        };

        public static readonly (string Name, string Hex)[] HairColors =
        {
            ("흑발", "#2A2A30"), ("짙은 갈색", "#4E3426"), ("밝은 갈색", "#8A5B3A"),
            ("애쉬 그레이", "#9AA0A8"), ("금발", "#E3C278"), ("붉은 갈색", "#8E3B2A"),
        };

        public static readonly (string Name, string File)[] Eyes =
        {
            ("또렷한 눈", "bright"), ("날카로운 눈", "sharp"), ("순한 눈", "gentle"),
            ("가는 눈", "narrow"), ("큰 눈", "round"), ("졸린 눈", "sleepy"),
        };

        public static readonly (string Name, string File)[] Brows =
        {
            ("곧은 눈썹", "straight"), ("올라간 눈썹", "angled"), ("짙은 눈썹", "thick"), ("처진 눈썹", "soft"),
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
                Brows = Pick(Brows.Length),
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
            look.Brows = C(look.Brows, Brows.Length);
            return look;
        }

        /// <summary>"보통 · 투블럭 · 흑발 · 또렷한 눈", for the profile and the registration summary.</summary>
        public static string Describe(PlayerLook look) =>
            look == null ? "" : $"{BuildName(look.Build)} · {Hairs[look.Hair].Name} · {HairColors[look.HairColor].Name} · {Eyes[look.Eyes].Name}";
    }
}
