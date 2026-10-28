// ─────────────────────────────────────────────────────────────────────────────
// 【链路】业务代码（波次/顿帧/暂停/生成）──> ITime ──> UnityTime（Unity 实现）
// 【职责】时间的唯一入口。业务代码禁止直接写 Time.timeScale——否则暂停、顿帧、
//        慢动作会互相覆盖，最后一个写入者说了算。
// 【扩展】将来做"子弹时间/受击顿帧"时，在 UnityTime 里叠加倍率，而不是散落改 timeScale。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Framework
{
    /// <summary>
    /// 时间入口。顿帧、减速、暂停、按波次变速都走这里，
    /// 避免业务代码散落 Time.timeScale 赋值。
    /// </summary>
    public interface ITime
    {
        float DeltaTime { get; }

        float UnscaledDeltaTime { get; }

        float TimeScale { get; set; }

        bool IsPaused { get; set; }
    }
}
