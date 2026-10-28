// ─────────────────────────────────────────────────────────────────────────────
// 【原语】E1 直射：一次施法发射一枚直线弹丸。
// 【只做参数拼装】这里不判断命中、不结算伤害——那些是 Projectile 的职责。
//                效果原语的边界就是"我要在世界里生成什么"。
// 【调参入口】速度读 rune.csv 的 speed；伤害读 context.BaseValue（由 RuneCaster 按表填入）。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>E1 直射弹：朝瞄准方向发射一枚弹。</summary>
    public sealed class DirectShotEffect : IRuneEffect
    {
        private readonly RuneDef _def;

        public DirectShotEffect(RuneDef def)
        {
            _def = def;
        }

        public void Execute(ref RuneEffectContext context)
        {
            context.World.SpawnProjectile(new ProjectileSpec
            {
                Origin = context.Origin,
                Direction = context.Direction,
                Speed = _def.GetFloat("speed", 20f),
                Damage = context.BaseValue,
                Lifetime = 3f,
                Radius = 0.28f,
                Meta = context.Meta,
                Modifiers = context.Modifiers
            });
        }
    }
}
