// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 HUD 从“运行时 Build”转成场景里的常驻 prefab：
//        Assets/Art/UI/Hud/HudRoot.prefab
//        菜单：Rune Ashes/UI/Build HUD Prefab
// 【为什么】prefab 里可以直接调锚点/尺寸/字体；HudView 只做数据刷新。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.EditorTools
{
    public static class UiHudPrefabBuilder
    {
        private const string Root = "Assets/Art/UI/Hud";
        private const string PrefabPath = Root + "/HudRoot.prefab";
        private const string FontPath =
            "Assets/RuneAshes/UI/Resources/Fonts/SourceHanSansSC.asset";

        [MenuItem("Rune Ashes/UI/Build HUD Prefab")]
        public static void Build()
        {
            EnsureFolder(Root);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var root = new GameObject("HudRoot", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var hpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/UI/Bars/UI_HUD_HealthBar.prefab");
            var xpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/UI/Bars/UI_HUD_ExpBar.prefab");

            var hpBar = InstantiatePrefab(hpPrefab, root.transform, "HP_Bar");
            SetRect(hpBar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 56f),
                new Vector2(24f, -24f));
            AddText(hpBar.transform, "HP_Text", "HP 100 / 100", 16, UiTheme.TextColor,
                TextAnchor.MiddleCenter, new Vector2(336f, 28f), Vector2.zero, font);

            var xpBar = InstantiatePrefab(xpPrefab, root.transform, "XP_Bar");
            SetRect(xpBar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520f, 96f),
                new Vector2(0f, 24f));
            AddText(xpBar.transform, "XP_Text", "0 / 14", 22, UiTheme.Amber,
                TextAnchor.MiddleCenter, new Vector2(300f, 40f), new Vector2(31f, -18f), font);

            var levelText = xpBar.transform.Find("LevelBadge/Level_Text");
            if (levelText != null)
            {
                var level = levelText.GetComponent<TextMeshProUGUI>();
                if (level != null)
                {
                    level.text = "1";
                    level.color = UiTheme.Amber;
                }
            }

            var timer = AddText(root.transform, "Timer", "00:00", 30, UiTheme.TextColor,
                TextAnchor.MiddleCenter, new Vector2(300f, 40f), Vector2.zero, font);
            SetRect(timer.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(300f, 40f), new Vector2(0f, -12f));

            var wave = AddText(root.transform, "Wave", "波次 0   存活 0", 16, UiTheme.Dim,
                TextAnchor.MiddleCenter, new Vector2(500f, 26f), Vector2.zero, font);
            SetRect(wave.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(500f, 26f), new Vector2(0f, -56f));

            var chain = AddText(root.transform, "Chain", "—", 14, UiTheme.Cyan,
                TextAnchor.MiddleRight, new Vector2(620f, 28f), Vector2.zero, font);
            SetRect(chain.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(620f, 28f), new Vector2(-24f, 58f));

            var fire = AddText(root.transform, "Fire", "开火：自动（F / Y）", 14, UiTheme.Amber,
                TextAnchor.MiddleRight, new Vector2(620f, 24f), Vector2.zero, font);
            SetRect(fire.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(620f, 24f), new Vector2(-24f, 30f));

            var fps = AddText(root.transform, "FPS", "FPS 60", 14, UiTheme.Dim,
                TextAnchor.MiddleRight, new Vector2(200f, 26f), Vector2.zero, font);
            SetRect(fps.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(200f, 26f), new Vector2(-24f, -16f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            var existing = GameObject.Find("HudRoot");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            existing = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            existing.name = "HudRoot";

            var player = GameObject.Find("Player");
            var hudView = player == null ? null : player.GetComponent<HudView>();
            if (hudView != null && existing != null)
            {
                var so = new SerializedObject(hudView);
                var prop = so.FindProperty("_hudCanvas");
                if (prop != null)
                {
                    prop.objectReferenceValue = existing.GetComponent<Canvas>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[UiHudPrefabBuilder] HUD prefab 已生成并挂到场景：" + PrefabPath);
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name)
        {
            if (prefab == null)
            {
                var fallback = new GameObject(name, typeof(RectTransform));
                fallback.transform.SetParent(parent, false);
                return fallback;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            instance.name = name;
            return instance;
        }

        private static void SetRect(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 size,
            Vector2 position)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string content, int fontSize,
            Color color, TextAnchor alignment, Vector2 size, Vector2 position, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = font != null ? font : UiTheme.Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = ToTmpAlignment(alignment);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
