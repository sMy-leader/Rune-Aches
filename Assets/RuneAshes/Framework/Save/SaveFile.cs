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
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long SavedAtUnix;
        public MetaSave Meta = new MetaSave();
        public SettingsSave Settings = new SettingsSave();
    }

    [Serializable]
    public sealed class MetaSave
    {
        public int RuneCoins;
        public List<string> UnlockedRuneIds = new List<string>();
        public int UnlockedSlots = 3;
        public int RunsPlayed;
        public int BestSurvivalSeconds;
    }

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
