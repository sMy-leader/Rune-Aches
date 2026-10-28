using System.Collections.Generic;
using NUnit.Framework;
using RuneAshes.Gameplay;
using UnityEngine;

namespace RuneAshes.Tests
{
    public sealed class SpatialHashGridTests
    {
        [Test]
        public void Query_Returns_Only_Neighbouring_Cells()
        {
            var grid = new SpatialHashGrid<int>(2f);
            grid.Insert(new Vector3(0f, 0f, 0f), 1);
            grid.Insert(new Vector3(3f, 0f, 0f), 2);
            grid.Insert(new Vector3(5f, 0f, 5f), 3);

            var results = new List<int>();

            grid.Query(Vector3.zero, 1.5f, results);
            CollectionAssert.AreEquivalent(new[] { 1 }, results);

            grid.Query(new Vector3(3f, 0f, 0f), 1.5f, results);
            CollectionAssert.AreEquivalent(new[] { 2 }, results);
        }

        [Test]
        public void Larger_Radius_Spans_Multiple_Cells()
        {
            var grid = new SpatialHashGrid<int>(2f);
            grid.Insert(new Vector3(0f, 0f, 0f), 1);
            grid.Insert(new Vector3(3f, 0f, 0f), 2);
            grid.Insert(new Vector3(5f, 0f, 5f), 3);

            var results = new List<int>();
            grid.Query(Vector3.zero, 4f, results);

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, results);
        }

        [Test]
        public void Negative_Coordinates_Map_To_Negative_Cells()
        {
            var grid = new SpatialHashGrid<int>(1f);
            grid.Insert(new Vector3(-0.5f, 0f, -0.5f), 7);

            var results = new List<int>();
            grid.Query(new Vector3(-0.5f, 0f, -0.5f), 0.1f, results);

            CollectionAssert.AreEquivalent(new[] { 7 }, results);
        }

        [Test]
        public void Clear_Empties_All_Cells()
        {
            var grid = new SpatialHashGrid<int>(2f);
            grid.Insert(Vector3.zero, 1);
            grid.Insert(new Vector3(4f, 0f, 4f), 2);
            Assert.AreEqual(2, grid.CellCount);

            grid.Clear();
            Assert.AreEqual(0, grid.CellCount);

            var results = new List<int>();
            grid.Query(Vector3.zero, 100f, results);
            Assert.IsEmpty(results);
        }
    }
}
