// ─────────────────────────────────────────────────────────────────────────────
// 【链路】（W3）UnityRuneWorld 每帧重建 ──> SpatialHashGrid.Insert(敌人)
//        ──> Projectile.TryHit / OrbitingOrbs / Mine 用 Query 代替 OverlapSphere
// 【职责】俯视平面（XZ）的空间哈希：把"全场 N 个敌人"切成格子，
//        查询时只遍历附近几个格子，而不是遍历全场。
// 【为什么忽略 Y】这是俯视弹幕游戏，所有战斗几乎发生在同一高度；
//                用 2D 哈希比 3D 更省内存、更少空桶。
// 【现状】W2 只有逻辑 + 单测，还没接到弹丸；W3 接上后会替换 OverlapSphere。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 俯视平面（XZ）空间哈希：格子粗筛 + 精确半径过滤。
    /// W2 先作为可测的纯逻辑存在，W3 接入 Job 化弹幕碰撞。
    /// </summary>
    public sealed class SpatialHashGrid<T>
    {
        private struct Entry
        {
            public Vector3 Position;
            public T Item;
        }

        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly Dictionary<long, List<Entry>> _cells = new Dictionary<long, List<Entry>>();

        /// <summary>
        /// cellSize 是唯一需要调的参数：
        /// 太小 → 查询要遍历很多格子；太大 → 每个格子里的候选太多。
        /// 经验值取"常见查询半径"的 1~2 倍（本作用 2m 左右比较合适）。
        /// </summary>
        public SpatialHashGrid(float cellSize)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
            _inverseCellSize = 1f / _cellSize;
        }

        public int CellCount
        {
            get { return _cells.Count; }
        }

        public float CellSize
        {
            get { return _cellSize; }
        }

        public void Clear()
        {
            foreach (var bucket in _cells.Values)
            {
                bucket.Clear();
            }

            _cells.Clear();
        }

        public void Insert(Vector3 position, T item)
        {
            var key = Key(CellX(position.x), CellZ(position.z));
            List<Entry> bucket;
            if (!_cells.TryGetValue(key, out bucket))
            {
                bucket = new List<Entry>(8);
                _cells.Add(key, bucket);
            }

            bucket.Add(new Entry { Position = position, Item = item });
        }

        /// <summary>
        /// 返回半径内的所有元素（只比较 XZ，忽略高度）。
        /// 两步：① 按半径算出要扫的格子范围；② 对格子内每个候选做精确距离过滤。
        /// [坑] results 由调用方传入并会先 Clear，方便复用；不要传共享列表给多个查询。
        /// </summary>
        public void Query(Vector3 center, float radius, List<T> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            var radiusSqr = radius * radius;
            var minX = CellX(center.x - radius);
            var maxX = CellX(center.x + radius);
            var minZ = CellZ(center.z - radius);
            var maxZ = CellZ(center.z + radius);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    List<Entry> bucket;
                    if (!_cells.TryGetValue(Key(x, z), out bucket))
                    {
                        continue;
                    }

                    for (var i = 0; i < bucket.Count; i++)
                    {
                        var entry = bucket[i];
                        var dx = entry.Position.x - center.x;
                        var dz = entry.Position.z - center.z;
                        if (dx * dx + dz * dz <= radiusSqr)
                        {
                            results.Add(entry.Item);
                        }
                    }
                }
            }
        }

        private int CellX(float value)
        {
            return Mathf.FloorToInt(value * _inverseCellSize);
        }

        private int CellZ(float value)
        {
            return Mathf.FloorToInt(value * _inverseCellSize);
        }

        private static long Key(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
