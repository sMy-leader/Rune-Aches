// ─────────────────────────────────────────────────────────────────────────────
// 【职责】UI 的唯一主题来源：颜色、TMP 字体、纯色 Sprite、Canvas/Text/Image 工厂。
// 【字体】正式 UI 统一走 TextMeshPro；中文使用动态 SDF 的思源黑体。
//        字体资产在 Assets/RuneAshes/UI/Resources/Fonts/。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Data;
using TMPro;
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

        private static TMP_FontAsset _font;
        private static TMP_FontAsset _fontOverride;
        private static Sprite _white;
        private static UiSpriteCatalog _catalog;

        /// <summary>由 UiBootstrap 在启动时注入正式 Sprite 目录；为空时全部回退纯色占位。</summary>
        public static void SetCatalog(UiSpriteCatalog catalog)
        {
            _catalog = catalog;
        }

        public static UiSpriteCatalog Catalog
        {
            get { return _catalog; }
        }

        /// <summary>由 UiBootstrap 在启动时注入思源 TMP 字体；为空时从 Resources 兜底。</summary>
        public static void SetFont(TMP_FontAsset font)
        {
            if (font != null)
            {
                _fontOverride = font;
                _font = font;
            }
        }

        public static TMP_FontAsset Font
        {
            get
            {
                if (_fontOverride != null)
                {
                    return _fontOverride;
                }

                if (_font == null)
                {
                    _font = Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansSC");
                    if (_font == null)
                    {
                        _font = TMP_Settings.defaultFontAsset;
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
            Vector2 position, bool raycast = false, Sprite sprite = null, bool preserveAspect = false,
            bool sliced = false)
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
            image.sprite = sprite != null ? sprite : White;
            image.color = color;
            image.type = sprite != null && sliced && sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycast;
            return image;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor alignment, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.font = Font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = ToTmpAlignment(alignment);
            text.textWrappingMode = TextWrappingModes.Normal;
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
