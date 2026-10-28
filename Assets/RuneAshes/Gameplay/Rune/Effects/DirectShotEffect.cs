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
