using UnityEngine;

namespace RuneAshes.Gameplay
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        Vector3 Position { get; }

        void ApplyDamage(DamageEvent damage);
    }
}
