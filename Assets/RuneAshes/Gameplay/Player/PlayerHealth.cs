// ─────────────────────────────────────────────────────────────────────────────
// 【链路】敌人攻击 / 弹幕 ──> IDamageable.ApplyDamage ──> PlayerHealth（本文件）
//        ──> PlayerAnimatorDriver.PlayHit/PlayDeath
// 【职责】玩家的 HP、无敌帧、受击/死亡表现。W2 的玩家没有任何受伤入口，
//        这个类是 W3a「玩家会被打死」的最小实现。
// 【W3 扩展】死亡结算、复活、伤害数字都从这里接。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable, IDebuffable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _health;
        private bool _dead;
        private PlayerController _player;
        private PlayerAnimatorDriver _animator;
        private RuneCaster _caster;

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

        private void Awake()
        {
            _health = _maxHealth;
            _player = GetComponent<PlayerController>();
            _animator = GetComponentInChildren<PlayerAnimatorDriver>(true);
            _caster = GetComponent<RuneCaster>();
        }

        public void ApplyDamage(DamageEvent damage)
        {
            if (!IsAlive)
            {
                return;
            }

            // 冲刺无敌：整个冲刺过程免疫，和 PlayerController 的 _dashInvulnerability 对齐。
            if (_player != null && _player.IsInvulnerable)
            {
                return;
            }

            _health -= damage.Amount;

            if (_health <= 0f)
            {
                _health = 0f;
                _dead = true;

                if (_animator != null)
                {
                    _animator.PlayDeath();
                }

                // 停止移动与开火，但不销毁对象：死亡动画还要播完。
                if (_player != null)
                {
                    _player.enabled = false;
                }

                if (_caster != null)
                {
                    _caster.enabled = false;
                }

                return;
            }

            // DoT 不触发受击动画：否则燃烧每帧都会把角色打进受击状态。
            if (damage.Type != DamageType.Dot && _animator != null)
            {
                _animator.PlayHit();
            }
        }

        public void ApplyBurn(float dps, float duration, CastMeta meta)
        {
            // 玩家暂时不吃燃烧；W3 敌人词缀接入时再实现。
        }

        public void ApplySlow(float percent, float duration)
        {
            // 玩家减速会影响手感，等 W3 的敌人词缀设计确认后再接。
        }

        /// <summary>调试/复活用：恢复满血并重新启用控制。</summary>
        public void ResetHealth()
        {
            _health = _maxHealth;
            _dead = false;
            if (_player != null)
            {
                _player.enabled = true;
            }

            if (_caster != null)
            {
                _caster.enabled = true;
            }
        }
    }
}
