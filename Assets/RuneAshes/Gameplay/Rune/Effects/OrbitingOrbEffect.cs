// ─────────────────────────────────────────────────────────────────────────────
// 【原语】E3 环绕球：生成一组围绕施法者旋转、持续造成伤害的球。
// 【生命周期】由 OrbitingOrbs 自己管理（Duration 到点自毁），不是一次性弹丸。
// 【调参入口】count 来自 rune.csv；半径/持续时间目前是硬编码的设计值。
// ─────────────────────────────────────────────────────────────────────────────
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
