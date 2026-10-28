// ─────────────────────────────────────────────────────────────────────────────
// 【链路】IRuneEffect.Execute() ──> IRuneWorld（本文件）──> UnityRuneWorld
//        ──> Projectile / OrbitingOrbs / Mine / Physics
// 【职责】效果层与 Unity 之间的唯一边界。效果只描述"我要生成什么"，
//        具体怎么用 GameObject/物理去实现，全部由实现方决定。
// 【为什么这样分层】效果层因此可以在 EditMode 用 fake world 单测：
//                  不用进 Play、不用场景、不用等 30 秒编译。
// 【扩展方式】新增效果原语时，如果需要新的世界操作，才在这里加方法；
//            只是参数不同的话（更快的弹、更大的范围），复用现有 spec 即可。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>直线弹丸的生成参数（E1 直射、E2 散射、M1 分裂子弹都用它）。</summary>
    public struct ProjectileSpec
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Speed;
        public float Damage;
        public float Lifetime;
        public float Radius;
        public CastMeta Meta;
        public ModifierSet Modifiers;
    }

    /// <summary>环绕球的生成参数（E3）：数量、半径、持续时间和转速。</summary>
    public struct OrbitSpec
    {
        public Vector3 Center;
        public int Count;
        public float Radius;
        public float Damage;
        public float Duration;
        public float RotationSpeed;
        public CastMeta Meta;
        public ModifierSet Modifiers;
    }

    /// <summary>地雷的生成参数（E4）：引信时间 + 触发半径 + 寿命。</summary>
    public struct MineSpec
    {
        public Vector3 Position;
        public float Radius;
        public float Damage;
        public float ArmDelay;
        public float Lifetime;
        public CastMeta Meta;
        public ModifierSet Modifiers;
    }

    /// <summary>线形伤害参数（E5 冲刺斩）：从 Origin 沿 Direction 的一条胶囊。</summary>
    public struct LineDamageSpec
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Length;
        public float Width;
        public float Damage;
        public CastMeta Meta;
        public ModifierSet Modifiers;
    }

    /// <summary>范围伤害参数（M6 爆炸、地雷引爆）。</summary>
    public struct AreaDamageSpec
    {
        public Vector3 Center;
        public float Radius;
        public float Damage;
        public DamageType Type;
        public CastMeta Meta;
    }

    /// <summary>
    /// 符文效果与 Unity 世界的唯一交界。效果原语只依赖这个接口，
    /// 因此可以在 EditMode 用 fake world 单测，不需要场景。
    /// </summary>
    public interface IRuneWorld
    {
        void SpawnProjectile(ProjectileSpec spec);

        void SpawnOrbitingOrbs(OrbitSpec spec);

        void SpawnMine(MineSpec spec);

        void DamageLine(LineDamageSpec spec);

        void DamageArea(AreaDamageSpec spec);

        /// <summary>用于追踪/弹射选目标。只返回"最近且存活"的位置，不返回对象本身。</summary>
        bool TryGetNearestTarget(Vector3 origin, float maxDistance, out Vector3 position);
    }
}
