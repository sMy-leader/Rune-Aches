// ─────────────────────────────────────────────────────────────────────────────
// 【原语】E5 冲刺斩：沿施法方向打一条胶囊形的线伤害。
// 【为什么不是弹丸】它没有飞行时间，命中是即时的；长度/宽度来自表与设计值。
// 【W3 联动】T4 冲刺触发接上后，这个效果会在冲刺瞬间沿冲刺方向结算。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>E5 冲刺斩：沿冲刺方向造成一条线伤害。</summary>
    public sealed class DashSlashEffect : IRuneEffect
    {
        private readonly RuneDef _def;

        public DashSlashEffect(RuneDef def)
        {
            _def = def;
        }

        public void Execute(ref RuneEffectContext context)
        {
            context.World.DamageLine(new LineDamageSpec
            {
                Origin = context.Origin,
                Direction = context.Direction,
                Length = _def.GetFloat("distance", 4f),
                Width = 1.6f,
                Damage = context.BaseValue,
                Meta = context.Meta,
                Modifiers = context.Modifiers
            });
        }
    }
}
