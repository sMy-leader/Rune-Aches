// ─────────────────────────────────────────────────────────────────────────────
// 【职责】游戏内 VFX 的稳定入口：代码只认 VfxId，不认具体 prefab 路径。
// 缺失素材的 fallback 集中在这里，避免业务代码到处判断 null。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Gameplay
{
    public enum VfxId
    {
        HitPhysical,
        HitBurn,
        HitFreeze,
        HitExplosion,
        HitShield,
        HitShieldBreak,
        PlayerHit,
        PlayerDeath,
        MuzzleDirect,
        MuzzleSpread,
        MuzzleOrbit,
        MuzzleMine,
        MuzzleDashSlash,
        MuzzleBeam,
        ProjectileDirect,
        ProjectileSpread,
        ProjectileBeam,
        ProjectileOrbit,
        ProjectileMine,
        ProjectileHoming,
        ProjectileEnemyMagenta,
        ProjectileEnemyOrange,
        ExplosionSmall,
        ExplosionM6,
        BomberExplode,
        Shockwave,
        DamageAreaRing,
        DashTrail,
        LevelUpRing,
        LevelUpColumn
    }

    [CreateAssetMenu(menuName = "Rune Ashes/VFX Catalog", fileName = "VfxCatalog")]
    public sealed class VfxCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _hitPhysical;
        [SerializeField] private GameObject _hitBurn;
        [SerializeField] private GameObject _hitFreeze;
        [SerializeField] private GameObject _hitExplosion;
        [SerializeField] private GameObject _hitShield;
        [SerializeField] private GameObject _hitShieldBreak;
        [SerializeField] private GameObject _playerHit;
        [SerializeField] private GameObject _playerDeath;

        [SerializeField] private GameObject _muzzleDirect;
        [SerializeField] private GameObject _muzzleSpread;
        [SerializeField] private GameObject _muzzleOrbit;
        [SerializeField] private GameObject _muzzleMine;
        [SerializeField] private GameObject _muzzleDashSlash;
        [SerializeField] private GameObject _muzzleBeam;

        [SerializeField] private GameObject _projectileDirect;
        [SerializeField] private GameObject _projectileSpread;
        [SerializeField] private GameObject _projectileBeam;
        [SerializeField] private GameObject _projectileOrbit;
        [SerializeField] private GameObject _projectileMine;
        [SerializeField] private GameObject _projectileHoming;
        [SerializeField] private GameObject _projectileEnemyMagenta;
        [SerializeField] private GameObject _projectileEnemyOrange;

        [SerializeField] private GameObject _explosionSmall;
        [SerializeField] private GameObject _explosionM6;
        [SerializeField] private GameObject _bomberExplode;
        [SerializeField] private GameObject _shockwave;
        [SerializeField] private GameObject _damageAreaRing;

        [SerializeField] private GameObject _dashTrail;
        [SerializeField] private GameObject _levelUpRing;
        [SerializeField] private GameObject _levelUpColumn;

        public GameObject Get(VfxId id)
        {
            switch (id)
            {
                case VfxId.HitPhysical: return _hitPhysical;
                case VfxId.HitBurn: return _hitBurn;
                case VfxId.HitFreeze: return _hitFreeze;
                case VfxId.HitExplosion: return _hitExplosion;
                case VfxId.HitShield: return _hitShield;
                case VfxId.HitShieldBreak: return _hitShieldBreak;
                case VfxId.PlayerHit: return _playerHit;
                case VfxId.PlayerDeath: return _playerDeath != null ? _playerDeath : _playerHit;

                case VfxId.MuzzleDirect: return _muzzleDirect;
                // 缺少的发射特效先用 Direct 复用，等正式素材再替换。
                case VfxId.MuzzleSpread: return _muzzleSpread != null ? _muzzleSpread : _muzzleDirect;
                case VfxId.MuzzleOrbit: return _muzzleOrbit != null ? _muzzleOrbit : _muzzleDirect;
                case VfxId.MuzzleMine: return _muzzleMine != null ? _muzzleMine : _muzzleDirect;
                case VfxId.MuzzleDashSlash: return _muzzleDashSlash;
                case VfxId.MuzzleBeam: return _muzzleBeam;

                case VfxId.ProjectileDirect: return _projectileDirect;
                case VfxId.ProjectileSpread: return _projectileSpread != null ? _projectileSpread : _projectileDirect;
                // 缺 Beam/Orbit/Mine 弹道，先用 Direct 复用。
                case VfxId.ProjectileBeam: return _projectileBeam != null ? _projectileBeam : _projectileDirect;
                case VfxId.ProjectileOrbit: return _projectileOrbit != null ? _projectileOrbit : _projectileDirect;
                case VfxId.ProjectileMine: return _projectileMine != null ? _projectileMine : _projectileDirect;
                case VfxId.ProjectileHoming: return _projectileHoming;
                case VfxId.ProjectileEnemyMagenta: return _projectileEnemyMagenta;
                case VfxId.ProjectileEnemyOrange: return _projectileEnemyOrange;

                case VfxId.ExplosionSmall: return _explosionSmall;
                case VfxId.ExplosionM6: return _explosionM6;
                case VfxId.BomberExplode: return _bomberExplode;
                case VfxId.Shockwave: return _shockwave;
                case VfxId.DamageAreaRing: return _damageAreaRing != null ? _damageAreaRing : _shockwave;

                case VfxId.DashTrail: return _dashTrail;
                case VfxId.LevelUpRing: return _levelUpRing;
                case VfxId.LevelUpColumn: return _levelUpColumn;
                default: return null;
            }
        }
    }
}
