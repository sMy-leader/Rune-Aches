// ─────────────────────────────────────────────────────────────────────────────
// 【链路】IRandom ──> XorShiftRandom（本文件，默认实现）
// 【算法】xorshift32：三次移位异或，周期 2^32-1，零分配、跨平台一致。
// 【不追求】密码学强度、长周期、无偏分布；玩法随机不需要这些。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace RuneAshes.Framework
{
    /// <summary>
    /// xorshift32：零分配、可播种、跨平台结果一致。
    /// 不追求密码学强度，只要求确定性（同种子同序列）。
    /// </summary>
    public sealed class XorShiftRandom : IRandom
    {
        /// <summary>
        /// 0 会让 xorshift 永远输出 0（吸收态），所以把 0 映射到一个非零常量。
        /// 这不是"更好的种子"，只是保证算法不会退化。
        /// </summary>
        private const uint FallbackSeed = 0x9E3779B9u;

        public XorShiftRandom(uint seed)
        {
            Reseed(seed);
        }

        public uint State { get; private set; }

        public void Reseed(uint seed)
        {
            State = seed == 0u ? FallbackSeed : seed;
        }

        /// <summary>核心三步。所有其他方法都建立在它之上。</summary>
        public uint NextUInt()
        {
            var x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }

        /// <summary>
        /// [注意] 取模会带来极小偏差（range 不是 2 的幂时），
        /// 对掉落/波次这种玩法随机完全可接受；不要拿它做公平性要求高的抽奖。
        /// </summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        /// <summary>
        /// 取高 24 位再除以 2^24：float 只有 24 位尾数，这样得到精确的 [0,1) 且不会等于 1。
        /// </summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float minInclusive, float maxInclusive)
        {
            return minInclusive + (maxInclusive - minInclusive) * NextFloat();
        }

        public bool Chance(float probability)
        {
            if (probability <= 0f)
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            return NextFloat() < probability;
        }

        public T Pick<T>(IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("Cannot pick from an empty list.", nameof(list));
            }

            return list[NextInt(0, list.Count)];
        }
    }
}
