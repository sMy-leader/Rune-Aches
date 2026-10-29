// ─────────────────────────────────────────────────────────────────────────────
// 【链路】UnityRuneWorld 的 Hit/Kill/DashCompleted/PlayerInjured 事件
//        ──> TriggerRouter（本文件）──> RuneCaster.CastFromEvent()
// 【职责】把"发生了什么"翻译成"这条链要不要再施法"。
//        RuneCaster 只负责执行；触发条件、ICD、概率、深度闸门都在这里。
// 【为什么单独一层】T2–T6 会同时存在多个事件源；如果全塞进 RuneCaster，
//        Update 会变成一张巨大的 switch，也没法单独测触发规则。
// 【深度】事件派生施法的 depth = 触发源伤害的 Depth + 1；
//        RuneCaster.CastFromEvent 会用 RuneRules.CanTrigger 砍断递归。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Framework;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(RuneCaster))]
    public sealed class TriggerRouter : MonoBehaviour
    {
        [Header("T3 命中传导")]
        [SerializeField] private float _hitChance = 0.25f;
        [SerializeField] private float _hitIcd = 0.05f;

        [Header("T5 受伤反击")]
        [SerializeField] private float _injuryIcd = 1f;

        [Header("T6 常驻光环")]
        [SerializeField] private float _auraInterval = 0.5f;
        [SerializeField] private float _auraRadius = 4f;

        /// <summary>光环每 tick 的伤害 = 效果基础值 × 这个比例。W4 再改成走效果原语。</summary>
        [SerializeField] private float _auraDamageRatio = 0.3f;

        private RuneCaster _caster;
        private UnityRuneWorld _world;
        private PlayerController _player;

        private readonly SameFrameGate _sameFrame = new SameFrameGate();
        private XorShiftRandom _random;
        private float _lastHitTime = -999f;
        private float _lastInjuryTime = -999f;
        private float _auraTimer;

        private void Awake()
        {
            _caster = GetComponent<RuneCaster>();
            _world = GetComponent<UnityRuneWorld>();
            _player = GetComponent<PlayerController>();

            // 固定种子：同样的战斗序列 → 同样的 T3 触发结果，便于复现。
            _random = new XorShiftRandom(0x5EED1234u);
        }

        private void OnEnable()
        {
            if (_world == null)
            {
                return;
            }

            _world.Hit += OnHit;
            _world.Kill += OnKill;
            _world.DashCompleted += OnDashCompleted;
            _world.PlayerInjured += OnPlayerInjured;
        }

        private void OnDisable()
        {
            if (_world == null)
            {
                return;
            }

            // [坑] 订阅了就必须退订，否则场景重载后旧对象仍会被回调。
            _world.Hit -= OnHit;
            _world.Kill -= OnKill;
            _world.DashCompleted -= OnDashCompleted;
            _world.PlayerInjured -= OnPlayerInjured;
        }

        private void Update()
        {
            if (!IsTrigger("T6"))
            {
                // 切走 T6 时清掉计时，切回来不会立刻炸一次。
                _auraTimer = 0f;
                return;
            }

            if (_world == null || _caster == null)
            {
                return;
            }

            _auraTimer += Time.deltaTime;
            if (_auraTimer < _auraInterval)
            {
                return;
            }

            // 光环不做补发：卡顿/后台回来后 _auraTimer 可能累积很大，
            // 用减法会连续 tick 十几帧，一次把满血目标打空。
            _auraTimer = 0f;

            _world.DamageArea(new AreaDamageSpec
            {
                Center = transform.position,
                Radius = _auraRadius,
                Damage = _caster.EffectBaseValue * _auraDamageRatio,
                Type = DamageType.Direct,
                Meta = new CastMeta
                {
                    ChainId = _caster.ChainId,
                    CastId = 0,
                    Depth = 1,
                    Seed = 0
                }
            });
        }

        // ── T2 击杀回响 ──────────────────────────────────────────────────────
        private void OnKill(DamageEvent damage, IDamageable target)
        {
            if (!IsTrigger("T2"))
            {
                return;
            }

            // 同帧同链只触发一次：散射一帧打死 5 个不会连开 5 次。
            if (!_sameFrame.ShouldMerge(_caster.ChainId, Time.frameCount))
            {
                return;
            }

            _caster.CastFromEvent(AimDirection, damage.Depth + 1);
        }

        // ── T3 命中传导 ──────────────────────────────────────────────────────
        private void OnHit(DamageEvent damage, IDamageable target)
        {
            if (damage.Type == DamageType.Dot)
            {
                return;
            }

            if (!IsTrigger("T3"))
            {
                return;
            }

            if (Time.time - _lastHitTime < _hitIcd)
            {
                return;
            }

            if (!_random.Chance(_hitChance))
            {
                return;
            }

            _lastHitTime = Time.time;

            if (!_sameFrame.ShouldMerge(_caster.ChainId, Time.frameCount))
            {
                return;
            }

            _caster.CastFromEvent(AimDirection, damage.Depth + 1);
        }

        // ── T4 冲刺余波 ──────────────────────────────────────────────────────
        private void OnDashCompleted()
        {
            if (!IsTrigger("T4"))
            {
                return;
            }

            var direction = _player != null ? _player.LastDashDirection : transform.forward;
            _caster.CastFromEvent(direction, 1);
        }

        // ── T5 受伤反击 ──────────────────────────────────────────────────────
        private void OnPlayerInjured(DamageEvent damage)
        {
            if (damage.Type == DamageType.Dot)
            {
                return;
            }

            if (!IsTrigger("T5"))
            {
                return;
            }

            if (Time.time - _lastInjuryTime < _injuryIcd)
            {
                return;
            }

            _lastInjuryTime = Time.time;

            if (!_sameFrame.ShouldMerge(_caster.ChainId, Time.frameCount))
            {
                return;
            }

            _caster.CastFromEvent(AimDirection, damage.Depth + 1);
        }

        private Vector3 AimDirection
        {
            get
            {
                if (_player != null && _player.AimDirection.sqrMagnitude > 0.0001f)
                {
                    return _player.AimDirection;
                }

                return transform.forward;
            }
        }

        private bool IsTrigger(string id)
        {
            return _caster != null && _caster.Chain != null &&
                   _caster.Chain.Trigger != null && _caster.Chain.Trigger.Id == id;
        }
    }
}
