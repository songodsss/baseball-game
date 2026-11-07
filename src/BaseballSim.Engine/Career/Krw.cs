using System;

namespace BaseballSim.Career
{
    /// <summary>
    /// Money on screen, kept short: amounts are counted in 만원 inside the game, shown as "3,500만원" below
    /// 1억 and as "1.2억원" / "15억원" from there.
    /// </summary>
    public static class Krw
    {
        public static string Format(long manwon)
        {
            long abs = Math.Abs(manwon);
            string sign = manwon < 0 ? "-" : "";
            if (abs < 10000) return $"{sign}{abs:N0}만원";
            double eok = abs / 10000.0;
            return sign + (eok >= 100 ? $"{Math.Round(eok):N0}억원" : $"{Math.Floor(eok * 10) / 10:0.#}억원");
        }

        /// <summary>With a + in front of a gain.</summary>
        public static string Signed(long manwon) => (manwon > 0 ? "+" : "") + Format(manwon);
    }
}
