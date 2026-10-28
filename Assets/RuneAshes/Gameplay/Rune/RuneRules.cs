// ─────────────────────────────────────────────────────────────────────────────
// 【链路】ChainCompiler（静态闸门）+ RuneCaster（运行时闸门）+ ProcBudget（全局闸门）
//        都从这里读常量
// 【职责】把"设计文档里拍板的规则"变成代码里唯一的数值来源。
// 【纪律】改这里必须同步 Docs/符文语义规范_v1.md，否则实现和文档会悄悄漂移；
//        反过来，如果代码里出现魔法数字，优先搬到这里。
// ─────────────────────────────────────────────────────────────────────────────
namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 已确认的符文规则常量（见 Docs/符文语义规范_v1.md 第十二节）。
    /// 改这里必须同步文档，否则实现与设计会漂移。
    /// </summary>
    public static class RuneRules
    {
        /// <summary>
        /// 递归深度上限。0 = 玩家直接施法，1 = 由这次施法派生的事件再触发，
        /// 2 = 派生的派生。再往上就砍断，防止"击杀→爆炸→击杀"无限递归。
        /// </summary>
        public const int MaxChainDepth = 2;

        /// <summary>全局 Proc 预算：每 0.1 秒最多 64 次。超出直接丢弃并计数，保护帧率。</summary>
        public const int ProcBudgetPerTenthSecond = 64;

        /// <summary>切片期上限：4 槽 = 1 触发 + 1 效果 + 2 修饰。meta 升级后才放宽。</summary>
        public const int SliceSlotLimit = 4;

        /// <summary>切片期能耗上限。当前符文组合下大约够"1 效果 + 2 中价修饰"。</summary>
        public const int SliceEnergyLimit = 10;

        /// <summary>同一条链里同修饰只能出现一次；重复叠同修饰会让乘区失控。</summary>
        public const bool AllowDuplicateModifiers = false;

        /// <summary>召唤物/环绕球的击杀是否算作玩家的击杀（决定能否触发 T2）。切片期关闭，避免自激循环。</summary>
        public const bool SummonKillsProcOwner = false;

        /// <summary>燃烧 DoT 的每跳是否算"命中"（决定能否触发 T3 命中类效果）。关闭，避免 DoT 自喂。</summary>
        public const bool DotCountsAsHit = false;

        /// <summary>命中触发的内部冷却：同一目标 0.05 秒内只算一次命中，防止多段弹幕瞬间刷满。</summary>
        public const float HitTriggerInternalCooldown = 0.05f;

        /// <summary>受伤触发的内部冷却：1 秒。防止"持续小伤害"把受伤型构筑变成永动机。</summary>
        public const float InjuryTriggerInternalCooldown = 1f;

        /// <summary>单次伤害的倍率上限，防止乘区叠出 NaN/溢出。</summary>
        public const float MaxDamageMultiplier = 20f;

        /// <summary>修饰的减益倍率下限：弹射衰减等最多把伤害压到 30%，避免无限弹射变零伤。</summary>
        public const float MinModifierMultiplier = 0.3f;

        /// <summary>
        /// [边界] 判断的是"当前 depth 还能不能触发"。
        /// depth=0 施法后派生 depth=1，depth=1 再派生 depth=2，depth=2 仍可触发一次，
        /// 之后 depth=3 被拒绝。改上限时记得同步语义规范里的示例。
        /// </summary>
        public static bool CanTrigger(int depth)
        {
            return depth <= MaxChainDepth;
        }
    }
}
