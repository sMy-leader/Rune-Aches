// ─────────────────────────────────────────────────────────────────────────────
// 【链路】RuneTable.g.cs ──> TableProvider.LoadAll() ──> .Runes ──> ChainCompiler
// 【职责】配置表的"访问入口"。业务代码只认 ITableProvider，不认具体实现。
// 【为什么多这一层】将来换 Luban / 热更配置时，只替换本文件，调用方一行不改。
// 【启动流程】AppBootstrap / RuneCaster 会 LoadAll()，然后 Validate() 打印全部问题。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;

namespace RuneAshes.Data
{
    public sealed class TableProvider : ITableProvider
    {
        public RuneTable Runes { get; private set; }

        /// <summary>
        /// 当前从"编译进包里的生成常量"加载。将来接 YooAsset/热更时，
        /// 改成读 bytes 再走同一套 new RuneTable(rows) 即可，接口不变。
        /// </summary>
        public void LoadAll()
        {
            Load(RuneTableData.All);
        }

        public void Load(IEnumerable<RuneDef> runes)
        {
            Runes = new RuneTable(runes);
        }

        /// <summary>
        /// 启动校验：返回"全部"错误而不是抛第一个，方便一次性改完表。
        /// 这里只做结构/数值合法性，不做玩法平衡判断（那是设计文档的事）。
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();

            if (Runes == null)
            {
                errors.Add("Rune table is not loaded. Call LoadAll() first.");
                return errors;
            }

            if (Runes.Count == 0)
            {
                errors.Add("Rune table is empty.");
                return errors;
            }

            foreach (var rune in Runes.All)
            {
                if (string.IsNullOrWhiteSpace(rune.Name))
                {
                    errors.Add(rune.Id + ": name is empty.");
                }

                if (rune.EnergyCost < 0)
                {
                    errors.Add(rune.Id + ": energy_cost must be >= 0.");
                }

                if (rune.MaxLevel < 0)
                {
                    errors.Add(rune.Id + ": max_level must be >= 0.");
                }

                if (float.IsNaN(rune.BaseValue) || float.IsInfinity(rune.BaseValue))
                {
                    errors.Add(rune.Id + ": base_value must be finite.");
                }

                if (rune.Category == RuneCategory.Effect && rune.EnergyCost == 0)
                {
                    errors.Add(rune.Id + ": effect runes must cost energy (0 would make them free picks).");
                }

                if (rune.Category == RuneCategory.Modifier && rune.EnergyCost == 0)
                {
                    errors.Add(rune.Id + ": modifier runes must cost energy.");
                }
            }

            return errors;
        }
    }
}
