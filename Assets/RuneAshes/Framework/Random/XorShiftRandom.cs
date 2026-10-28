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

        public uint NextUInt()
        {
            var x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                return minInclusive;
            }

            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            // 24 位精度，落在 [0, 1)
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
