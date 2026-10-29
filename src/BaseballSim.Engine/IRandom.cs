namespace BaseballSim
{
    /// <summary>Random source, so games can be replayed from a seed and tests stay deterministic.</summary>
    public interface IRandom
    {
        /// <summary>Uniform in [0, 1).</summary>
        double NextDouble();
    }

    public class SeededRandom : IRandom
    {
        readonly System.Random _random;

        public SeededRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        public double NextDouble() => _random.NextDouble();
    }
}
