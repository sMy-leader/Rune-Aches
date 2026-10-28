using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>E3 环绕球：围绕施法者旋转的持续伤害球。</summary>
    public sealed class OrbitingOrbEffect : IRuneEffect
    {
        private readonly RuneDef _def;

        public OrbitingOrbEffect(RuneDef def)
        {
            _def = def;
        }

        public void Execute(ref RuneEffectContext context)
        {
            context.World.SpawnOrbitingOrbs(new OrbitSpec
            {
                Center = context.Origin,
                Count = Mathf.Max(1, _def.GetInt("count", 2)),
                Radius = 2.5f,
                Damage = context.BaseValue,
                Duration = 6f,
                RotationSpeed = 180f,
                Meta = context.Meta,
                Modifiers = context.Modifiers
            });
        }
    }
}
