// ─────────────────────────────────────────────────────────────────────────────
// 【链路】（W3 事件总线）击杀/命中事件 ──> SameFrameGate.ShouldMerge() ──> 决定是否 Cast
// 【职责】同一帧、同一条链的多次触发合并成一次施法。
// 【为什么需要】"散射 5 发同帧击杀 5 个敌人"会让击杀触发连开 5 次，
//              再叠上爆炸就是帧内雪崩。合并后一帧只认一次。
// 【现状】W2 还没有事件总线，所以这个类暂时没被 RuneCaster 使用；
//        逻辑已经单测过，W3 直接接线即可。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 同帧合并：同一帧、同一条链的多次 Proc 合并为一次 Cast。
    /// 防止"散射 5 发同时暴击 → 5 次击杀触发 → 5 次爆炸"这种帧内爆炸。
    /// </summary>
    public sealed class SameFrameGate
    {
        private int _frame = int.MinValue;
        private int _chainId = int.MinValue;

        /// <summary>
        /// 返回 true 表示本次 Proc 应被合并（不重复施法）。
        /// [坑] 只记住"最后一次"的 chainId/frame，所以多链同时触发时会互相覆盖。
        ///      W3 多链并行前要改成按 chainId 各存一个 frame（字典或 small array）。
        /// </summary>
        public bool ShouldMerge(int chainId, int frame)
        {
            var merge = frame == _frame && chainId == _chainId;
            if (!merge)
            {
                _frame = frame;
                _chainId = chainId;
            }

            return merge;
        }
    }
}
