// ─────────────────────────────────────────────────────────────────────────────
// 【链路】业务代码 ──> IAssetLoader（本文件）──> ResourcesAssetLoader（切片期）
//                                    └──> YooAsset（Phase 2，只换实现）
// 【职责】资源加载的唯一入口。业务代码禁止直接 Resources.Load。
// 【为什么现在只有 Resources】切片期资源少、不需要热更；但接口先立好，
//        将来换 YooAsset 时调用点不用改。
// 【Resources 的缺点】全部打进包、无法按需下载/热更、路径是裸字符串容易拼错。
// ─────────────────────────────────────────────────────────────────────────────
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
