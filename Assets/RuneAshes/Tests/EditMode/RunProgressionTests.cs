using System.Collections.Generic;
using NUnit.Framework;
using RuneAshes.Data;
using RuneAshes.Gameplay;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class RunProgressionTests
    {
        [Test]
        public void Choices_Include_All_Implemented_Categories_And_Exclude_E6_E7()
        {
            var go = new GameObject("RunProgressionTest");
            var progression = go.AddComponent<RunProgression>();

            var choices = progression.GetChoices(20);
            var categories = new HashSet<RuneCategory>();
            var ids = new HashSet<string>();

            foreach (var rune in choices)
            {
                categories.Add(rune.Category);
                ids.Add(rune.Id);
                Assert.AreNotEqual("E6", rune.Id);
                Assert.AreNotEqual("E7", rune.Id);
            }

            Assert.IsTrue(categories.Contains(RuneCategory.Trigger));
            Assert.IsTrue(categories.Contains(RuneCategory.Effect));
            Assert.IsTrue(categories.Contains(RuneCategory.Modifier));
            Assert.AreEqual(choices.Count, ids.Count);

            Object.DestroyImmediate(go);
        }
    }
}
