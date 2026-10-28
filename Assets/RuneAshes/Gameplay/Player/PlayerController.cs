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
        [SerializeField] private float _acceleration = 70f;

        [Header("Dash")]
        [SerializeField] private float _dashSpeed = 18f;
        [SerializeField] private float _dashDuration = 0.25f;
        [SerializeField] private float _dashCooldown = 2.5f;
        [SerializeField] private float _dashInvulnerability = 0.25f;

        [Header("Aim")]
        [SerializeField] private Transform _aimPivot;
        [SerializeField] private float _groundPlaneHeight;

        private CharacterController _controller;
        private IInputSource _input;
        private Camera _camera;

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

        public bool IsDashing
        {
            get { return _isDashing; }
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
            _dashCooldownLeft = Mathf.Max(0f, _dashCooldownLeft - dt);
            _invulnerabilityLeft = Mathf.Max(0f, _invulnerabilityLeft - dt);

            UpdateAim();
            HandleDashInput();
            UpdateMovement(dt);

            if (_aimPivot != null)
            {
                _aimPivot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
            }
        }

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
