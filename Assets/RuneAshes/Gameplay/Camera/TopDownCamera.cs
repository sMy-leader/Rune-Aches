// ─────────────────────────────────────────────────────────────────────────────
// 【链路】PlayerController（目标）──> TopDownCamera（本文件，LateUpdate 跟随）
// 【职责】固定俯角的跟随相机 + 震屏入口。不做旋转、不做缩放，保证"上=北"的读图习惯。
// 【为什么是 LateUpdate】必须在玩家移动之后采样位置，否则相机会慢一帧、画面抖动。
// 【震屏现状】AddTrauma 已实现但没有调用方；W3 的打击感（受击/爆炸）接这里，
//            配合 PrimeTween 的顿帧使用。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 固定俯角的跟随相机。不做旋转，保证"上=北"的俯视读图习惯。
    /// 震屏走 AddTrauma，与 PrimeTween 的顿帧解耦。
    /// </summary>
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        /// <summary>俯角。55° 是"看得清地面走位、又能看出角色体积"的折中。</summary>
        [SerializeField] private float _pitch = 55f;

        [SerializeField] private float _distance = 9f;

        /// <summary>相机看向玩家脚上方一点，而不是脚底，避免角色偏在画面下缘。</summary>
        [SerializeField] private float _focusHeight = 1f;

        /// <summary>跟随锐度。越大越"贴"，越小越"飘"；14 兼顾响应与平滑。</summary>
        [SerializeField] private float _followSharpness = 14f;

        private Vector3 _shakeOffset;
        private float _trauma;

        public Transform Target
        {
            get { return _target; }
            set { _target = value; }
        }

        /// <summary>累加震屏强度（0~1）。调用方给"伤害占比/爆炸距离"之类的值即可。</summary>
        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;

            if (_target != null)
            {
                var rotation = Quaternion.Euler(_pitch, 0f, 0f);
                var focus = _target.position + Vector3.up * _focusHeight;
                var desired = focus - rotation * Vector3.forward * _distance;

                // 1 - exp(-k·dt) 是帧率无关的平滑系数：
                // 用固定的 Lerp 系数时，高帧率会比低帧率跟得更紧，录屏/掉帧时手感会变。
                var blend = 1f - Mathf.Exp(-_followSharpness * dt);
                transform.position = Vector3.Lerp(transform.position, desired, blend) + _shakeOffset;
                transform.rotation = rotation;
            }

            // 震屏偏移不写进 transform 的跟随目标，而是每帧单独叠加，
            // 这样震屏结束后相机不会带着偏移继续漂。
            _trauma = Mathf.Max(0f, _trauma - dt * 1.5f);
            if (_trauma > 0f)
            {
                // 平方让小幅 trauma 几乎不可见、大幅 trauma 很强烈，比线性更有冲击力。
                var strength = _trauma * _trauma * 0.6f;
                _shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * strength,
                    (Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f) * strength,
                    0f);
            }
            else
            {
                _shakeOffset = Vector3.zero;
            }
        }
    }
}
