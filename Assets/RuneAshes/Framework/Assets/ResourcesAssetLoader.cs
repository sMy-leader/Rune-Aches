// ─────────────────────────────────────────────────────────────────────────────
// 【链路】IAssetLoader ──> ResourcesAssetLoader（本文件，切片期实现）
// 【职责】用 Resources.Load 实现资源加载，并把失败显式报出来。
// 【坑】key 是相对 "Resources/" 的路径，且不带扩展名（例如 "Runes/Icon_Fire"）。
//      找不到时返回 null 并报 error——调用方必须自己判空，不要假设资源一定存在。
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

namespace RuneAshes.Framework
{
    public sealed class ResourcesAssetLoader : IAssetLoader
    {
        public T Load<T>(string key) where T : Object
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[AssetLoader] Empty asset key.");
                return null;
            }

            var asset = Resources.Load<T>(key);
            if (asset == null)
            {
                Debug.LogError("[AssetLoader] Missing asset at Resources/" + key + " (" + typeof(T).Name + ")");
            }

            return asset;
        }
    }
}
