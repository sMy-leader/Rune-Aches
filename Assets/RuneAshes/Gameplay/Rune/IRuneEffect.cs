// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ChainCompiler.Compile() ──> RuneEffectFactory 创建实现 ──> Execute(ref context)
// 【职责】"效果原语"的接口：只负责把一次施法翻译成世界操作。
// 【边界】效果实现里不查表、不判命中、不做 UI；参数从 context（含 BaseValue/Modifiers）拿。
// 【为什么用 ref context】避免 struct 拷贝；也是把"一次性上下文"明确成栈上数据的信号。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 符文效果原语。触发/效果/修饰最终都编译成该接口的实现；
    /// 未来升级自由节点图时复用此层，不重写。
    /// </summary>
    public interface IRuneEffect
    {
        void Execute(ref RuneEffectContext context);
    }

    /// <summary>
    /// 一次符文施法的上下文。包含递归深度、随机种子与折算后的修饰参数。
    /// 由 RuneCaster 组装，效果实现只读。
    /// </summary>
    public struct RuneEffectContext
    {
        /// <summary>施法起点（玩家位置 + 0.9m）。</summary>
        public Vector3 Origin;

        /// <summary>已归一化的施法方向。</summary>
        public Vector3 Direction;

        /// <summary>本次效果的基础数值（当前是伤害），来自效果符文的 BaseValue。</summary>
        public float BaseValue;

        /// <summary>递归深度，效果派生新触发时用。</summary>
        public int ChainDepth;

        public uint Seed;
        public CastMeta Meta;
        public ModifierSet Modifiers;

        /// <summary>世界操作入口。效果只依赖它，所以能用 fake world 单测。</summary>
        public IRuneWorld World;
    }
}
