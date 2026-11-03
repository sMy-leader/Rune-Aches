// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把导入的 Wraith / Bomber 模型 prefab 变成可被 WaveSpawner 生成的正式敌人。
// 只在 Editor 运行；重复执行会复用已有组件，不会重复添加。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RuneAshes.Editor
{
    public static class EnemyPrefabBuilder
    {
        private const string RangedPath =
            "Assets/Art/Enemies/Ranged/Prefabs/Enemy_Ranged.prefab";
        private const string BomberPath =
            "Assets/Art/Enemies/Bomber/Characters/Bomber Bug/_Prefabs/Enemy_Bomber.prefab";

        [MenuItem("Rune Ashes/Enemies/Build Enemy Prefabs")]
        public static void Build()
        {
            Configure(RangedPath, EnemyKind.Ranged, 2.0f, 0.7f, 120, 10);
            Configure(BomberPath, EnemyKind.Bomber, 1.2f, 0.55f, 80, 10);
            AssignSpawnerTemplates();
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyPrefabBuilder] Wraith/Bomber prefabs and WaveSpawner templates updated.");
        }

        private static void Configure(string path, EnemyKind kind, float height, float radius,
            float maxHealth, int xpReward)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError("[EnemyPrefabBuilder] Missing prefab: " + path);
                return;
            }

            var controller = root.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = root.AddComponent<CharacterController>();
            }

            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            controller.stepOffset = 0.3f;

            var actor = root.GetComponent<EnemyActor>();
            if (actor == null)
            {
                actor = root.AddComponent<EnemyActor>();
            }

            var actorSo = new SerializedObject(actor);
            actorSo.FindProperty("_maxHealth").floatValue = maxHealth;
            actorSo.FindProperty("_xpReward").intValue = xpReward;
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            var brain = root.GetComponent<EnemyBrain>();
            if (brain == null)
            {
                brain = root.AddComponent<EnemyBrain>();
            }

            var brainSo = new SerializedObject(brain);
            brainSo.FindProperty("_kind").enumValueIndex = (int)kind;
            brainSo.ApplyModifiedPropertiesWithoutUndo();

            if (kind == EnemyKind.Ranged && root.GetComponent<WraithAnimatorDriver>() == null)
            {
                root.AddComponent<WraithAnimatorDriver>();
            }

            if (kind == EnemyKind.Bomber && root.GetComponent<BomberAnimatorDriver>() == null)
            {
                root.AddComponent<BomberAnimatorDriver>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignSpawnerTemplates()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[EnemyPrefabBuilder] Player not found; open Arena_Blockout first.");
                return;
            }

            var spawner = player.GetComponent<WaveSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[EnemyPrefabBuilder] WaveSpawner not found on Player.");
                return;
            }

            var so = new SerializedObject(spawner);
            so.FindProperty("_rangedTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(RangedPath);
            so.FindProperty("_bomberTemplate").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(BomberPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
