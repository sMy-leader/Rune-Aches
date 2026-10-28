using System.Collections.Generic;
using NUnit.Framework;
using RuneAshes.Data;
using RuneAshes.Gameplay;

namespace RuneAshes.Tests
{
    public sealed class RuneModifierFactoryTests
    {
        private RuneTable _runes;
        private RuneModifierFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _runes = new RuneTable(RuneTableData.All);
            _factory = new RuneModifierFactory();
        }

        private ModifierSet Build(params string[] ids)
        {
            var defs = new List<RuneDef>();
            foreach (var id in ids)
            {
                defs.Add(_runes.Get(id));
            }

            return _factory.Build(defs);
        }

        [Test]
        public void Empty_Modifiers_Produce_Zero_Set()
        {
            var set = _factory.Build(null);
            Assert.IsFalse(set.HasAny);
        }

        [Test]
        public void Split_Reads_Count_And_Inherit()
        {
            var set = Build("M1");
            Assert.AreEqual(3, set.SplitCount);
            Assert.AreEqual(0.6f, set.SplitInherit, 0.001f);
        }

        [Test]
        public void Bounce_Reads_Bounces_And_Falloff()
        {
            var set = Build("M2");
            Assert.AreEqual(2, set.Bounces);
            Assert.AreEqual(0.2f, set.BounceFalloff, 0.001f);
        }

        [Test]
        public void Pierce_Reads_Count()
        {
            Assert.AreEqual(2, Build("M3").Pierce);
        }

        [Test]
        public void Burn_Reads_Dps_And_Duration()
        {
            var set = Build("M4");
            Assert.AreEqual(4f, set.BurnDps, 0.001f);
            Assert.AreEqual(3f, set.BurnDuration, 0.001f);
        }

        [Test]
        public void Explosion_Uses_Radius_And_Base_Value()
        {
            var set = Build("M6");
            Assert.AreEqual(2.5f, set.ExplosionRadius, 0.001f);
            Assert.AreEqual(15f, set.ExplosionDamage, 0.001f);
        }

        [Test]
        public void Homing_Reads_Turn_Rate()
        {
            Assert.AreEqual(180f, Build("M7").TurnRate, 0.001f);
        }

        [Test]
        public void Combined_Modifiers_Merge()
        {
            var set = Build("M1", "M4", "M6");
            Assert.AreEqual(3, set.SplitCount);
            Assert.AreEqual(4f, set.BurnDps, 0.001f);
            Assert.AreEqual(2.5f, set.ExplosionRadius, 0.001f);
            Assert.IsTrue(set.HasAny);
        }
    }
}
