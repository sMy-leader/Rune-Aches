// ─────────────────────────────────────────────────────────────────────────────
// 【链路】E3 OrbitingOrbEffect ──> IRuneWorld.SpawnOrbitingOrbs ──> OrbitingOrbs（本文件）
//        ──> IDamageable.ApplyDamage
// 【职责】围绕玩家旋转的持续伤害球。挂在玩家身下，自己计算每颗球的位置并做命中检测。
// 【为什么独立成类】它不像弹丸那样"发射后脱离"，而是持续存在、跟随、周期性判定，
//                  生命周期与 Update 循环都和弹丸不同。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 环绕球：挂在玩家身下，围绕旋转并周期性检测命中。
    /// 同一敌人 0.5s 内不重复受伤（与语义规范一致）。
    /// </summary>
    public sealed class OrbitingOrbs : MonoBehaviour
    {
        /// <summary>同一目标的重复命中冷却。没有它，一颗球每帧都会结算，DPS 会失控。</summary>
        private const float HitCooldown = 0.5f;

        private OrbitSpec _spec;
        private float _age;
        private float _angle;
        private readonly Collider[] _hits = new Collider[16];
        private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();

        /// <summary>
        /// 初始化并挂到施法者身下。用 localPosition=0 让它跟随玩家移动，
        /// 所以球员永远围着玩家转，而不是围着施法瞬间的位置转。
        /// </summary>
        public void Init(OrbitSpec spec, Transform follow)
        {
            _spec = spec;
            _age = 0f;
            _angle = 0f;
            if (follow != null)
            {
                transform.SetParent(follow, false);
                transform.localPosition = Vector3.zero;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _age += dt;
            _angle += _spec.RotationSpeed * dt;

            if (_age >= _spec.Duration)
            {
                Destroy(gameObject);
                return;
            }

            var count = Mathf.Max(1, _spec.Count);
            for (var i = 0; i < count; i++)
            {
                var offset = Quaternion.Euler(0f, _angle + i * (360f / count), 0f) * Vector3.forward;
                var point = transform.position + offset * _spec.Radius;
                HitAt(point);
            }
        }

        /// <summary>
        /// 单颗球的命中检测。每帧每颗球各查一次 OverlapSphere，
        /// 球数 × 敌人密度上来后成本明显；W3 接空间哈希后改成一次查询处理所有球。
        /// [坑] _lastHitTime 按目标 HashCode 记录，目标死亡后不会清理。
        ///      球只活 6 秒，条目有限所以现在无害；若将来环绕球常驻，需要定期剔除。
        /// </summary>
        private void HitAt(Vector3 point)
        {
            var count = Physics.OverlapSphereNonAlloc(point, 0.55f, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                var id = damageable.GetHashCode();
                float last;
                if (_lastHitTime.TryGetValue(id, out last) && Time.time - last < HitCooldown)
                {
                    continue;
                }

                _lastHitTime[id] = Time.time;
                damageable.ApplyDamage(new DamageEvent(_spec.Meta, _spec.Damage, DamageType.Direct));
            }
        }
    }
}
