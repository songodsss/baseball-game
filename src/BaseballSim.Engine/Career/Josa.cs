namespace BaseballSim.Career
{
    /// <summary>Korean particles that change with the word before them: 이/가, 을/를, 은/는, 과/와, 으로/로, 아/야, 이랑/랑.</summary>
    public static class Josa
    {
        /// <summary>The right form of <paramref name="particle"/> (given in either form) after <paramref name="word"/>.</summary>
        public static string After(string word, string particle)
        {
            bool batchim = HasBatchim(word, out bool rieul);
            switch (particle)
            {
                case "이": case "가": return batchim ? "이" : "가";
                case "을": case "를": return batchim ? "을" : "를";
                case "은": case "는": return batchim ? "은" : "는";
                case "과": case "와": return batchim ? "과" : "와";
                case "아": case "야": return batchim ? "아" : "야";
                case "이랑": case "랑": return batchim ? "이랑" : "랑";
                case "이에요": case "예요": return batchim ? "이에요" : "예요";
                case "으로": case "로": return batchim && !rieul ? "으로" : "로";
                default: return particle;
            }
        }

        /// <summary>Whether the last syllable ends in a consonant; digits and Latin letters by how they're read.</summary>
        public static bool HasBatchim(string word, out bool rieul)
        {
            rieul = false;
            if (string.IsNullOrEmpty(word)) return false;
            char last = word[word.Length - 1];
            if (last >= '가' && last <= '힣')
            {
                int final = (last - '가') % 28;
                rieul = final == 8;
                return final != 0;
            }
            switch (last)
            {
                case '0': case '1': case '3': case '6': case '7': case '8': rieul = last == '1' || last == '7' || last == '8'; return true;
                case '2': case '4': case '5': case '9': return false;
            }
            char l = char.ToLowerInvariant(last);
            if (l == 'l') { rieul = true; return true; }
            return l == 'm' || l == 'n' || l == 'k' || l == 'p' || l == 't' || l == 'b' || l == 'g' || l == 'c';
        }
    }
}
