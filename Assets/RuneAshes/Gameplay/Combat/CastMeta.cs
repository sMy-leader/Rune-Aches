// ─────────────────────────────────────────────────────────────────────────────
// 【链路】RuneCaster.Cast() 生成 ──> 塞进 RuneEffectContext / ProjectileSpec
//        ──> 每个弹丸、爆炸、DoT 都带着它 ──> DamageEvent
// 【职责】回答三个问题：这次伤害属于哪条链？哪一次施法？递归到第几层？
// 【为什么不用全局静态计数】同帧可能有多次施法，必须携带身份才能做
//                          "同帧合并"和"击杀归属"，否则根本分不清谁是谁。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 一次施法的身份信息。所有由它派生的弹丸/地雷/召唤物都带着它，
    /// 用于击杀归属、递归深度控制与确定性随机。
    /// </summary>
    public struct CastMeta
    {
        /// <summary>链的 ID。W2 只有一条链，固定为 1；多链装配时用来区分。</summary>
        public int ChainId;

        /// <summary>本局第几次施法，单调递增。同一次施法的所有弹丸共享同一个值。</summary>
        public int CastId;

        /// <summary>
        /// 递归深度。0 = 玩家直接施法；由这次施法的事件（击杀/命中）再次触发的为 1。
        /// RuneRules.CanTrigger 用它砍断无限递归——这是"动态闸门"，与静态能耗互补。
        /// </summary>
        public int Depth;

        /// <summary>本次施法的随机种子。同种子 + 同操作 = 同结果，为回放/每日挑战铺路。</summary>
        public uint Seed;

        /// <summary>
        /// 派生下一次触发时调用：CastId 不变（仍属于同一次施法谱系），Depth + 1。
        /// 不要手动 new 一个 Depth 归零的 meta，否则递归闸门会被绕过。
        /// </summary>
        public CastMeta NextDepth()
        {
            return new CastMeta
            {
                ChainId = ChainId,
                CastId = CastId,
                Depth = Depth + 1,
                Seed = Seed
            };
        }
    }
}
