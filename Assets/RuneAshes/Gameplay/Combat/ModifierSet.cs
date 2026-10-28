// ─────────────────────────────────────────────────────────────────────────────
// 【链路】CompiledChain.Modifiers（RuneDef 列表）──> RuneModifierFactory.Build()
//        ──> ModifierSet（本文件）──> Projectile / OrbitingOrbs / Mine
// 【职责】把"链上装了哪些修饰"折算成一组运行时参数。
// 【为什么用 struct】每个弹丸都带一份，class 会持续分配；struct 直接内联在
//                  ProjectileSpec 里，零 GC。
// 【为什么不在运行时查表】施法频率远高于改链频率，折算一次、复用很多次。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 一条链上所有修饰折算后的效果参数。用 struct 传给弹丸/地雷/环绕球，
    /// 避免运行时反复查 RuneDef。
    /// </summary>
    public struct ModifierSet
    {
        /// <summary>M1 分裂：命中后额外生成的子弹数；0 = 不分裂。</summary>
        public int SplitCount;

        /// <summary>M1 分裂：子弹继承的伤害比例（0~1）。</summary>
        public float SplitInherit;

        /// <summary>M2 弹射：命中后还能转向新目标的次数。</summary>
        public int Bounces;

        /// <summary>M2 弹射：每次弹射的伤害衰减比例（0~1），由 Projectile 累乘。</summary>
        public float BounceFalloff;

        /// <summary>M3 穿透：还能穿过几个目标（不改变方向）。</summary>
        public int Pierce;

        /// <summary>M7 追踪：每秒最大转向角度；0 = 直线飞行。</summary>
        public float TurnRate;

        /// <summary>M4 燃烧：每秒伤害。</summary>
        public float BurnDps;

        /// <summary>M4 燃烧：持续时间（秒）。</summary>
        public float BurnDuration;

        /// <summary>M6 爆炸：命中后的范围伤害半径；0 = 不爆炸。</summary>
        public float ExplosionRadius;

        /// <summary>M6 爆炸：范围伤害数值，默认取修饰符文的 BaseValue。</summary>
        public float ExplosionDamage;

        /// <summary>
        /// 是否至少有一个"会改变行为"的修饰。用于跳过不必要的分支判断。
        /// 注意没有把 BurnDuration 算进去：只填 duration 而不填 dps 不构成有效修饰。
        /// </summary>
        public bool HasAny
        {
            get
            {
                return SplitCount > 0 || Bounces > 0 || Pierce > 0 || TurnRate > 0f ||
                       BurnDps > 0f || ExplosionRadius > 0f;
            }
        }
    }
}
