// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ISaveStore.Load/Save ──> SaveFile（本文件，存档根对象）
// 【职责】定义"玩家进度到底存什么"。只存 ID 与数值，绝不存对象引用。
// 【版本规则】Version 永远放第一位；只增不减；新增字段给默认值，
//            删除字段要先标 Obsolete 一版，再由 SaveMigrations 处理。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace RuneAshes.Framework
{
    /// <summary>
    /// 存档根对象。只存 ID 与数值，绝不存场景对象引用。
    /// version 必须留在第一位，且只增不减。
    /// </summary>
    [Serializable]
    public sealed class SaveFile
    {
        /// <summary>当前代码支持的版本。改结构时 +1，并在 SaveMigrations 里加一步迁移。</summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        /// <summary>Unix 秒。用于"最近一次游戏时间"显示，也可用于冲突检测。</summary>
        public long SavedAtUnix;

        public MetaSave Meta = new MetaSave();

        public SettingsSave Settings = new SettingsSave();
    }

    /// <summary>局外进度：货币、解锁、槽位、历史成绩。</summary>
    [Serializable]
    public sealed class MetaSave
    {
        public int RuneCoins;

        /// <summary>已解锁的符文 ID 列表。存 ID 不存 index，表顺序变了也不会错位。</summary>
        public List<string> UnlockedRuneIds = new List<string>();

        /// <summary>已解锁的装配槽位数。切片期默认 3，meta 升级后提升。</summary>
        public int UnlockedSlots = 3;

        public int RunsPlayed;

        public int BestSurvivalSeconds;
    }

    /// <summary>玩家设置。可访问性相关项（震屏/伤害数字）放这里，不要散落在各处。</summary>
    [Serializable]
    public sealed class SettingsSave
    {
        public float MasterVolume = 1f;
        public float SfxVolume = 1f;
        public float MusicVolume = 0.8f;
        public bool ScreenShake = true;
        public bool DamageNumbers = true;
        public int QualityLevel = 1;
    }
}
