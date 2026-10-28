// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Projectile / DamageArea ──> IDamageable.ApplyDamage ──> EnemyActor（本文件）
//        ──> EnemyBrain（受击硬直/死亡）──> EnemyAnimatorDriver
// 【职责】敌人的 HP、燃烧与死亡事件。它不决定 AI 行为，只回答"我还活着吗、掉了多少血"。
// 【和 EnemyDummy 的区别】Dummy 是训练靶（死亡只隐藏）；Actor 是正式敌人（发事件、可销毁）。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class EnemyActor : MonoBehaviour, IDamageable, IDebuffable
    {
        [SerializeField] private float _maxHealth = 300f;

        private float _health;
        private bool _dead;

        private float _burnDps;
        private float _burnTimeLeft;
        private CastMeta _burnMeta;

        /// <summary>每次受伤都会触发（含 DoT）。AI 用 DamageType 过滤，避免燃烧把敌人一直打进硬直。</summary>
        public event Action<EnemyActor, DamageEvent> Damaged;

        /// <summary>HP 归零时触发一次。</summary>
        public event Action<EnemyActor, DamageEvent> Died;

        public bool IsAlive
        {
            get { return !_dead && _health > 0f; }
        }

        public Vector3 Position
        {
            get { return transform.position; }
        }

        public float Health
        {
            get { return _health; }
        }

        public float MaxHealth
        {
            get { return _maxHealth; }
        }

        /// <summary>0~1，HUD/血条用。</summary>
        public float Health01
        {
            get { return _maxHealth <= 0f ? 0f : Mathf.Clamp01(_health / _maxHealth); }
        }

        private void Awake()
        {
            _health = _maxHealth;
        }

        public void ApplyDamage(DamageEvent damage)
        {
            if (!IsAlive)
            {
                return;
            }

            _health -= damage.Amount;

            if (Damaged != null)
            {
                Damaged(this, damage);
            }

            if (_health <= 0f)
            {
                _health = 0f;
                _dead = true;
                _burnTimeLeft = 0f;

                if (Died != null)
                {
                    Died(this, damage);
                }
            }
        }

        /// <summary>燃烧取最大值不叠加，和 EnemyDummy / 语义规范一致。</summary>
        public void ApplyBurn(float dps, float duration, CastMeta meta)
        {
            if (!IsAlive)
            {
                return;
            }

            _burnDps = Mathf.Max(_burnDps, dps);
            _burnTimeLeft = Mathf.Max(_burnTimeLeft, duration);
            _burnMeta = meta;
        }

        public void ApplySlow(float percent, float duration)
        {
            // W3 的冰霜词缀接入时实现；当前没有减速敌人。
        }

        private void Update()
        {
            if (!IsAlive || _burnTimeLeft <= 0f)
            {
                return;
            }

            var dt = Time.deltaTime;
            _burnTimeLeft -= dt;
            ApplyDamage(new DamageEvent(_burnMeta, _burnDps * dt, DamageType.Dot));
        }
    }
}
