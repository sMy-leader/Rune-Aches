using UnityEngine;

namespace RuneAshes.Gameplay
{
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

        bool TryGetNearestTarget(Vector3 origin, float maxDistance, out Vector3 position);
    }
}
