// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 OTF/TTF 转成 TMP Font Asset（SDF 图集）。
//        菜单：Rune Ashes/UI/Build TMP Font Assets
// 【说明】中文用 Dynamic + Multi Atlas，避免把整个 CJK 字符集烘成一张巨图。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RuneAshes.EditorTools
{
    public static class TmpFontAssetBuilder
    {
        private const string OutDir = "Assets/RuneAshes/UI/Resources/Fonts";
        private const string LiberationPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [MenuItem("Rune Ashes/UI/Build TMP Font Assets")]
        public static void Build()
        {
            EnsureFolder(OutDir);
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
            var fallbackList = fallback == null ? null : new List<TMP_FontAsset> { fallback };

            Create("SourceHanSansSC", "Assets/Art/UI/Fonts/SiYuanHeiTi-Regular/SourceHanSansSC-Regular-2.otf",
                2048, fallbackList);
            Create("Cinzel", "Assets/Art/UI/Fonts/Cinzel/Cinzel-Regular-3.otf",
                1024, fallbackList);
            Create("JetBrainsMono", "Assets/Art/UI/Fonts/JetBrains-Mono/JetBrainsMono-Regular-2.ttf",
                1024, fallbackList);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TmpFontAssetBuilder] TMP 字体资产已生成：" + OutDir);
        }

        private static void Create(string name, string sourcePath, int atlasSize,
            List<TMP_FontAsset> fallback)
        {
            var outputPath = OutDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (existing != null)
            {
                Repair(existing, name, atlasSize);
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogWarning("[TmpFontAssetBuilder] 找不到字体：" + sourcePath);
                return;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 5, GlyphRenderMode.SDFAA,
                atlasSize, atlasSize, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogWarning("[TmpFontAssetBuilder] 创建失败：" + sourcePath);
                return;
            }

            asset.name = name;
            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            asset.isMultiAtlasTexturesEnabled = true;
            if (fallback != null)
            {
                asset.fallbackFontAssetTable = fallback;
            }

            AssetDatabase.CreateAsset(asset, outputPath);
            Repair(asset, name, atlasSize);
            Debug.Log("[TmpFontAssetBuilder] 生成：" + outputPath);
        }

        /// <summary>
        /// Unity 6.3 的 TMP_FontAsset.CreateFontAsset 不会自动把 atlas/material 写成子资产；
        /// 必须手动 AddObjectToAsset，否则运行时会报 "Font Atlas Texture missing"。
        /// </summary>
        private static void Repair(TMP_FontAsset asset, string name, int atlasSize)
        {
            if (asset == null)
            {
                return;
            }

            var atlas = asset.atlasTexture;
            if (atlas == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(atlas)))
            {
                if (atlas == null)
                {
                    atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.Alpha8, false);
                }
                else if (atlas.width <= 1 || atlas.height <= 1)
                {
                    atlas.Reinitialize(atlasSize, atlasSize, TextureFormat.Alpha8, false);
                }

                atlas.name = name + " Atlas";
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(atlas)))
                {
                    AssetDatabase.AddObjectToAsset(atlas, asset);
                }

                asset.atlasTextures = new Texture2D[1] { atlas };
            }

            var material = asset.material;
            if (material == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
            {
                if (material == null)
                {
                    var shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                    if (shader == null)
                    {
                        shader = Shader.Find("TextMeshPro/Distance Field");
                    }

                    material = new Material(shader);
                }

                material.name = name + " Material";
                material.SetTexture(ShaderUtilities.ID_MainTex, asset.atlasTexture);
                material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasSize);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasSize);
                material.SetFloat(ShaderUtilities.ID_GradientScale, 6f);
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                {
                    AssetDatabase.AddObjectToAsset(material, asset);
                }

                asset.material = material;
            }

            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            asset.isMultiAtlasTexturesEnabled = true;
            asset.ReadFontAssetDefinition();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[TmpFontAssetBuilder] 修复 atlas/material：" + AssetDatabase.GetAssetPath(asset));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
