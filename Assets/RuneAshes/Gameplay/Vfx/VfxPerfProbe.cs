// ─────────────────────────────────────────────────────────────────────────────
// 【职责】VFX 性能探针：记录 Unity 6.3 可用的 Camera.Render / PlayerLoop 耗时
// 和活跃 VFX 数。SetPassCalls/DrawCalls 不是 ProfilerRecorder marker，
// 需要用 Unity Profiler 或 Frame Debugger 做正式对照。
// ─────────────────────────────────────────────────────────────────────────────
using Unity.Profiling;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class VfxPerfProbe : MonoBehaviour
    {
        private ProfilerRecorder _cameraRender;
        private ProfilerRecorder _playerLoop;
        private float _nextLog;

        private void OnEnable()
        {
            _cameraRender = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render");
            _playerLoop = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");
            _nextLog = Time.unscaledTime + 1f;
        }

        private void OnDisable()
        {
            _cameraRender.Dispose();
            _playerLoop.Dispose();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextLog)
            {
                return;
            }

            _nextLog = Time.unscaledTime + 1f;
            Debug.Log("[VFX Perf] CameraRenderMs=" + ToMs(_cameraRender) +
                      " PlayerLoopMs=" + ToMs(_playerLoop) +
                      " Active=" + VfxService.ActiveCount +
                      " Enabled=" + VfxService.Enabled +
                      " LowQuality=" + VfxService.LowQuality);
        }

        private static float ToMs(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue / 1000000f : -1f;
        }
    }
}
