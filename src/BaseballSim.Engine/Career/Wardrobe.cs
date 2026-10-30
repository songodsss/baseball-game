using System.Collections.Generic;

namespace BaseballSim.Career
{
    /// <summary>A uniform colour for my player. Looks only: it never touches a rating.</summary>
    public class Uniform
    {
        public string Id;
        public string Name;
        /// <summary>RGB as "#RRGGBB".</summary>
        public string Color;
        /// <summary>Sold for real money in the store; the rest are free from the start.</summary>
        public bool Premium;
        /// <summary>Store product id for premium uniforms.</summary>
        public string ProductId;
    }

    public static class Wardrobe
    {
        public const string DefaultId = "gold";

        public static readonly List<Uniform> All = new List<Uniform>
        {
            new Uniform { Id = "gold", Name = "기본 골드", Color = "#FFD24D" },
            new Uniform { Id = "white", Name = "클래식 화이트", Color = "#F2F2F2" },
            new Uniform { Id = "navy", Name = "원정 네이비", Color = "#2B3F73" },
            new Uniform { Id = "pink", Name = "네온 핑크", Color = "#FF4FA3", Premium = true, ProductId = "uniform_pink" },
            new Uniform { Id = "black", Name = "블랙 앤 골드", Color = "#1A1A1A", Premium = true, ProductId = "uniform_black" },
            new Uniform { Id = "mint", Name = "민트 서머", Color = "#5FE0C1", Premium = true, ProductId = "uniform_mint" },
            new Uniform { Id = "retro", Name = "레트로 크림", Color = "#E8D8B0", Premium = true, ProductId = "uniform_retro" },
        };

        public static Uniform Find(string id) => All.Find(u => u.Id == id) ?? All[0];
    }
}
