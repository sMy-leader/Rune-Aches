using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RuneAshes.Data;
using RuneAshes.Gameplay;

namespace RuneAshes.Tests
{
    public sealed class ChainCompilerTests
    {
        private RuneTable _runes;
        private ChainCompiler _compiler;

        [SetUp]
        public void SetUp()
        {
            _runes = new RuneTable(RuneTableData.All);
            _compiler = new ChainCompiler(_runes);
        }

        [Test]
        public void Valid_Four_Slot_Chain_Compiles()
        {
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E1",
                ModifierIds = new List<string> { "M2", "M4" }
            });

            Assert.IsTrue(chain.IsValid, string.Join(" | ", chain.Errors));
            Assert.AreEqual(4, chain.SlotCount);
            Assert.AreEqual(8, chain.EnergyCost);
            Assert.AreEqual("E1", chain.Effect.Id);
            Assert.AreEqual(2, chain.Modifiers.Count);
        }

        [Test]
        public void Energy_Overflow_Is_Reported()
        {
            // T2(2) + E1(3) + M1(3) + M6(3) = 11 > 10
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "T2",
                EffectId = "E1",
                ModifierIds = new List<string> { "M1", "M6" }
            });

            Assert.IsFalse(chain.IsValid);
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("Energy")));
        }

        [Test]
        public void Slot_Overflow_Is_Reported()
        {
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E1",
                ModifierIds = new List<string> { "M1", "M2", "M3" }
            });

            Assert.IsFalse(chain.IsValid);
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("Slots")));
        }

        [Test]
        public void Duplicate_Modifier_Is_Rejected()
        {
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "T1",
                EffectId = "E1",
                ModifierIds = new List<string> { "M1", "M1" }
            });

            Assert.IsFalse(chain.IsValid);
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("Duplicate modifier")));
        }

        [Test]
        public void Wrong_Category_Is_Rejected()
        {
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "E1",
                EffectId = "T1",
                ModifierIds = new List<string>()
            });

            Assert.IsFalse(chain.IsValid);
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("not a trigger")));
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("not an effect")));
        }

        [Test]
        public void Unknown_Id_Is_Rejected()
        {
            var chain = _compiler.Compile(new ChainBlueprint
            {
                TriggerId = "NOPE",
                EffectId = "E1",
                ModifierIds = new List<string> { "ALSO_NOPE" }
            });

            Assert.IsFalse(chain.IsValid);
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("Unknown trigger")));
            Assert.IsTrue(chain.Errors.Any(e => e.Contains("Unknown modifier")));
        }

        [Test]
        public void Three_Baseline_Builds_All_Compile()
        {
            var builds = new[]
            {
                new ChainBlueprint { TriggerId = "T1", EffectId = "E2", ModifierIds = new List<string> { "M7" } },
                new ChainBlueprint { TriggerId = "T4", EffectId = "E4", ModifierIds = new List<string> { "M6" } },
                new ChainBlueprint { TriggerId = "T6", EffectId = "E3", ModifierIds = new List<string> { "M5" } }
            };

            foreach (var build in builds)
            {
                var chain = _compiler.Compile(build);
                Assert.IsTrue(chain.IsValid, build.TriggerId + "/" + build.EffectId + ": " + string.Join(" | ", chain.Errors));
            }
        }
    }
}
