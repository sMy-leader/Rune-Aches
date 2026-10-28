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

        public IReadOnlyList<RuneDef> Modifiers { get; private set; }

        public int EnergyCost { get; private set; }

        public int SlotCount { get; private set; }

        public IReadOnlyList<string> Errors { get; private set; }

        public bool IsValid
        {
            get { return Trigger != null && Effect != null && Errors.Count == 0; }
        }
    }
}
