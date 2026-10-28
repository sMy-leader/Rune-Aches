// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Projectile.Hit() ──> (damageable as IDebuffable).ApplyBurn()
// 【职责】可被附加持续状态的东西。与 IDamageable 分开：
//        有些目标只吃伤害（训练靶的简化版、可破坏物），不该被迫实现燃烧/减速。
// 【语义】燃烧"取最大值不叠加"，见 Docs/符文语义规范_v1.md。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 可被附加持续效果的实体。与 IDamageable 分开，方便只做伤害的靶子。
    /// </summary>
    public interface IDebuffable
    {
        /// <summary>施加燃烧。同一目标多次施加时取更强的 dps / 更长的持续时间，不叠层。</summary>
        void ApplyBurn(float dps, float duration, CastMeta meta);

        /// <summary>减速。W2 的靶子没有移动，空实现；W3 的移动敌人再接。</summary>
        void ApplySlow(float percent, float duration);
    }
}
