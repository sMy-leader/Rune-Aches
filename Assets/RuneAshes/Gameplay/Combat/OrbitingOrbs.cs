using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 环绕球：挂在玩家身下，围绕旋转并周期性检测命中。
    /// 同一敌人 0.5s 内不重复受伤（与语义规范一致）。
    /// </summary>
    public sealed class OrbitingOrbs : MonoBehaviour
    {
        private const float HitCooldown = 0.5f;

        private OrbitSpec _spec;
        private float _age;
        private float _angle;
        private readonly Collider[] _hits = new Collider[16];
        private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();

        public void Init(OrbitSpec spec, Transform follow)
        {
            _spec = spec;
            _age = 0f;
            _angle = 0f;
            if (follow != null)
            {
                transform.SetParent(follow, false);
                transform.localPosition = Vector3.zero;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _age += dt;
            _angle += _spec.RotationSpeed * dt;

            if (_age >= _spec.Duration)
            {
                Destroy(gameObject);
                return;
            }

            var count = Mathf.Max(1, _spec.Count);
            for (var i = 0; i < count; i++)
            {
                var offset = Quaternion.Euler(0f, _angle + i * (360f / count), 0f) * Vector3.forward;
                var point = transform.position + offset * _spec.Radius;
                HitAt(point);
            }
        }

        private void HitAt(Vector3 point)
        {
            var count = Physics.OverlapSphereNonAlloc(point, 0.55f, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                var id = damageable.GetHashCode();
                float last;
                if (_lastHitTime.TryGetValue(id, out last) && Time.time - last < HitCooldown)
                {
                    continue;
                }

                _lastHitTime[id] = Time.time;
                damageable.ApplyDamage(new DamageEvent(_spec.Meta, _spec.Damage, DamageType.Direct));
            }
        }
    }
}
