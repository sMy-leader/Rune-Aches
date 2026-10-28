using UnityEngine;

namespace RuneAshes.Framework
{
    /// <summary>
    /// 全局运行时配置。不依赖场景挂载，进入 Play 前生效。
    /// </summary>
    public static class AppBootstrap
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            // 幸存者类必须锁帧：帧率抖动会直接影响手感与弹幕密度。
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
