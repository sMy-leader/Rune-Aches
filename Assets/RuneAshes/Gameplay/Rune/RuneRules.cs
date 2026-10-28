namespace RuneAshes.Gameplay
{
    /// <summary>
    /// 已确认的符文规则常量（见 Docs/符文语义规范_v1.md 第十二节）。
    /// 改这里必须同步文档，否则实现与设计会漂移。
    /// </summary>
    public static class RuneRules
    {
        public const int MaxChainDepth = 2;
        public const int ProcBudgetPerTenthSecond = 64;
        public const int SliceSlotLimit = 4;
        public const int SliceEnergyLimit = 10;
        public const bool AllowDuplicateModifiers = false;
        public const bool SummonKillsProcOwner = false;
        public const bool DotCountsAsHit = false;
        public const float HitTriggerInternalCooldown = 0.05f;
        public const float InjuryTriggerInternalCooldown = 1f;
        public const float MaxDamageMultiplier = 20f;
        public const float MinModifierMultiplier = 0.3f;

        /// <summary>递归深度是否允许继续触发。</summary>
        public static bool CanTrigger(int depth)
        {
            return depth <= MaxChainDepth;
        }
    }
}
