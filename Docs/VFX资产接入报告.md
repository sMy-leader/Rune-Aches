# 《符文余烬》VFX 资产接入报告 v1

> 日期：2026-11-05  
> 范围：`Assets/Art/VFX/` 以及新导入的 `光环护盾`、`奖励光柱`  
> 对照：`Docs/VFX资源清单_v1.md`

---

## 一、结论先说

这批 VFX **可以用，但不能全部直接当成品接入**。

- 31 个 prefab 都有 ParticleSystem，**0 个缺失 shader**
- 但 **26 个可见 ParticleSystemRenderer 的 sharedMaterial 为空**
- **11 个 prefab 的 root ParticleSystem 是 loop**，其中一部分是 trail/charge，合理；但 `Hit_Physical_Cyan`、`Shockwave` 这类 one-shot 需要包装或关闭 loop
- 多个 prefab 的 root 只有 0.05s/2s，实际时长由子 ParticleSystem 决定，不能只看 `main.duration`
- 因此建议：**Pack 原 prefab 保持只读，游戏内用 wrapper prefab + 对象池接入**

---

## 二、目录整理结果

已用 `AssetDatabase.MoveAsset` 移动，GUID 保留：

```text
Assets/Art/VFX/
├── Prefabs/
│   ├── Hit/         8
│   ├── Muzzle/      3
│   ├── Projectile/  6
│   ├── Explosion/   4
│   ├── Status/      3
│   ├── Player/      3
│   ├── Enemy/       2
│   ├── LevelUp/     2
│   └── Utility/     0
└── Packs/
    ├── PilotoStudio/
    └── HovlShield/
```

- `Piloto Studio` → `Packs/PilotoStudio`
- `光环护盾` → `Packs/HovlShield`（4.3MB，只有材质/贴图/Shader/脚本，没有可直接用的 prefab）
- `奖励光柱`：未找到实际文件，当前为空/不存在，需要重新导入

---

## 三、现有 prefab 盘点

### 命中 Hit

```text
VFX_Hit_Physical_Cyan_01
VFX_Hit_Physical_Gold_01
VFX_Hit_Explosion_01
VFX_Hit_Burn_01
VFX_Hit_Freeze_01
VFX_Hit_Shield_01
VFX_Hit_ShieldBreak_01
VFX_Hit_Player_01
```

### 发射 Muzzle

```text
VFX_Muzzle_Direct_Cyan_01
VFX_Muzzle_DashSlash_Cyan_01
VFX_Muzzle_Beam_Gold_01
```

### 弹道 Projectile

```text
VFX_Projectile_Direct_Cyan_01
VFX_Projectile_Spread_Cyan_01_Burn
VFX_Projectile_Spread_Cyan_02_Freeze
VFX_Projectile_Homing_Gold_01
VFX_Projectile_Enemy_Magenta_01
VFX_Projectile_Enemy_Orange_01
```

### 爆炸 Explosion

```text
VFX_Explosion_Small_01
VFX_Explosion_M6_01
VFX_Mine_Trigger_01
VFX_Shockwave_01
```

### 状态 Status

```text
VFX_Status_Burn_Loop_01
VFX_Status_Freeze_Apply_01
VFX_Status_Shield_Loop_01
```

### 玩家 Player

```text
VFX_Dash_Trail_Cyan_01
VFX_Player_Cast_Charge_01
VFX_Player_Heal_01
```

### 敌人 Enemy

```text
VFX_Bomber_Explode_01
VFX_Enemy_Spawn_0
```

### 升级 LevelUp

```text
VFX_LevelUp_Ring_01
VFX_LevelUp_Column_01
```

---

## 四、技术问题

### 1. 空材质

审计结果：

- 总共 53 个 ParticleSystemRenderer 的 `sharedMaterial == null`
- 其中 **26 个是 enabled 且 active 的可见 renderer**
- 涉及 18 个 prefab

重点需要检查：

```text
VFX_Bomber_Explode_01
VFX_Explosion_M6_01
VFX_Explosion_Small_01
VFX_Mine_Trigger_01
VFX_Shockwave_01
VFX_Hit_Physical_Gold_01
VFX_Hit_ShieldBreak_01
VFX_Muzzle_Beam_Gold_01
VFX_Muzzle_DashSlash_Cyan_01
VFX_Dash_Trail_Cyan_01
VFX_Projectile_Direct_Cyan_01
VFX_Projectile_Enemy_Magenta_01
VFX_Projectile_Homing_Gold_01
VFX_Projectile_Spread_Cyan_01_Burn
VFX_Projectile_Spread_Cyan_02_Freeze
VFX_Status_Burn_Loop_01
VFX_Status_Freeze_Apply_01
VFX_Status_Shield_Loop_01
```

判断：

- Pack 里有些 root 是“控制器层”，没有材质是正常的
- 但上述 prefab 中存在可见的空材质 renderer，直接播放会少粒子/完全不可见
- 建议先在 Prefab Mode 里逐个确认；不要直接批量塞一个默认材质，会改变美术效果

### 2. loop 状态

root ParticleSystem 为 loop 的 11 个：

```text
VFX_Shockwave_01
VFX_Hit_Physical_Cyan_01
VFX_Muzzle_Direct_Cyan_01
VFX_Dash_Trail_Cyan_01
VFX_Player_Cast_Charge_01
VFX_Player_Heal_01
VFX_Projectile_Direct_Cyan_01
VFX_Projectile_Enemy_Magenta_01
VFX_Projectile_Homing_Gold_01
VFX_Projectile_Spread_Cyan_01_Burn
VFX_Projectile_Spread_Cyan_02_Freeze
```

判断：

- trail / charge / projectile 这类 loop 是合理的
- `VFX_Hit_Physical_Cyan_01` 和 `VFX_Shockwave_01` 是 one-shot 语义，接入时要：
  - 关闭 `main.loop`
  - 设置 `Stop Action = Disable` 或 `Destroy`
  - 由 wrapper 在 `max(child duration)` 后回收

### 3. root duration 不能代表总时长

例如：

```text
VFX_Explosion_M6_01 main.duration = 0.05s
VFX_Hit_Explosion_01 main.duration = 0.05s
```

实际粒子在子系统中，接入时应该：

```text
取所有子 ParticleSystem 的最大 duration
或由 wrapper 显式配置 lifetime
```

### 4. 材质/Shader 依赖

- 135 个材质
- 158 张贴图
- 没有缺失 shader
- 但 `HovlShield` 只有源文件，没有 prefab；护盾表现优先用 `PilotoStudio` 的 `VFX_Hit_Shield_01` / `VFX_Status_Shield_Loop_01`

---

## 五、对照清单还缺什么

### P0 已有

- Hit：8/8
- LevelUp：2/2
- Projectile：6/8（Spread 用 Burn/Freeze 两个变体替代）

### P0 缺口

| 类别 | 缺失 |
|---|---|
| Muzzle | `Spread`、`Orbit`、`Mine`、`Summon` |
| Projectile | `Beam`、`Orbit`、`Mine` |
| Explosion | `Bomber_Fuse`、`DamageArea_Ring` |
| Status | `Freeze_Break`、`Slow`、`Haste`、`Split`、`Homing_Trail` |
| Player | `Dash_Afterimage`、`Dash_Impact`、`Player_Death` |
| Enemy | `Melee_Windup`、`Melee_Impact`、`Ranged_Aim`、`Ranged_Muzzle`、`Bomber_Fuse`、`Elite_Shield`、`Elite_Haste`、`Death_Dissolve`、`Death_Ash`、`Death_Elite` |
| UI | 8 个全缺 |
| Environment / Boss | 暂缺 |

### 结论

现在够做 **W4a 战斗反馈**：

```text
普通命中、爆炸、燃烧、冰冻、远程弹、自爆、冲刺、升级
```

还不够做：

```text
护盾精英、突进精英、Boss、死亡溶解、UI 反馈
```

---

## 六、建议的接入方式

### 1. 不直接修改 Pack prefab

在 `Assets/Art/VFX/Gameplay/` 下建立 wrapper：

```text
VFX_Gameplay_Hit_Physical.prefab
VFX_Gameplay_Hit_Explosion.prefab
VFX_Gameplay_Muzzle_Direct.prefab
VFX_Gameplay_Projectile_Direct.prefab
VFX_Gameplay_Player_Hit.prefab
VFX_Gameplay_Dash_Trail.prefab
VFX_Gameplay_LevelUp_Ring.prefab
```

wrapper 负责：

- 关闭/保留 loop
- 设置 `Stop Action`
- 统一缩放
- 统一生命周期
- 挂 AudioSource 预留点

### 2. 加一个 VFX 对象池

建议新增：

```text
VfxService
├── Play(prefab, position, rotation)
├── PlayAttached(prefab, parent)
└── 自动按 max duration 回收
```

先不要每次命中 `Instantiate/Destroy`，否则 300 敌人会 GC 抖动。

### 3. 第一批接入点

按 `Docs/VFX资源清单_v1.md` 第七节：

| 代码点 | VFX |
|---|---|
| `Projectile` 命中 | `VFX_Hit_Physical_Cyan_01` |
| `Projectile` 发射 | `VFX_Muzzle_Direct_Cyan_01` |
| 弹丸本体 | `VFX_Projectile_Direct_Cyan_01` |
| M6 爆炸 | `VFX_Explosion_M6_01` / `VFX_Hit_Explosion_01` |
| 玩家受伤 | `VFX_Hit_Player_01` |
| 冲刺 | `VFX_Dash_Trail_Cyan_01` |
| 升级 | `VFX_LevelUp_Ring_01` |
| 自爆怪 | `VFX_Bomber_Explode_01` |
| 远程怪弹丸 | `VFX_Projectile_Enemy_Magenta_01` |

---

## 七、当前建议

1. **先不要提交 138MB VFX Pack 到 Git/LFS**
2. 先在 Prefab Mode 里逐个确认 18 个有空材质的 prefab
3. 先接 8 个 P0：Hit_Physical / Hit_Explosion / Muzzle_Direct / Projectile_Direct / Hit_Player / Dash_Trail / LevelUp_Ring / Bomber_Explode
4. 接入后再根据实际画面调整缩放、发光强度和粒子数

这样这批素材能马上服务 W4a/W4b，而不会被 Pack 的原始配置拖住。
