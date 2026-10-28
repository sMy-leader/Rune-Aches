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
