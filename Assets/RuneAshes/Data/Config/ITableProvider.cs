// ─────────────────────────────────────────────────────────────────────────────
// 【链路】TableProvider（当前实现）/ Luban（将来实现）──> ITableProvider（本文件）
//        ──> ChainCompiler、三选一、波次
// 【职责】业务代码只认这个接口，不认"表是从 CSV 还是 Excel 来的"。
// 【扩展方式】敌人表/波次表按同一模式加属性（Enemies/Waves），不要绕过接口直接读生成常量。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;

namespace RuneAshes.Data
{
    /// <summary>
    /// 配置表访问入口。当前只有符文表；敌人/波次表按同一模式追加。
    /// 将来迁移 Luban 时，只替换实现，业务代码不动。
    /// </summary>
    public interface ITableProvider
    {
        RuneTable Runes { get; }

        void LoadAll();

        /// <summary>启动时校验；返回空列表表示通过。</summary>
        IReadOnlyList<string> Validate();
    }
}
