// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerController（移动/瞄准）──> PlayerAnimatorDriver（本文件）
//        ──> Animator（MoveX/MoveY/Cast）
// 【职责】把玩法状态翻译成动画参数。它不决定移动，也不决定伤害。
// 【为什么用 MoveX/MoveY 而不是 Speed】双摇杆下角色始终面向瞄准方向，
//        移动可以是横移/后退；2D Blend Tree 才能播 Walk_Left / Run_Backwards。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveYId = Animator.StringToHash("MoveY");
        private static readonly int CastId = Animator.StringToHash("Cast");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        private Animator _animator;
        private PlayerController _player;
        private RuneCaster _caster;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _player = GetComponentInParent<PlayerController>();
            _caster = GetComponentInParent<RuneCaster>();
        }

        private void OnEnable()
        {
            // 施法事件来自 RuneCaster；用事件而不是每帧轮询，避免漏掉一次触发。
            if (_caster != null)
            {
                _caster.CastPerformed += OnCastPerformed;
            }
        }

        private void OnDisable()
        {
            // [坑] 订阅了就必须退订，否则场景重载后旧对象仍会被回调。
            if (_caster != null)
            {
                _caster.CastPerformed -= OnCastPerformed;
            }
        }

        private void Update()
        {
            if (_player == null || _animator == null)
            {
                return;
            }

            var input = _player.MoveInput;
            var aim = _player.AimPivot != null ? _player.AimPivot.rotation : transform.rotation;

            // 把世界移动方向转到"角色本地"：x=右、z=前。
            var local = Quaternion.Inverse(aim) * new Vector3(input.x, 0f, input.y);

            // 用实际速度比例驱动 Walk→Run 的混合，而不是永远播 Run。
            var speed01 = Mathf.Clamp01(_player.HorizontalSpeed / Mathf.Max(0.01f, _player.MoveSpeed));

            _animator.SetFloat(MoveXId, local.x * speed01);
            _animator.SetFloat(MoveYId, local.z * speed01);
        }

        /// <summary>受击动画。由 PlayerHealth 调用；DoT 不调用，避免每帧闪。</summary>
        public void PlayHit()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HitId);
            }
        }

        /// <summary>死亡动画。Dead 是 Bool，进入后不会返回。</summary>
        public void PlayDeath()
        {
            if (_animator != null)
            {
                _animator.SetBool(DeadId, true);
            }
        }

        private void OnCastPerformed()
        {
            // Cast 只在上半身 Layer 消费；Base Layer 的移动不会被打断。
            if (_animator != null)
            {
                _animator.SetTrigger(CastId);
            }
        }
    }
}
