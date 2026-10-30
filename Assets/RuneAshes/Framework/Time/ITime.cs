// ─────────────────────────────────────────────────────────────────────────────
// 【链路】业务代码（暂停/顿帧/慢动作）──> ITime ──> UnityTime（唯一写 Time.timeScale 的地方）
// 【职责】时间的唯一入口。业务代码禁止直接写 Time.timeScale。
// 【模型】有效倍率 = 暂停(0) ? 0 : min(基础 × 慢动作, 顿帧倍率)
//        - 暂停用引用计数（PushPause/PopPause），支持多个界面同时暂停；
//        - 顿帧是独立层，不会把"暂停前倍率"存成 0.05 再恢复；
//        - 顿帧用 unscaledTime 计时，暂停期间也会自然过期。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Framework
{
    public interface ITime
    {
        /// <summary>受时间缩放影响的帧间隔；暂停时为 0。</summary>
        float DeltaTime { get; }

        /// <summary>不受时间缩放影响的帧间隔；暂停时仍然推进，供 UI 动画使用。</summary>
        float UnscaledDeltaTime { get; }

        /// <summary>最终写进 Time.timeScale 的倍率。</summary>
        float EffectiveScale { get; }

        /// <summary>基础倍率，默认 1；用于全局变速。</summary>
        float BaseScale { get; set; }

        /// <summary>慢动作倍率，默认 1；与基础倍率相乘。</summary>
        float SlowMotionScale { get; set; }

        bool IsPaused { get; }

        int PauseDepth { get; }

        /// <summary>进入一层暂停；与 PopPause 成对使用。</summary>
        void PushPause();

        /// <summary>退出一层暂停；多层暂停时不会提前恢复。</summary>
        void PopPause();

        /// <summary>请求一次顿帧；duration 用真实时间（unscaled）计时。</summary>
        void RequestHitStop(float duration, float scale);

        void ClearHitStop();

        /// <summary>清空暂停/顿帧/变速，回到 1 倍速。</summary>
        void Reset();
    }
}
