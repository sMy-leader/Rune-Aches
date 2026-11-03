// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 BomberController 的双层参数翻译成 EnemyBrain 需要的动画语义。
// Controller 参数：locomotion / gotHit / death / explode / goGround / goAir / fly*。
// Base Layer 负责地面/飞行，Body Layer 是 additive 受击层；两者默认权重都是 0。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public sealed class BomberAnimatorDriver : MonoBehaviour, IEnemyAnimator
    {
        private static readonly int LocomotionId = Animator.StringToHash("locomotion");
        private static readonly int GotHitId = Animator.StringToHash("gotHit");
        private static readonly int DeathId = Animator.StringToHash("death");
        private static readonly int ExplodeId = Animator.StringToHash("explode");

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                // 双层都要显式打开；否则模型完全不动。
                _animator.SetLayerWeight(0, 1f);
                if (_animator.layerCount > 1)
                {
                    _animator.SetLayerWeight(1, 1f);
                }
            }
        }

        public void SetLocomotion(float value01)
        {
            if (_animator != null)
            {
                _animator.SetFloat(LocomotionId, Mathf.Clamp01(value01));
            }
        }

        public void PlayAttack()
        {
            // 自爆行为由 Fuse/Explode 控制，不走普通攻击。
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

        public void PlayExplode()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(ExplodeId);
            }
        }

        public bool IsInAttackState()
        {
            return false;
        }

        public float CurrentNormalizedTime()
        {
            return 0f;
        }

        public float CurrentAttackImpactNormalized()
        {
            return 0.45f;
        }
    }
}
