// ─────────────────────────────────────────────────────────────────────────────
// 【职责】在 UI 组件 Awake 之前注入正式字体（思源黑体），避免每个界面各自找字体。
// 【执行顺序】-1000：保证 HudView / UpgradeScreen / SettlementScreen 读到的已经是正式字体。
// 【兜底】字体没赋值时 UiTheme 回退系统微软雅黑，Windows 构建仍可读中文。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.UI
{
    [DefaultExecutionOrder(-1000)]
    public sealed class UiBootstrap : MonoBehaviour
    {
        [SerializeField] private Font _font;

        private void Awake()
        {
            if (_font != null)
            {
                UiTheme.SetFont(_font);
            }
        }
    }
}
