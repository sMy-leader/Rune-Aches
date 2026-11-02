// ─────────────────────────────────────────────────────────────────────────────
// 【职责】把 Assets/Art/UI 的散图整理成可用的 UI 资源：
//        1) 统一扩展名/导入设置；2) 给散图补 prefab；3) 生成运行时 Sprite 目录。
// 【菜单】Rune Ashes/UI/Normalize Art & Build Prefabs
// 【幂等】已存在的 prefab 不覆盖；目录资产原地更新。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RuneAshes.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.EditorTools
{
    public static class UiArtPipeline
    {
        private const string UiRoot = "Assets/Art/UI";
        private const string CatalogPath = "Assets/RuneAshes/UI/Resources/UiSpriteCatalog.asset";
        private const string GeneratedSpritesRoot = "Assets/Art/UI/GeneratedSprites";

        [MenuItem("Rune Ashes/UI/Normalize Art & Build Prefabs")]
        public static void RunAll()
        {
            NormalizeTextures();
            BuildPrefabs();
            BuildCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiArtPipeline] 完成：导入设置、缺失 prefab、Sprite Catalog 已更新。");
        }

        private static void NormalizeTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot });
            var renamed = 0;
            var changed = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                path = NormalizeExtension(path, ref renamed);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (importer == null || texture == null)
                {
                    continue;
                }

                var name = Path.GetFileName(path);
                var isIcon = path.Contains("/Icons/") || path.Contains("/RuneIcons/");

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteMeshType = isIcon ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
                settings.spritePixelsPerUnit = 100f;
                settings.npotScale = TextureImporterNPOTScale.None;
                importer.SetTextureSettings(settings);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = isIcon ? 256 : 2048;
                importer.textureCompression = isIcon
                    ? TextureImporterCompression.CompressedHQ
                    : TextureImporterCompression.Compressed;
                importer.spriteBorder = BorderFor(name, texture.width, texture.height);

                var platform = importer.GetDefaultPlatformTextureSettings();
                platform.maxTextureSize = isIcon ? 256 : 2048;
                importer.SetPlatformTextureSettings(platform);

                importer.SaveAndReimport();

                // Unity 6.3 对部分 NPOT 图片不会把 importer 的 npotScale/mipmap 写进 meta；
                // 这里补一次文本修正，再强制同步导入，否则 Sprite 子资产不会生成。
                PatchMeta(path);
                AssetDatabase.ImportAsset(path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                changed++;
            }

            Debug.Log("[UiArtPipeline] 纹理：" + changed + " 张已规范化，重命名 " + renamed + " 个扩展名。");
        }

        private static string NormalizeExtension(string path, ref int renamed)
        {
            var extension = Path.GetExtension(path);
            if (extension != ".Png" && extension != ".PNG")
            {
                return path;
            }

            var newPath = Path.ChangeExtension(path, ".png");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(newPath) != null)
            {
                return path;
            }

            var error = AssetDatabase.MoveAsset(path, newPath);
            if (string.IsNullOrEmpty(error))
            {
                renamed++;
                return newPath;
            }

            Debug.LogWarning("[UiArtPipeline] 重命名失败：" + path + " → " + newPath + " : " + error);
            return path;
        }

        private static void PatchMeta(string path)
        {
            var metaPath = path + ".meta";
            if (!File.Exists(metaPath))
            {
                return;
            }

            var text = File.ReadAllText(metaPath);
            var patched = text
                .Replace("nPOTScale: 1", "nPOTScale: 0")
                .Replace("enableMipMap: 1", "enableMipMap: 0");
            if (patched != text)
            {
                File.WriteAllText(metaPath, patched);
            }
        }

        private static Vector4 BorderFor(string name, int width, int height)
        {
            float left = 0f, bottom = 0f, right = 0f, top = 0f;

            if (name.StartsWith("UI_Button_"))
            {
                left = bottom = right = top = 48f;
            }
            else if (name == "UI_Card_IconSlot.png")
            {
                left = bottom = right = top = 20f;
            }
            else if (name == "UI_Chain_Slot_Trigger.png")
            {
                left = bottom = right = top = 24f;
            }
            else if (name == "UI_Chain_Slot_Effect.png")
            {
                left = bottom = right = top = 64f;
            }
            else if (name == "UI_Chain_Slot_Modifier.png")
            {
                left = bottom = right = top = 40f;
            }
            else if (name.StartsWith("UI_Chain_LibrarySlot_"))
            {
                left = bottom = right = top = 32f;
            }
            else if (name.StartsWith("UI_HUD_Slot_"))
            {
                left = bottom = right = top = 24f;
            }

            // 9-slice 的四条边不能占满整张图；留出至少 2px 中心区域。
            var maxX = Mathf.Max(0f, (width - 2f) * 0.5f);
            var maxY = Mathf.Max(0f, (height - 2f) * 0.5f);
            left = Mathf.Min(left, maxX);
            right = Mathf.Min(right, maxX);
            bottom = Mathf.Min(bottom, maxY);
            top = Mathf.Min(top, maxY);
            return new Vector4(left, bottom, right, top);
        }

        private static void BuildPrefabs()
        {
            CreatePrefab(UiRoot + "/BTN/UI_Button_Primary.png", UiRoot + "/BTN/UI_Button_Primary.prefab", true, false);
            CreatePrefab(UiRoot + "/BTN/UI_Button_Secondary.png", UiRoot + "/BTN/UI_Button_Secondary.prefab", true, false);
            CreatePrefab(UiRoot + "/Panels/UI_Panel_Main.png", UiRoot + "/Panels/UI_Panel_Main.prefab", false, false);
            CreatePrefab(UiRoot + "/Panels/UI_Chain_Panel.png", UiRoot + "/Panels/UI_Chain_Panel.prefab", false, false);
            CreatePrefab(UiRoot + "/Cards/UI_Card_IconSlot.png", UiRoot + "/Cards/UI_Card_IconSlot.prefab", false, true);

            var frames = new[]
            {
                "UI_Chain_Slot_Trigger", "UI_Chain_Slot_Effect", "UI_Chain_Slot_Modifier",
                "UI_Chain_LibrarySlot_common", "UI_Chain_LibrarySlot_equipped",
                "UI_Chain_LibrarySlot_hover", "UI_Chain_LibrarySlot_select",
                "UI_HUD_Slot_circle", "UI_HUD_Slot_square"
            };

            foreach (var frame in frames)
            {
                CreatePrefab(UiRoot + "/Frames/" + frame + ".png", UiRoot + "/Frames/" + frame + ".prefab", false, true);
            }
        }

        private static void CreatePrefab(string sourcePath, string prefabPath, bool button, bool preserveAspect)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            var sprite = GetSprite(sourcePath);
            if (sprite == null)
            {
                Debug.LogWarning("[UiArtPipeline] 找不到 Sprite：" + sourcePath);
                return;
            }

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                var existingImage = existingPrefab.GetComponent<Image>();
                var existingRect = existingPrefab.GetComponent<RectTransform>();
                if (existingImage != null)
                {
                    existingImage.sprite = sprite;
                    existingImage.type = sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
                    existingImage.preserveAspect = preserveAspect;
                    existingImage.raycastTarget = button;
                }

                if (existingRect != null)
                {
                    existingRect.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);
                }

                EditorUtility.SetDirty(existingPrefab);
                PrefabUtility.SavePrefabAsset(existingPrefab);
                return;
            }

            var go = new GameObject(Path.GetFileNameWithoutExtension(sourcePath),
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(sprite.texture.width, sprite.texture.height);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = button;

            if (button)
            {
                var buttonComponent = go.AddComponent<Button>();
                buttonComponent.targetGraphic = image;
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[UiArtPipeline] 创建 prefab：" + prefabPath);
        }

        private static void BuildCatalog()
        {
            EnsureFolder("Assets/RuneAshes/UI/Resources");
            var catalog = AssetDatabase.LoadAssetAtPath<UiSpriteCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UiSpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SetSprite(so, "_panelMain", GetSprite(UiRoot + "/Panels/UI_Panel_Main.png"));
            SetSprite(so, "_chainPanel", GetSprite(UiRoot + "/Panels/UI_Chain_Panel.png"));
            SetObject(so, "_healthBarPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + "/Bars/UI_HUD_HealthBar.prefab"));
            SetObject(so, "_expBarPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiRoot + "/Bars/UI_HUD_ExpBar.prefab"));
            SetSprite(so, "_cardIconSlot", GetSprite(UiRoot + "/Cards/UI_Card_IconSlot.png"));
            SetSprite(so, "_buttonPrimary", GetSprite(UiRoot + "/BTN/UI_Button_Primary.png"));
            SetSprite(so, "_buttonSecondary", GetSprite(UiRoot + "/BTN/UI_Button_Secondary.png"));
            SetSprite(so, "_chainSlotTrigger", GetSprite(UiRoot + "/Frames/UI_Chain_Slot_Trigger.png"));
            SetSprite(so, "_chainSlotEffect", GetSprite(UiRoot + "/Frames/UI_Chain_Slot_Effect.png"));
            SetSprite(so, "_chainSlotModifier", GetSprite(UiRoot + "/Frames/UI_Chain_Slot_Modifier.png"));
            SetSprite(so, "_hudSlotCircle", GetSprite(UiRoot + "/Frames/UI_HUD_Slot_circle.png"));
            SetSprite(so, "_hudSlotSquare", GetSprite(UiRoot + "/Frames/UI_HUD_Slot_square.png"));

            SetSprite(so, "_cardFrameCommon", PrefabSprite(UiRoot + "/Cards/UI_Card_Frame_common.prefab"));
            SetSprite(so, "_cardFrameRare", PrefabSprite(UiRoot + "/Cards/UI_Card_Frame_rare.prefab"));
            SetSprite(so, "_cardFrameFull", PrefabSprite(UiRoot + "/Cards/UI_Card_Frame_full.prefab"));
            SetSprite(so, "_cardFrameIntensify", PrefabSprite(UiRoot + "/Cards/UI_Card_Frame_intensify.prefab"));

            var iconPaths = Directory.GetFiles(UiRoot + "/RuneIcons", "UI_RuneIcon_*.png", SearchOption.TopDirectoryOnly)
                .OrderBy(p => p)
                .ToArray();
            var runeIcons = so.FindProperty("_runeIcons");
            runeIcons.arraySize = iconPaths.Length;
            for (var i = 0; i < iconPaths.Length; i++)
            {
                var id = Path.GetFileNameWithoutExtension(iconPaths[i]).Replace("UI_RuneIcon_", string.Empty);
                var element = runeIcons.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Id").stringValue = id;
                element.FindPropertyRelative("Icon").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i].Replace('\\', '/'));
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[UiArtPipeline] Sprite Catalog：" + CatalogPath + "（符文图标 " + iconPaths.Length + " 个）");
        }

        private static void SetSprite(SerializedObject so, string property, Sprite sprite)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = sprite;
            }
        }

        private static void SetObject(SerializedObject so, string property, Object value)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// 优先用 importer 生成的 Sprite；如果当前 Unity 版本/图片 NPOT 导致没有生成，
        /// 就从 Texture2D 直接创建 Sprite 资产。这样 prefab 和 Catalog 都不依赖“导入成功”。
        /// </summary>
        private static Sprite GetSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            var generatedPath = GeneratedSpritesRoot + "/Original/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(generatedPath);
            if (existing != null)
            {
                return existing;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }

            EnsureFolder(GeneratedSpritesRoot + "/Original");
            EnsureFolder(GeneratedSpritesRoot + "/Textures");

            var texturePath = GeneratedSpritesRoot + "/Textures/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) != null)
            {
                AssetDatabase.DeleteAsset(texturePath);
            }

            // 导入器可能把 NPOT 图非等比缩放到 512×128；
            // 这里直接读原始 PNG 字节，保留 398×186 这样的真实比例。
            var bytes = File.ReadAllBytes(path);
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!original.LoadImage(bytes))
            {
                Object.DestroyImmediate(original);
                return null;
            }

            original.name = name + "_Original";
            AssetDatabase.CreateAsset(original, texturePath);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var border = importer != null ? importer.spriteBorder : Vector4.zero;
            var created = Sprite.Create(original, new Rect(0f, 0f, original.width, original.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            if (created == null)
            {
                return null;
            }

            created.name = name;
            AssetDatabase.CreateAsset(created, generatedPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(generatedPath);
        }

        private static Sprite PrefabSprite(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var image = prefab == null ? null : prefab.GetComponent<Image>();
            return image == null ? null : image.sprite;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent.Replace('\\', '/'));
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
