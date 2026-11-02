# 《符文余烬》UI 资产接入报告 v1

> 日期：2026-11-01
> 范围：`Assets/Art/UI`、`Assets/Layer Lab`、现有 UI prefab、`HudView / UpgradeScreen / SettlementScreen`
> 结论：**卡牌/面板/按钮/符文图标可以接入第一版；HUD 条和链编辑器还不能完整接。**

---

## 一、资产盘点

### 1.1 图片

| 类别 | 数量 | 状态 |
|---|---:|---|
| Texture2D 总计 | 62 | 大多已是 Sprite |
| 符文图标 | 20 | T1–T6、E1–E7、M1–M7 完整，256×256 |
| 通用图标 | 13 | 缺 7 个（见缺口） |
| 按钮 | 2 | Primary 398×186、Secondary 283×189 |
| 面板/背景 | 4 | Main 1280×720、Settlement 1920×1080、MainMenu 2048×1152、Dark |
| 卡牌图标位 | 1 | 140×150 |
| 链槽位/库槽位/HUD 槽位 | 12 | 有图，部分没有 prefab |
| 字体 | 3 套 | 思源黑体 / Cinzel / JetBrains Mono |

### 1.2 Prefab

| 来源 | 数量 | 说明 |
|---|---:|---|
| 原有 UI prefab | 20 | HUD 条、卡牌框、图标按钮、面板、提示框等 |
| 本次生成 | 13 | 按钮×2、面板×1、卡牌图标位×1、槽位/库槽位/HUD 槽位×9 |
| 合计 | 33 | `UI_Button_*`、`UI_Panel_Main`、`UI_Card_IconSlot`、`UI_Chain_*`、`UI_HUD_Slot_*` |

### 1.3 两套素材并存（必须确认）

现有 20 个 prefab 主要引用 **`Assets/Layer Lab`** 的 GUI Pro 资源包（`Frame_CardFrame01_*`、`Slider_Level_*`、`Btn_IconButton_*`），不是 `Art/UI` 里的 PNG。

- 这些 prefab 只适合作为本地参考/外壳；
- 如果要把 prefab 提交到远端，必须同时提交 Layer Lab 对应资源（需确认授权），或把 sprite 替换成 `Art/UI` 素材；
- 目前 `UiSpriteCatalog` 的 `CardFrameCommon` 仍指向 `Frame_CardFrame01_Green`，这是 **Layer Lab 依赖**，是当前最大的接入风险。

---

## 二、本次已做的规范化

### 2.1 导入设置

`UiArtPipeline` 菜单：

```
Rune Ashes/UI/Normalize Art & Build Prefabs
```

做了：

- 统一 Texture Type = Sprite、Sprite Mode = Single；
- 图标：Tight mesh、max 256、无 Mipmap；
- 按钮/面板/槽位：FullRect、max 2048、设置 9-slice border；
- 扩展名 `.Png/.PNG` 统一尝试改为 `.png`；
- 生成缺失 prefab；
- 生成 `Assets/RuneAshes/UI/Resources/UiSpriteCatalog.asset`（面板/卡牌/按钮/槽位/20 个符文图标）。

### 2.2 NPOT 兜底（重要）

Unity 6.3 对部分 NPOT 图片没有生成 Sprite 子资产，并且会做非等比缩放：

```
UI_Button_Primary: 398×186 → 512×128
UI_Panel_Main:     1280×720 → 512×256
```

现在管线会：

1. 先尝试 importer 生成的 Sprite；
2. 失败时读取原始 PNG 字节，生成原尺寸 Texture/Sprite 资产：
   - `Assets/Art/UI/GeneratedSprites/Textures/*.asset`
   - `Assets/Art/UI/GeneratedSprites/Original/*.asset`

已验证：

| 资源 | 生成尺寸 |
|---|---|
| Button Primary | 398×186 |
| Panel Main | 1280×720 |
| Card Icon Slot | 140×150 |
| Chain Slot Effect | 707×686 |

> 因此“素材像素尺寸和清单不完全一致”不是阻塞项：用 `RectTransform` 缩放 + `preserveAspect` + 9-slice 即可；真正需要统一的是 Sprite 比例和 border，而不是原始像素。

### 2.3 代码接入

新增：

- `UI/Common/UiSpriteCatalog.cs`：正式 Sprite 目录；
- `Editor/UI/UiArtPipeline.cs`：导入/生成 prefab/Catalog；
- `UiBootstrap` 从 `Resources/UiSpriteCatalog` 加载目录；
- `UiTheme.CreateImage` 支持传入 Sprite / 9-slice / preserveAspect。

已接入：

| 界面 | 接入内容 |
|---|---|
| 三选一 | 面板 `UI_Panel_Main`、卡牌框（当前 Layer Lab）、卡牌图标位、符文图标、确认按钮 |
| 结算 | 面板 `UI_Panel_Main`、主按钮 |
| HUD | **还没有替换**，仍是纯色条/文字 |

---

## 三、现在能不能接正式 UI？

### 可以接（第一版）

- 三选一卡牌：面板、卡牌框、图标位、20 个符文图标、按钮；
- 结算：面板、按钮；
- 按钮/面板单独 prefab 已生成；
- 缺 Sprite 时自动回退纯色，不会崩。

### 还不能完整接

| 缺口 | 影响 | 需要什么 |
|---|---|---|
| HUD HP/经验条 | 现在仍是纯色条 | 需要 `UI_Bar_Health/Exp` 的底+填充 PNG，或确认使用 Layer Lab 条 |
| 卡牌框 | 当前引用 Layer Lab | 需要 `Art/UI/Cards/UI_Card_Frame.png`，或授权 Layer Lab |
| 链编辑器 | 只有槽位/库槽位图 | 缺主面板、连接线、能量条、滚动条、预览面板 |
| 通用图标 | 13/20 | 缺 Settings/Pause/Play/Volume/Info/Keyboard 等 7 个 |
| 按钮状态 | 只有 Normal 单图 | 缺 Hover/Pressed/Disabled（可先 Color Tint） |
| 字体排版 | 仍是 Legacy `Text` | TMP 已导入但 UI 代码未切 TMP；正式版建议切 TMP |
| 9-slice 视觉 | border 是估算值 | 需要实际拉长按钮/面板检查边缘是否变形 |
| Pause/MainMenu | 只有 Layer Lab 的 `UI_Pause_Panel` | 需要正式暂停/主菜单素材或确认复用 |

### 当前最大风险

**`UI_Card_Frame_*` / `UI_HUD_HealthBar` / `UI_HUD_ExpBar` 等 prefab 依赖 `Assets/Layer Lab`。**
如果只提交 `Art/UI` 而不提交 Layer Lab，远端 clone 会出现粉红/丢图；如果提交 Layer Lab，需要先确认该资源包的商用/再分发授权。

---

## 四、建议的下一步

### P0（现在）

1. 决定卡牌框/HUD 条走哪套：
   - A. 使用 `Art/UI`：补一张卡牌框 + HP/EXP 条 PNG，我重跑管线；
   - B. 使用 Layer Lab：确认授权并随工程提交。
2. 补 7 个通用图标（Settings/Pause/Play/Volume/Info/Keyboard/Close 等）。
3. 把 HUD 的 HP/EXP 替换成正式条 Sprite（或先保留纯色，等条素材）。

### P1（W4/W5）

1. 链编辑器主面板、连接线、能量条、滚动条、预览面板；
2. 按钮 Hover/Pressed/Disabled 三态；
3. 正式 TMP 字体资产与字号表；
4. 1920×1080 + 1280×720 截图验收，检查文字溢出/焦点/灰度可读性。

---

## 五、验收状态

- EditMode：**47/47 通过**
- Windows 构建：**258.49 MB，errors=0，warnings=1**
- Catalog：面板/卡牌/按钮/20 符文图标均非空
- 生成 prefab：33 个
- 未提交的第三方资源：`Assets/Layer Lab`、`Assets/Icon_Rune`（需授权确认）
