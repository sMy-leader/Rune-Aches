using System.IO;
using System.Text;
using RuneAshes.Data;
using UnityEditor;
using UnityEngine;

namespace RuneAshes.Editor
{
    public static class RuneCsvCodeGen
    {
        private const string CsvPath = "Assets/RuneAshes/Data/Config/rune.csv";
        private const string OutputPath = "Assets/RuneAshes/Data/Config/RuneTable.g.cs";

        [MenuItem("Rune Ashes/Config/Generate Rune Table")]
        public static void Generate()
        {
            var csvFullPath = ToProjectPath(CsvPath);
            if (!File.Exists(csvFullPath))
            {
                Debug.LogError("[Config] CSV not found: " + csvFullPath);
                return;
            }

            var csv = File.ReadAllText(csvFullPath, Encoding.UTF8);
            var runes = RuneCsvParser.Parse(csv);
            var code = RuneCodeEmitter.Emit(runes);

            File.WriteAllText(ToProjectPath(OutputPath), code, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("[Config] Generated " + runes.Count + " runes -> " + OutputPath);
        }

        [MenuItem("Rune Ashes/Config/Validate Config")]
        public static void Validate()
        {
            var csvFullPath = ToProjectPath(CsvPath);
            var provider = new TableProvider();
            provider.Load(provider.Runes != null ? provider.Runes.All : RuneCsvParser.Parse(File.ReadAllText(csvFullPath, Encoding.UTF8)));

            var errors = provider.Validate();
            if (errors.Count == 0)
            {
                Debug.Log("[Config] OK: " + provider.Runes.Count + " runes, no errors.");
                return;
            }

            foreach (var error in errors)
            {
                Debug.LogError("[Config] " + error);
            }
        }

        private static string ToProjectPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
