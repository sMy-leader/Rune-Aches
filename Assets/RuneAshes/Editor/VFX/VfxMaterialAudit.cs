// ─────────────────────────────────────────────────────────────────────────────
// 【职责】VFX 材质审计与“安全去重”。
// 只合并签名完全一致的材质：同 Shader、同属性、同贴图、同关键字、同 RenderQueue。
// 不删除原材质资产，不做 Atlas，不改视觉参数。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuneAshes.Editor
{
    public static class VfxMaterialAudit
    {
        private const string VfxRoot = "Assets/Art/VFX";
        private const string PrefabRoot = "Assets/Art/VFX/Prefabs";
        private const string ReportPath = "Docs/VFX材质审计报告.md";

        [MenuItem("Rune Ashes/VFX/Audit Materials")]
        public static void Audit()
        {
            var groups = BuildGroups();
            var sb = new StringBuilder();
            sb.AppendLine("# VFX 材质审计报告");
            sb.AppendLine();
            sb.AppendLine("> 只比较完全一致的材质签名；不修改任何材质或 prefab。");
            sb.AppendLine();
            sb.AppendLine("## 总览");
            sb.AppendLine();
            sb.AppendLine("```text");
            sb.AppendLine("materials=" + CountMaterials(groups));
            sb.AppendLine("uniqueSignatures=" + groups.Count);
            sb.AppendLine("duplicateMaterials=" + (CountMaterials(groups) - groups.Count));
            sb.AppendLine("```");
            sb.AppendLine();

            var index = 0;
            foreach (var group in groups)
            {
                if (group.Value.Count <= 1)
                {
                    continue;
                }

                index++;
                sb.AppendLine("## 重复组 " + index + "（" + group.Value.Count + " 个）");
                sb.AppendLine();
                sb.AppendLine("```text");
                sb.AppendLine("signature=" + Shorten(group.Key));
                foreach (var path in group.Value)
                {
                    sb.AppendLine(path);
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }

            if (index == 0)
            {
                sb.AppendLine("没有发现完全一致的重复材质。");
            }

            System.IO.File.WriteAllText(ReportPath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[VfxMaterialAudit] report=" + ReportPath + " groups=" + groups.Count);
        }

        [MenuItem("Rune Ashes/VFX/Deduplicate Materials (Safe)")]
        public static void Deduplicate()
        {
            var groups = BuildGroups();
            var duplicateToCanonical = new Dictionary<Material, Material>();
            var removed = 0;

            foreach (var group in groups)
            {
                if (group.Value.Count <= 1)
                {
                    continue;
                }

                var canonicalPath = group.Value[0];
                var canonical = AssetDatabase.LoadAssetAtPath<Material>(canonicalPath);
                if (canonical == null)
                {
                    continue;
                }

                for (var i = 1; i < group.Value.Count; i++)
                {
                    var duplicate = AssetDatabase.LoadAssetAtPath<Material>(group.Value[i]);
                    if (duplicate != null && duplicate != canonical)
                    {
                        duplicateToCanonical[duplicate] = canonical;
                        removed++;
                    }
                }
            }

            if (removed == 0)
            {
                Debug.Log("[VfxMaterialAudit] no exact duplicate materials to replace.");
                return;
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            var changedPrefabs = 0;
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    continue;
                }

                var changed = false;
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        Material canonical;
                        if (materials[i] != null && duplicateToCanonical.TryGetValue(materials[i], out canonical))
                        {
                            materials[i] = canonical;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = materials;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedPrefabs++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[VfxMaterialAudit] exact duplicates replaced=" + removed +
                      " prefabsChanged=" + changedPrefabs + " (original materials kept)");
        }

        private static Dictionary<string, List<string>> BuildGroups()
        {
            var groups = new Dictionary<string, List<string>>();
            var guids = AssetDatabase.FindAssets("t:Material", new[] { VfxRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                var signature = BuildSignature(material);
                List<string> list;
                if (!groups.TryGetValue(signature, out list))
                {
                    list = new List<string>();
                    groups.Add(signature, list);
                }

                list.Add(path);
            }

            return groups;
        }

        private static int CountMaterials(Dictionary<string, List<string>> groups)
        {
            var count = 0;
            foreach (var group in groups)
            {
                count += group.Value.Count;
            }

            return count;
        }

        private static string BuildSignature(Material material)
        {
            var sb = new StringBuilder(512);
            var shader = material.shader;
            sb.Append(shader == null ? "shader:null" : "shader:" + shader.name);
            sb.Append("|queue:").Append(material.renderQueue);
            sb.Append("|instancing:").Append(material.enableInstancing);

            var keywords = material.shaderKeywords;
            Array.Sort(keywords, StringComparer.Ordinal);
            sb.Append("|keywords:").Append(string.Join(",", keywords));

            if (shader == null)
            {
                return sb.ToString();
            }

            var count = shader.GetPropertyCount();
            var entries = new List<KeyValuePair<string, int>>(count);
            for (var i = 0; i < count; i++)
            {
                entries.Add(new KeyValuePair<string, int>(shader.GetPropertyName(i), i));
            }

            entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
            foreach (var entry in entries)
            {
                var name = entry.Key;
                sb.Append("|").Append(name).Append(":");
                var type = shader.GetPropertyType(entry.Value);
                switch (type)
                {
                    case ShaderPropertyType.Color:
                        sb.Append(material.GetColor(name).ToString("F6"));
                        break;
                    case ShaderPropertyType.Vector:
                        sb.Append(material.GetVector(name).ToString("F6"));
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        sb.Append(material.GetFloat(name).ToString("F6"));
                        break;
                    case ShaderPropertyType.Int:
                        sb.Append(material.GetInt(name));
                        break;
                    case ShaderPropertyType.Texture:
                        var texture = material.GetTexture(name);
                        sb.Append(texture == null ? "null" : texture.GetInstanceID().ToString());
                        var scale = material.GetTextureScale(name);
                        var offset = material.GetTextureOffset(name);
                        sb.Append(":s").Append(scale.ToString("F6"));
                        sb.Append(":o").Append(offset.ToString("F6"));
                        break;
                    default:
                        sb.Append("unknown");
                        break;
                }
            }

            return sb.ToString();
        }

        private static string Shorten(string value)
        {
            return value.Length <= 220 ? value : value.Substring(0, 220) + "...";
        }
    }
}
