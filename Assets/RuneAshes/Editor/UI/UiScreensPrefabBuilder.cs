// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把三选一/结算的“静态外壳”做成场景 prefab：
//        Assets/Art/UI/Screens/UpgradeRoot.prefab
//        Assets/Art/UI/Screens/SettlementRoot.prefab
//        动态卡牌仍由 UpgradeScreen 在运行时创建。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.EditorTools
{
    public static class UiScreensPrefabBuilder
    {
        private const string Folder = "Assets/Art/UI/Screens";
        private const string UpgradePath = Folder + "/UpgradeRoot.prefab";
        private const string SettlementPath = Folder + "/SettlementRoot.prefab";

        [MenuItem("Rune Ashes/UI/Build Upgrade & Settlement Prefabs")]
        public static void Build()
        {
            EnsureFolder(Folder);
            LoadTheme();

            BuildUpgrade();
            BuildSettlement();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[UiScreensPrefabBuilder] UpgradeRoot / SettlementRoot 已生成并接入场景。");
        }

        private static void BuildUpgrade()
        {
            var canvas = UiTheme.CreateCanvas("UpgradeRoot", 100);
            var overlay = UiTheme.CreateImage(canvas.transform, "Overlay",
                new Color(0f, 0f, 0f, 0.72f), new Vector2(1920f, 1080f), Vector2.zero, true);
            var panelSprite = UiTheme.Catalog != null ? UiTheme.Catalog.PanelMain : null;
            var panel = UiTheme.CreateImage(overlay.transform, "Panel",
                panelSprite != null ? Color.white : UiTheme.Bg,
                new Vector2(1440f, 780f), Vector2.zero, sprite: panelSprite);

            UiTheme.CreateImage(panel.transform, "TopLine", UiTheme.Amber,
                new Vector2(1440f, 4f), new Vector2(0f, 388f));
            UiTheme.CreateImage(panel.transform, "BottomLine", UiTheme.Line,
                new Vector2(1440f, 2f), new Vector2(0f, -388f));
            UiTheme.CreateText(panel.transform, "Title", "选择符文", 40, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(900f, 60f), new Vector2(0f, 320f));
            UiTheme.CreateText(panel.transform, "Hint", "选择一张，装入当前符文链", 16, UiTheme.Dim,
                TextAnchor.MiddleCenter, new Vector2(900f, 30f), new Vector2(0f, 270f));

            var cards = new GameObject("Cards", typeof(RectTransform));
            cards.transform.SetParent(panel.transform, false);
            var cardsRect = cards.GetComponent<RectTransform>();
            cardsRect.anchorMin = cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRect.sizeDelta = new Vector2(1200f, 480f);
            cardsRect.anchoredPosition = new Vector2(0f, -20f);

            canvas.gameObject.SetActive(false);
            SaveAndAttach(canvas.gameObject, UpgradePath, "UpgradeRoot", false);
            Object.DestroyImmediate(canvas.gameObject);
        }

        private static void BuildSettlement()
        {
            var canvas = UiTheme.CreateCanvas("SettlementRoot", 200);
            var overlay = UiTheme.CreateImage(canvas.transform, "Overlay",
                new Color(0f, 0f, 0f, 0.80f), new Vector2(1920f, 1080f), Vector2.zero, true);
            var panelSprite = UiTheme.Catalog != null ? UiTheme.Catalog.PanelMain : null;
            var panel = UiTheme.CreateImage(overlay.transform, "Panel",
                panelSprite != null ? Color.white : UiTheme.Bg,
                new Vector2(1100f, 640f), Vector2.zero, sprite: panelSprite);

            UiTheme.CreateImage(panel.transform, "TopLine", UiTheme.Danger,
                new Vector2(1100f, 4f), new Vector2(0f, 318f));
            UiTheme.CreateText(panel.transform, "Title", "本局结束", 44, UiTheme.Danger,
                TextAnchor.MiddleCenter, new Vector2(900f, 64f), new Vector2(0f, 245f));
            UiTheme.CreateText(panel.transform, "Stats", "—", 22, UiTheme.TextColor,
                TextAnchor.UpperLeft, new Vector2(900f, 180f), new Vector2(0f, 60f));
            UiTheme.CreateText(panel.transform, "Chain", "—", 18, UiTheme.Cyan,
                TextAnchor.MiddleCenter, new Vector2(900f, 60f), new Vector2(0f, -80f));

            var buttonSprite = UiTheme.Catalog != null ? UiTheme.Catalog.ButtonPrimary : null;
            var restart = UiTheme.CreateImage(panel.transform, "Restart",
                buttonSprite != null ? Color.white : UiTheme.Amber,
                new Vector2(280f, 68f), new Vector2(0f, -235f), true, buttonSprite, false, true);
            UiTheme.CreateText(restart.transform, "Label", "再来一局", 24,
                buttonSprite != null ? UiTheme.TextColor : UiTheme.Bg,
                TextAnchor.MiddleCenter, new Vector2(280f, 68f), Vector2.zero);

            var button = restart.gameObject.AddComponent<Button>();
            button.targetGraphic = restart;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = UiTheme.Amber;
            colors.pressedColor = Color.Lerp(UiTheme.Amber, Color.white, 0.35f);
            colors.selectedColor = UiTheme.Amber;
            button.colors = colors;

            canvas.gameObject.SetActive(false);
            SaveAndAttach(canvas.gameObject, SettlementPath, "SettlementRoot", true);
            Object.DestroyImmediate(canvas.gameObject);
        }

        private static void SaveAndAttach(GameObject root, string prefabPath, string sceneName, bool settlement)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            var instance = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject;
            instance.name = sceneName;
            instance.SetActive(false);

            var player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            if (settlement)
            {
                var screen = player.GetComponent<SettlementScreen>();
                if (screen == null) return;
                var so = new SerializedObject(screen);
                var panel = instance.transform.Find("Overlay/Panel");
                so.FindProperty("_canvas").objectReferenceValue = instance.GetComponent<Canvas>();
                so.FindProperty("_statsText").objectReferenceValue =
                    panel.Find("Stats").GetComponent<TMP_Text>();
                so.FindProperty("_chainText").objectReferenceValue =
                    panel.Find("Chain").GetComponent<TMP_Text>();
                so.FindProperty("_restartButton").objectReferenceValue =
                    panel.Find("Restart").GetComponent<Button>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var screen = player.GetComponent<UpgradeScreen>();
                if (screen == null) return;
                var so = new SerializedObject(screen);
                var panel = instance.transform.Find("Overlay/Panel");
                so.FindProperty("_canvas").objectReferenceValue = instance.GetComponent<Canvas>();
                so.FindProperty("_cardRoot").objectReferenceValue =
                    panel.Find("Cards").GetComponent<RectTransform>();
                so.FindProperty("_title").objectReferenceValue =
                    panel.Find("Title").GetComponent<TMP_Text>();
                so.FindProperty("_hint").objectReferenceValue =
                    panel.Find("Hint").GetComponent<TMP_Text>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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
