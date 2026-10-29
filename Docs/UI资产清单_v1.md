# 《符文余烬》UI 资产清单与生成提示词 v1

> 配套：`Docs/UI与图标设计规范_v1.md`（风格/色板）、`Docs/开发路线_v2.md`（W3b 排期）
> 用途：直接复制 prompt 到 Holopix / 其他生图工具；生成后按第六节后处理与导入
> 日期：2026-10-29

---

## 〇、怎么用这份文档

1. **先出 3 个样本**：T1、E1、M1。确认风格、灰度可辨、外发光强度，再批量生成 20 个。
2. **所有 prompt 都带 `no text / no letters / no numbers`**：AI 生成的文字不能用，UI 文字由 Unity 排。
3. **生成图不要直接当 9-slice**：面板/边框/按钮需要后处理（抠背景、统一描边、设 Border）。
4. **透明背景**：如果 Holopix 不支持透明，用纯黑/纯灰背景生成，后期抠图；不要用复杂渐变背景。
5. 生成后放进 `Assets/Art/UI/RuneIcons/`、`Frames/`、`Cards/`、`Icons/`，命名见第六节。

---

## 一、全部 UI 清单（按优先级）

### P0 — W3b 必须（没有就无法玩循环）

| ID | 资源 | 数量 | 尺寸 | 说明 |
|---|---|---:|---|---|
| `UI_HUD_HealthBar` | HP 条底 + 填充 | 2 | 512×64 | 底部可 9-slice |
| `UI_HUD_ExpBar` | 经验条底 + 填充 | 2 | 512×32 | 比 HP 细 |
| `UI_HUD_Slot` | 冲刺冷却/技能小槽 | 1 | 64×64 | 可复用 |
| `UI_HUD_Timer` | 计时/波次底 | 1 | 256×64 | 顶部中央 |
| `UI_HUD_ChainStrip` | 链缩略底 | 1 | 320×72 | 右下 4 图标 |
| `UI_Card_Frame` | 三选一卡牌底 | 4 态 | 320×440 | 普通/稀有/强化/已满 |
| `UI_Card_IconSlot` | 卡牌图标位 | 1 | 200×200 | 可透明 |
| `UI_Button_Primary` | 主按钮 | 3 态 | 256×96 | 再来一局/确认 |
| `UI_Button_Secondary` | 次按钮 | 3 态 | 192×72 | 取消/返回 |
| `UI_Panel_Main` | 通用面板底 | 1 | 1024×1024 | 9-slice |
| `UI_Panel_Dark` | 半透明遮罩 | 1 | 1024×1024 | 9-slice |
| `UI_Tooltip` | 提示框 | 1 | 512×256 | 9-slice |
| `UI_ErrorBanner` | 错误条 | 1 | 768×64 | 红边 + 警示图标位 |
| `UI_RuneIcon_*` | 20 个符文图标 | 20 | 256×256 | 最重要的一套 |
| `UI_Icon_*` | 通用图标 | 20 | 128/64 | 设置/暂停/音量等 |

### P1 — W3b 末 / W5（链编辑器与结算）

| ID | 资源 | 数量 | 尺寸 | 说明 |
|---|---|---:|---|---|
| `UI_Chain_Panel` | 链编辑器主面板 | 1 | 1600×900 | 9-slice |
| `UI_Chain_Slot_Trigger` | 触发槽（空/占用/悬停/焦点/非法） | 5 态 | 160×200 | 六边形 |
| `UI_Chain_Slot_Effect` | 效果槽 | 5 态 | 160×200 | 菱形 |
| `UI_Chain_Slot_Modifier` | 修饰槽 | 5 态 | 160×200 | 圆角方 |
| `UI_Chain_Connector` | 连接线/箭头/能量流 | 3 | 128×64 | 可平铺 |
| `UI_Chain_EnergyBar` | 10 段能耗条 | 2 | 512×64 | 底 + 填充 |
| `UI_Chain_LibrarySlot` | 符文库格子 | 4 态 | 96×96 | 普通/悬停/选中/已装 |
| `UI_Chain_Scrollbar` | 滚动条 | 1 | 16×256 | 细 |
| `UI_Chain_Preview` | 预览面板底 | 1 | 480×160 | 9-slice |
| `UI_Settlement_Background` | 结算背景 | 1 | 1920×1080 | 低对比 |
| `UI_Settlement_Chain` | 构筑链展示底 | 1 | 800×120 | 4 图标 |
| `UI_MainMenu_Background` | 主菜单背景 | 1 | 1920×1080 | P2 |
| `UI_Pause_Panel` | 暂停面板 | 1 | 800×600 | P2 |

### 状态与交互

每个可点击元素至少需要：

```
Normal / Hover / Pressed / Disabled / Selected / Error
```

如果不想做 6 套图，可以只做 Normal，Hover/Selected 用 Unity 的 Color Tint 或外发光叠加；但**错误态必须有独立视觉**（红边 + 抖动），不能只靠颜色。

### 字体

| 用途 | 字体 | 授权 |
|---|---|---|
| 中文正文 | 思源黑体 SC Regular/Medium | SIL OFL |
| 英文标题 | Cinzel | SIL OFL |
| 数字 | JetBrains Mono | SIL OFL |

---

## 二、符文图标：20 个完整 prompt

### 统一风格后缀（每条 prompt 末尾都加）

```
stylized hand-painted game icon, dark fantasy, low-poly flat shading, muted desaturated palette,
a single glowing rune symbol carved into dark slate, thick dark outline, high contrast,
centered composition, 45 degree rim light, subtle ember particles, plain near-black background,
no text, no letters, no numbers, no watermark, 512x512
```

### 通用负面词（每条都加）

```
photorealistic, pbr, 8k, blurry, noisy, gradient background, circular badge, multiple icons,
text, letters, numbers, watermark, lens flare, chromatic aberration, busy background
```

> 三类外轮廓是刻意的：**触发=六边形/圆、效果=菱形、修饰=圆角方/裂片**。
> 灰度测试时，外轮廓就能区分类别。

### T1–T6 触发（琥珀 #E8A13C）

**T1 定时脉冲**
```
a hexagonal dark slate rune tile, a glowing amber clock face with one tick mark and a thin pulsing ring, carved groove, amber emission #E8A13C, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**T2 击杀回响**
```
a hexagonal dark slate rune tile, a small skull silhouette with three echo arcs behind it, amber emission #E8A13C, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**T3 命中传导**
```
a hexagonal dark slate rune tile, three linked arrowheads forming a chain lightning path, amber emission #E8A13C, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**T4 冲刺余波**
```
a hexagonal dark slate rune tile, an afterimage silhouette of a sprinting figure with motion streaks, amber emission #E8A13C, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**T5 受伤反击**
```
a hexagonal dark slate rune tile, a cracked heart with a rebound arrow piercing outward, amber emission #E8A13C, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**T6 常驻光环**
```
a hexagonal dark slate rune tile, concentric aura rings around a small standing figure, amber emission #E8A13C, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

### E1–E7 效果（青绿 #45D9C4）

**E1 直射弹**
```
a diamond-shaped dark slate rune tile, a single sharp arrowhead pointing right, a thin trail behind it, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E2 散射**
```
a diamond-shaped dark slate rune tile, a fan of three arrows spreading from one point, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E3 环绕球**
```
a diamond-shaped dark slate rune tile, two glowing orbs orbiting a central point with a circular trail, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E4 地雷**
```
a diamond-shaped dark slate rune tile, a pressure plate with small spikes and a radiating burst, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E5 冲刺斩**
```
a diamond-shaped dark slate rune tile, a diagonal blade slash with a sharp motion trail, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E6 光束**
```
a diamond-shaped dark slate rune tile, a horizontal charged beam with a bright core and lens-like ends, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**E7 召唤物**
```
a diamond-shaped dark slate rune tile, a small sentry totem with a single glowing eye, cyan emission #45D9C4, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

### M1–M7 修饰（银白 #C9D4D0，少量元素色点缀）

**M1 分裂**
```
a rounded-square dark slate rune tile, one arrow splitting into three smaller arrows, silver emission #C9D4D0, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M2 弹射**
```
a rounded-square dark slate rune tile, an arrow bouncing between two points with a refraction arc, silver emission #C9D4D0, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M3 穿透**
```
a rounded-square dark slate rune tile, an arrow passing through a thin shield plate with a clean hole, silver emission #C9D4D0, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M4 燃烧**
```
a rounded-square dark slate rune tile, a stylized flame with three embers, silver body with amber inner glow, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M5 冰冻**
```
a rounded-square dark slate rune tile, a sharp ice crystal snowflake with three shards, silver body with cyan inner glow, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M6 爆炸**
```
a rounded-square dark slate rune tile, a radial explosion burst with chipped stone shards, silver body with amber inner glow, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

**M7 追踪**
```
a rounded-square dark slate rune tile, a single eye with a curved homing trajectory around it, silver emission #C9D4D0, carved groove, stylized hand-painted game icon, dark fantasy, low-poly flat shading, thick dark outline, centered, 45 degree rim light, subtle ember particles, plain near-black background, no text, no letters, no numbers, 512x512
```

---

## 三、链编辑器素材 prompt

> **不要直接生成“完整可切图 UI”**。AI 生成的整图文字会乱码、边缘不可控。
> 正确做法：先生成一张风格概念图做参考；再单独生成面板/槽位/连接线/按钮，后期拼装。

### 3.1 整体概念图（只做风格参考）

```
game UI mockup of a rune chain editor, dark slate stone panel with thin bronze trim,
horizontal row of four rune slots connected by glowing cyan energy lines,
left slot hexagonal amber frame, second slot diamond cyan frame, two modifier slots rounded-square silver frames,
bottom segmented energy bar, right side preview panel, dark fantasy, hand-painted low-poly,
high contrast, no text, no letters, no numbers, placeholder text as abstract thin lines,
16:9, centered, 1920x1080
```

### 3.2 主面板底（9-slice）

```
game UI panel, dark slate stone slab, thin bronze border, subtle carved edges, worn corners,
9-slice friendly, straight edges, empty center, no text, no icons, isolated on plain black background,
orthographic, flat lighting, 1024x1024
```

### 3.3 槽位边框：触发（六边形，琥珀）

```
game UI slot frame, tall hexagonal frame, dark slate interior, thin amber bronze border,
subtle inner glow, empty center, no icon, no text, isolated on plain black background,
orthographic, 9-slice friendly, 256x320
```

### 3.4 槽位边框：效果（菱形，青绿）

```
game UI slot frame, tall diamond-shaped frame, dark slate interior, thin cyan bronze border,
subtle inner glow, empty center, no icon, no text, isolated on plain black background,
orthographic, 9-slice friendly, 256x320
```

### 3.5 槽位边框：修饰（圆角方，银白）

```
game UI slot frame, tall rounded-square frame, dark slate interior, thin silver bronze border,
subtle inner glow, empty center, no icon, no text, isolated on plain black background,
orthographic, 9-slice friendly, 256x320
```

### 3.6 连接线 / 箭头 / 能量流

```
game UI connector, horizontal glowing cyan energy line with a sharp arrowhead and small floating particles,
dark slate groove behind it, isolated on plain black background, no text, 512x128
```

### 3.7 三选一卡牌底

```
game UI card frame, 320x440, dark slate stone, thin bronze border, 4px amber top edge,
empty large icon area in the upper 60 percent, empty text area below, no text, no letters,
isolated on plain black background, orthographic, 9-slice friendly
```

### 3.8 按钮（主 / 次）

```
game UI button, dark slate stone with thin bronze border, subtle amber edge glow,
empty center, no text, isolated on plain black background, orthographic, 9-slice friendly, 256x96
```

### 3.9 能耗条（10 段）

```
game UI segmented energy bar, ten separate rectangular segments, dark slate frame,
amber fill on the left four segments, empty remaining six, no text, no numbers,
isolated on plain black background, orthographic, 512x64
```

### 3.10 HP 条 / 经验条

```
game UI health bar frame, long dark slate bar with thin bronze border, empty fill,
no text, isolated on plain black background, orthographic, 9-slice friendly, 512x64
```

```
game UI experience bar, thin dark slate bar with subtle cyan edge, empty fill,
no text, isolated on plain black background, orthographic, 512x32
```

### 3.11 符文库格子 / 滚动条 / 预览面板

```
game UI inventory slot, square dark slate tile with thin bronze border, empty center,
no icon, no text, isolated on plain black background, 128x128
```

```
game UI vertical scrollbar, thin dark slate track with a small bronze handle,
no text, isolated on plain black background, 32x256
```

```
game UI preview panel, wide dark slate panel with thin bronze border, empty center,
no text, no icons, isolated on plain black background, 512x192
```

---

## 四、通用图标（20 个）

统一模板：

```
minimal game UI icon of {concept}, single color silver #C9D4D0, thick 2px outline,
flat vector, centered, no text, no letters, no numbers, isolated on plain black background, 128x128
```

| ID | Concept（替换 `{concept}`） |
|---|---|
| `UI_Icon_Settings` | a gear |
| `UI_Icon_Pause` | two vertical bars |
| `UI_Icon_Play` | a triangle |
| `UI_Icon_Volume` | a speaker with sound waves |
| `UI_Icon_Mute` | a speaker with a slash |
| `UI_Icon_Gamepad` | a game controller |
| `UI_Icon_Keyboard` | a keyboard |
| `UI_Icon_ArrowLeft` | a left arrow |
| `UI_Icon_ArrowRight` | a right arrow |
| `UI_Icon_ArrowUp` | an up arrow |
| `UI_Icon_ArrowDown` | a down arrow |
| `UI_Icon_Close` | an X |
| `UI_Icon_Check` | a check mark |
| `UI_Icon_Warning` | a triangle with an exclamation mark |
| `UI_Icon_Lock` | a padlock |
| `UI_Icon_Info` | a circle with an i |
| `UI_Icon_Restart` | a circular arrow |
| `UI_Icon_Home` | a house |
| `UI_Icon_Eye` | an eye |
| `UI_Icon_Coin` | a coin |

> 通用图标不要用琥珀/青绿：保持银白单色，避免和符文图标抢注意力。

---

## 五、生成后处理清单

每张图生成后必须做：

1. **裁切**：统一画布 512×512（图标）/ 1024×1024（面板），主体居中，安全区 8%。
2. **抠背景**：纯黑/纯灰背景 → 透明；不要保留生成图的圆底/棋盘格。
3. **统一描边**：深色外描边 2px（导出 256 后约 1px）。
4. **统一外发光**：按类别（触发琥珀、效果青绿、修饰银白），6~8px。
5. **灰度测试**：转灰度，主体与刻痕明度差 ≥ 40%。
6. **缩略测试**：缩到 64px 仍能认出符文；32px 能区分三类外轮廓。
7. **导出**：PNG，sRGB，Alpha Is Transparency 开，Mipmap 关；图标 256×256，面板 1024。
8. **9-slice**：面板/按钮/卡牌/槽位在 Unity Sprite Editor 设 Border（16~32px），边缘不要有复杂纹理。
9. **命名**：见下方目录与命名。

### 目录与命名

```
Assets/Art/UI/
├── RuneIcons/   UI_RuneIcon_T1.png ... UI_RuneIcon_M7.png
├── Icons/       UI_Icon_Settings.png ...
├── Frames/      UI_Frame_Slot_Trigger.png / UI_Frame_Slot_Effect.png / UI_Frame_Slot_Modifier.png
├── Panels/      UI_Panel_Main.png / UI_Panel_Preview.png
├── Cards/       UI_Card_Frame.png
├── Bars/        UI_Bar_Health.png / UI_Bar_Exp.png / UI_Bar_Energy.png
├── Connectors/  UI_Connector_Energy.png
└── Fonts/       思源黑体 / Cinzel / JetBrains Mono
```

### Unity 导入设置（UI Sprite）

| 项 | 值 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Mesh Type | Full Rect（9-slice 必须） |
| Max Size | 图标 256 / 面板 1024 |
| Compression | Normal Quality |
| Generate Mip Maps | Off |
| Alpha Is Transparency | On |
| Wrap Mode | Clamp |

---

## 六、与 W3b 的排期

### 现在（你导入/生成 UI 的同时）

我按以下顺序做，不阻塞：

1. **三选一 UI**：先用占位 Sprite 做布局/交互/数据绑定；卡牌底/按钮到位后替换。
2. **HUD**：HP/经验/计时/波次/链缩略；先用纯色 + 边框，图形素材到位后替换。
3. **波次 + 经验 + 等级**：这是 W3b 的核心逻辑，不依赖美术。
4. **链编辑器静态原型**：等符文图标到位再做，避免用假图标调布局。

### 建议你现在的导入顺序

| 顺序 | 内容 | 原因 |
|---|---|---|
| 1 | 面板、按钮、卡牌底、槽位边框、HP/经验/能耗条 | 三选一 + HUD 立刻能用 |
| 2 | 20 个符文图标（先生成 3 个样本确认风格） | 链编辑器/三选一/卡牌都依赖 |
| 3 | 通用图标 20 个 | 设置/暂停/提示 |
| 4 | 链编辑器整体概念图 | 只做风格参考，不直接切图 |
| 5 | 结算/主菜单/图鉴 | W3c 之后 |

### 没有 AI key 时我能做什么

- **能做**：uGUI 布局、交互、拖拽、手柄导航、数据绑定、主题 token、程序化面板/按钮/边框、动效。
- **不能替你做**：手绘质感、图标绘制、字体文件下载。
- **最稳的方式**：你用 Holopix/商店产出 PNG，我负责导入设置、9-slice、命名、代码接入、截图验收。
- **临时占位**：我用纯色 + 几何符号保证 UI 能跑；正式素材到位后只换 Sprite，不改布局与代码。

---

## 七、最后提醒

- **先出 3 个符文图标样本**（T1/E1/M1），不要一次生成 20 个；风格不一致返工成本很高。
- **灰度测试**比“好看”更重要：20 个图标在灰度下必须能区分。
- **AI 生成的文字一律不要**：按钮/卡牌文字用 Unity 排，字体用思源黑体。
- **链编辑器整体图不要直接切图**：它只适合做 mood board；真正要生成的是面板/槽位/连接线/按钮。
- 生成后把文件放进 `Assets/Art/UI/` 对应目录，告诉我，我做导入设置、9-slice 和 UI 接入。
