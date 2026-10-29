// ─────────────────────────────────────────────────────────────────────────────
// 【链路】UnityRuneWorld.Kill ──> ExperienceSystem（本文件）──> LeveledUp
//        ──> UpgradeScreen（UI）──> RunProgression.ApplyChoice
// 【职责】本局经验、等级与升级事件。它不决定升级给什么，只回答"升到几级了"。
// 【曲线】预算驱动：15 分钟大约 20 级；需求 = base + perLevel × level，
//        先按可调参数落地，W5 用真实击杀数再校准。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public sealed class ExperienceSystem : MonoBehaviour
    {
        [SerializeField] private int _level = 1;
        [SerializeField] private int _currentXp;
        [SerializeField] private float _baseXp = 10f;
        [SerializeField] private float _xpPerLevel = 4f;

        private UnityRuneWorld _world;

        public int Level { get; private set; }
        public int CurrentXp { get; private set; }
        public int XpToNext { get; private set; }

        /// <summary>升到新等级时触发，参数是新等级。可能一次击杀触发多级。</summary>
        public event Action<int> LeveledUp;

        public event Action Changed;

        public float Progress01
        {
            get { return XpToNext <= 0 ? 0f : Mathf.Clamp01((float)CurrentXp / XpToNext); }
        }

        private void Awake()
        {
            Level = Mathf.Max(1, _level);
            CurrentXp = Mathf.Max(0, _currentXp);
            XpToNext = CalculateRequirement(Level);
            _world = GetComponent<UnityRuneWorld>();
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
            var actor = target as EnemyActor;
            var reward = actor != null ? actor.XpReward : 10;
            AddXp(reward);
        }

        /// <summary>加经验；一次加够多级时会连续触发 LeveledUp。</summary>
        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentXp += amount;

            while (CurrentXp >= XpToNext)
            {
                CurrentXp -= XpToNext;
                Level++;
                XpToNext = CalculateRequirement(Level);

                if (LeveledUp != null)
                {
                    LeveledUp(Level);
                }
            }

            if (Changed != null)
            {
                Changed();
            }
        }

        /// <summary>从 1 级升到 `level` 所需经验。W5 用真实击杀预算校准。</summary>
        public int CalculateRequirement(int level)
        {
            return Mathf.Max(1, Mathf.RoundToInt(_baseXp + _xpPerLevel * level));
        }

        public void ResetRun()
        {
            Level = 1;
            CurrentXp = 0;
            XpToNext = CalculateRequirement(Level);
            if (Changed != null)
            {
                Changed();
            }
        }
    }
}
