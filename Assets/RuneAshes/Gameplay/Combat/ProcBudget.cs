// ─────────────────────────────────────────────────────────────────────────────
// 【链路】RuneCaster.Cast() ──> ProcBudget.TryConsume()（本文件）
// 【职责】全局"触发预算"：每 0.1 秒最多放行 64 次施法，超出丢弃并计数。
// 【为什么需要它】深度闸门只能管住"一条链递归多深"，管不住
//                "200 个敌人同时被点燃 → 每秒几百次 Proc"。
//                这是第三道闸门，保护的是帧率，不是玩法平衡。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 全局触发预算：每 windowSeconds 最多放行 capacity 次 Proc。
    /// 超出丢弃并计数，用于观测"这条链是否会爆表"，也保护帧率。
    /// </summary>
    public sealed class ProcBudget
    {
        private readonly int _capacity;
        private readonly float _windowSeconds;
        private float _elapsed;
        private int _used;

        public ProcBudget(int capacity, float windowSeconds)
        {
            _capacity = Mathf.Max(1, capacity);
            _windowSeconds = Mathf.Max(0.01f, windowSeconds);
        }

        /// <summary>被丢弃的 Proc 总数。平衡调试时看它是否持续增长，就知道链是不是过热。</summary>
        public int Dropped { get; private set; }

        /// <summary>
        /// 推进窗口。这是"固定窗口"而不是"滑动窗口"：每满 0.1 秒整体清零。
        /// 好处是 O(1)、无分配；代价是窗口边界附近最多可能放行 2 倍容量（跨窗口）。
        /// 对"保护帧率"这个目标足够，不要拿它做精确的 DPS 计量。
        /// </summary>
        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            while (_elapsed >= _windowSeconds)
            {
                _elapsed -= _windowSeconds;
                _used = 0;
            }
        }

        /// <summary>尝试占用一次预算。返回 false 表示本帧这次施法应被丢弃。</summary>
        public bool TryConsume()
        {
            if (_used >= _capacity)
            {
                Dropped++;
                return false;
            }

            _used++;
            return true;
        }
    }
}
