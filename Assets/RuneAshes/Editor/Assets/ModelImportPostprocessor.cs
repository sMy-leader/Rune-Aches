// ─────────────────────────────────────────────────────────────────────────────
// 【职责】Assets/Art 下所有模型/贴图的"导入闸门"：
//   - 模型：单位 1、不压缩、Read/Write 关、不导入相机/灯光、动画压缩统一；
//   - 贴图：按目录决定 512/1024、法线/遮罩不按 sRGB、UI 开 Alpha；
//   - 导入后：命名、面数、骨骼数超预算时给出警告。
// 【为什么用 AssetPostprocessor 而不是手动 Preset】
//   手动套 Preset 会漏；Postprocessor 对任何人、任何时间导入的资产都生效。
// 【注意】这里只做"设置 + 警告"，不自动改材质。导入后仍需把材质换成
//   RuneAshes/Lit_Stylized，避免资产自带材质破坏统一样式。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RuneAshes.EditorTools
{
    public sealed class ModelImportPostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";
        private const int PlayerBoneBudget = 32;
        private const int GruntBoneBudget = 16;

        // ── 模型 ──────────────────────────────────────────────────────────────
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = assetImporter as ModelImporter;
            if (importer == null)
            {
                return;
            }

            // 1 Unity unit = 1m。useFileScale 让 FBX 自带的单位换算生效；
            // 如果美术导出时已经是米，这一项不会改变尺寸。
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // 文档要求：不做网格压缩、运行时不读网格（省内存）。
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;

            // 游戏资产不需要相机/灯光；带进来会污染场景。
            importer.importCameras = false;
            importer.importLights = false;

            // 杂兵/玩家都用骨骼动画，不依赖 BlendShape；有表情需求时再单独开。
            importer.importBlendShapes = false;

            // 动画统一压缩；误差值留一点余量，避免手绘低模的硬边被压变形。
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = 0.5f;
            importer.animationPositionError = 0.5f;
            importer.animationScaleError = 0.5f;

            // 保留标准材质，先能看见模型；导入后再统一替换为 Lit_Stylized。
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

            // 碰撞体由玩法决定，不在导入时自动加。
            importer.addCollider = false;
        }

        // ── 贴图 ──────────────────────────────────────────────────────────────
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var lower = assetPath.ToLowerInvariant();
            var isNormal = lower.Contains("_normal") || lower.Contains("normalmap");
            var isMask = lower.Contains("_mask") || lower.Contains("_ao") ||
                         lower.Contains("_metallic") || lower.Contains("_roughness");

            importer.sRGBTexture = !isNormal && !isMask;
            importer.isReadable = false;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.npotScale = TextureImporterNPOTScale.ToNearest;
            importer.alphaIsTransparency = lower.Contains("/ui/");
            importer.maxTextureSize = ResolveMaxTextureSize(lower);
        }

        // ── 导入后检查 ────────────────────────────────────────────────────────
        private void OnPostprocessModel(GameObject model)
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(assetPath);

            // Incoming 是"还没分类"的临时区，不校验命名；归位后再检查。
            var isIncoming = assetPath.IndexOf("/Incoming/", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isIncoming && !HasValidPrefix(fileName))
            {
                Debug.LogWarning("[Import] 命名不符合规范（应为 CH_/EN_/ENV_ 前缀）: " + assetPath);
            }

            var triangles = 0;
            var maxBones = 0;

            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                {
                    triangles += filter.sharedMesh.triangles.Length / 3;
                }
            }

            foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned.sharedMesh != null)
                {
                    triangles += skinned.sharedMesh.triangles.Length / 3;
                }

                if (skinned.bones != null)
                {
                    maxBones = Mathf.Max(maxBones, skinned.bones.Length);
                }
            }

            var triangleBudget = ResolveTriangleBudget(assetPath);
            if (triangles > triangleBudget)
            {
                Debug.LogWarning("[Import] 面数超预算: " + assetPath + " = " + triangles +
                                 " tris（预算 " + triangleBudget + "）。考虑减面或只给精英/Boss 用。");
            }

            if (maxBones > PlayerBoneBudget)
            {
                Debug.LogWarning("[Import] 骨骼数超过玩家上限: " + assetPath + " = " + maxBones +
                                 " bones（玩家 ≤32）。杂兵建议 ≤16 或用 VAT。");
            }
            else if (maxBones > GruntBoneBudget && assetPath.IndexOf("/Enemies/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogWarning("[Import] 杂兵骨骼数偏高: " + assetPath + " = " + maxBones +
                                 " bones（建议 ≤16，否则 300 单位时 CPU 蒙皮会成为瓶颈）。");
            }
        }

        private static bool HasValidPrefix(string fileName)
        {
            return fileName.StartsWith("CH_", StringComparison.OrdinalIgnoreCase) ||
                   fileName.StartsWith("EN_", StringComparison.OrdinalIgnoreCase) ||
                   fileName.StartsWith("ENV_", StringComparison.OrdinalIgnoreCase);
        }

        private static int ResolveMaxTextureSize(string lowerPath)
        {
            if (lowerPath.Contains("/ui/runeicons/"))
            {
                return 256;
            }

            if (lowerPath.Contains("/ui/icons/"))
            {
                return 128;
            }

            if (lowerPath.Contains("/characters/") || lowerPath.Contains("/enemies/") ||
                lowerPath.Contains("/environment/"))
            {
                return 1024;
            }

            return 512;
        }

        private static int ResolveTriangleBudget(string path)
        {
            var lower = path.ToLowerInvariant();
            if (lower.Contains("/characters/"))
            {
                return 8000;
            }

            if (lower.Contains("/boss/"))
            {
                return 25000;
            }

            if (lower.Contains("/enemies/"))
            {
                return 2000;
            }

            if (lower.Contains("/environment/"))
            {
                return 1000;
            }

            return 5000;
        }
    }
}
