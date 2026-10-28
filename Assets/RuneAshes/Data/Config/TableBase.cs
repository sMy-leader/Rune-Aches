// ─────────────────────────────────────────────────────────────────────────────
// 【链路】RuneTable.g.cs（生成数据）──> new RuneTable(rows) ──> TableBase 建索引
// 【职责】所有配置表的共同基类：构造函数里一次性建 id → 行的字典，并做唯一性校验。
// 【为什么用基类】符文表、将来的敌人表/波次表都只需要"按 id 查 + 遍历 + 筛选"，
//                把重复逻辑收在这里，业务表只写自己特有的查询（如 ByCategory）。
// 【约束】构造完之后表是只读的：运行期不再增删行，避免"查表查到一半表变了"。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace RuneAshes.Data
{
    /// <summary>
    /// 所有配置表的基类：构造时建立 id 索引并做唯一性校验。
    /// 业务代码只依赖这个抽象，未来替换 Luban 生成器时不需要改调用点。
    /// </summary>
    public abstract class TableBase<T> where T : class
    {
        private readonly Dictionary<string, T> _rows;

        /// <summary>
        /// [坑] 重复 id / 空 id 在这里直接抛，而不是留到运行期覆盖。
        /// 表格类 bug 越早暴露越便宜——尤其是生成器出错时。
        /// </summary>
        protected TableBase(IEnumerable<T> rows, Func<T, string> idSelector)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            if (idSelector == null)
            {
                throw new ArgumentNullException(nameof(idSelector));
            }

            _rows = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                var id = idSelector(row);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException("Table " + typeof(T).Name + " has a row with an empty id.");
                }

                if (_rows.ContainsKey(id))
                {
                    throw new InvalidOperationException("Table " + typeof(T).Name + " has duplicate id '" + id + "'.");
                }

                _rows.Add(id, row);
            }
        }

        public int Count
        {
            get { return _rows.Count; }
        }

        public IReadOnlyCollection<T> All
        {
            get { return _rows.Values; }
        }

        /// <summary>
        /// 查不到会抛异常。只用于"这个 id 一定存在"的场合（如生成器产出的常量）；
        /// 玩家输入/存档数据一律用 TryGet，不要让坏存档把游戏炸掉。
        /// </summary>
        public T Get(string id)
        {
            T value;
            if (id != null && _rows.TryGetValue(id, out value))
            {
                return value;
            }

            throw new KeyNotFoundException("Table " + typeof(T).Name + " has no id '" + id + "'.");
        }

        public bool TryGet(string id, out T value)
        {
            if (id == null)
            {
                value = null;
                return false;
            }

            return _rows.TryGetValue(id, out value);
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            foreach (var row in _rows.Values)
            {
                if (predicate(row))
                {
                    yield return row;
                }
            }
        }
    }
}
