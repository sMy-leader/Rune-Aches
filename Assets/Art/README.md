# Assets/Art — 正式美术资产目录

> 规范来源：`Docs/美术风格与资源清单_v1.md`、`Docs/UI与图标设计规范_v1.md`
> 代码灰盒材质在 `Assets/RuneAshes/Art/Blockout/`，不要混进这里。

## 目录

| 目录 | 放什么 | 命名 |
|---|---|---|
| `Incoming/` | 刚下载、还没验收的模型/贴图。先放这里，跑验收后再归位 | 保持原名即可 |
| `Characters/` | 玩家、NPC | `CH_<Name>_<Variant>.fbx` |
| `Enemies/` | 杂兵、精英、Boss | `EN_<Type>_<Name>.fbx` |
| `Environment/Ground/` | 地面模块 | `ENV_Ground_<Name>_01.fbx` |
| `Environment/Props/` | 树、石、断碑、篝火 | `ENV_<Type>_<Name>_01.fbx` |
| `Environment/Skybox/` | 天空盒/HDRI | `ENV_Sky_<Name>.png` |
| `VFX/` | 粒子/Prefab | `VFX_<Event>_<Name>.prefab` |
| `UI/RuneIcons/` | 20 个符文图标 | `UI_RuneIcon_<ID>.png`（ID 必须与 rune.csv 一致） |
| `UI/Icons/` | 通用图标 | `UI_Icon_<Name>.png` |
| `UI/Frames/` | 9-slice 边框/底板 | `UI_Frame_<Name>.png` |
| `UI/Fonts/` | 可商用字体 | 原文件名，记录授权 |
| `Materials/` | 统一材质 | `MAT_<Category>_<Name>.mat` |
| `Textures/` | 共享贴图/图集 | `T_<Category>_<Name>_<Size>.png` |
| `Animations/` | 独立动画 clip | `AN_<Character>_<Action>.anim` |
| `Shaders/` | 自定义 Shader | `RuneAshes/Lit_Stylized` |

## 导入流程

1. 把文件放进 `Incoming/`。
2. Unity 自动套用 `ModelImportPostprocessor`（Scale 1、无压缩、Read/Write 关、贴图 512/1024）。
3. 菜单 `Rune Ashes/Assets/Audit Models` 生成 `Docs/资产盘点报告.md`。
4. 通过验收后，按命名规范移动到对应目录，并在 `Docs/资产台账.md` 登记来源与授权。

## 硬要求

- 1 Unity unit = 1m；玩家高 1.7~1.9m。
- 朝向 +Z；pivot 在脚底中心。
- 玩家 ≤32 bones；杂兵 ≤16 bones 或 VAT。
- 贴图 512/1024，不用 4K，不用资产自带材质。
- 文件名只用英文、数字、下划线；ID 必须与数据表一致。
