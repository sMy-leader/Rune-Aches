// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ITime ──> UnityTime（本文件，唯一写 Time.timeScale 的地方）
// 【职责】把暂停/顿帧/慢动作叠加成一个有效倍率，并在每帧写回 Unity。
// 【为什么是 MonoBehaviour】顿帧需要在 unscaledTime 到点后自动过期；
//        由 AppBootstrap 在进入场景前创建，DontDestroyOnLoad 跨场景保留。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Framework
{
    public sealed class UnityTime : MonoBehaviour, ITime
    {
        public static UnityTime Instance { get; private set; }

        private int _pauseDepth;
        private float _baseScale = 1f;
        private float _slowMotionScale = 1f;

        private bool _hitStopActive;
        private float _hitStopEnd;
        private float _hitStopScale = 1f;

        public float DeltaTime
        {
            get { return Time.deltaTime; }
        }

        public float UnscaledDeltaTime
        {
            get { return Time.unscaledDeltaTime; }
        }

        public float EffectiveScale
        {
            get
            {
                if (_pauseDepth > 0)
                {
                    return 0f;
                }

                var scale = Mathf.Max(0f, _baseScale) * Mathf.Max(0f, _slowMotionScale);
                if (_hitStopActive)
                {
                    scale = Mathf.Min(scale, _hitStopScale);
                }

                return Mathf.Max(0f, scale);
            }
        }

        public float BaseScale
        {
            get { return _baseScale; }
            set
            {
                _baseScale = Mathf.Max(0f, value);
                Apply();
            }
        }

        public float SlowMotionScale
        {
            get { return _slowMotionScale; }
            set
            {
                _slowMotionScale = Mathf.Max(0f, value);
                Apply();
            }
        }

        public bool IsPaused
        {
            get { return _pauseDepth > 0; }
        }

        public int PauseDepth
        {
            get { return _pauseDepth; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Reset();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_hitStopActive && Time.unscaledTime >= _hitStopEnd)
            {
                _hitStopActive = false;
            }

            // 每帧写回：确保没有其他代码偷偷改 Time.timeScale。
            Apply();
        }

        public void PushPause()
        {
            _pauseDepth++;
            Apply();
        }

        public void PopPause()
        {
            _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
            Apply();
        }

        public void RequestHitStop(float duration, float scale)
        {
            if (duration <= 0f)
            {
                return;
            }

            _hitStopEnd = Time.unscaledTime + duration;
            _hitStopScale = Mathf.Clamp(scale, 0.001f, 1f);
            _hitStopActive = true;
            Apply();
        }

        public void ClearHitStop()
        {
            _hitStopActive = false;
            Apply();
        }

        public void Reset()
        {
            _pauseDepth = 0;
            _baseScale = 1f;
            _slowMotionScale = 1f;
            _hitStopActive = false;
            Apply();
        }

        private void Apply()
        {
            Time.timeScale = EffectiveScale;
        }
    }
}
