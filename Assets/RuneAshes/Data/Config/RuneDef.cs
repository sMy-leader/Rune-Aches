// ─────────────────────────────────────────────────────────────────────────────
// 【链路】rune.csv ──(RuneCsvCodeGen)──> RuneTable.g.cs ──> TableProvider.Runes
//        ──> ChainCompiler.Compile() ──> CompiledChain ──> RuneCaster 施法
// 【职责】一个符文在内存里的只读定义。这里只描述"是什么"，不写任何玩法逻辑。
// 【约束】Data 层 asmdef 设了 noEngineReferences=true，所以不能 using UnityEngine。
//        这不是遗漏，而是为了：纯 NUnit 可测 + 将来可整体换成 Luban + 可进热更程序集。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace RuneAshes.Data
{
    /// <summary>
    /// 三类符文。玩家拼链的规则是：1 个 Trigger + 1 个 Effect + N 个 Modifier。
    /// 校验它们"能不能放在对应槽位"的地方在 ChainCompiler，不在这里。
    /// </summary>
    public enum RuneCategory
    {
        Trigger,
        Effect,
        Modifier
    }

    /// <summary>
    /// 单个符文的运行时定义。数据来自 rune.csv，由 RuneCsvCodeGen 生成 RuneTable.g.cs。
    /// </summary>
    public sealed class RuneDef
    {
        /// <summary>唯一 ID（T1/E1/M4…），存档、分享码、编译校验都用它，不要用 Name 做逻辑。</summary>
        public string Id { get; private set; }

        /// <summary>给玩家看的名字，可以随时改、可以本地化；代码里禁止拿它做判断。</summary>
        public string Name { get; private set; }

        public RuneCategory Category { get; private set; }

        /// <summary>装配链时的能量开销，由 ChainCompiler 累加后与上限比较。</summary>
        public int EnergyCost { get; private set; }

        /// <summary>
        /// 基础数值，含义随类别变化：
        /// Effect = 一次施法的基础伤害；Modifier = 该修饰的基准强度（例如 M6 的爆炸伤害）；
        /// Trigger = 目前不用（触发器读 Params 里的 interval）。
        /// 注意：运行时真正生效的是 RuneCaster 填进 RuneEffectContext.BaseValue 的值，
        /// 将来等级强化会在这里做乘法，所以效果代码不要自己去读 BaseValue 字段。
        /// </summary>
        public float BaseValue { get; private set; }

        /// <summary>可升级上限；0 表示当前版本不可升级（W2 还没接等级）。</summary>
        public int MaxLevel { get; private set; }

        public string Description { get; private set; }

        /// <summary>标签（如 "projectile"/"dot"），用于三选一筛选与将来的协同判定。</summary>
        public IReadOnlyList<string> Tags { get; private set; }

        /// <summary>
        /// 未结构化的键值参数（CSV 的 params 列），例如 interval=0.8、count=5。
        /// 用字符串保存是为了"加一个新参数不用改代码生成器"；
        /// 读取时请用 GetInt/GetFloat 并给默认值，避免表里漏填导致玩法崩。
        /// </summary>
        public IReadOnlyDictionary<string, string> Params { get; private set; }

        private RuneDef()
        {
        }

        /// <summary>
        /// 唯一构造入口。所有参数校验都放在这里，坏数据在"加载表"时就炸，
        /// 而不是等玩家打到第 8 分钟才 NullReference。
        /// 生成器（RuneCsvCodeGen）产出的是合法数据，所以这里抛异常属于开发期保护。
        /// </summary>
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

        /// <summary>标签比较大小写不敏感，方便 CSV 里写 Projectile / projectile。</summary>
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

        /// <summary>
        /// 读字符串参数。表里漏填时返回 fallback，不抛异常——
        /// 这样策划改表漏一列不会让整局游戏起不来。
        /// </summary>
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

        /// <summary>
        /// 读浮点参数。[坑] 必须用 InvariantCulture：CSV 里小数点固定是 "."，
        /// 而玩家系统的区域设置可能用 ","（如德语/法语），用默认文化会静默解析失败。
        /// </summary>
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
