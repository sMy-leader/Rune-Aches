// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 Art/VFX 下的正式 prefab 绑定到 VfxCatalog。
// 缺失项按“复用已有 prefab”原则填 fallback，不在这里硬编码业务逻辑。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Gameplay;
using UnityEditor;
using UnityEngine;

namespace RuneAshes.Editor
{
    public static class VfxCatalogBuilder
    {
        private const string CatalogPath = "Assets/RuneAshes/Gameplay/Resources/VfxCatalog.asset";
        private const string Root = "Assets/Art/VFX/Prefabs/";

        [MenuItem("Rune Ashes/VFX/Build VFX Catalog")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/RuneAshes/Gameplay/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/RuneAshes/Gameplay", "Resources");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<VfxCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            Set(so, "_hitPhysical", Root + "Hit/VFX_Hit_Physical_Cyan_01.prefab");
            Set(so, "_hitBurn", Root + "Hit/VFX_Hit_Burn_01.prefab");
            Set(so, "_hitFreeze", Root + "Hit/VFX_Hit_Freeze_01.prefab");
            Set(so, "_hitExplosion", Root + "Hit/VFX_Hit_Explosion_01.prefab");
            Set(so, "_hitShield", Root + "Hit/VFX_Hit_Shield_01.prefab");
            Set(so, "_hitShieldBreak", Root + "Hit/VFX_Hit_ShieldBreak_01.prefab");
            Set(so, "_playerHit", Root + "Hit/VFX_Hit_Player_01.prefab");
            Set(so, "_playerDeath", Root + "Hit/VFX_Hit_Player_01.prefab");

            Set(so, "_muzzleDirect", Root + "Muzzle/VFX_Muzzle_Direct_Cyan_01.prefab");
            // 缺 Spread/Orbit/Mine：用 Direct 复用，等正式素材替换。
            Set(so, "_muzzleSpread", Root + "Muzzle/VFX_Muzzle_Direct_Cyan_01.prefab");
            Set(so, "_muzzleOrbit", Root + "Muzzle/VFX_Muzzle_Direct_Cyan_01.prefab");
            Set(so, "_muzzleMine", Root + "Muzzle/VFX_Muzzle_Direct_Cyan_01.prefab");
            Set(so, "_muzzleDashSlash", Root + "Muzzle/VFX_Muzzle_DashSlash_Cyan_01.prefab");
            Set(so, "_muzzleBeam", Root + "Muzzle/VFX_Muzzle_Beam_Gold_01.prefab");

            Set(so, "_projectileDirect", Root + "Projectile/VFX_Projectile_Direct_Cyan_01.prefab");
            Set(so, "_projectileSpread", Root + "Projectile/VFX_Projectile_Spread_Cyan_01_Burn.prefab");
            // 缺 Beam/Orbit/Mine 弹道：用 Direct 复用。
            Set(so, "_projectileBeam", Root + "Projectile/VFX_Projectile_Direct_Cyan_01.prefab");
            Set(so, "_projectileOrbit", Root + "Projectile/VFX_Projectile_Direct_Cyan_01.prefab");
            Set(so, "_projectileMine", Root + "Projectile/VFX_Projectile_Direct_Cyan_01.prefab");
            Set(so, "_projectileHoming", Root + "Projectile/VFX_Projectile_Homing_Gold_01.prefab");
            Set(so, "_projectileEnemyMagenta", Root + "Projectile/VFX_Projectile_Enemy_Magenta_01.prefab");
            Set(so, "_projectileEnemyOrange", Root + "Projectile/VFX_Projectile_Enemy_Orange_01.prefab");

            Set(so, "_explosionSmall", Root + "Explosion/VFX_Explosion_Small_01.prefab");
            Set(so, "_explosionM6", Root + "Explosion/VFX_Explosion_M6_01.prefab");
            Set(so, "_bomberExplode", Root + "Enemy/VFX_Bomber_Explode_01.prefab");
            Set(so, "_shockwave", Root + "Explosion/VFX_Shockwave_01.prefab");
            // 缺 DamageAreaRing：用冲击波复用。
            Set(so, "_damageAreaRing", Root + "Explosion/VFX_Shockwave_01.prefab");

            Set(so, "_dashTrail", Root + "Player/VFX_Dash_Trail_Cyan_01.prefab");
            Set(so, "_levelUpRing", Root + "LevelUp/VFX_LevelUp_Ring_01.prefab");
            Set(so, "_levelUpColumn", Root + "LevelUp/VFX_LevelUp_Column_01.prefab");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[VfxCatalogBuilder] VFX Catalog updated: " + CatalogPath);
        }

        private static void Set(SerializedObject so, string field, string path)
        {
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError("[VfxCatalogBuilder] Missing field: " + field);
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[VfxCatalogBuilder] Missing prefab: " + path);
            }

            property.objectReferenceValue = prefab;
        }
    }
}
