// ─────────────────────────────────────────────────────────────────────────────
// 【链路】所有玩法随机（掉落/词缀/波次/弹道扰动）──> IRandom（本文件）
//        ──> XorShiftRandom（默认实现）
// 【为什么现在就要可播种】如果先用 UnityEngine.Random，将来做种子系统/每日挑战/回放时
//                        所有随机调用点都要重写。这个接口从第一天就在，成本几乎为零。
// 【纪律】Job/Burst 里不能调用 UnityEngine.Random；请用这里的实现或 NativeRandom。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Framework
{
    /// <summary>
    /// 可播种随机源。所有玩法随机（掉落/词缀/波次）必须走这里，
    /// 否则种子系统、每日挑战、回放、联机全部要返工。
    /// </summary>
    public interface IRandom
    {
        /// <summary>当前内部状态。存档/回放时保存它，就能从同一位置继续序列。</summary>
        uint State { get; }

        void Reseed(uint seed);

        uint NextUInt();

        /// <summary>[minInclusive, maxExclusive)。maxExclusive 传入非正值时返回 min。</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>[0, 1)</summary>
        float NextFloat();

        float Range(float minInclusive, float maxInclusive);

        bool Chance(float probability);

        T Pick<T>(System.Collections.Generic.IReadOnlyList<T> list);
    }
}
