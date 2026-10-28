// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ITime ──> UnityTime（本文件，Unity 实现）
// 【职责】把 Unity 的 Time 包成可控入口，暂停时统一返回 0。
// 【坑】暂停时 UnscaledDeltaTime 也返回 0：如果 UI 动画/暂停菜单依赖它，会一起停住。
//      将来接暂停菜单时，应让 UI 用真正的 Time.unscaledDeltaTime，或在这里区分"游戏时间/UI 时间"。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Framework
{
    public sealed class UnityTime : ITime
    {
        private bool _isPaused;

        public float DeltaTime
        {
            get { return _isPaused ? 0f : Time.deltaTime; }
        }

        public float UnscaledDeltaTime
        {
            get { return _isPaused ? 0f : Time.unscaledDeltaTime; }
        }

        public float TimeScale
        {
            get { return Time.timeScale; }
            set { Time.timeScale = value <= 0f ? 0f : value; }
        }

        /// <summary>
        /// [坑] 取消暂停时会把 TimeScale 强制写回 1，可能覆盖掉暂停前设置的慢动作倍率。
        /// 现在没有慢动作所以无害；接子弹时间时改成"保存/恢复上一个 TimeScale"。
        /// </summary>
        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                Time.timeScale = value ? 0f : 1f;
            }
        }
    }
}
