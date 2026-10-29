using NUnit.Framework;
using RuneAshes.Gameplay;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class ExperienceTests
    {
        [Test]
        public void AddXp_LevelsUp_AndKeepsRemainder()
        {
            var go = new GameObject("xp_test");
            var xp = go.AddComponent<ExperienceSystem>();
            xp.ResetRun();

            var requirement = xp.XpToNext;
            xp.AddXp(requirement + 1);

            Assert.AreEqual(2, xp.Level);
            Assert.AreEqual(1, xp.CurrentXp);
            Assert.Greater(xp.XpToNext, requirement);

            Object.DestroyImmediate(go);
        }
    }
}
