// ─────────────────────────────────────────────────────────────────────────────
// 【职责】资产业务验收：扫描 Assets/Art 下的模型，输出面数/顶点/骨骼/动画/材质/
//        命名/预算判定到 Docs/资产盘点报告.md。
// 【用法】菜单 Rune Ashes/Assets/Audit Models；模型先放 Assets/Art/Incoming/。
// 【判定】PASS = 可入库；WARN = 可用但需注意；FAIL = 需减面/换资产/改骨骼。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RuneAshes.EditorTools
{
    public static class AssetAuditMenu
    {
        private const string ArtRoot = "Assets/Art";
        private const string ReportPath = "Docs/资产盘点报告.md";

        [MenuItem("Rune Ashes/Assets/Audit Models")]
        public static void AuditModels()
        {
            var paths = AssetDatabase.FindAssets(string.Empty, new[] { ArtRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsModelPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var rows = paths.Select(Inspect).ToList();
            WriteReport(rows);

            var pass = rows.Count(row => row.Verdict == "PASS");
            var warn = rows.Count(row => row.Verdict == "WARN");
            var fail = rows.Count(row => row.Verdict == "FAIL");

            Debug.Log("[Audit] " + rows.Count + " 个模型：PASS " + pass + " / WARN " + warn +
                      " / FAIL " + fail + " → " + ReportPath);
        }

        private static bool IsModelPath(string path)
        {
            var extension = Path.GetExtension(path);
            return string.Equals(extension, ".fbx", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".obj", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".glb", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".gltf", StringComparison.OrdinalIgnoreCase);
        }

        private static Row Inspect(string path)
        {
            var row = new Row { Path = path, Name = Path.GetFileName(path) };

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                // 同一个 Mesh 被多个 Renderer 引用时只统计一次，避免重复计数。
                var meshes = new HashSet<Mesh>();
                var materials = new HashSet<Material>();

                foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    if (mesh != null && meshes.Add(mesh))
                    {
                        row.Triangles += mesh.triangles.Length / 3;
                        row.Vertices += mesh.vertexCount;
                    }
                }

                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = skinned.sharedMesh;
                    if (mesh != null && meshes.Add(mesh))
                    {
                        row.Triangles += mesh.triangles.Length / 3;
                        row.Vertices += mesh.vertexCount;
                    }

                    if (skinned.bones != null)
                    {
                        row.Bones = Mathf.Max(row.Bones, skinned.bones.Length);
                    }
                }

                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterials == null)
                    {
                        continue;
                    }

                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }

                row.Materials = materials.Count;
            }

            row.Clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Count(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));

            row.Naming = HasValidPrefix(row.Name) ? "OK" : "WARN";
            row.Verdict = "PASS";
            row.Notes = BuildNotes(path, row);
            return row;
        }

        private static string BuildNotes(string path, Row row)
        {
            var notes = new List<string>();
            var verdict = "PASS";
            var isIncoming = path.IndexOf("/Incoming/", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!HasValidPrefix(row.Name) && !isIncoming)
            {
                notes.Add("命名不符合 CH_/EN_/ENV_ 规范");
                verdict = "WARN";
            }

            var budget = ResolveTriangleBudget(path);
            if (budget > 0 && row.Triangles > budget)
            {
                notes.Add("面数 " + row.Triangles + " > 预算 " + budget);
                verdict = row.Triangles > budget * 1.5 ? "FAIL" : "WARN";
            }

            if (row.Bones > 32)
            {
                notes.Add("骨骼 " + row.Bones + " > 32");
                verdict = "FAIL";
            }
            else if (row.Bones > 16 && IsEnemy(path))
            {
                notes.Add("杂兵骨骼 " + row.Bones + " > 16，建议 ≤16 或 VAT");
                if (verdict == "PASS")
                {
                    verdict = "WARN";
                }
            }

            if ((IsCharacter(path) || IsEnemy(path)) && row.Clips == 0)
            {
                notes.Add("没有动画 clip");
                if (verdict == "PASS")
                {
                    verdict = "WARN";
                }
            }

            row.Verdict = verdict;
            return notes.Count == 0 ? "—" : string.Join("；", notes.ToArray());
        }

        private static bool HasValidPrefix(string fileName)
        {
            return fileName.StartsWith("CH_", StringComparison.OrdinalIgnoreCase) ||
                   fileName.StartsWith("EN_", StringComparison.OrdinalIgnoreCase) ||
                   fileName.StartsWith("ENV_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCharacter(string path)
        {
            return path.IndexOf("/Characters/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsEnemy(string path)
        {
            return path.IndexOf("/Enemies/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int ResolveTriangleBudget(string path)
        {
            if (path.IndexOf("/Incoming/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 0;
            }

            if (path.IndexOf("/Boss/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 25000;
            }

            if (IsCharacter(path))
            {
                return 8000;
            }

            if (IsEnemy(path))
            {
                return 2000;
            }

            if (path.IndexOf("/Environment/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1000;
            }

            return 5000;
        }

        private static void WriteReport(List<Row> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# 资产盘点报告");
            builder.AppendLine();
            builder.AppendLine("> 生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            builder.AppendLine("> 扫描目录：`Assets/Art`");
            builder.AppendLine("> 判定：PASS = 可入库；WARN = 可用但需注意；FAIL = 需减面/换资产/改骨骼");
            builder.AppendLine();

            var pass = rows.Count(row => row.Verdict == "PASS");
            var warn = rows.Count(row => row.Verdict == "WARN");
            var fail = rows.Count(row => row.Verdict == "FAIL");

            builder.AppendLine("## 汇总");
            builder.AppendLine();
            builder.AppendLine("| 总数 | PASS | WARN | FAIL |");
            builder.AppendLine("|---:|---:|---:|---:|");
            builder.AppendLine("| " + rows.Count + " | " + pass + " | " + warn + " | " + fail + " |");
            builder.AppendLine();

            builder.AppendLine("## 明细");
            builder.AppendLine();
            if (rows.Count == 0)
            {
                builder.AppendLine("（`Assets/Art` 下没有模型。把下载的模型先放进 `Assets/Art/Incoming/`，再运行本菜单。）");
            }
            else
            {
                builder.AppendLine("| 文件 | 面数 | 顶点 | 骨骼 | 动画 | 材质 | 命名 | 结论 | 备注 |");
                builder.AppendLine("|---|---:|---:|---:|---:|---:|---|---|---|");
                foreach (var row in rows)
                {
                    builder.AppendLine("| `" + row.Path + "` | " + row.Triangles + " | " + row.Vertices +
                                       " | " + row.Bones + " | " + row.Clips + " | " + row.Materials +
                                       " | " + row.Naming + " | **" + row.Verdict + "** | " + row.Notes + " |");
                }
            }

            builder.AppendLine();
            builder.AppendLine("## 下一步");
            builder.AppendLine();
            builder.AppendLine("- **FAIL**：先减面/降骨骼/换资产，不要直接进场景。");
            builder.AppendLine("- **WARN**：可用，但记录到 `Docs/资产台账.md`，W5 性能冲刺时优先处理。");
            builder.AppendLine("- **PASS**：按 `Assets/Art/README.md` 的命名规范归位，并把材质换成 `RuneAshes/Lit_Stylized`。");

            var fullPath = Path.GetFullPath(ReportPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private sealed class Row
        {
            public string Path;
            public string Name;
            public int Triangles;
            public int Vertices;
            public int Bones;
            public int Clips;
            public int Materials;
            public string Naming;
            public string Verdict;
            public string Notes;
        }
    }
}
