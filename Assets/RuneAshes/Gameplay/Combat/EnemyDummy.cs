using System;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 训练靶：验证符文输出用。死亡只隐藏，方便反复测试。
    /// 实现 IDebuffable 以验证燃烧；减速等 W3 有移动的敌人再接。
    /// </summary>
    public sealed class EnemyDummy : MonoBehaviour, IDamageable, IDebuffable
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Color _hitColor = Color.white;
        [SerializeField] private float _hitFlashDuration = 0.08f;

        private float _health;
        private float _flashTimer;
        private Color _baseColor;
        private Material _material;

        private float _burnDps;
        private float _burnTimeLeft;
        private CastMeta _burnMeta;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public static event Action<EnemyDummy, DamageEvent> Killed;

        public bool IsAlive
        {
            get { return _health > 0f; }
        }

        public Vector3 Position
        {
            get { return transform.position; }
        }

        public float Health
        {
            get { return _health; }
        }

        private void Awake()
        {
            _health = _maxHealth;
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                _material = _renderer.material;
                _baseColor = _material.GetColor(BaseColorId);
            }
        }

        public void ApplyDamage(DamageEvent damage)
        {
            if (!IsAlive)
            {
                return;
            }

            _health -= damage.Amount;
            if (damage.Type != DamageType.Dot)
            {
                _flashTimer = _hitFlashDuration;
            }

            if (_health <= 0f)
            {
                _health = 0f;
                _burnTimeLeft = 0f;
                if (Killed != null)
                {
                    Killed(this, damage);
                }

                gameObject.SetActive(false);
            }
        }

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
            // W3 接入移动后实现。
        }

        public void ResetHealth()
        {
            _health = _maxHealth;
            _burnTimeLeft = 0f;
            gameObject.SetActive(true);
        }

        public void SetMaxHealth(float value)
        {
            _maxHealth = Mathf.Max(1f, value);
            _health = _maxHealth;
        }

        private void Update()
        {
            var dt = Time.deltaTime;

            if (_burnTimeLeft > 0f && IsAlive)
            {
                _burnTimeLeft -= dt;
                ApplyDamage(new DamageEvent(_burnMeta, _burnDps * dt, DamageType.Dot));
            }

            if (_material == null)
            {
                return;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= dt;
                _material.SetColor(BaseColorId, _hitColor);
            }
            else
            {
                _material.SetColor(BaseColorId, _baseColor);
            }
        }
    }
}
