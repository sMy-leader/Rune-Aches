using UnityEngine;

namespace RuneAshes.Framework
{
    /// <summary>
    /// 资源加载入口。切片期走 Resources，Phase 2 换成 YooAsset 时只改实现。
    /// 业务代码禁止直接调用 Resources.Load。
    /// </summary>
    public interface IAssetLoader
    {
        T Load<T>(string key) where T : Object;
    }
}
