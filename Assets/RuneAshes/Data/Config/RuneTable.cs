// ─────────────────────────────────────────────────────────────────────────────
// 【链路】TableBase<RuneDef> ──> RuneTable（本文件）──> TableProvider.Runes
// 【职责】符文表的业务查询。通用能力（按 id、遍历、计数）全在 TableBase，
//        这里只加"玩法真正需要"的筛选。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;

namespace RuneAshes.Data
{
    public sealed class RuneTable : TableBase<RuneDef>
    {
        public RuneTable(IEnumerable<RuneDef> rows)
            : base(rows, row => row.Id)
        {
        }

        /// <summary>
        /// 按类别筛选。三选一抽卡时用它拿候选池：
        /// 例如"只出 Modifier"，或"Trigger 已有时只出 Effect/Modifier"。
        /// </summary>
        public IEnumerable<RuneDef> ByCategory(RuneCategory category)
        {
            return Where(rune => rune.Category == category);
        }
    }
}
