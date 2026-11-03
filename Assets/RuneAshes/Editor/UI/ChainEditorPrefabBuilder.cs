// ─────────────────────────────────────────────────────────────────────────────
// 【职责】生成链编辑器静态原型 prefab：
//        UI_Chain_Panel 底板 + 4 槽 + 符文库 + 能耗/预览。
//        拖拽/装配逻辑不在本文件，先验证布局与可读性。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Data;
using RuneAshes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.EditorTools
{
    public static class ChainEditorPrefabBuilder
    {
        private const string Folder = "Assets/Art/UI/Screens";
        private const string PrefabPath = Folder + "/ChainEditorRoot.prefab";

        [MenuItem("Rune Ashes/UI/Build Chain Editor Prefab")]
        public static void Build()
        {
            EnsureFolder(Folder);
            LoadTheme();

            var catalog = UiTheme.Catalog;
            var canvas = UiTheme.CreateCanvas("ChainEditorRoot", 150);
            var panelSprite = catalog != null ? catalog.ChainPanel : null;
            var panel = UiTheme.CreateImage(canvas.transform, "Panel",
                panelSprite != null ? Color.white : UiTheme.Bg,
                new Vector2(1344f, 768f), Vector2.zero, sprite: panelSprite);

            UiTheme.CreateText(panel.transform, "Title", "符文链", 36, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(700f, 50f), new Vector2(0f, 320f));
            UiTheme.CreateText(panel.transform, "Hint", "Tab / Esc 关闭    ·    拖拽装配（下一阶段）", 14,
                UiTheme.Dim, TextAnchor.MiddleCenter, new Vector2(900f, 26f), new Vector2(0f, 280f));

            AddSlot(panel.transform, "T2", "击杀回响", RuneCategory.Trigger, -480f, catalog);
            AddSlot(panel.transform, "E2", "散射", RuneCategory.Effect, -160f, catalog);
            AddSlot(panel.transform, "M1", "分裂", RuneCategory.Modifier, 160f, catalog);
            AddSlot(panel.transform, "M6", "爆炸", RuneCategory.Modifier, 480f, catalog);

            UiTheme.CreateText(panel.transform, "LibraryLabel", "符文库", 18, UiTheme.Silver,
                TextAnchor.MiddleLeft, new Vector2(200f, 28f), new Vector2(-540f, -190f));

            var libraryIds = new[] { "T1", "T2", "T3", "T4", "E1", "E2", "M1", "M6" };
            for (var i = 0; i < libraryIds.Length; i++)
            {
                AddLibrarySlot(panel.transform, libraryIds[i], -420f + i * 112f, -265f, catalog);
            }

            UiTheme.CreateText(panel.transform, "Energy", "能耗  7 / 10", 18, UiTheme.Amber,
                TextAnchor.MiddleLeft, new Vector2(300f, 28f), new Vector2(-540f, -338f));
            var energyBg = UiTheme.CreateImage(panel.transform, "EnergyBg", UiTheme.Slate,
                new Vector2(620f, 12f), new Vector2(-150f, -340f));
            UiTheme.CreateImage(energyBg.transform, "EnergyFill", UiTheme.Amber,
                new Vector2(430f, 8f), Vector2.zero);
            UiTheme.CreateText(panel.transform, "Preview", "预览  DPS 86    射速 1.2/s    弹数 5×3    关键词 连锁 / 范围", 14,
                UiTheme.Dim, TextAnchor.MiddleCenter, new Vector2(1100f, 26f), new Vector2(0f, -360f));

            canvas.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, PrefabPath);

            var existing = FindRoot("ChainEditorRoot");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var instance = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) as GameObject;
            instance.name = "ChainEditorRoot";
            instance.SetActive(false);

            var player = GameObject.Find("Player");
            if (player != null)
            {
                var screen = player.GetComponent<ChainEditorScreen>();
                if (screen == null)
                {
                    screen = player.AddComponent<ChainEditorScreen>();
                }

                var so = new SerializedObject(screen);
                so.FindProperty("_canvas").objectReferenceValue = instance.GetComponent<Canvas>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Object.DestroyImmediate(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ChainEditorPrefabBuilder] 生成并挂到场景：" + PrefabPath);
        }

        private static void AddSlot(Transform parent, string id, string name, RuneCategory category,
            float x, UiSpriteCatalog catalog)
        {
            Sprite frame = null;
            if (catalog != null)
            {
                frame = category == RuneCategory.Trigger ? catalog.ChainSlotTrigger
                    : category == RuneCategory.Effect ? catalog.ChainSlotEffect
                    : catalog.ChainSlotModifier;
            }

            var slot = UiTheme.CreateImage(parent, "Slot_" + id,
                frame != null ? Color.white : UiTheme.Slate,
                new Vector2(160f, 200f), new Vector2(x, 100f), false, frame, true);
            Sprite icon;
            if (catalog != null && catalog.TryGetRuneIcon(id, out icon))
            {
                UiTheme.CreateImage(slot.transform, "Icon", Color.white,
                    new Vector2(88f, 88f), new Vector2(0f, 26f), false, icon, true);
            }

            UiTheme.CreateText(slot.transform, "Id", id, 20, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(150f, 24f), new Vector2(0f, -42f));
            UiTheme.CreateText(slot.transform, "Name", name, 14, UiTheme.TextColor,
                TextAnchor.MiddleCenter, new Vector2(150f, 22f), new Vector2(0f, -66f));
        }

        private static void AddLibrarySlot(Transform parent, string id, float x, float y,
            UiSpriteCatalog catalog)
        {
            var slot = UiTheme.CreateImage(parent, "Lib_" + id,
                catalog != null && catalog.CardIconSlot != null ? Color.white : UiTheme.Slate,
                new Vector2(96f, 96f), new Vector2(x, y), false,
                catalog != null ? catalog.CardIconSlot : null, true);
            Sprite icon;
            if (catalog != null && catalog.TryGetRuneIcon(id, out icon))
            {
                UiTheme.CreateImage(slot.transform, "Icon", Color.white,
                    new Vector2(72f, 72f), Vector2.zero, false, icon, true);
            }
        }

        private static GameObject FindRoot(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            return null;
        }

        private static void LoadTheme()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UiSpriteCatalog>(
                "Assets/RuneAshes/UI/Resources/UiSpriteCatalog.asset");
            if (catalog != null) UiTheme.SetCatalog(catalog);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/RuneAshes/UI/Resources/Fonts/SourceHanSansSC.asset");
            if (font != null) UiTheme.SetFont(font);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
