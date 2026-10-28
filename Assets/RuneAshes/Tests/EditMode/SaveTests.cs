using System;
using System.IO;
using NUnit.Framework;
using RuneAshes.Framework;

namespace RuneAshes.Tests
{
    public sealed class SaveTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "RuneAshesTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void Save_Then_Load_RoundTrips()
        {
            var store = new JsonSaveStore(_directory);
            var data = new SaveFile();
            data.Meta.RuneCoins = 42;
            data.Meta.UnlockedSlots = 4;
            data.Meta.UnlockedRuneIds.Add("E1");
            data.Meta.UnlockedRuneIds.Add("M6");
            data.Settings.MasterVolume = 0.35f;

            store.Save(data);
            var loaded = new JsonSaveStore(_directory).Load();

            Assert.AreEqual(42, loaded.Meta.RuneCoins);
            Assert.AreEqual(4, loaded.Meta.UnlockedSlots);
            CollectionAssert.AreEqual(new[] { "E1", "M6" }, loaded.Meta.UnlockedRuneIds);
            Assert.AreEqual(0.35f, loaded.Settings.MasterVolume, 0.0001f);
            Assert.AreEqual(SaveFile.CurrentVersion, loaded.Version);
            Assert.IsTrue(loaded.SavedAtUnix > 0);
        }

        [Test]
        public void Corrupted_Primary_Falls_Back_To_Backup()
        {
            var store = new JsonSaveStore(_directory);

            var first = new SaveFile();
            first.Meta.RuneCoins = 10;
            store.Save(first);

            var second = new SaveFile();
            second.Meta.RuneCoins = 20;
            store.Save(second); // 产生 .bak（内容为 10）

            File.WriteAllText(store.SavePath, "{ this is broken json");

            var loaded = new JsonSaveStore(_directory).Load();
            Assert.AreEqual(10, loaded.Meta.RuneCoins);
        }

        [Test]
        public void Legacy_Version_Zero_Is_Migrated()
        {
            var legacy = new SaveFile { Version = 0, Meta = null, Settings = null };
            var migrated = SaveMigrations.Migrate(legacy);

            Assert.AreEqual(1, migrated.Version);
            Assert.IsNotNull(migrated.Meta);
            Assert.IsNotNull(migrated.Settings);
        }
    }
}
