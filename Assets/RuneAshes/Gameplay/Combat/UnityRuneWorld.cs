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
                if (damageable == null || !damageable.IsAlive)
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

        private void ApplyToHits(int count, float damage, DamageType type, CastMeta meta)
        {
            _uniqueHits.Clear();

            for (var i = 0; i < count; i++)
            {
                var collider = _overlap[i];
                var damageable = collider.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || _uniqueHits.Contains(collider))
                {
                    continue;
                }

                _uniqueHits.Add(collider);
                damageable.ApplyDamage(new DamageEvent(meta, damage, type));
            }
        }

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
