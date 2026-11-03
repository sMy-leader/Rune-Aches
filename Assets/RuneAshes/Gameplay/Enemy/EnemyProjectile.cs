// ─────────────────────────────────────────────────────────────────────────────
// 【职责】远程怪的占位弹丸。只追踪玩家、不做敌人碰撞；
//        正式 VFX/弹道资产到位后替换为 pooled projectile。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private PlayerHealth _target;
        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _age;
        private const float Lifetime = 5f;

        public static void Spawn(Vector3 origin, Vector3 direction, float damage, float speed,
            PlayerHealth target)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyProjectile";
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.32f;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.35f, 0.75f, 1f);
            }

            var projectile = go.AddComponent<EnemyProjectile>();
            projectile._target = target;
            projectile._direction = direction.normalized;
            projectile._speed = speed;
            projectile._damage = damage;
        }

        private void Update()
        {
            if (_target == null || !_target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            var dt = Time.deltaTime;
            _age += dt;
            if (_age >= Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += _direction * (_speed * dt);

            var targetPoint = _target.Position + Vector3.up * 1.0f;
            if (Vector3.Distance(transform.position, targetPoint) <= 0.65f)
            {
                var meta = new CastMeta { ChainId = 0, CastId = 0, Depth = 0, Seed = 0 };
                _target.ApplyDamage(new DamageEvent(meta, _damage, DamageType.Direct));
                Destroy(gameObject);
            }
        }
    }
}
