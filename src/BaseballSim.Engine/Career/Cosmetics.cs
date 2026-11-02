using System.Collections.Generic;
using System.Linq;

namespace BaseballSim.Career
{
    /// <summary>Where a cosmetic shows. One of each kind is worn at a time.</summary>
    public enum CosmeticKind
    {
        StadiumTheme, // 구장 테마: my team's home park in my-team league and arena games
        HomerEffect,  // 홈런·삼진 연출
        WalkUpSong,   // 등장곡
        CardFrame,    // 선수 카드 테두리
        TownTheme,    // 홈 마을 테마
    }

    /// <summary>
    /// A look, never an edge: nothing here changes ratings, growth or results. Bought slowly with
    /// club coins or straight away with real money; some come only from the season pass or a
    /// support pack.
    /// </summary>
    public class Cosmetic
    {
        public string Id;
        public string Name;
        public CosmeticKind Kind;
        /// <summary>Club-coin price; 0 = not sold for coins.</summary>
        public int Coins;
        /// <summary>Real-money price in won; 0 = not sold for money.</summary>
        public int Won;
        /// <summary>Only from the season pass.</summary>
        public bool PassOnly;
        public string Description;
    }

    public static class Cosmetics
    {
        public const string SupporterFrame = "frame_supporter";

        public static readonly List<Cosmetic> All = new List<Cosmetic>
        {
            new Cosmetic { Id = "stadium_night", Name = "야간 조명 구장", Kind = CosmeticKind.StadiumTheme, Coins = 3000, Won = 4900, Description = "조명탑이 환한 밤 경기 구장" },
            new Cosmetic { Id = "stadium_sunset", Name = "노을 구장", Kind = CosmeticKind.StadiumTheme, Coins = 3000, Won = 4900, Description = "주황빛 하늘 아래 저녁 경기" },
            new Cosmetic { Id = "stadium_snow", Name = "눈 내리는 구장", Kind = CosmeticKind.StadiumTheme, PassOnly = true, Description = "시즌 패스 보상" },
            new Cosmetic { Id = "homer_fireworks", Name = "불꽃놀이 홈런", Kind = CosmeticKind.HomerEffect, Coins = 2000, Won = 2900, Description = "내 선수 홈런 때 불꽃이 터져요" },
            new Cosmetic { Id = "homer_lightning", Name = "번개 홈런", Kind = CosmeticKind.HomerEffect, Coins = 2000, Won = 2900, Description = "타구에 번개가 따라가요" },
            new Cosmetic { Id = "homer_confetti", Name = "꽃가루 홈런", Kind = CosmeticKind.HomerEffect, PassOnly = true, Description = "시즌 패스 보상" },
            new Cosmetic { Id = "song_drum", Name = "응원 북", Kind = CosmeticKind.WalkUpSong, Coins = 1200, Won = 1900, Description = "타석 등장 때 북소리 응원" },
            new Cosmetic { Id = "song_brass", Name = "브라스 밴드", Kind = CosmeticKind.WalkUpSong, Coins = 1200, Won = 1900, Description = "트럼펫 응원가와 함께 등장" },
            new Cosmetic { Id = "frame_silver", Name = "은빛 테두리", Kind = CosmeticKind.CardFrame, Coins = 800, Won = 1200, Description = "선수 카드에 은빛 테두리" },
            new Cosmetic { Id = "frame_gold", Name = "금빛 테두리", Kind = CosmeticKind.CardFrame, Coins = 1200, Won = 1900, Description = "선수 카드에 금빛 테두리" },
            new Cosmetic { Id = SupporterFrame, Name = "후원자 테두리", Kind = CosmeticKind.CardFrame, Description = "응원 팩을 산 분께 드리는 테두리" },
            new Cosmetic { Id = "town_night", Name = "밤 마을", Kind = CosmeticKind.TownTheme, Coins = 2500, Won = 3900, Description = "불 켜진 건물들의 밤 풍경" },
            new Cosmetic { Id = "town_cherry", Name = "벚꽃 마을", Kind = CosmeticKind.TownTheme, Coins = 2500, Won = 3900, Description = "벚꽃이 흩날리는 봄 마을" },
            new Cosmetic { Id = "town_snow", Name = "눈 마을", Kind = CosmeticKind.TownTheme, PassOnly = true, Description = "시즌 패스 보상" },
        };

        public static Cosmetic Find(string id) => All.FirstOrDefault(c => c.Id == id);

        public static string KindName(CosmeticKind kind)
        {
            switch (kind)
            {
                case CosmeticKind.StadiumTheme: return "구장 테마";
                case CosmeticKind.HomerEffect: return "홈런 연출";
                case CosmeticKind.WalkUpSong: return "등장곡";
                case CosmeticKind.CardFrame: return "카드 테두리";
                default: return "마을 테마";
            }
        }
    }
}
