// ─────────────────────────────────────────────────────────────────────────────
// 【职责】本局统计：存活时间、击杀数。结算面板与 HUD 都读它。
// 【为什么单独一层】ExperienceSystem 只管等级；击杀数/时间属于"本局记录"，
//        将来埋点、每日挑战、分享图都从这里取。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class RunStats : MonoBehaviour
    {
        private UnityRuneWorld _world;
        private float _startTime;
        private int _kills;

        public int Kills
        {
            get { return _kills; }
        }

        public float Elapsed
        {
            get { return Mathf.Max(0f, Time.time - _startTime); }
        }

        private void Awake()
        {
            _world = GetComponent<UnityRuneWorld>();
            _startTime = Time.time;
        }

        private void OnEnable()
        {
            if (_world != null)
            {
                _world.Kill += OnKill;
            }
        }

        private void OnDisable()
        {
            if (_world != null)
            {
                _world.Kill -= OnKill;
            }
        }

        private void OnKill(DamageEvent damage, IDamageable target)
        {
            _kills++;
        }

        public void ResetRun()
        {
            _kills = 0;
            _startTime = Time.time;
        }
    }
}
