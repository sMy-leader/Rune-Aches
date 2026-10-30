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
            if (_animator != null)
            {
                // Troll Demo.controller 的层默认权重是 0；
                // 模板复制出来的实例如果沿用默认值，会出现"AI 在攻击但模型不动"。
                _animator.SetLayerWeight(0, 1f);
            }
        }

        /// <summary>0 = 站立，1 = 跑动。Blend Tree 的 Locomotion 参数。</summary>
        public void SetLocomotion(float value01)
        {
            if (_animator != null)
            {
                _animator.SetFloat(LocomotionId, Mathf.Clamp01(value01));
            }
        }

        /// <summary>
        /// 从第一帧强制播放一个攻击状态。
        /// [为什么不用 SetTrigger] AnyState 的 Trigger 在"当前已经在同一条 Attack 状态"时
        /// 不保证重启动画，AI 会在旧刀播到一半时又结算一次伤害。
        /// </summary>
        public void PlayAttack()
        {
            if (_animator == null)
            {
                return;
            }

            var index = Random.Range(0, AttackIds.Length);
            _animator.Play(AttackIds[index], 0, 0f);
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

        /// <summary>当前是否在 Attack 1/2/3 状态。命中帧判断要先确认还在攻击动画里。</summary>
        public bool IsInAttackState()
        {
            if (_animator == null)
            {
                return false;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName("Attack 1") || info.IsName("Attack 2") || info.IsName("Attack 3");
        }

        /// <summary>当前状态归一化时间（0~1）。不同攻击 clip 长度不同，用比例而不是固定秒数。</summary>
        public float CurrentNormalizedTime()
        {
            if (_animator == null)
            {
                return 0f;
            }

            return Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
        }

        /// <summary>
        /// 当前攻击段的挥击峰值（归一化时间）。
        /// 来自动画曲线里手/武器角速度峰值：Attack1≈0.44、Attack2≈0.55、Attack3≈0.39。
        /// 不要再用一个统一的 0.45 去套三段不同节奏的攻击。
        /// </summary>
        public float CurrentAttackImpactNormalized()
        {
            if (_animator == null)
            {
                return 0.45f;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Attack 1"))
            {
                return 0.44f;
            }

            if (info.IsName("Attack 2"))
            {
                return 0.55f;
            }

            if (info.IsName("Attack 3"))
            {
                return 0.39f;
            }

            return 0.45f;
        }
    }
}
