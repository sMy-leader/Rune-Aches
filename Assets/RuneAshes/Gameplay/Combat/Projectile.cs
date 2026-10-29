// ─────────────────────────────────────────────────────────────────────────────
// 【链路】Effect（E1/E2）──> IRuneWorld.SpawnProjectile ──> Projectile（本文件）
//        ──> IDamageable.ApplyDamage ──> EnemyDummy
// 【职责】弹丸的运行时行为：移动 → 命中 → 结算伤害 → 消费修饰（穿透/弹射/分裂/追踪/燃烧/爆炸）。
// 【为什么修饰都在这里】修饰不改变"这是不是弹丸"，只改变弹丸的参数与命中后的分支。
//                      所以一个 Projectile 类就能覆盖 6 种修饰，避免为每个组合写子类。
// 【性能】热路径，字段都是预分配的；命中检测用 NonAlloc，循环内不 new。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 直线弹丸。W2 用 OverlapSphere 粗命中，W3 换空间哈希。
    /// 支持穿透、弹射、分裂、追踪、燃烧、爆炸六种修饰。
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private ProjectileSpec _spec;
        private float _age;
        private bool _active;
        private Vector3 _direction;

        /// <summary>弹射会衰减伤害，用倍率累计，而不是反复改写 _spec.Damage（原值要保留）。</summary>
        private float _damageScale = 1f;

        private int _pierceLeft;
        private int _bouncesLeft;

        /// <summary>复用缓冲区：OverlapSphereNonAlloc 最多返回 8 个，超出部分本帧忽略。</summary>
        private readonly Collider[] _hits = new Collider[8];

        /// <summary>本发弹丸已经打过的目标，防止同一发在连续帧里反复打同一个敌人。</summary>
        private readonly List<int> _hitIds = new List<int>(8);

        internal UnityRuneWorld Owner;

        /// <summary>
        /// 从对象池取出后由这里"重置一切"。任何新增的运行时字段都必须在这里复位，
        /// 否则会出现"上一发的穿透次数/命中记录带到下一发"的幽灵 bug。
        /// </summary>
        public void Launch(ProjectileSpec spec)
        {
            _spec = spec;
            _age = 0f;
            _active = true;
            _damageScale = 1f;
            _direction = spec.Direction.sqrMagnitude > 0.0001f ? spec.Direction.normalized : Vector3.forward;
            _pierceLeft = spec.Modifiers.Pierce;
            _bouncesLeft = spec.Modifiers.Bounces;
            _hitIds.Clear();
            transform.position = spec.Origin;
            transform.forward = _direction;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            var dt = Time.deltaTime;
            _age += dt;

            // 顺序：先转向，再按"转向后的方向"前进，最后判定命中与寿命。
            // 反过来会出现"明明追踪到了却从目标旁边飞过去"的一帧延迟。
            if (_spec.Modifiers.TurnRate > 0f)
            {
                Home(dt);
            }

            transform.position += _direction * (_spec.Speed * dt);

            //打到子弹就消亡
            if (TryHit())
            {
                return;
            }
            //达到生命周期，消亡
            if (_age >= _spec.Lifetime)
            {
                Expire();
            }
        }

        /// <summary>
        /// 追踪：每帧最多转 TurnRate 度，而不是直接朝向目标。
        /// 限制转向速度才能让"追踪"有手感差异，也避免弹丸瞬间 180° 掉头。
        /// </summary>
        private void Home(float dt)
        {
            if (Owner == null)
            {
                return;
            }

            Vector3 target;
            if (!Owner.TryGetNearestTarget(transform.position, 15f, out target))
            {
                return;
            }

            var desired = target - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                return;
            }

            var maxRadians = _spec.Modifiers.TurnRate * Mathf.Deg2Rad * dt;
            _direction = Vector3.RotateTowards(_direction, desired.normalized, maxRadians, 0f);
            transform.forward = _direction;
        }

        /// <summary>
        /// 命中检测与修饰消费的入口。返回 true 表示本帧弹丸已被消耗（命中或消亡），
        /// Update 应立即停止，避免同一帧继续移动/重复命中。
        /// </summary>
        private bool TryHit()
        {
            // NonAlloc + 固定 8 格：弹丸每帧都要检测，不能在这里分配数组。
            // 半径内超过 8 个目标时本帧只处理前 8 个，对幸存者类密度足够。
            var count = Physics.OverlapSphereNonAlloc(
                transform.position, _spec.Radius, _hits, ~0, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();//敌人身上脚本继承IDa
                if (damageable == null || !damageable.IsAlive ||
                    (Owner != null && Owner.IsOwner(damageable)))
                {
                    continue;
                }

                // 同一发弹丸对同一目标只结算一次：否则穿透弹在连续帧里会重复打同一人。
                var id = damageable.GetHashCode();
                if (_hitIds.Contains(id))
                {
                    continue;
                }

                _hitIds.Add(id);
                Hit(damageable);

                // 修饰优先级：穿透 > 弹射 > 分裂。
                // 穿透只消耗次数、方向不变；弹射会重新瞄准并消耗本帧；都没有才分裂并回收。
                if (_pierceLeft > 0)
                {
                    _pierceLeft--;
                    continue;
                }

                if (_bouncesLeft > 0 && TryBounce())
                {
                    return false;
                }

                Split();
                Deactivate();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 结算一次命中：直接伤害 + 燃烧 DoT + 爆炸范围伤害。
        /// 三者可以同时存在（例如 M4 + M6），所以这里不是 switch，而是顺序叠加。
        /// </summary>
        private void Hit(IDamageable damageable)
        {
            var damageEvent = new DamageEvent(_spec.Meta, _spec.Damage * _damageScale, DamageType.Direct);
            damageable.ApplyDamage(damageEvent);

            // 命中事件用于 T3 命中传导；DoT 不算命中（语义规范 DotCountsAsHit=false）。
            if (Owner != null)
            {
                Owner.RaiseHit(damageEvent, damageable);
            }

            var debuff = damageable as IDebuffable;
            if (debuff != null && _spec.Modifiers.BurnDps > 0f)
            {
                debuff.ApplyBurn(_spec.Modifiers.BurnDps, _spec.Modifiers.BurnDuration, _spec.Meta);
            }

            // 燃烧与爆炸都用 _spec.Meta：归属仍是"这次施法"，
            // 所以由它们产生的后续击杀/伤害可以被递归闸门正确追踪。
            if (_spec.Modifiers.ExplosionRadius > 0f && Owner != null)
            {
                Owner.DamageArea(new AreaDamageSpec
                {
                    Center = transform.position,
                    Radius = _spec.Modifiers.ExplosionRadius,
                    Damage = _spec.Modifiers.ExplosionDamage,
                    Type = DamageType.Explosion,
                    Meta = _spec.Meta
                });
            }
        }

        /// <summary>
        /// 弹射：朝最近的存活目标重新定向，方向立即生效（所以本帧不再前进）。
        /// [坑] 目前没有排除 _hitIds 里的目标，可能弹回刚打过的敌人；
        ///      W3 接空间哈希时应把"已命中目标"传给选目标的逻辑。
        /// </summary>
        private bool TryBounce()
        {
            if (Owner == null)
            {
                return false;
            }

            Vector3 target;
            if (!Owner.TryGetNearestTarget(transform.position, 15f, out target))
            {
                return false;
            }

            var desired = target - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.01f)
            {
                return false;
            }

            _direction = desired.normalized;
            transform.forward = _direction;
            _bouncesLeft--;
            _damageScale *= Mathf.Clamp01(1f - _spec.Modifiers.BounceFalloff);
            return true;
        }

        /// <summary>
        /// 分裂：按扇形生成 N 枚继承部分伤害的子弹。
        /// [关键] ModifierSet 是 struct，`inherited = _spec.Modifiers` 是值拷贝，
        ///        把 SplitCount 清 0 只影响子弹，不会改到父弹——这就是"分裂不可再分裂"的保证。
        /// </summary>
        private void Split()
        {
            if (_spec.Modifiers.SplitCount <= 0 || Owner == null)
            {
                return;
            }

            var count = _spec.Modifiers.SplitCount;
            var damage = _spec.Damage * _spec.Modifiers.SplitInherit;
            var inherited = _spec.Modifiers;
            inherited.SplitCount = 0; // 分裂不可再分裂

            for (var i = 0; i < count; i++)
            {
                var angle = count <= 1 ? 0f : -30f + 60f * i / (count - 1);
                var direction = Quaternion.Euler(0f, angle, 0f) * _direction;
                Owner.SpawnProjectile(new ProjectileSpec
                {
                    Origin = transform.position,
                    Direction = direction,
                    Speed = _spec.Speed * 0.8f,
                    Damage = damage,
                    Lifetime = _spec.Lifetime * 0.6f,
                    Radius = _spec.Radius,
                    Meta = _spec.Meta,
                    Modifiers = inherited
                });
            }
        }

        /// <summary>
        /// 寿命到期：当前行为是"也会分裂一次"（空放也有收益）。
        /// 如果设计上不希望空放分裂，把这里的 Split() 去掉即可——只改这一行。
        /// </summary>
        private void Expire()
        {
            Split();
            Deactivate();
        }

        /// <summary>
        /// 回池：只关对象，不在这里重置字段。重置统一由 Launch 负责，
        /// 避免"池里重置一半、发射时又重置一半"的两处维护。
        /// </summary>
        private void Deactivate()
        {
            _active = false;
            if (Owner != null)
            {
                Owner.ReleaseProjectile(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
