// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 Wraith Controller 的参数翻译成 EnemyBrain 需要的动画语义。
// Controller 参数：Speed / Attack1..3 / Hit / Death；攻击状态：attackL / attackL_1 / attackL_2。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public sealed class WraithAnimatorDriver : MonoBehaviour, IEnemyAnimator
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeathId = Animator.StringToHash("Death");
        private static readonly int[] AttackIds =
        {
            Animator.StringToHash("Attack1"),
            Animator.StringToHash("Attack2"),
            Animator.StringToHash("Attack3")
        };

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                // RemoteMonsterController 的 Base Layer 默认权重为 0。
                _animator.SetLayerWeight(0, 1f);
            }
        }

        public void SetLocomotion(float value01)
        {
            if (_animator != null)
            {
                _animator.SetFloat(SpeedId, Mathf.Clamp01(value01));
            }
        }

        public void PlayAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackIds[Random.Range(0, AttackIds.Length)]);
            }
        }

        public void PlayHit()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(HitId);
            }
        }

        public void PlayDeath()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(DeathId);
            }
        }

        public void PlayExplode()
        {
            // Wraith 没有自爆动画。
        }

        public bool IsInAttackState()
        {
            if (_animator == null)
            {
                return false;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName("attackL") || info.IsName("attackL_1") || info.IsName("attackL_2");
        }

        public float CurrentNormalizedTime()
        {
            return _animator == null
                ? 0f
                : Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
        }

        public float CurrentAttackImpactNormalized()
        {
            return 0.45f;
        }
    }
}
