// ─────────────────────────────────────────────────────────────────────────────
// 【链路】EnemyActor（HP/受击/死亡）──> EnemyBrain（本文件，行为状态机）
//        ──> EnemyAnimatorDriver ──> Animator
// 【职责】近战敌人的行为：发现 → 追击 → 前摇 → 攻击 → 恢复 → 冷却。
// 【为什么用代码 FSM 而不是 Animator 状态机】
//   Animator 只负责"播什么"；"什么时候追、什么时候打"必须可测试、可降频、
//   将来换成 Flow Field / Job 时不用重写动画资产。
// 【W3 扩展】远程/自爆怪复用同一个接口：改 _attackRange / _windup / 命中逻辑即可。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public enum EnemyKind
    {
        Melee,
        Ranged,
        Bomber
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Chase,
            AttackWindup,
            AttackRecover,
            Aim,
            Fuse,
            HitStun,
            Dead
        }

        [Header("Kind")]
        [SerializeField] private EnemyKind _kind = EnemyKind.Melee;

        [Header("Detection")]
        [SerializeField] private float _detectRange = 12f;
        [SerializeField] private float _attackRange = 2.2f;

        [Header("Move")]
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _turnSpeed = 540f;

        [Header("Attack")]
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _windupDuration = 0.15f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _hitReach = 2.8f;

        [Header("Ranged")]
        [SerializeField] private float _rangedRange = 7f;
        [SerializeField] private float _rangedMinRange = 4f;
        [SerializeField] private float _rangedWindup = 0.4f;
        [SerializeField] private float _rangedCooldown = 2f;
        [SerializeField] private float _projectileDamage = 8f;
        [SerializeField] private float _projectileSpeed = 10f;

        [Header("Bomber")]
        [SerializeField] private float _fuseRange = 2.2f;
        [SerializeField] private float _fuseDuration = 0.8f;
        [SerializeField] private float _explosionRadius = 3f;
        [SerializeField] private float _explosionDamage = 20f;
        [SerializeField] private float _explosionDamageDelay = 0.3f;
        [SerializeField] private float _explosionDeathDelay = 1.1f;

        [Header("Hit")]
        [SerializeField] private float _hitStunDuration = 0.25f;

        private CharacterController _controller;
        private IEnemyAnimator _animator;
        private EnemyActor _actor;
        private PlayerHealth _target;
        private UnityRuneWorld _world;

        private State _state = State.Idle;
        private float _stateTime;
        private float _cooldown;
        private bool _didHit;
        private bool _exploding;

        /// <summary>
        /// 这一刀是否真的从第 0 帧开始播放过。
        /// 防止 Animator 还停在上一刀的 Attack 状态时，AI 用旧归一化时间直接结算伤害。
        /// </summary>
        private bool _attackStateStarted;

        public bool IsDead
        {
            get { return _state == State.Dead; }
        }

        public EnemyKind Kind
        {
            get { return _kind; }
        }

        /// <summary>由 WaveSpawner 在实例激活前设置；模板本身保持 Melee。</summary>
        public void SetKind(EnemyKind kind)
        {
            _kind = kind;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponent<IEnemyAnimator>();
            _actor = GetComponent<EnemyActor>();
            _world = FindFirstObjectByType<UnityRuneWorld>();
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

            ApplyKindVisual();
        }

        private void ApplyKindVisual()
        {
            // 只有占位 Troll 需要按 Kind 染色；真实 Wraith/Bomber 使用自己的材质。
            if (_kind != EnemyKind.Melee)
            {
                return;
            }

            var color = Color.white;
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = color;
                }
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
                _target = FindFirstObjectByType<PlayerHealth>();//通过挂载的血量脚本找场上玩家
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

            // 冻结期间完全停手；减速只影响 Move，不阻断转向/攻击判定。
            if (_actor != null && _actor.IsFrozen)
            {
                if (_animator != null)
                {
                    _animator.SetLocomotion(0f);
                }

                return;
            }

            switch (_state)
            {
                case State.Idle:
                    _animator.SetLocomotion(0f);
                    //达到距离->追击
                    if (distance <= _detectRange)
                    {
                        Enter(State.Chase);
                    }

                    break;

                case State.Chase:
                    Face(toTarget, dt);

                    if (_kind == EnemyKind.Ranged)
                    {
                        if (distance < _rangedMinRange)
                        {
                            // 太近就后撤，和近战/自爆的贴脸行为区分开。
                            Move(-toTarget.normalized, dt);
                            _animator.SetLocomotion(1f);
                        }
                        else if (distance > _rangedRange)
                        {
                            Move(toTarget.normalized, dt);
                            _animator.SetLocomotion(1f);
                        }
                        else
                        {
                            _animator.SetLocomotion(0f);
                            if (_cooldown <= 0f)
                            {
                                Enter(State.Aim);
                            }
                        }

                        break;
                    }

                    if (_kind == EnemyKind.Bomber)
                    {
                        if (distance > _fuseRange)
                        {
                            Move(toTarget.normalized, dt);
                            _animator.SetLocomotion(1f);
                        }
                        else
                        {
                            _animator.SetLocomotion(0f);
                            if (_cooldown <= 0f)
                            {
                                Enter(State.Fuse);
                            }
                        }

                        break;
                    }

                    if (distance <= _attackRange && _cooldown <= 0f)
                    {
                        Enter(State.AttackWindup);//到达攻击距离
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

                case State.Aim:
                    Face(toTarget, dt);
                    _animator.SetLocomotion(0f);
                    if (_stateTime >= _rangedWindup)
                    {
                        FireProjectile();
                        _cooldown = _rangedCooldown;
                        Enter(State.Chase);
                    }

                    break;

                case State.Fuse:
                    Face(toTarget, dt);
                    _animator.SetLocomotion(0f);
                    // 引信期间轻微膨胀，给玩家“要炸了”的占位反馈。
                    var pulse = 1f + Mathf.Sin(_stateTime * 26f) * 0.06f;
                    transform.localScale = Vector3.one * pulse;
                    if (_stateTime >= _fuseDuration)
                    {
                        Explode();
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
                    var inAttack = _animator != null && _animator.IsInAttackState();
                    var normalized = _animator != null ? _animator.CurrentNormalizedTime() : 0f;

                    // 只有确认这一刀从起始段开始，才承认它；
                    // 防止 Animator 还停在上一刀的 Attack 状态时，用旧归一化时间直接结算。
                    if (inAttack && normalized < 0.25f)
                    {
                        _attackStateStarted = true;
                    }
                    //根据三段Attack的不同时间做归一化->确保播到劈下瞬间触发hit
                    if (_attackStateStarted && !_didHit && inAttack &&
                        normalized >= _animator.CurrentAttackImpactNormalized())
                    {
                        _didHit = true;
                        TryDealDamage(distance);
                    }

                    // 动画播完才回 Chase；不要用固定 2s 提前打断 2.7s 的攻击 clip。
                    if (_attackStateStarted && (!inAttack || normalized >= 0.95f))
                    {
                        _cooldown = _attackCooldown;
                        Enter(State.Chase);
                    }
                    else if (!_attackStateStarted && _stateTime > 0.5f)
                    {
                        // 动画没有按预期启动（被其他状态卡住）：回 Chase，避免 AI 卡死。
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
        /// <summary>
        /// 状态机的统一进入点：重置计时器、攻击标记等。（初始重置）
        /// </summary>
        /// <param name="next"></param>
        private void Enter(State next)
        {
            _state = next;
            _stateTime = 0f;
            _didHit = false;
            _attackStateStarted = false;
        }

        private void Move(Vector3 direction, float dt)
        {
            if (_controller == null || !_controller.enabled)
            {
                return;
            }

            var speed = _moveSpeed * (_actor != null ? _actor.SpeedMultiplier : 1f);
            _controller.Move(direction * (speed * dt));
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

        private void FireProjectile()
        {
            if (_target == null || !_target.IsAlive)
            {
                return;
            }

            var origin = transform.position + Vector3.up * 1.3f;
            var targetPoint = _target.Position + Vector3.up * 1.0f;
            var direction = targetPoint - origin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (_animator != null)
            {
                _animator.PlayAttack();
            }

            EnemyProjectile.Spawn(origin, direction.normalized, _projectileDamage, _projectileSpeed, _target);
        }

        private void Explode()
        {
            if (_exploding)
            {
                return;
            }

            _exploding = true;
            VfxService.Play(VfxId.BomberExplode, transform.position, Quaternion.identity);

            if (_animator != null)
            {
                _animator.PlayExplode();
            }

            // 绑定到组件销毁，对象被 Destroy 时自动取消，避免访问已销毁的成员。
            ExplodeAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ExplodeAsync(CancellationToken token)
        {
            try
            {
                // 先让 explode 动画进入，再把伤害结算到玩家。
                // WaitForSeconds 用的是 scaled 时间，UniTask.Delay 默认同样是 scaled，行为一致。
                if (_explosionDamageDelay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_explosionDamageDelay), cancellationToken: token);
                }

                var meta = new CastMeta { ChainId = 0, CastId = 0, Depth = 0, Seed = 0 };
                if (_target != null && _target.IsAlive)
                {
                    var distance = Vector3.Distance(transform.position, _target.Position);
                    if (distance <= _explosionRadius)
                    {
                        _target.ApplyDamage(new DamageEvent(meta, _explosionDamage, DamageType.Explosion));
                    }
                }

                // 爆炸动画播到后段再让本体死亡，避免 death 立刻覆盖 explode。
                var remaining = Mathf.Max(0f, _explosionDeathDelay - _explosionDamageDelay);
                if (remaining > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(remaining), cancellationToken: token);
                }

                transform.localScale = Vector3.one;
                if (_actor != null)
                {
                    _actor.ApplyDamage(new DamageEvent(meta, _actor.MaxHealth + 1f, DamageType.Explosion));
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            catch (OperationCanceledException)
            {
                // 对象已销毁，忽略即可
            }
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

            // T2 击杀回响：把击杀事件发给玩家世界；TriggerRouter 决定要不要再施法。
            if (_world != null)
            {
                _world.RaiseKill(damage, actor);
            }

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
