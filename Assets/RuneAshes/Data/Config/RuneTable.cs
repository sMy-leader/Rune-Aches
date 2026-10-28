using System.Collections.Generic;

namespace RuneAshes.Data
{
    public sealed class RuneTable : TableBase<RuneDef>
    {
        public RuneTable(IEnumerable<RuneDef> rows)
            : base(rows, row => row.Id)
        {
        }

        public IEnumerable<RuneDef> ByCategory(RuneCategory category)
        {
            return Where(rune => rune.Category == category);
        }
    }
}
