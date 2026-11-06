# 《符文余烬》VFX Atlas 方案 v1

> 日期：2026-11-06  
> 范围：`Assets/Art/VFX/Packs/PilotoStudio`  
> 前置：`Docs/VFX材质审计报告.md`

---

## 一、先说结论

当前 **不能安全地自动把 146 个材质合成一张 Atlas**。

原因不是工具不会打包，而是：

1. **`Piloto Studio/UberFXSG` 没有暴露 `_MainTex_ST`**
   - 属性表里只有 `_MainTex`、`_MainTexturePanning` 等
   - 没有 `_MainTex_ST`
   - 只改材质的 Tiling/Offset 不会改变 Shader 里 `_MainTex` 的 UV

2. **31 个材质启用了 Texture Sheet Animation**
   - 这些是翻页/序列帧特效
   - 直接塞进 Atlas 会把整张 Atlas 当成一帧，效果会坏

3. **真正完全重复的材质只有 2 个**
   - 其余材质虽然同 Shader、同尺寸，但贴图/参数不同
   - 只能做 Atlas，不能做“去重”

所以：

```text
可以做的：精确去重 + Atlas 候选审计 + LOD/粒子降级
不能直接做的：不修改 Shader 就把所有材质塞进一张 Atlas
```

---

## 二、当前可 Atlas 的候选规模

按 Shader + 贴图尺寸分组：

| 组 | 数量 | 建议 |
|---|---:|---|
| `UberFXSG` 512×512 | 49 | Atlas 首选 |
| `UberFXSG` 1024×1024 | 29 | 可做 4096 Atlas |
| `UberFXSG` 2048×2048 | 19 | 不建议单张 8192，拆分或压缩 |
| `UberFXSG` 512×256 | 6 | 次选 |
| `UberFXSG` 256×256 | 11 | 次选 |
| `UberFXSG` 64×64 | 10 | 收益低，最后做 |
| `UberFXSG` 1024×512 | 4 | 次选 |
| `UberFXSG` 110×96 | 2 | 不建议 |
| `Hovl` 512×512 | 6 | 独立 Pack，单独处理 |

其中 **31 个材质使用 Texture Sheet Animation**，必须排除在“简单 Atlas”之外。

---

## 三、方案 A：不做 Atlas，先做安全优化（推荐当前阶段）

适合 W4/W5，风险最低：

1. 保留 Pack 原材质
2. 精确去重（已经做了 2 个）
3. 降低 Muzzle/Hit 的子 ParticleSystem
4. 加距离 LOD/屏幕外剔除
5. 用 Profiler 对照

收益：

- 不破坏翻页
- 不改美术
- 能降低同屏 VFX 数和部分 DrawCall
- 对 SetPassCalls 的帮助有限，但可控

---

## 四、方案 B：自定义 Atlas Shader（真正降 SetPassCalls）

这是唯一能显著合并 49 个 512 材质、29 个 1024 材质的路线。

### 步骤 1：复制 Shader Graph

复制：

```text
Assets/Art/VFX/Packs/PilotoStudio/Shaders_Reforged/UberFXSG.shadergraph
```

到项目自有目录：

```text
Assets/Art/VFX/Shaders/UberFXSG_Atlas.shadergraph
```

### 步骤 2：增加 Atlas UV 参数

在 Shader Graph 增加：

```text
_AtlasTiling   Vector2
_AtlasOffset   Vector2
```

在 `_MainTex` 采样前：

```text
UV = UV0 * _AtlasTiling + _AtlasOffset
```

如果材质使用 Texture Sheet Animation，还要把翻页 UV 先算完，再做 Atlas 变换，否则翻页会坏。

### 步骤 3：生成 Atlas

使用 TexturePacker / Photoshop / 自写 Editor 工具：

- 只选同一 Shader、同一尺寸、无翻页的贴图
- 2 的幂尺寸：2048 / 4096
- 四周留 **8px padding**，防止采样串色
- 保持 Alpha
- 关闭 Mipmap 或保留足够 padding
- 设置 Clamp、正确的 sRGB/Linear

建议先做：

```text
UberFXSG 512×512 组 → 4096 Atlas
```

### 步骤 4：每个材质设置 Atlas 区域

每个原材质保留：

```text
_MainTex = Atlas
_AtlasTiling = (1/列数, 1/行数)
_AtlasOffset = (列索引/列数, 行索引/行数)
```

注意 Y 轴方向要和 Unity 的 UV 坐标一致。

### 步骤 5：替换 prefab 引用

- 新建 Atlas 材质
- 用 Editor 工具批量替换 `ParticleSystemRenderer.sharedMaterial`
- 保留原材质资产作为回滚

### 步骤 6：验证

必须检查：

- 粒子是否采样到正确格子
- 翻页特效是否仍然正常
- 边缘是否有串色
- Alpha 是否变硬
- Frame Debugger 的 SetPassCalls 是否下降
- 视觉是否通过

### 风险

- 需要修改 Shader Graph，不是纯资源操作
- 翻页材质要额外处理
- 需要逐效果截图验收
- 不是“一键 Atlas”

---

## 五、方案 C：Texture2DArray（同尺寸、同格式）

适合 512/1024 这种同尺寸组：

1. 把同尺寸贴图打成 `Texture2DArray`
2. Shader 采样时用 `_TextureIndex` 选择层
3. 每个材质设置一个 index

优点：

- 不需要 UV 重排
- 不会串色
- 对同尺寸组很干净

缺点：

- 仍然要改 Shader
- Unity 粒子默认材质不支持数组采样
- 需要确认目标平台支持 Texture2DArray

---

## 六、方案 D：重新制作 Flipbook Atlas

把多个小特效直接烘成一张 flipbook：

- 每个特效占固定网格
- Shader 只做 UV 缩放/偏移
- 最适合 Hit/Muzzle 这种短特效

但这属于重新制作特效，不是整理现有素材。

---

## 七、我的建议

当前阶段建议：

```text
1. 保留“安全去重”结果（2 个）
2. 先做 Profiler 基线
3. 先做距离 LOD 与粒子降级
4. 如果 SetPassCalls 仍是瓶颈，再做方案 B（自定义 Atlas Shader）
```

原因：

- 现在直接做 Atlas 风险高于收益
- 31 个翻页材质需要单独处理
- 49 个 512 材质即使 Atlas 成功，也需要逐效果验收
- W4b 精英/Boss 还没开始，先不要引入 Shader Graph 维护成本

---

## 八、我可以继续做什么

我可以直接做：

- Atlas 候选审计报告
- 贴图分组/尺寸/翻页标记
- Editor 批量替换工具
- Profiler 前后对照工具
- 距离 LOD/粒子降级

需要你确认后再做：

- 自定义 Atlas Shader
- 贴图 Atlas 的实际视觉验收
- 翻页材质的 UV 改造
