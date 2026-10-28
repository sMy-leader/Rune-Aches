// ─────────────────────────────────────────────────────────────────────────────
// 【链路】JsonSaveStore.TryLoad() ──> SaveMigrations.Migrate() ──> 业务代码
// 【职责】把旧版本存档逐步升级到当前版本。
// 【规则】每一步迁移都是纯函数、只依赖输入、不读文件——所以能单测。
//        "只加字段、不改语义、不删字段"；必须删时先标 Obsolete 一版。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Framework
{
    /// <summary>
    /// 存档版本迁移链。每一步都是纯函数，便于单测。
    /// 规则：只加字段、不改语义、不删字段；必须删除时先标记 Obsolete 一版。
    /// </summary>
    public static class SaveMigrations
    {
        public static SaveFile Migrate(SaveFile data)
        {
            // 空档 = 全新玩家，直接给默认结构。
            if (data == null)
            {
                return new SaveFile();
            }

            // v0 = 没有版本号的早期测试档。这里只补版本号，不改数据。
            if (data.Version <= 0)
            {
                data.Version = 1;
            }

            // 未来的迁移步骤按顺序往下写，每一步结束把 Version 设为新值：
            // if (data.Version == 1) { data.Meta.UnlockedSlots = Mathf.Max(3, data.Meta.UnlockedSlots); data.Version = 2; }

            // 玩家用新版本存档、又回退到旧版本时会走这里：
            // 不阻断游戏，只提示"未知字段会被忽略"，避免旧版本直接打不开。
            if (data.Version > SaveFile.CurrentVersion)
            {
                Debug.LogWarning("[Save] Save is from a newer build (v" + data.Version +
                                 " > v" + SaveFile.CurrentVersion + "). Unknown fields will be ignored.");
            }

            // 反序列化时缺字段会得到 null，这里兜底，避免业务代码空引用。
            if (data.Meta == null)
            {
                data.Meta = new MetaSave();
            }

            if (data.Settings == null)
            {
                data.Settings = new SettingsSave();
            }

            return data;
        }
    }
}
