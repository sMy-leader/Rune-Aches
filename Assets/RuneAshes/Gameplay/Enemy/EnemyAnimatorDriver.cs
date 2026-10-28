// ─────────────────────────────────────────────────────────────────────────────
// 【链路】EnemyBrain（行为）──> EnemyAnimatorDriver（本文件）──> Animator
// 【职责】把 AI 状态翻译成 Troll Controller 的参数/Trigger。
// 【Troll Controller 的约定】Locomotion(Float 0~1)、Attack 1/2/3(Trigger)、
//        Got Hit(Trigger)、Death(Trigger)；不在这里改状态机结构。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyAnimatorDriver : MonoBehaviour
    {
        private static readonly int LocomotionId = Animator.StringToHash("Locomotion");
        private static readonly int GotHitId = Animator.StringToHash("Got Hit");
        private static readonly int DeathId = Animator.StringToHash("Death");

        private static readonly int[] AttackIds =
        {
            Animator.StringToHash("Attack 1"),
            Animator.StringToHash("Attack 2"),
            Animator.StringToHash("Attack 3")
        };

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>0 = 站立，1 = 跑动。Blend Tree 的 Locomotion 参数。</summary>
        public void SetLocomotion(float value01)
        {
            if (_animator != null)
            {
                _animator.SetFloat(LocomotionId, Mathf.Clamp01(value01));
            }
        }

        /// <summary>随机选一个攻击动作。切片期用 UnityEngine.Random；W5 换 IRandom 以支持回放。</summary>
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
                _animator.SetTrigger(GotHitId);
            }
        }

        public void PlayDeath()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(DeathId);
            }
        }
    }
}
