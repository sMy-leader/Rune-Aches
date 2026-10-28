// ─────────────────────────────────────────────────────────────────────────────
// 【原语】E4 地雷：在施法位置放一枚延迟引爆的地雷。
// 【注意】Origin 是施法者位置 + 0.9m，所以地雷会浮在地面附近，
//        判定用 XZ 半径，高度不影响触发。
// 【调参入口】radius 来自 rune.csv；引信/寿命目前是硬编码设计值。
// ─────────────────────────────────────────────────────────────────────────────
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
