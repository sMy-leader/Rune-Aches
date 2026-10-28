using System;
using System.Collections.Generic;

namespace RuneAshes.Data
{
    public enum RuneCategory
    {
        Trigger,
        Effect,
        Modifier
    }

    /// <summary>
    /// 单个符文的运行时定义。数据来自 rune.csv，由 RuneCsvCodeGen 生成 RuneTable.g.cs。
    /// 纯 C#，不引用 UnityEngine，便于单测与将来迁移 Luban。
    /// </summary>
    public sealed class RuneDef
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public RuneCategory Category { get; private set; }
        public int EnergyCost { get; private set; }
        public float BaseValue { get; private set; }
        public int MaxLevel { get; private set; }
        public string Description { get; private set; }
        public IReadOnlyList<string> Tags { get; private set; }
        public IReadOnlyDictionary<string, string> Params { get; private set; }

        private RuneDef()
        {
        }

        public static RuneDef Create(
            string id,
            string name,
            RuneCategory category,
            int energyCost,
            float baseValue,
            int maxLevel,
            string description,
            string[] tags,
            string[] paramKeys,
            string[] paramValues)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Rune id is required.", nameof(id));
            }

            if (paramKeys == null)
            {
                paramKeys = Array.Empty<string>();
            }

            if (paramValues == null)
            {
                paramValues = Array.Empty<string>();
            }

            if (paramKeys.Length != paramValues.Length)
            {
                throw new ArgumentException("paramKeys/paramValues length mismatch for rune " + id);
            }

            var parameters = new Dictionary<string, string>(paramKeys.Length, StringComparer.Ordinal);
            for (var i = 0; i < paramKeys.Length; i++)
            {
                parameters[paramKeys[i]] = paramValues[i];
            }

            return new RuneDef
            {
                Id = id,
                Name = name ?? string.Empty,
                Category = category,
                EnergyCost = energyCost,
                BaseValue = baseValue,
                MaxLevel = maxLevel,
                Description = description ?? string.Empty,
                Tags = tags ?? Array.Empty<string>(),
                Params = parameters
            };
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            for (var i = 0; i < Tags.Count; i++)
            {
                if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetParam(string key, string fallback = "")
        {
            string value;
            return Params.TryGetValue(key, out value) ? value : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            int value;
            return int.TryParse(GetParam(key), out value) ? value : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            float value;
            return float.TryParse(GetParam(key), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value)
                ? value
                : fallback;
        }

        public override string ToString()
        {
            return Id + " " + Name + " (" + Category + ", cost " + EnergyCost + ")";
        }
    }
}
