using System.Collections.Generic;
using NUnit.Framework;
using RuneAshes.Data;
using RuneAshes.Gameplay;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class RuneEffectTests
    {
        private sealed class FakeWorld : IRuneWorld
        {
            public readonly List<ProjectileSpec> Projectiles = new List<ProjectileSpec>();
            public readonly List<OrbitSpec> Orbits = new List<OrbitSpec>();
            public readonly List<MineSpec> Mines = new List<MineSpec>();
            public readonly List<LineDamageSpec> Lines = new List<LineDamageSpec>();
            public readonly List<AreaDamageSpec> Areas = new List<AreaDamageSpec>();

            public void SpawnProjectile(ProjectileSpec spec) { Projectiles.Add(spec); }
            public void SpawnOrbitingOrbs(OrbitSpec spec) { Orbits.Add(spec); }
            public void SpawnMine(MineSpec spec) { Mines.Add(spec); }
            public void DamageLine(LineDamageSpec spec) { Lines.Add(spec); }
            public void DamageArea(AreaDamageSpec spec) { Areas.Add(spec); }
            public bool TryGetNearestTarget(Vector3 origin, float maxDistance, out Vector3 position)
            {
                position = origin + Vector3.forward;
                return true;
            }
        }

        private RuneTable _runes;

        [SetUp]
        public void SetUp()
        {
            _runes = new RuneTable(RuneTableData.All);
        }

        /// <summary>
        /// BaseValue 模拟 RuneCaster 填入的"本次施法有效基础值"（含局内强化）。
        /// </summary>
        private RuneEffectContext Context(FakeWorld world, Vector3 direction, float baseValue)
        {
            return new RuneEffectContext
            {
                Origin = Vector3.zero,
                Direction = direction,
                BaseValue = baseValue,
                ChainDepth = 0,
                Seed = 1u,
                Meta = new CastMeta { ChainId = 1, CastId = 1, Depth = 0, Seed = 1u },
                World = world
            };
        }

        [Test]
        public void E1_Fires_One_Projectile_With_Table_Values()
        {
            var world = new FakeWorld();
            var rune = _runes.Get("E1");
            var context = Context(world, Vector3.forward, rune.BaseValue);
            new DirectShotEffect(rune).Execute(ref context);

            Assert.AreEqual(1, world.Projectiles.Count);
            Assert.AreEqual(12f, world.Projectiles[0].Damage, 0.001f);
            Assert.AreEqual(20f, world.Projectiles[0].Speed, 0.001f);
            Assert.AreEqual(Vector3.forward, world.Projectiles[0].Direction);
        }

        [Test]
        public void E2_Fires_Five_Projectiles_In_Arc()
        {
            var world = new FakeWorld();
            var rune = _runes.Get("E2");
            var context = Context(world, Vector3.forward, rune.BaseValue);
            new SpreadShotEffect(rune).Execute(ref context);

            Assert.AreEqual(5, world.Projectiles.Count);
            Assert.Less(world.Projectiles[0].Direction.x, 0f);
            Assert.Greater(world.Projectiles[4].Direction.x, 0f);
            Assert.AreEqual(8f, world.Projectiles[0].Damage, 0.001f);
        }

        [Test]
        public void E3_Requests_Two_Orbs()
        {
            var world = new FakeWorld();
            var rune = _runes.Get("E3");
            var context = Context(world, Vector3.forward, rune.BaseValue);
            new OrbitingOrbEffect(rune).Execute(ref context);

            Assert.AreEqual(1, world.Orbits.Count);
            Assert.AreEqual(2, world.Orbits[0].Count);
            Assert.AreEqual(10f, world.Orbits[0].Damage, 0.001f);
            Assert.AreEqual(2.5f, world.Orbits[0].Radius, 0.001f);
        }

        [Test]
        public void E4_Drops_Mine_At_Origin()
        {
            var world = new FakeWorld();
            var rune = _runes.Get("E4");
            var context = Context(world, Vector3.forward, rune.BaseValue);
            new MineEffect(rune).Execute(ref context);

            Assert.AreEqual(1, world.Mines.Count);
            Assert.AreEqual(25f, world.Mines[0].Damage, 0.001f);
            Assert.AreEqual(3f, world.Mines[0].Radius, 0.001f);
            Assert.AreEqual(Vector3.zero, world.Mines[0].Position);
        }

        [Test]
        public void E5_Applies_Line_Damage_Along_Dash()
        {
            var world = new FakeWorld();
            var rune = _runes.Get("E5");
            var context = Context(world, Vector3.right, rune.BaseValue);
            new DashSlashEffect(rune).Execute(ref context);

            Assert.AreEqual(1, world.Lines.Count);
            Assert.AreEqual(20f, world.Lines[0].Damage, 0.001f);
            Assert.AreEqual(4f, world.Lines[0].Length, 0.001f);
            Assert.AreEqual(Vector3.right, world.Lines[0].Direction);
        }

        [Test]
        public void Factory_Rejects_Unknown_Effect()
        {
            var fake = RuneDef.Create("X9", "Fake", RuneCategory.Effect, 1, 1f, 0, "", new string[0], new string[0], new string[0]);
            Assert.Throws<System.NotSupportedException>(() => new RuneEffectFactory().Create(fake));
        }
    }
}
