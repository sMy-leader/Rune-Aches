// ─────────────────────────────────────────────────────────────────────────────
// 【链路】IRuneWorld（接口）──> UnityRuneWorld（本文件，Unity 实现）
//        ──> 弹丸池 / OverlapSphere / OverlapCapsule ──> IDamageable
// 【职责】把"效果想要的世界操作"翻译成 Unity API：生成弹丸、打一条线、打一个圈、找最近目标。
// 【所在位置】挂在玩家 GameObject 上，由 ArenaBlockoutBuilder 创建；RuneCaster 直接取用。
// 【W2 已知短板】命中检测是 OverlapSphere/Capsule 的即时查询，
//               敌人数量上去后会变贵。W3 换成空间哈希 + Job，但接口不变。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// IRuneWorld 的 Unity 实现。W2 用 OverlapSphere/Capsule 做粗碰撞，
    /// W3 会替换成 Job 化的空间哈希，接口不变。
    /// </summary>
    public sealed class UnityRuneWorld : MonoBehaviour, IRuneWorld
    {
        [SerializeField] private Material _projectileMaterial;
        [SerializeField] private Material _mineMaterial;
        [SerializeField] private LayerMask _targetMask = ~0;

        private readonly List<Projectile> _projectiles = new List<Projectile>(64);
        private readonly List<Collider> _uniqueHits = new List<Collider>(64);
        private readonly Collider[] _overlap = new Collider[64];

        /// <summary>
        /// 施法者自己的 IDamageable。所有命中查询都要排除它，
        /// 否则玩家发射的弹丸出生在脚下、第一帧就会命中自己。
        /// </summary>
        private IDamageable _owner;

        private void Awake()
        {
            _owner = GetComponent<IDamageable>();
        }

        /// <summary>弹丸/环绕球/地雷用它排除施法者本体。</summary>
        public bool IsOwner(IDamageable damageable)
        {
            return damageable != null && ReferenceEquals(damageable, _owner);
        }

        public void SpawnProjectile(ProjectileSpec spec)
        {
            var projectile = GetProjectile();
            projectile.Launch(spec);
        }

        public void ReleaseProjectile(Projectile projectile)
        {
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
            }
        }

        public void SpawnOrbitingOrbs(OrbitSpec spec)
        {
            var orbObject = new GameObject("OrbitingOrbs");
            orbObject.transform.SetParent(transform, false);
            var orbs = orbObject.AddComponent<OrbitingOrbs>();
            orbs.Init(spec, transform);
        }

        public void SpawnMine(MineSpec spec)
        {
            var mineObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mineObject.name = "Mine";
            mineObject.transform.SetParent(transform, false);
            mineObject.transform.localScale = Vector3.one * 0.45f;

            var collider = mineObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = mineObject.GetComponent<Renderer>();
            if (renderer != null && _mineMaterial != null)
            {
                renderer.sharedMaterial = _mineMaterial;
            }

            var mine = mineObject.AddComponent<Mine>();
            mine.Init(spec, this);
        }

        public void DamageLine(LineDamageSpec spec)
        {
            var direction = spec.Direction.sqrMagnitude > 0.0001f ? spec.Direction.normalized : Vector3.forward;
            var start = spec.Origin;
            var end = spec.Origin + direction * spec.Length;
            var count = Physics.OverlapCapsuleNonAlloc(
                start, end, spec.Width * 0.5f, _overlap, _targetMask, QueryTriggerInteraction.Ignore);
            ApplyToHits(count, spec.Damage, DamageType.Direct, spec.Meta);
        }

        public void DamageArea(AreaDamageSpec spec)
        {
            var count = Physics.OverlapSphereNonAlloc(
                spec.Center, spec.Radius, _overlap, _targetMask, QueryTriggerInteraction.Ignore);
            ApplyToHits(count, spec.Damage, spec.Type, spec.Meta);
        }

        public bool TryGetNearestTarget(Vector3 origin, float maxDistance, out Vector3 position)
        {
            var count = Physics.OverlapSphereNonAlloc(
                origin, maxDistance, _overlap, _targetMask, QueryTriggerInteraction.Ignore);

            position = origin;
            var best = float.MaxValue;
            var found = false;

            for (var i = 0; i < count; i++)
            {
                var damageable = _overlap[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || IsOwner(damageable))
                {
                    continue;
                }

                var sqrDistance = (damageable.Position - origin).sqrMagnitude;
                if (sqrDistance < best)
                {
                    best = sqrDistance;
                    position = damageable.Position;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// 把一次范围/线形查询的结果结算到所有命中目标。
        /// [坑] 去重基于 Collider 而不是 IDamageable：同一个敌人如果有多个 Collider
        ///      （比如身体 + 受击框），会在这里被结算两次。W3 换敌人时应保证每敌一个 Collider，
        ///      或改成按 IDamageable 去重。
        /// </summary>
        private void ApplyToHits(int count, float damage, DamageType type, CastMeta meta)
        {
            _uniqueHits.Clear();

            for (var i = 0; i < count; i++)
            {
                var collider = _overlap[i];
                var damageable = collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || IsOwner(damageable) ||
                    _uniqueHits.Contains(collider))
                {
                    continue;
                }

                _uniqueHits.Add(collider);
                damageable.ApplyDamage(new DamageEvent(meta, damage, type));
            }
        }

        /// <summary>
        /// 从池里取一发空闲弹丸；没有就新建。
        /// 用 activeSelf 判断空闲，池只增不减（W2 简化）。
        /// [坑] 线性扫描空闲项：池里有几百发时每次发射都要扫。W3 应改成栈/队列索引。
        /// </summary>
        private Projectile GetProjectile()
        {
            for (var i = 0; i < _projectiles.Count; i++)
            {
                if (!_projectiles[i].gameObject.activeSelf)
                {
                    return _projectiles[i];
                }
            }

            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Projectile";
            projectileObject.transform.SetParent(transform, false);
            projectileObject.transform.localScale = Vector3.one * 0.5f;

            var collider = projectileObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = projectileObject.GetComponent<Renderer>();
            if (renderer != null && _projectileMaterial != null)
            {
                renderer.sharedMaterial = _projectileMaterial;
            }

            var projectile = projectileObject.AddComponent<Projectile>();
            projectile.Owner = this;
            _projectiles.Add(projectile);
            return projectile;
        }
    }
}
