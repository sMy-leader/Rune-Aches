// ─────────────────────────────────────────────────────────────────────────────
// 【链路】IInputSource ──> PlayerController（本文件）──> AimDirection / IsDashing
//        ──> RuneCaster.Cast(direction)、（W3）PlayerHealth 的冲刺无敌
// 【职责】把"玩家意图"变成位移与瞄准方向。它不关心符文、不关心伤害。
// 【为什么用 CharacterController】不需要刚体物理/重力，移动完全由代码控制，
//                              碰撞响应可预测，也避免弹幕游戏里常见的刚体抖动。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 双摇杆玩家控制器：WASD/左摇杆移动，鼠标/右摇杆瞄准，Space/B 冲刺。
    /// 冲刺 0.25s 无敌 + 2.5s 冷却，是 T4/E5 符文的载体。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float _moveSpeed = 6f;

        /// <summary>加速度。不是瞬间到最大速度，让起步/转向有一点重量感。</summary>
        [SerializeField] private float _acceleration = 70f;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 18f;
        [SerializeField] private float _dashDuration = 0.25f;
        [SerializeField] private float _dashCooldown = 2.5f;

        /// <summary>无敌时间与冲刺时长相同：整个冲刺过程都不可被伤害。</summary>
        [SerializeField] private float _dashInvulnerability = 0.25f;

        [Header("Aim")]
        [SerializeField] private Transform _aimPivot;

        /// <summary>鼠标射线与哪个高度的水平面求交。0 表示地面；角色中心大约 0.9m。</summary>
        [SerializeField] private float _groundPlaneHeight;

        private CharacterController _controller;
        private IInputSource _input;
        private Camera _camera;
        private UnityRuneWorld _world;

        private Vector3 _horizontalVelocity;
        private Vector3 _aimDirection = Vector3.forward;
        private Vector3 _dashDirection;
        private float _dashTimeLeft;
        private float _dashCooldownLeft;
        private float _invulnerabilityLeft;
        private bool _isDashing;

        public Vector3 AimDirection
        {
            get { return _aimDirection; }
        }

        /// <summary>当前移动输入（-1~1）。动画驱动用它算本地 MoveX/MoveY。</summary>
        public Vector2 MoveInput
        {
            get { return _input != null ? _input.Move : Vector2.zero; }
        }

        /// <summary>普通移动速度。动画用 HorizontalSpeed / MoveSpeed 得到 0~1 的混合权重。</summary>
        public float MoveSpeed
        {
            get { return _moveSpeed; }
        }

        /// <summary>当前水平速度（含冲刺）。</summary>
        public float HorizontalSpeed
        {
            get { return new Vector2(_horizontalVelocity.x, _horizontalVelocity.z).magnitude; }
        }

        /// <summary>瞄准挂点。动画模型挂在它下面，所以它也是角色的朝向。</summary>
        public Transform AimPivot
        {
            get { return _aimPivot; }
        }

        public bool IsDashing
        {
            get { return _isDashing; }
        }

        /// <summary>T4 冲刺余波用：最近一次冲刺的方向。</summary>
        public Vector3 LastDashDirection
        {
            get { return _dashDirection; }
        }

        public bool IsInvulnerable
        {
            get { return _invulnerabilityLeft > 0f; }
        }

        /// <summary>0 = 可用，1 = 刚进入冷却。</summary>
        public float DashCooldown01
        {
            get { return _dashCooldown <= 0f ? 0f : Mathf.Clamp01(_dashCooldownLeft / _dashCooldown); }
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<IInputSource>();
            _camera = Camera.main;
            _world = GetComponent<UnityRuneWorld>();

            if (_input == null)
            {
                Debug.LogError("[Player] No IInputSource on " + name + ".", this);
            }
        }

        private void Update()
        {
            if (_input == null)
            {
                return;
            }

            var dt = Time.deltaTime;

            // 先推进计时器，再读输入：这样冷却结束的同一帧就能再次冲刺/受伤。
            _dashCooldownLeft = Mathf.Max(0f, _dashCooldownLeft - dt);
            _invulnerabilityLeft = Mathf.Max(0f, _invulnerabilityLeft - dt);

            // 顺序有讲究：先瞄准（决定冲刺兜底方向），再处理冲刺，最后移动。
            UpdateAim();
            HandleDashInput();
            UpdateMovement(dt);

            // 只转视觉挂点，不转整个角色：CharacterController 的碰撞胶囊不受影响。
            if (_aimPivot != null)
            {
                _aimPivot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
            }
        }

        /// <summary>
        /// 两套瞄准输入：
        /// - 手柄：右摇杆方向，超过死区才更新（否则松手会瞬间朝 0 向量）；
        /// - 鼠标：屏幕射线与水平面求交，再算"玩家 → 交点"的方向。
        /// [为什么用 Plane 而不是 Physics.Raycast] 不依赖地面是否有 Collider，
        /// 也不会被场景里的障碍/触发器挡住，瞄准手感更稳定。
        /// </summary>
        private void UpdateAim()
        {
            if (_input.IsGamepadActive)
            {
                var stick = _input.Aim;
                var direction = new Vector3(stick.x, 0f, stick.y);
                if (direction.sqrMagnitude > 0.04f)
                {
                    _aimDirection = direction.normalized;
                }

                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            var ray = _camera.ScreenPointToRay(_input.Aim);
            var plane = new Plane(Vector3.up, new Vector3(0f, _groundPlaneHeight, 0f));
            float enter;
            if (!plane.Raycast(ray, out enter))
            {
                return;
            }

            var directionToCursor = ray.GetPoint(enter) - transform.position;
            directionToCursor.y = 0f;
            if (directionToCursor.sqrMagnitude > 0.0001f)
            {
                _aimDirection = directionToCursor.normalized;
            }
        }

        /// <summary>
        /// 冲刺输入：冷却中/正在冲刺时忽略。
        /// 方向优先取移动输入（想往哪冲），没有移动输入才朝瞄准方向冲。
        /// 冷却与无敌同时开始计时——所以无敌覆盖整个冲刺动作，但不会额外延长。
        /// </summary>
        private void HandleDashInput()
        {
            if (!_input.DashPressed || _isDashing || _dashCooldownLeft > 0f)
            {
                return;
            }

            var move = new Vector3(_input.Move.x, 0f, _input.Move.y);
            _dashDirection = move.sqrMagnitude > 0.01f ? move.normalized : _aimDirection;
            _dashTimeLeft = _dashDuration;
            _dashCooldownLeft = _dashCooldown;
            _invulnerabilityLeft = _dashInvulnerability;
            _isDashing = true;
        }

        /// <summary>
        /// 位移。冲刺阶段直接覆盖速度；结束后把速度保留为普通移动速度，
        /// 避免"冲刺一停就急停"的顿挫感（如果手感太滑，把这里改成 0 即可）。
        /// 普通移动用 MoveTowards 做加速度平滑，并把斜向输入归一化，防止斜着走更快。
        /// </summary>
        private void UpdateMovement(float dt)
        {
            if (_isDashing)
            {
                _dashTimeLeft -= dt;
                _controller.Move(_dashDirection * (_dashSpeed * dt));
                if (_dashTimeLeft <= 0f)
                {
                    _isDashing = false;
                    _horizontalVelocity = _dashDirection * _moveSpeed;

                    // T4 冲刺余波：冲刺结束的同一帧发事件。
                    if (_world != null)
                    {
                        _world.RaiseDashCompleted();
                    }
                }

                return;
            }

            var move = new Vector3(_input.Move.x, 0f, _input.Move.y);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            var targetVelocity = move * _moveSpeed;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, _acceleration * dt);
            _controller.Move(_horizontalVelocity * dt);
        }
    }
}
