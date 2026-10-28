using System.Collections.Generic;
using NUnit.Framework;
using RuneAshes.Framework;

namespace RuneAshes.Tests
{
    public sealed class RandomTests
    {
        [Test]
        public void Same_Seed_Produces_Same_Sequence()
        {
            var a = new XorShiftRandom(12345u);
            var b = new XorShiftRandom(12345u);

            for (var i = 0; i < 256; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt(), "Mismatch at index " + i);
            }
        }

        [Test]
        public void Different_Seed_Produces_Different_Sequence()
        {
            var a = new XorShiftRandom(1u);
            var b = new XorShiftRandom(2u);

            var same = 0;
            for (var i = 0; i < 256; i++)
            {
                if (a.NextUInt() == b.NextUInt())
                {
                    same++;
                }
            }

            Assert.Less(same, 2);
        }

        [Test]
        public void NextFloat_Stays_In_Range()
        {
            var random = new XorShiftRandom(7u);
            for (var i = 0; i < 10000; i++)
            {
                var value = random.NextFloat();
                Assert.GreaterOrEqual(value, 0f);
                Assert.Less(value, 1f);
            }
        }

        [Test]
        public void NextInt_Stays_In_Range_And_Covers_Bounds()
        {
            var random = new XorShiftRandom(99u);
            var seen = new HashSet<int>();

            for (var i = 0; i < 5000; i++)
            {
                var value = random.NextInt(-3, 4);
                Assert.GreaterOrEqual(value, -3);
                Assert.Less(value, 4);
                seen.Add(value);
            }

            Assert.AreEqual(7, seen.Count);
        }

        [Test]
        public void Zero_Seed_Is_Remapped_To_Keep_State_NonZero()
        {
            var random = new XorShiftRandom(0u);
            Assert.AreNotEqual(0u, random.State);
            Assert.AreNotEqual(0u, random.NextUInt());
        }
    }
}
