# 《符文余烬》VFX 资源清单 v1

> 更新：2026-10-30
> 用途：按这份清单采购 / 生成 / 验收 VFX，不再临时“缺什么补什么”
> 关联：`Docs/美术风格与资源清单_v1.md`、`Docs/UI与图标设计规范_v1.md`、`Docs/代码导读_v2.md`
> 结论先说：原清单 E 节只有 10 条“类别级”条目，**不完整**。本文件拆到“具体战斗事件 + 可读性”层级，约 90 个 ID，P0 约 40 个。

---

## 〇、先回答“原清单完整吗”

原清单的 E 节覆盖了这些大类：

- Muzzle / Projectile / Hit / Death / Explosion / Aura / Dash / LevelUp / Pickup / Boss

作为骨架是够的，但真正制作时会发现缺：

1. **每个效果原语的专属 VFX**（E1 直射、E2 散射、E3 环绕、E4 地雷、E5 冲刺斩、E6 光束、E7 召唤）
2. **每个修饰的专属反馈**（M1 分裂、M2 弹射、M3 穿透、M4 燃烧、M5 冰冻、M6 爆炸、M7 追踪）
3. **敌人攻击前摇/预警**（近战抬手、远程瞄准线、自爆引信）
4. **状态特效**（燃烧 loop、冰冻、护盾、减速、加速、破盾）
5. **玩家动作**（冲刺残影、受击、死亡、施法蓄力）
6. **循环奖励**（经验掉落、磁吸、升级、选卡、装配）
7. **UI/屏幕反馈**（低血、屏幕闪、错误、结算、Boss 血条）
8. **环境氛围**（余烬、雾、尘、萤火、边界雾）
9. **精英词缀视觉**（护盾/突进的识别）
10. **Boss 技能预警**（地面圈、扇形、直线、阶段转换）

所以：**原清单是目录，不是清单。** 下面这版才是可执行的制作表。

---

## 一、风格总纲（VFX 也要统一）

### 1.1 一句话风格

**手绘低多边形 + 暗调环境 + 发光符文法术。**  
VFX 是全画面唯一允许高饱和 + Bloom 的来源；环境一律压暗、去饱和。

### 1.2 色彩脚本

| 用途 | 主色 | 规则 |
|---|---|---|
| 玩家法术 | 青绿 / 青蓝（160~190°） | 只允许玩家弹道使用青绿 |
| 玩家暴击/稀有 | 金（40~50°） | 少量，作为“高价值”提示 |
| 敌人弹道 | 品红 / 橙红（320~350° / 10~25°） | 与玩家色相 + 形状双分离 |
| 危险/爆炸 | 橙红 + 白心 | 白心只持续 2~3 帧 |
| 治疗/护盾 | 青白 / 淡金 | 不用绿色（避免和毒混淆） |
| 环境 | 灰绿、土黄、冷蓝 | 饱和度 ≤ 30% |

**铁律**：只有“会伤害你的”和“你能捡的”允许高饱和 + Bloom。  
玩家弹幕与敌人弹幕必须**色相分离 + 形状分离**（例如玩家是细长针，敌人是圆球）。

### 1.3 形状语言

- 触发器：时钟、眼睛、闪电、裂纹
- 效果：弹道、光束、环绕、地面法阵
- 修饰：叠层、箭头、分叉、碎裂
- 危险预警：地面圈 / 扇形 / 直线，**必须比实际伤害早 0.4~0.8 秒出现**
- 命中：中心亮点 + 向外 3~6 根短射线，不要大面积白屏

### 1.4 可读性优先级

在 8m 俯视、300 敌人、500 弹幕下：

1. 先看剪影（形状）
2. 再看色相（敌我）
3. 最后看细节（纹理）

任何在灰度图里分不清的 VFX，都要重做。

---

## 二、技术规格（先定预算，再让美术做）

| 项 | 规范 |
|---|---|
| 渲染管线 | URP 17 / Unity 6 |
| 粒子系统 | **Particle System 优先**；只有复杂 GPU 粒子才用 VFX Graph |
| Shader | Shader Graph Unlit / Additive / Alpha Blend；不写透明阴影 |
| 纹理 | 单帧 256×256 或 512×512；Flipbook 4×4 / 8×8；图集 1024 |
| 材质 | `MAT_VFX_Additive`、`MAT_VFX_Alpha`、`MAT_VFX_Dissolve` 三个母材质 |
| 粒子预算 | 普通命中 ≤ 40；爆炸 ≤ 150；Boss ≤ 400；同屏总活粒子 ≤ 2000 |
| Overdraw | 全屏 ≤ 30%；禁止全屏白闪超过 2 帧 |
| 生命周期 | 命中 0.15~0.4s；爆炸 0.4~0.8s；环境 loop 常驻 |
| 面数 | 单特效 ≤ 200 面；拖尾 ≤ 2 个面片 |
| 排序 | 统一 `Transparent` 队列；避免和 UI/角色穿插 |
| 池化 | 所有战斗 VFX 必须可池化；禁止每次 `Instantiate` |
| 剔除 | 屏幕外不更新；距离 > 30m 降频或关闭 |
| LOD | 重要 VFX（Boss 预警）不 LOD；杂兵命中可 30fps 更新 |
| 音频 | 每个 P0 VFX 预留 1 个 SFX 挂点（AudioSource 或事件） |

### 目录与命名

```
Assets/Art/VFX/
├── Source/              # 源工程/工程文件，不入运行时
├── Textures/            # PNG/TGA/Flipbook
├── Materials/           # MAT_VFX_*
├── Prefabs/             # VFX_<Event>_<Name>_01.prefab
└── README.md
```

命名：

```
VFX_<Event>_<Variant>_<Color?>_<Index>
例：VFX_Hit_Physical_Cyan_01
    VFX_Muzzle_Spread_Gold_01
    VFX_Enemy_Telegraph_Circle_01
```

文件名必须和数据表/代码事件一一对应，禁止 `NewParticleSystem(3)`。

---

## 三、完整 VFX 清单

优先级：

- **P0**：W3c~W4 必须有，缺了会影响打击感/可读性
- **P1**：W5 前补齐，支撑 20 符文与三种敌人
- **P2**：上架/扩展期

### A. 命中反馈（P0，8 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Hit_Physical_Cyan_01` | 物理命中：中心亮点 + 4 根短射线 | 0.18s，≤24 粒子 | E1/E2 普通命中 |
| `VFX_Hit_Physical_Gold_01` | 暴击命中：更大亮点 + 金色碎屑 | 0.25s，≤32 粒子 | 暴击/稀有 |
| `VFX_Hit_Explosion_01` | 爆炸命中：白心 + 橙红环 + 尘 | 0.35s，≤80 粒子 | M6/地雷 |
| `VFX_Hit_Burn_01` | 燃烧命中：橙色火舌 + 余烬 | 0.3s，≤40 粒子 | M4 |
| `VFX_Hit_Freeze_01` | 冰冻命中：青白冰晶 + 碎屑 | 0.3s，≤32 粒子 | M5 |
| `VFX_Hit_Shield_01` | 护盾命中：六边形/球形波纹 | 0.2s，≤24 粒子 | 护盾精英 |
| `VFX_Hit_ShieldBreak_01` | 破盾：碎片 + 白闪 + 冲击环 | 0.45s，≤100 粒子 | 破盾瞬间 |
| `VFX_Hit_Player_01` | 玩家受击：红色边缘闪 + 少量火星 | 0.2s，UI/世界各一层 | 玩家受伤 |

> 命中 VFX 是“打击感”的一半；顿帧/震屏已经在代码里，VFX 只负责“点”。

### B. 发射/枪口（P0~P1，7 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Muzzle_Direct_Cyan_01` | 法杖/手心聚能 + 短促闪光 | 0.15s | E1 直射 |
| `VFX_Muzzle_Spread_Cyan_01` | 扇形三连闪光 | 0.2s | E2 散射 |
| `VFX_Muzzle_Orbit_Cyan_01` | 身体周围符文环展开 | 0.35s | E3 环绕 |
| `VFX_Muzzle_Mine_Cyan_01` | 脚下法阵 + 地雷落地 | 0.3s | E4 地雷 |
| `VFX_Muzzle_DashSlash_Cyan_01` | 冲刺起手刀光 | 0.2s | E5 冲刺斩 |
| `VFX_Muzzle_Beam_Gold_01` | 光束蓄力 + 发射 | 0.3s | E6 光束（P1） |
| `VFX_Muzzle_Summon_Cyan_01` | 召唤法阵 + 上升粒子 | 0.5s | E7 召唤（P1） |

### C. 弹道/拖尾（P0，8 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Projectile_Direct_Cyan_01` | 细长针状弹 + 1 条 Trail | 单面片 + Trail | E1 |
| `VFX_Projectile_Spread_Cyan_01` | 小弹丸 + 短 Trail | 0.25s | E2 |
| `VFX_Projectile_Beam_Gold_01` | 持续光束（面片 + 噪声滚动） | loop | E6 |
| `VFX_Projectile_Orbit_Cyan_01` | 环绕球 + 拖尾 | loop | E3 |
| `VFX_Projectile_Mine_Cyan_01` | 地雷本体 + 闪烁指示灯 | loop | E4 |
| `VFX_Projectile_Homing_Gold_01` | 追踪弹 + 弯曲 Trail | 0.5s | M7 |
| `VFX_Projectile_Enemy_Magenta_01` | 敌方圆球 + 品红 Trail | 0.5s | 远程怪 |
| `VFX_Projectile_Enemy_Orange_01` | 敌方自爆/火球 | 0.4s | 自爆怪/精英 |

> Trail 用 1 个面片 + 渐隐，禁止每个弹丸 20 段网格。

### D. 爆炸/区域（P0，7 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Explosion_Small_01` | 小爆炸：白心 + 橙环 + 尘 | 0.35s | M6 小范围 |
| `VFX_Explosion_M6_01` | M6 中爆炸：双环 + 碎片 | 0.6s | M6 |
| `VFX_Mine_Trigger_01` | 地雷触发：地面裂纹 + 上冲 | 0.4s | E4 |
| `VFX_Bomber_Fuse_01` | 自爆怪引信：红闪加速 | loop 1.2s | 自爆怪 |
| `VFX_Bomber_Explode_01` | 自爆怪爆炸：大环 + 烟 | 0.7s | 自爆怪 |
| `VFX_Shockwave_01` | 无伤害冲击波（可复用） | 0.4s | 击退/阶段转换 |
| `VFX_DamageArea_Ring_01` | 范围伤害警示圈（敌我通用） | 0.3s | T6/Boss |

### E. 状态/持续效果（P0~P1，8 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Status_Burn_Loop_01` | 身上橙色火苗 + 余烬 | loop，≤30 粒子 | M4 |
| `VFX_Status_Freeze_Apply_01` | 冰晶包裹 + 白闪 | 0.4s | M5 |
| `VFX_Status_Freeze_Break_01` | 冰壳碎裂 | 0.4s | 破冰 |
| `VFX_Status_Shield_Loop_01` | 半透明球壳 + 六边形纹 | loop | 护盾精英 |
| `VFX_Status_Slow_01` | 脚下青色泥/霜环 | loop | 减速 |
| `VFX_Status_Haste_01` | 红色残影 + 速度线 | loop | 突进精英 |
| `VFX_Status_Split_01` | 分裂瞬间的“分叉”闪光 | 0.2s | M1 |
| `VFX_Status_Homing_Trail_01` | 追踪弹的弯曲尾迹 | 0.3s | M7 |

### F. 玩家动作（P0，7 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Dash_Trail_Cyan_01` | 冲刺拖尾（面片） | 0.25s | 冲刺 |
| `VFX_Dash_Afterimage_01` | 2~3 帧角色残影 | 0.2s | 冲刺 |
| `VFX_Dash_Impact_01` | 冲刺落点小尘环 | 0.3s | 冲刺结束 |
| `VFX_Player_Hit_01` | 受击红闪 + 火星 | 0.2s | 玩家受伤 |
| `VFX_Player_Death_01` | 死亡溶解 + 灰烬 | 1.0s | 玩家死亡 |
| `VFX_Player_Cast_Charge_01` | 施法蓄力（手部光点） | 0.3s | 施法 |
| `VFX_Player_Heal_01` | 治疗/复活上升光粒 | 0.8s | 复活/拾取 |

### G. 敌人前摇/死亡（P0~P1，12 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Enemy_Melee_Windup_01` | 近战抬手：武器高亮 + 地面小圈 | 0.5s | Troll 前摇 |
| `VFX_Enemy_Melee_Impact_01` | 近战落地：尘 + 冲击环 | 0.3s | Troll 命中 |
| `VFX_Enemy_Ranged_Aim_01` | 远程瞄准线（细红线） | loop 0.8s | 远程怪 |
| `VFX_Enemy_Ranged_Muzzle_01` | 远程发射口闪光 | 0.2s | 远程怪 |
| `VFX_Enemy_Bomber_Fuse_01` | 自爆引信（红闪加速） | loop 1.2s | 自爆怪 |
| `VFX_Enemy_Bomber_Explode_01` | 自爆爆炸 | 0.7s | 自爆怪 |
| `VFX_Enemy_Elite_Shield_01` | 护盾精英外观 | loop | 精英 |
| `VFX_Enemy_Elite_Haste_01` | 突进精英红色拖尾 | loop | 精英 |
| `VFX_Enemy_Death_Dissolve_01` | 通用死亡溶解 | 0.8s | 杂兵 |
| `VFX_Enemy_Death_Ash_01` | 死亡灰烬上飘 | 1.0s | 杂兵 |
| `VFX_Enemy_Death_Elite_01` | 精英死亡：更大灰烬 + 金光 | 1.2s | 精英 |
| `VFX_Enemy_Spawn_01` | 生成：地面符文圈 + 上升 | 0.6s | 波次生成 |

### H. 循环/奖励/UI（P0~P1，9 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_XP_Pickup_01` | 经验球/碎片 + 呼吸光 | loop | 击杀掉落 |
| `VFX_XP_Magnet_01` | 磁吸轨迹 | 0.3s | 拾取范围 |
| `VFX_XP_Collect_01` | 吸入 HUD 的小闪光 | 0.2s | 拾取 |
| `VFX_LevelUp_Ring_01` | 地面符文环扩散 | 0.6s | 升级 |
| `VFX_LevelUp_Column_01` | 角色光柱 | 0.8s | 升级 |
| `VFX_Card_Reveal_01` | 三选一卡牌翻出 | 0.25s | UI |
| `VFX_Card_Select_01` | 选卡确认：金光/符文闪 | 0.3s | UI |
| `VFX_Rune_Equip_01` | 符文装入槽位 | 0.2s | 链编辑器 |
| `VFX_Chain_Energy_01` | 链连接线能量流动 | loop | 链编辑器/HUD |

### I. 屏幕/UI 反馈（P0~P1，8 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_UI_DamageNumber` | 伤害数字（已有 `TextMesh` 版） | 0.6s | 命中 |
| `VFX_UI_LowHP_Vignette` | 低血红屏（已有） | loop | 低血 |
| `VFX_UI_ScreenFlash_01` | 全屏短闪（受击/爆炸） | ≤0.1s | 重击 |
| `VFX_UI_Error_01` | 装配错误红闪 | 0.2s | 链编辑器 |
| `VFX_UI_Pause_01` | 暂停：去色 + 暗角 | 0.2s | 暂停 |
| `VFX_UI_Settlement_01` | 结算面板淡入 | 0.3s | 结算 |
| `VFX_UI_BossBar_01` | Boss 血条出现/阶段 | 0.4s | Boss |
| `VFX_UI_Cursor_01` | 瞄准光标（青/金） | loop | 瞄准 |

### J. 环境/氛围（P1，8 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Env_Embers_01` | 漂浮余烬 | loop，≤100 粒子 | 全场景 |
| `VFX_Env_Fog_01` | 地面薄雾 | loop，面片 | 边界/低地 |
| `VFX_Env_Dust_01` | 脚步/落点尘 | 0.3s | 移动 |
| `VFX_Env_Fireflies_01` | 萤火（森林） | loop | 森林群系 |
| `VFX_Env_RuinDust_01` | 废墟落灰 | loop | 村庄/城市 |
| `VFX_Env_BoundaryMist_01` | 边界迷雾墙 | loop | 竞技场边界 |
| `VFX_Env_Campfire_01` | 篝火/火盆 | loop | 休整点 |
| `VFX_Env_Weather_01` | 雨/灰烬天气（可选） | loop | 氛围 |

### K. Boss（P1~P2，6 个）

| ID | 资源 | 规格 | 用途 |
|---|---|---|---|
| `VFX_Boss_Entrance_01` | 出场：地面裂开 + 光柱 | 1.5s | Boss 出场 |
| `VFX_Boss_Phase_01` | 阶段转换：全屏波纹 + 颜色变化 | 1.0s | Boss 阶段 |
| `VFX_Boss_Slam_01` | 砸地：大冲击环 + 尘 | 0.8s | Boss 技能 |
| `VFX_Boss_ProjectileRain_01` | 弹幕雨预警圈 | 1.0s | Boss 技能 |
| `VFX_Boss_Beam_01` | 直线激光预警 + 发射 | 1.2s | Boss 技能 |
| `VFX_Boss_Death_01` | 死亡：多次爆炸 + 灰烬 | 2.0s | Boss 死亡 |

### L. 群系变体（P2）

- 森林：青绿余烬 + 萤火
- 村庄：琥珀火光 + 土尘
- 城市：冷蓝雾 + 紫罗兰符文
- 只换主色/贴图，不重做粒子结构，保证“一个世界”。

---

## 四、符文 → VFX 映射

| 符文 | 发射/弹道 | 命中/持续 | 备注 |
|---|---|---|---|
| T1 定时脉冲 | `Muzzle_Direct` | `Hit_Physical` | 默认链 |
| T2 击杀回响 | `Muzzle_Direct` + 击杀环 | `Hit_Physical` | 击杀瞬间要亮 |
| T3 命中触发 | `Muzzle_Direct` | `Hit_Physical` | 高频，控制粒子 |
| T4 冲刺触发 | `Muzzle_DashSlash` | `Hit_Physical` | 冲刺拖尾 |
| T5 受伤触发 | `Muzzle_Direct` + 红闪 | `Hit_Physical` | 受伤反馈 |
| T6 光环 | `Aura_Orbit` / `DamageArea_Ring` | `Hit_Burn` 可选 | 常驻，压低 overdraw |
| E1 直射 | `Muzzle_Direct` | `Projectile_Direct` | 青 |
| E2 散射 | `Muzzle_Spread` | `Projectile_Spread` | 青 |
| E3 环绕 | `Muzzle_Orbit` | `Projectile_Orbit` | 青 |
| E4 地雷 | `Muzzle_Mine` | `Projectile_Mine` / `Mine_Trigger` | 青 |
| E5 冲刺斩 | `Muzzle_DashSlash` | `Hit_Physical` | 青 |
| E6 光束 | `Muzzle_Beam` | `Projectile_Beam` | 金 |
| E7 召唤 | `Muzzle_Summon` | 召唤物 | P1 |
| M1 分裂 | `Status_Split` | — | 分叉闪光 |
| M2 弹射 | 弹射连线 | `Hit_Physical` | 需要连线 VFX |
| M3 穿透 | 穿透轨迹 | `Hit_Physical` | 同一命中复用 |
| M4 燃烧 | — | `Hit_Burn` / `Status_Burn` | 橙 |
| M5 冰冻 | — | `Hit_Freeze` / `Status_Freeze` | 青白 |
| M6 爆炸 | — | `Explosion_M6` | 橙红 |
| M7 追踪 | `Projectile_Homing` | `Hit_Physical` | 金 |

> 当前代码只实现了 E1–E5 / M1–M7；E6/E7 的 VFX 可以先做占位，等逻辑接入。

---

## 五、分阶段制作顺序

### W3c（现在）

先做 8 个，足够把“命中—死亡—升级”闭合：

1. `VFX_Hit_Physical_Cyan_01`
2. `VFX_Hit_Explosion_01`
3. `VFX_Muzzle_Direct_Cyan_01`
4. `VFX_Projectile_Direct_Cyan_01`
5. `VFX_Enemy_Death_Dissolve_01`
6. `VFX_Player_Hit_01`
7. `VFX_Dash_Trail_Cyan_01`
8. `VFX_LevelUp_Ring_01`

### W4（三种敌人 + 精英 + Boss）

1. 近战前摇/命中
2. 远程瞄准线/发射
3. 自爆引信/爆炸
4. 护盾/突进精英
5. 生成/死亡/精英死亡
6. Boss 预警/阶段/技能

### W5（20 符文 + 性能）

1. 状态类（燃烧/冰冻/护盾/减速/加速）
2. 修饰类（分裂/弹射/穿透/追踪）
3. 经验拾取/磁吸/选卡/装配
4. 环境氛围
5. 压测 overdraw/粒子数

---

## 六、Holopix / AI 生成提示词

> 只生成**贴图/Flipbook/单帧**，不要让 AI 直接生成“完整特效 prefab”。粒子系统在 Unity 里搭。

### 6.1 命中闪光 Flipbook

```
A 4x4 flipbook sprite sheet of a stylized hand-painted impact flash,
cyan-white core, short radial sparks, dark fantasy game VFX,
transparent background, no text, no watermark,
consistent shape across frames, 512x512 per frame, 16 frames
```

负面：

```
realistic photo, lens flare, text, watermark, background scene, character,
full screen white, excessive bloom, blurry, low contrast
```

### 6.2 爆炸 Flipbook

```
A 4x4 flipbook of a stylized low-poly explosion,
white hot core, orange-red ring, dark embers and dust,
hand-painted game VFX, transparent background, no text,
consistent silhouette, 512x512 per frame, 16 frames
```

### 6.3 枪口/法阵

```
A small stylized magic muzzle flash, cyan rune circle and short energy burst,
top-down game VFX, transparent background, centered, no text,
hand-painted, high contrast, 512x512
```

### 6.4 弹道拖尾

```
A seamless vertical trail texture for a cyan magic projectile,
soft gradient, wispy edges, transparent background, tileable,
hand-painted, no text, 256x512
```

### 6.5 灰烬/余烬

```
A seamless particle texture of a single glowing ember,
orange-yellow core, soft falloff, transparent background,
hand-painted, no text, 128x128
```

### 6.6 地面符文环

```
A top-down stylized rune circle, cyan glowing lines, weathered stone cracks,
transparent background, centered, symmetrical, no text, 1024x1024
```

### 6.7 敌人前摇预警圈

```
A top-down danger telegraph circle, thin bright orange-red ring,
dark translucent fill, clear edge, game VFX, transparent background,
no text, 512x512
```

### 6.8 通用负面词

```
photorealistic, cinematic lens flare, text, letters, watermark, logo,
background environment, character, UI frame, excessive glow, white screen,
low resolution, jpeg artifacts
```

> 生成后统一：去背景 → 压到 256/512 → 检查灰度剪影 → 放进 `Assets/Art/VFX/Textures/`。

---

## 七、代码接入点（做完 VFX 往哪挂）

| 事件 | 代码位置 | 挂什么 |
|---|---|---|
| 弹丸发射 | `Projectile.Init` / `RuneCaster.CastInternal` | Muzzle + Trail |
| 弹丸命中 | `Projectile.Hit` | Hit_* |
| 范围伤害 | `UnityRuneWorld.DamageArea` | Explosion / Ring |
| 敌人死亡 | `EnemyActor.OnDied` | Death_* |
| 敌人攻击前摇 | `EnemyBrain.Enter(State.AttackWindup)` | Melee_Windup |
| 敌人攻击命中 | `EnemyBrain.TryDealDamage` | Melee_Impact |
| 玩家受伤/死亡 | `PlayerHealth.ApplyDamage` | Player_Hit / Death |
| 冲刺 | `PlayerController` 冲刺开始/结束 | Dash_Trail / Impact |
| 升级 | `ExperienceSystem.LeveledUp` | LevelUp_Ring/Column |
| 选卡 | `UpgradeScreen.Show/Select` | Card_Reveal/Select |
| 拾取经验 | `ExperienceSystem` / 掉落物 | XP_Pickup/Magnet |
| T6 光环 | `TriggerRouter` | Aura/Ring |
| 地雷 | `Mine` | Fuse/Trigger |
| 环绕球 | `OrbitingOrbs` | Orbit Trail |
| 结算 | `SettlementScreen` | UI_Settlement |

**原则**：VFX 只做表现，不改伤害/时间/触发；伤害逻辑仍然由 `DamageEvent` 和 `ITime` 负责。

---

## 八、验收清单

- [ ] 8m 俯视下能一眼分清敌我弹道（色相 + 形状）
- [ ] 灰度截图仍能分清命中/爆炸/治疗
- [ ] 同屏 300 敌人 + 500 弹幕时，粒子总数 ≤ 2000
- [ ] 单次命中 ≤ 40 粒子；爆炸 ≤ 150；Boss ≤ 400
- [ ] 无全屏白闪超过 2 帧
- [ ] 所有战斗 VFX 可池化，无运行时 `Instantiate`
- [ ] 屏幕外/远距离特效降频或关闭
- [ ] 每个 P0 VFX 有对应 SFX 挂点
- [ ] 文件名与 ID 一致，材质使用三个母材质之一
- [ ] 透明排序不与角色/UI 穿插
- [ ] 录制 10 秒战斗片段，确认没有“满屏光污染”

---

## 九、最小可行动作

如果现在只做一件事：

1. 先做 **`VFX_Hit_Physical_Cyan_01` + `VFX_Muzzle_Direct_Cyan_01` + `VFX_Enemy_Death_Dissolve_01`** 三个。
2. 用 `HitFeedback` / `Projectile.Hit` / `EnemyActor.OnDied` 接上。
3. 录 10 秒，确认“打中—死亡—升级”有反馈，再批量做其余。

这比一次做 20 个更能验证风格是否统一。
