// ─────────────────────────────────────────────────────────────────────────────
// 【链路】玩家装配 UI / 预设 / 分享码 ──> ChainBlueprint ──> ChainCompiler.Compile()
// 【职责】"玩家想装什么"的原始意图：只有 ID 和上限，没有对象引用、没有运行时状态。
//        所以它能直接 JSON 序列化（存档）或压成短码（分享），不会把场景对象带进去。
// 【和 CompiledChain 的区别】Blueprint 是可变草稿，Compiled 是只读结果。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 玩家编辑中的链。只存 ID 与等级，不存对象引用——分享码与存档都直接序列化它。
    /// </summary>
    public sealed class ChainBlueprint
    {
        /// <summary>触发器 ID（当前只有 T1 已实现，T2~T6 在 W3 接事件总线）。</summary>
        public string TriggerId;

        /// <summary>效果 ID（E1~E5）。一条链只能有一个效果，这是"构筑身份"的来源。</summary>
        public string EffectId;

        /// <summary>修饰 ID 列表（M1~M7）。顺序不影响结果，因为最终会折算成 ModifierSet。</summary>
        public List<string> ModifierIds = new List<string>();

        /// <summary>
        /// 上限跟着蓝图走，而不是写死成全局常量：
        /// 切片期是 4 槽 / 10 能耗，将来 meta 升级后同一个编译器可以接受更大的上限。
        /// </summary>
        public int SlotLimit = RuneRules.SliceSlotLimit;

        public int EnergyLimit = RuneRules.SliceEnergyLimit;

        /// <summary>
        /// 深拷贝。装配 UI 需要"取消编辑"时用副本编译，确认后才写回，避免改到一半影响正在开火的链。
        /// </summary>
        public ChainBlueprint Clone()
        {
            return new ChainBlueprint
            {
                TriggerId = TriggerId,
                EffectId = EffectId,
                ModifierIds = new List<string>(ModifierIds),
                SlotLimit = SlotLimit,
                EnergyLimit = EnergyLimit
            };
        }
    }
}
