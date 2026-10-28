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

        public int Dropped { get; private set; }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            while (_elapsed >= _windowSeconds)
            {
                _elapsed -= _windowSeconds;
                _used = 0;
            }
        }

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
