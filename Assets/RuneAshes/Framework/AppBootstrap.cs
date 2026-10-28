// ─────────────────────────────────────────────────────────────────────────────
// 【链路】游戏启动（早于第一个场景）──> AppBootstrap.Configure()
// 【职责】全局运行时配置。用 RuntimeInitializeOnLoadMethod 而不是场景里的 MonoBehaviour，
//        这样不需要在任何场景挂载，也不会因为切场景而重复执行。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Framework
{
    /// <summary>
    /// 全局运行时配置。不依赖场景挂载，进入 Play 前生效。
    /// </summary>
    public static class AppBootstrap
    {
        public const int TargetFrameRate = 60;

        /// <summary>
        /// [为什么在这里锁帧] 幸存者类的手感与弹幕密度都依赖稳定帧率；
        /// 不锁帧时高刷显示器会跑到 144+，同样的"每秒伤害"看起来会不一样。
        /// vSyncCount=1 同时避免撕裂；两者配合时 targetFrameRate 以显示器刷新率为上限。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            // 幸存者类必须锁帧：帧率抖动会直接影响手感与弹幕密度。
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
