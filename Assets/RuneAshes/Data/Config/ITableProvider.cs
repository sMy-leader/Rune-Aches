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
