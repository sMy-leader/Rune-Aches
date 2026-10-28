namespace RuneAshes.Framework
{
    /// <summary>
    /// 可播种随机源。所有玩法随机（掉落/词缀/波次）必须走这里，
    /// 否则种子系统、每日挑战、回放、联机全部要返工。
    /// </summary>
    public interface IRandom
    {
        uint State { get; }

        void Reseed(uint seed);

        uint NextUInt();

        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>[0, 1)</summary>
        float NextFloat();

        float Range(float minInclusive, float maxInclusive);

        bool Chance(float probability);

        T Pick<T>(System.Collections.Generic.IReadOnlyList<T> list);
    }
}
