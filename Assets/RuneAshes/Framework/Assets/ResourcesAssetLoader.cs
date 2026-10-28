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
