using UnityEngine;

namespace RuneAshes.Framework
{
    public sealed class UnityTime : ITime
    {
        private bool _isPaused;

        public float DeltaTime
        {
            get { return _isPaused ? 0f : Time.deltaTime; }
        }

        public float UnscaledDeltaTime
        {
            get { return _isPaused ? 0f : Time.unscaledDeltaTime; }
        }

        public float TimeScale
        {
            get { return Time.timeScale; }
            set { Time.timeScale = value <= 0f ? 0f : value; }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                _isPaused = value;
                Time.timeScale = value ? 0f : 1f;
            }
        }
    }
}
