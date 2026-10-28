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
        [SerializeField] private float _pitch = 55f;
        [SerializeField] private float _distance = 9f;
        [SerializeField] private float _focusHeight = 1f;
        [SerializeField] private float _followSharpness = 14f;

        private Vector3 _shakeOffset;
        private float _trauma;

        public Transform Target
        {
            get { return _target; }
            set { _target = value; }
        }

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
                var blend = 1f - Mathf.Exp(-_followSharpness * dt);
                transform.position = Vector3.Lerp(transform.position, desired, blend) + _shakeOffset;
                transform.rotation = rotation;
            }

            _trauma = Mathf.Max(0f, _trauma - dt * 1.5f);
            if (_trauma > 0f)
            {
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
