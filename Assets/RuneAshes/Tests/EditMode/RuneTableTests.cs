using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class RuneTableTests
    {
        private static string CsvPath
        {
            get { return Path.Combine(Application.dataPath, "RuneAshes/Data/Config/rune.csv"); }
        }

        [Test]
        public void Csv_Parses_All_Twenty_Runes()
        {
            var runes = RuneCsvParser.Parse(File.ReadAllText(CsvPath));

            Assert.AreEqual(20, runes.Count);
            Assert.AreEqual(6, runes.Count(r => r.Category == RuneCategory.Trigger));
            Assert.AreEqual(7, runes.Count(r => r.Category == RuneCategory.Effect));
            Assert.AreEqual(7, runes.Count(r => r.Category == RuneCategory.Modifier));
        }

        [Test]
        public void Csv_Reads_Typed_Fields_And_Params()
        {
            var runes = RuneCsvParser.Parse(File.ReadAllText(CsvPath));
            var e1 = runes.First(r => r.Id == "E1");

            Assert.AreEqual("直射弹", e1.Name);
            Assert.AreEqual(RuneCategory.Effect, e1.Category);
            Assert.AreEqual(3, e1.EnergyCost);
            Assert.AreEqual(12f, e1.BaseValue, 0.001f);
            Assert.AreEqual(2, e1.MaxLevel);
            Assert.AreEqual(20f, e1.GetFloat("speed"), 0.001f);
            Assert.IsTrue(e1.HasTag("core"));
        }

        [Test]
        public void Generated_Table_Matches_Csv()
        {
            var parsed = RuneCsvParser.Parse(File.ReadAllText(CsvPath));
            var generated = new RuneTable(RuneTableData.All);

            Assert.AreEqual(parsed.Count, generated.Count);
            foreach (var rune in parsed)
            {
                var fromTable = generated.Get(rune.Id);
                Assert.AreEqual(rune.Name, fromTable.Name, rune.Id);
                Assert.AreEqual(rune.Category, fromTable.Category, rune.Id);
                Assert.AreEqual(rune.EnergyCost, fromTable.EnergyCost, rune.Id);
                Assert.AreEqual(rune.BaseValue, fromTable.BaseValue, 0.001f, rune.Id);
            }
        }

        [Test]
        public void Generated_Table_Passes_Validation()
        {
            var provider = new TableProvider();
            provider.LoadAll();

            CollectionAssert.IsEmpty(provider.Validate());
        }

        [Test]
        public void Duplicate_Id_Throws()
        {
            var a = RuneDef.Create("X1", "A", RuneCategory.Trigger, 0, 1f, 2, "", new string[0], new string[0], new string[0]);
            var b = RuneDef.Create("X1", "B", RuneCategory.Trigger, 0, 1f, 2, "", new string[0], new string[0], new string[0]);

            Assert.Throws<InvalidOperationException>(() => new RuneTable(new List<RuneDef> { a, b }));
        }

        [Test]
        public void Unknown_Category_Throws_With_Line_Number()
        {
            const string csv = "id,name,category,energy_cost,base_value,max_level,tags,params,desc\n" +
                               "X1,Bad,Nope,0,1,2,,,bad row\n";

            var ex = Assert.Throws<FormatException>(() => RuneCsvParser.Parse(csv));
            StringAssert.Contains("Line 2", ex.Message);
        }
    }
}
