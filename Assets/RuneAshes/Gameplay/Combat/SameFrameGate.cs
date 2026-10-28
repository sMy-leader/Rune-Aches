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

        /// <summary>返回 true 表示本次 Proc 应被合并（不重复施法）。</summary>
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
