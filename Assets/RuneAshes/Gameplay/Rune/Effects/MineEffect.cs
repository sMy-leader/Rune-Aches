using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>E4 地雷：在脚下放置，触发后范围伤害。</summary>
    public sealed class MineEffect : IRuneEffect
    {
        private readonly RuneDef _def;

        public MineEffect(RuneDef def)
        {
            _def = def;
        }

        public void Execute(ref RuneEffectContext context)
        {
            context.World.SpawnMine(new MineSpec
            {
                Position = context.Origin,
                Radius = _def.GetFloat("radius", 3f),
                Damage = context.BaseValue,
                ArmDelay = 0.3f,
                Lifetime = 12f,
                Meta = context.Meta,
                Modifiers = context.Modifiers
            });
        }
    }
}
