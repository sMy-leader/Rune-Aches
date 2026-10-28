using System.Collections.Generic;
using System.IO;
using RuneAshes.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneAshes.Editor
{
    /// <summary>
    /// 程序化生成灰盒竞技场，保证场景可复现、可版本化。
    /// 菜单：Rune Ashes/Scenes/Build Blockout Arena
    /// </summary>
    public static class ArenaBlockoutBuilder
    {
        private const string ScenePath = "Assets/Scenes/Arena_Blockout.unity";
        private const string MaterialDir = "Assets/RuneAshes/Art/Blockout";
        private const string InputPath = "Assets/RuneAshes/Gameplay/Input/RuneAshes.inputactions";
        private const float ArenaHalfSize = 20f;

        [MenuItem("Rune Ashes/Scenes/Build Blockout Arena")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialDir);
            AssetDatabase.Refresh();

            var groundMaterial = CreateMaterial("MAT_Blockout_Ground", new Color(0.22f, 0.25f, 0.21f));
            var wallMaterial = CreateMaterial("MAT_Blockout_Wall", new Color(0.14f, 0.14f, 0.15f));
            var obstacleMaterial = CreateMaterial("MAT_Blockout_Obstacle", new Color(0.30f, 0.26f, 0.22f));
            var playerMaterial = CreateMaterial("MAT_Blockout_Player", new Color(0.75f, 0.85f, 0.80f));
            var aimMaterial = CreateMaterial("MAT_Blockout_Aim", new Color(0.20f, 0.90f, 0.85f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateBox("Ground", new Vector3(0f, -0.5f, 0f), new Vector3(ArenaHalfSize * 2f, 1f, ArenaHalfSize * 2f), groundMaterial, true);
            CreateBox("Wall_North", new Vector3(0f, 2f, ArenaHalfSize + 0.5f), new Vector3(ArenaHalfSize * 2f + 1f, 4f, 1f), wallMaterial, true);
            CreateBox("Wall_South", new Vector3(0f, 2f, -ArenaHalfSize - 0.5f), new Vector3(ArenaHalfSize * 2f + 1f, 4f, 1f), wallMaterial, true);
            CreateBox("Wall_East", new Vector3(ArenaHalfSize + 0.5f, 2f, 0f), new Vector3(1f, 4f, ArenaHalfSize * 2f + 1f), wallMaterial, true);
            CreateBox("Wall_West", new Vector3(-ArenaHalfSize - 0.5f, 2f, 0f), new Vector3(1f, 4f, ArenaHalfSize * 2f + 1f), wallMaterial, true);

            // 障碍：给玩家/敌人一点遮挡与绕行信息，不做装饰。
            CreateBox("Obstacle_01", new Vector3(6f, 0.75f, 5f), new Vector3(2f, 1.5f, 2f), obstacleMaterial, true);
            CreateBox("Obstacle_02", new Vector3(-7f, 1f, 6f), new Vector3(3f, 2f, 1.5f), obstacleMaterial, true);
            CreateBox("Obstacle_03", new Vector3(4f, 1.25f, -8f), new Vector3(1.5f, 2.5f, 1.5f), obstacleMaterial, true);
            CreateBox("Obstacle_04", new Vector3(-8f, 0.5f, -5f), new Vector3(2.5f, 1f, 2.5f), obstacleMaterial, true);
            CreateBox("Obstacle_05", new Vector3(0f, 0.75f, 11f), new Vector3(4f, 1.5f, 1f), obstacleMaterial, true);

            var player = BuildPlayer(playerMaterial, aimMaterial);
            var camera = BuildCamera(player.transform);
            BuildLight();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (entry.path != ScenePath)
                {
                    scenes.Add(entry);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log("[Blockout] Arena created at " + ScenePath +
                      " (player=" + player.name + ", camera=" + camera.name + ")");
        }

        private static GameObject BuildPlayer(Material bodyMaterial, Material aimMaterial)
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, 1f, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var aimPivot = new GameObject("AimPivot");
            aimPivot.transform.SetParent(player.transform, false);
            aimPivot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            var muzzle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muzzle.name = "Muzzle";
            muzzle.transform.SetParent(aimPivot.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            muzzle.transform.localScale = new Vector3(0.15f, 0.15f, 0.9f);
            Object.DestroyImmediate(muzzle.GetComponent<Collider>());
            muzzle.GetComponent<Renderer>().sharedMaterial = aimMaterial;

            var input = player.AddComponent<PlayerInputSource>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var inputSo = new SerializedObject(input);
            var actionsProperty = inputSo.FindProperty("_actions");
            if (actionsProperty == null)
            {
                Debug.LogError("[Blockout] PlayerInputSource._actions field not found.");
            }
            else
            {
                actionsProperty.objectReferenceValue = inputAsset;
                inputSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var playerController = player.AddComponent<PlayerController>();
            var controllerSo = new SerializedObject(playerController);
            var pivotProperty = controllerSo.FindProperty("_aimPivot");
            if (pivotProperty == null)
            {
                Debug.LogError("[Blockout] PlayerController._aimPivot field not found.");
            }
            else
            {
                pivotProperty.objectReferenceValue = aimPivot.transform;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return player;
        }

        private static Camera BuildCamera(Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 12f, -6f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 250f;
            camera.clearFlags = CameraClearFlags.Skybox;

            cameraObject.AddComponent<AudioListener>();

            var rig = cameraObject.AddComponent<TopDownCamera>();
            rig.Target = target;
            return camera;
        }

        private static void BuildLight()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = MaterialDir + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[Blockout] URP Lit shader not found.");
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = color;
            material.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateBox(string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }
        }
    }
}
