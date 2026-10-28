// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Projectile / UnityRuneWorld / OrbitingOrbs / Mine ──> IDamageable（本文件）
// 【职责】"能被伤害的东西"的最小契约。玩家、敌人、可破坏物都实现它。
// 【为什么只要三个成员】追踪/弹射选目标只要位置，命中结算只要 ApplyDamage，
//        过滤器只要 IsAlive；把更多东西塞进来会让测试桩变重。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public interface IDamageable
    {
        /// <summary>死亡后仍可能被查询到（对象池未回收），所有调用方必须先判这个。</summary>
        bool IsAlive { get; }

        /// <summary>世界坐标。用于追踪/弹射找最近目标、爆炸距离衰减。</summary>
        Vector3 Position { get; }

        /// <summary>唯一的伤害入口。实现方负责扣血、死亡、发事件。</summary>
        void ApplyDamage(DamageEvent damage);
    }
}
