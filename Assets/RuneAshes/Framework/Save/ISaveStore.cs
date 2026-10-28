// ─────────────────────────────────────────────────────────────────────────────
// 【链路】业务代码 ──> ISaveStore（本文件）──> JsonSaveStore（文件实现）
// 【为什么是接口】将来接 Steam Cloud / 主机存档 / 测试用内存实现时只换实现，
//                业务代码不需要知道"存档到底写在哪"。
// 【纪律】只存 ID 与数值；不要把场景对象、ScriptableObject 引用塞进 SaveFile。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Framework
{
    public interface ISaveStore
    {
        /// <summary>实际写入位置。调试/日志/玩家反馈 bug 时非常有用。</summary>
        string SavePath { get; }

        /// <summary>主档或备份档任一存在即为 true。</summary>
        bool HasSave { get; }

        /// <summary>读取并自动迁移版本；两份都坏时返回全新的默认存档，不抛异常。</summary>
        SaveFile Load();

        void Save(SaveFile data);

        void Delete();
    }
}
