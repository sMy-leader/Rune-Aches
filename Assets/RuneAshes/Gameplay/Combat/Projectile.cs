using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 直线弹丸。W2 用 OverlapSphere 粗命中，W3 换空间哈希。
    /// 支持穿透、弹射、分裂、追踪、燃烧、爆炸六种修饰。
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectileSpec _spec;
        private float _age;
        private bool _active;
        private Vector3 _direction;
        private float _damageScale = 1f;
        private int _pierceLeft;
        private int _bouncesLeft;
        private readonly Collider[] _hits = new Collider[8];
        private readonly List<int> _hitIds = new List<int>(8);

        internal UnityRuneWorld Owner;

        public void Launch(ProjectileSpec spec)
        {
            _spec = spec;
            _age = 0f;
            _active = true;
            _damageScale = 1f;
            _direction = spec.Direction.sqrMagnitude > 0.0001f ? spec.Direction.normalized : Vector3.forward;
            _pierceLeft = spec.Modifiers.Pierce;
            _bouncesLeft = spec.Modifiers.Bounces;
            _hitIds.Clear();
            transform.position = spec.Origin;
            transform.forward = _direction;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            var dt = Time.deltaTime;
            _age += dt;

            if (_spec.Modifiers.TurnRate > 0f)
            {
                Home(dt);
            }

            transform.position += _direction * (_spec.Speed * dt);

            if (TryHit())
            {
                return;
            }

            if (_age >= _spec.Lifetime)
            {
                Expire();
            }
        }

        private void Home(float dt)
        {
            if (Owner == null)
            {
                return;
            }

            Vector3 target;
            if (!Owner.TryGetNearestTarget(transform.position, 15f, out target))
            {
                return;
            }

            var desired = target - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                return;
            }

            var maxRadians = _spec.Modifiers.TurnRate * Mathf.Deg2Rad * dt;
            _direction = Vector3.RotateTowards(_direction, desired.normalized, maxRadians, 0f);
            transform.forward = _direction;
        }

        /// <summary>返回 true 表示本帧弹丸已被消耗（命中或消亡），Update 应停止。</summary>
        private bool TryHit()
        {
            var count = Physics.OverlapSphereNonAlloc(
                transform.position, _spec.Radius, _hits, ~0, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                var id = damageable.GetHashCode();
                if (_hitIds.Contains(id))
                {
                    continue;
                }

                _hitIds.Add(id);
                Hit(damageable);

                if (_pierceLeft > 0)
                {
                    _pierceLeft--;
                    continue;
                }

                if (_bouncesLeft > 0 && TryBounce())
                {
                    return false;
                }

                Split();
                Deactivate();
                return true;
            }

            return false;
        }

        private void Hit(IDamageable damageable)
        {
            damageable.ApplyDamage(new DamageEvent(_spec.Meta, _spec.Damage * _damageScale, DamageType.Direct));

            var debuff = damageable as IDebuffable;
            if (debuff != null && _spec.Modifiers.BurnDps > 0f)
            {
                debuff.ApplyBurn(_spec.Modifiers.BurnDps, _spec.Modifiers.BurnDuration, _spec.Meta);
            }

            if (_spec.Modifiers.ExplosionRadius > 0f && Owner != null)
            {
                Owner.DamageArea(new AreaDamageSpec
                {
                    Center = transform.position,
                    Radius = _spec.Modifiers.ExplosionRadius,
                    Damage = _spec.Modifiers.ExplosionDamage,
                    Type = DamageType.Explosion,
                    Meta = _spec.Meta
                });
            }
        }

        private bool TryBounce()
        {
            if (Owner == null)
            {
                return false;
            }

            Vector3 target;
            if (!Owner.TryGetNearestTarget(transform.position, 15f, out target))
            {
                return false;
            }

            var desired = target - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                return false;
            }

            _direction = desired.normalized;
            transform.forward = _direction;
            _bouncesLeft--;
            _damageScale *= Mathf.Clamp01(1f - _spec.Modifiers.BounceFalloff);
            return true;
        }

        private void Split()
        {
            if (_spec.Modifiers.SplitCount <= 0 || Owner == null)
            {
                return;
            }

            var count = _spec.Modifiers.SplitCount;
            var damage = _spec.Damage * _spec.Modifiers.SplitInherit;
            var inherited = _spec.Modifiers;
            inherited.SplitCount = 0; // 分裂不可再分裂

            for (var i = 0; i < count; i++)
            {
                var angle = count <= 1 ? 0f : -30f + 60f * i / (count - 1);
                var direction = Quaternion.Euler(0f, angle, 0f) * _direction;
                Owner.SpawnProjectile(new ProjectileSpec
                {
                    Origin = transform.position,
                    Direction = direction,
                    Speed = _spec.Speed * 0.8f,
                    Damage = damage,
                    Lifetime = _spec.Lifetime * 0.6f,
                    Radius = _spec.Radius,
                    Meta = _spec.Meta,
                    Modifiers = inherited
                });
            }
        }

        private void Expire()
        {
            Split();
            Deactivate();
        }

        private void Deactivate()
        {
            _active = false;
            if (Owner != null)
            {
                Owner.ReleaseProjectile(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
