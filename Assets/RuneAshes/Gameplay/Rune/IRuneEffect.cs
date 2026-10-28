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
    /// </summary>
    public struct RuneEffectContext
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float BaseValue;
        public int ChainDepth;
        public uint Seed;
        public CastMeta Meta;
        public ModifierSet Modifiers;
        public IRuneWorld World;
    }
}
