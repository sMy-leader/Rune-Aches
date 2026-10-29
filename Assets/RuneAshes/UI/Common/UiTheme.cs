// ─────────────────────────────────────────────────────────────────────────────
// 【职责】UI 的唯一主题来源：颜色、字体、纯色 Sprite、Canvas/Text/Image 工厂。
// 【为什么用纯色占位】正式 UI 素材还没到位；先保证布局/交互/可读性，
//        素材到位后只替换 Sprite，不改布局代码。
// 【字体】暂时用系统微软雅黑（Windows 可用）；正式版换成思源黑体 + TMP。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public static class UiTheme
    {
        public static readonly Color Bg = new Color(0.055f, 0.07f, 0.07f, 0.86f);
        public static readonly Color Slate = new Color(0.10f, 0.13f, 0.13f, 1f);
        public static readonly Color Line = new Color(0.20f, 0.25f, 0.24f, 1f);
        public static readonly Color TextColor = new Color(0.86f, 0.90f, 0.89f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.62f, 0.60f, 1f);
        public static readonly Color Amber = new Color(0.91f, 0.63f, 0.24f, 1f);
        public static readonly Color Cyan = new Color(0.27f, 0.85f, 0.77f, 1f);
        public static readonly Color Silver = new Color(0.79f, 0.83f, 0.82f, 1f);
        public static readonly Color Danger = new Color(0.85f, 0.36f, 0.31f, 1f);

        private static Font _font;
        private static Sprite _white;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial" }, 24);
                    if (_font == null)
                    {
                        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    }
                }

                return _font;
            }
        }

        /// <summary>无贴图时的纯白 Sprite；Image 的 Filled 模式需要它。</summary>
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                        new Vector2(0.5f, 0.5f));
                }

                return _white;
            }
        }

        public static Color CategoryColor(RuneCategory category)
        {
            switch (category)
            {
                case RuneCategory.Trigger:
                    return Amber;
                case RuneCategory.Effect:
                    return Cyan;
                default:
                    return Silver;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static Image CreateImage(Transform parent, string name, Color color, Vector2 size,
            Vector2 position, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.sprite = White;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize,
            Color color, TextAnchor alignment, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
