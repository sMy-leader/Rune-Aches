// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ChainCompiler.Compile(blueprint) ──> CompiledChain ──> RuneCaster
// 【职责】一次编译的只读产物：触发/效果/修饰的 RuneDef 引用 + 能耗/槽位/错误列表。
// 【为什么不可变】玩家改链是低频操作，开火是高频操作。
//                编译后运行时只读这个对象，不再查表、不再分配，也不会出现
//                "链在开火途中被 UI 改掉"的竞态。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using RuneAshes.Data;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 编译后的链：不可变、可直接交给运行时。改链时重建，运行时不再遍历数据表。
    /// </summary>
    public sealed class CompiledChain
    {
        public CompiledChain(
            RuneDef trigger,
            RuneDef effect,
            IReadOnlyList<RuneDef> modifiers,
            int energyCost,
            int slotCount,
            IReadOnlyList<string> errors)
        {
            Trigger = trigger;
            Effect = effect;
            Modifiers = modifiers ?? new List<RuneDef>();
            EnergyCost = energyCost;
            SlotCount = slotCount;
            Errors = errors ?? new List<string>();
        }

        public RuneDef Trigger { get; private set; }

        public RuneDef Effect { get; private set; }

        /// <summary>修饰的原始定义，顺序 = 蓝图顺序；运行时由 RuneModifierFactory 折算成 struct。</summary>
        public IReadOnlyList<RuneDef> Modifiers { get; private set; }

        /// <summary>所有槽位能耗之和（触发器 + 效果 + 每个修饰）。</summary>
        public int EnergyCost { get; private set; }

        /// <summary>已占用的槽位数，用于 HUD 显示 "3/4"。</summary>
        public int SlotCount { get; private set; }

        /// <summary>
        /// 编译期收集到的全部问题（空 = 通过）。装不上的链仍然返回一个对象，
        /// 而不是抛异常——装配 UI 要能一边拼一边显示"为什么装不上"。
        /// </summary>
        public IReadOnlyList<string> Errors { get; private set; }

        /// <summary>
        /// 唯一的"可用"判定。RuneCaster 只检查这里，不再自己重复校验。
        /// 注意 Errors 非空即不可用，哪怕 Trigger/Effect 都在（例如能耗超了）。
        /// </summary>
        public bool IsValid
        {
            get { return Trigger != null && Effect != null && Errors.Count == 0; }
        }
    }
}
