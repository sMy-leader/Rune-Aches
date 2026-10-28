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
    /// 一次符文施法的上下文。包含递归深度与随机种子，
    /// 用于实现"链内触发深度上限"和可复现随机。
    /// </summary>
    public struct RuneEffectContext
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float BaseValue;
        public int ChainDepth;
        public uint Seed;
    }
}
