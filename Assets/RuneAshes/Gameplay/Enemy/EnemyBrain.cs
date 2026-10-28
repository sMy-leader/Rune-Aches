// ─────────────────────────────────────────────────────────────────────────────
// 【链路】EnemyActor（HP/受击/死亡）──> EnemyBrain（本文件，行为状态机）
//        ──> EnemyAnimatorDriver ──> Animator
// 【职责】近战敌人的行为：发现 → 追击 → 前摇 → 攻击 → 恢复 → 冷却。
// 【为什么用代码 FSM 而不是 Animator 状态机】
//   Animator 只负责"播什么"；"什么时候追、什么时候打"必须可测试、可降频、
//   将来换成 Flow Field / Job 时不用重写动画资产。
// 【W3 扩展】远程/自爆怪复用同一个接口：改 _attackRange / _windup / 命中逻辑即可。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Chase,
            AttackWindup,
            AttackRecover,
            HitStun,
            Dead
        }

        [Header("Detection")]
        [SerializeField] private float _detectRange = 12f;
        [SerializeField] private float _attackRange = 2.2f;

        [Header("Move")]
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _turnSpeed = 540f;

        [Header("Attack")]
        [SerializeField] private float _attackCooldown = 1.2f;
        [SerializeField] private float _windupDuration = 0.15f;
        [SerializeField] private float _hitDelay = 0.5f;
        [SerializeField] private float _recoverDuration = 0.8f;
        [SerializeField] private float _damage = 15f;
        [SerializeField] private float _hitReach = 2.8f;

        [Header("Hit")]
        [SerializeField] private float _hitStunDuration = 0.25f;

        private CharacterController _controller;
        private EnemyAnimatorDriver _animator;
        private EnemyActor _actor;
        private PlayerHealth _target;

        private State _state = State.Idle;
        private float _stateTime;
        private float _cooldown;
        private bool _didHit;

        public bool IsDead
        {
            get { return _state == State.Dead; }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<EnemyAnimatorDriver>();
            _actor = GetComponent<EnemyActor>();
        }

        private void OnEnable()
        {
            if (_actor != null)
            {
                _actor.Damaged += OnDamaged;
                _actor.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_actor != null)
            {
                _actor.Damaged -= OnDamaged;
                _actor.Died -= OnDied;
            }
        }

        private void Start()
        {
            _target = FindFirstObjectByType<PlayerHealth>();
            if (_animator != null)
            {
                _animator.SetLocomotion(0f);
            }
        }

        private void Update()
        {
            if (_state == State.Dead)
            {
                return;
            }

            var dt = Time.deltaTime;
            _stateTime += dt;
            _cooldown = Mathf.Max(0f, _cooldown - dt);

            if (_target == null || !_target.IsAlive)
            {
                _target = FindFirstObjectByType<PlayerHealth>();
                if (_target == null || !_target.IsAlive)
                {
                    if (_animator != null)
                    {
                        _animator.SetLocomotion(0f);
                    }

                    return;
                }
            }

            var toTarget = _target.Position - transform.position;
            toTarget.y = 0f;
            var distance = toTarget.magnitude;

            switch (_state)
            {
                case State.Idle:
                    _animator.SetLocomotion(0f);
                    if (distance <= _detectRange)
                    {
                        Enter(State.Chase);
                    }

                    break;

                case State.Chase:
                    Face(toTarget, dt);

                    if (distance <= _attackRange && _cooldown <= 0f)
                    {
                        Enter(State.AttackWindup);
                    }
                    else if (distance > _attackRange)
                    {
                        Move(toTarget.normalized, dt);
                        _animator.SetLocomotion(1f);
                    }
                    else
                    {
                        // 已经在攻击距离内但还在冷却：停下来等，不要贴脸抖动。
                        _animator.SetLocomotion(0f);
                    }

                    break;

                case State.AttackWindup:
                    Face(toTarget, dt);
                    _animator.SetLocomotion(0f);
                    if (_stateTime >= _windupDuration)
                    {
                        _animator.PlayAttack();
                        Enter(State.AttackRecover);
                    }

                    break;

                case State.AttackRecover:
                    // 命中帧用固定延迟，而不是 Animation Event：
                    // 切片期更好调，也不依赖 FBX 里是否加了事件。
                    if (!_didHit && _stateTime >= _hitDelay)
                    {
                        _didHit = true;
                        TryDealDamage(distance);
                    }

                    if (_stateTime >= _recoverDuration)
                    {
                        _cooldown = _attackCooldown;
                        Enter(State.Chase);
                    }

                    break;

                case State.HitStun:
                    _animator.SetLocomotion(0f);
                    if (_stateTime >= _hitStunDuration)
                    {
                        Enter(State.Chase);
                    }

                    break;
            }
        }

        private void Enter(State next)
        {
            _state = next;
            _stateTime = 0f;
            _didHit = false;
        }

        private void Move(Vector3 direction, float dt)
        {
            if (_controller == null || !_controller.enabled)
            {
                return;
            }

            _controller.Move(direction * (_moveSpeed * dt));
        }

        private void Face(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * dt);
        }

        private void TryDealDamage(float distance)
        {
            if (_target == null || distance > _hitReach)
            {
                return;
            }

            // 敌人攻击不属于符文链：ChainId=0、Depth=0，不会被递归闸门误判。
            var meta = new CastMeta { ChainId = 0, CastId = 0, Depth = 0, Seed = 0 };
            _target.ApplyDamage(new DamageEvent(meta, _damage, DamageType.Direct));
        }

        private void OnDamaged(EnemyActor actor, DamageEvent damage)
        {
            if (_state == State.Dead)
            {
                return;
            }

            // DoT 不触发硬直，否则燃烧会把敌人永久锁在受击状态。
            if (damage.Type == DamageType.Dot)
            {
                return;
            }

            if (_animator != null)
            {
                _animator.PlayHit();
            }

            Enter(State.HitStun);
        }

        private void OnDied(EnemyActor actor, DamageEvent damage)
        {
            Enter(State.Dead);

            if (_animator != null)
            {
                _animator.PlayDeath();
            }

            if (_controller != null)
            {
                _controller.enabled = false;
            }

            // 留 2 秒播死亡动画；W4 接溶解 Shader 后改成溶解结束再销毁。
            Destroy(gameObject, 2f);
        }
    }
}
