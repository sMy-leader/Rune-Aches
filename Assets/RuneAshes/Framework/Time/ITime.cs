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
