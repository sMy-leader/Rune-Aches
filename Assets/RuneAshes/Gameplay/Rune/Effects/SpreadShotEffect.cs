using RuneAshes.Data;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>E2 散射：扇形发射多枚弹。</summary>
    public sealed class SpreadShotEffect : IRuneEffect
    {
        private readonly RuneDef _def;

        public SpreadShotEffect(RuneDef def)
        {
            _def = def;
        }

        public void Execute(ref RuneEffectContext context)
        {
            var count = Mathf.Max(1, _def.GetInt("count", 5));
            var angle = _def.GetFloat("angle", 45f);
            var step = count <= 1 ? 0f : angle / (count - 1);
            var start = count <= 1 ? 0f : -angle * 0.5f;

            for (var i = 0; i < count; i++)
            {
                var direction = Quaternion.Euler(0f, start + step * i, 0f) * context.Direction;
                context.World.SpawnProjectile(new ProjectileSpec
                {
                    Origin = context.Origin,
                    Direction = direction,
                    Speed = 18f,
                    Damage = context.BaseValue,
                    Lifetime = 2.5f,
                    Radius = 0.26f,
                    Meta = context.Meta,
                    Modifiers = context.Modifiers
                });
            }
        }
    }
}
