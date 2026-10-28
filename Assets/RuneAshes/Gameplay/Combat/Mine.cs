// ─────────────────────────────────────────────────────────────────────────────
// 【链路】E4 MineEffect ──> IRuneWorld.SpawnMine ──> Mine（本文件）
//        ──> UnityRuneWorld.DamageArea ──> IDamageable
// 【职责】布在脚下的陷阱：短暂引信 → 待触发 → 敌人进入半径或寿命到 → 爆炸一次。
// 【为什么地雷不直接用碰撞体】它需要"引信时间"和"单次结算"，
//                            用物理 Trigger 还得处理重复进入；自己查半径更可控。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 地雷：布下后短暂引信，随后进入待触发状态；
    /// 敌人进入半径或寿命结束即引爆（一次只结算一次）。
    /// </summary>
    public sealed class Mine : MonoBehaviour
    {
        private MineSpec _spec;
        private UnityRuneWorld _world;
        private float _age;
        private bool _armed;
        private float _radius;
        private readonly Collider[] _hits = new Collider[16];

        public void Init(MineSpec spec, UnityRuneWorld world)
        {
            _spec = spec;
            _world = world;
            _age = 0f;
            _armed = false;
            _radius = spec.Radius;
            transform.position = spec.Position;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _age += dt;

            if (!_armed)
            {
                if (_age >= _spec.ArmDelay)
                {
                    _armed = true;
                }

                return;
            }

            if (_age >= _spec.Lifetime)
            {
                Explode();
                return;
            }

            var count = Physics.OverlapSphereNonAlloc(
                transform.position, _radius, _hits, ~0, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Explode();
                    return;
                }
            }
        }

        private void Explode()
        {
            if (_world != null)
            {
                _world.DamageArea(new AreaDamageSpec
                {
                    Center = transform.position,
                    Radius = _radius,
                    Damage = _spec.Damage,
                    Type = DamageType.Explosion,
                    Meta = _spec.Meta
                });
            }

            Destroy(gameObject);
        }
    }
}
