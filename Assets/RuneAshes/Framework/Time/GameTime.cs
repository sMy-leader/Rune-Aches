// ─────────────────────────────────────────────────────────────────────────────
// 【职责】ITime 的静态访问点。业务代码通过 GameTime.Current 读/控时间，
//        不再直接碰 Time.timeScale。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Framework
{
    public static class GameTime
    {
        public static ITime Current
        {
            get { return UnityTime.Instance; }
        }
    }
}
