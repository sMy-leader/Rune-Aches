using NUnit.Framework;
using RuneAshes.Gameplay;

namespace RuneAshes.Tests
{
    public sealed class ProcGuardTests
    {
        [Test]
        public void Budget_Allows_Capacity_Then_Drops()
        {
            var budget = new ProcBudget(3, 0.1f);

            Assert.IsTrue(budget.TryConsume());
            Assert.IsTrue(budget.TryConsume());
            Assert.IsTrue(budget.TryConsume());
            Assert.IsFalse(budget.TryConsume());
            Assert.AreEqual(1, budget.Dropped);
        }

        [Test]
        public void Budget_Resets_After_Window()
        {
            var budget = new ProcBudget(1, 0.1f);

            Assert.IsTrue(budget.TryConsume());
            Assert.IsFalse(budget.TryConsume());

            budget.Tick(0.11f);
            Assert.IsTrue(budget.TryConsume());
        }

        [Test]
        public void SameFrameGate_Merges_Only_Same_Chain_In_Same_Frame()
        {
            var gate = new SameFrameGate();

            Assert.IsFalse(gate.ShouldMerge(1, 100));
            Assert.IsTrue(gate.ShouldMerge(1, 100));
            Assert.IsFalse(gate.ShouldMerge(2, 100));
            Assert.IsFalse(gate.ShouldMerge(2, 101));
        }

        [Test]
        public void Chain_Depth_Is_Capped()
        {
            Assert.IsTrue(RuneRules.CanTrigger(0));
            Assert.IsTrue(RuneRules.CanTrigger(1));
            Assert.IsTrue(RuneRules.CanTrigger(2));
            Assert.IsFalse(RuneRules.CanTrigger(3));
        }
    }
}
