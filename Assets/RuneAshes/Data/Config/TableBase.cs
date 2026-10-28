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
