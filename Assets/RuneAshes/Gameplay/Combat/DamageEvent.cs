// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Projectile.Hit() / UnityRuneWorld.DamageArea() ──> DamageEvent
//        ──> EnemyDummy.ApplyDamage()
// 【职责】一次伤害结算的最小数据包：多少伤害、什么类型、来自哪条链/哪次施法/第几层。
// 【为什么用 struct】弹幕游戏每帧可能几十次伤害；class 会持续产生 GC 尖峰。
//                   struct 按值传递，栈上分配，零 GC。
// 【为什么保留 SourceChainId/Depth】伤害事件是"击杀→触发"递归链的起点，
//                                  没有来源信息就无法判断该不该继续触发。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    public enum DamageType
    {
        /// <summary>直接命中（弹丸、斩击、环绕球）。会触发受击闪白。</summary>
        Direct,

        /// <summary>持续伤害（燃烧）。不触发闪白，也不默认算"命中"（见 RuneRules.DotCountsAsHit）。</summary>
        Dot,

        /// <summary>范围爆炸（M6、地雷）。与 Direct 的区别留给将来的抗性/护盾规则。</summary>
        Explosion
    }

    /// <summary>
    /// 伤害结算的最小单位。struct + 只读字段，热路径零分配。
    /// </summary>
    public struct DamageEvent
    {
        public int SourceChainId;
        public int CastId;
        public int Depth;
        public float Amount;
        public DamageType Type;

        /// <summary>是否无视护盾（穿透/真实伤害预留）。当前没有护盾敌人，所以还没消费。</summary>
        public bool IgnoreShield;

        public DamageEvent(CastMeta meta, float amount, DamageType type, bool ignoreShield = false)
        {
            SourceChainId = meta.ChainId;
            CastId = meta.CastId;
            Depth = meta.Depth;
            Amount = amount;
            Type = type;
            IgnoreShield = ignoreShield;
        }
    }
}
