// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Projectile.Hit / UnityRuneWorld.DamageArea ──> IDamageable.ApplyDamage
//        ──> EnemyDummy（本文件）──> Killed 事件 ──> （W3）经验掉落 / T2 击杀触发
// 【职责】W2 的训练靶：验证"伤害能落到目标身上 + 燃烧能持续掉血 + 死亡能发事件"。
// 【W3 会替换】正式敌人需要移动/攻击/掉落/受击反馈；本类保留在测试场景里，
//            因为它是最小、最可控的 IDamageable 实现。
// ─────────────────────────────────────────────────────────────────────────────
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

        /// <summary>
        /// [坑] 静态事件：场景里所有靶子共享同一个订阅列表。
        /// W3 换成实例事件或事件总线；在那之前，订阅方必须在 OnDisable/OnDestroy 退订，
        /// 否则场景重载后旧对象仍会被回调（编辑器里表现为 MissingReferenceException）。
        /// </summary>
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

            // DoT 不闪白：否则燃烧的每一帧都会把靶子刷成白色，反而看不出命中反馈。
            if (damage.Type != DamageType.Dot)
            {
                _flashTimer = _hitFlashDuration;
            }

            if (_health <= 0f)
            {
                _health = 0f;
                _burnTimeLeft = 0f;

                // 先发事件、再隐藏：订阅方（掉落/计数/触发链）还能读到死亡位置。
                // 顺序反过来就只能拿到 inactive 对象，位置和组件都不方便用。
                if (Killed != null)
                {
                    Killed(this, damage);
                }

                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 燃烧采用"取最大值"而不是叠加层数：后一次燃烧只会刷新更疼的那个。
        /// 这是《符文语义规范》里定死的规则——否则多个燃烧来源会在同一目标上
        /// 指数级叠伤害，几秒内秒掉 Boss。
        /// </summary>
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

            // 燃烧按帧结算（dps × dt），不是固定 0.5s 跳一次。
            // 好处是总伤害与帧率无关；代价是掉帧时飘字频率会变，W3 做伤害数字时统一按 0.2s 聚合显示。
            if (_burnTimeLeft > 0f && IsAlive)
            {
                _burnTimeLeft -= dt;
                ApplyDamage(new DamageEvent(_burnMeta, _burnDps * dt, DamageType.Dot));
            }

            if (_material == null)
            {
                return;
            }

            // [注意] _renderer.material 会创建一份材质实例，每个靶子一份。
            // 训练场景可以接受；正式敌人数量大时应改用 MaterialPropertyBlock，避免材质实例爆内存。
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
