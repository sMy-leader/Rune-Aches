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
            if (data == null)
            {
                return new SaveFile();
            }

            if (data.Version <= 0)
            {
                // v0 = 无版本号的早期档
                data.Version = 1;
            }

            // 示例（未来版本）：
            // if (data.Version == 1) { data.Meta.UnlockedSlots = Mathf.Max(3, data.Meta.UnlockedSlots); data.Version = 2; }

            if (data.Version > SaveFile.CurrentVersion)
            {
                Debug.LogWarning("[Save] Save is from a newer build (v" + data.Version +
                                 " > v" + SaveFile.CurrentVersion + "). Unknown fields will be ignored.");
            }

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
