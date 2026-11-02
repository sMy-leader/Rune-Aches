// ─────────────────────────────────────────────────────────────────────────────
// 【职责】在 UI 组件 Awake 之前注入正式字体（思源黑体），避免每个界面各自找字体。
// 【执行顺序】-1000：保证 HudView / UpgradeScreen / SettlementScreen 读到的已经是正式字体。
// 【兜底】字体没赋值时 UiTheme 回退系统微软雅黑，Windows 构建仍可读中文。
// ─────────────────────────────────────────────────────────────────────────────
using TMPro;
using UnityEngine;

namespace RuneAshes.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed class UiBootstrap : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private UiSpriteCatalog _catalog;

        private void Awake()
        {
            var font = _fontAsset != null
                ? _fontAsset
                : Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansSC");
            if (font != null)
            {
                UiTheme.SetFont(font);
            }

            var catalog = _catalog != null
                ? _catalog
                : Resources.Load<UiSpriteCatalog>("UiSpriteCatalog");
            if (catalog != null)
            {
                UiTheme.SetCatalog(catalog);
            }
        }
    }
}
